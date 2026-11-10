using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> SiphonCases()
    {
        yield return new("Odyssey.ManaSiphon.NativeCapacityReductionDoesNotLoseMana", f => SiphonRun(f, SiphonCapacityReduction));
        yield return new("Odyssey.ManaSiphon.ActualMagicalGravcoreRecipient", f => SiphonRun(f, SiphonGravcore));
        yield return new("Odyssey.ManaSiphon.HiddenBufferDrainOwnershipAndCapacity", f => SiphonRun(f, SiphonBuffer));
        yield return new("Odyssey.ManaSiphon.FullStartContinuesAndNoDuplicate", f => SiphonRun(f, SiphonPayments));
        yield return new("Odyssey.ManaSiphon.ReserveVisitLimitAndPartialTank", f => SiphonRun(f, SiphonLimits));
        yield return new("Odyssey.ManaSiphon.NetworkOwnershipAndNoConsumers", f => SiphonRun(f, SiphonTargets));
        yield return new("Odyssey.ManaSiphon.NativeDriverScribeDeadlineAndCancel", f => SiphonRun(f, () => SiphonDriver(f)));
        yield return new("Odyssey.ManaSiphon.SettingsAndCooldownScribe", f => SiphonRun(f, () => SiphonSave(f)));
    }
    static void SiphonRun(RuntimeFixture f, Action action)
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var oldConscious=PawnCapacityDefOf.Consciousness;
        try { SiphonDefs<WorkTypeDef>(() => SiphonDefs<PawnCapacityDef>(() => Run(f,action))); }
        finally { PawnCapacityDefOf.Consciousness=oldConscious; }
    }
    static void SiphonDefs<TDef>(Action action) where TDef : Def, new()
    {
        var defs=DefDatabase<TDef>.AllDefsListForReading; var old=defs.ToArray();
        var names=(Dictionary<string,TDef>)typeof(DefDatabase<TDef>).GetField("defsByName",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!; var oldNames=names.ToArray();
        try { action(); } finally { defs.Clear(); defs.AddRange(old); names.Clear(); foreach(var pair in oldNames) names.Add(pair.Key,pair.Value); }
    }
    static (ThingComp siphon, ThingComp tank, Pawn pawn, Need_Mana need) SiphonSetup()
    {
        Current.Game.Scenario = new Scenario(); Set(Current.Game,"playSettings",new PlaySettings());
        Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
        map.info=new MapInfo {Size=new IntVec3(80,1,80)}; map.cellIndices=new CellIndices(map); map.edificeGrid=new EdificeGrid(map);
        map.events=new MapEvents(map); map.reservationManager=new ReservationManager(map); map.physicalInteractionReservationManager=new PhysicalInteractionReservationManager();
        Make("ManaBeacon", 11, 11);
        var siphon = Make("ManaSiphon", 10, 10);
        siphon.parent.def.hasInteractionCell = true;
        siphon.parent.def.interactionCellOffset = new IntVec3(0, 0, -1);
        var endpoint = (ThingComp)Activator.CreateInstance(T("CompManaNetworkEndpoint"))!;
        endpoint.parent = siphon.parent; endpoint.props = (CompProperties)Activator.CreateInstance(T("CompProperties_ManaNetworkEndpoint"))!;
        siphon.parent.AllComps.Add(endpoint); endpoint.Initialize(endpoint.props); endpoint.PostSpawnSetup(false);
        var tank = Tank(12, 10, true); tank.parent.def.defName = "TM_MagofuelReservoir";
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); d.defName="SiphonMage"; d.race=new RaceProperties { intelligence=Intelligence.Humanlike };
        var pawn = new Pawn { def = d };
        pawn.jobs = new Pawn_JobTracker(pawn); pawn.workSettings=new Pawn_WorkSettings(pawn);
        var work=DefDatabase<WorkTypeDef>.GetNamedSilentFail("TM_Magic");
        if(work==null) { work=new WorkTypeDef { defName="TM_Magic" }; DefDatabase<WorkTypeDef>.Add(work); }
        Set(pawn.workSettings,"priorities",new DefMap<WorkTypeDef,int>());
        ((DefMap<WorkTypeDef,int>)typeof(Pawn_WorkSettings).GetField("priorities",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(pawn.workSettings)!)[work]=3;
        pawn.health = new Pawn_HealthTracker(pawn);
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var conscious=PawnCapacityDefOf.Consciousness;
        if(conscious==null) { conscious=new PawnCapacityDef {defName="Consciousness",minForCapable=.1f}; DefDatabase<PawnCapacityDef>.Add(conscious); PawnCapacityDefOf.Consciousness=conscious; }
        var cacheField=typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var cache=Activator.CreateInstance(cacheField.FieldType)!;
        var entry=cacheField.FieldType.GetProperty("Item",new[]{typeof(PawnCapacityDef)})!.GetValue(cache,new object[]{conscious})!;
        Set(entry,"status",Enum.Parse(entry.GetType().GetField("status")!.FieldType,"Cached")); Set(entry,"value",1f); cacheField.SetValue(pawn.health.capacities,cache);
        pawn.needs = new Pawn_NeedsTracker(pawn);
        pawn.mindState = (Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));
        pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        pawn.story.traits.allTraits.Add(new Trait(new TraitDef { defName = "Faceless" }));
        pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "ageBiologicalTicksInt", 72000000L);
        Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "positionInt", siphon.parent.InteractionCell);
        Set(pawn, "comps", new List<ThingComp> { new CompAbilityUserMagic { parent = pawn } });
        var need = (Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)); Set(need, "pawn", pawn);
        need.def = new NeedDef { defName = "TM_Mana" }; pawn.needs.AllNeeds.Add(need); need.CurLevel = 1f;
        return (siphon, tank, pawn, need);
    }
    static bool Begin(ThingComp c, Pawn p) => (bool)Call(c, "CanBeginDonation", p, false)!;
    static float Donate(ThingComp c, Pawn p, float paid = 0f) => (float)Call(c, "Donate", p, 1f, paid)!;
    static void Near(float expected, float actual, string message) => Check.Equal(true, Math.Abs(expected - actual) < .00002f, message + $" expected {expected}, actual {actual}");
    static void SiphonPayments()
    {
        var (s, t, p, n) = SiphonSetup();
        var work=DefDatabase<WorkTypeDef>.GetNamed("TM_Magic");
        var priorities=(DefMap<WorkTypeDef,int>)typeof(Pawn_WorkSettings).GetField("priorities",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p.workSettings)!;
        priorities[work]=0; Check.Equal(false,Begin(s,p),"disabled TM_Magic never starts"); priorities[work]=3;
        n.CurLevel = .999f; Check.Equal(false, Begin(s,p), "not full cannot start");
        n.CurLevel = 1f; Check.Equal(true, Begin(s,p), "full mage can begin");
        Near(.01f, Donate(s,p), "one pulse personal payment"); Near(.99f,n.CurLevel,"native need debited"); Near(1f,Mana(t),"one unit stored");
        Near(0f,Donate(s,p,.01f),"same tick cannot duplicate payment");
        Check.Equal(false, Begin(s,p),"new visit requires full again");
        Set(Find.TickManager,"ticksGameInt",60);
        Near(.01f,Donate(s,p,.01f),"existing visit continues below full"); Near(2f,Mana(t),"second paid unit stored");
        p.drafter = new Pawn_DraftController(p); Set(p.drafter,"draftedInt",true);
        Set(Find.TickManager,"ticksGameInt",120); Near(0f,Donate(s,p,.02f),"drafted cannot pay");
        Set(p.drafter,"draftedInt",false); Set(p.health,"healthState",PawnHealthState.Down);
        Near(0f,Donate(s,p,.02f),"downed cannot pay");
    }
    static void SiphonBuffer()
    {
        var (s,t,p,n)=SiphonSetup();
        var bank=t.parent.AllComps.Single(x=>x.GetType().Name=="CompMagofuelReservoir"); Set(bank,"storedMana",999.5f);
        Near(.01f,Donate(s,p),"already started pulse is fully paid"); Near(.5f,(float)Get(s,"BufferedMana"),"excess has one storage owner");
        n.CurLevel=1f; Check.Equal(false,Begin(s,p),"full tanks never create jobs despite hidden buffer space");
        Check.Equal(false,(bool)Call(s,"CanContinueDonation",p,1f,.01f,true)!,"full tanks stop existing visit");
        Set(bank,"storedMana",999f);
        var flick=new CompFlickable {parent=s.parent}; Set(flick,"switchOnInt",false); s.parent.AllComps.Add(flick);
        Set(Find.TickManager,"ticksGameInt",60); s.CompTick(); Near(.5f,(float)Get(s,"BufferedMana"),"off siphon retains buffered mana");
        s.parent.AllComps.Remove(flick); Set(t.parent,"factionInt",new Faction {def=new FactionDef()});
        Set(Find.TickManager,"ticksGameInt",120); s.CompTick(); Near(.5f,(float)Get(s,"BufferedMana"),"foreign tank cannot consume buffer");
        Set(t.parent,"factionInt",faction); Set(t.parent,"positionInt",new IntVec3(40,0,40));
        Set(Find.TickManager,"ticksGameInt",180); s.CompTick(); Near(.5f,(float)Get(s,"BufferedMana"),"disconnected tank cannot consume buffer");
        Set(t.parent,"positionInt",new IntVec3(12,0,10)); Set(s.parent,"factionInt",new Faction {def=new FactionDef()});
        Set(Find.TickManager,"ticksGameInt",240); s.CompTick(); Near(.5f,(float)Get(s,"BufferedMana"),"captured siphon cannot drain buffer");
        Set(s.parent,"factionInt",faction); Set(Find.TickManager,"ticksGameInt",300); s.CompTick();
        Near(0f,(float)Get(s,"BufferedMana"),"reconnected buffer drains automatically"); Near(999.5f,Mana(t),"drained amount conserved"); Near(1f,n.CurLevel,"autonomous drain never charges donor");
        Check.Equal(100f,(float)s.GetType().GetField("BufferCapacity")!.GetRawConstantValue()!,"hidden capacity is exactly100 network mana");
        Set(s,"bufferedMana",100f); Set(bank,"storedMana",999.5f); Set(Find.TickManager,"ticksGameInt",360);
        Near(0f,Donate(s,p),"old buffer fills tank before considering fresh donor debit");
        Near(99.5f,(float)Get(s,"BufferedMana"),"buffer first transfers exact space"); Near(1000f,Mana(t),"buffer first fills physical tank"); Near(1f,n.CurLevel,"full tank after buffer drain costs no personal mana");
        s.CompTick(); Near(99.5f,(float)Get(s,"BufferedMana"),"duplicate tick cannot replay buffer drain");
        Set(bank,"storedMana",0f); Set(Find.TickManager,"ticksGameInt",6000); s.CompTick();
        Near(10f,Mana(t),"long gap drains only one bounded network pulse without catchup"); Near(89.5f,(float)Get(s,"BufferedMana"),"bounded drain preserves remaining buffer");
        s.CompTick(); Near(10f,Mana(t),"same tick drain cannot replay");
    }
    static void SiphonCapacityReduction()
    {
        var (s,t,p,n)=SiphonSetup();
        var magic=p.GetCompAbilityUserMagic();
        magic.maxMP=2f; n.CurLevel=3f; magic.maxMP=1f;
        Near(3f,n.CurLevel,"native capacity reduction leaves old current level until next setter");
        Near(0f,Donate(s,p),"siphon refuses payment that would clamp unrelated excess mana");
        Near(3f,n.CurLevel,"rejected debit preserves all old personal mana");
        Near(0f,Mana(t),"rejected debit creates no tank charge");
    }
    static void SiphonGravcore()
    {
        var oldLanguage=LanguageDatabase.activeLanguage; var oldDefault=LanguageDatabase.defaultLanguage;
        try
        {
            var language=(LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage)); language.folderName="English"; Set(language,"dataIsLoaded",true); Set(language,"workerInt",new LanguageWorker_English());
            language.keyedReplacements=new Dictionary<string,LoadedLanguage.KeyedReplacement> { ["Gravship"]=new LoadedLanguage.KeyedReplacement {key="Gravship",value="Gravship"} };
            LanguageDatabase.activeLanguage=LanguageDatabase.defaultLanguage=language;
            SiphonGravcoreInner();
        }
        finally { LanguageDatabase.activeLanguage=oldLanguage; LanguageDatabase.defaultLanguage=oldDefault; }
    }
    static void SiphonGravcoreInner()
    {
        var (s,t,p,n)=SiphonSetup();
        var old=t.parent;
        var engine=(ThingWithComps)Activator.CreateInstance(T("Building_MagicalGravEngine"))!;
        engine.def=old.def; engine.def.defName="TM_TestMagicalGravcore"; engine.thingIDNumber=++id;
        Set(engine,"positionInt",old.Position); Set(engine,"mapIndexOrState",(sbyte)0); Set(engine,"factionInt",faction);
        var comps=old.AllComps.ToList(); Set(engine,"comps",comps); foreach(var comp in comps) comp.parent=engine;
        Check.Equal(true,Begin(s,p),"actual magical gravcore storage is eligible without reservoir def name");
        Near(.01f,Donate(s,p),"gravcore transfer pays native personal mana");
        Near(.99f,n.CurLevel,"gravcore payment conserved"); Near(1f,Mana(t),"actual gravcore bank receives charge");
        var bank=comps.Single(x=>x.GetType().Name=="CompMagofuelReservoir"); Set(bank,"storedMana",1000f);
        n.CurLevel=1f; Check.Equal(false,Begin(s,p),"full magical gravcore cannot create another job");
    }
    static void SiphonLimits()
    {
        var (s,t,p,n)=SiphonSetup();
        s.GetType().GetProperty("PersonalReserve")!.SetValue(s,1f); n.CurLevel=1.5f;
        Check.Equal(false,Begin(s,p),"100 percent reserve disables work even overfilled"); Near(0f,Donate(s,p),"100 percent reserve never pays overfill");
        s.GetType().GetProperty("PersonalReserve")!.SetValue(s,.2f);
        s.GetType().GetProperty("MaximumDonation")!.SetValue(s,0f); n.CurLevel=1f;
        Check.Equal(false,Begin(s,p),"zero donation disables work"); Near(0f,Donate(s,p),"zero donation never pays");
        s.GetType().GetProperty("MaximumDonation")!.SetValue(s,.8f);
        n.CurLevel=.2f; Near(0f,Donate(s,p),"reserve protects personal mana");
        n.CurLevel=1f; Near(0f,Donate(s,p,.8f),"visit ceiling cannot be exceeded even after regeneration");
        s.GetType().GetProperty("MaximumDonation")!.SetValue(s,.1f);
        Near(0f,Donate(s,p,.1f),"configured visit ceiling applies");
        s.GetType().GetProperty("PersonalReserve")!.SetValue(s,.6f);
        n.CurLevel=.6f; Near(0f,Donate(s,p),"configured reserve applies");
        n.CurLevel=1f;
        var bank=t.parent.AllComps.Single(c=>c.GetType().Name=="CompMagofuelReservoir");
        Set(bank,"storedMana",999.5f);
        Near(.01f,Donate(s,p),"in-flight pulse pays full amount"); Near(.99f,n.CurLevel,"overflow remains paid"); Near(1000f,Mana(t),"partial tank acceptance fills exactly"); Near(.5f,(float)Get(s,"BufferedMana"),"in-flight remainder retained in hidden buffer");
    }
    static void SiphonTargets()
    {
        var (s,t,p,n)=SiphonSetup();
        var bank=t.parent.AllComps.Single(x=>x.GetType().Name=="CompMagofuelReservoir");
        t.GetType().GetProperty("ManaTarget")!.SetValue(t,.1f); Set(bank,"storedMana",99.5f);
        Near(.01f,Donate(s,p),"receive target takes part of complete paid pulse"); Near(100f,Mana(t),"receive target ceiling never overshoots"); Near(.5f,(float)Get(s,"BufferedMana"),"receive target overflow retained");
        n.CurLevel=1f; Check.Equal(false,Begin(s,p),"receive target reached prevents another visit");
        t.GetType().GetProperty("ManaMode")!.SetValue(t,Enum.Parse(T("ManaNetworkMode"),"Send"));
        Check.Equal(true,Begin(s,p),"send mode ignores stale receive target and uses physical tank capacity");
        Set(Find.TickManager,"ticksGameInt",60); s.CompTick(); Near(100.5f,Mana(t),"send tank first receives already paid buffer"); Near(1f,n.CurLevel,"buffer drain costs no personal mana");
        Set(Find.TickManager,"ticksGameInt",120); Near(.01f,Donate(s,p),"send tank accepts paid pulse above stale receive target"); Near(101.5f,Mana(t),"send deposits above receive target");
        n.CurLevel=1f; Set(bank,"storedMana",0f);
        t.GetType().GetProperty("ManaMode")!.SetValue(t,Enum.Parse(T("ManaNetworkMode"),"Off"));
        Check.Equal(false,Begin(s,p),"off tank rejects job");
        t.GetType().GetProperty("ManaMode")!.SetValue(t,Enum.Parse(T("ManaNetworkMode"),"Receive"));
        var flick=new CompFlickable {parent=s.parent}; Set(flick,"switchOnInt",false); s.parent.AllComps.Add(flick);
        Check.Equal(false,Begin(s,p),"switched off siphon rejects job"); s.parent.AllComps.Remove(flick);
        t.parent.def.defName="ConsumerWithStorage";
        Check.Equal(false,Begin(s,p),"consumer is not a direct tank target");
        t.parent.def.defName="TM_MagofuelReservoir"; Set(t.parent,"factionInt",new Faction {def=new FactionDef()});
        Check.Equal(false,Begin(s,p),"foreign tank rejected");
        Set(t.parent,"factionInt",faction); Set(t.parent,"positionInt",new IntVec3(40,0,40));

        Check.Equal(false,Begin(s,p),"disconnected tank rejected"); Near(0f,Donate(s,p),"disconnected does not pay"); Near(1f,n.CurLevel,"denials retain personal mana");
    }
    static Toil SiphonToil(JobDriver driver) => ((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).Last();
    static JobDriver SiphonRoundTrip(RuntimeFixture f,JobDriver source,string name)
    {
        string path=Path.Combine(f.WorkingDirectory,name+".xml"); var saved=source;
        try { Scribe.saver.InitSaving(path,"regression"); Scribe_Deep.Look(ref saved,"driver"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        JobDriver loaded=null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded,"driver"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        loaded.pawn=source.pawn; loaded.job=source.job; return loaded;
    }
    static void SiphonDriver(RuntimeFixture f)
    {
        var (s,t,p,n)=SiphonSetup();
        var driver=(JobDriver)Activator.CreateInstance(T("JobDriver_DonateManaAtSiphon"))!;
        driver.pawn=p; driver.job=new Job(new JobDef {defName="RegressionSiphon"},s.parent);
        Check.Equal(true,driver.TryMakePreToilReservations(false),"native reservation acquired before visit");
        var toil=SiphonToil(driver); toil.initAction(); toil.tickAction(); Near(1f,n.CurLevel,"initial callbacks never pay early");
        driver=SiphonRoundTrip(f,driver,"siphon-before"); toil=SiphonToil(driver); toil.initAction(); toil.tickAction(); Near(1f,n.CurLevel,"load before deadline does not pay");
        Set(Find.TickManager,"ticksGameInt",60); toil.tickAction(); Near(.99f,n.CurLevel,"deadline pays once"); toil.tickAction(); Near(.99f,n.CurLevel,"duplicate job callback blocked");
        driver=SiphonRoundTrip(f,driver,"siphon-after");
        Near(.01f,(float)driver.GetType().GetField("paidThisVisit",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!,"saved visit expenditure retained");
        Near(1f,(float)driver.GetType().GetField("startCapacity",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!,"saved initial capacity retained");
        toil=SiphonToil(driver); toil.initAction(); toil.tickAction(); Near(.99f,n.CurLevel,"load after pulse cannot replay");
        Set(Find.TickManager,"ticksGameInt",120); toil.tickAction(); Near(.98f,n.CurLevel,"saved visit continues without full mana");
        driver.Cleanup(JobCondition.InterruptForced); Near(.98f,n.CurLevel,"cancellation cannot pay or refund");
        Near(2f,Mana(t),"two actual driver payments conserved");
    }
    public sealed class SiphonSettingsSave : IExposable
    {
        public ThingComp Comp=(ThingComp)Activator.CreateInstance(T("CompManaSiphon"))!;
        public void ExposeData() => Comp.PostExposeData();
    }
    static void SiphonSave(RuntimeFixture f)
    {
        var (s,t,p,n)=SiphonSetup(); var bank=t.parent.AllComps.Single(x=>x.GetType().Name=="CompMagofuelReservoir"); Set(bank,"storedMana",999.5f); Donate(s,p); s.CompTick(); var saved=new SiphonSettingsSave {Comp=s};
        saved.Comp.GetType().GetProperty("PersonalReserve")!.SetValue(saved.Comp,.6f);
        saved.Comp.GetType().GetProperty("MaximumDonation")!.SetValue(saved.Comp,.3f);
        string path=Path.Combine(f.WorkingDirectory,"siphon-settings.xml");
        try { Scribe.saver.InitSaving(path,"regression"); Scribe_Deep.Look(ref saved,"settings"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        SiphonSettingsSave loaded=null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded,"settings"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        loaded.Comp.parent=s.parent;
        Near(.5f,(float)Get(loaded.Comp,"BufferedMana"),"real Scribe preserves paid overflow");
        Set(bank,"storedMana",999f);
        Near(0f,Donate(loaded.Comp,p),"component save retains cooldown against reentry");
        loaded.Comp.CompTick(); Near(.5f,(float)Get(loaded.Comp,"BufferedMana"),"saved drain deadline prevents early replay"); Near(999f,Mana(t),"no early postload buffer credit");
        Near(.6f,(float)Get(loaded.Comp,"PersonalReserve"),"reserve saved"); Near(.3f,(float)Get(loaded.Comp,"MaximumDonation"),"visit maximum saved");
        Set(bank,"storedMana",999f); Set(Find.TickManager,"ticksGameInt",60); loaded.Comp.CompTick();
        Near(0f,(float)Get(loaded.Comp,"BufferedMana"),"loaded buffer drains when space returns"); Near(999.5f,Mana(t),"loaded drain conserved"); Near(.99f,n.CurLevel,"loaded drain has no new personal payment");
    }
}














