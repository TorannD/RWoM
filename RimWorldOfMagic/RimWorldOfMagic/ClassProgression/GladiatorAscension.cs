using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public enum GladiatorUltimate { None, Whirlwind, LastCircle }

    internal static class GladiatorAscension
    {
        internal const string Trial = "rwom.gladiator.evacuation";
        internal static bool Qualified(Pawn pawn) => JobDriver_GladiatorDefense.Path(pawn)?.Stage == InitiationStage.Master
            && pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel >= 30;
        internal static bool Graduate(Pawn pawn) => Qualified(pawn) && JobDriver_GladiatorDefense.Path(pawn).RecordCompletedTrial(Trial);

        internal static bool Choose(Pawn pawn, GladiatorUltimate choice)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,choice==GladiatorUltimate.LastCircle)) return false;
            if (!Qualified(pawn) || (choice != GladiatorUltimate.Whirlwind && choice != GladiatorUltimate.LastCircle)) return false;
            var path = JobDriver_GladiatorDefense.Path(pawn);
            var might = pawn.GetCompAbilityUserMight();
            var owner = might.MightData.MightPowersG.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Whirlwind));
            if (owner == null || path.Ultimate != GladiatorUltimate.None ||
                !ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Master, Trial)) return false;
            path.Ultimate = choice;
            if (choice == GladiatorUltimate.Whirlwind)
            {
                owner.learned = true;
                TM_ClassState.AddRuntimeAbility(might, owner.abilityDef);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(JobDriver_TakeToBed), "MakeNewToils")]
    internal static class GladiatorRescueTrial
    {
        private static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_TakeToBed __instance)
        {
            foreach (var toil in __result) yield return toil;
            if (!ClassProgressionFeature.Enabled || __instance.job.def != JobDefOf.Rescue) yield break;
            yield return Toils_General.Do(() => {
                var pawn = __instance.pawn;
                var ally = __instance.job.targetA.Pawn;
                if (pawn?.Spawned != true || pawn.Downed || pawn.Dead || ally?.Spawned != true || ally.Map != pawn.Map ||
                    ally == pawn || ally.Dead || !ally.Downed || ally.IsPrisoner || pawn.Faction == null || pawn.Faction != ally.Faction ||
                    !ally.health.hediffSet.hediffs.Any(h => h is Hediff_Injury) ||
                    ally.CurrentBed() is not Building_Bed bed || bed != __instance.job.targetB.Thing) return;
                BladeMastery.Delivered(pawn, ally);
                MonkMartialPath.Rescue(pawn, ally);
                PsionicMartialPath.Service(pawn,rescueId:ally.thingIDNumber);
                DeathKnightMartialPath.Rescue(pawn, ally);
                FacelessMartialPath.Rescue(pawn, ally);
                ShadowWalkerMartialPath.Rescue(pawn, ally);
                if (!GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map)) return;
                if (GladiatorAscension.Qualified(pawn) && JobDriver_GladiatorDefense.Allies(pawn, ally))
                    GladiatorAscension.Graduate(pawn);
                SniperRescueTrial.Delivered(pawn, ally);
            });
        }
    }

    public sealed class GladiatorCircle : IExposable
    {
        private IntVec3 center;
        private int started, pulses;
        private Pawn soul;
        private Hediff possession;
        private bool projected;
        public GladiatorCircle() { }
        internal GladiatorCircle(IntVec3 cell, int now) { center = cell; started = now; }
        internal int Started => started;
        internal void Bind(Pawn host,Pawn original)
        {
            projected = true;
            soul = original;
            possession = host.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
        }
        internal bool Contains(Pawn actor, Pawn ally, int now) => actor.Spawned && !actor.Downed && !actor.Dead &&
            now >= started && now - started < 1200 && actor.Position.InHorDistOf(center,4)
            && ally.Spawned && ally.Map == actor.Map && ally.Position.InHorDistOf(center,4)
            && (!projected || (possession != null && actor.health.hediffSet.hediffs.Contains(possession) &&
                SpiritRebirthState.TryGetPossessingSpirit(actor,out var current) && current == soul));
        public void ExposeData()
        {
            Scribe_Values.Look(ref center,"center"); Scribe_Values.Look(ref started,"started"); Scribe_Values.Look(ref pulses,"pulses");
            Scribe_Values.Look(ref projected,"projected");
            Scribe_References.Look(ref soul,"circleSoul");
            Scribe_References.Look(ref possession,"circlePossession");
        }
        internal static IEnumerable<Pawn> Nearby(Map map, IntVec3 center)
            => GenRadial.RadialDistinctThingsAround(center,map,4f,true).OfType<Pawn>()
                .OrderBy(p => (p.Position-center).LengthHorizontalSquared).ThenBy(p => p.thingIDNumber);
        internal bool Tick(Pawn actor,int now)
        {
            if (!ClassProgressionFeature.Enabled || !Contains(actor,actor,now)) return false;
            if (pulses >= 3 || now-started < 300*(pulses+1)) return true;
            pulses++;
            var weapon = actor.equipment?.Primary;
            if (weapon?.def.IsMeleeWeapon != true) return true;
            var strike = weapon.GetComp<CompEquippable>()?.AllVerbs.Where(v => v.verbProps.IsMeleeAttack && v.tool != null)
                .OrderByDescending(v => v.tool.power).FirstOrDefault();
            if (strike == null) return true;
            var hit = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, .8f*strike.verbProps.AdjustedMeleeDamageAmount(strike,actor),0,-1,actor,null,weapon.def);
            var targets = Nearby(actor.Map,center).Where(p => !p.Dead && !p.Downed && !p.IsPrisoner && p.HostileTo(actor)
                && GenSight.LineOfSight(actor.Position,p.Position,actor.Map)).Take(3).ToList();
            foreach (var target in targets)
                if (Contains(actor,target,now) && !target.Dead && !target.Downed && target.HostileTo(actor)) target.TakeDamage(hit);
            return true;
        }
    }

    public sealed class GladiatorWard : IExposable
    {
        private Pawn guardian;
        private int until;
        private float remaining;
        public GladiatorWard() { }
        internal GladiatorWard(Pawn guardian,int now) { this.guardian=guardian; until=now+1200; remaining=25; }
        internal bool Active(Pawn ally,int now) => remaining > 0 && now < until && guardian?.Faction != null && guardian.Faction == ally.Faction
            && ClassIdentity.Component(guardian)?.Circle is GladiatorCircle circle &&
            circle.Started == until-1200 && circle.Contains(guardian,ally,now);
        internal void Absorb(ref DamageInfo hit)
        {
            if (hit.Amount <= 0 || hit.Def?.isExplosive != false || hit.Instigator?.HostileTo(guardian) != true) return;
            float n=System.Math.Min(remaining,hit.Amount); remaining-=n; hit.SetAmount(hit.Amount-n);
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref guardian,"guardian"); Scribe_Values.Look(ref until,"until"); Scribe_Values.Look(ref remaining,"remaining");
        }
    }

    public sealed class JobDriver_LastCircle : JobDriver
    {
        private bool begun, committed;
        internal const string HostedJobName = "TM_SpiritCastLastCircle";
        private Pawn Source => job.def.defName == HostedJobName ? job.targetB.Pawn : pawn;
        private static bool Chosen(Pawn pawn) => JobDriver_GladiatorDefense.Path(pawn)?.Ultimate == GladiatorUltimate.LastCircle;
        internal static bool CanProject(Pawn host,Pawn soul) =>
            soul!=null && Chosen(soul) && !Chosen(host) && SpiritRebirthState.TryGetPossessingSpirit(host,out var held) &&
            held==soul && SpiritRebirthState.TryGet(soul,out var state) && !state.DepletionPending;
        internal static bool Ready(Pawn pawn) => ReadyFrom(pawn,pawn);
        internal static bool ReadyFrom(Pawn pawn,Pawn source)
        {
            var path=JobDriver_GladiatorDefense.Path(source);
            var sp=source?.GetCompAbilityUserMight()?.Stamina;
            return pawn?.Spawned == true && !pawn.Dead && !pawn.Downed && pawn.IsColonistPlayerControlled
                && (source==pawn ? !SpiritRebirthActionPolicy.Marked(pawn) : CanProject(pawn,source))
                && ClassIdentity.Component(pawn)?.Circle == null
                && !pawn.WorkTagIsDisabled(WorkTags.Violent) && pawn.equipment?.Primary?.def.IsMeleeWeapon == true && path?.Ultimate == GladiatorUltimate.LastCircle
                && path.CircleReadyAt <= Find.TickManager.TicksGame && sp?.MaxLevel > 0 && sp.CurLevel >= .6f*sp.MaxLevel;
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => ReadyFrom(pawn,Source);
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Values.Look(ref begun,"circleBegun"); Scribe_Values.Look(ref committed,"circleCommitted");
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(condition => Interrupt());
            this.FailOn(() => !committed && !ReadyFrom(pawn,Source));
            yield return Toils_General.Do(() => begun=true);
            yield return Toils_General.Wait(300).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(Release);
        }
        private void Interrupt()
        {
            var path = JobDriver_GladiatorDefense.Path(Source);
            if (!begun || committed || path == null) return;
            committed = true;
            var stamina = Source.GetCompAbilityUserMight()?.Stamina;
            if (stamina != null) stamina.CurLevel = System.Math.Max(0, stamina.CurLevel - .06f * stamina.MaxLevel);
            path.CircleReadyAt = Find.TickManager.TicksGame + 6000;
        }

        private void Release()
        {
            if (committed) return;
            if (!begun || !ReadyFrom(pawn,Source)) { EndJobWith(JobCondition.Incompletable); return; }
            committed = true;
            var comp = ClassIdentity.Component(pawn);
            int now = Find.TickManager.TicksGame;
            JobDriver_GladiatorDefense.Path(Source).CircleReadyAt = now + 120000;
            var stamina = Source.GetCompAbilityUserMight().Stamina;
            stamina.CurLevel -= .6f * stamina.MaxLevel;
            comp.Circle = new GladiatorCircle(pawn.Position, now);
            if (Source!=pawn) comp.Circle.Bind(pawn,Source);
            HealthUtility.AdjustSeverity(pawn, DefDatabase<HediffDef>.GetNamed("TM_LastCircleStance"), 1);
            var allies = GladiatorCircle.Nearby(pawn.Map, pawn.Position)
                .Where(p => JobDriver_GladiatorDefense.Allies(pawn, p))
                .Select(ClassIdentity.Component)
                .Where(c => c.Ward == null || !c.Ward.Active((Pawn)c.parent, now)).Take(4);
            foreach (var ally in allies) ally.Ward = new GladiatorWard(pawn, now);
        }
    }
}
