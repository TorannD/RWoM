using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    static IEnumerable<TestCase> BreachSealPaymentCases()
    {
        yield return new("Odyssey.BreachSeal.NativeManaAndCooldownSettleOnce", f => SealWorld(f, (w, core, floors) =>
        {
            var previousSettings = TorannMagic.ModOptions.Settings.Instance;
            var previousWeakness = TorannMagicDefOf.TM_ArcaneWeakness;
            try
            {
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                GrimoireProgressionTests.LoadBaseDefinitions(f);
                TorannMagicDefOf.TM_ArcaneWeakness = new HediffDef { defName = "SealPaymentWeakness", hediffClass = typeof(Hediff), maxSeverity = 10 };
                w.Map.gameConditionManager = new GameConditionManager(w.Map);
                w.Map.weatherManager = Blank<WeatherManager>(); w.Map.weatherManager.curWeather = new WeatherDef { maxRangeCap = -1 };
                Current.Game.history = Blank<History>();
                Current.Game.history.historyEventsManager = Blank<HistoryEventsManager>();
                var pawn = w.Caster;
                pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
                pawn.needs = new Pawn_NeedsTracker(pawn); pawn.apparel = new Pawn_ApparelTracker(pawn);
                pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
                var magic = new CompAbilityUserMagic { parent = pawn }; pawn.AllComps.Add(magic); TM_Calc.InvalidateCompCache(pawn);
                Set(magic, "magicData", new MagicData());
                var mana = new Need_Mana(pawn); pawn.needs.AllNeeds.Add(mana); mana.CurLevel = .9f;
                using var syrrium = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null
                    ? new DefinitionScope<HediffDef>(new HediffDef { defName = "TM_SyrriumSenseHD" }) : null;
                using var patches = w.HealingBoundaries();
                patches.Prefix(typeof(HistoryEventsManager).GetMethod(nameof(HistoryEventsManager.RecordEvent))!, nameof(SkipSeerVisual));
                patches.Prefix(typeof(GameConditionManager).GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetMethod!, nameof(ShellNoParentConditions));
                patches.Prefix(typeof(MagicAbility).GetMethod(nameof(MagicAbility.CanCastPowerCheck))!, nameof(ShellResourcesAvailable));
                patches.Prefix(typeof(BreachSeal).GetMethod(nameof(BreachSeal.SpawnWall))!, nameof(SealSpawnBoundary));
                var abilityDef = f.AddAbility("TM_BreachSealPayment"); abilityDef.manaCost = .3f; abilityDef.consumeEnergy = true;
                abilityDef.MainVerb.SecondsToRecharge = 60;
                var ability = new BreachSealAbility(magic) { Def = abilityDef };
                var verb = new SealPaymentVerb { caster = pawn, Ability = ability, verbTracker = new VerbTracker(pawn),
                    verbProps = new AbilityUser.VerbProperties_Ability { range = 15, targetParams = new TargetingParameters { canTargetLocations = true } } };
                shellResources = false;
                Check.Equal(false, verb.Fire(SealCenter), "completion rechecks resources before creating walls");
                Check.Equal(.9f, mana.CurLevel, "refusal leaves real mana unchanged");
                shellResources = true;
                Check.Equal(true, verb.Fire(SealCenter), "seal applied before native settlement");
                Check.Equal(true, Math.Abs(.6f - mana.CurLevel) < .00001f, "native settlement charges thirty percent synchronously");
                int cooldown = ability.CooldownTicksLeft;
                Check.Equal(true, cooldown > 0, "native cooldown started immediately");
                ability.PostAbilityAttempt(); ability.PostAbilityAttempt();
                Check.Equal(true, Math.Abs(.6f - mana.CurLevel) < .00001f, "finish callbacks leave real mana unchanged");
                Check.Equal(cooldown, ability.CooldownTicksLeft, "finish callbacks do not restart cooldown");
                Check.Equal(false, verb.Fire(SealCenter), "already sealed target refuses another effect");
                ability.PostAbilityAttempt();
                Check.Equal(true, Math.Abs(.6f - mana.CurLevel) < .00001f, "no-op leaves real mana unchanged");
                Check.Equal(1, magic.MagicUsed.Count, "exactly one native magic-use record");
                Check.Equal(true, Math.Abs(.5f - pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness).Severity) < .00001f, "native mana-strain consequence occurs once");
            }
            finally { TorannMagic.ModOptions.Settings.Instance = previousSettings; TorannMagicDefOf.TM_ArcaneWeakness = previousWeakness; }
        }));
        yield return new("Odyssey.BreachSeal.NativeRegionAndRoomExposureContract", f => SealWorld(f, (w, core, floors) =>
        {
            var map = w.Map;
            map.info.isPocketMap = true;
            map.pocketTileInfo = new RimWorld.Planet.SurfaceTile();
            Set(map.pocketTileInfo, "biome", new BiomeDef { inVacuum = true });
            map.regionDirtyer = new RegionDirtyer(map);
            map.regionAndRoomUpdater = new RegionAndRoomUpdater(map); Set(map.regionAndRoomUpdater, "initialized", true);
            map.regionGrid = new RegionGrid(map);
            map.roofGrid = new RoofGrid(map);
            var roofs = new RoofDef[map.cellIndices.NumGridCells];
            roofs[map.cellIndices.CellToIndex(SealCenter)] = new RoofDef { defName = "SealRoomRoof" };
            Set(map.roofGrid, "roofGrid", roofs);
            var region = Blank<Region>(); region.mapIndex = 0; region.type = RegionType.Normal;
            region.extentsClose = new CellRect(SealCenter.x, SealCenter.z, 1, 1);
            map.regionGrid.DirectGrid[map.cellIndices.CellToIndex(SealCenter)] = region;
            Room InteriorRoom()
            {
                var district = new District { mapIndex = 0 }; district.Regions.Add(region);
                var room = Blank<Room>(); Set(room, "districts", new List<District> { district });
                Set(room, "cachedExposedCount", -1); return room;
            }
            Check.Equal(false, InteriorRoom().ExposedToSpace, "roofed non-vacuum floor in an enclosed native room is not exposed");
            roofs[map.cellIndices.CellToIndex(SealCenter)] = null!;
            Check.Equal(true, InteriorRoom().ExposedToSpace, "native room remains exposed through missing roof");
            roofs[map.cellIndices.CellToIndex(SealCenter)] = new RoofDef { defName = "SealRoomRoof" };
            var floor = SealCenter.GetTerrain(map); floor.exposesToVacuum = true;
            Check.Equal(true, InteriorRoom().ExposedToSpace, "native room remains exposed through vacuum floor");
            floor.exposesToVacuum = false;
            var edge = new IntVec3(9,0,6);
            Check.Equal(RegionType.Normal, edge.GetExpectedRegionType(map), "empty walkable edge participates in room topology");
            var def = DefDatabase<ThingDef>.GetNamed(BreachSeal.WallName);
            def.fillPercent = 1; def.passability = Traversability.Impassable;
            var wall = new Building { def = def };
            SealSpawnBoundary(wall, edge, map); map.edificeGrid.Register(wall);
            // Supply the native path-grid cache, as a normal Building.SpawnSetup would do.
            // This is a topology/query contract, not a full map-spawn integration test.
            var grid = map.pathing.Normal.pathGrid.Grid_Unsafe;
            grid[map.cellIndices.CellToIndex(edge)] = Verse.AI.PathGrid.ImpassableCost;
            Check.Equal(RegionType.None, edge.GetExpectedRegionType(map), "full-fill impassable seal excludes its cell from room region traversal");
            Check.Equal(true, wall.IsAirtight, "native fixed airtight property used without stuff");
            Check.Equal(false, wall.ExchangeVacuum, "native seal refuses vacuum exchange");
        }));
    }

    sealed class SealPaymentVerb : Verb_BreachSeal
    {
        public bool Fire(IntVec3 target) { currentTarget = target; return TryCastShot(); }
    }
}
