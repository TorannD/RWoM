using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritMapLoss
    {
        internal static List<Pawn> Remaining(Map map)
        {
            var souls = new HashSet<Pawn>();
            if (map == null || !ClassProgressionFeature.Enabled) return new List<Pawn>();
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_SoulStone");
            if (def != null)
            {
                var stones = new List<Thing_SoulStone>();
                ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForDef(def), stones);
                foreach (var stone in stones)
                    if (OwnedOn(map, stone.ContainedSpirit)) souls.Add(stone.ContainedSpirit);
            }
            var keeper = map.GetComponent<MapComponent_SpiritCollapse>();
            if (keeper != null)
                foreach (var thing in keeper.GetDirectlyHeldThings())
                    if (thing is Pawn pawn && OwnedOn(map, pawn)) souls.Add(pawn);
            return souls.OrderBy(p => p.thingIDNumber).ToList();
        }

        internal static bool OwnedOn(Map map, Pawn pawn)
        {
            if (!SpiritRebirthState.TryGet(pawn, out var state) || pawn.holdingOwner?.Owner == null
                || ThingOwnerUtility.GetRootMap(pawn.holdingOwner.Owner) != map) return false;
            return state.HasValidStone(pawn) || MapComponent_SpiritCollapse.IsPendingAccountingOwner(pawn);
        }

        internal static void DiscardRemaining(Map map)
        {
            foreach (var soul in Remaining(map))
            {
                if (!OwnedOn(map, soul)) continue;
                var holder = soul.holdingOwner;
                var result = SpiritRebirthTerminalLoss.TryDiscardOnTerminalLoss(soul, holder);
                if (result != SpiritTerminalLossResult.Completed)
                    throw new InvalidOperationException("Could not discard the map-bound Spirit " + soul.ThingID + ": " + result);
            }
        }

        internal static StringBuilder AppendWarning(StringBuilder text, MapParent settlement)
        {
            var souls = Remaining(settlement?.Map);
            if (souls.Count != 0)
            {
                if (text.Length != 0) text.AppendLine().AppendLine();
                text.Append("TM_SoulStoneAbandonWarning".Translate(string.Join(", ", souls.Select(p => p.LabelShortCap))).ToString());
            }
            return text;
        }
    }

    // Map removal is committed here, after map-parent callbacks can evacuate cargo,
    // but before the native pawn transfer. A normal game save does not call Deinit.
    [HarmonyPatch(typeof(MapDeiniter), nameof(MapDeiniter.Deinit))]
    internal static class SpiritMapDeinitPatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Prefix(Map map, bool __runOriginal)
        {
            if (__runOriginal) SpiritMapLoss.DiscardRemaining(map);
        }
    }

    [HarmonyPatch(typeof(Dialog_FormCaravan), "TrySend")]
    internal static class SpiritReformWarningPatch
    {
        private static readonly System.Reflection.MethodInfo send = AccessTools.Method(typeof(Dialog_FormCaravan), "TrySend");
        private static Dialog_FormCaravan confirmed;

        private static bool LeavesWithParty(Pawn soul, List<TransferableOneWay> cargo)
        {
            if (!(soul.holdingOwner?.Owner is Thing_SoulStone stone)) return false;
            for (IThingHolder holder = stone; holder != null; holder = holder.ParentHolder)
            {
                if (!(holder is Thing item)) continue;
                var row = cargo.Find(r => r.things.Contains(item));
                if (row != null)
                    return row.CountToTransfer == row.MaxCount; // partial shared row cannot guarantee this exact item
            }
            return false;
        }

        static bool Prefix(Dialog_FormCaravan __instance, bool ___reform, Map ___map, List<TransferableOneWay> ___transferables)
        {
            if (!___reform || ReferenceEquals(confirmed, __instance)) return true;
            var left = SpiritMapLoss.Remaining(___map).Where(p => !LeavesWithParty(p, ___transferables)).ToList();
            if (left.Count == 0) return true;
            var text = "TM_SoulStoneAbandonWarning".Translate(string.Join(", ", left.Select(p => p.LabelShortCap)));
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () =>
            {
                confirmed = __instance;
                try { send.Invoke(__instance, null); }
                finally { confirmed = null; }
            }));
            return false;
        }
    }
    [HarmonyPatch(typeof(SettlementAbandonUtility), "TryAbandonWithColonyCheck")]
    internal static class SpiritMapAbandonWarningPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var length = AccessTools.PropertyGetter(typeof(StringBuilder), nameof(StringBuilder.Length));
            bool afterThoughts = false;
            foreach (var instruction in instructions)
            {
                if (instruction.operand is System.Reflection.MethodInfo called && called.DeclaringType == typeof(RimWorld.PawnDiedOrDownedThoughtsUtility)
                    && called.Name == "BuildMoodThoughtsListString") afterThoughts = true;
                if (afterThoughts && instruction.Calls(length))
                {
                    afterThoughts = false;
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpiritMapLoss), nameof(SpiritMapLoss.AppendWarning)));
                }
                yield return instruction;
            }
        }
    }
}
