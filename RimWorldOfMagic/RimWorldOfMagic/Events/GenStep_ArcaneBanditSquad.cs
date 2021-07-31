using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace TorannMagic
{
    public class GenStep_ArcaneBanditSquad : GenStep
    {
        public FloatRange pointsRange = new FloatRange(350f, 600f);

        public override int SeedPart
        {
            get;
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 intVec;
            if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => GenGrid.Standable(x, map) && !GridsUtility.Fogged(x, map) && GridsUtility.GetRoom(x, map).CellCount >= 4, map, out intVec))
            {
                float num = this.pointsRange.RandomInRange;
                Faction faction = map.ParentFaction;
                List<Pawn> list = new List<Pawn>();
                for (int i = 0; i < 50; i++)
                {
                    PawnKindDef pawnKindDef = GenCollection.RandomElementByWeight<PawnKindDef>(from kind in DefDatabase<PawnKindDef>.AllDefsListForReading
                                                                                               where kind.RaceProps.IsFlesh
                                                                                               select kind, (PawnKindDef kind) => 1f / kind.combatPower);
                    list.Add(PawnGenerator.GeneratePawn(pawnKindDef, faction));
                    num -= pawnKindDef.combatPower;
                    if (num <= 0f)
                    {
                        break;
                    }
                }
                IntVec3 intVec2 = default(IntVec3);
                for (int j = 0; j < list.Count; j++)
                {
                    IntVec3 intVec3 = CellFinder.RandomSpawnCellForPawnNear(intVec, map, 10);
                    intVec2 = intVec3;
                    GenSpawn.Spawn(list[j], intVec3, map, Rot4.Random, WipeMode.Vanish, false);
                }
                LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(intVec2), map, list);
            }
        }
    }
}
