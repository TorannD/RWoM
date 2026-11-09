using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class NecromancerIdentityTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("NecromancerIdentity.SavedAuthorityAndClassicCompatibility", Identity);
        yield return new("NecromancerIdentity.NativeDefinitionOffsetsAndDeduplication", Passives);
        yield return new("NecromancerIdentity.NativeUndeadMaintenance", Maintenance);
    }

    static CompClassProgression Enroll(Pawn pawn, Vocation vocation = Vocation.Mage)
    {
        var comp = new CompClassProgression { parent = pawn };
        pawn.AllComps.Add(comp);
        comp.BeginNewProgression(vocation);
        comp.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
        return comp;
    }

    static void Identity(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(false, false);
        Check.Equal(false, ClassIdentity.IsNecromancer(p.pawn), "ordinary pawn stays ordinary");
        foreach (var trait in new[] { TorannMagicDefOf.Necromancer, TorannMagicDefOf.Lich })
        {
            p.pawn.story.traits.allTraits.Add(new Trait(trait));
            Check.Equal(true, ClassIdentity.IsNecromancer(p.pawn), "classic class retained");
            Check.Equal(true, TM_Calc.IsNecromancer(p.pawn), "native identity retained");
            Check.Equal(trait == TorannMagicDefOf.Necromancer, ClassIdentity.IsBaseNecromancer(p.pawn), "base-only callers retain old Lich distinction");
            p.pawn.story.traits.allTraits.Clear();
        }
        p.magic.customClass = new TM_CustomClass { isNecromancer = true };
        Check.Equal(true, ClassIdentity.IsNecromancer(p.pawn), "custom magic class retained");
        Check.Equal(false, ClassIdentity.IsBaseNecromancer(p.pawn), "base-only social/scribing checks do not change addons");
        p.magic.customClass = null;
        p.might.customClass = new TM_CustomClass { isNecromancer = true };
        Check.Equal(true, ClassIdentity.IsNecromancer(p.pawn), "custom might class retained");
        p.might.customClass = null;
        var progression = Enroll(p.pawn);
        Check.Equal(true, ClassIdentity.IsNecromancer(p.pawn), "saved identity needs no trait or profile def");
        Check.Equal(true, TM_Calc.IsNecromancer(p.pawn), "native protection sees saved identity");
        Check.Equal(true, ClassIdentity.IsBaseNecromancer(p.pawn), "base social/scribing checks see new identity");
        Check.Equal(0, p.pawn.story.traits.allTraits.Count, "resolution grants no trait");
        progression.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Lich));
        Check.Equal(false, ClassIdentity.IsNecromancer(p.pawn), "unsupported multiple paths cannot fall back to stale trait");
        var hybrid = PawnWithComps(false, false);
        Enroll(hybrid.pawn, Vocation.WarriorMage);
        Check.Equal(false, ClassIdentity.IsNecromancer(hybrid.pawn), "hybrid gameplay remains disabled");
        var previousSkeleton = TorannMagicDefOf.TM_SkeletonLichR;
        try
        {
            var skeleton = PawnWithComps(false, false).pawn;
            TorannMagicDefOf.TM_SkeletonLichR = skeleton.def;
            Check.Equal(true, TM_Calc.IsNecromancer(skeleton), "native skeleton protection retained");
        }
        finally { TorannMagicDefOf.TM_SkeletonLichR = previousSkeleton; }
    }

    static void Passives(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var previous = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<ClassDisciplineDef>.Clear();
            var xml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements("TraitDef").Single(x => (string?)x.Element("defName") == "Necromancer");
            var offsets = xml.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(x => new StatModifier { stat = new StatDef { defName = x.Name.LocalName },
                    value = float.Parse(x.Value, CultureInfo.InvariantCulture) }).ToList();
            Check.Equal(8, offsets.Count, "audited native Necromancer offset set");
            var trait = new TraitDef { defName = "Necromancer",
                degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }] };
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TestNecromancerPassive",
                profileId = "rwom.necromancer", domain = DisciplineDomain.Magic, legacyTrait = trait });
            var pawn = PawnWithComps(false, false).pawn;
            Enroll(pawn);
            foreach (var offset in offsets)
                Check.Equal(offset.value, ClassPassiveEffects.StatOffset(pawn, offset.stat), "runtime native offset " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(pawn), "no Priest vow for Necromancer");
            offsets[0].value = -.7f;
            Check.Equal(-.7f, ClassPassiveEffects.StatOffset(pawn, offsets[0].stat), "runtime balance amendments remain authoritative");
            foreach (var existing in new[] { trait, TorannMagicDefOf.Lich })
            {
                pawn.story.traits.allTraits.Add(new Trait(existing));
                Check.Equal(0f, ClassPassiveEffects.StatOffset(pawn, offsets[0].stat), "native class trait prevents doubled effects");
                pawn.story.traits.allTraits.Clear();
            }
            DefDatabase<ClassDisciplineDef>.Clear();
            Check.Equal(0f, ClassPassiveEffects.StatOffset(pawn, offsets[0].stat), "missing profile fails closed");
            Check.Equal(true, ClassIdentity.IsNecromancer(pawn), "missing metadata does not erase saved ownership");
        }
        finally { DefDatabase<ClassDisciplineDef>.Clear(); foreach (var def in previous) DefDatabase<ClassDisciplineDef>.Add(def); }
    }

    static void Maintenance(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var dismiss = TorannMagicDefOf.TM_DismissUndead;
        var oldClass = dismiss.abilityClass;
        var oldInitialize = dismiss.shouldInitialize;
        try
        {
            dismiss.abilityClass = typeof(PawnAbility); dismiss.shouldInitialize = true;
            var p = PawnWithComps(true, false);
            Enroll(p.pawn);
            var owned = PawnWithComps(false, false).pawn;
            p.magic.supportedUndead = [null!, owned];
            var data = p.magic.MagicData;
            data.MagicUserXP = 37;
            p.magic.ResolveUndead();
            Check.Equal(1, p.magic.supportedUndead.Count, "native maintenance removes missing owned pawn");
            var ability = p.magic.AbilityData.Powers.Single(a => a.Def == dismiss);
            p.magic.ResolveUndead();
            Check.Same(ability, p.magic.AbilityData.Powers.Single(a => a.Def == dismiss), "repeat maintenance keeps exact runtime ability");
            p.magic.supportedUndead.Clear();
            p.magic.ResolveUndead();
            Check.Equal(false, p.magic.AbilityData.Powers.Any(a => a.Def == dismiss), "last minion gone removes native dismiss ability");
            Check.Same(data, p.magic.MagicData, "maintenance retains data owner");
            Check.Equal(37, data.MagicUserXP, "maintenance grants no XP");
            Check.Equal(false, data.MagicPowersN.Any(power => power.learned), "maintenance learns no class spells");
            var harmony = Assembly.Load("0Harmony");
            var read = harmony.GetType("HarmonyLib.PatchProcessor")!.GetMethods().Single(m => m.Name == "GetOriginalInstructions"
                && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
            var il = (IEnumerable)read.Invoke(null, [typeof(CompAbilityUserMagic).GetMethod("CompTick")!, null])!;
            var resolver = typeof(ClassIdentity).GetMethod(nameof(ClassIdentity.IsNecromancer))!;
            Check.Equal(true, il.Cast<object>().Any(i => Equals(i.GetType().GetField("operand")!.GetValue(i), resolver)),
                "actual comp tick routes through saved identity resolver");
        }
        finally { dismiss.abilityClass = oldClass; dismiss.shouldInitialize = oldInitialize; }
    }
}
