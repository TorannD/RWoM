using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthHealthPlanTests
{
    const BindingFlags Hidden = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
    static Type PlanType => typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthHealthPlan", true)!;
    static bool Prepare(Pawn pawn, out object? plan, out Hediff? unsupported)
    {
        object?[] args = [pawn, null, null];
        bool result = (bool)PlanType.GetMethod("TryPrepare", Hidden)!.Invoke(null, args)!;
        plan = args[1]; unsupported = args[2] as Hediff; return result;
    }
    static bool Apply(object plan) => (bool)PlanType.GetMethod("Apply", Hidden)!.Invoke(plan, null)!;

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRebirthHealthPlan.ApothecaryStockRetainsOriginalOwner", ApothecaryStock);
        yield return new("SpiritRebirthHealthPlan.BardSongsRetainTheirOriginalOwner", BardSongs);
        yield return new("SpiritRebirthHealthPlan.NativeResourceAndEnchantmentMarkers", NativeMarkers);
        yield return new("SpiritRebirthHealthPlan.UnsupportedLinkLeavesOriginalGraph", Unsupported);
        yield return new("SpiritRebirthHealthPlan.NativePhysicalRemovalKeepsExactClassMarker", PhysicalRemoval);
        yield return new("SpiritRebirthHealthPlan.StaleSnapshotCannotApply", Stale);
        yield return new("SpiritRebirthHealthPlan.UnknownCompAndIdentityImplantRejected", UnsafePhysical);
    }

    static void ApothecaryStock(RuntimeFixture r)
    {
        using var f=new SpiritRebirthStateTests.Fixture(r);Dead(f.Original);
        using var defs=new HealthDefinitions();
        var def=new HediffDef { defName="TM_ApothecaryHerbsHD",hediffClass=typeof(TorannMagic.Hediff_ApothecaryHerbs),maxSeverity=100 };
        DefDatabase<HediffDef>.Add(def);
        var herbs=new TorannMagic.Hediff_ApothecaryHerbs {def=def,pawn=f.Original};
        herbs.Severity=37.25f;Set(herbs,"herbPwr",3);Set(herbs,"herbEff",2);Set(herbs,"herbVer",4);
        f.Original.health.hediffSet.hediffs.Add(herbs);
        herbs.Part=Part();
        Check.Equal(false,Prepare(f.Original,out _,out _),"body-attached foreign stock must not be silently retained");
        herbs.Part=null;
        def.comps=[new HediffCompProperties {compClass=typeof(HediffComp_SeverityPerDay)}];
        Check.Equal(false,Prepare(f.Original,out _,out _),"patched resource callbacks need explicit adapter");
        def.comps=null;
        Check.Equal(true,Prepare(f.Original,out var plan,out _),"native herbs do not block ritual");
        Check.Equal(true,Apply(plan!),"conversion preserves stock rather than refilling/removing it");
        SameGraph(f.Original,[herbs],"same herb stock retained");
        Check.Equal(37.25f,herbs.Severity,"quantity unchanged");
        Check.Equal(130f,herbs.MaxSeverity,"trained capacity unchanged");
        Check.Same(f.Original,herbs.pawn,"stock keeps original owner");
    }

    static Hediff_ClassDiscipline Marker(Pawn pawn, BodyPartRecord part)
        => new() { pawn = pawn, Part = part, profileId = "rwom.necromancer",
            def = DefDatabase<HediffDef>.GetNamed("TM_ClassDiscipline") };
    sealed class HealthDefinitions : IDisposable
    {
        readonly HediffDef[] original = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        public HealthDefinitions()
        {
            DefDatabase<HediffDef>.Clear();
            DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_ClassDiscipline", hediffClass = typeof(Hediff_ClassDiscipline) });
            DefDatabase<HediffDef>.Add(new HediffDef { defName = "CochlearImplant", hediffClass = typeof(Hediff_AddedPart) });
        }
        public void Dispose()
        {
            DefDatabase<HediffDef>.Clear();
            foreach (var def in original) DefDatabase<HediffDef>.Add(def);
        }
    }
    static BodyPartRecord Part() => new() { def = new BodyPartDef { defName = "SpiritHealthFixturePart" } };
    static void Dead(Pawn pawn) => Set(pawn.health, "healthState", PawnHealthState.Dead);
    static void NativeMarkers(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); Dead(f.Original);
        using var defs = new HealthDefinitions();
        var markers = new List<Hediff>();
        foreach (var name in new[] {"TM_MagicUserHD", "TM_MightUserHD", "TM_HediffEnchantment_dmgBonus", "TM_LightCapacitanceHD", "TM_TechnoBitHD"})
        {
            var type = name switch {
                "TM_HediffEnchantment_dmgBonus" => typeof(CompClassProgression).Assembly.GetType("TorannMagic.Enchantment.HediffComp_Enchantment", true)!,
                "TM_LightCapacitanceHD" => typeof(TorannMagic.HediffComp_LightCapacitance),
                "TM_TechnoBitHD" => typeof(CompClassProgression).Assembly.GetType("TorannMagic.HediffComp_TechnoBit", true)!,
                _ => typeof(HediffComp_SeverityPerDay)
            };
            var def = new HediffDef {defName=name, hediffClass=typeof(HediffWithComps), comps=[new HediffCompProperties {compClass=type}]};
            DefDatabase<HediffDef>.Add(def);
            var marker = new HediffWithComps {def=def,pawn=f.Original};
            var component = (HediffComp)Activator.CreateInstance(type)!;
            component.parent = marker;
            marker.comps = [component];
            markers.Add(marker);
        }
        f.Original.health.hediffSet.hediffs.AddRange(markers);
        var light = ((HediffWithComps)markers[^2]).TryGetComp<TorannMagic.HediffComp_LightCapacitance>();
        light.LightEnergy = 61.25f;
        var bit = ((HediffWithComps)markers[^1]).comps.Single();
        Set(bit, "nextBitGrenade", 421);
        markers[^1].Part = Part();
        Check.Equal(false, Prepare(f.Original, out _, out var unsupported), "a body-attached bit needs its own adapter");
        Check.Same(markers[^1], unsupported!, "the body-attached effect is identified");
        markers[^1].Part = null;
        Check.Equal(true, Prepare(f.Original, out var plan, out _), "native class resource markers accepted");
        Check.Equal(true, Apply(plan!), "resource markers preserved at payment");
        SameGraph(f.Original, markers.ToArray(), "existing resource markers retained");
        Check.Equal(61.25f, light.LightEnergy, "stored sunlight retained");
        Check.Equal(421, (int)bit.GetType().GetField("nextBitGrenade", Hidden)!.GetValue(bit)!, "scheduled grenade retained");
    }
    static void BardSongs(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); Dead(f.Original);
        using var definitions = new HealthDefinitions();
        var severity = typeof(HediffComp_SeverityPerDay);
        var assembly = typeof(CompClassProgression).Assembly;
        var songs = new Dictionary<string, Type[]> {
            ["TM_EntertainingHD"] = [severity],
            ["TM_InspirationalHD"] = [assembly.GetType("TorannMagic.HediffComp_Inspirational", true)!, severity],
            ["TM_SingBattleHymnHD"] = [severity, assembly.GetType("TorannMagic.HediffComp_BattleHymn", true)!]
        };
        foreach (var (name, components) in songs)
        {
            var def = new HediffDef { defName = name, hediffClass = typeof(HediffWithComps),
                comps = components.Select(t => new HediffCompProperties { compClass = t }).ToList() };
            DefDatabase<HediffDef>.Add(def);
            var song = new HediffWithComps { pawn = f.Original, def = def, comps = [] };
            foreach (var type in components)
            {
                var component = (HediffComp)Activator.CreateInstance(type)!;
                component.parent = song; song.comps.Add(component);
            }
            f.Original.health.hediffSet.hediffs.Add(song);
            song.Part = Part();
            Check.Equal(false, Prepare(f.Original, out _, out _), name + " attached to a body part rejected");
            song.Part = null;
            song.comps[0].parent = null;
            Check.Equal(false, Prepare(f.Original, out _, out _), name + " detached component rejected");
            song.comps[0].parent = song;
            Check.Equal(true, Prepare(f.Original, out _, out _), name + " supported on its original owner");
        }
        var original = f.Original.health.hediffSet.hediffs.ToArray();
        Check.Equal(true, Prepare(f.Original, out var plan, out _) && Apply(plan!), "convert all three native songs");
        SameGraph(f.Original, original, "each song remains with the soul");
    }

    static void SameGraph(Pawn pawn, Hediff[] before, string reason)
    {
        Check.Equal(before.Length, pawn.health.hediffSet.hediffs.Count, reason + " count");
        for (int i = 0; i < before.Length; i++) Check.Same(before[i], pawn.health.hediffSet.hediffs[i], reason + " exact entry " + i);
        Check.Equal(true, pawn.Dead, reason + " dead state");
    }

    static void Unsupported(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); Dead(f.Original);
        using var defs = new HealthDefinitions();
        var marker = Marker(f.Original, Part());
        var link = new Hediff_PsychicBond { pawn = f.Original, target = f.Other,
            def = new HediffDef { defName = "PsychicBond" } };
        var injury = new Hediff_Injury { pawn = f.Original, Part = marker.Part,
            def = new HediffDef { defName = "Cut" } };
        f.Original.health.hediffSet.hediffs.AddRange([injury, marker, link]);
        var before = f.Original.health.hediffSet.hediffs.ToArray();
        var magic = f.Magic.MagicData; var might = f.Might.MightData;
        Check.Equal(false, Prepare(f.Original, out var plan, out var unsupported), "active external link blocks entire plan");
        Check.Equal(true, plan == null, "no partial plan published");
        Check.Same(link, unsupported!, "specific unsupported health entry returned");
        SameGraph(f.Original, before, "rejected preparation leaves full original graph");
        Check.Same(f.Other, link.target, "other pawn link intact");
        Check.Same(magic, f.Magic.MagicData, "magic owner untouched");
        Check.Same(might, f.Might.MightData, "might owner untouched");
    }

    static void PhysicalRemoval(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); Dead(f.Original);
        var corpse = new Corpse { InnerPawn = f.Original };
        Set(f.Original, "mapIndexOrState", (sbyte)-2);
        Check.Same(corpse, f.Original.ParentHolder, "kill-finalized body held by exact corpse");
        using var defs = new HealthDefinitions();
        using var capacities = (IDisposable)Activator.CreateInstance(typeof(SpiritNativeResurrectionTests)
            .GetNestedType("CapacityDefinitions", BindingFlags.NonPublic)!, true)!;
        var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
        world.factionManager = new FactionManager();
        Set(Current.Game, "worldInt", world); Set(Current.Game, "tickManager", new TickManager());
        var part = Part(); var marker = Marker(f.Original, part);
        part.def.hitPoints = 100; part.coverage = 1f;
        var missingPart = Part(); missingPart.parent = part; missingPart.coverage = .3f;
        var implantPart = Part(); implantPart.parent = part; implantPart.coverage = .3f;
        part.parts.AddRange([missingPart, implantPart]);
        f.Original.def.race.body = new BodyDef { defName = "SpiritHealthFixtureBody", corePart = part };
        f.Original.def.race.body.ResolveReferences();
        var injury = new Hediff_Injury { pawn = f.Original, Part = part, def = new HediffDef { defName = "Cut" } };
        var missing = new Hediff_MissingPart { pawn = f.Original, Part = missingPart, def = new HediffDef { defName = "MissingBodyPart" } };
        var implant = new Hediff_AddedPart { pawn = f.Original, Part = implantPart,
            def = DefDatabase<HediffDef>.GetNamed("CochlearImplant") };
        var bloodDef = new HediffDef { defName="TM_BloodHD", hediffClass=typeof(HediffWithComps),
            comps=[new HediffCompProperties {compClass=typeof(TorannMagic.HediffComp_Blood)}] };
        DefDatabase<HediffDef>.Add(bloodDef);
        var blood = new HediffWithComps {def=bloodDef,pawn=f.Original};
        blood.comps = [new TorannMagic.HediffComp_Blood {parent=blood}];
        var crowDef = new HediffDef { defName="TM_CrowInsightHD", hediffClass=typeof(HediffWithComps),
            comps=[new HediffCompProperties { compClass=typeof(HediffComp_CrowInsight) }] };
        DefDatabase<HediffDef>.Add(crowDef);
        var crow = new HediffWithComps {def=crowDef,pawn=f.Original};
        crow.comps = [new HediffComp_CrowInsight {parent=crow}];
        f.Original.health.hediffSet.hediffs.AddRange([injury, marker, missing, implant, blood, crow]);
        var physicalClassEffects = new Dictionary<string, Type> {
            ["TM_SoulBondMentalHD"] = typeof(CompClassProgression).Assembly.GetType("TorannMagic.HediffComp_SoulBondHost",true)!,
            ["TM_SoulBondPhysicalHD"] = typeof(CompClassProgression).Assembly.GetType("TorannMagic.HediffComp_SoulBondHost",true)!,
            ["TM_WDSoulBondMentalHD"] = typeof(CompClassProgression).Assembly.GetType("TorannMagic.HediffComp_SoulBond",true)!,
            ["TM_SDSoulBondPhysicalHD"] = typeof(CompClassProgression).Assembly.GetType("TorannMagic.HediffComp_SoulBond",true)!,
            ["TM_EmpathHD"] = typeof(TorannMagic.HediffComp_Empath), ["TM_SuppressiveAuraHD"] = typeof(TorannMagic.HediffComp_SuppressiveAura),
            ["TM_HarvestPassionHD"] = typeof(CompClassProgression).Assembly.GetType("TorannMagic.HediffComp_HarvestPassion",true)!,
            ["TM_EmotionSuppressionHD"] = typeof(HediffComp_SeverityPerDay),
            ["TM_OutOfBodyHD"] = typeof(TorannMagic.HediffComp_SymbiosisCaster), ["TM_SymbiosisHD"] = typeof(TorannMagic.HediffComp_SymbiosisHost)
        };
        foreach (var (name,type) in physicalClassEffects)
        {
            var definition = new HediffDef {defName=name,hediffClass=typeof(HediffWithComps), comps=[new HediffCompProperties{compClass=type}]};
            DefDatabase<HediffDef>.Add(definition);
            var h = new HediffWithComps {pawn=f.Original,def=definition};
            var component=(HediffComp)Activator.CreateInstance(type)!; component.parent=h;h.comps=[component];
            f.Original.health.hediffSet.hediffs.Add(h);
        }
        blood.Part = part;
        Check.Equal(false,Prepare(f.Original,out _,out _),"modified body-part blood effect excluded");
        blood.Part = null;
        Check.Equal(1, f.Original.health.hediffSet.GetMissingPartsCommonAncestors().Count, "prime real missing-part cache before cleanup");
        var magic = f.Magic.MagicData; var might = f.Might.MightData;
        Check.Equal(true, Prepare(f.Original, out var plan, out _), "known physical conditions accepted");
        Check.Equal(6 + physicalClassEffects.Count, f.Original.health.hediffSet.hediffs.Count, "preparation removes nothing");
        Check.Equal(true, Apply(plan!), "native health removal succeeds");
        Check.Equal(1, f.Original.health.hediffSet.hediffs.Count, "only class marker survives");
        Check.Same(marker, f.Original.health.hediffSet.hediffs[0], "original class marker instance retained");
        Check.Equal(0, f.Original.health.hediffSet.GetMissingPartsCommonAncestors().Count, "native missing-part cache refreshed");
        Check.Same(part, marker.Part, "body-attached marker is not misclassified as injury");
        Check.Same(magic, f.Magic.MagicData, "magic owner retained");
        Check.Same(might, f.Might.MightData, "might owner retained");
        Check.Equal(true, f.Original.Dead, "cleanup does not resurrect");
        Check.Equal(false, Apply(plan!), "plan cannot replay callbacks");
        Check.Same(marker, f.Original.health.hediffSet.hediffs.Single(), "repeated application leaves marker untouched");
    }

    static void Stale(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); Dead(f.Original);
        using var defs = new HealthDefinitions();
        var marker = Marker(f.Original, Part());
        var missing = new Hediff_MissingPart { pawn = f.Original, Part = marker.Part,
            def = new HediffDef { defName = "MissingBodyPart" } };
        f.Original.health.hediffSet.hediffs.AddRange([marker, missing]);
        Check.Equal(true, Prepare(f.Original, out var plan, out _), "prepare valid initial graph");
        var unknown = new Hediff { pawn = f.Original, def = new HediffDef { defName = "ForeignIdentity" } };
        f.Original.health.hediffSet.hediffs.Add(unknown);
        var changed = f.Original.health.hediffSet.hediffs.ToArray();
        Check.Equal(false, Apply(plan!), "new effect invalidates pending plan");
        SameGraph(f.Original, changed, "stale plan changes nothing");
    }

    sealed class UnknownComp : HediffComp { }
    static void UnsafePhysical(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); Dead(f.Original);
        using var defs = new HealthDefinitions();
        var effect = new Hediff_Injury { pawn = f.Original, Part = Part(), def = new HediffDef { defName = "Cut" } };
        effect.comps = [new UnknownComp { parent = effect }];
        f.Original.health.hediffSet.hediffs.Add(effect);
        Check.Equal(false, Prepare(f.Original, out _, out _), "familiar physical type with unknown callback is rejected");
        Check.Same(effect, f.Original.health.hediffSet.hediffs.Single(), "unknown callback graph retained");
        var identity = new Hediff_Implant { pawn = f.Original, Part = effect.Part,
            def = new HediffDef { defName = "ForeignProgressionImplant" } };
        f.Original.health.hediffSet.hediffs.Clear(); f.Original.health.hediffSet.hediffs.Add(identity);
        Check.Equal(false, Prepare(f.Original, out _, out _), "exact implant runtime type cannot authorize arbitrary mod identity removal");
        Check.Same(identity, f.Original.health.hediffSet.hediffs.Single(), "identity implant retained");
        var marker = Marker(f.Original, effect.Part);
        marker.def.forceRemoveOnResurrection = true;
        f.Original.health.hediffSet.hediffs.Clear(); f.Original.health.hediffSet.hediffs.Add(marker);
        Check.Equal(false, Prepare(f.Original, out _, out _), "class marker configured for native resurrection removal cannot be promised preserved");
        Check.Same(marker, f.Original.health.hediffSet.hediffs.Single(), "unsafe marker remains untouched on rejection");
    }

}
