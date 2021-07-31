using System;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class Toils_Enchant
    {
        public static void ErrorCheck(Pawn pawn, Thing haulThing)
        {
            if (!haulThing.Spawned)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which isn't spawned."
                }));
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
            }
            if (haulThing.stackCount == 0)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which had stackcount 0."
                }));
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
            }
            if (pawn.jobs.curJob.count <= 0)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Invalid count: ",
                    pawn.jobs.curJob.count,
                    ", setting to 1. Job was ",
                    pawn.jobs.curJob
                }));
                pawn.jobs.curJob.count = 1;
            }
        }

        public static Toil TakeEnchantGem(TargetIndex ind, int count)
        {
            return Toils_Enchant.TakeEnchantGem(ind, () => count);
        }

        public static Toil TakeEnchantGem(TargetIndex ind, Func<int> countGetter)
        {
            Toil takeThing = new Toil();
            takeThing.initAction = delegate
            {
                Pawn actor = takeThing.actor;
                Thing thing = actor.CurJob.GetTarget(ind).Thing;
                Toils_Enchant.ErrorCheck(actor, thing);
                int num = Mathf.Min(1, thing.stackCount);
                if (num <= 0)
                {
                    actor.jobs.curDriver.ReadyForNextToil();
                }
                else
                {
                    CompEnchant comp = actor.GetComp<CompEnchant>();
                    if (comp != null)
                    {
                        if (comp.enchantingContainer.Count > 0)
                        {
                            comp.enchantingContainer.TryDropAll(actor.Position, actor.Map, ThingPlaceMode.Near);                            
                        }
                        comp.enchantingContainer.TryAdd(thing.SplitOff(1), true);
                        thing.def.soundPickup.PlayOneShot(new TargetInfo(actor.Position, actor.Map, false));
                    }
                    else
                    {
                        Log.Message("comp was null");
                    }
                }
            };
            return takeThing;
        }
    }
}
