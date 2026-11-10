using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;
using System.Linq;

namespace TorannMagic
{    
    public class Effect_LightLance : Verb_UseAbility
    {
        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    if (this.CasterIsPawn && this.CasterPawn.apparel != null)
                    {
                        List<Apparel> wornApparel = this.CasterPawn.apparel.WornApparel;
                        for (int i = 0; i < wornApparel.Count; i++)
                        {
                            if (!wornApparel[i].AllowVerbCast(this))
                            {
                                return false;
                            }
                        }
                        validTarg = true;
                    }
                    else
                    {
                        validTarg = true;
                    }                    
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        internal static bool PayForPulse(Pawn owner, TMAbilityDef spell, int remaining, int total)
        {
            int stopAt = 3;
            if (!owner.DestroyedOrNull())
            {
                if (owner.GetCompAbilityUserMagic() != null) stopAt = TM_Calc.GetSkillPowerLevel(owner, spell, true);
                var light = owner.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD)?.TryGetComp<HediffComp_LightCapacitance>();
                if (light != null) light.LightEnergy -= 3f;
            }
            return remaining == total || remaining > stopAt;
        }

        protected override bool TryCastShot()
        {
            if (!PayForPulse(CasterPawn, Ability.Def as TMAbilityDef, burstShotsLeft, verbProps.burstShotCount)) return false;
            if (this.burstShotsLeft == this.verbProps.burstShotCount)
            {
                return base.TryCastShot();
            }
            bool outResult = true;
            PostCastShot(outResult, out outResult);
            if (!outResult) Ability.Notify_AbilityFailed(UseAbilityProps.refundsPointsAfterFailing);
            return outResult;
        }

        public virtual void Effect()
        {            
            LocalTargetInfo t = this.TargetsAoE[0];
            bool flag = t.Cell != default(IntVec3);
            if (flag)
            {
                Emit(CasterPawn, Ability, t);
            }
        }

        internal static void Emit(Pawn body, PawnAbility ability, LocalTargetInfo target)
        {
            var trial = ClassProgression.BrightmageTrials.CaptureSpawn(ability);
            var lance = (FlyingObject_LightLance)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_LightLance, body.Position, body.Map);
            lance.LaunchFromCaster(body, ability.Pawn, target);
            ClassProgression.BrightmageTrials.CompleteSpawn(trial, lance);
        }

        public override void PostCastShot(bool inResult, out bool outResult)
        {
            outResult = inResult;
            this.Effect();            
        }

        
    }    
}
