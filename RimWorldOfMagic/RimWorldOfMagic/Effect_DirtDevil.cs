using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_DirtDevil : Verb_UseAbility
    {
        bool validTarg;

        public virtual void Effect()
        {
            LocalTargetInfo t = this.TargetsAoE[0];
            bool flag = t.Cell != default(IntVec3);
            if (flag)
            {
                Thing dirtDevil = new Thing();
                dirtDevil.def = TorannMagicDefOf.FlyingObject_DirtDevil;
                Pawn casterPawn = base.CasterPawn;
                FlyingObject_DirtDevil flyingObject = (FlyingObject_DirtDevil)GenSpawn.Spawn(ThingDef.Named("FlyingObject_DirtDevil"), this.CasterPawn.Position, this.CasterPawn.Map);
                flyingObject.Launch(this.CasterPawn, t.Cell, dirtDevil);
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
