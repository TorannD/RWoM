using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.TheOnlyAllowedMentalBreaks), MethodType.Getter)]
    public static class LichMentalBreakWhitelistPatch
    {
        public static void Postfix(Pawn ___pawn, ref IEnumerable<MentalBreakDef> __result)
        {
            var allowed = ClassPassiveEffects.Lich(___pawn)?.DataAtDegree(0)?.theOnlyAllowedMentalBreaks;
            if (allowed != null) __result = __result.Concat(allowed);
        }
    }

    [HarmonyPatch(typeof(MentalBreakWorker), nameof(MentalBreakWorker.BreakCanOccur))]
    public static class LichMentalBreakRestrictionPatch
    {
        public static void Postfix(MentalBreakWorker __instance, Pawn pawn, ref bool __result)
        {
            if (__result && __instance.def.mentalState != null
                && ClassPassiveEffects.Lich(pawn)?.DataAtDegree(0)?.disallowedMentalStates?.Contains(__instance.def.mentalState) == true)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
    public static class LichPassiveFactorPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var multiplier = AccessTools.Method(typeof(Trait), nameof(Trait.MultiplierOfStat));
            var story = AccessTools.Field(typeof(Pawn), nameof(Pawn.story));
            var calls = Enumerable.Range(0, System.Math.Max(0, code.Count - 2)).Where(i => code[i].Calls(multiplier)
                && code[i + 1].opcode == OpCodes.Mul && code[i + 2].IsStloc()).ToList();
            if (calls.Count != 1) return Fail(code);
            // Native 1.7: after equipment offsets, `if (pawn.story != null)` starts trait factors.
            // Insert at its null-check, so an empty trait list still receives the saved factor.
            int anchor = -1;
            for (int i = 0; i < calls[0] - 1; i++)
                if (code[i].LoadsField(story) && (code[i + 1].opcode == OpCodes.Brfalse || code[i + 1].opcode == OpCodes.Brfalse_S))
                    anchor = i;
            if (anchor < 0) return Fail(code);
            var store = code[calls[0] + 2];
            CodeInstruction load;
            if (store.opcode == OpCodes.Stloc_0) load = new CodeInstruction(OpCodes.Ldloc_0);
            else if (store.opcode == OpCodes.Stloc_1) load = new CodeInstruction(OpCodes.Ldloc_1);
            else if (store.opcode == OpCodes.Stloc_2) load = new CodeInstruction(OpCodes.Ldloc_2);
            else if (store.opcode == OpCodes.Stloc_3) load = new CodeInstruction(OpCodes.Ldloc_3);
            else load = new CodeInstruction(store.opcode == OpCodes.Stloc_S ? OpCodes.Ldloc_S : OpCodes.Ldloc, store.operand);
            var injected = new List<CodeInstruction> {
                new CodeInstruction(OpCodes.Dup), new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ClassPassiveEffects), nameof(ClassPassiveEffects.StatFactor))),
                load, new CodeInstruction(OpCodes.Mul), new CodeInstruction(store.opcode, store.operand)
            };
            injected[0].labels.AddRange(code[anchor].labels);
            code[anchor].labels.Clear();
            injected[0].blocks.AddRange(code[anchor].blocks);
            code[anchor].blocks.Clear();
            code.InsertRange(anchor, injected);
            return code;
        }

        private static IEnumerable<CodeInstruction> Fail(List<CodeInstruction> code)
        {
            Log.Warning("[RWoM] Lich progression stat factors disabled: native trait-factor stage was not uniquely recognized.");
            return code;
        }
    }
}
