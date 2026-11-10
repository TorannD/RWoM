using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class JobDriver_SpiritTigerStrike : JobDriver
    {
        internal const string Order="TM_SpiritTigerStrike";
        private bool begun, paid;
        private Verb_TigerStrike burst;

        private MightAbility Power
        {
            get
            {
                var source=job?.targetA.Pawn?.GetCompAbilityUserMight();
                var powers=source?.AbilityData.Powers.OfType<MightAbility>()
                    .Where(p=>p.Def==TorannMagicDefOf.TM_TigerStrike && ReferenceEquals(p.AbilityUser,source)).Take(2).ToList();
                return powers?.Count==1 ? powers[0] : null;
            }
        }

        private bool Valid(MightAbility power) => power!=null && pawn?.Spawned==true && !pawn.Dead && !pawn.Downed &&
            !pawn.InMentalState && !pawn.IsPrisoner && pawn.Faction==Faction.OfPlayer && pawn.equipment?.Primary==null &&
            !pawn.WorkTagIsDisabled(WorkTags.Violent) && pawn.stances?.stunner?.Stunned!=true &&
            SpiritRebirthState.TryGetPossessingSpirit(pawn,out var soul) && soul==power.Pawn &&
            SpiritRebirthState.TryGet(soul,out var state) && !state.DepletionPending &&
            job.targetB.Pawn?.Downed==false && SpiritHostedMartial.CanStrike(pawn,job.targetB.Pawn,power);

        public override bool TryMakePreToilReservations(bool errorOnFailed) => Valid(Power);

        private void Bind(MightAbility power)
        {
            burst.caster=pawn;
            burst.Ability=power;
            burst.verbProps=power.mightDef.MainVerb;
            burst.verbTracker=pawn.verbTracker;
        }

        private bool Begin()
        {
            var power=Power;
            if(paid || !Valid(power)) return false;
            if(begun) return burst!=null;
            var chi=MonkMartialPath.Chi(power.Pawn);
            if(power.CooldownTicksLeft>0 || chi==null || chi.Severity<100*power.MightUser.ActualChiCost(power.mightDef)) return false;
            burst=new Verb_TigerStrike{loadID="SpiritTiger_"+job.GetUniqueLoadID()};
            Bind(power);
            begun=true;
            burst.BeginFromHost(job.targetB);
            return true;
        }

        private bool Step()
        {
            if(paid || !begun) return false;
            var power=Power;
            if(!Valid(power) || burst==null) {Finish();return false;}
            Bind(power);
            if(burst.Bursting) burst.VerbTick();
            if(burst.Bursting) return true;
            Finish();
            return false;
        }

        private void Finish()
        {
            burst?.Reset();
            if(!begun || paid) return;
            paid=true;
            // A started series consumes the original ability even if possession is interrupted.
            Power?.PostAbilityAttempt();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun,"tigerBegun");
            Scribe_Values.Look(ref paid,"tigerPaid");
            Scribe_Deep.Look(ref burst,"tigerBurst");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_=>Finish());
            this.FailOn(()=>!Valid(Power));
            var warmup=Toils_General.Wait(1);
            warmup.initAction=()=>ticksLeftThisToil=UnityEngine.Mathf.Max(1,GenTicks.SecondsToTicks(
                Power.mightDef.MainVerb.warmupTime*Compatibility.AbilityTiming.WarmupMultiplier));
            yield return warmup;
            var channel=new Toil{defaultCompleteMode=ToilCompleteMode.Never};
            channel.initAction=()=>{if(!Begin())EndJobWith(JobCondition.Incompletable);};
            channel.tickAction=()=>{if(!Step())ReadyForNextToil();};
            yield return channel;
        }
    }
}
