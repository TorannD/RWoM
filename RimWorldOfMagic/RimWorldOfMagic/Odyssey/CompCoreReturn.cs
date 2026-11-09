using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class CompProperties_CoreReturn : CompProperties
    {
        public CompProperties_CoreReturn() { compClass = typeof(CompCoreReturn); }
    }

    public class CompCoreReturn : ThingComp
    {
        public const float ReturnFuelPerPawn = 10f;
        private List<Pawn> boundPawns = new List<Pawn>();
        private double paidFuelCredit;
        private long receiptEnd = -1;
        private bool maintenancePaid = true;
        private bool paying;
        private int nextPaymentTick;
        public double PaidFuelCredit => paidFuelCredit;
        public bool MaintenancePaid => maintenancePaid;
        public IEnumerable<Pawn> BoundPawns => boundPawns.Where(IsBound);
        public static Hediff_CoreReturnMark FindMark(Pawn pawn) => pawn?.health?.hediffSet?.hediffs.OfType<Hediff_CoreReturnMark>().FirstOrDefault();
        private static bool PlayerCustody(Pawn pawn) => pawn != null && pawn.Faction == Faction.OfPlayer && !pawn.IsPrisoner
            && (pawn.HostFaction == null || pawn.HostFaction == Faction.OfPlayer);
        public bool IsBound(Pawn pawn) => PlayerCustody(pawn) && !pawn.Dead && FindMark(pawn)?.Core == parent;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref boundPawns, "TM_coreReturnPawns", LookMode.Reference);
            Scribe_Values.Look(ref paidFuelCredit, "TM_coreReturnCredit", 0d);
            Scribe_Values.Look(ref receiptEnd, "TM_coreReturnReceiptEnd", -1L);
            Scribe_Values.Look(ref nextPaymentTick, "TM_coreReturnNextPaymentTick", 0);
            Scribe_Values.Look(ref maintenancePaid, "TM_coreReturnMaintenancePaid", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (boundPawns == null) boundPawns = new List<Pawn>();
                boundPawns = boundPawns.Where(p => p != null).Distinct().ToList();
                if (paidFuelCredit < 0 || double.IsNaN(paidFuelCredit) || double.IsInfinity(paidFuelCredit)) paidFuelCredit = 0;
            }
        }
        public void Register(Pawn pawn) { if (pawn != null && !boundPawns.Contains(pawn)) boundPawns.Add(pawn); }
        public void Unregister(Pawn pawn) => boundPawns.Remove(pawn);
        private bool Operational => parent != null && parent.Spawned && !parent.Destroyed && parent.Faction == Faction.OfPlayer
            && parent.GetComp<CompFlickable>()?.SwitchIsOn != false && parent.GetComp<CompBreakdownable>()?.BrokenDown != true;
        public bool CanOperate(out string reason)
        {
            reason = !Operational ? "TM_CoreReturnUnavailable".Translate().ToString()
                : !maintenancePaid ? "TM_CoreReturnUnpaid".Translate().ToString() : null;
            return reason == null;
        }
        public override void CompTick() => TickMaintenance(Find.TickManager.TicksGame);
        public override void CompTickInterval(int delta) => TickMaintenance(Find.TickManager.TicksGame, delta);
        public void TickMaintenance(int now, int delta = 1)
        {
            if (paying || now < 0 || delta <= 0 || now < receiptEnd || !Operational) return;
            if (!maintenancePaid && now < nextPaymentTick) return;
            receiptEnd = (long)now + delta;
            if (now % 250 == 0) boundPawns.RemoveAll(p => p == null || p.Destroyed || FindMark(p)?.Core != parent);
            int count = BoundPawns.Count();
            if (count == 0) { maintenancePaid = true; return; }
            double demand = count * (double)delta / 60000d;
            paying = true;
            maintenancePaid = false;
            nextPaymentTick = now > int.MaxValue - 60 ? int.MaxValue : now + 60;
            try
            {
                double deficit = demand - paidFuelCredit;
                if (deficit > 1e-10)
                {
                    float debit = (float)Math.Ceiling(deficit - 1e-10);
                    if (CoreReturnUtility.TryConsumeFuel(parent, debit)) paidFuelCredit += debit;
                }
                if (!Operational || paidFuelCredit + 1e-10 < demand) return;
                paidFuelCredit = Math.Max(0, paidFuelCredit - demand);
                maintenancePaid = true;
            }
            finally { paying = false; }
        }
        public bool CanMark(Pawn pawn, bool requirePosition = false) => Operational && pawn != null && pawn.Spawned && pawn.Map == parent.Map
            && PlayerCustody(pawn) && pawn.IsColonistPlayerControlled && !pawn.Dead && !pawn.Downed && !pawn.InMentalState
            && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)
            && (!requirePosition || pawn.Position.AdjacentTo8WayOrInside(parent));

        // Player-facing reason a colonist cannot be marked right now, or null.
        // Sleeping is deliberately not a blocker: the ordered mark job wakes the
        // pawn (a night-time "no candidates" dead end was a player report).
        public string MarkBlockReason(Pawn pawn)
        {
            if (!Operational) return "TM_CoreReturnUnavailable".Translate().ToString();
            if (pawn.Downed) return "TM_CoreReturnBlockDowned".Translate().ToString();
            if (pawn.InMentalState) return "TM_CoreReturnBlockMental".Translate().ToString();
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) return "TM_CoreReturnBlockCapacity".Translate().ToString();
            return null;
        }
        public bool Bind(Pawn pawn)
        {
            if (!CanMark(pawn, true) || FindMark(pawn) != null) return false;
            var mark = (Hediff_CoreReturnMark)HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("TM_CoreReturnMark"), pawn);
            mark.Core = parent;
            pawn.health.AddHediff(mark);
            Register(pawn);
            return true;
        }
        public void Unbind(Pawn pawn)
        {
            var mark = FindMark(pawn);
            if (mark?.Core == parent) pawn.health.RemoveHediff(mark);
            Unregister(pawn);
        }
        internal bool CanOrderMark(Pawn pawn, Hediff_CoreReturnMark expectedMark, Thing expectedCore)
            => CanMark(pawn) && FindMark(pawn) == expectedMark && expectedMark?.Core == expectedCore;

        private void ChooseMark()
        {
            if (!Operational) return;
            var options = new List<FloatMenuOption>();
            foreach (Pawn candidate in parent.Map.mapPawns.FreeColonistsSpawned)
            {
                Pawn pawn = candidate;
                var old = FindMark(pawn);
                if (old?.Core == parent) continue;
                var previousCore = old?.Core;
                Action order = () =>
                {
                    // Sleep is not a blocker; anything still blocking shows as a reason.
                    if (!CanOrderMark(pawn, old, previousCore)) return;
                    // Rebinding is explicit and only removes the old mark upon arrival.
                    var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_ReceiveCoreReturnMark"), parent);
                    job.targetB = previousCore;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                };
                string blocked = CanMark(pawn) ? null : MarkBlockReason(pawn);
                if (blocked != null)
                    options.Add(new FloatMenuOption(pawn.LabelShortCap + " — " + blocked, null));
                else
                    options.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                    {
                        if (old == null) order();
                        else Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TM_CoreReturnRebindConfirm".Translate(pawn.LabelShortCap), order));
                    }));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("TM_CoreReturnNoCandidates".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }
        public void RequestReturn(IReadOnlyList<Pawn> requested)
        {
            var snapshot = requested.Distinct().ToList();
            var caravanRosters = snapshot.Select(p => p.GetCaravan()).Where(c => c != null).Distinct()
                .ToDictionary(c => c, c => new HashSet<Pawn>(c.PawnsListForReading));
            if (!CoreReturnUtility.TryPreviewRoster(this, snapshot, out var moving, out string reason))
            { Messages.Message(reason, MessageTypeDefOf.RejectInput, false); return; }
            int count = moving.Count;
            var destinationMap = parent.Map;
            bool stranded = snapshot.Any(p => p.GetCaravan() is Caravan caravan && caravan.PawnsListForReading.Any(q => !snapshot.Contains(q) && (q.RaceProps.Animal || q.IsPrisoner))
                && !caravan.PawnsListForReading.Any(q => !snapshot.Contains(q) && caravan.IsOwner(q) && !q.Downed));
            string destination = parent.LabelCap + " — " + destinationMap.Parent.LabelCap;
            string prompt = "TM_CoreReturnConfirm".Translate(count, count * ReturnFuelPerPawn, destination)
                + "\n\n" + string.Join("\n", moving.Select(p => p.LabelShortCap.ToString()));
            if (stranded) prompt += "\n\n" + "TM_CoreReturnStranding".Translate();
            bool consumed = false;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(prompt, () =>
            {
                if (consumed) return;
                consumed = true;
                if (parent.Map != destinationMap || caravanRosters.Any(pair => pair.Key.Destroyed || !pair.Value.SetEquals(pair.Key.PawnsListForReading)))
                { Messages.Message("TM_CoreReturnChanged".Translate(), MessageTypeDefOf.RejectInput, false); return; }
                if (!CoreReturnUtility.TryPreviewRoster(this, snapshot, out var currentMoving, out string currentReason) || !currentMoving.SequenceEqual(moving))
                { Messages.Message(currentReason ?? "TM_CoreReturnChanged".Translate().ToString(), MessageTypeDefOf.RejectInput, false); return; }
                if (!CoreReturnUtility.TryReturn(this, snapshot, out string failure)) Messages.Message(failure, MessageTypeDefOf.RejectInput, false);
            }));
        }
        private void ReturnAll()
        {
            var marked = BoundPawns.ToList();
            var caravans = marked.Select(p => p.GetCaravan()).Where(c => c != null).Distinct().ToList();
            if (caravans.Count == 0) { RequestReturn(marked); return; }
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("TM_CoreReturnMarkedOnly".Translate().ToString(), () => RequestReturn(marked)),
                new FloatMenuOption("TM_CoreReturnIncludeCaravans".Translate().ToString(), () => RequestReturn(marked.Concat(caravans.SelectMany(c => c.PawnsListForReading)).Distinct().ToList()))
            }));
        }
        public override string CompInspectStringExtra() => "TM_CoreReturnStatus".Translate(BoundPawns.Count(), BoundPawns.Count(), CoreReturnUtility.AvailableFuel(parent).ToString("0.##")).ToString()
            + (maintenancePaid ? "" : "\n" + "TM_CoreReturnUnpaid".Translate().ToString());
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            yield return new Command_Action { defaultLabel = "TM_CoreReturnMark".Translate(), defaultDesc = "TM_CoreReturnMarkDesc".Translate(), icon = ContentFinder<Texture2D>.Get("UI/transfermana"), action = ChooseMark };
            var recall = new Command_Action { defaultLabel = "TM_CoreReturnAll".Translate(), defaultDesc = "TM_CoreReturnAllDesc".Translate(), icon = ContentFinder<Texture2D>.Get("UI/transfermana"), action = ReturnAll };
            if (!CanOperate(out string reason)) recall.Disable(reason);
            yield return recall;
        }
    }
}
