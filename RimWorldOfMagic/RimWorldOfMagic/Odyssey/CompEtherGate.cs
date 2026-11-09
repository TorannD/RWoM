using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_EtherGate : CompProperties
    {
        public CompProperties_EtherGate() { compClass = typeof(CompEtherGate); }
    }

    public sealed class EtherGateFuelPayment
    {
        private readonly Dictionary<CompRefuelable, float> debits = new Dictionary<CompRefuelable, float>();
        private static bool paying;
        public void Refund()
        {
            foreach (var pair in debits.ToList())
            {
                if (pair.Value <= 0) continue;
                float before = pair.Key.Fuel;
                try { pair.Key.Refuel(pair.Value); }
                catch (Exception ex) { Compatibility.TM_Log.Error("EtherGate", "Refund signal failed: " + ex); }
                finally { debits[pair.Key] = Math.Max(0, pair.Value - Math.Max(0, pair.Key.Fuel - before)); }
            }
        }
        public static bool TryDebit(Thing gate, float amount, out EtherGateFuelPayment payment)
        {
            payment = new EtherGateFuelPayment();
            if (paying || amount < 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            var sources = CoreReturnUtility.FuelSources(gate).Distinct().ToList();
            if (sources.Sum(f => MagofuelReserve.Available(f)) < amount) return false;
            var beforePayment = new Dictionary<CompRefuelable, float>();
            paying = true;
            try
            {
                float remaining = amount;
                foreach (var source in sources)
                {
                    if (remaining <= 0) break;
                    if (!CoreReturnUtility.FuelSources(gate).Contains(source)) throw new InvalidOperationException("Fuel network changed.");
                    float debit = Math.Min(MagofuelReserve.Available(source), remaining), before = source.Fuel;
                    if (debit <= 0f) continue;
                    beforePayment[source] = before;
                    try { source.ConsumeFuel(debit); }
                    finally { payment.debits[source] = Math.Min(debit, Math.Max(0, before - source.Fuel)); }
                    if (Math.Abs(payment.debits[source] - debit) > .0001f || !MagofuelReserve.AllowsDebit(source, before, debit)) throw new InvalidOperationException("Incomplete or protected fuel debit.");
                    remaining -= debit;
                }
                if (remaining > .0001f || !gate.Spawned || gate.Destroyed || gate.Faction != Faction.OfPlayer
                    || sources.Any(s => !CoreReturnUtility.FuelSources(gate).Contains(s))
                    || payment.debits.Any(p => !MagofuelReserve.AllowsDebit(p.Key, beforePayment[p.Key], p.Value))) throw new InvalidOperationException("Fuel network or reserve changed.");
                return true;
            }
            catch (Exception ex)
            {
                payment.Refund();
                Compatibility.TM_Log.Warning("EtherGate", "Payment cancelled: " + ex.Message);
                return false;
            }
            finally { paying = false; }
        }
    }

    public class CompEtherGate : ThingComp
    {
        public const float FuelCost = 100f;
        public const int DurationTicks = 60000;
        private int summonAnimationStartTick = -1;
        private static bool summoning;
        public int SummonAnimationStartTick => summonAnimationStartTick;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref summonAnimationStartTick, "TM_etherSummonAnimationStartTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (summonAnimationStartTick > Find.TickManager.TicksGame
                || (long)Find.TickManager.TicksGame - summonAnimationStartTick >= 360)) summonAnimationStartTick = -1;
        }
        public bool CanOperate => parent != null && parent.Spawned && !parent.Destroyed && parent.Faction == Faction.OfPlayer
            && parent.GetComp<CompFlickable>()?.SwitchIsOn != false && parent.GetComp<CompBreakdownable>()?.BrokenDown != true;
        private static bool Fail(string key, out string reason) { reason = key.Translate(); return false; }
        public bool TrySummon(out string reason)
        {
            reason = null;
            if (summoning) return Fail("TM_EtherGateBusy", out reason);
            if (!CanOperate) return Fail("TM_EtherGateUnavailable", out reason);
            if (CoreReturnUtility.AvailableFuel(parent) < FuelCost) return Fail("TM_EtherGateNoFuel", out reason);
            summoning = true;
            Pawn_EtherServant pawn = null;
            EtherGateFuelPayment payment = null;
            bool committed = false;
            try
            {
                var kind = DefDatabase<PawnKindDef>.GetNamed("TM_EtherServantKind");
                pawn = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer) as Pawn_EtherServant;
                if (pawn == null) return Fail("TM_EtherGateFailed", out reason);
                var map = parent.Map;
                var landing = GenRadial.RadialCellsAround(parent.Position, 4f, true)
                    .FirstOrDefault(c => CoreReturnUtility.SafeLanding(pawn, map, c), IntVec3.Invalid);
                if (!landing.IsValid) return Fail("TM_EtherGateNoLanding", out reason);
                if (!EtherGateFuelPayment.TryDebit(parent, FuelCost, out payment)) return Fail("TM_EtherGateNoFuel", out reason);
                if (!CanOperate || parent.Map != map || !CoreReturnUtility.SafeLanding(pawn, map, landing))
                    return Fail("TM_EtherGateNoLanding", out reason);
                pawn.StartLifetime(Find.TickManager.TicksGame);
                GenSpawn.Spawn(pawn, landing, map);
                if (!pawn.Spawned || pawn.Destroyed || pawn.Map != map || pawn.Position != landing)
                    return Fail("TM_EtherGateFailed", out reason);
                committed = true;
                summonAnimationStartTick = Find.TickManager.TicksGame;
                return true;
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Error("EtherGate", "Summoning failed: " + ex);
                return Fail("TM_EtherGateFailed", out reason);
            }
            finally
            {
                if (!committed)
                {
                    try
                    {
                        if (pawn != null && !pawn.Destroyed)
                        {
                            pawn.TryEnd(EtherServantEndReason.Cancelled);
                            if (!pawn.Spawned && !pawn.Destroyed) pawn.CancelUnspawned();
                            if (!pawn.Spawned && !pawn.Destroyed && pawn.ParentHolder == null) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                        }
                    }
                    catch (Exception ex) { Compatibility.TM_Log.Error("EtherGate", "Cancelled pawn recovery failed: " + ex); }
                    payment?.Refund();
                }
                summoning = false;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            var command = new Command_Action
            {
                defaultLabel = "TM_EtherGateSummon".Translate(),
                defaultDesc = "TM_EtherGateSummonDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", false),
                action = () => { if (!TrySummon(out var reason)) Messages.Message(reason, parent, MessageTypeDefOf.RejectInput, false); }
            };
            if (!CanOperate) command.Disable("TM_EtherGateUnavailable".Translate());
            else if (CoreReturnUtility.AvailableFuel(parent) < FuelCost) command.Disable("TM_EtherGateNoFuel".Translate());
            yield return command;
        }
        public override void PostDraw() { base.PostDraw(); TorannMagic.Visuals.EtherSummonAnimation.Draw(parent, summonAnimationStartTick); }
    }
}


