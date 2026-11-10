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
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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

        public static int GetWeaponDmg(Pawn pawn) =>
            ClassProgression.BladeMartialCast.WeaponDamage(pawn,pawn,.7f+.07f*TM_Calc.GetSkillPowerLevel(pawn,TorannMagicDefOf.TM_SeismicSlash));

        protected override bool TryCastShot()
        {
            PostCastShot(Slash(currentTarget),out bool success);
            return success;
        }

        internal bool Slash(LocalTargetInfo target)
        {
            var body=CasterPawn;
            if(!ClassProgression.BladeMartialCast.HasWeapon(body) || target.Cell==body.Position || !CanHitTargetFrom(body.Position,target))return false;
            currentTarget=target;
            var owner=Ability?.Pawn ?? body;
            var def=Ability?.Def as TMAbilityDef ?? TorannMagicDefOf.TM_SeismicSlash;
            int pwrVal=TM_Calc.GetSkillPowerLevel(owner,def);
            int verVal=TM_Calc.GetSkillVersatilityLevel(owner,def,true);
            var map=body.Map;
            this.origin = base.CasterPawn.Position.ToVector3();
            this.destination = this.currentTarget.Cell.ToVector3Shifted();
            this.ticksToImpact = Mathf.RoundToInt((this.origin - this.destination).magnitude);

            if (this.CasterPawn.equipment.Primary != null && !this.CasterPawn.equipment.Primary.def.IsRangedWeapon)
            {
                TMAbilityDef ad = (TMAbilityDef)this.Ability.Def;
                int dmgNum = Mathf.RoundToInt(ClassProgression.BladeMartialCast.StrikeDamage(body,owner) * ad.weaponDamageFactor * (1 + (.1f * pwrVal)));
                
                if (!this.CasterPawn.IsColonist && ModOptions.Settings.Instance.AIHardMode)
                {
                    dmgNum += 10;
                }

                Vector3 strikeVec = this.origin;
                DrawBlade(strikeVec, 0);
                for (int i = 0; i < this.StartingTicksToImpact; i++)
                {
                    strikeVec = this.ExactPosition;
                    if(!strikeVec.ToIntVec3().InBounds(map))break;
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
                    for (int j = 0; j < Mathf.Min(8,2+2*verVal); j++)
                    {
                        IntVec3 searchCell = strikeVec.ToIntVec3() + GenAdj.AdjacentCells8WayRandomized()[j];
                        if (!searchCell.InBounds(map)) continue;
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
            return true;
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
