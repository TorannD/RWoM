using Verse;
using AbilityUser;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{    
    public class Effect_DragonStrike : Verb_UseAbility
    {
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

        public virtual void Effect()
        {
            LocalTargetInfo t = this.currentTarget;
            //
            //MightPowerSkill pwr = comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DragonStrike_pwr");
            //int pwrVal = pwr.level;
            //
            //if (this.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    pwrVal = mpwr.level;
            //}
            Pawn casterPawn = base.CasterPawn;
            CompAbilityUserMight comp = this.CasterPawn.GetCompAbilityUserMight();
            int pwrVal = TM_Calc.GetSkillPowerLevel(casterPawn, this.Ability.Def as TMAbilityDef, false);
            if (comp != null)
            {
                MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_DragonStrike, Mathf.RoundToInt(Rand.Range(8f, 15f) * (1 + (.1f * pwrVal) + (.05f * str.level))), 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                bool flag = t.Cell != default(IntVec3);
                if (flag)
                {
                    //this.Ability.PostAbilityAttempt();
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        ModCheck.GiddyUp.ForceDismount(base.CasterPawn);
                    }

                    LongEventHandler.QueueLongEvent(delegate
                    {
                        FlyingObject_DragonStrike flyingObject = (FlyingObject_DragonStrike)GenSpawn.Spawn(ThingDef.Named("FlyingObject_DragonStrike"), this.CasterPawn.Position, this.CasterPawn.Map);
                    flyingObject.Launch(this.CasterPawn, t, this.CasterPawn, dinfo2);
                    }, "LaunchingFlyer", false, null);
                }
            }
        }

        public override void PostCastShot(bool inResult, out bool outResult)
        {
            if (inResult)
            {
                this.Effect();
                outResult = true;
            }
            outResult = inResult;
        }
    }    
}
