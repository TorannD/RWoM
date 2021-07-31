using RimWorld;
using System;
using Verse;
using System.Collections.Generic;

namespace TorannMagic
{
    public class DeathWorker_Skeletal : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {
            for (int i = 0; i < 3; i++)
            {
                FleckMaker.ThrowSmoke(corpse.DrawPos, corpse.Map, Rand.Range(.5f, 1.1f));
            }
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, corpse.DrawPos, corpse.Map, 1f, .25f, 0f, .25f, 0, Rand.Range(2f, 3f), 0, 0);
            //FleckMaker.ThrowHeatGlow(corpse.Position, corpse.Map, 1f);
            List<Thing> rewards = new List<Thing>();
            Thing arcalleum = ThingMaker.MakeThing(TorannMagicDefOf.TM_Arcalleum, null);
            if (corpse.Faction != Faction.OfPlayer)
            {
                if (corpse.InnerPawn.def == TorannMagicDefOf.TM_SkeletonR)
                {
                    arcalleum.stackCount = Rand.Range(4, 8);
                    rewards.Add(arcalleum);                   
                }
                else if (corpse.InnerPawn.def == TorannMagicDefOf.TM_GiantSkeletonR)
                {
                    arcalleum.stackCount = Rand.Range(25, 60);
                    rewards.Add(arcalleum);
                }
                else if (corpse.InnerPawn.def == TorannMagicDefOf.TM_SkeletonLichR)
                {
                    arcalleum.stackCount = Rand.Range(40, 80);
                    rewards.Add(arcalleum);

                    Thing tome = ThingMaker.MakeThing(TM_Data.MageBookList().RandomElement(), null);
                    tome.stackCount = 1;
                    rewards.Add(tome);
                    rewards.AddRange(ItemCollectionGenerator_Internal_Arcane.Generate(2000));
                }
                for (int i = 0; i < rewards.Count; i++)
                {
                    GenPlace.TryPlaceThing(rewards[i], corpse.Position, corpse.Map, ThingPlaceMode.Near);
                }
            }
            corpse.Destroy();            
        }
    }
}
