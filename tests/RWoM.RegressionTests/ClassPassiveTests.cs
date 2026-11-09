using System.Collections;
using System.Reflection;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ClassPassiveTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassPassive.SavedPriestOnlyAndLegacyDeduplication", Ownership);
        yield return new("ClassPassive.NativeOffsetStageMatcher", NativeMatcher);
    }

    static void Ownership(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var defs = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<ClassDisciplineDef>.Clear();
            var stat = new StatDef { defName = "PriestPassiveTest" };
            var trait = new TraitDef { defName = "Priest", disabledWorkTags = WorkTags.Violent,
                degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = [new StatModifier { stat = stat, value = .25f }] }] };
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TestPriestProfile", profileId = "rwom.priest", domain = DisciplineDomain.Magic, legacyTrait = trait });
            var p = PawnWithComps(false, false).pawn;
            var comp = new CompClassProgression { parent = p }; p.AllComps.Add(comp);
            Check.Equal(0f, ClassPassiveEffects.StatOffset(p, stat), "classic pawn unchanged");
            comp.BeginNewProgression(Vocation.Mage);
            comp.State.TryAddFoundation("rwom.fire", DisciplineDomain.Magic);
            Check.Equal(0f, ClassPassiveEffects.StatOffset(p, stat), "unsupported profile has no Priest passive");
            comp.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
            Check.Equal(.25f, ClassPassiveEffects.StatOffset(p, stat), "saved Priest reads runtime trait definition");
            Check.Equal(WorkTags.Violent, ClassPassiveEffects.DisabledWork(p), "saved Priest retains vow");
            var disabled = WorkTags.ManualDumb;
            ClassPassiveWorkPatch.Postfix(p, ref disabled);
            Check.Equal(WorkTags.ManualDumb | WorkTags.Violent, disabled, "native backstory restrictions are preserved");
            trait.degreeDatas[0].statOffsets[0].value = .3f;
            Check.Equal(.3f, ClassPassiveEffects.StatOffset(p, stat), "no copied static balance values");
            p.story.traits.allTraits.Add(new Trait(trait));
            Check.Equal(0f, ClassPassiveEffects.StatOffset(p, stat), "legacy trait owns its own bonus");
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(p), "legacy trait owns work tags");
            p.story.traits.allTraits.Clear();
            DefDatabase<ClassDisciplineDef>.Clear();
            Check.Equal(0f, ClassPassiveEffects.StatOffset(p, stat), "missing profile fails closed without erasing state");
            Check.Equal(true, comp.State.Find("rwom.priest") != null, "unknown profile survives");
        }
        finally { DefDatabase<ClassDisciplineDef>.Clear(); foreach (var def in defs) DefDatabase<ClassDisciplineDef>.Add(def); }
    }

    static void NativeMatcher(RuntimeFixture f)
    {
        var harmony = Assembly.Load("0Harmony");
        var read = harmony.GetType("HarmonyLib.PatchProcessor")!.GetMethods().Single(m => m.Name == "GetOriginalInstructions"
            && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        var native = typeof(StatWorker).GetMethod(nameof(StatWorker.GetValueUnfinalized))!;
        var input = read.Invoke(null, [native, null])!;
        var before = ((IEnumerable)input).Cast<object>().ToList();
        var transpiler = typeof(ClassPassiveStatPatch).GetMethod("Transpiler")!;
        var after = ((IEnumerable)transpiler.Invoke(null, [input])!).Cast<object>().ToList();
        var helper = typeof(ClassPassiveEffects).GetMethod(nameof(ClassPassiveEffects.StatOffset))!;
        object? Operand(object instruction) => instruction.GetType().GetField("operand")!.GetValue(instruction);
        Check.Equal(before.Count + 7, after.Count, "native method has unique recognized offset stage");
        Check.Equal(1, after.Count(i => Equals(Operand(i), helper)), "exactly one per-pawn helper call");
        int helperIndex = after.FindIndex(i => Equals(Operand(i), helper));
        int multiplier = after.FindIndex(i => Equals(Operand(i), typeof(Trait).GetMethod(nameof(Trait.MultiplierOfStat))));
        Check.Equal(true, helperIndex < multiplier, "offset injected before native trait multipliers");
        var harmonyType = harmony.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.priest-passive." + Guid.NewGuid();
        var instance = Activator.CreateInstance(harmonyType, id)!;
        try
        {
            var processor = harmonyType.GetMethod("CreateClassProcessor")!.Invoke(instance, [typeof(ClassPassiveStatPatch)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            var patched = ((IEnumerable<MethodBase>)harmonyType.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!).ToList();
            Check.Equal(true, patched.Contains(native), "transformed native IL compiles and attaches");
        }
        finally { harmonyType.GetMethod("UnpatchAll")!.Invoke(instance, [id]); }
        RuntimeLog.AssertClean();
        // A method with no native stage must remain byte-for-byte instruction-identical.
        var noMatch = read.Invoke(null, [typeof(ClassPassiveTests).GetMethod(nameof(Empty), BindingFlags.Public | BindingFlags.Static)!, null])!;
        var unchanged = ((IEnumerable)transpiler.Invoke(null, [noMatch])!).Cast<object>().ToList();
        Check.Equal(true, ((IEnumerable)noMatch).Cast<object>().SequenceEqual(unchanged), "unrecognized native stage fails closed");
        try { RuntimeLog.AssertClean(); throw new Exception("Expected the fail-closed diagnostic"); }
        catch (InvalidOperationException e)
        {
            Check.Equal("Runtime logged warnings/errors:\n[RWoM] Priest progression stat offsets disabled: native trait-offset stage was not uniquely recognized.", e.Message, "only the intentional no-match warning is accepted");
            RuntimeLog.Clear();
        }
    }
    public static void Empty() { }
}
