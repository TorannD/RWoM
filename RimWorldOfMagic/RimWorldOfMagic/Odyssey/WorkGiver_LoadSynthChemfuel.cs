using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    // Hauls chemfuel from ground/stockpile stacks into the magofuel reservoir's
    // synthesis hopper through the "Magic" work type. The hopper feeds the
    // auto-synthesis conversion (mana + chemfuel -> magofuel, 50/day).
    public class WorkGiver_LoadSynthChemfuel : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;
        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        private const float HopperLowThreshold = 50f;
        private const float HopperCapacity = 100f;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn.Map == null) yield break;
            foreach (var building in pawn.Map.listerBuildings.allBuildingsColonist)
                if (building.def == DefDatabase<ThingDef>.GetNamedSilentFail("TM_MagofuelReservoir")) yield return building;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (thing.IsForbidden(pawn) || !pawn.CanReserve(thing, 1, -1, null, forced)) return false;
            var bank = (thing as ThingWithComps)?.GetComp<CompMagofuelReservoir>();
            if (bank == null || !MagofuelReserve.IsStorage(bank) || bank.ChemfuelHopper >= HopperCapacity) return false;
            if (bank.ChemfuelHopper > HopperLowThreshold && !forced) return false;
            return FindNearbyChemfuelStack(pawn, thing.Position) != null
                && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly);
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            var stack = FindNearbyChemfuelStack(pawn, thing.Position);
            if (stack == null) return null;
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_LoadSynthChemfuel"), stack, thing);
            job.count = Math.Min(stack.stackCount, (int)(HopperCapacity - HopperLowThreshold));
            return job;
        }

        private static Thing FindNearbyChemfuelStack(Pawn pawn, IntVec3 near)
        {
            Thing best = null;
            float bestDist = float.MaxValue;
            foreach (var stack in pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Chemfuel))
            {
                if (stack.IsForbidden(pawn) || !pawn.CanReserve(stack, 1, -1, null, false)) continue;
                if (!pawn.Map.reachability.CanReach(pawn.Position, stack, PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly))) continue;
                float dist = (stack.Position - near).LengthHorizontalSquared;
                if (dist < bestDist) { bestDist = dist; best = stack; }
            }
            return best;
        }
    }
}
