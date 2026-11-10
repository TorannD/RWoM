using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthRitualTransactionTests
{
    static readonly Type Transaction = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthRitualTransaction", true)!;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRitualTransaction.NecromancerNeedsPersonalRenunciation", r => Run(r,c =>
        {
            var component = c.Original.GetComp<CompClassProgression>();
            var path = new ClassProgressionState(Vocation.Mage);
            Set(component,"state",path);
            Check.Equal(true,path.TryAddFoundation("rwom.necromancer",DisciplineDomain.Magic),"seed native necromancer");
            var data = c.Original.GetCompAbilityUserMagic().MagicData;
            var power = data.MagicPowersN.First(p=>p.TMabilityDefs.Contains(TorannMagicDefOf.TM_RaiseUndead));
            power.learned=true;
            var priest=data.MagicPowersPR[0]; priest.learned=true; priest.level=2;
            var magic=c.Original.GetCompAbilityUserMagic();
            Set(magic,"powerModifier",2);
            var servant=PawnWithComps(false,false).pawn; magic.supportedUndead.Add(servant);
            int level=data.MagicUserLevel, xp=data.MagicUserXP, points=data.MagicAbilityPoints;
            Check.Equal("Rejected",c.Execute(),"no explicit renunciation");
            var policy = Transaction.Assembly.GetType("TorannMagic.ClassProgression.SpiritNecromancyRenunciation",true)!;
            var confirm = policy.GetMethod("Confirm",BindingFlags.Static | BindingFlags.NonPublic)!;
            confirm.Invoke(null,[c.Job,c.Creator]);
            Check.Equal("Rejected",c.Execute(),"consent belonging to another pawn");
            Check.Equal(true,c.Original.Dead && !c.Corpse.Destroyed && power.learned && path.Find("rwom.necromancer")!=null,"rejection preserves corpse and class");
            confirm.Invoke(null,[c.Job,c.Original]);
            Check.Equal("ConvertedHeld",c.Execute(),"consented original completes paid native conversion");
            Check.Equal(true,path.Find("rwom.necromancer")==null && !power.learned,"only the renounced class is removed");
            Check.Equal(true,priest.learned && priest.level==2,"unrelated paid spell untouched");
            Check.Equal(0,magic.PowerModifier,"former lich charges cleared");
            Check.Equal(true,magic.supportedUndead.Contains(servant),"existing summoned bond preserved");
            Check.Equal(level,data.MagicUserLevel,"overall level preserved");
            Check.Equal(xp,data.MagicUserXP,"overall XP preserved");
            Check.Equal(points,data.MagicAbilityPoints,"available points preserved");
            Check.Equal("ConvertedHeld",c.Execute(),"repeat completion is idempotent");
        }));
        yield return new("SpiritRitualTransaction.NativeConversionPaymentAndDuplicate", r => Run(r, Success));
        yield return new("SpiritRitualTransaction.LegacyLichConsentIsBoundToOrder", r => Run(r,c =>
        {
            var priorDegrees = TorannMagicDefOf.Lich.degreeDatas;
            try
            {
                TorannMagicDefOf.Lich.degreeDatas = [new TraitDegreeData { degree = 0 }];
                var original = c.Original;
                var lich = new Trait(TorannMagicDefOf.Lich);
                original.story.traits.allTraits.Add(lich);
                var confirm = Transaction.Assembly.GetType("TorannMagic.ClassProgression.SpiritNecromancyRenunciation",true)!
                    .GetMethod("Confirm",BindingFlags.Static | BindingFlags.NonPublic)!;
                var abandonedOrder = JobMaker.MakeJob(c.Job.def, c.Circle, c.Corpse);
                confirm.Invoke(null,[abandonedOrder,original]);
                Check.Equal("Rejected",c.Execute(),"old order cannot consent to a replacement ritual");
                Check.Equal(true,original.Dead && original.story.traits.allTraits.Contains(lich),"old class survives refusal");
                confirm.Invoke(null,[c.Job,original]);
                var result = c.Execute();
                RuntimeLog.AssertClean();
                Check.Equal("ConvertedHeld",result,"legacy lich can consent without a new-style discipline");
                Check.Equal(false,original.story.traits.HasTrait(TorannMagicDefOf.Lich),"legacy lich trait removed");
                Check.Equal(InitiationStage.Ascended,c.Creator.GetComp<CompClassProgression>().State.Find("rwom.necromancer").Stage,"caster keeps ascension");
            }
            finally { TorannMagicDefOf.Lich.degreeDatas = priorDegrees; }
        }));
        yield return new("SpiritRitualTransaction.ExactJobReservationAndInterruptedWork", r => Run(r, Rejected));
        yield return new("SpiritRitualTransaction.ExceptionAfterDetachKeepsActualPawn", r => Run(r, ExceptionAfterDetach));
        yield return new("SpiritRitualTransaction.RefusedEscrowRefundAllowsNewJob", r => Run(r, RefusedEscrow));
        yield return new("SpiritRitualTransaction.NativePendingReceiptAndInventoryScribe", r => Run(r, c => Save(r, c, false)));
        yield return new("SpiritRitualTransaction.NativeCompletedReceiptScribe", r => Run(r, c => Save(r, c, true)));
        yield return new("SpiritRitualTransaction.ApothecaryStockThroughConversionAndScribe", r => Run(r, c =>
        {
            var prior=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            try
            {
                var def=new HediffDef{defName="TM_ApothecaryHerbsHD",hediffClass=typeof(Hediff_ApothecaryHerbs),maxSeverity=100};
                DefDatabase<HediffDef>.Clear();
                foreach(var existing in prior.Where(d=>d.defName!=def.defName))DefDatabase<HediffDef>.Add(existing);
                DefDatabase<HediffDef>.Add(def);
                var herbs=new Hediff_ApothecaryHerbs{def=def,pawn=c.Original};
                herbs.Severity=37.25f;Set(herbs,"herbPwr",3);Set(herbs,"herbEff",2);Set(herbs,"herbVer",4);
                c.Original.health.hediffSet.hediffs.Add(herbs);
                Save(r,c,true,herbs);
            }
            finally {DefDatabase<HediffDef>.Clear();foreach(var def in prior)DefDatabase<HediffDef>.Add(def);}
        }));
        yield return new("SpiritRitualTransaction.AliveExceptionIsDormantAndOwned", r => Run(r, AliveException));
        yield return new("SpiritRitualTransaction.RefundDropExceptionRetainsNativeResource", r => Run(r, RefundException));
    }
    static T Blank<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static bool SkipGraphics() => false;
    static void InitializeOverlayGraphics()
    {
        var mods = LoadedModManager.RunningModsListForReading.ToArray(); var patches = new List<Patch>();
        try
        {
            foreach (var method in typeof(MaterialPool).GetMethods().Where(m => m.Name == nameof(MaterialPool.MatFrom)))
                patches.Add(new Patch(method, nameof(SkipGraphics)));
            LoadedModManager.RunningModsListForReading.Clear();
            RuntimeHelpers.RunClassConstructor(typeof(OverlayDrawer).TypeHandle);
        }
        finally
        {
            LoadedModManager.RunningModsListForReading.AddRange(mods);
            foreach (var patch in patches) patch.Dispose();
        }
    }
    internal sealed class Context
    {
        public Pawn Creator = null!, Original = null!;
        public Corpse Corpse = null!;
        public Building_TMMagicCircle Circle = null!;
        public Job Job = null!;
        public Map Map = null!;
        public Thing Magicyte = null!, Silver = null!;
        public List<ThingCountClass> Staged = null!;
        public string Execute(int ticks = 10000, Job? job = null)
        {
            try { return Transaction.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [Creator, Circle, Corpse, Original, job ?? Job, Staged, ticks])!.ToString()!; }
            catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
        }
        public void Reserve()
        {
            Check.Same(Map, Corpse.MapHeld, "carried corpse held map matches reservation map");
            Check.Equal(1, Corpse.stackCount, "native corpse is one reservable item");
            foreach (var entry in new[] { new ThingCountClass(Circle, 1), new ThingCountClass(Corpse, 1) }.Concat(Staged))
                Check.Equal(true, Map.reservationManager.Reserve(Creator, Job, entry.thing, 1, entry.Count), "actual native reservation " + entry.thing.def.defName);
        }
    }
    internal static void Run(RuntimeFixture r, Action<Context> test)
    {
        using var textures = new Patch(typeof(ContentFinder<UnityEngine.Texture2D>).GetMethod("Get", [typeof(string), typeof(bool)])!, nameof(SkipGraphics));
        using var textureCreation = new Patch(typeof(SolidColorMaterials).GetMethod("NewSolidColorTexture", [typeof(UnityEngine.Color)])!, nameof(SkipGraphics));
        using var eligibility = new SpiritRebirthRitualEligibilityTests.Fixture(r);
        var actorMap = eligibility.Caster.Map;
        var priorMagicyte = TorannMagicDefOf.RawMagicyte; var priorSilver = ThingDefOf.Silver;
        try
        {
            SpiritNativeResurrectionTests.WithPreparedCorpse(r, (original, corpse) =>
            {
                Set(Current.Game, "maps", new List<Map> { actorMap });
                var world = Find.World; world.factionManager.AllFactionsListForReading.Add(eligibility.Player);
                world.worldPawns = new RimWorld.Planet.WorldPawns();
                Set(world.factionManager, "ofPlayer", eligibility.Player);
                Set(Current.Game, "questManager", new QuestManager()); Set(Current.Game, "playSettings", new PlaySettings());
                Set(Current.Game, "storyWatcher", new StoryWatcher());
                Set(Current.Game, "hiddenItemsManager", new HiddenItemsManager());
                if (DefDatabase<WorkTypeDef>.DefCount == 0) DefDatabase<WorkTypeDef>.Add(new WorkTypeDef { defName = "SpiritTransactionWork", alwaysStartActive = true });
                Set(original, "factionInt", eligibility.Player);
                original.inventory = new Pawn_InventoryTracker(original);
                var creator = eligibility.Caster; creator.thingIDNumber = 69001; eligibility.Circle.thingIDNumber = 69002;
                creator.inventory = new Pawn_InventoryTracker(creator); creator.jobs = new Pawn_JobTracker(creator);
                actorMap.info = new MapInfo { Size = new IntVec3(30, 1, 30) };
                actorMap.cellIndices = new CellIndices(actorMap); actorMap.thingGrid = new ThingGrid(actorMap);
                actorMap.edificeGrid = new EdificeGrid(actorMap);
                actorMap.spawnedThings = new ThingOwner<Thing>(actorMap);
                eligibility.Circle.def.stackLimit = 1; eligibility.Circle.stackCount = 1;
                Check.Equal(true, actorMap.spawnedThings.TryAdd(creator, false), "native map owns actual spawned creator");
                Check.Equal(true, actorMap.spawnedThings.TryAdd(eligibility.Circle, false), "native map owns actual spawned circle");
                InitializeOverlayGraphics();
                actorMap.overlayDrawer = new OverlayDrawer(); actorMap.listerThings = new ListerThings(ListerThingsUse.Global);
                actorMap.coverGrid = new CoverGrid(actorMap); actorMap.tooltipGiverList = new TooltipGiverList();
                actorMap.dynamicDrawManager = new DynamicDrawManager(actorMap); actorMap.regionGrid = new RegionGrid(actorMap);
                actorMap.regionAndRoomUpdater = new RegionAndRoomUpdater(actorMap);
                actorMap.regionAndRoomUpdater.Enabled = false;
                Set(actorMap.regionAndRoomUpdater, "initialized", true);
                actorMap.regionDirtyer = new RegionDirtyer(actorMap);
                actorMap.gasGrid = new GasGrid(actorMap); actorMap.mapTemperature = new MapTemperature(actorMap);
                actorMap.zoneManager = new ZoneManager(actorMap);
                using var pathGrid = new PathGrid(actorMap, new PathGridDef { defName = "SpiritTransactionPath" });
                actorMap.pathing = Blank<Pathing>(); var pathContext = new PathingContext(actorMap, pathGrid);
                Set(actorMap.pathing, "normal", pathContext); Set(actorMap.pathing, "fenceBlocked", pathContext);
                actorMap.listerHaulables = new ListerHaulables(actorMap); actorMap.attackTargetsCache = new AttackTargetsCache(actorMap);
                actorMap.haulDestinationManager = new HaulDestinationManager(actorMap); actorMap.listerMergeables = new ListerMergeables(actorMap);
                actorMap.reservationManager = new ReservationManager(actorMap); actorMap.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
                Set(eligibility.Circle, "positionInt", new IntVec3(10, 0, 10));
                eligibility.Circle.def.hasInteractionCell = true; eligibility.Circle.def.interactionCellOffset = new IntVec3(0, 0, -1); eligibility.Circle.def.size = new IntVec2(3, 3);
                Set(creator, "positionInt", eligibility.Circle.InteractionCell);
                corpse.holdingOwner.Remove(corpse);
                Check.Equal(true, creator.carryTracker.GetDirectlyHeldThings().TryAdd(corpse, false), "actual selected corpse carried by exact creator");
                Thing Resource(string name, int count, int id)
                {
                    var def = Blank<ThingDef>(); def.defName = name; def.thingClass = typeof(Thing); def.category = ThingCategory.Item;
                    def.stackLimit = 1000; def.destroyable = true; def.drawerType = DrawerType.RealtimeOnly;
                    def.size = new IntVec2(1, 1); def.passability = Traversability.Standable;
                    def.comps = new List<CompProperties>();
                    var item = new Thing { def = def, stackCount = count, thingIDNumber = id };
                    Set(item, "positionInt", new IntVec3(id == 69003 ? 11 : 12, 0, 10)); Set(item, "mapIndexOrState", (sbyte)0);
                    Check.Equal(true, actorMap.spawnedThings.TryAdd(item, false), "native map owns staged resource");
                    actorMap.thingGrid.Register(item); actorMap.listerThings.Add(item);
                    return item;
                }
                var magicyte = Resource("RawMagicyte", 300, 69003); var silver = Resource("Silver", 50, 69004);
                TorannMagicDefOf.RawMagicyte = magicyte.def; ThingDefOf.Silver = silver.def;
                var job = new Job(new JobDef { defName = "SpiritTransactionTest" }, eligibility.Circle, corpse);
                Set(creator.jobs, "curJob", job);
                var context = new Context { Creator = creator, Original = original, Corpse = corpse, Circle = eligibility.Circle, Job = job,
                    Map = actorMap, Magicyte = magicyte, Silver = silver, Staged = [new ThingCountClass(magicyte, 300), new ThingCountClass(silver, 50)] };
                context.Reserve();
                Check.Equal(SpiritRebirthRitualRejection.None, SpiritRebirthRitualEligibility.Rejection(creator, context.Circle, corpse, original), "native preflight validates actual different creator and corpse");
                test(context);
            });
        }
        finally { TorannMagicDefOf.RawMagicyte = priorMagicyte; ThingDefOf.Silver = priorSilver; }
    }
    static void Success(Context c)
    {
        var progression = ClassIdentity.Component(c.Original); var state = progression.State;
        var name = c.Original.Name; var skills = c.Original.skills; var inventory = c.Original.inventory;
        var magic = c.Original.GetComp<CompAbilityUserMagic>();
        var minion = PawnWithComps(false, false).pawn;
        var undead = PawnWithComps(false, false).pawn;
        magic.summonedMinions.Add(minion); magic.supportedUndead.Add(undead);
        var minions = magic.summonedMinions; var supported = magic.supportedUndead;
        string id = c.Original.GetUniqueLoadID();
        Check.Equal("ConvertedHeld", c.Execute(), "real cleanup/native revival/payment succeeds into inactive custody");
        Check.Equal(false, c.Original.Dead, "actual original revived"); Check.Equal(true, c.Corpse.Destroyed, "actual corpse consumed by native resurrection");
        Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "actual original is owned, not a saved reference only");
        Check.Same(c.Creator.carryTracker.GetDirectlyHeldThings(), c.Original.holdingOwner, "native carried ownership reciprocal");
        Check.Equal(id, c.Original.GetUniqueLoadID(), "same original identity"); Check.Same(name, c.Original.Name, "same biography");
        Check.Same(skills, c.Original.skills, "same skills"); Check.Same(state, progression.State, "same class progression"); Check.Same(inventory, c.Original.inventory, "native inventory survived conversion");
        Check.Equal(true, c.Magicyte.Destroyed, "exact 300 Magicyte stack paid"); Check.Equal(true, c.Silver.Destroyed, "exact 50 Silver stack paid");
        Check.Equal(0, inventory.innerContainer.Count, "exact escrow resources consumed");
        Check.Equal(SpiritRebirthForm.Dormant, progression.SpiritRebirth.Form, "paid conversion remains inactive until form pipeline");
        Check.Equal("ConvertedHeld", c.Execute(), "duplicate returns recorded conversion without rerunning");
        Check.Equal(0, inventory.innerContainer.Count, "duplicate cannot debit again");
        Check.Same(minions, magic.summonedMinions, "native conversion retains summoned list owner");
        Check.Same(supported, magic.supportedUndead, "native conversion retains undead list owner");
        Check.Same(minion, magic.summonedMinions.Single(), "conversion and duplicate retain exact existing minion");
        Check.Same(undead, magic.supportedUndead.Single(), "conversion and duplicate retain exact existing undead");
        RuntimeLog.AssertClean();
    }
    static void Rejected(Context c)
    {
        Check.Equal("Rejected", c.Execute(9999), "insufficient actual work cannot cross final boundary");
        c.Magicyte.stackCount++;
        Check.Equal("Rejected", c.Execute(), "merged or oversized staged stack cannot be split by payment");
        c.Magicyte.stackCount--;
        var foreign = new Job(new JobDef { defName = "ForeignSpiritJob" }, c.Circle);
        Check.Equal("Rejected", c.Execute(job: foreign), "foreign job cannot use another job's reservations");
        c.Map.reservationManager.Release(c.Magicyte, c.Creator, c.Job);
        Check.Equal("Rejected", c.Execute(), "missing material reservation rejects all debit");
        Check.Equal(300, c.Magicyte.stackCount, "interruption/rejection preserves magicyte"); Check.Equal(50, c.Silver.stackCount, "interruption/rejection preserves silver");
        Check.Equal(true, c.Original.Dead, "no revival before validation"); Check.Same(c.Original, c.Corpse.InnerPawn, "original stays inside native corpse");
        RuntimeLog.AssertClean();
    }
    static Pawn? injectedPawn;
    static ThingOwner? refusedOwner;
    static Thing? refundThing;
    static bool ThrowDuringRefund(Thing thing)
    {
        if (ReferenceEquals(thing, refundThing)) throw new InvalidOperationException("injected after native refund detachment");
        return true;
    }
    static bool ThrowAfterDetach(Pawn pawn)
    {
        if (ReferenceEquals(pawn, injectedPawn)) throw new InvalidOperationException("injected after native corpse detachment");
        return true;
    }
    static void ThrowAfterRevival(Pawn pawn)
    {
        if (ReferenceEquals(pawn, injectedPawn)) throw new InvalidOperationException("injected after native resurrection completed");
    }
    static bool RefuseAdd(ThingOwner __instance, ref bool __result)
    {
        if (!ReferenceEquals(__instance, refusedOwner)) return true;
        __result = false; return false;
    }
    sealed class Patch : IDisposable
    {
        readonly object harmony; readonly string id = "rwom.spirit-transaction." + Guid.NewGuid();
        public Patch(MethodBase target, string prefix, bool postfix = false)
        {
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthRitualTransactionTests).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, postfix ? null : method, postfix ? method : null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void ExceptionAfterDetach(Context c)
    {
        injectedPawn = c.Original;
        try
        {
            using var patch = new Patch(typeof(PawnComponentsUtility).GetMethod(nameof(PawnComponentsUtility.CreateInitialComponents))!, nameof(ThrowAfterDetach));
            Check.Equal("RecoveryRequired", c.Execute(), "post-detach exception does not claim rejection or completion");
        }
        finally { injectedPawn = null; }
        Check.Equal(true, c.Corpse.Destroyed, "exception happens after actual native corpse destruction");
        Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "actual original retained in native carry owner after exception");
        Check.Equal(350, c.Original.inventory.innerContainer.Sum(t => t.stackCount), "physical escrow remains attached to original");
        Check.Equal("RecoveryRequired", c.Execute(), "retry never repeats irreversible resurrection");
        Check.Equal(350, c.Original.inventory.innerContainer.Sum(t => t.stackCount), "retry never debits again");
        var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check.Equal(1, problems.Count, "one explicit interrupted transaction diagnostic");
        Check.Equal(true, problems[0].Contains("injected after native corpse detachment"), "diagnostic identifies injected boundary"); RuntimeLog.Clear();
    }
    static void RefusedEscrow(Context c)
    {
        refusedOwner = c.Original.inventory.innerContainer;
        try
        {
            using var patch = new Patch(typeof(ThingOwner<Thing>).GetMethod("TryAdd", [typeof(Thing), typeof(bool)])!, nameof(RefuseAdd));
            Check.Equal("Rejected", c.Execute(), "native add refusal refunds actual whole stack before conversion");
        }
        finally { refusedOwner = null; }
        Check.Equal(300, c.Magicyte.stackCount, "refused escrow restored actual source amount");
        Check.Equal(50, c.Silver.stackCount, "not-yet-debited source untouched");
        Check.Equal(0, c.Original.inventory.innerContainer.Count, "no orphan hidden escrow");
        Check.Same(c.Original, c.Corpse.InnerPawn, "original remains in corpse");
        c.Map.reservationManager.ReleaseAllForTarget(c.Magicyte); c.Map.reservationManager.ReleaseAllForTarget(c.Silver);
        c.Map.reservationManager.ReleaseAllForTarget(c.Circle); c.Map.reservationManager.ReleaseAllForTarget(c.Corpse);
        c.Job = new Job(new JobDef { defName = "SpiritTransactionRetry" }, c.Circle, c.Corpse); Set(c.Creator.jobs, "curJob", c.Job); c.Reserve();
        Check.Equal("ConvertedHeld", c.Execute(), "fully refunded failed preattempt permits new ordered job");
        RuntimeLog.AssertClean();
    }
    static void AliveException(Context c)
    {
        injectedPawn = c.Original;
        try
        {
            using var patch = new Patch(typeof(ResurrectionUtility).GetMethod(nameof(ResurrectionUtility.TryResurrect))!, nameof(ThrowAfterRevival), true);
            Check.Equal("RecoveryRequired", c.Execute(), "native success followed by callback failure retains recovery receipt");
        }
        finally { injectedPawn = null; }
        Check.Equal(false, c.Original.Dead, "real native resurrection finished before injected failure");
        Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "living actual original has native ownership");
        Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "pending living original blocked by explicit Dormant marker");
        Check.Equal(350, c.Original.inventory.innerContainer.Sum(t => t.stackCount), "pending original owns all unpaid actual resources");
        Check.Equal("RecoveryRequired", c.Execute(), "failure never repeats resurrection or payment");
        var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check.Equal(1, problems.Count, "one native callback interruption diagnostic");
        Check.Equal(true, problems[0].Contains("injected after native resurrection completed"), "expected precise injected tail failure"); RuntimeLog.Clear();
    }
    static void RefundException(Context c)
    {
        refusedOwner = c.Original.inventory.innerContainer; refundThing = c.Magicyte;
        try
        {
            using var refusal = new Patch(typeof(ThingOwner<Thing>).GetMethod("TryAdd", [typeof(Thing), typeof(bool)])!, nameof(RefuseAdd));
            using var placement = new Patch(typeof(GenPlace).GetMethods().Single(m => m.Name == "TryPlaceThing" && m.GetParameters().Any(p => p.IsOut)), nameof(ThrowDuringRefund));
            Check.Equal("RefundPending", c.Execute(), "native drop interruption remains retryable");
        }
        finally { refusedOwner = null; refundThing = null; }
        Check.Same(c.Creator.inventory.innerContainer, c.Magicyte.holdingOwner, "detached actual resource reacquired by real creator inventory");
        Check.Equal(300, c.Magicyte.stackCount, "no fabricated or lost amount");
        Check.Equal(true, c.Original.Dead, "refund exception never begins revival");
        var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check.Equal(1, problems.Count, "one refund boundary diagnostic");
        Check.Equal(true, problems[0].Contains("injected after native refund detachment"), "expected injected native drop failure"); RuntimeLog.Clear();
        Check.Equal("Rejected", c.Execute(), "retry returns actual held resource to native map");
        Check.Same(c.Map.spawnedThings, c.Magicyte.holdingOwner, "refund retry confirms real map custody");
        RuntimeLog.AssertClean();
    }

    static void Save(RuntimeFixture r, Context c, bool complete, Hediff_ApothecaryHerbs? herbs=null)
    {
        var comp = ClassIdentity.Component(c.Original);
        if (complete) Success(c);
        else
        {
            Set(comp, "state", null);
            ExceptionAfterDetach(c);
            Check.Equal(true, comp.State == null && comp.SpiritRebirth == null, "classic pending receipt has neither progression nor Spirit marker");
        }
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        if(herbs!=null)
        {
            Check.Equal(true,c.Original.health.hediffSet.hediffs.Contains(herbs),"paid ritual retains original herb instance");
            Check.Equal(37.25f,herbs.Severity,"ritual neither refills nor consumes herbs");
        }
        try
        {
            // Match the game's invariant serialization thread, as in other native Scribe fixtures.
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
            // Behavior above ran the real native conversion graph. For this Scribe
            // proof retain the actual Pawn, native health/inventory and receipt;
            // unrelated optional tracker references are outside this save fixture.
            var pawn = c.Original;
            foreach (var field in typeof(Pawn).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (!field.IsInitOnly && field.FieldType.Name.StartsWith("Pawn_") && field.Name != "inventory" && field.Name != "health")
                    field.SetValue(pawn, null);
            pawn.Name = null; pawn.kindDef = null; Set(pawn, "factionInt", null);
            pawn.health.hediffSet.hediffs.RemoveAll(h=>h!=herbs);
            pawn.def.thingClass = typeof(Pawn); pawn.def.category = ThingCategory.Pawn;
            pawn.def.comps = new List<CompProperties> { new CompProperties { compClass = typeof(CompClassProgression) } };
            Set(pawn, "comps", new List<ThingComp> { comp });
            foreach (var def in new[] { pawn.def, c.Magicyte.def, c.Silver.def })
                if (DefDatabase<ThingDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<ThingDef>.Add(def);
            c.Map.uniqueID = 654;
            var path = Path.Combine(r.WorkingDirectory, complete ? "spirit-paid-receipt.xml" : "spirit-pending-receipt.xml");
            var carry = c.Creator.carryTracker;
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref carry, "carry", c.Creator); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            Pawn_CarryTracker loaded = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "carry", c.Creator);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(c.Creator); directory.RegisterLoaded(c.Circle); directory.RegisterLoaded(c.Map);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            var restored = (Pawn)loaded.CarriedThing;
            Check.Equal(pawn.GetUniqueLoadID(), restored.GetUniqueLoadID(), "same saved Pawn identity restored by native owner");
            Check.Same(loaded.GetDirectlyHeldThings(), restored.holdingOwner, "loaded carry owns restored original");
            if(herbs!=null)
            {
                var restoredHerbs=restored.health.hediffSet.hediffs.OfType<Hediff_ApothecaryHerbs>().Single();
                Check.Same(restored,restoredHerbs.pawn,"loaded stock belongs to deep restored soul");
                Check.Equal(37.25f,restoredHerbs.Severity,"loaded stock amount preserved");
                Check.Equal(130f,restoredHerbs.MaxSeverity,"loaded stock capacity preserved");
                foreach(var entry in new[]{("herbPwr",3),("herbEff",2),("herbVer",4)})
                    Check.Equal(entry.Item2,(int)typeof(Hediff_ApothecaryHerbs).GetField(entry.Item1,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(restoredHerbs)!,"loaded herb training "+entry.Item1);
            }
            Check.Equal(complete ? 0 : 350, restored.inventory.innerContainer.Sum(t => t.stackCount), "native inventory serializes exact outstanding escrow");
            foreach (var item in restored.inventory.innerContainer) Check.Same(restored.inventory.innerContainer, item.holdingOwner, "restored escrow has reciprocal native owner");
            var restoredComp = ClassIdentity.Component(restored);
            var receipt = typeof(CompClassProgression).GetProperty("SpiritRitual", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(restoredComp);
            Check.Equal(true, receipt != null, "receipt survives including classic null-state save path");
            Check.Same(restored, Transaction.GetProperty("Original", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(receipt), "receipt original reference resolves to deep owned Pawn");
            c.Original = restored; c.Creator.carryTracker = loaded;
            Check.Equal(complete ? "ConvertedHeld" : "RecoveryRequired", c.Execute(), "saved receipt retry cannot resurrect or debit twice");
            Check.Equal(complete ? 0 : 350, restored.inventory.innerContainer.Sum(t => t.stackCount), "retry preserves native payment state");
            RuntimeLog.AssertClean();
        }
        finally { System.Globalization.CultureInfo.CurrentCulture=culture;DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def); }
    }
}
