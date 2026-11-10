using System.Reflection;
using RimWorld;
using TorannMagic.Odyssey;
using TorannMagic.SpellItems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    public static IEnumerable<TestCase> BreachSealCases()
    {
        foreach (var test in BreachSealPaymentCases()) yield return test;
        yield return new("Odyssey.BreachSeal.ExtendedDurationAppliedWithoutChangingDefinitions", f => SealWorld(f, (w, core, floors) =>
        {
            var def = DefDatabase<ThingDef>.GetNamed(BreachSeal.WallName);
            var props = new CompProperties_DestroyAfterDelay { delayTicks = 60000, destroyMode = DestroyMode.Vanish };
            def.comps.Add(props);
            Set(Find.TickManager, "ticksGameInt", 100);
            using var patch = new Patches();
            patch.Prefix(typeof(BreachSeal).GetMethod(nameof(BreachSeal.SpawnWall))!, nameof(SealSpawnBoundary));
            Check.Equal(true, BreachSeal.TryApply(w.Caster, SealCenter, 105000), "extended walls actually created");
            foreach (var cell in BreachSeal.GetFarEdgeCells(SealCenter, core.OccupiedRect()))
            {
                var wall = cell.GetThingList(w.Map).OfType<Building>().Single();
                var timer = wall.GetComp<CompDestroyAfterDelay>();
                Check.Equal(105100, timer.DestructionTick, "per-wall extended deadline");
                Check.Equal(60000, props.delayTicks, "shared definition remains unchanged for later item casts");
            }
        }));
        foreach (int distance in new[] { 4, 5 })
        yield return new("Odyssey.BreachSeal.NearCorePlatform." + distance, f => SealWorld(f, (w, core, floors) =>
        {
            var center = core.Position + new IntVec3(distance, 0, 0);
            floors.Add(center);
            foreach (var cell in BreachSeal.GetFarEdgeCells(center, core.OccupiedRect())) floors.Add(cell);
            Check.Equal(true, BreachSeal.TryPlan(w.Caster, center, out _, out var reason), "near-core platform has no exclusion zone: " + reason);
            using var boundaries = PreviewBoundaries();
            sealPreviewReasons.Clear(); sealPreviewCells.Clear();
            BreachSeal.DrawPreview(w.Caster, center, 1f);
            Check.Equal(0, sealPreviewReasons.Count, "world rendering never draws IMGUI text");
            BreachSeal.DrawTargetGui(w.Caster, center, 1f);
            Check.Equal("TM_SpellItem_OutOfRange", sealPreviewReasons.Single(), "range rejection now explained in preview");
            var verb = new TorannMagic.Verb_BreachSeal { caster = w.Caster, verbProps = new AbilityUser.VerbProperties_Ability { range = 1f } };
            Check.Equal(false, verb.ValidateTarget(center, false), "native click validation rejects invalid selection without charging");
            verb.verbProps.range = 15f;
            Check.Equal(true, verb.ValidateTarget(center, false), "native click validation accepts near-core platform");
        }, true));
        yield return new("Odyssey.BreachSeal.PreviewDistinguishesRepairCellsAndRejection", f => SealWorld(f, (w, core, floors) =>
        {
            using var boundaries = PreviewBoundaries();
            var edge = BreachSeal.GetFarEdgeCells(SealCenter, core.OccupiedRect());
            var existing = new Building { def = SealDef("PreviewExistingWall") }; existing.def.building.isWall = true; existing.def.building.isAirtight = true;
            Set(existing, "factionInt", w.Caster.Faction); SealSpawnBoundary(existing, edge[0], w.Map);
            sealPreviewCells.Clear(); sealPreviewReasons.Clear();
            BreachSeal.DrawPreview(w.Caster, SealCenter);
            Check.Equal(3, sealPreviewCells.Count, "outline, preserved edge, and new wall overlays");
            Check.Equal(UnityEngine.Color.white, sealPreviewCells[0].color, "selected 3x3 outline white");
            Check.Equal(9, sealPreviewCells[0].cells.Count, "selected area retains nine cells");
            Check.Equal(UnityEngine.Color.gray, sealPreviewCells[1].color, "full edge has neutral baseline");
            Check.Equal(true, edge.SequenceEqual(sealPreviewCells[1].cells), "baseline is exactly the far edge");
            Check.Equal(UnityEngine.Color.cyan, sealPreviewCells[2].color, "new walls highlighted cyan");
            Check.Equal(true, edge.Skip(1).SequenceEqual(sealPreviewCells[2].cells), "existing wall never highlighted as newly created");
            Check.Equal(0, sealPreviewReasons.Count, "valid repair has no rejection reason");
            w.Map.thingGrid.Deregister(w.Patient); Set(w.Patient, "positionInt", edge[1]); w.Map.thingGrid.Register(w.Patient);
            sealPreviewCells.Clear(); sealPreviewReasons.Clear();
            SpellItemGizmos.DrawPreview(w.Caster, Props(SpellItemKind.BreachSeal), SealCenter);
            Check.Equal(0, sealPreviewReasons.Count, "world highlight leaves text to GUI callback");
            BreachSeal.DrawTargetGui(w.Caster, SealCenter, Props(SpellItemKind.BreachSeal).range);
            Check.Equal(2, sealPreviewCells.Count, "invalid item preview has outline and rejected edge only");
            Check.Equal(UnityEngine.Color.red, sealPreviewCells[1].color, "invalid edge red");
            Check.Equal(true, edge.SequenceEqual(sealPreviewCells[1].cells), "entire rejected edge indicated");
            Check.Equal("TM_BreachSeal_Blocked", sealPreviewReasons.Single(), "planner blocker reason reaches renderer boundary");
            Check.Equal(0, sealPreviewCells.Count(p => p.color == UnityEngine.Color.cyan), "rejected preview does not imply partial spawn");
        }));
        yield return new("Odyssey.BreachSeal.LearnedAndItemInclusiveRange", f => SealWorld(f, (w, core, floors) =>
        {
            var target = new IntVec3(18,0,6);
            floors.UnionWith(new[] { target, new IntVec3(19,0,5), new IntVec3(19,0,6), new IntVec3(19,0,7) });
            Set(w.Caster, "positionInt", new IntVec3(3,0,6));
            var verb = new TorannMagic.Verb_BreachSeal { caster = w.Caster, verbTracker = new VerbTracker(w.Caster),
                verbProps = new AbilityUser.VerbProperties_Ability { range = 15, targetParams = new TargetingParameters { canTargetLocations = true } } };
            var props = Props(SpellItemKind.BreachSeal); props.range = 15;
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, target, props), "item accepts exact fifteen-cell boundary");
            Check.Equal(true, verb.CanHitTargetFrom(w.Caster.Position, target), "learned spell also accepts exact fifteen-cell boundary");
            Set(w.Caster, "positionInt", new IntVec3(2,0,6));
            Check.Equal("TM_SpellItem_OutOfRange", SpellItemEffects.TargetRejection(w.Caster, target, props), "item rejects sixteen cells");
            Check.Equal(false, verb.CanHitTargetFrom(w.Caster.Position, target), "learned spell rejects sixteen cells");
        }));
        yield return new("Odyssey.BreachSeal.LearnedAndItemOccupiedUntouchedCenter", f => SealWorld(f, (w, core, floors) =>
        {
            w.Building(SealCenter);
            var grid = ((PathGrid)Get(w, "pathGrid")!).Grid_Unsafe; grid[w.Map.cellIndices.CellToIndex(SealCenter)] = 10000;
            Set(w.Caster, "positionInt", SealCenter);
            Check.Equal(false, SealCenter.Walkable(w.Map), "fixture center is actually impassable");
            Check.Equal(true, GenSight.LineOfSight(SealCenter, SealCenter, w.Map, skipFirstCell: true), "same-cell targeting has unobstructed native sight");
            var verb = new TorannMagic.Verb_BreachSeal { caster = w.Caster, verbTracker = new VerbTracker(w.Caster),
                verbProps = new AbilityUser.VerbProperties_Ability { range = 15, targetParams = new TargetingParameters { canTargetLocations = true } } };
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, SealCenter, Props(SpellItemKind.BreachSeal)), "item accepts occupied untouched center");
            Check.Equal(true, verb.CanHitTargetFrom(w.Caster.Position, SealCenter), "learned spell accepts occupied untouched center");
        }));
        yield return new("Odyssey.BreachSeal.FarEdgeCardinalsDiagonalEvenFootprint", _ =>
        {
            var core = new CellRect(9, 9, 3, 3);
            foreach (var entry in new[] {
                (new IntVec3(15,0,10),new IntVec3(16,0,9),new IntVec3(16,0,11)),
                (new IntVec3(5,0,10),new IntVec3(4,0,9),new IntVec3(4,0,11)),
                (new IntVec3(10,0,15),new IntVec3(9,0,16),new IntVec3(11,0,16)),
                (new IntVec3(10,0,5),new IntVec3(9,0,4),new IntVec3(11,0,4)),
                (new IntVec3(15,0,15),new IntVec3(16,0,14),new IntVec3(16,0,16)) })
            {
                var edge = BreachSeal.GetFarEdgeCells(entry.Item1, core);
                Check.Equal(3, edge.Count, "exactly three edge cells");
                Check.Equal(entry.Item2, edge[0], "far edge start"); Check.Equal(entry.Item3, edge[2], "far edge end");
                Check.Equal(3, edge.Distinct().Count(), "no repeated cells");
            }
            Check.Equal(0, BreachSeal.GetFarEdgeCells(new IntVec3(11,0,10), core).Count, "entire core footprint rejects overlap");
            var even = BreachSeal.GetFarEdgeCells(new IntVec3(12,0,7), new CellRect(9,9,2,2));
            Check.Equal(new IntVec3(13,0,6), even[0], "half-cell center diagonal chooses X without anchor bias");
        });
        foreach (bool magical in new[] { false, true })
            yield return new("Odyssey.BreachSeal.NativePlanner." + (magical ? "MagicalCore" : "BaseCore"), f => SealWorld(f, (w, core, floors) =>
            {
                Check.Equal(true, BreachSeal.TryPlan(w.Caster, SealCenter, out var plan, out _), "owned native engine resolves");
                Check.Same(core, plan.Core, "same live core owns every affected cell");
                Check.Equal(3, plan.SpawnCells.Count, "three initially empty cells");
                // Only four supported cells are supplied. The untouched six may be off-deck or occupied.
                Check.Equal(4, floors.Count, "no extra platform cells required");
                Set(core, "factionInt", new Faction { def = new FactionDef() });
                Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "hostile engine cannot authorize casting");
                Set(core, "factionInt", w.Caster.Faction);
                floors.Remove(plan.EdgeCells[1]);
                Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "missing one edge platform rejects all");
                floors.Add(plan.EdgeCells[1]);
                var floor = w.Map.terrainGrid.topGrid[w.Map.cellIndices.CellToIndex(SealCenter)];
                floor.exposesToVacuum = true;
                Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "vacuum-exposing existing floor rejects");
                floor.exposesToVacuum = false;
                var duplicate = SealEngine(w, floors, false); w.Map.listerThings.Add(duplicate);
                Check.Equal("TM_BreachSeal_AmbiguousCore", BreachSeal.TargetRejection(w.Caster, SealCenter), "overlapping owners ambiguous even same faction");
                w.Map.listerThings.Remove(duplicate);
                Set(core, "mapIndexOrState", (sbyte)-1);
                Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "departed core cannot authorize completion");
            }, magical));
        yield return new("Odyssey.BreachSeal.BlockersPartialRepairAndNoOp", f => SealWorld(f, (w, core, floors) =>
        {
            var cells = BreachSeal.GetFarEdgeCells(SealCenter, core.OccupiedRect());
            foreach (var cell in cells)
            {
                w.Map.thingGrid.Deregister(w.Patient); Set(w.Patient, "positionInt", cell); w.Map.thingGrid.Register(w.Patient);
                Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "pawn anywhere on edge rejects entire cast");
            }
            w.Map.thingGrid.Deregister(w.Patient); Set(w.Patient, "positionInt", SealCenter); w.Map.thingGrid.Register(w.Patient);
            Check.Equal(true, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "pawn in untouched center allowed");
            foreach (Thing blocker in new Thing[] { new Thing { def = SealDef("Item", ThingCategory.Item) }, new Building { def = SealDef("Building") }, new Blueprint_Build { def = SealDef("Blueprint", ThingCategory.Ethereal) }, new Frame { def = SealDef("Frame") }, new Plant { def = SealDef("Plant", ThingCategory.Plant) } })
            {
                Set(blocker, "positionInt", cells[1]); Set(blocker, "mapIndexOrState", (sbyte)0); w.Map.thingGrid.Register(blocker);
                Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "blocker rejects: " + blocker.GetType().Name);
                w.Map.thingGrid.Deregister(blocker);
            }
            foreach (var cell in cells)
            {
                var wall = new Building { def = SealDef("ExistingWall") }; wall.def.building.isWall = true; wall.def.building.isAirtight = true;
                Set(wall, "factionInt", w.Caster.Faction); Set(wall, "positionInt", cell); Set(wall, "mapIndexOrState", (sbyte)0); w.Map.thingGrid.Register(wall);
                if (cell != cells.Last())
                {
                    Check.Equal(true, BreachSeal.TryPlan(w.Caster, SealCenter, out var plan, out _), "partial airtight wall allows repair");
                    Check.Equal(false, plan.SpawnCells.Contains(cell), "existing wall excluded from writes");
                }
            }
            Check.Equal("TM_BreachSeal_AlreadySealed", BreachSeal.TargetRejection(w.Caster, SealCenter), "complete wall is no-op");
            Check.Equal(false, BreachSeal.TryApply(w.Caster, SealCenter), "no effect or refresh for already sealed edge");
        }));
        yield return new("Odyssey.BreachSeal.DlcFogBoundsAndWallOwnership", f => SealWorld(f, (w, core, floors) =>
        {
            var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
            flag.SetValue(null, false);
            Check.Equal(false, BreachSeal.TryApply(w.Caster, SealCenter), "no cast without DLC"); flag.SetValue(null, true);
            w.Fog(SealCenter, true); Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "unrevealed center rejected"); w.Fog(SealCenter, false);
            var edge = BreachSeal.GetFarEdgeCells(SealCenter, core.OccupiedRect());
            w.Fog(edge[2], true); Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "unrevealed edge rejected"); w.Fog(edge[2], false);
            var wall = new Building { def = SealDef("ForeignWall") }; wall.def.building.isWall = true; wall.def.building.isAirtight = true;
            Set(wall, "factionInt", new Faction { def = new FactionDef() }); SealSpawnBoundary(wall, edge[0], w.Map);
            Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "foreign airtight wall is blocker");
            Set(wall, "factionInt", w.Caster.Faction); wall.def.building.isAirtight = false;
            Check.Equal(false, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "friendly non-airtight wall is blocker");
            wall.def.building.isAirtight = true;
            Check.Equal(true, BreachSeal.TryPlan(w.Caster, SealCenter, out _, out _), "friendly airtight wall is preserved");
            var margin = new IntVec3(19,0,6); floors.Add(margin);
            Check.Equal(false, BreachSeal.TryPlan(w.Caster, margin, out _, out _), "edge outside map rejected");
            floors.Add(core.Position);
            Check.Equal("TM_BreachSeal_CoreOverlap", BreachSeal.TargetRejection(w.Caster, core.Position), "target center cannot overlap live core");
        }));
        foreach (int bonus in new[] { 0, 45000 })
        yield return new("Odyssey.BreachSeal.NativeAirtightExpirySaveAndGravshipTransfer." + bonus, f => World(f, w =>
        {
            var wall = new Building { def = SealDef("TimerWall") }; wall.def.building.isWall = true; wall.def.building.isAirtight = true;
            Check.Equal(true, wall.IsAirtight, "native non-stuff wall is airtight");
            Check.Equal(false, wall.ExchangeVacuum, "native wall blocks vacuum exchange");
            var timer = new CompDestroyAfterDelay { parent = wall, props = new CompProperties_DestroyAfterDelay { delayTicks = 60000, destroyMode = DestroyMode.Vanish } };
            timer.PostSpawnSetup(false); timer.spawnTick += bonus; Check.Equal(60100 + bonus, timer.DestructionTick, "absolute trained deadline");
            Set(Find.TickManager, "ticksGameInt", 1234);
            wall.PreSwapMap(); Check.Equal(true, wall.BeingTransportedOnGravship, "native transfer state");
            timer.PostSpawnSetup(false); Check.Equal(60100 + bonus, timer.DestructionTick, "transport respawn does not refresh life");
            var saved = new SealTimerSave { Timer = timer }; string path = Path.Combine(f.WorkingDirectory, "breach-seal-timer.xml");
            try { Scribe.saver.InitSaving(path, "regression"); saved.ExposeData(); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            var restored = new SealTimerSave { Timer = new CompDestroyAfterDelay { parent = wall, props = timer.props } };
            try { Scribe.loader.InitLoading(path); restored.ExposeData(); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
            restored.Timer.PostSpawnSetup(true);
            Check.Equal(60100 + bonus, restored.Timer.DestructionTick, "save and load preserve original deadline");
            using var patches = new Patches(); patches.Prefix(typeof(Building).GetMethod(nameof(Building.Destroy))!, nameof(SealDestroyBoundary));
            sealDestroyed.Clear();
            Set(Find.TickManager, "ticksGameInt", 60099 + bonus); restored.Timer.CompTick(); Check.Equal(0, sealDestroyed.Count, "not destroyed early");
            Set(Find.TickManager, "ticksGameInt", 60100 + bonus); restored.Timer.CompTick(); restored.Timer.CompTick();
            Check.Equal(1, sealDestroyed.Count, "native expiry destroys exactly once");
            Check.Same(wall, sealDestroyed.Single(), "only expiring wall destroyed");
        }));
        foreach (bool destroyFailure in new[] { false, true })
        yield return new("Odyssey.BreachSeal.SpawnFailureRollsBackOnlyNewWalls" + (destroyFailure ? ".DestroyFailure" : ""), f => SealWorld(f, (w, core, floors) =>
        {
            var edge = BreachSeal.GetFarEdgeCells(SealCenter, core.OccupiedRect());
            var existing = new Building { def = SealDef("PreservedAirtightWall") }; existing.def.building.isWall = true; existing.def.building.isAirtight = true;
            Set(existing, "factionInt", w.Caster.Faction); SealSpawnBoundary(existing, edge[0], w.Map);
            using var patches = new Patches();
            patches.Prefix(typeof(BreachSeal).GetMethod(nameof(BreachSeal.SpawnWall))!, nameof(SealFailSecondSpawn));
            patches.Prefix(typeof(Building).GetMethod(nameof(Building.Destroy))!, nameof(SealDestroyBoundary));
            patches.Prefix(typeof(Building).GetMethod(nameof(Building.DeSpawn))!, nameof(SealDespawnBoundary));
            patches.Prefix(typeof(TorannMagic.Compatibility.TM_Log).GetMethod("Warning", [typeof(string), typeof(string)])!, nameof(SealExpectedRollbackWarning));
            sealDestroyed.Clear(); sealSpawnAttempts = 0; sealRollbackWarnings = 0; sealDespawned = 0; sealThrowFirstDestroy = destroyFailure;
            try { Check.Equal(false, BreachSeal.TryApply(w.Caster, SealCenter), "mid-spawn exception refuses success"); }
            finally { sealThrowFirstDestroy = false; }
            Check.Equal(2, sealDestroyed.Count, "both newly created walls cleaned up");
            Check.Equal(destroyFailure ? 2 : 1, sealRollbackWarnings, "placement and optional cleanup failures diagnosed");
            Check.Equal(destroyFailure ? 1 : 0, sealDespawned, "failed destroy falls back to despawn");
            Check.Equal(false, existing.Destroyed, "existing wall survives rollback");
            Check.Equal(true, edge.Skip(1).All(c => c.GetThingList(w.Map).Count == 0), "no partial new row remains");
        }));
        yield return new("Odyssey.BreachSeal.RealScrollCommitAndRepeat", f => SealWorld(f, (w, core, floors) =>
        {
            using var patches = new Patches(); patches.Prefix(typeof(BreachSeal).GetMethod(nameof(BreachSeal.SpawnWall))!, nameof(SealSpawnBoundary));
            HeadlessDefOf(typeof(PawnCapacityDefOf)); var old = PawnCapacityDefOf.Manipulation;
            using var capacity = new DefinitionScope<PawnCapacityDef>(new PawnCapacityDef { defName = "SealManipulation", minForCapable = .1f });
            try
            {
                PawnCapacityDefOf.Manipulation = capacity.Definition; Capacity(w.Caster, capacity.Definition);
                w.Caster.inventory = new Pawn_InventoryTracker(w.Caster);
                var scroll = Item("seal scroll", SpellItemKind.BreachSeal, false, 3); scroll.Props.cooldownTicks = 0;
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(scroll.parent, false), "real inventory ownership");
                var token = scroll.TryBegin(w.Caster); Check.Equal(true, token > 0, "real item admission");
                floors.Remove(SealCenter);
                Check.Equal(false, scroll.TryCommit(w.Caster, token, SealCenter), "warmup platform loss refuses payment");
                Check.Equal(3, scroll.parent.stackCount, "failed cast retains scroll"); floors.Add(SealCenter);
                Check.Equal(true, scroll.TryCommit(w.Caster, token, SealCenter), "real shared effect and payment succeed");
                Check.Equal(2, scroll.parent.stackCount, "one scroll consumed");
                Check.Equal(false, scroll.TryCommit(w.Caster, token, SealCenter), "same receipt cannot charge twice");
                var again = scroll.TryBegin(w.Caster);
                Check.Equal(false, scroll.TryCommit(w.Caster, again, SealCenter), "already sealed repeat rejected");
                Check.Equal(2, scroll.parent.stackCount, "no-op repeat is free");
            }
            finally { PawnCapacityDefOf.Manipulation = old; }
        }));
    }

    static readonly IntVec3 SealCenter = new(8, 0, 6);
    static readonly List<(List<IntVec3> cells, UnityEngine.Color color)> sealPreviewCells = [];
    static readonly List<string> sealPreviewReasons = [];
    static bool CaptureSealPreviewCells(List<IntVec3> cells, UnityEngine.Color color)
    { sealPreviewCells.Add((cells.ToList(), color)); return false; }
    static bool CaptureSealPreviewReason(string reason)
    { sealPreviewReasons.Add(reason); return false; }
    static ThingDef SealDef(string name, ThingCategory category = ThingCategory.Building)
    {
        var def = Blank<ThingDef>(); def.defName = name; def.category = category; def.thingClass = typeof(Building);
        def.size = new IntVec2(1,1); def.building = new BuildingProperties(); def.comps = []; return def;
    }
    static void SealWorld(RuntimeFixture f, Action<WorldState, Building_GravEngine, HashSet<IntVec3>> body, bool magical = false) => World(f, w =>
    {
        using var language = new LanguageScope(f.Repository);
        HeadlessDefOf(typeof(ThingCategoryDefOf));
        LanguageDatabase.activeLanguage.keyedReplacements["Gravship"] = new LoadedLanguage.KeyedReplacement { key = "Gravship", value = "Gravship" };
        var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!; var old = flag.GetValue(null);
        var def = SealDef(BreachSeal.WallName); def.building.isWall = true; def.building.isAirtight = true;
        using var scope = new DefinitionScope<ThingDef>(def);
        try
        {
            flag.SetValue(null, true);
            var world = Blank<RimWorld.Planet.World>(); world.factionManager = new FactionManager();
            world.factionManager.AllFactionsListForReading.Add(w.Caster.Faction); Set(world.factionManager, "ofPlayer", w.Caster.Faction);
            Set(Current.Game, "worldInt", world);
            w.Map.listerThings = new ListerThings(ListerThingsUse.Global);
            w.Map.terrainGrid = new TerrainGrid(w.Map);
            var floor = Blank<TerrainDef>(); floor.defName = "SealFloor"; floor.exposesToVacuum = false;
            Array.Fill(w.Map.terrainGrid.topGrid, floor);
            var floors = new HashSet<IntVec3> { SealCenter, new(9,0,5), new(9,0,6), new(9,0,7) };
            var core = SealEngine(w, floors, magical); w.Map.listerThings.Add(core);
            body(w, core, floors);
        }
        finally { flag.SetValue(null, old); }
    });
    static Building_GravEngine SealEngine(WorldState w, HashSet<IntVec3> floors, bool magical)
    {
        typeof(OdysseyReservoirTransportTests).GetMethod("InitializeEngineGraphics", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
        Building_GravEngine core = magical ? new Building_MagicalGravEngine() : new Building_GravEngine();
        core.thingIDNumber = Find.UniqueIDsManager.GetNextThingID();
        core.def = SealDef("SealEngine"); Set(core, "positionInt", new IntVec3(3,0,6)); Set(core, "mapIndexOrState", (sbyte)0);
        Set(core, "factionInt", w.Caster.Faction); Set(core, "validSubstructure", floors); core.substructureDirty = false; return core;
    }
    // Renderer/map-registration boundary only; planner, creation, ownership, and item payment remain native.
    static bool SealSpawnBoundary(Thing wall, IntVec3 cell, Map map)
    { Set(wall, "positionInt", cell); Set(wall, "mapIndexOrState", (sbyte)0); map.thingGrid.Register(wall); return false; }
    static int sealSpawnAttempts, sealRollbackWarnings, sealDespawned;
    static bool sealThrowFirstDestroy;
    static readonly List<Thing> sealDestroyed = [];
    static bool SealFailSecondSpawn(Thing wall, IntVec3 cell, Map map)
    { if (++sealSpawnAttempts == 2) throw new InvalidOperationException("injected seal spawn failure"); return SealSpawnBoundary(wall, cell, map); }
    static bool SealDestroyBoundary(Building __instance, DestroyMode mode)
    {
        Check.Equal(DestroyMode.Vanish, mode, "no deconstruction/refund on rollback or expiry");
        sealDestroyed.Add(__instance);
        if (sealThrowFirstDestroy && sealDestroyed.Count == 1) throw new InvalidOperationException("injected seal destruction failure");
        if (__instance.Spawned) __instance.Map.thingGrid.Deregister(__instance);
        Set(__instance, "mapIndexOrState", (sbyte)-2); return false;
    }
    static bool SealDespawnBoundary(Building __instance, DestroyMode mode)
    { Check.Equal(DestroyMode.Vanish, mode, "fallback does not refund materials"); __instance.Map.thingGrid.Deregister(__instance); __instance.ForceSetStateToUnspawned(); sealDespawned++; return false; }
    static bool SealExpectedRollbackWarning(string category, string message)
    { Check.Equal("BreachSeal", category, "only seal failure log intercepted"); Check.Equal(true, message.Contains("injected seal spawn failure") || message.Contains("injected seal destruction failure"), "injected failure diagnosed: " + message); sealRollbackWarnings++; return false; }
    public sealed class SealTimerSave : IExposable
    {
        public CompDestroyAfterDelay Timer = null!;
        public void ExposeData() => Timer.PostExposeData();
    }
}




