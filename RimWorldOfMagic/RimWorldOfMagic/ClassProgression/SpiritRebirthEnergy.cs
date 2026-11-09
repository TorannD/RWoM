using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Marked original only. Legacy race/possession energy never enters this service.
    internal static class SpiritRebirthEnergy
    {
        private static readonly HashSet<Pawn> collapsing = new HashSet<Pawn>();
        internal static bool Marked(Pawn pawn) => ClassIdentity.Component(pawn)?.SpiritRebirth != null;
        internal static Need_Spirit Need(Pawn pawn) => pawn?.needs?.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
        internal static bool CanCast(Pawn pawn) => SpiritRebirthFormPolicy.IsEthereal(pawn)
            && SpiritRebirthState.TryGet(pawn, out var state) && !state.DepletionPending
            && Need(pawn) != null && Need(pawn).CurLevel > 0f;

        internal static void Change(Pawn pawn, Need_Spirit need, float amount, bool deferCollapse = false)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || !ReferenceEquals(Need(pawn), need)
                || !SpiritRebirthFormPolicy.IsEthereal(pawn) || !SpiritRebirthState.TryGet(pawn, out var state)) return;
            if (!state.DepletionPending) need.CurLevel += amount;
            if (need.CurLevel <= 0f && state.BeginDepletion(pawn) && !deferCollapse) Retry(pawn);
        }

        internal static bool AbsorbDamage(Pawn pawn, float amount)
        {
            if (!SpiritRebirthFormPolicy.IsSpawnedEthereal(pawn)) return false;
            var need = Need(pawn);
            if (need == null) return false; // No free immunity from an incomplete energy owner.
            need.SpendSpirit(Math.Max(0f, amount) * .25f);
            return true;
        }

        internal static void Retry(Pawn pawn)
        {
            if (!SpiritRebirthState.TryGet(pawn, out var state) || !state.DepletionPending
                || Need(pawn)?.CurLevel > 0f || Find.TickManager.TicksGame < state.DepletionRetryAt || !collapsing.Add(pawn)) return;
            Thing_SoulStone candidate = state.DepletionStone;
            try
            {
                if (candidate != null && ReferenceEquals(candidate.ContainedSpirit, pawn))
                {
                    state.TryEnterStone(pawn, candidate, null, false);
                    return;
                }
                if (!SpiritRebirthFormPolicy.IsSpawnedEthereal(pawn))
                {
                    if (pawn.Spawned && state.TakeDepletionWarning())
                        Compatibility.TM_Log.Warning("SpiritEnergy", "Pending original has incomplete native map custody; capture deferred.");
                    return;
                }
                var map = pawn.Map;
                if (candidate != null && candidate.ContainedSpirit == null)
                {
                    // An empty shell is an ordinary movable/destroyable item.
                    // Abandon a stale reference without deleting somebody's item.
                    bool moved = !ReferenceEquals(candidate.holdingOwner, map.spawnedThings) && candidate.holdingOwner?.Owner != null
                        && candidate.holdingOwner.Contains(candidate);
                    if (candidate.Destroyed || moved)
                    {
                        state.RememberDepletionStone(null);
                        candidate = null;
                    }
                }
                if (candidate == null)
                {
                    var def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_SoulStone");
                    if (def?.thingClass != typeof(Thing_SoulStone)) return;
                    candidate = ThingMaker.MakeThing(def) as Thing_SoulStone;
                    state.RememberDepletionStone(candidate);
                    // Only an EMPTY shell is placed. The Pawn keeps native map
                    // custody until the shell itself has a real saved map owner.
                    GenPlace.TryPlaceThing(candidate, pawn.Position, map, ThingPlaceMode.Near, out var _);
                }
                if (!MapOwns(candidate, map) || candidate.ContainedSpirit != null)
                {
                    if (candidate?.Spawned == true && state.TakeDepletionWarning())
                        Compatibility.TM_Log.Warning("SpiritEnergy", "Pending shell has incomplete native map custody; original retained for retry.");
                    return;
                }
                candidate.TryCapture(pawn, null, false);
            }
            catch (Exception error)
            {
                // Native capture can throw after transfer. Commit the same held
                // original; never allocate a replacement Pawn or second shell.
                if (candidate != null && ReferenceEquals(candidate.ContainedSpirit, pawn))
                    state.TryEnterStone(pawn, candidate, null, false);
                if (state.TakeDepletionWarning()) Compatibility.TM_Log.Warning("SpiritEnergy", "Depletion interrupted; original custody retained for retry: " + error.Message);
            }
            finally
            {
                try
                {
                    if (state.DepletionPending && candidate != null && candidate.ContainedSpirit == null
                        && !candidate.Spawned && candidate.holdingOwner == null)
                    {
                        if (!candidate.Destroyed) candidate.Destroy(DestroyMode.Vanish);
                        state.RememberDepletionStone(null);
                    }
                    if (state.DepletionPending) state.DeferDepletionRetry(Find.TickManager.TicksGame);
                }
                finally { collapsing.Remove(pawn); }
            }
        }
        private static bool MapOwns(Thing thing, Map map) => thing != null && thing.Spawned && thing.Map == map
            && ReferenceEquals(thing.holdingOwner, map?.spawnedThings)
            && ReferenceEquals(thing.holdingOwner?.Owner, map) && map.spawnedThings.Contains(thing);
    }
}
