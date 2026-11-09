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
    public sealed class Verb_MonkPalmSeal : Verb_UseAbility
    {
        internal const string HostedOrder="TM_SpiritPalmSeal";
        internal static bool Target(Pawn actor,Pawn enemy) =>
            actor?.Spawned==true && !actor.Dead && !actor.Downed && actor.equipment?.Primary==null &&
            MonkMartialPath.Path(MonkMartialPath.Owner(actor))!=null && enemy?.Spawned==true && !enemy.Dead && !enemy.Downed && !enemy.IsPrisoner &&
            enemy.Map==actor.Map && enemy.HostileTo(actor) && actor.CanReachImmediate(enemy,PathEndMode.Touch);

        public override bool CanHitTargetFrom(IntVec3 root,LocalTargetInfo target) =>
            Target(CasterPawn,target.Pawn) && root.AdjacentTo8WayOrInside(target.Cell);

        protected override bool TryCastShot() => Strike(CasterPawn,currentTarget.Pawn);
        internal static bool Strike(Pawn actor,Pawn enemy)
        {
            if (!Target(actor,enemy)) return false;
            var warmup=enemy.stances?.curStance as Stance_Warmup;
            var info=new DamageInfo(TMDamageDefOf.DamageDefOf.TM_ChiBurn,10+6*MonkMartialPath.Rank(MonkMartialPath.Owner(actor)),instigator:actor);
            var result=enemy.TakeDamage(info);
            if(result.totalDamageDealt>0 && !enemy.Dead && enemy.Spawned && enemy.stances?.stunner!=null)
            {
                enemy.stances.stunner.StunFor(60,actor);
                if (warmup != null)
                {
                    if (enemy.stances.curStance == warmup) enemy.stances.CancelBusyStanceSoft();
                    if (enemy.stances.curStance != warmup) MonkMartialPath.Interrupted(actor);
                }
            }
            return true;
        }
    }

    public sealed class MonkMeridianAbility : MightAbility
    {
        public MonkMeridianAbility() {}
        public MonkMeridianAbility(CompAbilityUser owner):base(owner) {}
        public MonkMeridianAbility(AbilityData data):base(data) {}
        public MonkMeridianAbility(Pawn pawn,AbilityUser.AbilityDef def):base(pawn,def) {}
        public override Job GetJob(AbilityContext context,LocalTargetInfo target)
        {
            var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_MonkMeridianCast"),Pawn.Position);
            job.playerForced=context==AbilityContext.Player;
            return job;
        }
        public override bool CanCastPowerCheck(AbilityContext context,out string reason)
        {
            if (!base.CanCastPowerCheck(context,out reason)) return false;
            if (JobDriver_MonkMeridian.Ready(Pawn)) return true;
            reason="TM_MonkMeridianUnavailable".Translate();
            return false;
        }
    }

    public sealed class Verb_MonkMeridian : Verb_UseAbility
    {
        public override bool CanHitTargetFrom(IntVec3 root,LocalTargetInfo target) =>
            CasterPawn != null && target.Thing==CasterPawn && JobDriver_MonkMeridian.Ready(CasterPawn);
        protected override bool TryCastShot()=>false;
    }

    public sealed class JobDriver_MonkMeridian : JobDriver
    {
        internal const string HostedOrder="TM_SpiritMonkMeridian";
        private Pawn SkillOwner => job?.def.defName==HostedOrder ? job.targetB.Pawn : pawn;
        private bool ValidOwner => SkillOwner!=null && MonkMartialPath.Owner(pawn)==SkillOwner;
        private bool begun,paid;
        private int pulses,mapId;
        private List<Pawn> targets;

        internal static bool Ready(Pawn monk)
        {
            var owner=MonkMartialPath.Owner(monk);
            var path=MonkMartialPath.Path(owner);
            var d=MonkMartialPath.Data(path);
            var chi=MonkMartialPath.Chi(owner);
            return monk?.Spawned==true && !monk.Dead && !monk.Downed && monk.equipment?.Primary==null &&
                !monk.WorkTagIsDisabled(WorkTags.Violent) && path?.Stage==InitiationStage.Ascended && d?.Meridian==true &&
                d.MeridianReadyAt<=Find.TickManager.TicksGame && chi!=null && MonkMartialPath.Capacity(chi)>0 &&
                chi.Severity/MonkMartialPath.Capacity(chi)>=.6f;
        }
        private bool Valid() =>
            ValidOwner && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.equipment?.Primary==null && pawn.Position==job.targetA.Cell &&
            pawn.stances?.stunner?.Stunned!=true && (!begun || pawn.Map.uniqueID==mapId) &&
            MonkMartialPath.Data(MonkMartialPath.Path(SkillOwner))?.Meridian==true;

        private bool InRange(Pawn p) => p?.Spawned==true && p.Map==pawn.Map && !p.Dead && !p.Downed && !p.IsPrisoner &&
            p.HostileTo(pawn) && p.Position.InHorDistOf(pawn.Position,5) && GenSight.LineOfSight(pawn.Position,p.Position,pawn.Map);
        private IEnumerable<Pawn> Nearby() => GenRadial.RadialDistinctThingsAround(pawn.Position,pawn.Map,5f,true).OfType<Pawn>().Where(InRange);
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Valid() && Ready(pawn) && Nearby().Any();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun,"monkMeridianBegun");
            Scribe_Values.Look(ref paid,"monkMeridianPaid");
            Scribe_Values.Look(ref pulses,"monkMeridianPulses");
            Scribe_Values.Look(ref mapId,"monkMeridianMap");
            Scribe_Collections.Look(ref targets,"monkMeridianTargets",LookMode.Reference);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_=>Finish());
            this.FailOn(()=>!Valid() || (!paid && !Ready(pawn)));
            yield return Toils_General.Do(()=>{begun=true;mapId=pawn.Map.uniqueID;});
            yield return Toils_General.Wait(Mathf.Max(1,Mathf.RoundToInt(300*AbilityTiming.WarmupMultiplier))).WithProgressBarToilDelay(TargetIndex.A);
            for(int i=0;i<3;i++)
            {
                int expected=i;
                yield return Toils_General.Do(()=>Pulse(expected));
                yield return Toils_General.Wait(240);
            }
        }
        private void Pulse(int expected)
        {
            if (pulses>expected) return;
            if (pulses!=expected || !Valid() || (!paid && !Ready(pawn))) { EndJobWith(JobCondition.Incompletable); return; }
            if (!paid)
            {
                targets=Nearby().OrderBy(p=>p.Position.DistanceToSquared(pawn.Position)).ThenBy(p=>p.thingIDNumber).Take(4).ToList();
                if (targets.Count==0) {EndJobWith(JobCondition.Incompletable);return;}
                Pay(.60f,120000);
            }
            pulses++;
            foreach(var enemy in targets.Where(InRange))
            {
                var result=enemy.TakeDamage(new DamageInfo(TMDamageDefOf.DamageDefOf.TM_ChiBurn,12f,instigator:pawn));
                if(expected!=0 || enemy.Dead || result.totalDamageDealt<=0) continue;
                enemy.stances?.CancelBusyStanceSoft();
                enemy.stances?.stunner?.StunFor(120,pawn);
                // A hybrid loses only its first available resource.
                Need energy=enemy.needs?.TryGetNeed<Need_Mana>() ?? (Need)enemy.needs?.TryGetNeed<Need_Stamina>();
                if (energy?.MaxLevel>0f) energy.CurLevel=Mathf.Max(0,energy.CurLevel-.15f*energy.MaxLevel);
            }
        }
        private void Finish()
        {
            if(begun && !paid) Pay(.06f,6000);
            begun=false;
        }
        private void Pay(float fraction,int cooldown)
        {
            if(paid) return;
            var d=MonkMartialPath.Data(MonkMartialPath.Path(SkillOwner));
            var chi=MonkMartialPath.Chi(SkillOwner);
            if(d==null || chi==null) return;
            paid=true;
            chi.Severity=Mathf.Max(chi.def.minSeverity,chi.Severity-fraction*MonkMartialPath.Capacity(chi));
            int duration=AbilityTiming.Cooldown(cooldown);
            d.MeridianReadyAt=Find.TickManager.TicksGame+duration;
            var ability=SkillOwner.GetCompAbilityUserMight()?.AbilityData.Powers.OfType<MonkMeridianAbility>().FirstOrDefault();
            if(ability!=null) ability.CooldownTicksLeft=duration;
        }
    }
}
