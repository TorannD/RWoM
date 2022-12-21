﻿using Verse;
using AbilityUser;
using System.Collections.Generic;
using RimWorld;

namespace TorannMagic
{    
    public class Effect_DeathBolt : Verb_UseAbility
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
            LocalTargetInfo t = this.TargetsAoE[0];
            bool flag = t.Cell != default(IntVec3);
            if (flag)
            {
                Thing launchedThing = new Thing()
                {
                    def = TorannMagicDefOf.FlyingObject_DeathBolt
                };
                Pawn casterPawn = base.CasterPawn;
                //LongEventHandler.QueueLongEvent(delegate
                //{
                    FlyingObject_DeathBolt flyingObject = (FlyingObject_DeathBolt)GenSpawn.Spawn(ThingDef.Named("FlyingObject_DeathBolt"), this.CasterPawn.Position, this.CasterPawn.Map);
                    flyingObject.Launch(this.CasterPawn, t.Cell, launchedThing);
                //}, "LaunchingFlyer", false, null);
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
