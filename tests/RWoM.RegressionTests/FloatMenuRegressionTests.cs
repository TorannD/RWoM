using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.Enchantment;
using TorannMagic.ModOptions;
using TorannMagic.TMDefs;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    public static IEnumerable<TestCase> FloatMenuCases()
    {
        foreach (bool nativePipeline in new[] { false, true })
        yield return new("FloatMenu.MageMissingManaKeepsOtherOrders." + (nativePipeline ? "NativePipeline" : "Direct"), f => World(f, w =>
        {
            using var language = new LanguageScope(f.Repository);
            var context = Blank<FloatMenuContext>(); context.allSelectedPawns = [w.Caster]; context.map = w.Map;
            context.clickPosition = new UnityEngine.Vector3(4,0,4);
            var magic = new CompAbilityUserMagic { parent = w.Caster };
            Set(magic, "initializedIsMagicUser", true); Set(magic, "isMagicUser", true);
            w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
            w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
            var enchant = new CompEnchant { parent = w.Caster };
            var gemDef = Blank<ThingDef>(); gemDef.defName = "FloatMenuGem"; gemDef.category = ThingCategory.Item; gemDef.stackLimit=1;
            Check.Equal(true, enchant.enchantingContainer.TryAdd(new Thing { def = gemDef }), "real gem ownership");
            w.Caster.AllComps.Add(magic); w.Caster.AllComps.Add(enchant); TM_Calc.InvalidateCompCache(w.Caster);
            var armorDef = Blank<ThingDef>(); armorDef.defName="FloatMenuArmor"; armorDef.label="armor"; armorDef.size=new IntVec2(1,1); armorDef.category=ThingCategory.Item; armorDef.alwaysHaulable=true; armorDef.apparel=new ApparelProperties();
            var armor = new Thing { def=armorDef }; Set(armor,"mapIndexOrState",(sbyte)0); Set(armor,"positionInt",new IntVec3(4,0,4)); w.Map.thingGrid.Register(armor);
            using var patches = new Patches();
            patches.Prefix(typeof(FloatMenuOption).GetMethod("SetSizeMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!, nameof(FloatMenuNoSizing));
            patches.Prefix(typeof(ReachabilityUtility).GetMethods().Single(m => m.Name=="CanReach" && m.GetParameters().Length==7), nameof(FloatMenuReachable));
            var native = new FloatMenuOption("Native order", () => { }); var options = new List<FloatMenuOption> { native };
            if (nativePipeline) InvokeNativeFloatMenu(context, options);
            else TorannMagicMod.FloatMenuMakerMap_Patch.Postfix(context, ref options, w.Caster);
            Check.Same(native, options[0], "native order survives a mage without mana need");
            Check.Equal(2, options.Count, "enchant action becomes disabled instead of throwing");
            Check.Equal(true, options[1].Disabled, "no free enchant order without mana");
            Check.Equal(1, enchant.enchantingContainer.Count, "opening menu never spends gem");
        }));
        foreach (bool missing in new[] { true, false })
            yield return new("FloatMenu.RegistryPartialStartup." + (missing ? "MissingDefinition" : "MissingSettings"), f =>
            {
                var oldSettings = Settings.Instance;
                var oldClasses = TM_ClassUtility.CustomClasses;
                var indexes = typeof(TM_ClassUtility).GetField("CustomClassTraitIndexes", BindingFlags.NonPublic | BindingFlags.Static)!;
                var oldIndexes = indexes.GetValue(null);
                var defs = DefDatabase<TM_CustomClassDef>.AllDefsListForReading.ToArray();
                var lists = new[] { TM_ClassUtility.CustomBaseClasses, TM_ClassUtility.CustomMageClasses,
                    TM_ClassUtility.CustomFighterClasses, TM_ClassUtility.CustomAdvancedClasses };
                var saved = lists.Select(l => l.ToArray()).ToArray();
                try
                {
                    Settings.Instance = new Settings { CustomClass = null! };
                    DefDatabase<TM_CustomClassDef>.Clear();
                    var trait = new TraitDef { defName = "FloatMenuCustomMage" };
                    var custom = new TM_CustomClass { classTrait = trait, isMage = true };
                    if (!missing) DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [custom] });
                    TM_ClassUtility.CustomClasses = null!;
                    TM_ClassUtility.LoadCustomClasses();
                    Check.NotNull(TM_ClassUtility.CustomClasses, "missing registry cannot leave null runtime state");
                    Check.NotNull(Settings.Instance.CustomClass, "missing settings dictionary initialized at load boundary");
                    Check.Equal(missing ? 0 : 1, TM_ClassUtility.CustomMageClasses.Count, "valid classes remain available");
                    if (!missing) Check.Same(custom, TM_ClassUtility.CustomMageClasses.Single(), "class object/progression identity retained");
                }
                finally
                {
                    Settings.Instance = oldSettings; TM_ClassUtility.CustomClasses = oldClasses;
                    DefDatabase<TM_CustomClassDef>.Clear(); foreach (var def in defs) DefDatabase<TM_CustomClassDef>.Add(def);
                    for (int i=0;i<lists.Length;i++) { lists[i].Clear(); lists[i].AddRange(saved[i]); }
                    indexes.SetValue(null, oldIndexes);
                }
            });

        foreach (string invalid in new[] { "NullContext", "OtherMap", "OutsideMap", "Multiselect" })
            yield return new("FloatMenu.InvalidContextPreservesNativeOptions." + invalid, f => World(f, w =>
            {
                var context = Blank<FloatMenuContext>(); context.allSelectedPawns = [w.Caster]; context.map = w.Map;
                context.clickPosition = new UnityEngine.Vector3(4,0,4);
                if (invalid == "OtherMap") context.map = Blank<Map>();
                if (invalid == "OutsideMap") context.clickPosition = new UnityEngine.Vector3(-1,0,-1);
                if (invalid == "Multiselect") context.allSelectedPawns.Add(w.Patient);
                var magic = new CompAbilityUserMagic { parent = w.Caster };
                Set(magic, "initializedIsMagicUser", true); Set(magic, "isMagicUser", true);
                w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
                var enchant = new CompEnchant { parent = w.Caster, enchantingContainer = null! };
                w.Caster.AllComps.Add(magic); w.Caster.AllComps.Add(enchant);
                TM_Calc.InvalidateCompCache(w.Caster);
                var native = Blank<FloatMenuOption>();
                var options = new List<FloatMenuOption> { native };
                TorannMagicMod.FloatMenuMakerMap_Patch.Postfix(invalid == "NullContext" ? null! : context, ref options, w.Caster);
                Check.Equal(1, options.Count, "existing orders retained"); Check.Same(native, options[0], "same native option retained");
                Check.Equal(true, enchant.enchantingContainer == null, "invalid context does not initialize pawn state");
            }));
    }
    static bool FloatMenuReachable(ref bool __result) { __result=true; return false; }
    static bool FloatMenuNoSizing() => false;
    static void InvokeNativeFloatMenu(FloatMenuContext context, List<FloatMenuOption> options)
    {
        var field = typeof(FloatMenuMakerMap).GetField("providers", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previous = field.GetValue(null); var pawn = FloatMenuMakerMap.makingFor;
        var assembly = Assembly.Load("0Harmony"); string id="rwom.regression.native-float-menu";
        var harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        try
        {
            field.SetValue(null, new List<FloatMenuOptionProvider>());
            FloatMenuMakerMap.makingFor=context.allSelectedPawns[0];
            var original=typeof(FloatMenuMakerMap).GetMethod("GetProviderOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
            var postfix=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(TorannMagicMod.FloatMenuMakerMap_Patch).GetMethod("Postfix")!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [original,null,postfix,null,null]);
            original.Invoke(null,[context,options]);
        }
        finally
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            field.SetValue(null,previous); FloatMenuMakerMap.makingFor=pawn;
        }
    }
}


