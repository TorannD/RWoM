using RimWorld;
using System.Collections.Generic;
using Verse;
using AbilityUser;
using UnityEngine;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_SummonDemon : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            Map map = base.CasterPawn.Map;

            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            Pawn sacrificialPawn = comp.soulBondPawn;

            if (!sacrificialPawn.DestroyedOrNull() && sacrificialPawn.Spawned && !sacrificialPawn.Dead)
            {
                if (this.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                {
                    Job summonDemon = new Job(TorannMagicDefOf.JobDriver_SummonDemon, sacrificialPawn);
                    this.CasterPawn.jobs.TryTakeOrderedJob(summonDemon, JobTag.Misc);
                    return true;
                }
                else
                {
                    List<Pawn> allPawns = this.CasterPawn.Map.mapPawns.AllPawnsSpawned;
                    for(int i = 0; i < allPawns.Count; i++)
                    {
                        if(allPawns[i].IsColonist && allPawns[i].RaceProps.Humanlike)
                        {
                            if(allPawns[i].story.traits.HasTrait(TorannMagicDefOf.Succubus)) 
                            {
                                if (allPawns[i].CurJobDef == TorannMagicDefOf.JobDriver_SummonDemon)
                                {
                                    CompAbilityUserMagic supportComp = allPawns[i].GetCompAbilityUserMagic();
                                    if (supportComp.soulBondPawn == sacrificialPawn)
                                    {
                                        this.TryLaunchProjectile(ThingDef.Named("Projectile_SummonDemon"), this.CasterPawn);
                                        return true;
                                    }
                                    else
                                    {
                                        Log.Message("summon demon failed - warlock and succubus bonded pawns are not the same");
                                    }
                                }                                
                            }
                        }
                    }
                }
            }
            else
            {
                Log.Message("failed demon summoning - bonded pawn doesn't exist or unable to be sacrificed");
            }

            this.burstShotsLeft = 0;
            this.PostCastShot(flag, out flag);
            return false;
        }

    }
}
