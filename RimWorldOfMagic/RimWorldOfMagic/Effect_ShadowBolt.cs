using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_ShadowBolt : Verb_UseAbility
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

        public virtual void Effect()
        {
            var target = TargetsAoE?.Count > 0 ? TargetsAoE[0] : currentTarget;
            if (target.IsValid) SpawnOrb(CasterPawn,CasterPawn,target);
        }

        internal static void SpawnOrb(Pawn body, Pawn owner, LocalTargetInfo target)
        {
            var orb = (FlyingObject_ShadowBolt)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_ShadowBolt,body.Position,body.Map);
            orb.LaunchFromCaster(body,owner,target);
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
