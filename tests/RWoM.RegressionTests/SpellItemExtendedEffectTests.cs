using System.Xml.Linq;
using Verse;
using TorannMagic.SpellItems;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using System.Reflection;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    static IEnumerable<TestCase> ExtendedCases()
    {
        yield return new("SpellItemEffects.Extended.CatalogComplete", f =>
        {
            Check.Equal(0, (int)SpellItemKind.Fireball, "saved enum unchanged");
            Check.Equal(1, (int)SpellItemKind.Heal, "saved enum unchanged");
            Check.Equal(2, (int)SpellItemKind.Blink, "saved enum unchanged");
            foreach (var name in new[] { "Lightning", "Regeneration", "CureDisease", "Purify", "Soothe", "Portal", "Resurrection" })
                Check.Equal(true, Enum.TryParse<SpellItemKind>(name, out _), name + " fixed effect supported");
            var xml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/SpellItems/SpellItems.xml"));
            var defs = xml.Root!.Elements("ThingDef").Select(x => (string?)x.Element("defName")).Where(x => x != null).ToArray();
            Check.Equal(10, defs.Count(x => x!.StartsWith("TM_CastScroll_")), "ten scrolls");
            Check.Equal(9, defs.Count(x => x!.StartsWith("TM_SpellStaff_")), "nine staffs");
            Check.Equal(false, defs.Contains("TM_SpellStaff_Resurrection"), "no resurrection staff");
        });
        yield return new("SpellItemEffects.Extended.CureRejectsHealthyAndUnknownDisease", f => World(f, w =>
        {
            var props = Props(Enum.Parse<SpellItemKind>("CureDisease"));
            Check.NotNull(SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "healthy target rejected");
            w.Patient.health.hediffSet.hediffs.Add(new Hediff { pawn = w.Patient, def = new HediffDef { defName = "UnknownModDisease" } });
            Check.NotNull(SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "unknown effects never removed indiscriminately");
            w.Patient.health.hediffSet.hediffs.Add(new Hediff { pawn = w.Patient, def = new HediffDef { defName = "Flu" } });
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "known disease eligible");
        }));
        foreach (bool purify in new[] { false, true })
        yield return new("SpellItemEffects.Extended.TreatmentRemovesOne." + purify, f => World(f, w =>
        {
            using var boundaries = w.HealingBoundaries();
            var first = purify ? Injury(w.Patient, Part("Scar"), 9, true)
                : new Hediff { pawn = w.Patient, def = new HediffDef { defName = "Flu" } };
            if (!purify) w.Patient.health.hediffSet.hediffs.Add(first);
            var other = new Hediff { pawn = w.Patient, def = new HediffDef { defName = "UnknownModDisease" } };
            w.Patient.health.hediffSet.hediffs.Add(other);
            var props = Props(purify ? SpellItemKind.Purify : SpellItemKind.CureDisease);
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "actual health removal succeeds");
            Check.Equal(false, w.Patient.health.hediffSet.hediffs.Contains(first), "intended condition removed");
            Check.Equal(true, w.Patient.health.hediffSet.hediffs.Contains(other), "unknown condition retained");
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "no effect left means no successful cast");
        }));
        yield return new("SpellItemEffects.Extended.RegenerationPulsesBoundedNaturalHealing", f => World(f, w =>
        {
            using var boundaries = w.HealingBoundaries();
            var injury = Injury(w.Patient, Part("Wound"), 80);
            var scar = Injury(w.Patient, Part("Scar"), 10, true);
            var effect = new Hediff_SpellItemRegeneration { pawn = w.Patient };
            effect.TickInterval(119); Check.Equal(80f, injury.Severity, "no early pulse");
            effect.TickInterval(1); Check.Equal(75f, injury.Severity, "one pulse at two seconds");
            effect.TickInterval(1080); Check.Equal(30f, injury.Severity, "all ten pulses total fifty");
            effect.TickInterval(1200); Check.Equal(30f, injury.Severity, "expired effect cannot heal again");
            Check.Equal(10f, scar.Severity, "scar untouched");
            Check.Equal(true, effect.ShouldRemove, "temporary effect expires");
        }));
        yield return new("SpellItemEffects.Extended.PurifyDoesNotRemoveMissingPartsOrLuciferium", f => World(f, w =>
        {
            w.Patient.health.hediffSet.hediffs.Add(new Hediff_MissingPart { pawn = w.Patient, def = new HediffDef { defName = "MissingBodyPart" } });
            w.Patient.health.hediffSet.hediffs.Add(new Hediff_Addiction { pawn = w.Patient, def = new HediffDef { defName = "LuciferiumAddiction" } });
            Check.Equal<Hediff?>(null, SpellItemEffects.TreatmentTarget(w.Patient, true), "unsupported alterations retained");
        }));
        yield return new("SpellItemEffects.Extended.LightningUsesRealProjectileLaunch", f => World(f, w =>
        {
            using var boundaries = w.LaunchBoundaries();
            var def = ProjectileDef(); def.defName = "TM_ItemLightningProjectile"; def.thingClass = typeof(Bullet);
            using var definitions = new DefinitionScope<ThingDef>(def);
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Lightning)), "bolt launches without magic data");
            var shot = w.Projectiles.Single();
            Check.Same(w.Caster, shot.Launcher, "real launch owns caster");
            Check.Same(w.Patient, shot.usedTarget.Thing, "real native target");
            w.Wall();
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Lightning)), "wall prevents new launch");
            Check.Equal(1, w.Projectiles.Count, "no duplicate after rejected shot");
        }));
        yield return new("SpellItemEffects.Extended.PortalRejectsSameBlockedFoggedOccupiedCells", f => World(f, w =>
        {
            var entrance = new IntVec3(5, 0, 6); var exit = new IntVec3(15, 0, 6);
            Check.Equal(true, SpellItemPortal.ValidDestination(w.Map, entrance, new GlobalTargetInfo(exit, w.Map)), "revealed empty endpoint accepted");
            Check.Equal(false, SpellItemPortal.ValidDestination(w.Map, entrance, new GlobalTargetInfo(entrance, w.Map)), "same cell rejected");
            Check.Equal(false, SpellItemPortal.ValidCell(w.Map, w.Patient.Position), "occupied cell rejected");
            w.Fog(exit, true); Check.Equal(false, SpellItemPortal.ValidCell(w.Map, exit), "fog rejected"); w.Fog(exit, false);
            w.Building(exit); Check.Equal(false, SpellItemPortal.ValidCell(w.Map, exit), "building rejected");
            Check.Equal(false, SpellItemPortal.ValidDestination(w.Map, entrance, GlobalTargetInfo.Invalid), "cancelled destination rejected");
        }));
        yield return new("SpellItemEffects.Extended.RegenerationAddsRealHediffAndCannotStack", f => World(f, w =>
        {
            using var boundaries = w.HealingBoundaries();
            var def = new HediffDef { defName = "TM_ItemRegeneration", hediffClass = typeof(Hediff_SpellItemRegeneration), initialSeverity = 1 };
            using var definitions = new DefinitionScope<HediffDef>(def);
            var injury = Injury(w.Patient, Part("Wound"), 40);
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Regeneration)), "actual hediff installed");
            Check.Equal(40f, injury.Severity, "no instant healing before pulse");
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Regeneration)), "active effect rejects duplicate");
            var effect = (Hediff_SpellItemRegeneration)w.Patient.health.hediffSet.GetFirstHediffOfDef(def);
            effect.TickInterval(120); Check.Equal(35f, injury.Severity, "installed effect really heals");
        }));
        yield return new("SpellItemEffects.Extended.SootheRealMemoryTemporaryNonStacking", f => World(f, w =>
        {
            using var boundaries = w.HealingBoundaries();
            boundaries.Prefix(typeof(ThoughtUtility).GetMethod(nameof(ThoughtUtility.CanGetThought), [typeof(Pawn), typeof(ThoughtDef), typeof(bool)])!, nameof(AllowFixtureThought));
            var def = new ThoughtDef { defName = "TM_ItemSoothed", thoughtClass = typeof(Thought_Memory), durationDays = 1, stackLimit = 1, showBubble = false,
                stages = [new ThoughtStage { label = "soothed", baseMoodEffect = 6 }] };
            using var definitions = new DefinitionScope<ThoughtDef>(def);
            w.Patient.needs = new Pawn_NeedsTracker(w.Patient);
            w.Patient.AllComps.Add(new SpellFixtureComp { parent = w.Patient, props = new CompProperties() });
            var mood = new Need_Mood(w.Patient);
            w.Patient.needs.mood = mood;
            Set(w.Patient.needs, "needs", new List<Need> { mood });
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Soothe)), "native memory gain");
            var memory = mood.thoughts.memories.Memories.Single();
            Check.Same(def, memory.def, "fixed thought installed");
            Check.Equal(6f, memory.MoodOffset(), "modest fixed mood bonus");
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Soothe)), "cannot stack or refresh");
            memory.age = 60001; mood.thoughts.memories.MemoryThoughtInterval();
            Check.Equal(0, mood.thoughts.memories.Memories.Count, "native expiration removes temporary mood");
        }));
        yield return new("SpellItemEffects.Extended.RegenerationRealScribeDoesNotRepeatPulses", f => World(f, w =>
        {
            using var boundaries = w.HealingBoundaries();
            var def = new HediffDef { defName = "TM_ItemRegeneration", hediffClass = typeof(Hediff_SpellItemRegeneration), initialSeverity = 1 };
            using var definitions = new DefinitionScope<HediffDef>(def);
            var injury = Injury(w.Patient, Part("Wound"), 80);
            Hediff_SpellItemRegeneration? effect = new() { pawn = w.Patient, def = def };
            effect.TickInterval(350); Check.Equal(70f, injury.Severity, "two pulses before saving");
            string path = Path.Combine(f.WorkingDirectory, "item-regeneration.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref effect, "effect"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            effect = null;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref effect, "effect"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            effect!.pawn = w.Patient;
            effect.TickInterval(9); Check.Equal(70f, injury.Severity, "saved partial interval retained");
            effect.TickInterval(1); Check.Equal(65f, injury.Severity, "third pulse only once");
            effect.TickInterval(840); Check.Equal(30f, injury.Severity, "ten total pulses across save/load");
        }));
        foreach (bool failSecond in new[] { false, true })
        yield return new("SpellItemEffects.Extended.PortalPairAtomicCreation." + failSecond, f => World(f, w =>
        {
            using var boundaries = new Patches();
            boundaries.Prefix(typeof(Faction).GetProperty(nameof(Faction.OfPlayer))!.GetMethod!, nameof(FixturePlayerFaction));
            boundaries.Prefix(typeof(Find).GetProperty(nameof(Find.FactionManager))!.GetMethod!, nameof(FixtureFactions));
            boundaries.Prefix(typeof(GenSpawn).GetMethods().Single(m => m.Name == nameof(GenSpawn.Spawn) && m.GetParameters().Length == 7 && m.GetParameters()[0].ParameterType == typeof(Thing)), nameof(SpawnPortalBoundary));
            boundaries.Prefix(typeof(Building).GetMethod(nameof(Thing.Destroy))!, nameof(DestroyPortalBoundary));
            boundaries.Prefix(typeof(Log).GetMethod(nameof(Log.Warning), [typeof(string)])!, nameof(PortalExpectedWarning));
            var def = Blank<ThingDef>(); def.defName = "TM_ItemPortal"; def.thingClass = typeof(Building_SpellItemPortal);
            def.category = ThingCategory.Building; def.size = new IntVec2(1,1); def.comps = []; def.destroyable = true;
            using var definitions = new DefinitionScope<ThingDef>(def);
            portalSpawns.Clear(); portalFailure = failSecond;
            w.WarningToCapture = "[RWoM.SpellItems] Item portal creation interrupted: System.InvalidOperationException: fixture second portal spawn";
            // The renderer/world registration boundary is headless; pair creation,
            // bidirectional linking and rollback are production code.
            bool made = SpellItemPortal.TryCreate(w.Caster, new IntVec3(5,0,6), new GlobalTargetInfo(new IntVec3(15,0,6), w.Map));
            Check.Equal(!failSecond, made, "only complete pair succeeds");
            if (failSecond)
            {
                Check.Equal(true, portalSpawns.All(p => p.Destroyed), "partial pair rolled back");
                Check.Equal(1, w.CapturedWarnings.Count, "exact injected failure diagnosed");
            }
            else
            {
                Check.Equal(2, portalSpawns.Count, "exactly two portals");
                Check.Same(portalSpawns[1], portalSpawns[0].Partner, "forward link");
                Check.Same(portalSpawns[0], portalSpawns[1].Partner, "return link");
                Check.Equal(60100, portalSpawns[0].ExpiresAt, "one day lifetime");
                Check.Equal(true, portalSpawns.All(p => p.Active), "both endpoints usable");
                foreach (var portal in portalSpawns) Set(portal, "factionInt", null);
                var jobDef = new JobDef { defName = "SpellItemPortalSavedJob", driverClass = typeof(JobDriver_SpellItemCast) };
                using var jobDefinitions = new DefinitionScope<JobDef>(jobDef);
                PortalSaveEnvelope? saved = new() { Portals = portalSpawns.ToList(), Pending = new Job(jobDef) { globalTarget = new GlobalTargetInfo(new IntVec3(16,0,6), w.Map) } };
                string path = Path.Combine(f.WorkingDirectory, "item-portals.xml");
                try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "portals"); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                saved = null;
                try
                {
                    Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref saved, "portals");
                    var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                    directory.RegisterLoaded(w.Map);
                    Scribe.loader.FinalizeLoading();
                }
                finally { Scribe.ForceStop(); }
                RuntimeLog.AssertClean();
                Check.Same(saved!.Portals[1], saved.Portals[0].Partner, "Scribe resolves actual forward portal reference");
                Check.Same(saved.Portals[0], saved.Portals[1].Partner, "Scribe resolves return reference");
                Check.Equal(60100, saved.Portals[0].ExpiresAt, "Scribe retains expiry, not a fresh day");
                Check.Equal(new IntVec3(16,0,6), saved.Pending.globalTarget.Cell, "queued portal destination survives native Job Scribe");
                Check.Same(w.Map, saved.Pending.globalTarget.Map, "queued destination map resolves");
                boundaries.Prefix(typeof(Thing).GetProperty(nameof(Thing.Position))!.SetMethod!, nameof(AssignPosition));
                boundaries.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.Notify_Teleported))!, nameof(NotifyTeleport));
                w.Map.thingGrid.Deregister(w.Caster); Set(w.Caster, "positionInt", portalSpawns[0].Position); w.Map.thingGrid.Register(w.Caster);
                Check.Equal(true, portalSpawns[0].TryTravel(w.Caster), "manual portal passage succeeds");
                Check.Equal(portalSpawns[1].Position, w.Caster.Position, "traveler reaches paired exit");
                Check.Equal(false, w.TeleportEndsJob, "same-map transit does not cancel its own driver");
                Set(Find.TickManager, "ticksGameInt", 60100);
                Check.Equal(false, portalSpawns[0].Active, "expired pair refuses travel before cleanup");
                var tick = typeof(Building_SpellItemPortal).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)!;
                foreach (var portal in portalSpawns) tick.Invoke(portal, null);
                Check.Equal(true, portalSpawns.All(p => p.Destroyed), "both expired endpoints removed");
            }
        }));
        yield return new("SpellItemEffects.Extended.ResurrectionRealCorpseEligibility", f => World(f, w =>
        {
            var patient = w.Patient; patient.AllComps.Add(new SpellFixtureComp { parent = patient, props = new CompProperties() });
            var corpse = FreshCorpse(w, patient);
            RuntimeLog.AssertClean();
            var props = Props(SpellItemKind.Resurrection);
            Check.Same(patient, corpse.InnerPawn, "actual corpse holder owns the original pawn");
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, corpse, props), "fresh corpse accepted");
            var rot = corpse.GetComp<CompRottable>(); Set(rot, "rotProgressInt", 180000f);
            Check.NotNull(SpellItemEffects.TargetRejection(w.Caster, corpse, props), "rotten corpse rejected"); Set(rot, "rotProgressInt", 0f);
            w.Wall(); Check.NotNull(SpellItemEffects.TargetRejection(w.Caster, corpse, props), "wall blocks resurrection");
        }));
        foreach (bool revive in new[] { false, true })
        yield return new("SpellItemEffects.Extended.ResurrectionPaysConfirmedHealthOutcome." + revive, f => World(f, w =>
        {
            using var boundaries = w.HealingBoundaries();
            boundaries.Prefix(typeof(ResurrectionUtility).GetMethod(nameof(ResurrectionUtility.TryResurrectWithSideEffects))!, nameof(RevivalEngineBoundary));
            boundaries.Prefix(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.ShouldBeDead))!, nameof(FixtureNotLethal));
            HeadlessDefOf(typeof(PawnCapacityDefOf));
            var old = PawnCapacityDefOf.Manipulation;
            using var definitions = old == null ? new DefinitionScope<PawnCapacityDef>(new PawnCapacityDef { defName = "Manipulation", minForCapable = .1f }) : null;
            try
            {
                PawnCapacityDefOf.Manipulation = old ?? definitions!.Definition; Capacity(w.Caster, PawnCapacityDefOf.Manipulation);
                w.Caster.inventory = new Pawn_InventoryTracker(w.Caster);
                w.Patient.AllComps.Add(new SpellFixtureComp { parent = w.Patient, props = new CompProperties() });
                var corpse = FreshCorpse(w, w.Patient); var item = Item("resurrection", SpellItemKind.Resurrection, false, 2);
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(item.parent, false), "real inventory owns scrolls");
                revivalSucceeds = revive; revivalCalls = 0;
                int token = item.TryBegin(w.Caster);
                Check.NotEqual(0, token, "caster can reserve resurrection scroll");
                Check.Equal(revive, item.TryCommit(w.Caster, token, corpse), "native true alone cannot prove revival");
                Check.Equal(revive ? 1 : 2, item.parent.stackCount, "only revived pawn pays");
                Check.Equal(revive ? 220 : 0, item.ReadyTick, "cooldown only after revival");
                Check.Equal(1, revivalCalls, "one native invocation");
                Check.Equal(false, item.TryCommit(w.Caster, token, corpse), "cannot replay completion");
                Check.Equal(1, revivalCalls, "no repeated native invocation");
            }
            finally { PawnCapacityDefOf.Manipulation = old; }
        }));
        yield return new("SpellItemEffects.Extended.AllNewItemsPreviewRangeAndSight", f => World(f, w =>
        {
            using var boundaries = PreviewBoundaries();
            foreach (var kind in Enum.GetValues<SpellItemKind>().Where(k => (int)k >= 3))
            {
                previewRings.Clear(); previewLines.Clear();
                var props = Props(kind); props.range = 12;
                SpellItemGizmos.DrawPreview(w.Caster, props, w.Patient);
                Check.Equal(1, previewRings.Count, kind + " draws a range ring, no invented blast");
                Check.Equal(12f, previewRings.Single().radius, kind + " configured range");
                Check.Equal(true, previewRings.Single().predicate(w.Patient.Position), kind + " visible cell included");
            }
            w.Wall();
            foreach (var kind in Enum.GetValues<SpellItemKind>().Where(k => (int)k >= 3))
            {
                previewRings.Clear(); previewLines.Clear(); SpellItemGizmos.DrawPreview(w.Caster, Props(kind), w.Patient);
                Check.Equal(false, previewRings.Single().predicate(w.Patient.Position), kind + " wall clips range contour");
                Check.Equal(SimpleColor.Red, previewLines.Single(), kind + " blocked target red");
            }
        }));
        yield return new("SpellItemEffects.Extended.PortalQueueAcquiresNoEarlyToken", f => World(f, w =>
        {
            using var boundaries = new Patches();
            boundaries.Prefix(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.TryTakeOrderedJob))!, nameof(CapturePortalOrder));
            HeadlessDefOf(typeof(PawnCapacityDefOf)); var old = PawnCapacityDefOf.Manipulation;
            using var capacity = old == null ? new DefinitionScope<PawnCapacityDef>(new PawnCapacityDef { defName = "Manipulation", minForCapable = .1f }) : null;
            using var jobDef = new DefinitionScope<JobDef>(new JobDef { defName = "TM_SpellItemCast", driverClass = typeof(JobDriver_SpellItemCast) });
            try
            {
                PawnCapacityDefOf.Manipulation = old ?? capacity!.Definition; Capacity(w.Caster, PawnCapacityDefOf.Manipulation);
                w.Caster.inventory = new Pawn_InventoryTracker(w.Caster); w.Caster.jobs = new Pawn_JobTracker(w.Caster);
                var item = Item("portal scroll", SpellItemKind.Portal, false, 2);
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(item.parent, false), "physical scroll held");
                portalOrder = null; var entrance = new IntVec3(5,0,6); var destination = new GlobalTargetInfo(new IntVec3(15,0,6), w.Map);
                Check.Equal(false, SpellItemCasting.TryStart(w.Caster, item.parent, entrance), "one-stage portal order rejected");
                Check.Equal(false, SpellItemCasting.TryStartPortal(w.Caster, item.parent, entrance, GlobalTargetInfo.Invalid), "cancelled second stage rejected");
                Check.Equal<Job?>(null, portalOrder, "no queued work after cancellation");
                Check.Equal(true, SpellItemCasting.TryStartPortal(w.Caster, item.parent, entrance, destination), "both chosen endpoints submit work");
                Check.Equal(0, portalOrder!.count, "no token before actual warmup");
                Check.Equal(destination.Cell, portalOrder.globalTarget.Cell, "second endpoint carried by native job");
                Check.Equal(false, item.IsBusy, "queue removal cannot leave item locked");
                Check.Equal(2, item.parent.stackCount, "selecting and queueing spend nothing");
                w.Fog(destination.Cell, true);
                Check.Equal(false, SpellItemCasting.TryStartPortal(w.Caster, item.parent, entrance, destination), "destination revalidated");
            }
            finally { PawnCapacityDefOf.Manipulation = old; portalOrder = null; }
        }));
    }
    static bool AllowFixtureThought(ref bool __result) { __result = true; return false; }
    sealed class SpellFixtureComp : ThingComp { }
    static bool revivalSucceeds; static int revivalCalls;
    static Job? portalOrder;
    static bool CapturePortalOrder(Job job, ref bool __result) { portalOrder = job; __result = true; return false; }
    static bool FixtureNotLethal(ref bool __result) { __result = false; return false; }
    static bool RevivalEngineBoundary(Pawn pawn, ref bool __result)
    {
        // Isolate the engine's world spawning, graphics and random side effects;
        // retain real health revival and corpse ownership. This is an adapter
        // outcome/payment test, not a full running-game resurrection test.
        revivalCalls++;
        if (revivalSucceeds)
        {
            pawn.Corpse.InnerPawn = null;
            pawn.health.Notify_Resurrected(restoreMissingParts: false);
        }
        __result = true; return false;
    }
    static readonly List<Building_SpellItemPortal> portalSpawns = [];
    static bool portalFailure;
    static bool FixturePlayerFaction(ref Faction __result) { __result = active!.Caster.Faction; return false; }
    static bool FixtureFactions(ref FactionManager __result) { __result = new FactionManager(); return false; }
    static bool PortalExpectedWarning(string text)
    {
        if (portalFailure && text.Split('\n')[0].TrimEnd('\r') == "[RWoM.SpellItems] Item portal creation interrupted: System.InvalidOperationException: fixture second portal spawn")
        { active!.CapturedWarnings.Add(text); return false; }
        return true;
    }
    static bool SpawnPortalBoundary(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
    {
        if (newThing is not Building_SpellItemPortal portal) return true;
        portalSpawns.Add(portal);
        if (portalFailure && portalSpawns.Count == 2) throw new InvalidOperationException("fixture second portal spawn");
        Set(portal, "positionInt", loc); Set(portal, "mapIndexOrState", (sbyte)Find.Maps.IndexOf(map));
        map.thingGrid.Register(portal); __result = portal; return false;
    }
    static bool DestroyPortalBoundary(Thing __instance)
    {
        if (__instance is not Building_SpellItemPortal) return true;
        if (__instance.Spawned) __instance.Map.thingGrid.Deregister(__instance);
        Set(__instance, "mapIndexOrState", (sbyte)-2); return false;
    }
    public sealed class PortalSaveEnvelope : IExposable
    {
        public List<Building_SpellItemPortal> Portals = [];
        public Job Pending = null!;
        public void ExposeData()
        {
            Scribe_Collections.Look(ref Portals, "endpoints", LookMode.Deep);
            Scribe_Deep.Look(ref Pending, "pending");
        }
    }
    static Corpse FreshCorpse(WorldState w, Pawn patient)
    {
        var cell = patient.Position;
        patient.def.stackLimit = 1;
        w.Map.thingGrid.Deregister(patient); Set(patient, "mapIndexOrState", (sbyte)-1);
        Set(patient.health, "healthState", PawnHealthState.Dead);
        var def = Blank<ThingDef>(); def.defName = "SpellFixtureCorpse"; def.thingClass = typeof(Corpse);
        def.category = ThingCategory.Item; def.size = new IntVec2(1,1); def.destroyable = true; def.stackLimit = 1;
        var props = new CompProperties_Rottable { daysToRotStart = 2, daysToDessicated = 5 };
        def.comps = [props];
        var corpse = new Corpse { def = def, thingIDNumber = 401, InnerPawn = patient };
        var rot = new CompRottable { parent = corpse }; rot.Initialize(props); Set(corpse, "comps", new List<ThingComp> { rot });
        Set(corpse, "positionInt", cell); Set(corpse, "mapIndexOrState", (sbyte)0); w.Map.thingGrid.Register(corpse);
        return corpse;
    }
}
