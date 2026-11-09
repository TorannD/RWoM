using Verse;
using AbilityUser;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_PsionicDash : Verb_UseAbility
    {
        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if(CasterPawn?.Spawned!=true || !targ.IsValid || targ.Cell==root)return false;
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
            if (this.TargetsAoE == null || this.TargetsAoE.Count == 0 || CasterPawn?.Spawned != true) return;
            LocalTargetInfo t = this.TargetsAoE[0];
            bool flag = t.Cell != default(IntVec3);
            if (flag)
            {
                Pawn casterPawn = base.CasterPawn;
                //this.Ability.PostAbilityAttempt();
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    ModCheck.GiddyUp.ForceDismount(base.CasterPawn);
                }

                Map launchMap = casterPawn.Map;
                LongEventHandler.QueueLongEvent(delegate
                {
                    if (!casterPawn.Spawned || casterPawn.Map != launchMap || !CanHitTargetFrom(casterPawn.Position, t) ||
                        !Odyssey.ManualTravelSafety.CanComplete(casterPawn, launchMap, t.Cell, 180)) return;
                    FlyingObject_PsionicDash flyingObject = (FlyingObject_PsionicDash)GenSpawn.Spawn(ThingDef.Named("FlyingObject_PsionicDash"), this.CasterPawn.Position, this.CasterPawn.Map);
                    flyingObject.SetSkillOwner(this.Ability.Pawn);
                    flyingObject.Launch(this.CasterPawn, t.Cell, this.CasterPawn);
                    // Legacy queued jobs already paid psi before their windup. Settle only stamina/cooldown here.
                    if (flyingObject.GetDirectlyHeldThings().Contains(casterPawn))
                        Odyssey.ManualTravelSafety.Settle(this.Ability);
                }, "LaunchingFlyer", false, null);
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
