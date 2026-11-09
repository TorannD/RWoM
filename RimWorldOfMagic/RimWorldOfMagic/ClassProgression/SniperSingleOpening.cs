using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.Compatibility;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TorannMagic.ClassProgression
{
    public sealed class SniperOpeningAbility : MightAbility
    {
        internal const string JobName = "TM_SniperSingleOpeningCast";
        public SniperOpeningAbility() { }
        public SniperOpeningAbility(CompAbilityUser owner) : base(owner) { }
        public SniperOpeningAbility(AbilityData data) : base(data) { }
        public SniperOpeningAbility(Pawn pawn, AbilityUser.AbilityDef def) : base(pawn, def) { }

        public override Job GetJob(AbilityContext context, LocalTargetInfo target)
        {
            var job = MakeJobFor(Pawn, target);
            job.playerForced = context == AbilityContext.Player;
            return job;
        }

        internal Job MakeJobFor(Pawn body, LocalTargetInfo target)
        {
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(JobName),target,body.equipment?.Primary,body.Position);
            // The three primary targets are occupied by the victim, original gun and firing cell.
            if (body != Pawn) job.AddQueuedTarget(TargetIndex.B, Pawn);
            return job;
        }

        internal Verb_SniperSingleOpening VerbFor(Pawn body) => body == Pawn ? (Verb_SniperSingleOpening)Verb :
            new Verb_SniperSingleOpening { Ability = this, caster = body, verbProps = mightDef.MainVerb };

        public override bool CanCastPowerCheck(AbilityContext context, out string reason)
        {
            if (!base.CanCastPowerCheck(context, out reason)) return false;
            if (JobDriver_SniperSingleOpening.Ready(Pawn)) return true;
            reason = "TM_SniperOpeningUnavailable".Translate();
            return false;
        }
    }

    public sealed class Verb_SniperSingleOpening : Verb_SniperBallisticShot
    {
        protected override bool OwnsTechnique(DisciplineProgress path) => SniperAscension.HasOpening(Ability?.Pawn ?? CasterPawn);
        protected override string RoundName => "TM_SniperOpeningRound";
        public override float EffectiveRange => Mathf.Min(45f, base.EffectiveRange);
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
            => target.IsValid && root.DistanceToSquared(target.Cell) <= 45 * 45 && base.CanHitTargetFrom(root, target);
        protected override bool TryCastShot() => false;
        internal bool Fire(Pawn target)
        {
            if (!FireWeapon(target)) return false;
            CasterPawn.equipment?.Primary?.GetComp<CompEquippable>()?.PrimaryVerb?.verbProps.soundCast?.PlayOneShot(SoundInfo.InMap(CasterPawn));
            return true;
        }
    }

    public sealed class SniperOpeningBullet : SniperWeaponBullet
    {
        public override int DamageAmount => Mathf.Max(1, Mathf.RoundToInt(base.DamageAmount * 1.2f));
    }

    public sealed class JobDriver_SniperSingleOpening : JobDriver
    {
        private bool begun;
        private bool paid;
        private int fired;
        private int mapId;
        private SniperOpeningAbility owner;
        private Verb_SniperSingleOpening firingVerb;
        internal bool Aiming => begun && fired < 3;
        private bool Hosted => job.targetQueueB?.Count > 0;
        private Pawn Source => Hosted ? job.targetQueueB[0].Pawn : pawn;
        private SniperOpeningAbility Owner => owner ??= Source?.GetCompAbilityUserMight()?.AbilityData?.Powers
            .OfType<SniperOpeningAbility>().FirstOrDefault();
        private Verb_SniperSingleOpening FiringVerb => firingVerb ??= Owner?.VerbFor(pawn);
        private bool OwnsOrderedTechnique() => !Hosted || (Owner != null &&
            SpiritHostedMartial.Find(pawn, Source, SniperOpeningAbility.JobName) == Owner);

        internal static bool Ready(Pawn shooter, Pawn source = null)
        {
            source ??= shooter;
            if (shooter?.Spawned != true || shooter.Dead || shooter.Downed || !SniperAscension.HasOpening(source) ||
                shooter.WorkTagIsDisabled(WorkTags.Violent)) return false;
            var stamina = source.GetCompAbilityUserMight()?.Stamina;
            return SniperSurvey.Path(source).OpeningReadyAt <= Find.TickManager.TicksGame &&
                stamina?.MaxLevel > 0f && stamina.CurLevel >= stamina.MaxLevel * .60f;
        }

        private bool CanContinue()
        {
            if (pawn.Position != job.targetC.Cell || pawn.equipment?.Primary == null || pawn.equipment.Primary != job.targetB.Thing ||
                !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.stances?.stunner?.Stunned == true ||
                (begun && mapId != pawn.Map.uniqueID) || !SniperAscension.HasOpening(Source)) return false;
            if (Hosted && (!SpiritRebirthState.TryGetPossessingSpirit(pawn, out var spirit) || spirit != Source ||
                pawn.InMentalState || pawn.IsPrisoner || pawn.Faction != Faction.OfPlayer)) return false;
            return FiringVerb?.CanHitTargetFrom(pawn.Position, job.targetA) == true;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => Ready(pawn, Source) && CanContinue() && OwnsOrderedTechnique();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun, "sniperOpeningBegun");
            Scribe_Values.Look(ref paid, "sniperOpeningPaid");
            Scribe_Values.Look(ref fired, "sniperOpeningShots");
            Scribe_Values.Look(ref mapId, "sniperOpeningMap");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_ => Interrupt());
            this.FailOn(() => !CanContinue() || (!paid && !Ready(pawn, Source)));
            yield return Toils_General.Do(() => { begun = true; mapId = pawn.Map.uniqueID; });
            yield return Toils_General.Wait(Mathf.Max(1, Mathf.RoundToInt(300f * AbilityTiming.WarmupMultiplier)),
                TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(() => Shoot(0));
            yield return Toils_General.Wait(270, TargetIndex.A);
            yield return Toils_General.Do(() => Shoot(1));
            yield return Toils_General.Wait(270, TargetIndex.A);
            yield return Toils_General.Do(() => Shoot(2));
        }

        private void Shoot(int expectedFired)
        {
            if (fired > expectedFired) return;
            if (fired != expectedFired || !CanContinue() || !OwnsOrderedTechnique() || (!paid && !Ready(pawn, Source)) ||
                !FiringVerb.Fire(job.targetA.Pawn))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            fired++;
            if (!paid) Pay(.60f, 120000);
        }

        private void Interrupt()
        {
            if (begun && !paid) Pay(.06f, 6000);
            begun = false;
        }

        private void Pay(float fraction, int baseTicks)
        {
            var path = SniperSurvey.Path(Source);
            if (paid || path == null) return;
            paid = true;
            var stamina = Source.GetCompAbilityUserMight()?.Stamina;
            if (stamina != null) stamina.CurLevel = Mathf.Max(0, stamina.CurLevel - fraction * stamina.MaxLevel);
            int cooldown = AbilityTiming.Cooldown(baseTicks);
            path.OpeningReadyAt = Find.TickManager.TicksGame + cooldown;
            if (Owner != null) Owner.CooldownTicksLeft = cooldown;
        }
    }
}
