using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse.AI;
using AbilityUser;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private static Pawn? sprintAlly;
    private static bool StanceHitPoints(ref int __result) { __result=100;return false; }
    private static bool SprintAllies(ref List<Pawn> __result) { __result=[sprintAlly!]; return false; }

    private static void SprintJob(RuntimeFixture fixture,SpiritRebirthRitualTransactionTests.Context c,int rank,bool otherStances=false,bool migrate=false)
    {
        using var possession=new Fixture(fixture,c);
        var original=c.Original;
        var body=c.Creator;
        var source=original.GetCompAbilityUserMight();
        var defs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        var historyDefs=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
        var beforeDefs=(TorannMagicDefOf.TM_HediffSprint,TorannMagicDefOf.TM_EvasionHD,TorannMagicDefOf.TM_AdrenalineHD,TorannMagicDefOf.TM_EnergyRegenHD);
        var priorEvent=TorannMagicDefOf.TM_UsedManeuver;var settings=TorannMagic.ModOptions.Settings.Instance;
        TMAbilityDef def=new[]{TorannMagicDefOf.TM_Sprint,TorannMagicDefOf.TM_Sprint_I,TorannMagicDefOf.TM_Sprint_II,TorannMagicDefOf.TM_Sprint_III}[rank];
        var prior=(def.upkeepEnergyCost,def.upkeepRegenCost,def.upkeepEfficiencyPercent,def.consumeEnergy,def.MainVerb.SecondsToRecharge);
        try
        {
            TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
            HediffDef Effect(string name,bool timed)
            {
                var effect=new HediffDef{defName=name,hediffClass=typeof(HediffWithComps),maxSeverity=5,
                    comps=[new HediffCompProperties{compClass=typeof(HediffComp_HostedStance)}]};
                if(timed)effect.comps.Add(new HediffCompProperties_SetDuration());
                DefDatabase<HediffDef>.Add(effect);return effect;
            }
            TorannMagicDefOf.TM_HediffSprint=Effect("SprintPhysicalTest",false);
            TorannMagicDefOf.TM_EvasionHD=Effect("SprintEvasionTest",false);
            TorannMagicDefOf.TM_AdrenalineHD=Effect("SprintAdrenalineTest",true);
            TorannMagicDefOf.TM_EnergyRegenHD=Effect("SprintRegenTest",true);
            if (rank == 3)
            {
                // Kure replaces the whole Def, retaining its own 30-second duration comp.
                var regen = TorannMagicDefOf.TM_EnergyRegenHD;
                regen.maxSeverity = 200;
                regen.comps.RemoveAll(p => p.compClass == typeof(HediffComp_HostedStance));
                var timer = (HediffCompProperties_SetDuration)regen.comps.Single();
                timer.duration = 30;
                var ensure = typeof(ClassProgressionBootstrap).GetMethod("EnsureRegenOrigin", BindingFlags.Static | BindingFlags.NonPublic);
                Check.NotNull(ensure, "reconcile the resolved Kure regeneration def before pawns load");
                ensure!.Invoke(null, [regen]); ensure.Invoke(null, [regen]);
                Check.Same(timer, regen.comps[0], "keep Kure timer and ordering");
                Check.Equal(30, timer.duration, "preserve Kure default duration");
                Check.Equal(200f, regen.maxSeverity, "preserve Kure upper severity");
                Check.Equal(2, regen.comps.Count, "append exactly one source marker");
            }
            if(DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD")==null)
                DefDatabase<HediffDef>.Add(new HediffDef{defName="TM_SyrriumSenseHD"});
            var ev=TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="HostedSprintUse"};
            DefDatabase<HistoryEventDef>.Add(ev);
            var history=(History)RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
            using var historyPatch=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
            using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!,nameof(CancelNative));
            using var dust=new Patch(typeof(FleckMaker).GetMethod("ThrowDustPuff",[typeof(IntVec3),typeof(Map),typeof(float)])!,nameof(CancelNative));
            using var allies=new Patch(typeof(TM_Calc).GetMethod(nameof(TM_Calc.FindAllPawnsAround))!,nameof(SprintAllies));
            sprintAlly=PawnWithComps(false,false).pawn; sprintAlly.def.race.body=body.RaceProps.body;
            original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear; source.maxSP=1;
            def.consumeEnergy=true;def.upkeepEnergyCost=.3f;def.upkeepRegenCost=.2f;def.upkeepEfficiencyPercent=.15f;def.MainVerb.SecondsToRecharge=30;
            source.MightData.MightPowerSkill_Sprint.Single(s=>s.label=="TM_Sprint_pwr").level=2;
            if(source.Stamina==null)original.needs.AllNeeds.Add(new Need_Stamina(original){def=TorannMagicDefOf.TM_Stamina});
            var ability=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(ability);source.AbilityData.AllPowers=null;
            source.Stamina.CurLevel=.1f;source.Stamina.GainNeed(1f);float normalGain=source.Stamina.CurLevel-.1f;
            source.Stamina.CurLevel=1;
            void RunJob(string name = "TM_SpiritCastSprint")
            {
                var job=JobMaker.MakeJob(new JobDef{defName=name},original);
                var driver=new JobDriver_SpiritHostedMartial{pawn=body,job=job};
                Set(body.jobs,"curJob",job);Set(body.jobs,"curDriver",driver);
                var toils=((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                toils[0].initAction();toils[1].initAction();
            }
            var capacity=typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var cache=Activator.CreateInstance(capacity.FieldType)!;
            var capacityEntry=capacity.FieldType.GetProperty("Item",[typeof(PawnCapacityDef)])!.GetValue(cache,[PawnCapacityDefOf.Moving])!;
            Set(capacityEntry,"status",Enum.Parse(capacityEntry.GetType().GetField("status")!.FieldType,"Cached"));Set(capacityEntry,"value",1f);
            capacity.SetValue(body.health.capacities,cache);
            if(migrate)
            {
                var old=HediffMaker.MakeHediff(TorannMagicDefOf.TM_HediffSprint,original);
                old.Severity=2.5f; original.health.AddHediff(old);
                source.maxSP=.7f;
                var transfer=Production("SpiritHostedStance").GetMethod("TransferActive",BindingFlags.Static|BindingFlags.NonPublic)!;
                int priorCooldown=ability.CooldownTicksLeft;
                transfer.Invoke(null,[original,body]);transfer.Invoke(null,[original,body]);
                var moved=body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffSprint);
                Check.NotNull(moved,"previously active native Sprint moved to the body");
                Check.Equal(1,body.health.hediffSet.hediffs.Count(h=>h.def==TorannMagicDefOf.TM_HediffSprint),"no repeated transfer");
                Check.Equal(false,original.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffSprint),"original dropped physical buff");
                Check.Equal(false,body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_AdrenalineHD),"transfer does not fire rank burst");
                Check.Equal(priorCooldown,ability.CooldownTicksLeft,"transfer does not cast or charge again");
                Check.Equal(true,Math.Abs(source.maxSP-source.Stamina.MaxLevel-.3f)<.0001f,"only one reserve after migration");
                return;
            }
            RunJob();
            var sprint=body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffSprint);
            Check.NotNull(sprint,"native body sprint");
            Check.Equal(2.5f,sprint.Severity,"physical severity uses original rank");
            Check.Equal(true,Math.Abs(.7f-source.Stamina.MaxLevel)<.0001f,"native reserve");
            Check.Equal(true,ability.CooldownTicksLeft>0,"original ability cooldown");
            source.Stamina.CurLevel=.1f;source.Stamina.GainNeed(1f);
            float activeGain=source.Stamina.CurLevel-.1f;
            Check.Equal(true,normalGain>0f && Math.Abs(activeGain-normalGain*.8f)<.00001f,"one native GainNeed call includes the regen debit on the original");
            source.Stamina.UseMightPower(-1f);
            Check.Equal(true,Math.Abs(source.Stamina.CurLevel-source.Stamina.MaxLevel)<.00001f,"native resource adjustments cannot overfill reserved stamina");
            Check.Equal(rank>0,body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_AdrenalineHD),"ranked adrenaline");
            Check.Equal(rank>=2,sprintAlly.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EvasionHD),"ranked allied burst");
            if(rank>=2) Check.Equal(rank==2?.25f:.3f,sprintAlly.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EvasionHD).Severity,"native area amount");
            if(otherStances)
            {
                source.MightData.MightPowerSkill_FieldTraining.Single(p=>p.label=="TM_FieldTraining_pwr").level=3;
                source.MightData.MightPowerSkill_FieldTraining.Single(p=>p.label=="TM_FieldTraining_ver").level=12;
                foreach (var entry in new[]{("StrongBack",.2f,0f,2.5f),("HeavyBlow",.3f,0f,1.52f),
                    ("ThickSkin",.3f,.15f,3.5f),("FightersFocus",.15f,0f,1.5f),
                    ("GearRepair",.2f,0f,.5f),("InnerHealing",.1f,0f,.5f)})
                {
                    var field=typeof(TorannMagicDefOf).GetField("TM_Hediff"+entry.Item1)!;
                    var previous=field.GetValue(null);
                    var stance=(TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_"+entry.Item1)!.GetValue(null)!;
                    var priorStance=(stance.upkeepEnergyCost,stance.upkeepRegenCost,stance.consumeEnergy,stance.MainVerb.SecondsToRecharge);
                    try
                    {
                        var bodyEffect=Effect("Hosted"+entry.Item1,false);field.SetValue(null,bodyEffect);
                        bool pulses=entry.Item1=="GearRepair"||entry.Item1=="InnerHealing";
                        Type? pulseType=pulses?typeof(MightAbility).Assembly.GetType("TorannMagic.HediffComp_"+entry.Item1,true):null;
                        if(pulses) bodyEffect.comps.Add(new HediffCompProperties{compClass=pulseType});
                        stance.upkeepEnergyCost=entry.Item2;stance.upkeepRegenCost=entry.Item3;stance.consumeEnergy=true;stance.MainVerb.SecondsToRecharge=25;
                        var native=new MightAbility(source){Def=stance};
                        source.AbilityData.Powers.Add(native);source.AbilityData.AllPowers=null;
                        RunJob("TM_SpiritCast"+entry.Item1);
                        var actual=body.health.hediffSet.GetFirstHediffOfDef(bodyEffect);
                        Check.NotNull(actual,"physical "+entry.Item1);
                        Check.Equal(true,Math.Abs(entry.Item4-actual.Severity)<.0001f,"native Field Training severity "+entry.Item1);
                        Check.Equal(true,Math.Abs(source.Stamina.MaxLevel-(.7f-entry.Item2))<.0001f,"two independent reserves "+entry.Item1);
                        Check.Equal(true,native.CooldownTicksLeft>0,"source cooldown "+entry.Item1);
                        if(pulses)
                        {
                            var action=((HediffWithComps)actual).comps.Single(p=>p.GetType()==pulseType);
                            int previousTick=Find.TickManager.TicksGame;
                            try
                            {
                                Set(Find.TickManager,"ticksGameInt",1200);
                                Set(action,"initializing",false);
                                if(entry.Item1=="GearRepair")
                                {
                                    using var hp=new Patch(typeof(Thing).GetProperty(nameof(Thing.MaxHitPoints))!.GetMethod!,nameof(StanceHitPoints));
                                    body.apparel??=new Pawn_ApparelTracker(body);
                                    var gear=new Apparel{def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
                                    Set(gear,"hitPointsInt",100);Set(gear,"wornByCorpseInt",true);body.apparel.WornApparel.Add(gear);
                                    pulseType!.GetMethod("TickAction")!.Invoke(action,null);
                                    Check.Equal(false,gear.WornByCorpse,"original rank five purifies equipment physically worn by the body");
                                    Set(gear,"wornByCorpseInt",true);
                                    pulseType.GetMethod("TickAction")!.Invoke(action,null);
                                    Check.Equal(true,gear.WornByCorpse,"same equipment pulse cannot replay");
                                    body.apparel.WornApparel.Remove(gear);
                                }
                                else
                                {
                                    var wound=new Hediff_Injury{pawn=body,Part=body.RaceProps.body.corePart,
                                        def=new HediffDef{defName="WayfarerWound",injuryProps=new InjuryProps(),maxSeverity=100}};
                                    Set(wound,"severityInt",5f);body.health.hediffSet.hediffs.Add(wound);
                                    float change=0;
                                    action.CompPostTick(ref change);
                                    float healed=wound.Severity;
                                    Check.Equal(true,healed<5,"rank four extra pulse healed the physical injury");
                                    action.CompPostTick(ref change);
                                    Check.Equal(healed,wound.Severity,"same healing pulse cannot replay");
                                    body.health.hediffSet.hediffs.Remove(wound);
                                }
                            }
                            finally { Set(Find.TickManager,"ticksGameInt",previousTick); }
                        }
                        native.CooldownTicksLeft=0;
                        RunJob("TM_SpiritCast"+entry.Item1);
                        Check.Equal(false,body.health.hediffSet.HasHediff(bodyEffect),"own stance turned off");
                        Check.Equal(true,Math.Abs(.7f-source.Stamina.MaxLevel)<.0001f,"Sprint reserve remains");
                        Check.Equal(true,body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffSprint),"Sprint stays active");
                        source.AbilityData.Powers.Remove(native);source.AbilityData.AllPowers=null;
                    }
                    finally
                    {
                        field.SetValue(null,previous);
                        (stance.upkeepEnergyCost,stance.upkeepRegenCost,stance.consumeEnergy,stance.MainVerb.SecondsToRecharge)=priorStance;
                    }
                }
            }
            if(rank==3)
            {
                var activeFile=Path.Combine(fixture.WorkingDirectory,"sprint-active.xml");
                possession.Possession.parent.loadID=888765;
                try { Scribe.saver.InitSaving(activeFile,"test");Scribe_Deep.Look(ref sprint,"sprint");Scribe.saver.FinalizeSaving(); }
                finally {Scribe.ForceStop();}
                Hediff restored=null!;
                try
                {
                    Scribe.loader.InitLoading(activeFile);Scribe_Deep.Look(ref restored,"sprint");restored.pawn=body;
                    var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
                    directory.RegisterLoaded(original);directory.RegisterLoaded(body);directory.RegisterLoaded(possession.Possession.parent);
                    Scribe.loader.FinalizeLoading();
                }
                finally {Scribe.ForceStop();}
                var marker=restored.TryGetComp<HediffComp_HostedStance>();
                Check.Equal(false,marker.CompShouldRemove,"Scribe retains exact native custody of the paid active effect");
                Check.Same(def,marker.GetType().GetField("ability",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(marker)!, "saved rank definition supplies the upkeep");
            }
            source.Stamina.CurLevel=.1f;ability.CooldownTicksLeft=0;
            RunJob();
            Check.Equal(false,body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffSprint),"ordinary off job removes the same owner's sprint");
            float refund=rank==0?0:rank==1?.2f:rank==2?.3f:.4f;
            Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(.1f+refund))<.0001f,"one native refund goes to soul");
            Check.Equal(1f,source.Stamina.MaxLevel,"reserve released");
            if(rank>0)
            {
                var regen=body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD);
                Check.Equal(1f+refund,regen.Severity,"refund regen multiplier");
                Check.Equal(rank==1?5:rank==2?8:10,regen.TryGetComp<HediffComp_SetDuration>().duration,"native regen duration");
            }
        }
        finally
        {
            sprintAlly=null;
            (def.upkeepEnergyCost,def.upkeepRegenCost,def.upkeepEfficiencyPercent,def.consumeEnergy,def.MainVerb.SecondsToRecharge)=prior;
            (TorannMagicDefOf.TM_HediffSprint,TorannMagicDefOf.TM_EvasionHD,TorannMagicDefOf.TM_AdrenalineHD,TorannMagicDefOf.TM_EnergyRegenHD)=beforeDefs;
            TorannMagicDefOf.TM_UsedManeuver=priorEvent;TorannMagic.ModOptions.Settings.Instance=settings;
            DefDatabase<HediffDef>.Clear();foreach(var d in defs)DefDatabase<HediffDef>.Add(d);
            DefDatabase<HistoryEventDef>.Clear();foreach(var d in historyDefs)DefDatabase<HistoryEventDef>.Add(d);
        }
    }

    private static IEnumerable<TestCase> HostedGladiatorCases()
    {
        yield return new("SpiritProjectedCast.Gladiator.ActiveLegacySprintTransfersOnce",f=>
            SpiritRebirthRitualTransactionTests.Run(f,c=>SprintJob(f,c,1,migrate:true)));
        yield return new("SpiritProjectedCast.Gladiator.WayfarerStancesShareOnlyOwnerReserve",f=>
            SpiritRebirthRitualTransactionTests.Run(f,c=>SprintJob(f,c,0,true)));
        foreach(int rank in Enumerable.Range(0,4))
            yield return new("SpiritProjectedCast.Gladiator.SprintJobRank"+rank,f=>
                SpiritRebirthRitualTransactionTests.Run(f,c=>SprintJob(f,c,rank)));

        yield return new("SpiritProjectedCast.Gladiator.PassiveFortitude",f=>SpiritRebirthRitualTransactionTests.Run(f,c=>{
            using var possession=new Fixture(f,c);
            var source=c.Original.GetCompAbilityUserMight();
            var power=source.MightData.MightPowersG.Single(p=>p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fortitude));
            var method=Production("SpiritHostedGladiator").GetMethod("FortitudeOwner",BindingFlags.Static|BindingFlags.NonPublic)!;
            power.learned=false;
            Check.Equal(null,method.Invoke(null,[c.Creator,possession.Possession.parent]),"possession alone confers no mitigation");
            power.learned=true;
            Check.Same(source,method.Invoke(null,[c.Creator,possession.Possession.parent])!,"the learned passive resolves to original warrior without a cast");
            Check.Equal(null,method.Invoke(null,[c.Creator,new HediffWithComps{def=TorannMagicDefOf.TM_SpiritPossessionHD,pawn=c.Creator}]),"another possession hediff cannot apply Fortitude a second time");
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var oldFortitude=TorannMagicDefOf.TM_HediffFortitude;
            try
            {
                TorannMagicDefOf.TM_HediffFortitude=new HediffDef { defName="TM_HediffFortitude" };
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                source.MightData.MightPowerSkill_Fortitude.Single(s=>s.label=="TM_Fortitude_pwr").level=2;
                source.MightData.MightPowerSkill_Fortitude.Single(s=>s.label=="TM_Fortitude_ver").level=3;
                source.maxSP=1;c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;
                if(source.Stamina==null)c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina});
                source.Stamina.CurLevel=.5f;
                var attacker=new Pawn{def=c.Creator.def,kindDef=c.Creator.kindDef};
                attacker.story=new Pawn_StoryTracker(attacker){traits=new TraitSet(attacker)};
                attacker.health=new Pawn_HealthTracker(attacker);
                Set(attacker,"comps",new List<ThingComp>()); Set(attacker,"mapIndexOrState",(sbyte)0);
                var info=new DamageInfo(new DamageDef{defName="HostedFortitudeBlast",isExplosive=true},12,instigator:attacker);
                Check.Equal(true,TorannMagicMod.Pawn_PreApplyDamage.Prefix(c.Creator,ref info,out bool absorbed),"partially mitigated native hit continues");
                Check.Equal(true,absorbed,"same native absorption signal");
                Check.Equal(5f,info.Amount,"original rank two provides seven flat reduction on body");
                Check.Equal(true,source.Stamina.CurLevel>.5f,"native gain credits the original pool");
                var effect=new HediffWithComps {def=TorannMagicDefOf.TM_HediffFortitude,pawn=c.Creator};
                c.Creator.health.hediffSet.hediffs.Add(effect);
                var hostMight=c.Creator.GetCompAbilityUserMight();
                if(hostMight!=null)c.Creator.AllComps.Remove(hostMight);
                try
                {
                    Check.Equal(null,method.Invoke(null,[c.Creator,possession.Possession.parent]),"native host protection takes priority");
                    var localHit=new DamageInfo(info.Def,12,instigator:attacker);
                    float before=source.Stamina.CurLevel;
                    TorannMagicMod.Pawn_PreApplyDamage.Prefix(c.Creator,ref localHit,out _);
                    Check.Equal(7f,localHit.Amount,"body without warrior ranks still receives the base local Fortitude reduction");
                    Check.Equal(before,source.Stamina.CurLevel,"local Fortitude cannot restore the soul's stamina");
                }
                finally { if(hostMight!=null)c.Creator.AllComps.Add(hostMight); c.Creator.health.hediffSet.hediffs.Remove(effect); }
            }
            finally { TorannMagic.ModOptions.Settings.Instance=settings; TorannMagicDefOf.TM_HediffFortitude=oldFortitude; }
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(null,method.Invoke(null,[c.Creator,possession.Possession.parent]),"pending collapse stops passive");
        }));

        yield return new("SpiritProjectedCast.Gladiator.SprintReserveAndRank",f=>SpiritRebirthRitualTransactionTests.Run(f,c=>{
            using var possession=new Fixture(f,c);
            var source=c.Original.GetCompAbilityUserMight();
            var data=source.MightData;
            var def=TorannMagicDefOf.TM_Sprint_III;
            var saved=(def.upkeepRegenCost,def.upkeepEnergyCost,def.upkeepEfficiencyPercent);
            var active=new HediffWithComps{def=TorannMagicDefOf.TM_HediffSprint,pawn=c.Creator};
            var marker=new HediffComp_HostedStance{parent=active};
            const BindingFlags instance=BindingFlags.Instance|BindingFlags.NonPublic, api=BindingFlags.Static|BindingFlags.NonPublic;
            try
            {
                def.upkeepRegenCost=.2f;def.upkeepEnergyCost=.3f;def.upkeepEfficiencyPercent=.15f;
                data.MightPowerSkill_Sprint.Single(s=>s.label=="TM_Sprint_eff").level=1;
                data.MightPowerSkill_FieldTraining.Single(s=>s.label=="TM_FieldTraining_eff").level=2;
                var learned=new MightAbility(source){Def=def};
                source.AbilityData.Powers.Add(learned);source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",api)!;
                Check.Same(learned,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastSprint"])!,"exact original rank three");
                var local=c.Creator.GetCompAbilityUserMagic();var duplicate=new PawnAbility(local){Def=TorannMagicDefOf.TM_Sprint};
                local.AbilityData.Powers.Add(duplicate);local.AbilityData.AllPowers=null;
                Check.Equal(null,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastSprint"]),"body's rank zero hides original rank three");
                local.AbilityData.Powers.Remove(duplicate);local.AbilityData.AllPowers=null;
                marker.GetType().GetMethod("Bind",instance)!.Invoke(marker,[c.Original,c.Creator]);
                Set(marker,"ability",def);
                active.comps=[marker];c.Creator.health.hediffSet.hediffs.Add(active);
                var method=Production("SpiritHostedStance").GetMethod("Cost",api)!;
                float Cost(Pawn pawn,bool regen)=>(float)method.Invoke(null,[pawn,regen])!;
                Check.Equal(true,Math.Abs(Cost(c.Original,false)-.3f*.85f*.94f)<.00001f,"only original pool pays reserve");
                Check.Equal(true,Math.Abs(Cost(c.Original,true)-.2f*.85f*.94f)<.00001f,"both efficiency skills scale native regen debit");
                Check.Equal(0f,Cost(c.Creator,false),"host pool unaffected");
                var old=c.Creator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
                c.Creator.health.hediffSet.hediffs.Remove(old);
                Check.Equal(true,marker.CompShouldRemove,"severed exact possession invalidates effect");
                Check.Equal(0f,Cost(c.Original,false),"reserve released immediately");
            }
            finally {(def.upkeepRegenCost,def.upkeepEnergyCost,def.upkeepEfficiencyPercent)=saved;}
        }));
    }
}
