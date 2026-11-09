using System;
using HarmonyLib;
using TorannMagic.Compatibility;
using Verse;

namespace TorannMagic.Odyssey
{
    // TickManager advances the clock before dispatching these lists. Buy coverage before
    // any pawn, plant, fire or projectile can read it at an interval boundary. Component
    // receipts make the normal/rare/long lists and subsequent map ticks idempotent.
    [HarmonyPatch(typeof(TickList), nameof(TickList.Tick))]
    public static class MagicalFieldTickPatch
    {
        public static void Prefix()
        {
            if (Current.Game == null || !ModsConfig.OdysseyActive) return;
            foreach (var map in Find.Maps)
            {
                Tick(map.GetComponent<MapComponent_ManaNetwork>());
                Tick(map.GetComponent<MapComponent_MagicalAtmosphere>());
                Tick(map.GetComponent<MapComponent_MagicalThermal>());
                Tick(map.GetComponent<MapComponent_MagicalCombat>());
                try { map.GetComponent<MapComponent_GrandAtmosphereDome>()?.PrepareTick(Find.TickManager.TicksGame); }
                catch (Exception exception) { TM_Log.Error("Odyssey.Fields", "Grand atmosphere dome payment failed before thing ticking.", exception); }
            }
        }

        private static void Tick(MapComponent component)
        {
            if (component == null) return;
            // Match native per-thing failure isolation: a payment callback must not abort
            // the original dispatcher, the remaining field components or another map.
            try { component.MapComponentTick(); }
            catch (Exception exception) { TM_Log.Error("Odyssey.Fields", component.GetType().Name + " payment failed before thing ticking.", exception); }
        }
    }
}
