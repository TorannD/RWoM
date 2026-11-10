using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class WorkGiver_DonateManaAtSiphon : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn.Map == null) yield break;
            foreach (var building in pawn.Map.listerBuildings.allBuildingsColonist)
                if (building.GetComp<CompManaSiphon>() != null) yield return building;
        }
        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false) =>
            !thing.IsForbidden(pawn) && (thing as ThingWithComps)?.GetComp<CompManaSiphon>()?.CanBeginDonation(pawn) == true &&
            pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some);
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false) => HasJobOnThing(pawn, thing, forced)
            ? JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_DonateManaAtSiphon"), thing) : null;
    }
}
