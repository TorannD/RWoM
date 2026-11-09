using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace TorannMagic.Compatibility
{
    public static class TM_SpiritCommands
    {
        private sealed class LeaveBodyCommand : Command_Action
        {
            public Pawn Host;
        }

        public static HediffComp_SpiritPossession Possession(Pawn pawn)
        {
            // Match the exact hediff consumed by RemovePossession, not a trait or
            // a class-data marker that may survive after possession has ended.
            Hediff hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
            HediffComp_SpiritPossession possession = hediff?.TryGetComp<HediffComp_SpiritPossession>();
            Pawn spirit = possession?.SpiritPawn;
            return spirit != null && !spirit.Destroyed && spirit != pawn && possession.parent?.pawn == pawn
                && possession.GetDirectlyHeldThings()?.Contains(spirit) == true ? possession : null;
        }

        public static bool CanLeaveBody(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Destroyed || Find.Selector?.NumSelected != 1 ||
                Find.Selector.SingleSelectedThing != pawn || Possession(pawn) is not HediffComp_SpiritPossession link) return false;
            var original = link.SpiritPawn;
            if (ClassProgression.SpiritRebirthState.IsPaused(original)) return false;
            if (ClassProgression.SpiritRebirthState.TryGet(original, out var state))
                return original.Faction?.IsPlayer == true && state.CanVoluntarilyLeaveHost(original);
            return pawn.Faction?.IsPlayer == true && !ClassProgression.SpiritHostLoss.IsMarkedHost(pawn);
        }

        public static void LeaveBody(Pawn pawn)
        {
            // Gizmos can outlive a selection or possession change. Never release
            // an unrelated/missing spirit or assume an animal has a drafter.
            if (!CanLeaveBody(pawn)) return;
            var link = Possession(pawn);
            var original = link?.SpiritPawn;
            if (ClassProgression.SpiritRebirthState.TryGet(original, out var state))
            {
                LeaveRitualHost(pawn, original, link, state);
                return;
            }
            if (pawn.drafter != null) pawn.drafter.Drafted = false;
            TM_Action.RemovePossession(pawn, pawn.Position, false);
        }

        private static void LeaveRitualHost(Pawn host, Pawn original, HediffComp_SpiritPossession link, ClassProgression.SpiritRebirthState state)
        {
            var map = host.Map;
            var holder = link.GetDirectlyHeldThings();
            foreach (var cell in GenRadial.RadialCellsAround(host.Position, 3.9f, true))
            {
                var footprint = GenAdj.OccupiedRect(cell, original.Rotation, original.def.Size);
                if (!footprint.InBounds(map) || footprint.Any(c => !c.Standable(map) || c.GetThingList(map).Count != 0)) continue;
                bool released = false;
                try
                {
                    GenSpawn.Spawn(original,cell,map,original.Rotation,WipeMode.Vanish);
                    released = state.TryLeaveHost(original,host);
                }
                finally
                {
                    if (!released)
                    {
                        if (original.Spawned && original.Map == map) original.DeSpawn(DestroyMode.WillReplace);
                        if (!original.Spawned && original.holdingOwner == null && !holder.TryAdd(original,false))
                            throw new System.InvalidOperationException("Could not return the spirit to its host.");
                    }
                }
                if (released)
                {
                    host.health.RemoveHediff(link.parent);
                    ClassProgression.SpiritRebirthHealthForm.TryRefreshPending(original);
                }
                return;
            }
            Messages.Message("TM_SoulStoneNoExitCell".Translate(), MessageTypeDefOf.RejectInput, false);
        }

        public static IEnumerable<Gizmo> AppendLeaveBody(IEnumerable<Gizmo> gizmos, Pawn pawn)
        {
            if (gizmos == null) return gizmos;
            int remaining = 0;
            if (!CanLeaveBody(pawn))
            {
                if (pawn?.Spawned != true || Find.Selector?.SingleSelectedThing != pawn ||
                    !ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(pawn,out var original) ||
                    original.Faction?.IsPlayer != true || !ClassProgression.SpiritRebirthState.TryGet(original,out var state) ||
                    !state.RecoveryRequired) return gizmos;
                remaining = ClassProgression.SpiritRebirthState.RecoveryDurationTicks - state.RecoveryTicks;
                if (remaining <= 0) return gizmos;
            }
            List<Gizmo> result = gizmos.ToList();
            if (result.OfType<LeaveBodyCommand>().Any(command => command.Host == pawn)) return result;
            var command = new LeaveBodyCommand
            {
                Host = pawn,
                action = () => LeaveBody(pawn),
                Order = 49,
                defaultLabel = TM_TextPool.TM_RemovePossess,
                defaultDesc = TM_TextPool.TM_RemovePossessDesc,
                icon = ContentFinder<Texture2D>.Get("UI/remove_spiritpossession", true)
            };
            if (remaining > 0) command.Disable("TM_SoulStoneRecoveryRemaining".Translate(remaining.ToStringTicksToPeriod()));
            result.Add(command);
            return result;
        }
    }
}
