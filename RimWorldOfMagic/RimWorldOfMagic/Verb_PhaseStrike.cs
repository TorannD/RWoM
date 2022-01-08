using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_PhaseStrike : Verb_UseAbility  
    {
        bool arg_41_0;
        bool arg_42_0;
        MightPowerSkill pwr;
        MightPowerSkill ver;
        MightPowerSkill str;

        private int verVal;
        private int pwrVal;
        private int dmgNum = 0;

        bool validTarg;
        IntVec3 arg_29_0;

        private static readonly Color bladeColor = new Color(160f, 160f, 160f);
        private static readonly Material bladeMat = MaterialPool.MatFrom("Spells/cleave", false);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    //out of range
                    validTarg = true;
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

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
            ThingWithComps weaponComp = pawn.equipment.Primary;
            float weaponDPS = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, false) * .7f;
            float dmgMultiplier = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier, false);
            float pawnDPS = pawn.GetStatValue(StatDefOf.MeleeDPS, false);
            float skillMultiplier = (.6f) * comp.mightPwr;
            return Mathf.RoundToInt(skillMultiplier * dmgMultiplier * (pawnDPS + weaponDPS));
        }

        protected override bool TryCastShot()
        {
            bool result = false;
            bool arg_40_0;

            CompAbilityUserMight comp = this.CasterPawn.GetComp<CompAbilityUserMight>();
            verVal = TM_Calc.GetSkillVersatilityLevel(CasterPawn, this.Ability.Def as TMAbilityDef);
            pwrVal = TM_Calc.GetSkillPowerLevel(CasterPawn, this.Ability.Def as TMAbilityDef);
            //pwr = comp.MightData.MightPowerSkill_PhaseStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PhaseStrike_pwr");
            //ver = comp.MightData.MightPowerSkill_PhaseStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PhaseStrike_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            //if (base.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    MightPowerSkill mpwr = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    verVal = mver.level;
            //    pwrVal = mpwr.level;
            //}
            if (this.CasterPawn.equipment.Primary != null && !this.CasterPawn.equipment.Primary.def.IsRangedWeapon)
            {
                TMAbilityDef ad = (TMAbilityDef)this.Ability.Def;
                this.dmgNum = Mathf.RoundToInt(comp.weaponDamage * ad.weaponDamageFactor);
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (!this.CasterPawn.IsColonist && settingsRef.AIHardMode)
                {
                    this.dmgNum += 10;
                }

                if (this.currentTarget != null && base.CasterPawn != null)
                {
                    arg_29_0 = this.currentTarget.Cell;
                    Vector3 vector = this.currentTarget.CenterVector3;
                    arg_40_0 = this.currentTarget.Cell.IsValid;
                    arg_41_0 = vector.InBounds(base.CasterPawn.Map);
                    arg_42_0 = true; // vector.ToIntVec3().Standable(base.CasterPawn.Map);
                }
                else
                {
                    arg_40_0 = false;
                }
                bool flag = arg_40_0;
                bool flag2 = arg_41_0;
                bool flag3 = arg_42_0;
                if (flag)
                {
                    if (flag2 & flag3)
                    {
                        Pawn p = this.CasterPawn;
                        Map map = this.CasterPawn.Map;
                        IntVec3 pLoc = this.CasterPawn.Position;
                        bool drafted = p.Drafted;
                        if (p.IsColonist)
                        {
                            try
                            {
                                ThingSelectionUtility.SelectNextColonist();
                                this.CasterPawn.DeSpawn();
                                //p.SetPositionDirect(this.currentTarget.Cell);
                                SearchForTargets(arg_29_0, (2f + (float)(.5f * verVal)), map, p);
                                GenSpawn.Spawn(p, this.currentTarget.Cell, map);
                                DrawBlade(p.Position.ToVector3Shifted(), 4f + (float)(verVal));
                                p.drafter.Drafted = drafted;
                                if (ModOptions.Settings.Instance.cameraSnap)
                                {
                                    CameraJumper.TryJumpAndSelect(p);
                                }
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BladeSweep, this.CasterPawn.DrawPos, this.CasterPawn.Map, 1.4f + .4f * ver.level, .04f, 0f, .18f, 1000, 0, 0, Rand.Range(0, 360));
                            }
                            catch
                            {
                                if(!CasterPawn.Spawned)
                                {
                                    GenSpawn.Spawn(p, pLoc, map);
                                    p.drafter.Drafted = true;
                                    Log.Message("Phase strike threw exception - despawned pawn has been recovered at casting location");
                                }
                            }

                        }
                        else
                        {
                            this.CasterPawn.DeSpawn();
                            SearchForTargets(arg_29_0, (2f + (float)(.5f * verVal)), map, p);
                            GenSpawn.Spawn(p, this.currentTarget.Cell, map);
                            DrawBlade(p.Position.ToVector3Shifted(), 4f + (float)(verVal));
                        }
                    
                        //this.CasterPawn.SetPositionDirect(this.currentTarget.Cell);
                        //base.CasterPawn.SetPositionDirect(this.currentTarget.Cell);
                        //this.CasterPawn.pather.ResetToCurrentPosition();
                        result = true;
                    }
                    else
                    {
                    
                        Messages.Message("InvalidTargetLocation".Translate(), MessageTypeDefOf.RejectInput);
                    }
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
            //this.ability.TicksUntilCasting = (int)base.UseAbilityProps.SecondsToRecharge * 60;
            return result;
        }

        public void SearchForTargets(IntVec3 center, float radius, Map map, Pawn pawn)
        {
            Pawn victim = null;
            IntVec3 curCell;            
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(center, radius, true);
            for (int i = 0; i < targets.Count(); i++)
            {
                curCell = targets.ToArray<IntVec3>()[i];
                FleckMaker.ThrowDustPuff(curCell, map, .2f);
                if (curCell.InBounds(map) && curCell.IsValid)
                {
                    victim = curCell.GetFirstPawn(map);
                }

                if (victim != null && victim.Faction != pawn.Faction)
                {
                    if(Rand.Chance(.1f + .15f*pwrVal))
                    {
                        this.dmgNum *= 3;
                        MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Critical Hit", -1f);
                    }
                    DrawStrike(center, victim.Position.ToVector3(), map);
                    damageEntities(victim, null, this.dmgNum, TMDamageDefOf.DamageDefOf.TM_PhaseStrike);
                }
                targets.GetEnumerator().MoveNext();
            }
        }

        public void DrawStrike(IntVec3 center, Vector3 strikePos, Map map)
        {
            TM_MoteMaker.ThrowCrossStrike(strikePos, map, 1f);
            TM_MoteMaker.ThrowBloodSquirt(strikePos, map, 1.5f);
        }

        private void DrawBlade(Vector3 center, float magnitude)
        {

            Vector3 vector = center;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(magnitude, magnitude, (1.5f * magnitude));
            Matrix4x4 matrix = default(Matrix4x4);

            for (int i = 0; i < 6; i++)
            {
                float angle = Rand.Range(0, 360);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Verb_PhaseStrike.bladeMat, 0);
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.5f, 1.5f));
            dinfo = new DamageInfo(type, amt, 0, (float)-1, this.CasterPawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }
    }
}
