using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public abstract class Verb_SoldierBurst : Verb_SpiritTargetable
    {
        internal void BeginFromHost(LocalTargetInfo target)
        {
            currentTarget = target;
            WarmupComplete();
        }
    }

    public sealed class JobDriver_SpiritSoldierBurst : JobDriver
    {
        private bool begun, paid, ended;
        private Verb_SoldierBurst burst;
        private MightAbility Power => SpiritHostedMartial.Find(pawn, job.targetA.Pawn, job.def.defName);

        private bool Valid(MightAbility power) => power != null && pawn.Spawned && !pawn.Dead && !pawn.Downed &&
            !pawn.InMentalState && !pawn.IsPrisoner && pawn.Faction == Faction.OfPlayer &&
            pawn.stances?.stunner?.Stunned != true && job.targetC.Thing != null &&
            pawn.equipment?.Primary == job.targetC.Thing &&
            SuperSoldierCast.Funded(pawn, power, job.def.defName) &&
            SuperSoldierCast.Valid(pawn, power, job.def.defName, job.targetB);

        public override bool TryMakePreToilReservations(bool errorOnFailed) => Valid(Power);

        private void Bind(MightAbility power)
        {
            burst.caster = pawn;
            burst.Ability = power;
            burst.verbProps = power.mightDef.MainVerb;
            burst.verbTracker = pawn.verbTracker;
        }

        private bool Begin()
        {
            var power = Power;
            if (ended || !Valid(power)) return false;
            if (begun) return paid && burst != null;
            if (!SpiritHostedMartial.Ready(pawn, power, job.def.defName)) return false;
            burst = job.def.defName == SuperSoldierCast.Buckshot ? new Verb_Buckshot() : new Verb_SuppressingFire();
            burst.loadID = "SpiritSoldier_" + job.GetUniqueLoadID();
            Bind(power);
            begun = true;
            burst.BeginFromHost(job.targetB);
            // Pay the original ability once for a started series, including later interruption.
            paid = true;
            power.PostAbilityAttempt();
            return true;
        }

        private bool Step()
        {
            if (ended || !begun || !paid) return false;
            var power = Power;
            if (!Valid(power) || burst == null) { Finish(); return false; }
            Bind(power);
            if (burst.Bursting) burst.VerbTick();
            if (burst.Bursting) return true;
            Finish();
            return false;
        }

        private void Finish()
        {
            ended = true;
            burst?.Reset();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun, "soldierBegun");
            Scribe_Values.Look(ref paid, "soldierPaid");
            Scribe_Values.Look(ref ended, "soldierEnded");
            Scribe_Deep.Look(ref burst, "soldierBurst");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_ => Finish());
            this.FailOn(() => !Valid(Power));
            var warmup = Toils_General.Wait(1);
            warmup.initAction = () => ticksLeftThisToil = UnityEngine.Mathf.Max(1, GenTicks.SecondsToTicks(
                Power.mightDef.MainVerb.warmupTime * Compatibility.AbilityTiming.WarmupMultiplier));
            yield return warmup;
            var channel = new Toil { defaultCompleteMode = ToilCompleteMode.Never };
            channel.initAction = () => { if (!Begin()) EndJobWith(JobCondition.Incompletable); };
            channel.tickAction = () => { if (!Step()) ReadyForNextToil(); };
            yield return channel;
        }
    }
}
