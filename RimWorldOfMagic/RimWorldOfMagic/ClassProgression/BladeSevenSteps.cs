using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.Compatibility;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class BladeFormAbility : MightAbility
    {
        internal const string JobName = "TM_BladeSevenStepsCast";
        public BladeFormAbility() { }
        public BladeFormAbility(CompAbilityUser owner) : base(owner) { }
        public BladeFormAbility(AbilityData data) : base(data) { }
        public BladeFormAbility(Pawn pawn, AbilityUser.AbilityDef def) : base(pawn, def) { }

        public override Job GetJob(AbilityContext context, LocalTargetInfo target)
        {
            var job = MakeJobFor(Pawn,target);
            job.playerForced = context == AbilityContext.Player;
            return job;
        }
        internal Job MakeJobFor(Pawn body, LocalTargetInfo target)
        {
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(JobName),target,body.equipment?.Primary,body.Position);
            if (body != Pawn) job.AddQueuedTarget(TargetIndex.B,Pawn);
            return job;
        }
        internal void ChooseRoute(Pawn body)
        {
            var map = body.Map;
            var verb = new Verb_BladeSevenSteps { Ability=this, caster=body, verbProps=mightDef.MainVerb };
            bool Valid(LocalTargetInfo target) => body.Spawned && body.Map == map && body.CurJobDef?.defName != JobName &&
                SpiritHostedMartial.Find(body,Pawn,JobName)==this && SpiritHostedMartial.Ready(body,this,JobName) &&
                verb.CanHitTargetFrom(body.Position,target);
            Find.Targeter.BeginTargeting(mightDef.MainVerb.targetParams,target => {
                if (!Valid(target)) return;
                var job=MakeJobFor(body,target);
                Odyssey.ManualTravelSafety.Submit(body,job,map,target.Cell,true,()=>Valid(target) && body.Position==job.targetC.Cell,
                    ()=>body.jobs.TryTakeOrderedJob(job,JobTag.Misc));
            },highlightAction:verb.DrawHighlight,targetValidator:Valid,caster:body);
        }
        public override bool CanCastPowerCheck(AbilityContext context, out string reason)
        {
            if (!base.CanCastPowerCheck(context, out reason)) return false;
            if (JobDriver_BladeSevenSteps.Ready(Pawn)) return true;
            reason = "TM_BladeFormUnavailable".Translate();
            return false;
        }
    }

    public sealed class Verb_BladeSevenSteps : Verb_UseAbility
    {
        internal static bool Clear(Pawn actor, IntVec3 cell) => cell.InBounds(actor.Map) && !cell.Fogged(actor.Map) &&
            cell.StandableBy(actor.Map, actor) && (cell.GetDoor(actor.Map) is not Building_Door door || door.Open) &&
            (cell.GetFirstPawn(actor.Map) is not Pawn other || other == actor);

        // Native sight-line enumeration returns cardinal neighbours, including both endpoints.
        internal static bool ShortRoute(IntVec3 start, IntVec3 end) =>
            start.IsValid && end.IsValid && start != end && (end - start).LengthManhattan <= 12;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var pawn = CasterPawn;
            if (target.HasThing || pawn?.Spawned != true || !ShortRoute(root,target.Cell) ||
                !target.Cell.InBounds(pawn.Map) || !BladeAscension.HasForm(Ability?.Pawn ?? pawn) || Verb_BladePassage.Strike(pawn) == null ||
                !GenSight.LineOfSight(root,target.Cell,pawn.Map)) return false;
            return GenSight.PointsOnLineOfSight(root,target.Cell).Skip(1).All(cell => Clear(pawn,cell));
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            var pawn = CasterPawn;
            if (pawn?.Spawned != true || !target.IsValid || !ShortRoute(pawn.Position,target.Cell) || !target.Cell.InBounds(pawn.Map)) return;
            GenDraw.DrawFieldEdges(GenSight.PointsOnLineOfSight(pawn.Position,target.Cell).ToList(),
                CanHitTargetFrom(pawn.Position,target) ? Color.cyan : Color.red);
        }
        protected override bool TryCastShot() => false;
    }

    public sealed class JobDriver_BladeSevenSteps : JobDriver
    {
        private bool preparing, paid, finished;
        private int mapId, expires, lastStep;
        private List<int> hitIds = new();
        internal bool Active => paid && !finished;

        private Pawn Source => job.targetQueueB?.Count > 0 ? job.targetQueueB[0].Pawn : pawn;

        internal static bool Exhausted(Pawn body)
        {
            var path=BladeCareer.ActivePath(body,out _);
            return path?.BladeExhaustedUntil > 0 && path.BladeExhaustedUntil > Find.TickManager.TicksGame;
        }

        internal static bool Ready(Pawn pawn, Pawn source = null)
        {
            source ??= pawn;
            if (pawn?.Spawned != true || pawn.Dead || pawn.Downed || !BladeAscension.HasForm(source) ||
                pawn.WorkTagIsDisabled(WorkTags.Violent) || Verb_BladePassage.Strike(pawn) == null) return false;
            var stamina = source.GetCompAbilityUserMight()?.Stamina;
            return BladeCareer.Path(source).BladeFormReadyAt <= Find.TickManager.TicksGame && stamina?.MaxLevel > 0 &&
                stamina.CurLevel >= .6f * stamina.MaxLevel;
        }

        private bool Equipped() => pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.stances?.stunner?.Stunned != true &&
            pawn.equipment?.Primary != null && pawn.equipment.Primary == job.targetB.Thing && Source != null &&
            BladeAscension.HasForm(Source) && (Source==pawn || SpiritHostedMartial.Find(pawn,Source,BladeFormAbility.JobName) is BladeFormAbility);
        private bool ValidStart() => Equipped() && Ready(pawn,Source) && pawn.Position == job.targetC.Cell &&
            !job.targetA.HasThing && Verb_BladeSevenSteps.ShortRoute(pawn.Position,job.targetA.Cell) &&
            job.targetA.Cell.InBounds(pawn.Map) && GenSight.LineOfSight(pawn.Position,job.targetA.Cell,pawn.Map) &&
            GenSight.PointsOnLineOfSight(pawn.Position,job.targetA.Cell).Skip(1).All(cell=>Verb_BladeSevenSteps.Clear(pawn,cell));
        public override bool TryMakePreToilReservations(bool errorOnFailed) => ValidStart();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref preparing,"bladeFormPreparing"); Scribe_Values.Look(ref paid,"bladeFormPaid");
            Scribe_Values.Look(ref finished,"bladeFormFinished"); Scribe_Values.Look(ref expires,"bladeFormEnds");
            Scribe_Values.Look(ref mapId,"bladeFormMap"); Scribe_Values.Look(ref lastStep,"bladeFormLastContactStep");
            Scribe_Collections.Look(ref hitIds,"bladeFormHits",LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) hitIds ??= new List<int>();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_=>FinishForm());
            this.FailOn(()=>!Equipped() || ((preparing || paid) && pawn.Map.uniqueID != mapId) ||
                (paid && Find.TickManager.TicksGame >= expires));
            var start = job.targetC.Cell;
            var end = job.targetA.Cell;
            yield return Toils_General.Do(() =>
            {
                if (!ValidStart()) { EndJobWith(JobCondition.Incompletable); return; }
                preparing = true;
                mapId = pawn.Map.uniqueID;
            });
            yield return Toils_General.Wait(Mathf.Max(1, Mathf.RoundToInt(300f * AbilityTiming.WarmupMultiplier)))
                .WithProgressBarToilDelay(TargetIndex.A).FailOn(()=>pawn.Position != start);
            yield return Toils_General.Do(() =>
            {
                if (paid) return;
                if (!ValidStart()) { EndJobWith(JobCondition.Incompletable); return; }
                Pay(.6f,120000);
                paid = true;
                preparing = false;
                expires = Find.TickManager.TicksGame + 840;
            });
            if (!Verb_BladeSevenSteps.ShortRoute(start,end)) yield break;
            int index = 0;
            var previous = start;
            foreach (var point in GenSight.PointsOnLineOfSight(start,end).Skip(1))
            {
                int step = ++index;
                var from = previous;
                var to = point;
                previous = to;
                yield return Toils_Goto.GotoCell(to, PathEndMode.OnCell).FailOn(() =>
                    !StepOpen(from,to) || (pawn.pather.Moving && pawn.pather.curPath != null &&
                    pawn.pather.nextCell != from && pawn.pather.nextCell != to));
                yield return Toils_General.Wait(45).FailOn(() => pawn.Position != to);
                yield return Toils_General.Do(() =>
                {
                    if (pawn.Position != to) { EndJobWith(JobCondition.Incompletable); return; }
                    Contact(step);
                });
            }
        }

        private bool StepOpen(IntVec3 from, IntVec3 to) => (pawn.Position == from || pawn.Position == to) &&
            Verb_BladeSevenSteps.Clear(pawn,to);

        private void Contact(int step)
        {
            if (!Active || lastStep >= step) return;
            lastStep = step;
            if (hitIds.Count >= 7) return;
            var strike = Verb_BladePassage.Strike(pawn);
            if (strike == null) return;
            var victim = GenAdj.AdjacentCells.Select(offset=>pawn.Position + offset)
                .Where(cell=>cell.InBounds(pawn.Map)).Select(cell=>cell.GetFirstPawn(pawn.Map))
                .Where(p=>p != null && p.Spawned && !p.Dead && !p.Downed && !p.IsPrisoner && p.HostileTo(pawn) &&
                    pawn.CanReachImmediate(p, PathEndMode.Touch) && hitIds.Count(id=>id == p.thingIDNumber) < 3 &&
                    (hitIds.Contains(p.thingIDNumber) || hitIds.Distinct().Count() < 3))
                .OrderBy(p=>hitIds.Count(id=>id == p.thingIDNumber)).ThenBy(p=>p.thingIDNumber).FirstOrDefault();
            if (victim == null) return;
            var damage = new DamageInfo(strike.verbProps.meleeDamageDef,
                Mathf.Max(1f,.55f * strike.verbProps.AdjustedMeleeDamageAmount(strike,pawn)),
                strike.verbProps.AdjustedArmorPenetration(strike,pawn),instigator:pawn,weapon:pawn.equipment.Primary.def);
            hitIds.Add(victim.thingIDNumber);
            victim.TakeDamage(damage);
        }

        private void Pay(float fraction, int ticks)
        {
            var comp = Source?.GetCompAbilityUserMight();
            var path = BladeCareer.Path(Source);
            if (path == null || comp?.Stamina == null) return;
            comp.Stamina.CurLevel = Mathf.Max(0,comp.Stamina.CurLevel - fraction * comp.Stamina.MaxLevel);
            int duration = AbilityTiming.Cooldown(ticks);
            path.BladeFormReadyAt = Find.TickManager.TicksGame + duration;
            var owner = comp.AbilityData.Powers.OfType<BladeFormAbility>().FirstOrDefault();
            if (owner != null) owner.CooldownTicksLeft = duration;
        }

        private void FinishForm()
        {
            if (finished) return;
            finished = true;
            if (preparing && !paid) Pay(.06f,6000);
            if (paid && BladeCareer.Path(Source) is DisciplineProgress path)
                path.BladeExhaustedUntil = Find.TickManager.TicksGame + 360;
        }
    }
}
