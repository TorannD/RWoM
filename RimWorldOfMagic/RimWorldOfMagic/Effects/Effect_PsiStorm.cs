using Verse;
using AbilityUser;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_PsiStorm : Verb_UseAbility
    {
        //bool validTarg;

        //public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        //{
        //    if (targ.Thing != null && targ.Thing == this.caster)
        //    {
        //        return this.verbProps.targetParams.canTargetSelf;
        //    }
        //    if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
        //    {
        //        if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
        //        {
        //            ShootLine shootLine;
        //            validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
        //        }
        //        else
        //        {
        //            validTarg = false;
        //        }
        //    }
        //    else
        //    {
        //        validTarg = false;
        //    }
        //    return validTarg;
        //}

        public virtual void Effect()
        {
            LocalTargetInfo t = this.TargetsAoE[0];
            bool flag = t.Cell != default(IntVec3);
            if (flag)
            {
                Pawn casterPawn = base.CasterPawn;
                //this.Ability.PostAbilityAttempt();
                Thing psiOrb = new Thing();
                psiOrb.def = TorannMagicDefOf.FlyingObject_PsiStorm;
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    ModCheck.GiddyUp.ForceDismount(base.CasterPawn);
                }

                LongEventHandler.QueueLongEvent(delegate
                {
                    FlyingObject_PsiStorm flyingObject = (FlyingObject_PsiStorm)GenSpawn.Spawn(ThingDef.Named("FlyingObject_PsiStorm"), this.CasterPawn.Position, this.CasterPawn.Map);
                    flyingObject.Launch(this.CasterPawn, t.Cell, psiOrb);
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
