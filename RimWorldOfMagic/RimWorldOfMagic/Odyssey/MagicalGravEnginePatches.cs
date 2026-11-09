using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    [HarmonyPatch]
    public static class MagicalGravEngineFacilityPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.CanLinkTo));
            yield return AccessTools.Method(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.IsFacilityActive));
        }
        // Check activity as well as linking: old saves and optional mods can supply links directly.
        public static bool Prefix(CompAffectedByFacilities __instance, Thing facility, ref bool __result)
        {
            bool magicExtender = facility?.def?.defName == "TM_MagicalGravExtender";
            bool magicEngine = __instance.parent is Building_MagicalGravEngine;
            bool supportFacility = facility?.TryGetComp<CompFacility>()?.Props.statOffsets?
                .Any(s => s.stat == StatDefOf.SubstructureSupport && s.value != 0f) == true;
            if ((magicExtender && !magicEngine) || (magicEngine && supportFacility && !magicExtender))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}

namespace TorannMagic.Odyssey
{
    [HarmonyPatch(typeof(RimWorld.GravshipUtility), nameof(RimWorld.GravshipUtility.GetPlayerGravEngine_NewTemp))]
    public static class MagicalGravEngineMapLookupPatch
    {
        // The native getter caches per map/tick, but its postfix still runs on every
        // cache hit. Cache misses too; otherwise quest maps repeatedly walk every
        // thing and inventory. Weak keys do not retain discarded expedition maps.
        private sealed class Lookup
        {
            public int tick = int.MinValue;
            public Building_MagicalGravEngine engine;
        }
        private static readonly ConditionalWeakTable<Map, Lookup> lookups = new ConditionalWeakTable<Map, Lookup>();

        public static void Postfix(Map map, ref Building_GravEngine __result)
        {
            if (__result is Building_MagicalGravEngine && __result.Faction?.IsPlayer != true) __result = null;
            if (__result != null || !ModsConfig.OdysseyActive || map == null) return;
            Lookup cached = lookups.GetOrCreateValue(map);
            int tick = Find.TickManager.TicksGame;
            if (cached.tick == tick && (cached.engine == null ||
                (!cached.engine.Destroyed && cached.engine.Faction?.IsPlayer == true && cached.engine.MapHeld == map)))
            {
                __result = cached.engine;
                return;
            }
            cached.engine = null;
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.GetInnerIfMinified() is Building_MagicalGravEngine engine && engine.Faction?.IsPlayer == true)
                { cached.engine = engine; break; }
            }
            if (cached.engine == null) cached.engine = FindHeldEngine(map);
            cached.tick = tick;
            __result = cached.engine;
        }

        internal static Building_MagicalGravEngine FindHeldEngine(IThingHolder holder)
        {
            foreach (Thing thing in ThingOwnerUtility.GetAllThingsRecursively(holder))
                if (thing.GetInnerIfMinified() is Building_MagicalGravEngine engine && engine.Faction?.IsPlayer == true)
                    return engine;
            return null;
        }
    }

    [HarmonyPatch(typeof(RimWorld.GravshipUtility), nameof(RimWorld.GravshipUtility.PlayerHasGravEngine), new System.Type[0])]
    public static class MagicalGravEngineWorldLookupPatch
    {
        public static void Postfix(ref bool __result)
        {
            if (__result || !ModsConfig.OdysseyActive) return;
            __result = Building_MagicalGravEngine.PlayerHasWorldEngine();
        }
    }
}

namespace TorannMagic.Odyssey
{
    [HarmonyPatch(typeof(CompGravshipFacility), nameof(CompGravshipFacility.CanBeActive), MethodType.Getter)]
    public static class MagicalGravEngineFacilityActivityPatch
    {
        public static void Postfix(CompGravshipFacility __instance, ref bool __result)
        {
            if (!__result || __instance.Props.statOffsets?.Any(s => s.stat == StatDefOf.SubstructureSupport && s.value != 0f) != true) return;
            bool magicExtender = __instance.parent.def.defName == "TM_MagicalGravExtender";
            if (__instance.engine != null)
            {
                if ((__instance.engine is Building_MagicalGravEngine) != magicExtender) __result = false;
                return;
            }
            // Native footprint validity asks CanBeActive even before a facility has a link.
            // Only a compatible nearby hull may activate that footprint.
            __result = __instance.parent.Map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>()
                .Any(engine => (engine is Building_MagicalGravEngine) == magicExtender &&
                    __instance.parent.Position.InHorDistOf(engine.Position, __instance.Props.maxDistance) &&
                    (__instance.Props.onlyRequiresLooseConnection ? engine.LooselyConnectedToGravEngine(__instance.parent) : engine.OnValidSubstructure(__instance.parent)));
        }
    }
}
