using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthBodyEffectsTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRebirthBodyEffects.IndependentLichHealthAndSpawn", Lich);
        yield return new("SpiritRebirthBodyEffects.IndependentResolveAndHost", Bright);
        yield return new("SpiritRebirthBodyEffects.AllFormsPreventLegacyFallback", Forms);
        yield return new("SpiritRebirthBodyEffects.IndependentWarriorResolve", Warrior);
        yield return new("SpiritRebirthBodyEffects.PassivesNeutralWithoutChangingOwnership", Passives);
    }

    static IDisposable Fixture(Type owner, params object[] args)
        => (IDisposable)Activator.CreateInstance(owner.GetNestedType("Fixture", BindingFlags.NonPublic)!, args)!;
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name)!.GetValue(obj)!;

    static void Lich(RuntimeFixture runtime)
    {
        using var f = Fixture(typeof(LichTransformationTests), runtime, InitiationStage.Ascended);
        var pawn = Field<Pawn>(f, "Pawn"); var magic = Field<CompAbilityUserMagic>(f, "Magic");
        var comp = Field<CompClassProgression>(f, "Progression"); var hd = Field<HediffDef>(f, "LichHealth");
        Check.Equal(true, SpiritRebirthStateTests.Invoke(comp.State, "ActivateLichForm"), "activate retained lich ownership");
        LichTransformation.RestoreHealth(pawn);
        Check.Equal(true, pawn.health.hediffSet.HasHediff(hd), "unmarked native restoration remains functional");
        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(hd));
        var state = comp.State; var owner = magic.MagicData.MagicPowersN[0]; owner.learned = true; owner.level = 2;
        var ability = Field<AbilityUser.PawnAbility>(f, "Ability"); ability.CooldownTicksLeft = 123;
        Check.Equal(true, SpiritRebirthStateTests.Invoke(comp, "BeginSpiritRebirth"), "mark original");
        LichTransformation.RestoreHealth(pawn);
        Check.Equal(false, pawn.health.hediffSet.HasHediff(hd), "independent RestoreHealth cannot rebuild former lich body");
        comp.PostSpawnSetup(true);
        Check.Equal(false, pawn.health.hediffSet.HasHediff(hd), "native PostSpawnSetup cannot rebuild former lich body");
        LichTransformation.Restore(magic);
        Check.Equal(true, ReferenceEquals(state, comp.State), "class state owner retained");
        Check.Equal(true, ClassIdentity.IsLich(pawn), "lich ownership remains available for progression");
        Check.Equal(true, ClassPassiveEffects.Lich(pawn) == null, "mental/stat passive authority is suppressed");
        Check.Equal(true, ReferenceEquals(owner, magic.MagicData.MagicPowersN[0]), "learned owner retained");
        Check.Equal(true, owner.learned, "learned state retained"); Check.Equal(2, owner.level, "rank retained");
        Check.Equal(true, magic.AbilityData.Powers.Contains(ability), "runtime owner retained");
        Check.Equal(123, ability.CooldownTicksLeft, "cooldown retained");
    }

    static void Bright(RuntimeFixture runtime)
    {
        using var f = Fixture(typeof(BrightmageLifecycleTests), runtime);
        var caster = f.GetType().GetMethod("Caster")!;
        var original = ((Pawn pawn, CompAbilityUserMagic magic))caster.Invoke(f, ["rwom.brightmage", true])!;
        var host = ((Pawn pawn, CompAbilityUserMagic magic))caster.Invoke(f, ["rwom.brightmage", true])!;
        var resource = Field<HediffDef>(f, "ResourceDef");
        var comp = ClassIdentity.Component(original.pawn);
        Check.Equal(true, SpiritRebirthStateTests.Invoke(comp, "BeginSpiritRebirth"), "mark original only");
        Check.Equal(true, BrightmageLifecycle.Restore(original.magic), "marked restoration handled");
        Check.Equal(false, original.pawn.health.hediffSet.HasHediff(resource), "direct restore cannot add resource");
        original.magic.ResolveClassSkills();
        Check.Equal(false, original.pawn.health.hediffSet.HasHediff(resource), "independent skill resolution cannot add resource");
        BrightmageLifecycle.Restore(host.magic);
        Check.Equal(true, host.pawn.health.hediffSet.HasHediff(resource), "unmarked host retains native class restoration");
    }

    static void Forms(RuntimeFixture runtime)
    {
        using var f = new SpiritRebirthStateTests.Fixture(runtime);
        Check.Equal(false, BrightmageLifecycle.Restore(f.Magic), "unmarked legacy fallback unchanged");
        var marker = f.Begin();
        int validVersion = marker.Version;
        foreach (var form in Enum.GetValues<SpiritRebirthForm>().Append((SpiritRebirthForm)999))
        foreach (int version in new[] { validVersion, -1 })
        {
            Set(marker, "form", form);
            Set(marker, "version", version);
            Check.Equal(true, BrightmageLifecycle.Restore(f.Magic), "all marked forms handle bright restoration");
            Check.Equal(true, GolemancerLifecycle.Restore(f.Magic), "all marked forms handle golem restoration");
            Check.Equal(true, EmpathLifecycle.Restore(f.Magic), "all marked forms handle empath restoration");
            Check.Equal(true, ShamanLifecycle.Restore(f.Magic), "all marked forms handle shaman restoration");
            f.Magic.ResolveClassSkills();
            Check.Equal(0f, ClassPassiveEffects.StatOffset(f.Original, null), "neutral offsets");
            Check.Equal(1f, ClassPassiveEffects.StatFactor(f.Original, null), "neutral factors");
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(f.Original), "neutral work restrictions");
        }
    }

    static void Warrior(RuntimeFixture runtime)
    {
        using var f = Fixture(typeof(LichTransformationTests), runtime, InitiationStage.Ascended);
        var pawn = Field<Pawn>(f, "Pawn"); var comp = Field<CompClassProgression>(f, "Progression");
        var might = pawn.AllComps.OfType<CompAbilityUserMight>().Single();
        var hd = Field<HediffDef>(f, "LichHealth");
        might.customClass = new TorannMagic.TMDefs.TM_CustomClass { classHediff = hd, hediffSeverity = .5f };
        might.ResolveClassSkills();
        Check.Equal(true, pawn.health.hediffSet.HasHediff(hd), "unmarked warrior custom class restores native effect");
        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(hd));
        Check.Equal(true, SpiritRebirthStateTests.Invoke(comp, "BeginSpiritRebirth"), "mark former warrior body");
        var data = might.MightData; var custom = might.customClass;
        might.ResolveClassSkills();
        Check.Equal(false, pawn.health.hediffSet.HasHediff(hd), "marked warrior cannot restore custom body effect");
        Check.Equal(true, ReferenceEquals(data, might.MightData), "warrior investment owner retained");
        Check.Equal(true, ReferenceEquals(custom, might.customClass), "custom class identity retained");
    }

    static void Passives(RuntimeFixture runtime)
    {
        using var f = Fixture(typeof(LichTransformationTests), runtime, InitiationStage.Ascended);
        var pawn = Field<Pawn>(f, "Pawn"); var comp = Field<CompClassProgression>(f, "Progression");
        Check.Equal(true, SpiritRebirthStateTests.Invoke(comp.State, "ActivateLichForm"), "retain lich authority");
        var old = TorannMagicDefOf.Lich;
        try
        {
            var stat = new StatDef { defName = "FormerBodyTestStat" };
            TorannMagicDefOf.Lich = new TraitDef { defName = "Lich", disabledWorkTags = WorkTags.Caring,
                degreeDatas = [new TraitDegreeData { degree = 0,
                    statOffsets = [new StatModifier { stat = stat, value = 3f }],
                    statFactors = [new StatModifier { stat = stat, value = 2f }] }] };
            Check.Equal(3f, ClassPassiveEffects.StatOffset(pawn, stat), "unmarked offset retained");
            Check.Equal(2f, ClassPassiveEffects.StatFactor(pawn, stat), "unmarked factor retained");
            Check.Equal(WorkTags.Caring, ClassPassiveEffects.DisabledWork(pawn), "unmarked work restriction retained");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(comp, "BeginSpiritRebirth"), "mark passive owner");
            Check.Equal(0f, ClassPassiveEffects.StatOffset(pawn, stat), "former offset neutral");
            Check.Equal(1f, ClassPassiveEffects.StatFactor(pawn, stat), "former factor neutral");
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(pawn), "former work restriction neutral");
            Check.Equal(true, ClassIdentity.IsLich(pawn), "ownership remains true");
        }
        finally { TorannMagicDefOf.Lich = old; }
    }
}
