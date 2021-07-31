using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic 
{
    class GenStep_MagicAdventureGenerator : GenStep
    {
        protected MagicAdventureWorker adventure = null;

        protected Dictionary<string, float> randomRoomEvents = new Dictionary<string, float>();

        protected CellRect adventureRegion;

        protected ResolveParams baseResolveParams;

        public override int SeedPart
        {
            get;
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            int num = map.Size.x / 5;
            int num2 = 3 * map.Size.x / 5;
            int num3 = map.Size.z / 5;
            int num4 = 3 * map.Size.z / 5;
            this.adventureRegion = new CellRect(num, num3, num2, num4);
            this.adventureRegion.ClipInsideMap(map);
            BaseGen.globalSettings.map = map;
            this.randomRoomEvents.Clear();
            IntVec3 playerStartSpot;
            CellFinder.TryFindRandomEdgeCellWith((IntVec3 v) => GenGrid.Standable(v, map), map, 0f, out playerStartSpot);
            MapGenerator.PlayerStartSpot = playerStartSpot;
            this.baseResolveParams = default(ResolveParams);
            foreach (string current in this.randomRoomEvents.Keys)
            {
                this.baseResolveParams.SetCustom<float>(current, this.randomRoomEvents[current], false);
            }
        }
    }
}
