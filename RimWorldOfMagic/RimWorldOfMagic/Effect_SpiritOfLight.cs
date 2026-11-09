using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_SpiritOfLight : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool ApplyHostedEffect()
        {
            var owner = Ability?.Pawn ?? CasterPawn;
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null || CasterPawn?.Spawned != true) return false;
            if (comp.SoL != null && !comp.SoL.Destroyed)
            {
                comp.SoL.shouldDismiss = true;
                return true;
            }
            var spirit = (FlyingObject_SpiritOfLight)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_SpiritOfLight,CasterPawn.Position,CasterPawn.Map);
            try
            {
                spirit.Launch(owner,CasterPawn.Position,null);
                comp.SoL = spirit;
            }
            catch { if (!spirit.Destroyed) spirit.Destroy(); throw; }
            ClassProgression.BrightmageLifecycle.SynchronizeControl(comp);
            return true;
        }

        public virtual void Effect() => ApplyHostedEffect();
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
