using System.Collections;
using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class LichPassiveTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LichPassive.SavedTransformationOwnership", Ownership);
        yield return new("LichPassive.NativeFactorStageAndPatchAttachment", NativeMatcher);
    }

    static void Ownership(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        var originalLich = TorannMagicDefOf.Lich;
        try
        {
            DefDatabase<ClassDisciplineDef>.Clear();
            var stat = new StatDef { defName = "LichPassiveStat" };
            var allowed = new MentalBreakDef { defName = "LichAllowed" };
            var existing = new MentalBreakDef { defName = "OtherTraitAllowed" };
            var blocked = new MentalStateDef { defName = "LichBlockedState" };
            TorannMagicDefOf.Lich = new TraitDef { defName = "Lich", disabledWorkTags = WorkTags.Caring | WorkTags.ManualDumb,
                degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = [new StatModifier { stat = stat, value = -2f }],
                    statFactors = [new StatModifier { stat = stat, value = 0f }],
                    theOnlyAllowedMentalBreaks = [allowed], disallowedMentalStates = [blocked] }] };
            var necro = new TraitDef { defName = "Necromancer", degreeDatas = [new TraitDegreeData { degree = 0,
                statOffsets = [new StatModifier { stat = stat, value = -.3f }] }] };
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "LichPassiveNecro", profileId = "rwom.necromancer", domain = DisciplineDomain.Magic, legacyTrait = necro });
            var p = PawnWithComps(false, false).pawn;
            var comp = new CompClassProgression { parent = p }; p.AllComps.Add(comp);
            Check.Equal(1f, ClassPassiveEffects.StatFactor(p, stat), "classic unchanged");
            comp.BeginNewProgression(Vocation.Mage);
            comp.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
            var path = comp.State.Find("rwom.necromancer");
            Set(path, "lichFormActive", true);
            Check.Equal(-.3f, ClassPassiveEffects.StatOffset(p, stat), "malformed early transformation cannot acquire Lich effects");
            Set(path, "stage", InitiationStage.Ascended);
            Check.Equal(-2f, ClassPassiveEffects.StatOffset(p, stat), "Lich replaces Necromancer offset");
            Check.Equal(0f, ClassPassiveEffects.StatFactor(p, stat), "zero native factor preserved");
            Check.Equal(1f, ClassPassiveEffects.StatFactor(p, new StatDef()), "absent factor neutral");
            Check.Equal(WorkTags.Caring | WorkTags.ManualDumb, ClassPassiveEffects.DisabledWork(p), "Lich restrictions read from trait");
            IEnumerable<MentalBreakDef> whitelist = new[] { existing };
            LichMentalBreakWhitelistPatch.Postfix(p, ref whitelist);
            Check.Equal(true, whitelist.SequenceEqual(new[] { existing, allowed }), "native whitelist union and fallback remain native");
            var worker = new MentalBreakWorker { def = new MentalBreakDef { mentalState = blocked } };
            bool canOccur = true;
            LichMentalBreakRestrictionPatch.Postfix(worker, p, ref canOccur);
            Check.Equal(false, canOccur, "native disallowed state gate");
            worker.def.mentalState = new MentalStateDef(); canOccur = false;
            LichMentalBreakRestrictionPatch.Postfix(worker, p, ref canOccur);
            Check.Equal(false, canOccur, "native denials never reversed");
            Check.Equal(0, p.story.traits.allTraits.Count, "no temporary or permanent trait inserted");
            p.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Lich));
            Check.Equal(0f, ClassPassiveEffects.StatOffset(p, stat), "native trait owns offsets");
            Check.Equal(1f, ClassPassiveEffects.StatFactor(p, stat), "native trait owns factors");
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(p), "native trait owns work restrictions");
            whitelist = new[] { existing };
            LichMentalBreakWhitelistPatch.Postfix(p, ref whitelist);
            Check.Equal(1, whitelist.Count(), "native trait owns whitelist");
        }
        finally { TorannMagicDefOf.Lich = originalLich; DefDatabase<ClassDisciplineDef>.Clear(); foreach (var def in profiles) DefDatabase<ClassDisciplineDef>.Add(def); }
    }

    static void NativeMatcher(RuntimeFixture f)
    {
        var harmony = Assembly.Load("0Harmony");
        var read = harmony.GetType("HarmonyLib.PatchProcessor")!.GetMethods().Single(m => m.Name == "GetOriginalInstructions"
            && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        var native = typeof(StatWorker).GetMethod(nameof(StatWorker.GetValueUnfinalized))!;
        var input = read.Invoke(null, [native, null])!;
        var before = ((IEnumerable)input).Cast<object>().ToList();
        var transpiler = typeof(LichPassiveFactorPatch).GetMethod("Transpiler")!;
        var after = ((IEnumerable)transpiler.Invoke(null, [input])!).Cast<object>().ToList();
        object? Operand(object instruction) => instruction.GetType().GetField("operand")!.GetValue(instruction);
        var helper = typeof(ClassPassiveEffects).GetMethod(nameof(ClassPassiveEffects.StatFactor))!;
        Check.Equal(before.Count + 7, after.Count, "native factor stage recognized");
        Check.Equal(1, after.Count(i => Equals(Operand(i), helper)), "one factor application");
        var index = after.FindIndex(i => Equals(Operand(i), helper));
        Check.Equal(true, index > after.FindLastIndex(i => Equals(Operand(i), typeof(StatWorker).GetMethod(nameof(StatWorker.StatOffsetFromGear)))), "factor follows equipment offsets");
        Check.Equal(true, index < after.FindIndex(i => Equals(Operand(i), typeof(Trait).GetMethod(nameof(Trait.MultiplierOfStat)))), "factor precedes native trait multiplier");
        var harmonyType = harmony.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.lich-passive." + Guid.NewGuid();
        var instance = Activator.CreateInstance(harmonyType, id)!;
        try
        {
            foreach (var patch in new[] { typeof(ClassPassiveStatPatch), typeof(LichPassiveFactorPatch), typeof(LichMentalBreakWhitelistPatch), typeof(LichMentalBreakRestrictionPatch) })
            {
                var processor = harmonyType.GetMethod("CreateClassProcessor")!.Invoke(instance, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
            var patched = ((IEnumerable<MethodBase>)harmonyType.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!).ToList();
            Check.Equal(true, patched.Contains(native), "both stat transforms compile together");
            Check.Equal(true, patched.Contains(typeof(TraitSet).GetProperty(nameof(TraitSet.TheOnlyAllowedMentalBreaks))!.GetMethod!), "native whitelist hook attaches");
            Check.Equal(true, patched.Contains(typeof(MentalBreakWorker).GetMethod(nameof(MentalBreakWorker.BreakCanOccur))!), "native restriction hook attaches");
        }
        finally { harmonyType.GetMethod("UnpatchAll")!.Invoke(instance, [id]); }
        RuntimeLog.AssertClean();
    }
}
