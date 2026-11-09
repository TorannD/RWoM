using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class ApothecaryKitAbility : MightAbility
    {
        public ApothecaryKitAbility() { }
        public ApothecaryKitAbility(CompAbilityUser user):base(user) { }
        public ApothecaryKitAbility(AbilityData data):base(data) { }
        public ApothecaryKitAbility(Pawn pawn,AbilityUser.AbilityDef def):base(pawn,def) { }

        // The field job owns the atomic herbs-and-medicines transaction.
        public override void PostAbilityAttempt() { }
        internal const string Order="TM_ApothecaryGoldenHourCast";
        internal Job MakeJobFor(Pawn body,LocalTargetInfo target) =>
            JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(Order),target,body.Position,Pawn);
        public override Job GetJob(AbilityContext context,LocalTargetInfo target) => MakeJobFor(Pawn,target);
        public override bool CanCastPowerCheck(AbilityContext context,out string reason)
        {
            if(!base.CanCastPowerCheck(context,out reason)) return false;
            if(JobDriver_ApothecaryKit.Ready(Pawn)) return true;
            reason="TM_ApothecaryKitUnavailable".Translate();return false;
        }
    }

    public sealed class Verb_ApothecaryKit : Verb_UseAbility
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) =>
            JobDriver_ApothecaryKit.Candidate(CasterPawn,target.Pawn) && JobDriver_ApothecaryKit.Ready(CasterPawn);
        protected override bool TryCastShot() => false;
    }

    public sealed class Hediff_ApothecaryDressing : Hediff
    {
        internal Pawn doctor;
        public override bool ShouldRemove => doctor?.Spawned != true || pawn?.Spawned!=true || doctor.Map!=pawn.Map || !doctor.Position.InHorDistOf(pawn.Position,8f) ||
            !GenSight.LineOfSight(doctor.Position,pawn.Position,doctor.Map) ||
            // The paid job owns the duration. Four waits plus toil transitions
            // exceed 1200 ticks; a second expiry clock removed the final pulse.
            doctor.jobs?.curDriver is not JobDriver_ApothecaryKit kit || !kit.Channeling;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref doctor,"apothecary");
        }
    }

    public sealed class JobDriver_ApothecaryKit : JobDriver
    {
        private bool begun,paid,finished;
        private int pulses,mapId;
        private List<Pawn> patients=new();
        // TargetC binds the paying soul for the entire saved job. Older native
        // jobs did not store it; their physical doctor remains their owner.
        private Pawn Source=>job.targetC.Pawn??pawn;
        internal bool Channeling=>paid&&!finished && PositionValid();
        internal static bool Candidate(Pawn doctor,Pawn patient) => doctor?.Spawned==true && !doctor.Downed && !doctor.Dead &&
            patient?.Spawned==true && doctor.Map==patient.Map && !patient.Dead && !patient.IsPrisoner && patient.Faction!=null &&
            patient.Faction==doctor.Faction && patient.Position.InHorDistOf(doctor.Position,8f) &&
            GenSight.LineOfSight(doctor.Position,patient.Position,doctor.Map) && patient.health.hediffSet.hediffs.Any(ApothecaryEpisodes.Wound);
        private static IEnumerable<Thing> Medicines(Pawn doctor)
        {
            var def=ThingDefOf.MedicineHerbal;
            if (def==null) yield break;
            if (doctor.inventory!=null)
                foreach(var item in doctor.inventory.innerContainer)
                    if(item.def==def && item.stackCount>0) yield return item;
            foreach(var offset in GenAdj.AdjacentCellsAndInside)
            {
                var cell=doctor.Position+offset;
                if(!cell.InBounds(doctor.Map)) continue;
                foreach(var item in cell.GetThingList(doctor.Map))
                    if(item.def==def && item.stackCount>0 && !item.IsForbidden(doctor) &&
                        doctor.CanReachImmediate(item,PathEndMode.Touch) && doctor.CanReserve(item)) yield return item;
            }
        }
        internal static bool Ready(Pawn doctor)
        {
            var source=ApothecaryMartialPath.Owner(doctor);
            if(doctor?.Spawned!=true || doctor.Dead || doctor.Downed ||
                ApothecaryMartialPath.Path(source)?.MartialData is not ApothecaryProgress data || !data.GoldenHour ||
                data.KitReadyAt>Find.TickManager.TicksGame) return false;
            var stock=ApothecaryMartialPath.Stock(source);
            return stock!=null && stock.Severity>=stock.MaxSeverity*.6f && Medicines(doctor).Sum(t=>t.stackCount)>=4;
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Source==ApothecaryMartialPath.Owner(pawn) && Ready(pawn) && Candidate(pawn,job.targetA.Pawn);
        private bool PositionValid()=>pawn.Spawned && !pawn.Downed && !pawn.Dead && pawn.Position==job.targetB.Cell &&
            (!begun || mapId==pawn.Map.uniqueID) && pawn.stances?.stunner?.Stunned!=true &&
            Source==ApothecaryMartialPath.Owner(pawn) &&
            ApothecaryMartialPath.Path(Source)?.MartialData is ApothecaryProgress d && d.GoldenHour;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun,"begun");Scribe_Values.Look(ref paid,"paid");Scribe_Values.Look(ref finished,"finished");
            Scribe_Values.Look(ref pulses,"pulses");Scribe_Values.Look(ref mapId,"mapId");
            Scribe_Collections.Look(ref patients,"fieldPatients",LookMode.Reference);
            if(Scribe.mode==LoadSaveMode.PostLoadInit) patients??=new();
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_=>Finish());
            this.FailOn(()=>!PositionValid());
            yield return Toils_General.Do(()=>{
                if(!Ready(pawn) || !Candidate(pawn,job.targetA.Pawn)){EndJobWith(JobCondition.Incompletable);return;}
                begun=true;mapId=pawn.Map.uniqueID;
            });
            yield return Toils_General.Wait(Mathf.Max(1,Mathf.RoundToInt(300*Compatibility.AbilityTiming.WarmupMultiplier)),TargetIndex.A)
                .WithProgressBarToilDelay(TargetIndex.A).FailOn(()=>!Candidate(pawn,job.targetA.Pawn));
            yield return Toils_General.Do(()=>{if(!paid&&!Commit())EndJobWith(JobCondition.Incompletable);});
            for(int i=0;i<4;i++)
            {
                int pulse=i;
                yield return Toils_General.Wait(300);
                yield return Toils_General.Do(()=>Pulse(pulse));
            }
        }
        private void Cooldown(int ticks)
        {
            var path=ApothecaryMartialPath.Path(Source);
            if(path==null) return;
            int duration=Compatibility.AbilityTiming.Cooldown(ticks);
            ApothecaryMartialPath.Data(path).KitReadyAt=Find.TickManager.TicksGame+duration;
            var owner=Source.GetCompAbilityUserMight().AbilityData.Powers.OfType<ApothecaryKitAbility>().FirstOrDefault();
            if(owner!=null) owner.CooldownTicksLeft=duration;
        }
        private bool Commit()
        {
            if(paid || !PositionValid() || !Ready(pawn) || !Candidate(pawn,job.targetA.Pawn)) return false;
            var dressing=DefDatabase<HediffDef>.GetNamed("TM_ApothecaryDressing");
            var primary=job.targetA.Pawn;
            if(primary.health.hediffSet.HasHediff(dressing)) return false;
            var candidates=new List<Pawn>{primary};
            candidates.AddRange(GenRadial.RadialDistinctThingsAround(pawn.Position,pawn.Map,8,true).OfType<Pawn>()
                .Where(p=>p!=primary && Candidate(pawn,p) && !p.health.hediffSet.HasHediff(dressing))
                .OrderBy(p=>(pawn.Position-p.Position).LengthHorizontalSquared).ThenBy(p=>p.thingIDNumber).Take(3));
            var sources=Medicines(pawn).ToList();
            var pieces=new List<Thing>();
            int needed=4;
            try
            {
                foreach(var source in sources)
                {
                    int n=Mathf.Min(needed,source.stackCount);
                    pieces.Add(source.SplitOff(n));needed-=n;
                    if(needed==0) break;
                }
                var stock=ApothecaryMartialPath.Stock(Source);
                if(needed!=0 || stock==null || stock.Severity<.60f*stock.MaxSeverity) return false;
                stock.Severity-=.60f*stock.MaxSeverity;
                patients=candidates;paid=true;Cooldown(120000);
            }
            finally
            {
                foreach(var item in pieces)
                    if(paid) item.Destroy();
                    else if(pawn.inventory?.innerContainer.TryAdd(item,true)!=true)
                        GenPlace.TryPlaceThing(item,pawn.Position,pawn.Map,ThingPlaceMode.Near);
            }
            foreach(var p in patients)
            {
                var h=(Hediff_ApothecaryDressing)HediffMaker.MakeHediff(dressing,p);
                h.doctor=pawn;p.health.AddHediff(h);
            }
            return true;
        }
        private void Pulse(int index)
        {
            if(!Channeling || index!=pulses || !PositionValid()) return;
            pulses++;
            foreach(var p in patients)
            {
                if(!Candidate(pawn,p) || !p.health.hediffSet.hediffs.Any(h=>h is Hediff_ApothecaryDressing d && d.doctor==pawn)) continue;
                float remaining=5f;
                foreach(var wound in p.health.hediffSet.hediffs.Where(ApothecaryEpisodes.Wound).OrderByDescending(h=>h.Severity).ToList())
                {
                    float heal=Mathf.Min(remaining,wound.Severity);
                    wound.Heal(heal);remaining-=heal;
                    if(remaining<=0) break;
                }
            }
        }
        private void Finish()
        {
            if(finished) return;
            finished=true;
            if(begun && !paid)
            {
                var stock=ApothecaryMartialPath.Stock(Source);
                if(stock!=null) stock.Severity-=stock.MaxSeverity*.06f;
                Cooldown(6000);
            }
            foreach(var p in patients)
                if(p?.health?.hediffSet?.hediffs.FirstOrDefault(h=>h is Hediff_ApothecaryDressing d && d.doctor==pawn) is Hediff h)
                    p.health.RemoveHediff(h);
        }
    }
}
