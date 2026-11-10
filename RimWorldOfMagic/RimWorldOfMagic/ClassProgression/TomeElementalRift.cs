using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class TomeElementalRift
    {
        internal const string Marker = "TM_MasterTomeRift";
        private static readonly string[] Elements = {"Earth","Fire","Water","Wind"};
        internal static float Budget(float threat) =>
            !float.IsFinite(threat) || threat <= 0 ? 0 : Math.Min(2400f,.6f*threat);

        internal static Faction Faction()
        {
            var def = DefDatabase<FactionDef>.GetNamedSilentFail("TM_ElementalFaction");
            var faction = def == null ? null : Find.FactionManager.FirstFactionOfDef(def);
            return faction != null && !faction.defeated && faction.HostileTo(RimWorld.Faction.OfPlayer) ? faction : null;
        }

        internal static List<PawnKindDef> Kinds(string profile,float budget) =>
            Elements.Where(e => !(profile=="rwom.fire" && e=="Fire") && !(profile=="rwom.ice" && e=="Water") &&
                !(profile=="rwom.geomancer" && e=="Earth") && !(profile=="rwom.lightning" && e=="Wind"))
                .Select(e => DefDatabase<PawnKindDef>.GetNamedSilentFail("TM_Lesser"+e+"_Elemental"))
                .Where(k=>k?.race?.thingClass!=null && typeof(TMPawnSummoned).IsAssignableFrom(k.race.thingClass) &&
                    k.combatPower>0 && k.combatPower<=budget).ToList();

        internal static bool Empty(Map map,IntVec3 cell) => cell.InBounds(map) && cell.Standable(map) &&
            !cell.Roofed(map) && !cell.Fogged(map) && cell.GetThingList(map).Count == 0 && map.reachability.CanReachColony(cell);

        internal static List<IntVec3> Cells(Map map,IntVec3 center,int wanted) =>
            GenRadial.RadialCellsAround(center,6f,true).Where(cell=>Empty(map,cell)).Take(wanted).ToList();
    }
}
