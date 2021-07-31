using System;
using Verse;

namespace TorannMagic.SihvRMagicScrollScribe
{
    class SihvSpawnThings
    {
        public static void SpawnThingDefOfCountAt(ThingDef of, int count, TargetInfo target)
        {

            Thing thing = ThingMaker.MakeThing(of, null);
            thing.stackCount = Math.Min(count, of.stackLimit);
            GenPlace.TryPlaceThing(thing, target.Cell, target.Map, ThingPlaceMode.Near, null);
            count -= thing.stackCount;
            
        }
    }
}
