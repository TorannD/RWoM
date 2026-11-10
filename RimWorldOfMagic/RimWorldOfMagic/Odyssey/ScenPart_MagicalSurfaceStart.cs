using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.Odyssey
{
    public class ScenPart_MagicalSurfaceStart : ScenPart
    {
        public override void PreMapGenerate()
        {
            // Quick-start and scenario editors may bypass the starting-site page.
            // Keep a chosen valid surface location; native random selection uses Surface.
            var init = Find.GameInitData;
            if (init != null && (!init.startingTile.Valid || init.startingTile.LayerDef != PlanetLayerDefOf.Surface))
                init.startingTile = TileFinder.RandomStartingTile();
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "CanDoNext")]
    public static class MagicalSurfaceStartingTilePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var nativeValidator = AccessTools.Method(typeof(TileFinder), nameof(TileFinder.IsValidTileForNewSettlement));
            var startingSiteValidator = AccessTools.Method(typeof(MagicalSurfaceStartingTilePatch), nameof(IsValidStartingTile));
            var code = instructions.ToList();
            if (code.Count(instruction => instruction.Calls(nativeValidator)) != 1)
                throw new InvalidOperationException("[RWoM] Starting-site validation layout changed; expected exactly one settlement validation call.");
            foreach (var instruction in code)
            {
                // Only player starting-site acceptance owns this restriction. World generation
                // also calls TileFinder for NPC settlements on Orbit and must remain native.
                if (instruction.Calls(nativeValidator)) instruction.operand = startingSiteValidator;
                yield return instruction;
            }
        }

        public static bool IsValidStartingTile(PlanetTile tile, StringBuilder reason, bool forGravship)
        {
            if (!forGravship && Find.GameInitData != null && tile.Valid &&
                Current.Game?.Scenario?.AllParts.OfType<ScenPart_MagicalSurfaceStart>().Any() == true &&
                tile.LayerDef != PlanetLayerDefOf.Surface)
            {
                reason?.Append("TM_MagicalSurfaceOnly".Translate());
                return false;
            }
            return TileFinder.IsValidTileForNewSettlement(tile, reason, forGravship);
        }
    }
}
