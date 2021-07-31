using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TorannMagic.TMDefs
{
    public sealed class TM_SpawnThings
    {
        //ThingDef the recipe spawns
        public ThingDef resultSpawnThing = new ThingDef();
        public PawnKindDef resultPawnKindDef = new PawnKindDef();
        //Number of things to spawn
        public int spawnThingCount = 0;
        public int spawnThingStackCount = 1;
        //If thing is a magic summon
        public int summonDuration = 0;
        public bool summonTemporary = true;
        //Try to spawn thing as hostile
        public bool spawnHostile = false;

        //How many times to execute
        public IntRange countRange = new IntRange(1, 1);
    }
}
