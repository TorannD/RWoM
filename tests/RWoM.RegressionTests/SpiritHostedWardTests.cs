using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private static IEnumerable<TestCase> HostedWardCases()
    {
        foreach (var test in HostedMartialCases()) yield return test;
        foreach (var test in HostedGladiatorCases()) yield return test;
        foreach(bool shield in new[]{true,false})
            yield return new("SpiritProjectedCast.TimedBodyEffect."+(shield?"PaladinShield":"FacelessDisguise"), f=>
                SpiritRebirthRitualTransactionTests.Run(f,c=>{
                    using var custody=new Fixture(f,c);
                    var soul=c.Original;var body=c.Creator;
                    var oldSettings=TorannMagic.ModOptions.Settings.Instance;
                    var hediffs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
                    var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
                    var shieldDef=TorannMagicDefOf.TM_HediffShield;var disguiseDef=TorannMagicDefOf.TM_DisguiseHD;
                    var priorManaEvent=TorannMagicDefOf.TM_UsedMagic;var priorMightEvent=TorannMagicDefOf.TM_UsedManeuver;
                    var def=shield?TorannMagicDefOf.TM_Shield_II:TorannMagicDefOf.TM_Disguise;
                    var props=def.MainVerb;float prevMana=def.manaCost,prevStamina=def.staminaCost,prevRecharge=props.SecondsToRecharge;var prevEffects=props.hediffsToApply;
                    bool consume=def.consumeEnergy;
                    try {
                        TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                        var effect=new HediffDef{defName="TestSpiritProjected"+(shield?"Shield":"Disguise"),hediffClass=typeof(HediffWithComps),
                            maxSeverity=200,comps=[new HediffCompProperties{compClass=typeof(HediffComp_SpiritCastOrigin)}]};
                        DefDatabase<HediffDef>.Add(effect);
                        TorannMagicDefOf.TM_HediffShield=shield?effect:shieldDef;TorannMagicDefOf.TM_DisguiseHD=shield?disguiseDef:effect;
                        var ev=new HistoryEventDef{defName="SpiritProjectedUseEvent"};DefDatabase<HistoryEventDef>.Add(ev);
                        TorannMagicDefOf.TM_UsedMagic=TorannMagicDefOf.TM_UsedManeuver=ev;
                        var history=(History)RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                        using var eventsPatch=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                        if(DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD")==null)DefDatabase<HediffDef>.Add(new HediffDef{defName="TM_SyrriumSenseHD"});
                        var magic=soul.GetCompAbilityUserMagic();var might=soul.GetCompAbilityUserMight();
                        soul.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;might.maxSP=1f;_=might.MightData;def.consumeEnergy=true;def.manaCost=shield?.20f:0;def.staminaCost=shield?0:.20f;props.SecondsToRecharge=4;
                        props.hediffsToApply=[new ApplyHediffs{hediffDef=effect,applyChance=1,severity=.54f}];
                        PawnAbility power=shield?new MagicAbility(magic){Def=def}:new MightAbility(might){Def=def};
                        CompAbilityUser source=shield?(CompAbilityUser)magic:might;
                        source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                        if(magic.Mana==null)soul.needs.AllNeeds.Add(new Need_Mana(soul){def=TorannMagicDefOf.TM_Mana});
                        if(might.Stamina==null)soul.needs.AllNeeds.Add(new Need_Stamina(soul){def=TorannMagicDefOf.TM_Stamina});
                        Need pool=shield?(Need)magic.Mana:might.Stamina;pool.CurLevel=.8f;
                        float before=pool.CurLevel;float price=shield?magic.ActualManaCost(def):might.ActualStaminaCost(def);
                        var api=Production("SpiritHostedWard");const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic;
                        var find=api.GetMethod("Find",hidden)!;
                        Check.Same(power,find.Invoke(null,[body,soul,shield]),"verified original instance");
                        CompAbilityUser local=shield?(CompAbilityUser)body.GetCompAbilityUserMight():body.GetCompAbilityUserMagic();
                        var overlap=new PawnAbility(local){Def=shield?TorannMagicDefOf.TM_Shield:def};
                        local.AbilityData.Powers.Add(overlap);local.AbilityData.AllPowers=null;
                        Check.Equal(true,find.Invoke(null,[body,soul,shield])==null,"body owns a rank of the same family through another component");
                        local.AbilityData.Powers.Remove(overlap);local.AbilityData.AllPowers=null;
                        var selectedEffect=api.GetMethod("Effect",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[power,0f]);
                        Check.Same(effect,selectedEffect,"expected effect for native rank");
                        Check.Equal(true,(bool)api.GetMethod("Ready",hidden)!.Invoke(null,[body,power])!,"ready for price "+price+" out of "+pool.CurLevel+" mana cost "+def.manaCost+" stamina "+def.staminaCost);
                        using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!,nameof(CancelNative));
                        var job=JobMaker.MakeJob(new JobDef{defName=shield?"TM_SpiritCastPaladinShield":"TM_SpiritCastDisguise"},soul);
                        var driver=new JobDriver_SpiritHostedWard{pawn=body,job=job};
                        Set(body.jobs,"curJob",job);Set(body.jobs,"curDriver",driver);
                        var toils=((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToList();
                        Check.Equal(2,toils.Count,"warmup before effect");Check.Equal(before,pool.CurLevel,"construction does not pay");
                        toils[0].initAction();Check.Equal(before,pool.CurLevel,"warmup cannot charge");
                        toils[1].initAction();
                        var applied=body.health.hediffSet.GetFirstHediffOfDef(effect);
                        Check.NotNull(applied,"native body effect present");
                        Check.Equal(true,Math.Abs(before-price-pool.CurLevel)<.0001f && price>0,"one original resource charge");
                        Check.Equal(true,power.CooldownTicksLeft>0,"original cooldown");
                        Check.Equal(false,soul.health.hediffSet.HasHediff(effect),"body alone receives effect");
                        Check.Equal(shield?.54f:30f,applied.Severity,"native rank strength");
                        var origin=applied.TryGetComp<HediffComp_SpiritCastOrigin>();Check.Equal(false,origin.CompShouldRemove,"live reciprocal effect");
                        Check.Equal(false,(bool)api.GetMethod("Apply",hidden)!.Invoke(null,[body,soul,shield])!,"cannot charge twice");
                        var file=Path.Combine(f.WorkingDirectory,"spirit-ward.xml");
                        try {Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref applied,"effect");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
                        applied=null!;
                        try {Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref applied,"effect");applied.pawn=body;
                            var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                            directory.RegisterLoaded(soul);directory.RegisterLoaded(body);Scribe.loader.FinalizeLoading();
                        }finally{Scribe.ForceStop();}
                        origin=applied.TryGetComp<HediffComp_SpiritCastOrigin>();Check.Equal(false,origin.CompShouldRemove,"saved hediff still references original soul");
                        var state=ClassIdentity.Component(soul).SpiritRebirth;
                        Set(state,"host",null);Check.Equal(true,origin.CompShouldRemove,"old host effect expires immediately on severed custody");
                        Check.Equal(true,find.Invoke(null,[body,soul,shield])==null,"stale job cannot resolve soul");
                    } finally {
                        def.manaCost=prevMana;def.staminaCost=prevStamina;props.hediffsToApply=prevEffects;props.SecondsToRecharge=prevRecharge;def.consumeEnergy=consume;
                        TorannMagicDefOf.TM_HediffShield=shieldDef;TorannMagicDefOf.TM_DisguiseHD=disguiseDef;
                        TorannMagicDefOf.TM_UsedMagic=priorManaEvent;TorannMagicDefOf.TM_UsedManeuver=priorMightEvent;
                        DefDatabase<HediffDef>.Clear();foreach(var d in hediffs)DefDatabase<HediffDef>.Add(d);
                        DefDatabase<HistoryEventDef>.Clear();foreach(var d in events)DefDatabase<HistoryEventDef>.Add(d);
                        TorannMagic.ModOptions.Settings.Instance=oldSettings;
                    }
                }));
    }
}
