using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using Verse;
namespace RWoM.RegressionTests;
internal static class MagicalSurfaceStartTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.StartScenario.SurfaceOnlyAndFlightScope", f => CheckScope());
    }
    static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(target, value);
    static int selections, validations;
    static bool nativeResult;
    public static bool SurfacePicker(ref PlanetTile __result) { selections++; __result = new PlanetTile(9, 0); return false; }
    public static bool NativeValidation(ref bool __result) { validations++; __result = nativeResult; return false; }
    static void CheckScope()
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PlanetLayerDefOf));
        var assembly = typeof(TorannMagic.Odyssey.MagicalThrusterFuel).Assembly;
        var partType = assembly.GetType("TorannMagic.Odyssey.ScenPart_MagicalSurfaceStart")!;
        var patchType = assembly.GetType("TorannMagic.Odyssey.MagicalSurfaceStartingTilePatch")!;
        var oldGame = Current.Game; var oldSurface = PlanetLayerDefOf.Surface;
        var harmonyAssembly = Assembly.Load("0Harmony"); var ht = harmonyAssembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.surface." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        void Prefix(MethodInfo method, string prefix)
        {
            var hm = Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(MagicalSurfaceStartTests).GetMethod(prefix)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, new object?[] { method, hm, null, null, null });
        }
        try
        {
            var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game)); Current.Game = game;
            game.InitData = new GameInitData(); game.Scenario = new Scenario();
            var part = (ScenPart)Activator.CreateInstance(partType)!;
            Set(game.Scenario, "parts", new List<ScenPart> { part });
            var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World)); Set(game, "worldInt", world);
            world.grid = (WorldGrid)RuntimeHelpers.GetUninitializedObject(typeof(WorldGrid));
            PlanetLayerDefOf.Surface = new PlanetLayerDef { defName = "Surface" };
            var surface = (PlanetLayer)RuntimeHelpers.GetUninitializedObject(typeof(PlanetLayer)); Set(surface, "def", PlanetLayerDefOf.Surface);
            var orbit = (PlanetLayer)RuntimeHelpers.GetUninitializedObject(typeof(PlanetLayer)); Set(orbit, "def", new PlanetLayerDef { defName = "Orbit" });
            Set(world.grid, "planetLayers", new Dictionary<int, PlanetLayer> { { 0, surface }, { 1, orbit } });

            var native = typeof(TileFinder).GetMethod("IsValidTileForNewSettlement")!;
            var page = typeof(Page_SelectStartingSite).GetMethod("CanDoNext", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var wrapper = patchType.GetMethod("IsValidStartingTile")!;
            // Check the actual installed game's call site, not a synthetic instruction fixture.
            var processor = harmonyAssembly.GetType("HarmonyLib.PatchProcessor")!;
            var read = processor.GetMethods().Single(m => m.Name == "GetOriginalInstructions" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
            var original = read.Invoke(null, new object?[] { page, null })!;
            var originalCode = ((System.Collections.IEnumerable)original).Cast<object>().ToList();
            var originalOpcodes = originalCode.Select(i => i.GetType().GetField("opcode")!.GetValue(i)).ToList();
            var rewritten = (System.Collections.IEnumerable)patchType.GetMethod("Transpiler")!.Invoke(null, new[] { original })!;
            int wrapperCalls = 0, nativeCalls = 0, index = 0;
            foreach (var instruction in rewritten)
            {
                Check.Same(originalCode[index], instruction, "call-site rewrite preserves instruction labels and exception blocks");
                Check.Equal(originalOpcodes[index++], instruction.GetType().GetField("opcode")!.GetValue(instruction), "rewrite preserves opcodes");
                var operand = instruction.GetType().GetField("operand")!.GetValue(instruction);
                if (Equals(operand, wrapper)) wrapperCalls++;
                if (Equals(operand, native)) nativeCalls++;
            }
            Check.Equal(1, wrapperCalls, "real starting-site page redirects one validation call");
            Check.Equal(0, nativeCalls, "page cannot bypass scenario validation");
            var classProcessor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, new object[] { patchType })!;
            classProcessor.GetType().GetMethod("Patch")!.Invoke(classProcessor, null);
            var patched = ((IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!).ToList();
            Check.Equal(true, patched.Contains(page), "production patch attaches to the starting-site page");
            var nativePatchInfo = ht.GetMethod("GetPatchInfo")!.Invoke(null, new object[] { native });
            if (nativePatchInfo != null)
                Check.Equal(false, ((IEnumerable<string>)nativePatchInfo.GetType().GetProperty("Owners")!.GetValue(nativePatchInfo)!).Contains(id), "production scenario patch does not intercept global settlement validation");
            // Native acceptance is controlled to exercise ownership without requiring UI or generated terrain.
            Prefix(native, nameof(NativeValidation));
            nativeResult = true; validations = 0;
            Check.Equal(true, TileFinder.IsValidTileForNewSettlement(new PlanetTile(0, 1)), "NPC orbital settlement remains native during magical scenario world generation");
            Check.Equal(1, validations, "NPC validation reaches native path");
            bool Allows(PlanetTile tile, bool flight = false) => (bool)wrapper.Invoke(null, new object?[] { tile, null, flight })!;
            Check.Equal(false, Allows(new PlanetTile(0, 1)), "player starting-site acceptance rejects orbit");
            Check.Equal(1, validations, "orbital player start rejected before native validation");
            Check.Equal(true, Allows(new PlanetTile(0, 0)), "valid surface start accepted");
            nativeResult = false;
            Check.Equal(false, Allows(new PlanetTile(0, 0)), "native surface biome and occupation rejection retained");
            Check.Equal(false, Allows(PlanetTile.Invalid), "invalid starting tile retains native rejection");
            nativeResult = true;
            Check.Equal(true, TileFinder.IsValidTileForNewSettlement(new PlanetTile(0, 1), null, true), "gravship landing validator untouched");
            game.InitData = null;
            Check.Equal(true, Allows(new PlanetTile(0, 1)), "running game untouched");
            game.InitData = new GameInitData(); Set(game.Scenario, "parts", new List<ScenPart>());
            Check.Equal(true, Allows(new PlanetTile(0, 1)), "other scenarios untouched");
            Set(game.Scenario, "parts", new List<ScenPart> { part });
            Prefix(typeof(TileFinder).GetMethod("RandomStartingTile")!, nameof(SurfacePicker));
            selections = 0; game.InitData.startingTile = new PlanetTile(0, 1); part.PreMapGenerate();
            Check.Equal(9, game.InitData.startingTile.tileId, "quick-start asks native surface picker");
            Check.Same(PlanetLayerDefOf.Surface, game.InitData.startingTile.LayerDef, "quick-start cannot keep orbit");
            part.PreMapGenerate(); Check.Equal(1, selections, "existing surface selection retained");
            game.InitData.startingTile = PlanetTile.Invalid; part.PreMapGenerate(); Check.Equal(2, selections, "invalid quick-start selects surface");
            RuntimeLog.AssertClean();
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { id }); PlanetLayerDefOf.Surface = oldSurface; Current.Game = oldGame; }
    }
}
