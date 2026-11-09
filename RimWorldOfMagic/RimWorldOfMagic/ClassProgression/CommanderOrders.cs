using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using TorannMagic.AutoCast;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal enum CommanderOrder { Crew, Rally, Evacuate, Emergency, March }

    public sealed class CommanderOrderEffect : Hediff
    {
        internal Pawn Commander, Threat;
        internal Pawn DisciplineOwner;
        // Old native orders had no separate owner; never bind one to a later occupant.
        internal Pawn Source => DisciplineOwner ?? Commander;
        internal CommanderOrder Order;
        internal IntVec3 Center, Start, LastCell;
        internal int Serial, Until, JobId, Rank = 1;
        internal float Guard;
        internal bool Credited, Notified, DisasterRepair;
        internal int InitialHP;

        public override HediffStage CurStage => def.stages[Order switch {
            CommanderOrder.Crew => Mathf.Clamp(Rank-1,0,2),
            CommanderOrder.Rally => 3, CommanderOrder.Evacuate => 4, CommanderOrder.Emergency => 5, _ => 6
        }];

        internal bool Live
        {
            get
            {
                if(Find.TickManager.TicksGame>=Until || !CommanderOrders.Ally(Commander,pawn) ||
                    CommanderMartialPath.Owner(Commander)!=Source || CommanderMartialPath.Path(Source)==null) return false;
                if(Order==CommanderOrder.March) return true;
                if(Order==CommanderOrder.Crew) return pawn.CurJob?.loadID==JobId;
                if(Order==CommanderOrder.Rally) return pawn.Position.InHorDistOf(Center,3);
                if (Order == CommanderOrder.Emergency && !CommanderOrders.EmergencyJob(pawn)) return false;
                return Commander.jobs?.curDriver is JobDriver_CommanderOperation driver && driver.Supports(Serial) &&
                    GenSight.LineOfSight(Commander.Position,pawn.Position,pawn.Map);
            }
        }
        public override bool ShouldRemove => !Live;
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (Credited || Threat==null || !Live || pawn.Position==LastCell) return;
            if (!LastCell.AdjacentTo8WayOrInside(pawn.Position) || pawn.pather==null ||
                Find.TickManager.TicksGame-pawn.pather.LastMovedTick>delta+1 ||
                !TM_AutocastCombat.IsActiveOpponent(pawn,Threat))
            { Threat=null; return; }
            LastCell=pawn.Position;
            if ((pawn.Position-Start).LengthHorizontalSquared>=4 &&
                pawn.Position.DistanceToSquared(Threat.Position)>Start.DistanceToSquared(Threat.Position) &&
                GenSight.LineOfSight(pawn.Position,Threat.Position,pawn.Map))
            {
                CommanderMartialPath.Escape(Source,pawn);
                Credited=true;
            }
        }
        public override void PostAdd(DamageInfo? dinfo) {base.PostAdd(dinfo); CommanderOrderEffects.Index(this);}
        public override void PostRemoved()
        {
            CommanderOrderEffects.Drop(this);
            if(!Notified && (Order==CommanderOrder.Evacuate || Order==CommanderOrder.Emergency))
            {
                Notified=true;
                var fatigue=DefDatabase<ThoughtDef>.GetNamedSilentFail("TM_CommanderOperationFatigue");
                if(fatigue!=null && pawn?.Dead==false) pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(fatigue);
            }
            base.PostRemoved();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Commander,"commander"); Scribe_References.Look(ref Threat,"threat");
            Scribe_References.Look(ref DisciplineOwner,"disciplineOwner");
            Scribe_Values.Look(ref Order,"order"); Scribe_Values.Look(ref Center,"center"); Scribe_Values.Look(ref Start,"start"); Scribe_Values.Look(ref LastCell,"lastCell");
            Scribe_Values.Look(ref Serial,"serial"); Scribe_Values.Look(ref Until,"until");
            Scribe_Values.Look(ref JobId,"jobId"); Scribe_Values.Look(ref Rank,"rank",1); Scribe_Values.Look(ref Guard,"guard");
            Scribe_Values.Look(ref Credited,"credited"); Scribe_Values.Look(ref Notified,"notified");
            Scribe_Values.Look(ref InitialHP,"initialHP"); Scribe_Values.Look(ref DisasterRepair,"disasterRepair");
            if(Scribe.mode==LoadSaveMode.PostLoadInit && pawn!=null) CommanderOrderEffects.Index(this);
        }
    }

    internal static class CommanderOrderEffects
    {
        private static readonly ConditionalWeakTable<Pawn,CommanderOrderEffect> current=new();
        internal static void ClearExpired(Pawn pawn)
        {
            if (current.TryGetValue(pawn,out var old) && !old.Live && pawn.health.hediffSet.hediffs.Contains(old))
                pawn.health.RemoveHediff(old);
        }
        internal static void Index(CommanderOrderEffect order)
        {
            current.Remove(order.pawn);
            current.Add(order.pawn,order);
        }
        internal static void Drop(CommanderOrderEffect order)
        {
            if(current.TryGetValue(order.pawn,out var live) && live==order) current.Remove(order.pawn);
        }
        internal static CommanderOrderEffect Current(Pawn pawn) =>
            pawn!=null && current.TryGetValue(pawn,out var order) && order.Live ? order : null;

        internal static void Shield(Pawn pawn,ref DamageInfo hit,out bool absorbed)
        {
            absorbed=false;
            if(pawn==null || !current.TryGetValue(pawn,out var order) || order.Guard<=0f || hit.Amount<=0f ||
                hit.Instigator?.HostileTo(pawn)!=true || !order.Live) return;
            float used=Mathf.Min(hit.Amount,order.Guard);
            order.Guard-=used;
            hit.SetAmount(hit.Amount-used);
            absorbed=hit.Amount<=0f;
        }
    }

    internal static class CommanderOrders
    {
        internal static bool Ally(Pawn commander,Pawn recipient) => commander?.Spawned==true && recipient?.Spawned==true &&
            commander!=recipient && recipient.Map==commander.Map && !commander.Dead && !commander.Downed &&
            !recipient.Dead && !recipient.Downed && !recipient.IsPrisoner && recipient.RaceProps.Humanlike &&
            commander.Faction!=null && recipient.Faction==commander.Faction;

        internal static bool SupplyJob(JobDriver driver) => driver is JobDriver_HaulToCell || driver is JobDriver_HaulToContainer;
        internal static bool EmergencyJob(Pawn worker) => worker.CurJob?.def==JobDefOf.Rescue ||
            worker.jobs?.curDriver is JobDriver_TendPatient || worker.jobs?.curDriver is JobDriver_Repair;
        internal static bool Eligible(Pawn commander,Pawn ally,CommanderOrder order)
        {
            if(!Ally(commander,ally) || CommanderOrderEffects.Current(ally)!=null ||
                !GenSight.LineOfSight(commander.Position,ally.Position,commander.Map)) return false;
            return order switch {
                CommanderOrder.Crew => ally.CurJob?.workGiverDef!=null || SupplyJob(ally.jobs?.curDriver) || EmergencyJob(ally),
                CommanderOrder.Emergency => EmergencyJob(ally),
                _ => true
            };
        }

        internal static bool Accept(Pawn commander,Pawn ally)
        {
            var source=CommanderMartialPath.Owner(commander);
            int social=source?.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            float chance=CommanderMartialPath.Acceptance(source,Mathf.Clamp(.1f*social,.1f,.95f));
            return Rand.Chance(chance);
        }
        internal static void AcceptedMove(Pawn commander,Pawn ally)
        {
            var source=CommanderMartialPath.Owner(commander);
            var path=CommanderMartialPath.Path(source);
            if(path?.Stage!=InitiationStage.Adept || !Ally(commander,ally) || CommanderOrderEffects.Current(ally)!=null) return;
            var threat=NearestThreat(ally);
            if(threat==null) return;
            var def=DefDatabase<HediffDef>.GetNamedSilentFail("TM_CommanderAssignment");
            if(def==null) return;
            var order=(CommanderOrderEffect)HediffMaker.MakeHediff(def,ally);
            order.Commander=commander;order.Order=CommanderOrder.March;order.Start=order.LastCell=ally.Position;
            order.DisciplineOwner=source;
            order.Threat=threat;order.Until=Find.TickManager.TicksGame+480;
            CommanderOrderEffects.ClearExpired(ally);
            ally.health.AddHediff(order);
        }
        private static Pawn NearestThreat(Pawn ally) =>
            GenRadial.RadialDistinctThingsAround(ally.Position,ally.Map,18f,true).OfType<Pawn>()
                .Where(t=>TM_AutocastCombat.IsActiveOpponent(ally,t) && GenSight.LineOfSight(ally.Position,t.Position,ally.Map))
                .OrderBy(t=>t.Position.DistanceToSquared(ally.Position)).FirstOrDefault();

        internal static int Issue(Pawn commander,IntVec3 center,CommanderOrder order,int serial)
        {
            int now=Find.TickManager.TicksGame;
            var source=CommanderMartialPath.Owner(commander);
            var path=CommanderMartialPath.Path(source);
            if(path==null || !center.InBounds(commander.Map)) return 0;
            var data=CommanderMartialPath.Data(path);
            var def=DefDatabase<HediffDef>.GetNamedSilentFail("TM_CommanderAssignment");
            if(def==null) return 0;
            int limit = order==CommanderOrder.Crew ? 3 : order==CommanderOrder.Rally ? data.RallyRank+1 : 4;
            // A work-speed buff on a colonist already doing that exact job is
            // never refused by the worker; the wider radius covers a workshop
            // area instead of the three cells around one clicked tile.
            float radius=order==CommanderOrder.Crew?6:order==CommanderOrder.Rally?3:5;
            var candidates=GenRadial.RadialDistinctThingsAround(center,commander.Map,radius,true).OfType<Pawn>()
                .Where(p=>Eligible(commander,p,order)).OrderBy(p=>p.Position.DistanceToSquared(center)).ThenBy(p=>p.thingIDNumber).Take(limit).ToList();
            int accepted=0;
            foreach(var ally in candidates)
            {
                if(order!=CommanderOrder.Crew && !Accept(commander,ally)) continue;
                var effect=(CommanderOrderEffect)HediffMaker.MakeHediff(def,ally);
                effect.Commander=commander; effect.Order=order; effect.Serial=serial; effect.Center=center; effect.Start=effect.LastCell=ally.Position;
                effect.DisciplineOwner=source;
                effect.JobId=ally.CurJob?.loadID??-1;
                effect.InitialHP=ally.CurJob?.targetA.Thing?.HitPoints??0;
                effect.DisasterRepair=CommanderWorkReceipts.WasDisaster(ally.CurJob?.targetA.Thing);
                effect.Rank=data.CrewRank;
                effect.Until=now+(order==CommanderOrder.Crew?600:order==CommanderOrder.Rally?480:1200);
                effect.Guard=order==CommanderOrder.Rally?15:order==CommanderOrder.Evacuate?25:0;
                if(path.Stage==InitiationStage.Adept)
                    effect.Threat=NearestThreat(ally);
                CommanderOrderEffects.ClearExpired(ally);
                ally.health.AddHediff(effect);
                if(ally.needs?.mood!=null && Rand.Chance(Mathf.Clamp(TM_Calc.GetRelationsFactor(commander,ally),.1f,.9f)))
                    ally.needs?.mood?.thoughts?.memories?.TryGainMemory(TorannMagicDefOf.TM_TakingOrdersTD);
                accepted++;
            }
            return accepted;
        }
    }

    public sealed class Verb_CommanderCoordination : Verb_UseAbility
    {
        internal const string CrewOrder="TM_SpiritCommanderCrew", RallyOrder="TM_SpiritCommanderRally";
        internal static bool Handles(string job) => job==CrewOrder || job==RallyOrder;
        internal static Verb_CommanderCoordination For(Pawn body,MightAbility power) =>
            new Verb_CommanderCoordination{caster=body,Ability=power,verbProps=power.mightDef.MainVerb};
        internal static void Choose(Pawn body,MightAbility power,string name)
        {
            var verb=For(body,power);var map=body.Map;
            bool Valid(LocalTargetInfo target) => body.Map==map && SpiritHostedMartial.Find(body,power.Pawn,name)==power &&
                SpiritHostedMartial.Ready(body,power,name) && verb.CanHitTargetFrom(body.Position,target);
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,target=> {
                if(Valid(target))body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(name),power.Pawn,target));
            },highlightAction:verb.DrawHighlight,targetValidator:Valid,caster:body);
        }
        public override bool CanHitTargetFrom(IntVec3 root,LocalTargetInfo target)
        {
            var pawn=CasterPawn;
            var source=Ability?.Pawn??pawn;
            return pawn?.Spawned==true && !pawn.Dead && !pawn.Downed && CommanderMartialPath.Owner(pawn)==source && CommanderMartialPath.Path(source)!=null &&
                target.IsValid && target.Cell.InBounds(pawn.Map) && !target.Cell.Fogged(pawn.Map) &&
                root.InHorDistOf(target.Cell,EffectiveRange) && GenSight.LineOfSight(root,target.Cell,pawn.Map);
        }
        protected override bool TryCastShot() => Issue(currentTarget);
        internal bool Issue(LocalTargetInfo target)
        {
            if(!CanHitTargetFrom(CasterPawn.Position,target)) return false;
            var path=CommanderMartialPath.Path(Ability?.Pawn??CasterPawn);
            var name=Ability.Def.defName;
            if (name!=CommanderMartialPath.Rally && name!=CommanderMartialPath.Crew) return false;
            bool rally=name==CommanderMartialPath.Rally;
            if(rally && path.Stage<InitiationStage.Master) return false;
            int serial=++CommanderMartialPath.Data(path).Serial;
            // An issued but refused order still consumes its normal stamina and cooldown.
            var order=rally?CommanderOrder.Rally:CommanderOrder.Crew;
            int accepted=CommanderOrders.Issue(CasterPawn,target.Cell,order,serial);
            // A zero-accepted cast silently burning the cooldown read as "the
            // ability does nothing"; tell the player why nothing happened.
            if(accepted==0 && CasterPawn.Faction==Faction.OfPlayer && LanguageDatabase.activeLanguage != null)
            {
                // The rejection text resolves through the active language
                // worker; a headless fixture without a language pack skips it.
                Messages.Message("TM_CommanderNoTakers".Translate(order==CommanderOrder.Crew?"Work Crew":"Rally"),
                    CasterPawn,MessageTypeDefOf.RejectInput,false);
            }
            return true;
        }
    }
}
