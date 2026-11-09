using System.Reflection;
using TorannMagic.ClassProgression;
using TorannMagic;
using Verse;
using Verse.AI;
using RimWorld;
using AbilityUser;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private sealed class CommanderAuraSave : IExposable
    {
        public Hediff Aura=null!;
        public void ExposeData() => Scribe_Deep.Look(ref Aura,"aura");
    }
    private static void RunCommanderAuraJob(RuntimeFixture r,Pawn body,Pawn soul,MightAbility ability,string name,HediffDef auraDef)
    {
        var def=ability.mightDef;var source=ability.MightUser;
        var oldVerb=def.MainVerb;var oldCosts=(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost);
        var settings=TorannMagic.ModOptions.Settings.Instance;var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
        var oldEvents=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
        var oldJobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        try {
            TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
            TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="HostedAuraActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
            var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
            using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
            using var endJob=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
            def.consumeEnergy=false;def.staminaCost=.6f;def.manaCost=def.chiCost=def.bloodCost=0;
            def.MainVerb=new VerbProperties_Ability{verbClass=typeof(Verb_CommanderAuras),abilityDef=def,warmupTime=1,SecondsToRecharge=40};
            source.maxSP=1;soul.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;source.Stamina.CurLevelPercentage=1;
            Check.Equal(true,(bool)Production("SpiritHostedMartial").GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[body,ability,name])!,"native non-consuming aura is available through host");
            var jobDef=new JobDef{defName=name,driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
            var order=JobMaker.MakeJob(jobDef,soul);
            var driver=new JobDriver_SpiritHostedMartial{pawn=body,job=order};
            Set(body.jobs,"curJob",order);Set(body.jobs,"curDriver",driver);
            Toil[] Toils(JobDriver d)=>((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(d,null)!).ToArray();
            float before=source.Stamina.CurLevel;
            Toils(driver)[0].initAction();Check.Equal(before,source.Stamina.CurLevel,"aura preparation does not charge");
            Check.Equal(false,soul.health.hediffSet.HasHediff(auraDef),"aura preparation has no effect");
            var resumed=RoundTripSummon(r,"commander-aura-warmup-"+name,new MonkJobSave{Body=body,Order=order,Driver=driver},body,soul);
            Set(body.jobs,"curJob",resumed.Order);Set(body.jobs,"curDriver",resumed.Driver);
            Toils(resumed.Driver)[1].initAction();
            Check.Equal(true,soul.health.hediffSet.HasHediff(auraDef),"saved warmup completes native aura toggle");
            Check.Equal(before,source.Stamina.CurLevel,"native aura toggle reserves upkeep without spending activation stamina");
            Check.Equal(true,ability.CooldownTicksLeft>0,"aura cooldown belongs to soul");
            float paid=source.Stamina.CurLevel;int xp=source.MightUserXP,cd=ability.CooldownTicksLeft;
            var completed=RoundTripSummon(r,"commander-aura-completed-"+name,resumed,body,soul);
            Set(body.jobs,"curJob",completed.Order);Set(body.jobs,"curDriver",completed.Driver);
            Toils(completed.Driver)[1].initAction();
            Check.Equal(true,soul.health.hediffSet.HasHediff(auraDef),"completed job cannot toggle aura off after load");
            Check.Equal(paid,source.Stamina.CurLevel,"completed aura job does not charge twice");
            Check.Equal(xp,source.MightUserXP,"completed aura job does not grant repeated XP");
            Check.Equal(cd,ability.CooldownTicksLeft,"completed aura job does not reset cooldown");
        } finally {
            def.MainVerb=oldVerb;(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost)=oldCosts;
            TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
            DefDatabase<HistoryEventDef>.Clear();foreach(var d in oldEvents)DefDatabase<HistoryEventDef>.Add(d);
            DefDatabase<JobDef>.Clear();foreach(var d in oldJobs)DefDatabase<JobDef>.Add(d);
        }
    }
    internal static JobDriver_CommanderOperation CommanderRoundTrip(RuntimeFixture r,Pawn body,Pawn source,JobDriver_CommanderOperation driver)
    {
        var saved=RoundTripSummon(r,"commander-owned-job",new MonkJobSave{Body=body,Order=driver.job,Driver=driver},body,source);
        Check.Same(source,saved.Order.targetC.Pawn,"operation source survives actual job serialization");
        Check.Same(body,saved.Driver.pawn,"physical operator survives job serialization");
        return (JobDriver_CommanderOperation)saved.Driver;
    }
    private static IEnumerable<TestCase> HostedCommanderCases()
    {
        foreach(string family in new[]{"Provisioner","TaskMaster","Commander"})
            foreach(int rank in new[]{2,3})
            yield return new("CommanderProjection.AuraRoute."+family+".Rank"+rank,r=>SpiritRebirthRitualTransactionTests.Run(r,c=> {
                using var fixture=new Fixture(r,c);
                using var definitions=CommanderCareerTests.Setup(r);
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_"+family+"Aura")!.GetValue(null)!;
                var ability=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(ability);source.AbilityData.AllPowers=null;
                string job="TM_SpiritCommander"+family+"Aura";
                Check.Same(ability,Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,c.Original,job])!,"aura command resolves exact soul-owned ability");
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                source.MightData.GetSkill_Power(def).level=rank;
                source.MightData.GetSkill_Versatility(def).level=rank==3?3:0;
                var auraField=typeof(TorannMagicDefOf).GetField("TM_"+family+"AuraHD")!;
                var effectField=typeof(TorannMagicDefOf).GetField("TM_"+family+"HD"+(family=="Commander"?(rank==3?"_III":"_II"):""))!;
                var oldAura=auraField.GetValue(null);var oldEffect=effectField.GetValue(null);
                var oldRegen=TorannMagicDefOf.TM_EnergyRegenHD;
                var oldDefs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
                var auraType=typeof(HediffComp_CommanderAura).Assembly.GetType("TorannMagic.HediffComp_"+family+"Aura")!;
                var auraDef=new HediffDef{defName="TestHosted"+family+"Aura",hediffClass=typeof(HediffWithComps),initialSeverity=.5f,
                    comps=new List<HediffCompProperties>{new(){compClass=auraType}}};
                var effectDef=new HediffDef{defName="TestHosted"+family+"Effect",hediffClass=family=="Commander"?typeof(Hediff):typeof(HediffWithComps),initialSeverity=0};
                if(family!="Commander") effectDef.comps=new List<HediffCompProperties>{new(){compClass=typeof(HediffComp_CommanderAura).Assembly.GetType("TorannMagic.HediffComp_"+family)!}};
                var regenDef=new HediffDef{defName="TestHostedAuraEnergy",hediffClass=typeof(HediffWithComps),initialSeverity=0,
                    comps=new List<HediffCompProperties>{new HediffCompProperties_SetDuration{duration=10}}};
                try {
                    auraField.SetValue(null,auraDef);effectField.SetValue(null,effectDef);
                    TorannMagicDefOf.TM_EnergyRegenHD=regenDef;DefDatabase<HediffDef>.Add(regenDef);
                    DefDatabase<HediffDef>.Add(auraDef);DefDatabase<HediffDef>.Add(effectDef);
                    CommanderFieldTests.Verify(r,c.Creator,source,false,c.Original,worker=> {
                        var oldMapPawns=c.Map.mapPawns;c.Map.mapPawns=new MapPawns(c.Map);
                        try {
                            Set(c.Map.mapPawns,"pawnsSpawned",new List<Pawn>{worker,c.Creator});
                            RunCommanderAuraJob(r,c.Creator,c.Original,ability,job,auraDef);
                            var aura=(HediffWithComps)c.Original.health.hediffSet.GetFirstHediffOfDef(auraDef);
                            Check.NotNull(aura,"source owns sustained aura");
                            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(auraDef),"no upkeep copied to body");
                            var comp=aura.comps.Single();Set(comp,"nextApplyTick",-1);
                            var visit=Production("SpiritHostedCommander").GetMethod("VisitAuras",BindingFlags.NonPublic|BindingFlags.Static)!;
                            visit.Invoke(null,[c.Original,false]);
                            var effect=worker.health.hediffSet.GetFirstHediffOfDef(effectDef);
                            Check.NotNull(effect,"physical nearby ally gets native ranked aura effect");
                            Check.Equal(family=="Commander"?(rank==3?.65f:.5f):.5f+rank,effect.Severity,"rank belongs to original soul");
                            if(effect is HediffWithComps durationEffect)
                                Check.Equal(3,(int)durationEffect.comps.Single().GetType().GetField("duration")!.GetValue(durationEffect.comps.Single())!,"native recipient duration extends once");
                            if(family=="Provisioner" && rank==3)
                            {
                                var regen=worker.health.hediffSet.GetFirstHediffOfDef(regenDef);
                                Check.NotNull(regen,"rank3 versatility grants energy regeneration");
                                Check.Equal(1.15f,regen.Severity,"native energy regeneration strength");
                                Check.Equal(26,regen.TryGetComp<HediffComp_SetDuration>().duration,"native energy regeneration duration");
                            }
                            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(effectDef),"physical emitter is excluded from its own aura");
                            float xp=source.MightUserXP;float severity=effect.Severity;
                            var saved=RoundTripSummon(r,"commander-aura-"+family,new CommanderAuraSave{Aura=aura},c.Original,c.Creator);
                            c.Original.health.RemoveHediff(aura);c.Original.health.AddHediff(saved.Aura);
                            visit.Invoke(null,[c.Original,false]);
                            Check.Equal(xp,source.MightUserXP,"loaded interval prevents immediate extra XP pulse");
                            Check.Equal(severity,effect.Severity,"loaded interval prevents repeated ally effect");
                            visit.Invoke(null,[c.Original,true]);
                            Check.Equal(false,c.Original.health.hediffSet.HasHediff(auraDef),"exit clears soul aura");
                        } finally { c.Map.mapPawns=oldMapPawns; }
                    });
                } finally {
                    auraField.SetValue(null,oldAura);effectField.SetValue(null,oldEffect);TorannMagicDefOf.TM_EnergyRegenHD=oldRegen;
                    DefDatabase<HediffDef>.Clear();foreach(var hd in oldDefs) DefDatabase<HediffDef>.Add(hd);
                }
            }));
        foreach (var auraType in new[]{typeof(HediffComp_CommanderAura),typeof(HediffComp_ProvisionerAura),typeof(HediffComp_TaskMasterAura)})
            yield return new("CommanderProjection.AuraLifecycle."+auraType.Name,r=>SpiritRebirthRitualTransactionTests.Run(r,c=> {
                using var fixture=new Fixture(r,c);
                using var definitions=CommanderCareerTests.Setup(r);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
                _=c.Original.GetCompAbilityUserMight().MightData;
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                var def=new HediffDef{defName="HostedLifecycle"+auraType.Name,hediffClass=typeof(HediffWithComps),
                    comps=new List<HediffCompProperties>{new(){compClass=auraType}}};
                var aura=(HediffWithComps)HediffMaker.MakeHediff(def,c.Original);
                c.Original.health.AddHediff(aura);
                var comp=aura.comps.Single();
                Set(comp,"nextApplyTick",Find.TickManager.TicksGame+2000);
                float adjustment=0;
                comp.CompPostTick(ref adjustment);
                Check.Equal(true,c.Original.health.hediffSet.hediffs.Contains(aura),"valid hosted aura remains on its resource owner");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                comp.CompPostTick(ref adjustment);
                Check.Equal(false,c.Original.health.hediffSet.hediffs.Contains(aura),"depleted source cannot retain a projected aura");
            }));
        foreach(string family in new[]{"MoveOut","StayAlert","HoldTheLine"}) yield return new("CommanderProjection.Legacy"+family,r=>SpiritRebirthRitualTransactionTests.Run(r,c=> {
            using var fixture=new Fixture(r,c);
            using var definitions=CommanderCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            c.Creator.skills=new Pawn_SkillTracker(c.Creator);
            c.Creator.skills.skills=c.Original.skills.skills.Select(s=>new SkillRecord(c.Creator,s.def)).ToList();
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(SkillDefOf));
            var oldSocial=SkillDefOf.Social;SkillDefOf.Social=c.Original.skills.skills[0].def;
            c.Original.skills.GetSkill(SkillDefOf.Social).Level=20;
            c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
            var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_"+family+"_II")!.GetValue(null)!;
            var skill=source.MightData.GetSkill_Power(def);Check.NotNull(skill,"native order power skill");skill.level=2;
            var oldVerb=def.MainVerb;var oldEffect=TorannMagicDefOf.TM_CommanderOrderED;
            var effectField=typeof(TorannMagicDefOf).GetField("TM_"+family+"HD")!;
            var oldHediff=effectField.GetValue(null);
            var oldShield=TorannMagicDefOf.TM_HTLShieldHD;var oldShieldEffect=TorannMagicDefOf.TM_HTL_EffecterED;
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var oldCost=(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost);
            var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var oldEvents=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            try {
                TorannMagicDefOf.TM_CommanderOrderED=new EffecterDef{children=[]};
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                def.consumeEnergy=true;def.staminaCost=.2f;def.manaCost=def.chiCost=def.bloodCost=0;
                source.maxSP=1;c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;source.Stamina.CurLevelPercentage=1;
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="HostedLegacyCommanderActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                effectField.SetValue(null,new HediffDef{defName="CommanderLegacy"+family,hediffClass=typeof(Hediff),initialSeverity=0});
                TorannMagicDefOf.TM_HTLShieldHD=new HediffDef{defName="CommanderLegacyShield",hediffClass=typeof(Hediff),initialSeverity=0};
                TorannMagicDefOf.TM_HTL_EffecterED=new EffecterDef{children=[]};
                def.MainVerb=new VerbProperties_Ability{verbClass=typeof(Verb_CommanderOrders),abilityDef=def,warmupTime=1,SecondsToRecharge=40,
                    TargetAoEProperties=new TargetAoEProperties{startsFromCaster=true,range=8,maxTargets=5},
                    targetParams=new TargetingParameters{canTargetPawns=true,canTargetHumans=true,canTargetSelf=false}};
                var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                string hostedJob=family=="MoveOut"?"TM_SpiritCommanderMove":family=="StayAlert"?"TM_SpiritCommanderAlert":"TM_SpiritCommanderHold";
                Check.Same(power,Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,c.Original,hostedJob])!,"ranked native family resolves original owner");
                using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                using var motes=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
                using var flecks=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericFleck))!,nameof(CancelNative));
                using var chance=new Patch(typeof(Rand).GetMethod(nameof(Rand.Chance),[typeof(float)])!,nameof(CommanderAccept));
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                using var endJob=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
                CommanderFieldTests.Verify(r,c.Creator,source,false,c.Original,worker=> {
                    var oldMapPawns=c.Map.mapPawns;c.Map.mapPawns=new MapPawns(c.Map);
                    try {
                        Set(c.Map.mapPawns,"pawnsSpawned",new List<Pawn>{worker});
                        worker.needs.rest=new Need_Rest(worker){def=NeedDefOf.Rest,CurLevel=.1f};
                        worker.needs.AllNeeds.Add(worker.needs.rest);
                        var hd=(HediffDef)typeof(TorannMagicDefOf).GetField("TM_"+family+"HD")!.GetValue(null)!;
                        var verb=new Verb_CommanderOrders{caster=c.Creator,Ability=power,verbProps=def.MainVerb};
                        commanderChances.Clear();
                        Check.Equal(true,(bool)typeof(Verb_CommanderOrders).GetMethod("TryCastShot",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(verb,null)!,"completed native order reports success");
                        Check.Equal(1f,commanderChances[0],"acceptance uses original social20 rather than carrier social0");
                        var actual=worker.health.hediffSet.GetFirstHediffOfDef(hd);
                        Check.NotNull(actual,"real nearby ally receives native order");
                        float expected=family=="MoveOut"?.86f:.9f;
                        Check.Equal(true,Math.Abs(actual!.Severity-expected)<.001f,"native order scales with original two power upgrades");
                        if(family=="StayAlert")Check.Equal(true,Math.Abs(worker.needs.rest.CurLevel-.7f)<.001f,"original power restores real rest need");
                        if(family=="HoldTheLine")Check.Equal(45f,worker.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HTLShieldHD).Severity,"original power supplies native shield budget");
                        worker.health.RemoveHediff(actual);
                        var shield=worker.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HTLShieldHD);
                        if(shield!=null)worker.health.RemoveHediff(shield);
                        worker.needs.rest.CurLevel=.1f;
                        var jobDef=new JobDef{defName=hostedJob,driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
                        var job=JobMaker.MakeJob(jobDef,c.Original);
                        var driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=job};
                        Set(c.Creator.jobs,"curJob",job);Set(c.Creator.jobs,"curDriver",driver);
                        var make=typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!;
                        var toils=((IEnumerable<Toil>)make.Invoke(driver,null)!).ToArray();
                        float before=source.Stamina.CurLevel;int xp=source.MightUserXP;
                        toils[0].initAction();Check.Equal(before,source.Stamina.CurLevel,"legacy warmup is free");
                        var warm=RoundTripSummon(r,"legacy-commander-warmup",new MonkJobSave{Body=c.Creator,Order=job,Driver=driver},c.Creator,c.Original);
                        Set(c.Creator.jobs,"curJob",warm.Order);Set(c.Creator.jobs,"curDriver",warm.Driver);
                        var resumed=((IEnumerable<Toil>)make.Invoke(warm.Driver,null)!).ToArray();resumed[1].initAction();
                        Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(before-source.ActualStaminaCost(def)))<.001f,"legacy order bills source once after warmup load");
                        Check.Equal(true,power.CooldownTicksLeft>0 && source.MightUserXP>xp,"legacy order cooldown and XP belong to soul");
                        Check.Equal(true,Math.Abs(worker.health.hediffSet.GetFirstHediffOfDef(hd).Severity-expected)<.001f,"full saved job executes native effect");
                        float paid=source.Stamina.CurLevel;
                        var done=RoundTripSummon(r,"legacy-commander-completed",warm,c.Creator,c.Original);
                        Set(c.Creator.jobs,"curJob",done.Order);Set(c.Creator.jobs,"curDriver",done.Driver);
                        ((IEnumerable<Toil>)make.Invoke(done.Driver,null)!).ToArray()[1].initAction();
                        Check.Equal(paid,source.Stamina.CurLevel,"completed load cannot charge again");
                        Check.Equal(true,Math.Abs(worker.health.hediffSet.GetFirstHediffOfDef(hd).Severity-expected)<.001f,"completed load cannot stack another effect");
                        if(family=="StayAlert")Check.Equal(true,Math.Abs(worker.needs.rest.CurLevel-.7f)<.001f,"completed load cannot restore rest again");
                    } finally {c.Map.mapPawns=oldMapPawns;}
                });
            } finally {def.MainVerb=oldVerb;TorannMagicDefOf.TM_CommanderOrderED=oldEffect;effectField.SetValue(null,oldHediff);
                TorannMagicDefOf.TM_HTLShieldHD=oldShield;TorannMagicDefOf.TM_HTL_EffecterED=oldShieldEffect;TorannMagic.ModOptions.Settings.Instance=settings;SkillDefOf.Social=oldSocial;
                (def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost)=oldCost;
                TorannMagicDefOf.TM_UsedManeuver=oldEvent;DefDatabase<HistoryEventDef>.Clear();foreach(var d in oldEvents)DefDatabase<HistoryEventDef>.Add(d);}
        }));
        foreach(bool rally in new[]{false,true}) yield return new("CommanderProjection.Coordination"+(rally?"Rally":"Crew"), r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
            var path=state.Find("rwom.commander")!;Set(path,"stage",rally?InitiationStage.Master:InitiationStage.Foundation);
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            source.maxSP=1;c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;
            c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
            c.Map.info=new MapInfo{Size=new IntVec3(60,1,60)};c.Map.cellIndices=new CellIndices(c.Map);
            c.Map.thingGrid=new ThingGrid(c.Map);c.Map.edificeGrid=new EdificeGrid(c.Map);c.Map.fogGrid=new FogGrid(c.Map);
            c.Map.weatherManager=(WeatherManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
            c.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
            var def=r.AddAbility(rally?"TM_CommanderRally":"TM_CommanderWorkCrew");
            def.consumeEnergy=true;def.staminaCost=.2f;def.manaCost=def.chiCost=def.bloodCost=0;
            def.MainVerb=new VerbProperties_Ability{verbClass=typeof(Verb_CommanderCoordination),range=10,warmupTime=1,SecondsToRecharge=40};
            var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            string name=rally?"TM_SpiritCommanderRally":"TM_SpiritCommanderCrew";
            var jobDef=new JobDef{defName=name,driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
            var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
            Check.Same(power,find.Invoke(null,[c.Creator,c.Original,name])!,"hosted coordination resolves original ability");
            var oldSettings=TorannMagic.ModOptions.Settings.Instance;var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var oldEvents=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="HostedCommanderActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                using var nativeOwner=new Patch(typeof(Verb).GetProperty(nameof(Verb.DirectOwner))!.GetMethod!,nameof(NativeAbilityOwner));
                using var endJob=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
                var ready=Production("SpiritHostedMartial").GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Equal(true,(bool)ready.Invoke(null,[c.Creator,power,name])!,"trained soul may issue coordination");
                if(rally)
                {
                    Set(path,"stage",InitiationStage.Foundation);
                    Check.Equal(false,(bool)ready.Invoke(null,[c.Creator,power,name])!,"rally remains locked before mastery");
                    Set(path,"stage",InitiationStage.Master);
                }
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(true,find.Invoke(null,[c.Creator,c.Original,name])==null,"depletion blocks stale coordination");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
                var job=JobMaker.MakeJob(jobDef,c.Original,c.Creator.Position+IntVec3.East);
                var driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=job};
                Set(c.Creator.jobs,"curJob",job);Set(c.Creator.jobs,"curDriver",driver);
                var toils=((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null)!).ToArray();
                float before=source.Stamina.CurLevel;int xp=source.MightUserXP;
                toils[0].initAction();Check.Equal(before,source.Stamina.CurLevel,"preparation is free");
                toils[1].initAction();
                Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(before-source.ActualStaminaCost(def)))<.001f,"issued order pays soul even without recipients");
                Check.Equal(true,power.CooldownTicksLeft>0,"soul gets cooldown");
                Check.Equal(true,source.MightUserXP>xp,"soul gets use experience");
                var api=Production("CommanderMartialPath");
                var data=api.GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[path])!;
                var serial=data.GetType().GetField("Serial",BindingFlags.Instance|BindingFlags.NonPublic)!;
                Check.Equal(1,(int)serial.GetValue(data)!,"one order batch issued");
                float paid=source.Stamina.CurLevel;
                var saved=RoundTripSummon(r,"commander-coordination-paid",new MonkJobSave{Body=c.Creator,Order=job,Driver=driver},c.Creator,c.Original);
                Set(c.Creator.jobs,"curJob",saved.Order);Set(c.Creator.jobs,"curDriver",saved.Driver);
                var repeated=((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(saved.Driver,null)!).ToArray();
                repeated[1].initAction();Check.Equal(paid,source.Stamina.CurLevel,"loaded completed order cannot pay twice");
                Check.Equal(1,(int)serial.GetValue(data)!,"loaded completed order cannot issue another batch");
            } finally {TorannMagic.ModOptions.Settings.Instance=oldSettings;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
                DefDatabase<HistoryEventDef>.Clear();foreach(var old in oldEvents)DefDatabase<HistoryEventDef>.Add(old);}
        }));
        yield return new("CommanderProjection.Operation", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=CommanderCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
            var path=state.Find("rwom.commander")!;
            Set(path,"stage",InitiationStage.Ascended);
            var api=Production("CommanderMartialPath");
            var data=api.GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[path])!;
            Set(data,"Operation",true);
            var might=c.Original.GetCompAbilityUserMight();_=might.MightData;
            api.GetMethod("Restore",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(Activator.CreateInstance(api,true),[might,path]);
            CommanderFieldTests.Verify(r,c.Creator,might,true,c.Original);
        }));
        yield return new("CommanderProjection.WorkReceipt", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=CommanderCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
            CommanderFieldTests.Verify(r,c.Creator,c.Original.GetCompAbilityUserMight(),false,c.Original);
            Check.Equal(true,ClassIdentity.Component(c.Creator).State.Find("rwom.commander")==null,"receipt cannot create host class");
        }));
        yield return new("CommanderProjection.Owner", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
            var method=Production("CommanderMartialPath").GetMethod("Owner",BindingFlags.Static|BindingFlags.NonPublic);
            Check.NotNull(method,"commander must resolve physical actor's discipline owner");
            Pawn? Owner(Pawn actor)=>(Pawn?)method!.Invoke(null,[actor]);
            Check.Same(c.Original,Owner(c.Creator)!,"host borrows soul's command training");
            Check.Same(c.Original,Owner(c.Original)!,"native owner remains itself");
            var local=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Creator),"state",local);
            local.TryAddFoundation("rwom.commander",DisciplineDomain.Might);
            Check.Same(c.Creator,Owner(c.Creator)!,"native commander has priority");
            Set(ClassIdentity.Component(c.Creator),"state",new ClassProgressionState(Vocation.Warrior));
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(true,Owner(c.Creator)==null,"depletion revokes borrowed orders");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            var receipt=c.Creator.health.hediffSet.GetFirstHediffOfDef(TorannMagic.TorannMagicDefOf.TM_SpiritPossessionHD);
            c.Creator.health.hediffSet.hediffs.Remove(receipt);
            Check.Equal(true,Owner(c.Creator)==null,"detached possession cannot command");
        }));
    }
    private static readonly List<float> commanderChances=[];
    private static bool CommanderAccept(float __0,ref bool __result) {commanderChances.Add(__0);__result=true;return false;}
}
