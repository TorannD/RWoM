using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_SeismicSlash : Verb_UseAbility
    {

        bool flag10;

        private static int verVal;
        private static int pwrVal;

        protected Vector3 origin;
        protected Vector3 destination;
        protected int ticksToImpact;

        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material bladeMat = MaterialPool.MatFrom("Spells/cleave", false);

        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((this.origin - this.destination).magnitude);
                bool flag = num < 1;
                if (flag)
                {
                    num = 1;
                }
                return num;
            }
        }

        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * 2;
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_SeismicSlash, true);
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_SeismicSlash);
            //MightPowerSkill pwr = comp.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SeismicSlash_pwr");
            //MightPowerSkill ver = comp.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SeismicSlash_ver");
            MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    MightPowerSkill mpwr = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    verVal = mver.level;
            //    pwrVal = mpwr.level;
            //}
            int dmgNum = 0;
            ThingWithComps weaponComp = pawn.equipment.Primary;
            float weaponDPS = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, false) * .7f;
            float dmgMultiplier = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier, false);
            float pawnDPS = pawn.GetStatValue(StatDefOf.MeleeDPS, false);
            float skillMultiplier = (.7f + (.07f * pwrVal));
            return dmgNum = Mathf.RoundToInt(skillMultiplier * dmgMultiplier * (pawnDPS + weaponDPS) * comp.mightPwr);
        }

        protected override bool TryCastShot()
        {
            CompAbilityUserMight comp = this.CasterPawn.GetComp<CompAbilityUserMight>();
            //MightPowerSkill ver = comp.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SeismicSlash_ver");
            pwrVal = TM_Calc.GetSkillPowerLevel(CasterPawn, this.Ability.Def as TMAbilityDef);
            CellRect cellRect = CellRect.CenteredOn(base.CasterPawn.Position, 1);
            Map map = base.CasterPawn.Map;
            cellRect.ClipInsideMap(map);

            IntVec3 centerCell = cellRect.CenterCell;
            this.origin = base.CasterPawn.Position.ToVector3();
            this.destination = this.currentTarget.Cell.ToVector3Shifted();
            this.ticksToImpact = Mathf.RoundToInt((this.origin - this.destination).magnitude);

            if (this.CasterPawn.equipment.Primary != null && !this.CasterPawn.equipment.Primary.def.IsRangedWeapon)
            {
                TMAbilityDef ad = (TMAbilityDef)this.Ability.Def;
                int dmgNum = Mathf.RoundToInt(comp.weaponDamage * ad.weaponDamageFactor * (1 + (.1f * pwrVal)));
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (!this.CasterPawn.IsColonist && settingsRef.AIHardMode)
                {
                    dmgNum += 10;
                }

                Vector3 strikeVec = this.origin;
                DrawBlade(strikeVec, 0);
                for (int i = 0; i < this.StartingTicksToImpact; i++)
                {
                    strikeVec = this.ExactPosition;
                    Pawn victim = strikeVec.ToIntVec3().GetFirstPawn(map);
                    if (victim != null && victim.Faction != base.CasterPawn.Faction)
                    {
                        DrawStrike(strikeVec.ToIntVec3(), strikeVec, map);
                        damageEntities(victim, null, dmgNum, DamageDefOf.Cut);
                    }
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.CasterPawn.DrawPos, this.currentTarget.CenterVector3)).ToAngleFlat();
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirt, strikeVec, this.CasterPawn.Map, .3f + (.08f * i), .05f, .15f, .38f, 0, 5f - (.2f * i), angle, angle);
                    if (i == 2)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Cleave, strikeVec, this.CasterPawn.Map, .6f + (.05f * i), .05f, .04f + (.03f * i), .15f, -10000, 30, angle, angle);
                    }
                    //FleckMaker.ThrowTornadoDustPuff(strikeVec, map, .6f, Color.white);
                    for (int j = 0; j < 2+(2*verVal); j++)
                    {
                        IntVec3 searchCell = strikeVec.ToIntVec3() + GenAdj.AdjacentCells8WayRandomized()[j];
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirt, searchCell.ToVector3Shifted(), this.CasterPawn.Map, .1f + (.04f * i), .05f, .04f, .28f, 0, 4f - (.2f * i), angle, angle);
                        //FleckMaker.ThrowTornadoDustPuff(searchCell.ToVector3(), map, .4f, Color.gray);
                        victim = searchCell.GetFirstPawn(map);
                        if (victim != null && victim.Faction != base.CasterPawn.Faction)
                        {
                            DrawStrike(searchCell, searchCell.ToVector3(), map);
                            damageEntities(victim, null, dmgNum, DamageDefOf.Cut);
                        }
                    }
                    this.ticksToImpact--;
                }
            }
            else
            {
                Messages.Message("MustHaveMeleeWeapon".Translate(
                    this.CasterPawn.LabelCap
                ), MessageTypeDefOf.RejectInput);
                return false;
            }

            this.burstShotsLeft = 0;
            this.PostCastShot(flag10, out flag10);
            return flag10;
        }

        private void DrawBlade(Vector3 center, float magnitude)
        {
            Graphics.DrawMesh(MeshPool.plane10, center, this.ExactRotation, Verb_SeismicSlash.bladeMat, 0);           
        }

        public void DrawStrike(IntVec3 center, Vector3 strikePos, Map map)
        {
            TM_MoteMaker.ThrowMultiStrike(strikePos, map, .5f);
            TM_MoteMaker.ThrowBloodSquirt(strikePos, map, 1.2f);
        }        

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.7f, 1.3f));
            dinfo = new DamageInfo(type, amt, 0, (float)-1, this.CasterPawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);            
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }
    }
}
