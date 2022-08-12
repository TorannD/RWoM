using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_SpiritOfLight : Verb_UseAbility
    {
        bool validTarg;

        public virtual void Effect()
        {
            CompAbilityUserMagic comp = CasterPawn.GetCompAbilityUserMagic();
            if (comp.SoL != null)
            {
                comp.SoL.shouldDismiss = true;
            }
            else
            {
                LocalTargetInfo t = this.TargetsAoE[0];
                bool flag = t.Cell != default(IntVec3);
                if (flag)
                {
                    Thing sol = new Thing();
                    sol.def = TorannMagicDefOf.FlyingObject_SpiritOfLight;

                    LongEventHandler.QueueLongEvent(delegate
                    {
                        FlyingObject_SpiritOfLight flyingObject = (FlyingObject_SpiritOfLight)GenSpawn.Spawn(ThingDef.Named("FlyingObject_SpiritOfLight"), this.CasterPawn.Position, this.CasterPawn.Map);
                        flyingObject.Launch(this.CasterPawn, t.Cell, sol);
                        flyingObject.shouldDismiss = false;
                        comp.SoL = flyingObject;
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
