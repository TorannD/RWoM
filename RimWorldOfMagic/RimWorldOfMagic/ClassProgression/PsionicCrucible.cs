using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class PsionicCrucibleAbility : MightAbility
    {
        public PsionicCrucibleAbility() { }
        public PsionicCrucibleAbility(CompAbilityUser user):base(user) { }
        public PsionicCrucibleAbility(AbilityData data):base(data) { }
        public PsionicCrucibleAbility(Pawn pawn,AbilityUser.AbilityDef def):base(pawn,def) { }
        internal const string JobName="TM_PsionicCrucibleCast";
        public override void PostAbilityAttempt() { }
        internal void ChooseFrom(Pawn body)
        {
            var map=body.Map;
            bool Valid(LocalTargetInfo target)=>body.Spawned && body.Map==map &&
                SpiritHostedMartial.Find(body,Pawn,JobName)==this && JobDriver_PsionicCrucible.ReadyFrom(body,Pawn) &&
                JobDriver_PsionicCrucible.Location(body,target.Cell);
            Find.Targeter.BeginTargeting(new TargetingParameters{canTargetLocations=true,canTargetPawns=false,canTargetBuildings=false},
                target=>{if(Valid(target))body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(JobName),target.Cell,body.Position,Pawn),JobTag.Misc);},
                highlightAction:target=>GenDraw.DrawRadiusRing(target.Cell,3),targetValidator:Valid,caster:body);
        }
        public override Job GetJob(AbilityContext context,LocalTargetInfo target)=>
            JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(JobName),target.Cell,Pawn.Position,Pawn);
        public override bool CanCastPowerCheck(AbilityContext context,out string reason)
        {
            if(!base.CanCastPowerCheck(context,out reason))return false;
            if(JobDriver_PsionicCrucible.Ready(Pawn)) return true;
            reason="TM_PsionicCrucibleUnavailable".Translate();return false;
        }
    }
    public sealed class Verb_PsionicCrucible : Verb_UseAbility
    {
        public override bool CanHitTargetFrom(IntVec3 root,LocalTargetInfo target)=>JobDriver_PsionicCrucible.Ready(CasterPawn) &&
            JobDriver_PsionicCrucible.Location(CasterPawn,target.Cell);
        protected override bool TryCastShot()=>false;
    }
    public sealed class Hediff_CrucibleWard : Hediff
    {
        internal Pawn source;
        public override void ExposeData() {base.ExposeData();Scribe_References.Look(ref source,"psionicSource");}
        public override bool ShouldRemove=>source?.jobs?.curDriver is not JobDriver_PsionicCrucible job || !job.Protects(pawn);
        internal static void Shield(Pawn target,ref DamageInfo hit,out bool absorbed)
        {
            absorbed=false;
            if(!ClassProgressionFeature.Enabled || hit.Amount<=0 || hit.Instigator==null)return;
            var marker=DefDatabase<HediffDef>.GetNamedSilentFail("TM_PsionicCrucibleWard");
            if(marker!=null && target.health?.hediffSet?.GetFirstHediffOfDef(marker) is Hediff_CrucibleWard h &&
                h.source?.jobs?.curDriver is JobDriver_PsionicCrucible driver && hit.Instigator.HostileTo(target) &&
                hit.Def?.ExternalViolenceFor(target)==true && driver.Protects(target))
                driver.Shield(ref hit,out absorbed);
        }
    }
    public sealed class JobDriver_PsionicCrucible : JobDriver
    {
        bool begun,paid,finished;
        int pulses,mapId;
        float barrier;
        List<Pawn> enemies=new(),allies=new();
        internal static bool Location(Pawn p,IntVec3 cell)=>p?.Spawned==true && !p.Downed && !p.Dead && cell.InBounds(p.Map) &&
            !cell.Fogged(p.Map) && p.Position.InHorDistOf(cell,12) && GenSight.LineOfSight(p.Position,cell,p.Map);
        internal Pawn SkillOwner=>job?.targetC.Pawn ?? pawn;
        internal static bool Bound(Pawn body,Pawn owner)=>body!=null && owner!=null &&
            (body==owner || SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) && soul==owner);
        internal static bool Ready(Pawn p)=>ReadyFrom(p,p);
        internal static bool ReadyFrom(Pawn p,Pawn owner)
        {
            var career=PsionicMartialPath.Path(owner);var data=PsionicMartialPath.Read(career);
            var stam=owner?.GetCompAbilityUserMight()?.Stamina;
            return Bound(p,owner) && career?.Stage==InitiationStage.Ascended && data?.Crucible==true && Find.TickManager.TicksGame>=data.CrucibleReadyAt &&
                p.Spawned && !p.Downed && !p.Dead && !p.WorkTagIsDisabled(WorkTags.Violent) && stam!=null && stam.CurLevel>=stam.MaxLevel*.3f &&
                PsionicMartialPath.Energy(owner)?.Severity>=.6f*PsionicMartialPath.Capacity(owner);
        }
        internal bool Protects(Pawn ally)=>paid && !finished && barrier>0 && InPlace() && ally?.Spawned==true && ally.Map==pawn.Map &&
            ally.Faction==pawn.Faction && !ally.IsPrisoner && allies.Contains(ally) && ally.Position.InHorDistOf(job.targetA.Cell,3f) &&
            GenSight.LineOfSight(job.targetA.Cell,ally.Position,pawn.Map);
        internal void Shield(ref DamageInfo hit,out bool absorbed)
        {
            float taken=Mathf.Clamp(hit.Amount,0,barrier);barrier-=taken;hit.SetAmount(hit.Amount-taken);absorbed=hit.Amount<=0;
        }
        private bool InPlace()=>Bound(pawn,SkillOwner) && Location(pawn,job.targetA.Cell) && pawn.Position==job.targetB.Cell &&
            (!begun || mapId==pawn.Map.uniqueID) && PsionicMartialPath.Read(PsionicMartialPath.Path(SkillOwner))?.Crucible==true;
        public override bool TryMakePreToilReservations(bool errorOnFailed)=>true;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun,"begun");Scribe_Values.Look(ref paid,"paid");Scribe_Values.Look(ref finished,"finished");
            Scribe_Values.Look(ref pulses,"pulses");Scribe_Values.Look(ref mapId,"map");Scribe_Values.Look(ref barrier,"barrier");
            Scribe_Collections.Look(ref enemies,"enemies",LookMode.Reference);Scribe_Collections.Look(ref allies,"allies",LookMode.Reference);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){enemies??=new();allies??=new();}
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_=>Finish());this.FailOn(()=>!InPlace());
            yield return Toils_General.Do(()=>{if(!ReadyFrom(pawn,SkillOwner)){EndJobWith(JobCondition.Incompletable);return;}mapId=pawn.Map.uniqueID;begun=true;});
            yield return Toils_General.Wait(Mathf.Max(1,Mathf.RoundToInt(300*Compatibility.AbilityTiming.WarmupMultiplier)),TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(()=>{if(!paid&&!Commit())EndJobWith(JobCondition.Incompletable);});
            for(int i=0;i<4;i++)
            {
                int index=i;yield return Toils_General.Wait(240);
                yield return Toils_General.Do(()=>Pulse(index));
            }
        }
        private static bool Enemy(Pawn p,Pawn c)=>p?.Spawned==true && !p.Dead && !p.Downed && !p.IsPrisoner && p.HostileTo(c);
        private bool Commit()
        {
            if(!begun || paid || finished || !InPlace() || !ReadyFrom(pawn,SkillOwner))return false;
            var center=job.targetA.Cell;
            var visible=GenRadial.RadialDistinctThingsAround(center,pawn.Map,3,true).OfType<Pawn>().Where(p=>GenSight.LineOfSight(center,p.Position,pawn.Map))
                .OrderBy(p=>p.Position.DistanceToSquared(center)).ThenBy(p=>p.thingIDNumber).ToList();
            var ward=DefDatabase<HediffDef>.GetNamedSilentFail("TM_PsionicCrucibleWard");
            if(ward==null)return false;
            enemies=visible.Where(p=>Enemy(p,pawn)).Take(3).ToList();
            allies=visible.Where(p=>!p.Dead && p.Faction==pawn.Faction && !p.IsPrisoner && !p.health.hediffSet.HasHediff(ward)).Take(3).ToList();
            if(enemies.Count+allies.Count==0)return false;
            Charge(.60f,.30f);barrier=60;paid=true;Cooldown(120000);
            foreach(var a in allies){var h=(Hediff_CrucibleWard)HediffMaker.MakeHediff(ward,a);h.source=pawn;a.health.AddHediff(h);}
            return true;
        }
        private void Charge(float psi,float stamina)
        {
            if(PsionicMartialPath.Energy(SkillOwner) is Hediff energy)energy.Severity-=PsionicMartialPath.Capacity(SkillOwner)*psi;
            var s=SkillOwner.GetCompAbilityUserMight()?.Stamina;if(s!=null)s.CurLevel-=s.MaxLevel*stamina;
        }
        private void Cooldown(int ticks)
        {
            var d=PsionicMartialPath.Read(PsionicMartialPath.Path(SkillOwner));if(d==null)return;
            int duration=Compatibility.AbilityTiming.Cooldown(ticks);
            d.CrucibleReadyAt=Find.TickManager.TicksGame+duration;
            var ability=SkillOwner.GetCompAbilityUserMight()?.AbilityData.Powers.OfType<PsionicCrucibleAbility>().FirstOrDefault();
            if(ability!=null)ability.CooldownTicksLeft=duration;
        }
        private void Pulse(int index)
        {
            if(!paid || finished || pulses!=index || !InPlace())return;
            pulses++;
            var cell=job.targetA.Cell;
            foreach(var target in enemies)
            {
                if(!Enemy(target,pawn) || target.Map!=pawn.Map || !target.Position.InHorDistOf(cell,3) ||
                    !GenSight.LineOfSight(cell,target.Position,pawn.Map))continue;
                target.TakeDamage(new DamageInfo(TMDamageDefOf.DamageDefOf.TM_PsionicInjury,14,0,-1,pawn));
                if(index==0 && Enemy(target,pawn) && target.Map==pawn.Map)Push(target,cell);
            }
        }
        private void Push(Pawn target,IntVec3 center)
        {
            var away=target.Position-center;if(away==IntVec3.Zero)away=target.Position-pawn.Position;if(away==IntVec3.Zero)return;
            var step=System.Math.Abs(away.x)>=System.Math.Abs(away.z)?new IntVec3(System.Math.Sign(away.x),0,0):new IntVec3(0,0,System.Math.Sign(away.z));
            var end=target.Position;
            for(int i=0;i<2;i++)
            {
                var n=end+step;
                if(!n.InBounds(target.Map) || !n.Standable(target.Map) || n.GetEdifice(target.Map)!=null || n.GetFirstPawn(target.Map)!=null ||
                    !GenSight.LineOfSight(end,n,target.Map))break;
                end=n;
            }
            if(end!=target.Position){target.Position=end;target.Notify_Teleported();}
        }
        private void Finish()
        {
            if(finished)return;finished=true;
            if(begun && !paid){Charge(.06f,.03f);Cooldown(6000);}
            if(paid)
            {
                var d=PsionicMartialPath.Read(PsionicMartialPath.Path(SkillOwner));
                if(d!=null)d.DisruptedUntil=Find.TickManager.TicksGame+600;
            }
            foreach(var a in allies)
                if(a?.health?.hediffSet?.hediffs.FirstOrDefault(h=>h is Hediff_CrucibleWard w && w.source==pawn) is Hediff found)a.health.RemoveHediff(found);
        }
    }
}
