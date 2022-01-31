using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using TorannMagic.Golems;

namespace TorannMagic
{
    public class DeathWorker_StoneGolem : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {
            TMPawnGolem innerPawn = corpse.InnerPawn as TMPawnGolem;
            if (innerPawn != null)
            {                
                FleckMaker.ThrowHeatGlow(corpse.Position, corpse.Map, 1f);
                Building_TMGolemStone b = innerPawn.Golem.InnerWorkstation as Building_TMGolemStone;
                if (b != null && b.Stuff != null)
                {
                    SingleSpawnLoop(corpse.Position, corpse.Map, b);
                }
            }
            
            //innerPawn.SetFaction(Faction.OfAncients, null);
            corpse.Destroy();           
        }

        public void SingleSpawnLoop(IntVec3 position, Map map, Building_TMGolemStone building)
        {
            AbilityUser.SpawnThings spawnables = new SpawnThings();
            if (building.madeFromChunk != null)
            {
                spawnables.def = building.madeFromChunk;
                spawnables.spawnCount = 1;
            }
            else if(building.Stuff != null)
            {
                spawnables.def = building.Stuff;
                spawnables.spawnCount = Rand.Range(20, 30);
            }
            bool flag = spawnables.def != null;
            if (flag)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 deathPos = position.ToVector3Shifted();
                    deathPos.x += Rand.Range(-1f, 1f);
                    deathPos.z += Rand.Range(-1f, 1f);
                    FleckMaker.ThrowSmoke(deathPos, map, Rand.Range(.7f, 1.6f));

                    ThingDef def = spawnables.def;

                    Thing spawnedThing = ThingMaker.MakeThing(def);
                    spawnedThing.stackCount = spawnables.spawnCount;
                    GenSpawn.Spawn(spawnedThing, position, map, Rot4.North, WipeMode.Vanish, false);
                }
            }
        }

    }
}

