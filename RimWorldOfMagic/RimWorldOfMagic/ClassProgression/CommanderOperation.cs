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
    public sealed class CommanderOperationAbility : MightAbility
    {
        public CommanderOperationAbility() { }
        public CommanderOperationAbility(CompAbilityUser owner) : base(owner) { }
        public CommanderOperationAbility(AbilityData data) : base(data) { }
        public CommanderOperationAbility(Pawn caster,AbilityUser.AbilityDef def) : base(caster,def) { }
        public override Job GetJob(AbilityContext context,LocalTargetInfo target) => MakeJobFor(Pawn,target,context);
        internal Job MakeJobFor(Pawn body,LocalTargetInfo target,AbilityContext context)
        {
            var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_CommanderOperationJob"),target,body.Position,Pawn);
            job.count=Def.defName==CommanderMartialPath.Evac ? 1 : 2;
            job.playerForced=context==AbilityContext.Player;
            return job;
        }
        public override bool CanCastPowerCheck(AbilityContext context,out string reason)
        {
            if(!base.CanCastPowerCheck(context,out reason)) return false;
            if(JobDriver_CommanderOperation.Ready(Pawn)) return true;
            reason="TM_CommanderOperationUnavailable".Translate();return false;
        }
    }
    public sealed class JobDriver_CommanderOperation : JobDriver
    {
        private bool begun,paid,finished;
        private int serial,mapId;
        // Native pre-upgrade jobs have no targetC; their owner remains the actor.
        private Pawn Source => job.targetC.Pawn ?? pawn;
        internal bool Supports(int token) => paid && !finished && serial==token;
        internal static bool Ready(Pawn p)
            => ReadyFrom(p,CommanderMartialPath.Owner(p));
        internal static bool ReadyFrom(Pawn p,Pawn source)
        {
            if(source==null || CommanderMartialPath.Owner(p)!=source) return false;
            var path=CommanderMartialPath.Path(source);
            var data=CommanderMartialPath.Existing(path);
            if(path?.Stage!=InitiationStage.Ascended || data?.Operation!=true || p?.Spawned!=true || p.Dead || p.Downed ||
                Find.TickManager.TicksGame<data.OperationUntil) return false;
            var stam=source.GetCompAbilityUserMight()?.Stamina;
            return stam?.MaxLevel>0f && stam.CurLevel>=.6f*stam.MaxLevel;
        }
        private bool PositionValid()
        {
            if(CommanderMartialPath.Owner(pawn)!=Source || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.InMentalState || pawn.IsPrisoner || pawn.stances?.stunner?.Stunned==true || pawn.Position!=job.targetB.Cell ||
                (begun && mapId!=pawn.Map.uniqueID) || !job.targetA.Cell.InBounds(pawn.Map) ||
                !pawn.Position.InHorDistOf(job.targetA.Cell,14) || job.targetA.Cell.Fogged(pawn.Map)) return false;
            return GenSight.LineOfSight(pawn.Position,job.targetA.Cell,pawn.Map);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => ReadyFrom(pawn,Source) && PositionValid();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun,"commanderBegun"); Scribe_Values.Look(ref paid,"commanderPaid");
            Scribe_Values.Look(ref finished,"commanderFinished"); Scribe_Values.Look(ref serial,"commanderToken");
            Scribe_Values.Look(ref mapId,"commanderMap");
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_=>Finish());
            this.FailOn(()=>!PositionValid() || (!paid && !ReadyFrom(pawn,Source)) || CommanderMartialPath.Existing(CommanderMartialPath.Path(Source))?.Operation!=true);
            yield return Toils_General.Do(()=>{begun=true;mapId=pawn.Map.uniqueID;});
            yield return Toils_General.Wait(Mathf.Max(1,Mathf.RoundToInt(300f*AbilityTiming.WarmupMultiplier)),TargetIndex.A)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(()=>
            {
                if(paid) return;
                if(!ReadyFrom(pawn,Source) || !PositionValid()) {EndJobWith(JobCondition.Incompletable);return;}
                var d=CommanderMartialPath.Data(CommanderMartialPath.Path(Source));
                Pay(.6f,120000);
                paid=true;
                serial=++d.Serial;
                CommanderOrders.Issue(pawn,job.targetA.Cell,job.count==1?CommanderOrder.Evacuate:CommanderOrder.Emergency,serial);
            });
            yield return Toils_General.Wait(1200,TargetIndex.A);
        }
        private void Finish()
        {
            if(finished) return;
            finished=true;
            if(begun && !paid) Pay(.06f,6000);
        }
        private void Pay(float fraction,int duration)
        {
            var d=CommanderMartialPath.Existing(CommanderMartialPath.Path(Source));
            var might=Source.GetCompAbilityUserMight();
            if(d==null || might?.Stamina==null) return;
            might.Stamina.CurLevel=Mathf.Max(0f,might.Stamina.CurLevel-might.Stamina.MaxLevel*fraction);
            int cooldown=AbilityTiming.Cooldown(duration);
            d.OperationUntil=Find.TickManager.TicksGame+cooldown;
            foreach(var owner in might.AbilityData.Powers.OfType<CommanderOperationAbility>()) owner.CooldownTicksLeft=cooldown;
        }
    }
}
