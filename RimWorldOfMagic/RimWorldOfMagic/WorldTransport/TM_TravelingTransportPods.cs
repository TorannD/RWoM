using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.WorldTransport
{
    public class TM_TravelingTransportPods : TravellingTransporters
    {
        public IntVec3 destinationCell = IntVec3.Invalid;
        public bool draftFlag;
        public float TravelSpeed = NativeSpeed;
        private const float NativeSpeed = .00025f;
        private static readonly AccessTools.FieldRef<TravellingTransporters, float> Progress =
            AccessTools.FieldRefAccess<TravellingTransporters, float>("traveledPct");
        private static readonly Func<TravellingTransporters, float> NativeStep =
            AccessTools.MethodDelegate<Func<TravellingTransporters, float>>(
                AccessTools.PropertyGetter(typeof(TravellingTransporters), "TraveledPctStepPerTick"));

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref destinationCell, "TM_lightDestination", IntVec3.Invalid);
            Scribe_Values.Look(ref draftFlag, "TM_lightDrafted");
            Scribe_Values.Look(ref TravelSpeed, "TM_lightTravelSpeed", NativeSpeed);
        }

        protected override void TickInterval(int delta)
        {
            if (arrivalAction == null && destinationCell.IsValid)
            {
                var map = Find.Maps.Find(m => m.Tile == destinationTile);
                if (map != null && destinationCell.InBounds(map))
                    arrivalAction = new TM_TransportPodsArrivalAction_LandAtExactCell(map.Parent, destinationCell, draftFlag);
            }
            // The vanilla interval owns tick dispatch, the passenger manifest and arrival.
            // Only add the extra distance covered by a magic pod during that interval.
            if (TravelSpeed != NativeSpeed)
                Progress(this) += NativeStep(this) * delta * (TravelSpeed / NativeSpeed - 1f);
            base.TickInterval(delta);
        }

        public new void AddPod(ActiveTransporterInfo contents, bool justLeftTheMap)
            => AddTransporter(contents, justLeftTheMap);
    }
}
