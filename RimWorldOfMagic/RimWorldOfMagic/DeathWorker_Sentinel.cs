using RimWorld;
using System;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    public class DeathWorker_Sentinel : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {
            for (int i = 0; i < 3; i++)
            {
                FleckMaker.ThrowSmoke(corpse.DrawPos, corpse.Map, Rand.Range(.5f, 1.1f));
            }
            FleckMaker.ThrowHeatGlow(corpse.Position, corpse.Map, 1f);
            SingleSpawnLoop(corpse.Position, corpse.Map);
            Pawn innerPawn = corpse.InnerPawn;
            innerPawn.SetFaction(Faction.OfAncients, null);
            corpse.Destroy();           
        }

        public void SingleSpawnLoop(IntVec3 position, Map map)
        {
            AbilityUser.SpawnThings spawnables = new SpawnThings();
            spawnables.def = ThingDef.Named("ChunkGranite");
            spawnables.spawnCount = 1;
            bool flag = spawnables.def != null;
            if (flag)
            {
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff)
                {
                    stuff = ThingDefOf.WoodLog;
                }
                Thing spawnedThing = ThingMaker.MakeThing(def, stuff);
                GenSpawn.Spawn(spawnedThing, position, map, Rot4.North, WipeMode.Vanish, false);                
            }
        }

    }
}

