using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.Odyssey
{
    // A request is an exact roster. Preview and execution independently validate live ownership.
    public static class CoreReturnUtility
    {
        public const float LandingRadius = 12f;
        public const float AlreadyNearRadius = 3f;
        private static bool transferring, paying;

        public static List<CompRefuelable> FuelSources(Thing core)
        {
            var result = new List<CompRefuelable>();
            if (core == null || !core.Spawned || core.Destroyed || core.Faction != Faction.OfPlayer) return result;
            var local = core.TryGetComp<CompRefuelable>();
            if (MapComponent_ManaNetwork.CompatibleFuel(local)) result.Add(local);
            var endpoint = core.TryGetComp<CompManaNetworkEndpoint>();
            var network = core.Map.GetComponent<MapComponent_ManaNetwork>();
            if (endpoint == null || endpoint.FuelMode == ManaNetworkMode.Off || network == null) return result;
            result.AddRange(network.CoreReturnFuelSources(endpoint));
            return result;
        }

        public static float AvailableFuel(Thing core) => FuelSources(core).Sum(f => MagofuelReserve.Available(f));

        private sealed class Payment
        {
            internal readonly Dictionary<CompRefuelable, float> Debits = new Dictionary<CompRefuelable, float>();
            internal void Refund()
            {
                foreach (var pair in Debits.ToList())
                {
                    if (pair.Value <= 0f) continue;
                    float before = pair.Key.Fuel;
                    try { pair.Key.Refuel(pair.Value); }
                    catch (Exception ex) { Compatibility.TM_Log.Error("CoreReturn", "Core return refund signal failed: " + ex); }
                    finally { Debits[pair.Key] = Math.Max(0f, pair.Value - Math.Max(0f, pair.Key.Fuel - before)); }
                }
            }
        }

        private static bool Debit(Thing core, float amount, out Payment payment)
        {
            payment = new Payment();
            if (paying || float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            if (amount == 0f) return true;
            var sources = FuelSources(core);
            if (sources.Sum(f => MagofuelReserve.Available(f)) < amount) return false;
            var beforePayment = new Dictionary<CompRefuelable, float>();
            paying = true;
            try
            {
                float remaining = amount;
                foreach (var source in sources)
                {
                    if (remaining <= 0f) break;
                    if (!FuelSources(core).Contains(source)) throw new InvalidOperationException("Fuel network changed.");
                    float debit = Math.Min(MagofuelReserve.Available(source), remaining), before = source.Fuel;
                    if (debit <= 0f) continue;
                    beforePayment[source] = before;
                    try { source.ConsumeFuel(debit); }
                    finally { payment.Debits[source] = Math.Min(debit, Math.Max(0f, before - source.Fuel)); }
                    if (Math.Abs(payment.Debits[source] - debit) > .0001f || !MagofuelReserve.AllowsDebit(source, before, debit)) throw new InvalidOperationException("Incomplete or protected fuel debit.");
                    remaining -= debit;
                }
                if (remaining > .0001f || !core.Spawned || core.Destroyed || core.Faction != Faction.OfPlayer
                    || sources.Any(s => !FuelSources(core).Contains(s))
                    || payment.Debits.Any(p => !MagofuelReserve.AllowsDebit(p.Key, beforePayment[p.Key], p.Value))) throw new InvalidOperationException("Fuel network or reserve changed.");
                return true;
            }
            catch (Exception ex)
            {
                payment.Refund();
                Compatibility.TM_Log.Warning("CoreReturn", "Core return payment cancelled: " + ex.Message);
                return false;
            }
            finally { paying = false; }
        }

        public static bool TryConsumeFuel(Thing core, float amount) => !transferring && Debit(core, amount, out _);

        private sealed class Move
        {
            internal Pawn Pawn;
            internal Map SourceMap;
            internal IntVec3 SourceCell, Landing;
            internal Rot4 Rotation;
            internal Caravan Caravan;
            internal Thing Cargo;
            internal bool Started;
        }

        public static bool TryPreview(CompCoreReturn core, IReadOnlyList<Pawn> requested, out int count, out string reason)
        {
            bool valid = Prepare(core, requested, out var moves, out reason);
            count = moves.Count;
            return valid;
        }

        public static bool TryPreviewRoster(CompCoreReturn core, IReadOnlyList<Pawn> requested, out List<Pawn> moving, out string reason)
        {
            bool valid = Prepare(core, requested, out var moves, out reason);
            moving = moves.Select(m => m.Pawn).ToList();
            return valid;
        }

        private static bool Fail(string key, out string reason) { reason = key.Translate(); return false; }

        private static bool Prepare(CompCoreReturn core, IReadOnlyList<Pawn> requested, out List<Move> moves, out string reason, bool checkFuel = true)
        {
            moves = new List<Move>();
            reason = null;
            if (core == null || !core.CanOperate(out reason)) return false;
            if (requested == null || requested.Count == 0 || requested.Any(p => p == null) || requested.Distinct().Count() != requested.Count)
                return Fail("TM_CoreReturn_InvalidSelection", out reason);
            var roster = new HashSet<Pawn>(requested);
            foreach (var pawn in requested)
            {
                if (pawn.Dead || pawn.Destroyed) return Fail("TM_CoreReturn_InvalidSelection", out reason);
                var caravan = pawn.GetCaravan();
                if (caravan != null)
                {
                    if (caravan.Shuttle != null) return Fail("TM_CoreReturn_UnsupportedTransport", out reason);
                    if (!caravan.Spawned || caravan.Destroyed || caravan.Faction != Faction.OfPlayer || pawn.ParentHolder != caravan
                        || !caravan.PawnsListForReading.Any(p => roster.Contains(p) && core.IsBound(p)))
                        return Fail("TM_CoreReturn_InvalidSelection", out reason);
                    if (!core.IsBound(pawn) && !caravan.PawnsListForReading.All(roster.Contains))
                        return Fail("TM_CoreReturn_InvalidSelection", out reason);
                }
                else if (!pawn.Spawned || pawn.ParentHolder != null || !core.IsBound(pawn))
                    return Fail("TM_CoreReturn_InvalidSelection", out reason);
                // A carried pawn is not free cargo and may be in a transit/guest state.
                if (pawn.carryTracker?.CarriedThing is Pawn) return Fail("TM_CoreReturn_CarriedPawn", out reason);
                if (pawn.Spawned && pawn.Map == core.parent.Map && pawn.Position.DistanceToSquared(core.parent.Position) <= AlreadyNearRadius * AlreadyNearRadius) continue;
                moves.Add(new Move { Pawn = pawn, SourceMap = pawn.Map, SourceCell = pawn.Position, Rotation = pawn.Rotation,
                    Caravan = caravan, Cargo = pawn.carryTracker?.CarriedThing });
            }
            if (moves.Count == 0) return Fail("TM_CoreReturn_AlreadyNear", out reason);
            var reserved = new HashSet<IntVec3>();
            foreach (var move in moves)
            {
                bool found = false;
                foreach (var cell in GenRadial.RadialCellsAround(core.parent.Position, LandingRadius, true))
                {
                    if (!SafeLanding(move.Pawn, core.parent.Map, cell) || !reserved.Add(cell)) continue;
                    move.Landing = cell; found = true; break;
                }
                if (!found) return Fail("TM_CoreReturn_NoLanding", out reason);
            }
            if (checkFuel && AvailableFuel(core.parent) < moves.Count * CompCoreReturn.ReturnFuelPerPawn) return Fail("TM_CoreReturn_NoFuel", out reason);
            return true;
        }

        public static bool SafeLanding(Pawn pawn, Map map, IntVec3 cell)
            => map != null && cell.InBounds(map) && cell.Standable(map) && !cell.GetTerrain(map).IsWater
                && cell.GetEdifice(map) == null && cell.GetFirstPawn(map) == null && !cell.ContainsStaticFire(map)
                && TravelEnvironmentSafety.IsSafe(pawn, map, cell);

        public static bool TryReturn(CompCoreReturn core, IReadOnlyList<Pawn> requested, out string reason)
        {
            if (transferring || paying) return Fail("TM_CoreReturn_Busy", out reason);
            if (!Prepare(core, requested, out var moves, out reason)) return false;
            transferring = true;
            Payment payment = null;
            bool arrived = false;
            var map = core.parent.Map;
            try
            {
                if (!Debit(core.parent, moves.Count * CompCoreReturn.ReturnFuelPerPawn, out payment))
                    return Fail("TM_CoreReturn_NoFuel", out reason);
                // Native fuel signals can mutate ownership, destination or passengers.
                if (!Prepare(core, requested, out var checkedMoves, out reason, false) || checkedMoves.Count != moves.Count
                    || core.parent.Map != map
                    || moves.Any(m => m.Pawn.Dead || m.Pawn.Destroyed || m.Pawn.Map != m.SourceMap
                        || m.Pawn.GetCaravan() != m.Caravan || m.Pawn.carryTracker?.CarriedThing != m.Cargo
                        || !checkedMoves.Any(c => c.Pawn == m.Pawn && c.Landing == m.Landing)
                        || !SafeLanding(m.Pawn, map, m.Landing)))
                    return Fail("TM_CoreReturn_InvalidSelection", out reason);
                foreach (var move in moves)
                {
                    move.Started = true;
                    // StopAll during native despawn must not drop a carried stack at the origin.
                    if (move.Cargo != null) move.Pawn.carryTracker.innerContainer.Remove(move.Cargo);
                    if (move.Pawn.Spawned) move.Pawn.DeSpawn();
                    else move.Caravan.pawns.Remove(move.Pawn);
                    GenSpawn.Spawn(move.Pawn, move.Landing, map, move.Rotation);
                    RestoreCargo(move);
                    if (!move.Pawn.Spawned || move.Pawn.Map != map || move.Pawn.Position != move.Landing)
                        throw new InvalidOperationException("Passenger did not arrive.");
                }
                arrived = true;
                // Match native caravan entry grace only after all passengers have arrived;
                // an aborted transaction must not change a prisoner's escape timer.
                if (map.IsPlayerHome)
                    foreach (var move in moves.Where(m => m.Caravan != null && m.Pawn.IsPrisoner))
                    {
                        try { move.Pawn.guest?.WaitInsteadOfEscapingForDefaultTicks(); }
                        catch (Exception grace) { Compatibility.TM_Log.Error("CoreReturn", "Core return arrived; prisoner arrival grace failed: " + grace); }
                    }
                // Caravan cargo lives inside these same pawn inventories; no stack splitting/dropping.
                foreach (var caravan in moves.Select(m => m.Caravan).Where(c => c != null).Distinct())
                    if (caravan.PawnsListForReading.Count == 0 && !caravan.Destroyed)
                    {
                        try { caravan.Destroy(); }
                        catch (Exception cleanup) { Compatibility.TM_Log.Error("CoreReturn", "Core return arrived; empty caravan cleanup failed: " + cleanup); }
                    }
                return true;
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Error("CoreReturn", "Core return transfer failed: " + ex);
                return Fail("TM_CoreReturn_Failed", out reason);
            }
            finally
            {
                if (!arrived)
                {
                    foreach (var move in moves.Where(m => m.Started).Reverse())
                    {
                        try
                        {
                            if (move.Cargo != null) move.Pawn.carryTracker.innerContainer.Remove(move.Cargo);
                            if (move.Pawn.Spawned) move.Pawn.DeSpawn();
                            if (move.SourceMap != null) GenSpawn.Spawn(move.Pawn, move.SourceCell, move.SourceMap, move.Rotation);
                            else
                            {
                                if (!move.Caravan.pawns.Contains(move.Pawn) && !move.Caravan.pawns.TryAdd(move.Pawn))
                                    throw new InvalidOperationException("Could not restore caravan member.");
                                if (!Find.WorldPawns.Contains(move.Pawn)) Find.WorldPawns.PassToWorld(move.Pawn);
                            }
                            RestoreCargo(move);
                        }
                        catch (Exception rollback)
                        {
                            // Native mod hooks can throw after mutation. Keep a durable owner even
                            // if an origin map can no longer accept its passenger.
                            Compatibility.TM_Log.Error("CoreReturn", "Core return origin restore failed: " + rollback);
                            try
                            {
                                if (!move.Pawn.Spawned && move.Pawn.ParentHolder == null && !Find.WorldPawns.Contains(move.Pawn))
                                    Find.WorldPawns.PassToWorld(move.Pawn, PawnDiscardDecideMode.KeepForever);
                                if (move.Cargo != null && move.Cargo.holdingOwner == null && !move.Cargo.Spawned)
                                {
                                    if (!move.Pawn.carryTracker.innerContainer.TryAdd(move.Cargo, false)
                                        && !move.Pawn.inventory.innerContainer.TryAdd(move.Cargo, false))
                                        Compatibility.TM_Log.Error("CoreReturn", "Core return could not recover cargo ownership for " + move.Cargo);
                                }
                            }
                            catch (Exception recovery) { Compatibility.TM_Log.Error("CoreReturn", "Core return emergency recovery failed: " + recovery); }
                        }
                    }
                    payment?.Refund();
                }
                transferring = false;
            }
        }

        private static void RestoreCargo(Move move)
        {
            if (move.Cargo != null && !move.Pawn.carryTracker.innerContainer.Contains(move.Cargo)
                && !move.Pawn.carryTracker.innerContainer.TryAdd(move.Cargo, false))
                throw new InvalidOperationException("Could not restore carried cargo.");
        }
    }
}

