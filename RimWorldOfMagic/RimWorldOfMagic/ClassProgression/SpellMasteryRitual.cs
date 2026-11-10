using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public static class SpellMasteryRitual
    {
        private static readonly HashSet<Pawn> committing = new HashSet<Pawn>();
        internal static SpellMastery.Choice Selected(Job job) => SpellMastery.Choices.FirstOrDefault(c => c.Number == job?.count);

        private static bool Valid(Pawn pawn, Thing circle, SpellMastery.Choice choice)
        {
            return pawn?.Spawned == true && pawn.IsColonistPlayerControlled && !pawn.Dead && !pawn.Downed
                && circle is Building_TMMagicCircle ring && ring.Spawned && ring.Map == pawn.Map
                && ring.Faction == pawn.Faction && !ring.IsActive && !ring.IsPending && ring.MageList.Count == 0
                && !ring.IsForbidden(pawn) && SpellMastery.Eligible(pawn, choice, out var state)
                && state.Mastery(choice.Spell) != choice.Id;
        }

        private static bool WithinCircle(Pawn pawn, Thing circle, Thing thing)
            => thing != null && !thing.Destroyed && thing.def.destroyable && ((pawn.inventory != null && thing.ParentHolder == pawn.inventory)
                || (thing.Spawned && thing.Map == pawn.Map && thing.Position.InHorDistOf(circle.Position, 5f) && !thing.IsForbidden(pawn)));

        private static bool AddStacks(Pawn pawn, Thing circle, List<ThingCount> offerings, ThingDef def, int needed)
        {
            if (def == null) return false;
            var inventory = pawn.inventory?.innerContainer.AsEnumerable() ?? Enumerable.Empty<Thing>();
            foreach (var item in inventory.Concat(circle.Map.listerThings.ThingsOfDef(def)))
            {
                if (item.def != def || !WithinCircle(pawn, circle, item)) continue;
                int n = System.Math.Min(needed, item.stackCount);
                if (n <= 0 || (item.Spawned && !pawn.CanReserve(item, 1, n))) continue;
                offerings.Add(new ThingCount(item, n));
                needed -= n;
                if (needed == 0) return true;
            }
            return false;
        }

        private static List<ThingCount> Offerings(Pawn pawn, Thing circle, SpellMastery.Choice choice)
        {
            if (!Valid(pawn, circle, choice)) return null;
            var items = new List<ThingCount>();
            if (!AddStacks(pawn, circle, items, TorannMagicDefOf.RawMagicyte, choice.Magicyte)
                || (choice.Scroll != null && !AddStacks(pawn, circle, items, DefDatabase<ThingDef>.GetNamedSilentFail(choice.Scroll), 1))) return null;
            return items;
        }

        internal static Job Order(Pawn pawn, Thing circle, SpellMastery.Choice choice)
        {
            var def = DefDatabase<JobDef>.GetNamedSilentFail("TM_SpellMasteryRitual");
            var items = Offerings(pawn, circle, choice);
            if (def == null || items == null) return null;
            var job = JobMaker.MakeJob(def, circle);
            job.count = choice.Number;
            job.targetQueueB = items.Select(item => (LocalTargetInfo)item.Thing).ToList();
            job.countQueue = items.Select(item => item.Count).ToList();
            return job;
        }

        internal static bool Validate(Pawn pawn, Job job)
        {
            var choice = Selected(job); var ring = job?.targetA.Thing;
            if (!Valid(pawn, ring, choice) || job.targetQueueB == null || job.countQueue?.Count != job.targetQueueB.Count) return false;
            int magicyte = 0, scroll = 0;
            var seen = new HashSet<Thing>();
            for (int i = 0; i < job.countQueue.Count; i++)
            {
                var item = job.targetQueueB[i].Thing; int n = job.countQueue[i];
                if (!WithinCircle(pawn, ring, item) || !seen.Add(item) || n <= 0 || n > item.stackCount) return false;
                if (item.def == TorannMagicDefOf.RawMagicyte && n <= choice.Magicyte - magicyte) magicyte += n;
                else if (choice.Scroll != null && item.def.defName == choice.Scroll && n == 1 && scroll == 0) scroll++;
                else return false;
            }
            return magicyte == choice.Magicyte && scroll == (choice.Scroll == null ? 0 : 1);
        }

        private static bool Reserved(Pawn pawn, Job job, LocalTargetInfo target, int count)
            => pawn.Map.reservationManager.ReservationsReadOnly.Any(r => r.Claimant == pawn && r.Job == job
                && r.Target == target && r.Layer == null && (r.StackCount == -1 || r.StackCount >= count));

        internal static bool Complete(Pawn pawn, Job job)
        {
            if (pawn == null || !committing.Add(pawn)) return false;
            var pieces = new List<Thing>();
            bool committed = false;
            try
            {
                if (!Validate(pawn, job) || pawn.CurJob != job || pawn.Position != job.targetA.Thing.InteractionCell
                    || !Reserved(pawn, job, job.targetA, 1)) return false;
                var choice = Selected(job);
                for (int i = 0; i < job.countQueue.Count; i++)
                {
                    var item = job.targetQueueB[i].Thing;
                    if (!WithinCircle(pawn, job.targetA.Thing, item) || item.stackCount < job.countQueue[i]
                        || (item.Spawned && !Reserved(pawn, job, item, job.countQueue[i]))) return false;
                    pieces.Add(item.SplitOff(job.countQueue[i]));
                }
                if (pieces.Any(t => t == null || t.Spawned || t.Destroyed || t.holdingOwner != null)
                    || !Valid(pawn, job.targetA.Thing, choice) || pawn.CurJob != job) return false;
                ClassIdentity.Component(pawn).State.SetMastery(choice.Spell, choice.Id);
                committed = true;
                return true;
            }
            finally
            {
                try
                {
                    foreach (var item in pieces)
                    {
                        if (item == null || item.Destroyed || item.Spawned || item.holdingOwner != null) continue;
                        if (committed) item.Destroy();
                        else
                        {
                            pawn.inventory ??= new Pawn_InventoryTracker(pawn);
                            if (!pawn.inventory.innerContainer.TryAdd(item, true))
                                GenPlace.TryPlaceThing(item, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _);
                        }
                    }
                }
                finally { committing.Remove(pawn); }
            }
        }

        public static IEnumerable<FloatMenuOption> Options(Pawn pawn, Thing circle)
        {
            foreach (var choice in SpellMastery.Choices)
            {
                if (!SpellMastery.Eligible(pawn, choice, out var state)) continue;
                string label = "TM_MasteryRitual".Translate(choice.Ability.LabelCap, ("TM_Mastery_" + choice.Id).Translate(),
                    choice.Magicyte, choice.Scroll == null ? "" : " + " + (DefDatabase<ThingDef>.GetNamedSilentFail(choice.Scroll)?.LabelCap.ToString() ?? choice.Scroll));
                if (state.Mastery(choice.Spell) == choice.Id || Offerings(pawn, circle, choice) == null)
                    yield return new FloatMenuOption(label, null);
                else yield return new FloatMenuOption(label, () =>
                {
                    var order = Order(pawn, circle, choice);
                    if (order != null) pawn.jobs.TryTakeOrderedJob(order, JobTag.Misc);
                });
            }
        }
    }

    public sealed class JobDriver_SpellMasteryRitual : JobDriver
    {
        private bool attempted;
        public override void ExposeData() { base.ExposeData(); Scribe_Values.Look(ref attempted, "attempted"); }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!SpellMasteryRitual.Validate(pawn, job) || !pawn.Reserve(job.targetA,job,1,-1,null,errorOnFailed)) return false;
            for (int i = 0; i < job.countQueue.Count; i++)
                if (job.targetQueueB[i].Thing.Spawned && !pawn.Reserve(job.targetQueueB[i],job,1,job.countQueue[i],null,errorOnFailed)) return false;
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(2500, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            var commit = ToilMaker.MakeToil("SpellMasteryCommit");
            commit.initAction = () =>
            {
                if (attempted || pawn.jobs.curDriver != this) { EndJobWith(JobCondition.Incompletable); return; }
                attempted = true;
                if (!SpellMasteryRitual.Complete(pawn,job)) EndJobWith(JobCondition.Incompletable);
            };
            commit.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return commit;
        }
    }
}
