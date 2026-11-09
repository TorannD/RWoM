using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace TorannMagic.WorldTransport
{
    public class TM_TransportPodsArrivalAction_LandAtExactCell : TransportersArrivalAction_LandInSpecificCell
    {
        private static readonly AccessTools.FieldRef<TransportersArrivalAction_LandInSpecificCell, MapParent> Destination =
            AccessTools.FieldRefAccess<TransportersArrivalAction_LandInSpecificCell, MapParent>("mapParent");
        private static readonly AccessTools.FieldRef<TransportersArrivalAction_LandInSpecificCell, IntVec3> LandingCell =
            AccessTools.FieldRefAccess<TransportersArrivalAction_LandInSpecificCell, IntVec3>("cell");

        public bool draftFlag;
        public TM_TransportPodsArrivalAction_LandAtExactCell() { }

        public TM_TransportPodsArrivalAction_LandAtExactCell(MapParent mapParent, IntVec3 cell, bool dFlag = false)
            : base(mapParent, cell) { draftFlag = dFlag; }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Older versions wrote the base defaults and then the actual destination
                // under the same keys. Verse reads the first match in the in-memory record.
                XmlNode record = Scribe.loader.curXmlParent;
                foreach (var key in new[] { "mapParent", "cell" })
                {
                    var entries = record.SelectNodes(key);
                    if (entries.Count > 1) record.InsertBefore(entries[entries.Count - 1], entries[0]);
                }
            }
            base.ExposeData();
            Scribe_Values.Look(ref draftFlag, "TM_lightDrafted");
        }

        public override void Arrived(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            TM_TransportPodsArrivalActionUtility.DropTravelingTransportPods(pods, LandingCell(this), Destination(this).Map, true, draftFlag);
        }
    }
}
