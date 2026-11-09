using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(Caravan), nameof(Caravan.Notify_MemberDied))]
    internal static class SpiritCaravanHostLoss
    {
        // Run inside the native death callback at the actual point of removal.
        // A competing mod can veto the callback; entering a Prefix alone is not authorization.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var remove = AccessTools.Method(typeof(Caravan), nameof(Caravan.RemovePawn));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(remove))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpiritCaravanHostLoss), nameof(BeforeRemoval)));
                }
                yield return instruction;
            }
        }

        internal static void BeforeRemoval(Caravan caravan, Pawn member)
        {
            if (!caravan.Spawned || member == null || !member.Dead
                || member.Spawned || !caravan.pawns.Contains(member)) return;
            var original = SpiritHostLoss.IsMarkedHost(member) ? Compatibility.TM_SpiritCommands.Possession(member).SpiritPawn : null;
            if (!SpiritRebirthState.TryGetStored(original, out var state) || !state.CanEnterStone(original, member, true)) original = null;

            bool hasOwner = false;
            foreach (var other in caravan.PawnsListForReading)
            {
                if (other == member || !caravan.IsOwner(other)) continue;
                hasOwner = true;
                if (original == null || TrySecure(original, member, other.inventory?.innerContainer)) return;
            }
            if (hasOwner)
            {
                Compatibility.TM_Log.Warning("SpiritCaravan", "Could not secure the Spirit of " + original.ThingID + " in the surviving caravan.");
                return;
            }

            if (original != null) Retire(original);
            foreach (var stone in CaravanInventoryUtility.AllInventoryItems(caravan).OfType<Thing_SoulStone>().ToList())
            {
                var soul = stone.ContainedSpirit;
                if (!SpiritRebirthState.TryGetStored(soul, out var saved) || !saved.HasValidStone(soul)) continue;
                // Re-check the item's actual owner: an abandonment callback can transfer it.
                for (var holder = stone.ParentHolder; holder != null; holder = holder.ParentHolder)
                    if (ReferenceEquals(holder, caravan)) { Retire(soul); break; }
            }
        }

        private static void Retire(Pawn soul)
        {
            var result = SpiritRebirthTerminalLoss.TryDiscardOnTerminalLoss(soul, soul.holdingOwner);
            if (result != SpiritTerminalLossResult.Completed)
                Compatibility.TM_Log.Warning("SpiritCaravan", "Could not retire the Spirit of " + soul.ThingID + " with the lost caravan: " + result);
        }

        internal static bool TrySecure(Pawn original, Pawn host, ThingOwner destination)
        {
            if (destination == null || !SpiritRebirthState.TryGetStored(original, out var state)
                || !state.CanEnterStone(original, host, true)) return false;
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_SoulStone");
            if (def?.thingClass != typeof(Thing_SoulStone)) return false;
            var stone = (Thing_SoulStone)ThingMaker.MakeThing(def);
            try
            {
                destination.TryAdd(stone, false);
                return ReferenceEquals(stone.holdingOwner, destination) && stone.TryCapture(original, host, true);
            }
            finally
            {
                if (stone.ContainedSpirit == null && !stone.Destroyed) stone.Destroy();
            }
        }
    }
}
