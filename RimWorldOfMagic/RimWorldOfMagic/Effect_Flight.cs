using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_Flight : Verb_UseAbility
    {
        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Cell == root) return false;
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
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

        public virtual void Effect()
            => TryLaunch();

        private bool TryLaunch()
        {
            Odyssey.ManualTravelSafety.BeginAttempt(Ability);
            if (!Odyssey.ManualTravelSafety.CanPay(Ability)) return false;
            LocalTargetInfo t = this.TargetsAoE[0];
            bool flag = t.Cell != base.CasterPawn.Position && CanHitTargetFrom(base.CasterPawn.Position, t)
                && Odyssey.ManualTravelSafety.CanComplete(CasterPawn, CasterPawn.Map, t.Cell,
                    (int)System.Math.Ceiling((CasterPawn.Position - t.Cell).LengthHorizontal * 5f));
            if (flag)
            {
                //this.Ability.PostAbilityAttempt();
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    ModCheck.GiddyUp.ForceDismount(base.CasterPawn);
                }
                base.CasterPawn.rotationTracker.Face(t.CenterVector3);
                FlyingObject_Flight flyingObject = (FlyingObject_Flight)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Flight"), this.CasterPawn.Position, this.CasterPawn.Map);
                flyingObject.Launch(this.CasterPawn, t.Cell, base.CasterPawn);
                if (CasterPawn.Spawned) return false;
                Odyssey.ManualTravelSafety.Settle(Ability);
                return true;
            }
            return false;
        }

        public override void PostCastShot(bool inResult, out bool outResult)
        {
            outResult = inResult && TryLaunch();
        }
    }    
}
