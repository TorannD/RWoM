using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic
{
    // Old-save migration: on every save load / new game init, inject the
    // knowledge comp into every existing humanlike pawn. The comp's first
    // tick imports legacy knowledge (schema v2 reconciles flags and ranks)
    // and the sync grants the vanilla bridge abilities — the save converts
    // to the new format in place instead of staying on the classic fallback.
    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    internal static class VanillaBridgeSaveConversionPatch
    {
        static void Postfix(Game __instance)
        {
            VanillaBridgeSync.ConvertExistingPawns(__instance);
        }
    }

    // Pawn_AbilityTracker deep-scribes its owned list. Give Scribe a filtered
    // local only for saving, leaving the live tracker and native load logic intact.
    [HarmonyPatch(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.ExposeData))]
    internal static class VanillaBridgeTrackerSavePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var snapshot = generator.DeclareLocal(typeof(List<Ability>));
            var field = AccessTools.Field(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.abilities));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldflda && Equals(instruction.operand, field))
                {
                    yield return new CodeInstruction(OpCodes.Ldloca, snapshot);
                    yield return CodeInstruction.Call(typeof(VanillaBridgeTrackerSavePatch), nameof(ListForScribe));
                }
            }
        }

        private static ref List<Ability> ListForScribe(ref List<Ability> live, ref List<Ability> snapshot)
        {
            if (Scribe.mode != LoadSaveMode.Saving || live == null || !live.Exists(a => a is TMBridgeAbility))
                return ref live;
            snapshot = live.FindAll(a => !(a is TMBridgeAbility));
            return ref snapshot;
        }
    }
}
