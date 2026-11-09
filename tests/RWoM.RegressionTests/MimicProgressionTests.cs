using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class MimicProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MimicProgression.NativeTraitlessPoolsAndLockedOwners", NativePools);
        yield return new("MimicProgression.DemonCanonicalOwnersAndBranches", Demons);
        yield return new("MimicProgression.LegacyBardAndLichCompatibility", Legacy);
        yield return new("MimicProgression.BrightmageNativeCopyFlagsRanksAndCustomBoundary", Brightmage);
        yield return new("MimicProgression.ShamanNativeCopyFlagsRanksAndCustomBoundary", Shaman);
        yield return new("MimicProgression.EmpathNativeFamiliesRemainUncopyable", Empath);
        yield return new("MimicProgression.GolemancerNativeFamiliesRemainUncopyable", Golemancer);
    }

    static (Pawn pawn, CompAbilityUserMagic magic, CompClassProgression progression) Target(string id)
    {
        var p = PawnWithComps(true, false);
        var progression = new CompClassProgression { parent = p.pawn };
        p.pawn.AllComps.Add(progression);
        progression.BeginNewProgression(Vocation.Mage);
        Check.Equal(true, progression.State.TryAddFoundation(id, DisciplineDomain.Magic), "seed saved discipline");
        foreach (var power in p.magic.MagicData.AllMagicPowers) { power.learned = false; power.level = 0; }
        return (p.pawn, p.magic, progression);
    }

    static TMAbilityDef? Copy(Pawn target, Pawn caster)
    {
        Rand.PushState(9341);
        try
        {
            for (int i = 0; i < 32; i++)
            {
                var result = TM_Calc.GetCopiedMagicAbility(target, caster);
                if (result != null) return result;
            }
            return null;
        }
        finally { Rand.PopState(); }
    }

    static void NativePools(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var routes = new (string id, Func<MagicData, List<MagicPower>> list, int index, TMAbilityDef ability, bool ranked)[] {
            ("rwom.arcanist", d => d.MagicPowersA, 0, TorannMagicDefOf.TM_Shadow_II, true),
            ("rwom.lightning", d => d.MagicPowersSB, 0, TorannMagicDefOf.TM_AMP_II, true),
            ("rwom.fire", d => d.MagicPowersIF, 0, TorannMagicDefOf.TM_RayofHope_II, true),
            ("rwom.ice", d => d.MagicPowersHoF, 0, TorannMagicDefOf.TM_Soothe_II, true),
            ("rwom.druid", d => d.MagicPowersD, 1, TorannMagicDefOf.TM_SootheAnimal_II, true),
            ("rwom.necromancer", d => d.MagicPowersN, 1, TorannMagicDefOf.TM_DeathMark_II, true),
            ("rwom.paladin", d => d.MagicPowersP, 1, TorannMagicDefOf.TM_Shield_II, true),
            ("rwom.priest", d => d.MagicPowersPR, 2, TorannMagicDefOf.TM_HealingCircle_II, true),
            ("rwom.summoner", d => d.MagicPowersS, 1, TorannMagicDefOf.TM_SummonPylon, false),
            ("rwom.bard", d => d.MagicPowersB, 3, TorannMagicDefOf.TM_Lullaby_II, true),
            ("rwom.geomancer", d => d.MagicPowersG, 1, TorannMagicDefOf.TM_Encase_II, true),
            ("rwom.technomancer", d => d.MagicPowersT, 3, TorannMagicDefOf.TM_TechnoShield, false),
            ("rwom.enchanter", d => d.MagicPowersE, 4, TorannMagicDefOf.TM_Polymorph_II, true),
            ("rwom.chronomancer", d => d.MagicPowersC, 4, TorannMagicDefOf.TM_ChronostaticField_II, true)
        };
        foreach (var route in routes)
        {
            var target = Target(route.id); var caster = PawnWithComps(true, false);
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), route.id + " no learned eligible owner");
            var owner = route.list(target.magic.MagicData)[route.index]; owner.learned = true; owner.level = 2;
            // Contradictory old class metadata must not override the saved discipline.
            target.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Arcanist));
            target.magic.customClass = new TorannMagic.TMDefs.TM_CustomClass { isMage = true };
            Check.Equal(route.ability, Copy(target.pawn, caster.pawn), route.id + " native pool copied");
            if (route.ranked) Check.Equal(2, route.list(caster.magic.MagicData)[route.index].level, route.id + " rank transferred");
            if (route.id == "rwom.necromancer")
            {
                var path = target.progression.State.Find(route.id);
                Set(path, "stage", InitiationStage.Ascended); Set(path, "lichFormActive", true);
                Check.Equal(route.ability, Copy(target.pawn, caster.pawn), "saved Lich retains necromancer pool");
            }
        }
    }

    static void Brightmage(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var definitions = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(f.Repository,
            "v1.7/Defs/AbilityDefs/Abilities_TMagic_Brightmage.xml"));
        var target = Target("rwom.brightmage"); var caster = PawnWithComps(true, false);
        var saved = target.magic.MagicData.MagicPowersBrightmage.Select(p =>
            (def: (TMAbilityDef)p.TMabilityDefs[0], copy: ((TMAbilityDef)p.TMabilityDefs[0]).canCopy)).ToArray();
        try
        {
            foreach (var item in saved)
                item.def.canCopy = bool.Parse(definitions.Root!.Elements().Single(e =>
                    (string?)e.Element("defName") == item.def.defName).Element("canCopy")!.Value);
            target.magic.customClass = new TorannMagic.TMDefs.TM_CustomClass { isMage = true,
                classMageAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_SpiritOfLight } };
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "locked owners cannot leak through stale custom metadata");
            var spirit = target.magic.MagicData.MagicPowersBrightmage.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritOfLight));
            spirit.learned = true;
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "native canCopy=false excludes learned ultimate");
            var sun = target.magic.MagicData.MagicPowersBrightmage.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sunfire));
            sun.learned = true; sun.level = 2;
            Check.Equal(TorannMagicDefOf.TM_Sunfire_II, Copy(target.pawn, caster.pawn), "copies saved current native rank");
            var recipient = caster.magic.MagicData.MagicPowersBrightmage.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sunfire));
            Check.Equal(2, recipient.level, "copier skill owner receives rank");
            Check.Equal(false, recipient.learned, "copy does not permanently teach");
            recipient.learned = true;
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "already learned caster owner cannot be copied again");
        }
        finally { foreach (var item in saved) item.def.canCopy = item.copy; }
    }

    static void Empath(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        using var scope = EmpathProgressionTests.LoadDefinitions(f);
        var target = Target("rwom.empath"); var caster = PawnWithComps(true, false);
        var definitions = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(f.Repository,
            "v1.7/Defs/AbilityDefs/Abilities_TMagic_Empath.xml"));
        foreach (var owner in EmpathOwners.Resolve(target.magic.MagicData))
        {
            var node = definitions.Root!.Elements().Single(e => (string?)e.Element("defName") == owner.TMabilityDefs[0].defName);
            Check.Equal("false", node.Element("canCopy")!.Value, "native Empath family excludes copying");
            owner.learned = true;
        }
        target.magic.customClass = new TorannMagic.TMDefs.TM_CustomClass { isMage = true,
            classMageAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_Firebolt } };
        Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "Empath refusal survives contradictory addon metadata");
        Check.Equal(false, EmpathOwners.Resolve(caster.magic.MagicData).Any(p => p.learned), "no copied owners granted");
    }

    static void Golemancer(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var target = Target("rwom.golemancer"); var caster = PawnWithComps(true, false);
        var definitions = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(f.Repository,
            "v1.7/Defs/AbilityDefs/Abilities_TMagic_Golemancer.xml"));
        foreach (var owner in target.magic.MagicData.MagicPowersGolemancer)
        {
            var node = definitions.Root!.Elements().Single(e => (string?)e.Element("defName") == owner.TMabilityDefs[0].defName);
            Check.Equal("false", node.Element("canCopy")!.Value, "every native Golemancer family excludes copying");
            owner.learned = true;
        }
        target.magic.customClass = new TorannMagic.TMDefs.TM_CustomClass { isMage = true,
            classMageAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_Firebolt } };
        Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "native no-copy class cannot leak through contradictory metadata");
        Check.Equal(false, caster.magic.MagicData.MagicPowersGolemancer.Any(p => p.learned), "refusal grants no learned owner");
    }

    static void Shaman(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var definitions = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(f.Repository,
            "v1.7/Defs/AbilityDefs/Abilities_TMagic_Shaman.xml"));
        var target = Target("rwom.shaman"); var caster = PawnWithComps(true, false);
        var saved = target.magic.MagicData.MagicPowersShaman.Select(p =>
            (def: (TMAbilityDef)p.TMabilityDefs[0], copy: ((TMAbilityDef)p.TMabilityDefs[0]).canCopy)).ToArray();
        try
        {
            foreach (var item in saved)
                item.def.canCopy = bool.Parse(definitions.Root!.Elements().Single(e =>
                    (string?)e.Element("defName") == item.def.defName).Element("canCopy")?.Value ?? "false");
            target.magic.customClass = new TorannMagic.TMDefs.TM_CustomClass { isMage = true,
                classMageAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_GuardianSpirit } };
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "locked owners cannot leak through stale custom metadata");
            var spirit = target.magic.MagicData.MagicPowersShaman.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_GuardianSpirit));
            spirit.learned = true;
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "native canCopy=false excludes learned ultimate");
            var sun = target.magic.MagicData.MagicPowersShaman.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritWolves));
            sun.learned = true; sun.level = 2;
            Check.Equal(TorannMagicDefOf.TM_SpiritWolves_II, Copy(target.pawn, caster.pawn), "copies saved current native rank");
            var recipient = caster.magic.MagicData.MagicPowersShaman.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritWolves));
            Check.Equal(2, recipient.level, "copier skill owner receives rank");
            Check.Equal(false, recipient.learned, "copy does not permanently teach");
            recipient.learned = true;
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "already learned caster owner cannot be copied again");
        }
        finally { foreach (var item in saved) item.def.canCopy = item.copy; }
    }

    static void Demons(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        foreach (bool warlock in new[] { true, false })
        foreach (Gender gender in new[] { Gender.Male, Gender.Female })
        {
            var target = Target(warlock ? "rwom.warlock" : "rwom.succubus"); target.pawn.gender = gender;
            var caster = PawnWithComps(true, false);
            Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "unlearned demon powers stay unavailable");
            var selected = warlock ? target.magic.MagicData.MagicPowersWD : target.magic.MagicData.MagicPowersSD;
            var opposite = warlock ? target.magic.MagicData.MagicPowersSD : target.magic.MagicData.MagicPowersWD;
            opposite[1].learned = true; opposite[1].level = 2; selected[1].level = 1;
            Check.Equal(TorannMagicDefOf.TM_ShadowBolt_II, Copy(target.pawn, caster.pawn), "stale shared owner repaired before copy for " + gender);
            Check.Equal(true, selected[1].learned, "selected branch learns saved shared investment");
            Check.Equal(2, selected[1].level, "canonical rank uses maximum, not sum");
            Check.Equal(2, (warlock ? caster.magic.MagicData.MagicPowersWD : caster.magic.MagicData.MagicPowersSD)[1].level, "rank copied to selected caster list");
            selected[1].learned = false; opposite[1].learned = false;
            selected[3].learned = true; selected[3].level = 2;
            Check.Equal(warlock ? TorannMagicDefOf.TM_Repulsion_II : TorannMagicDefOf.TM_Attraction_II, Copy(target.pawn, caster.pawn), "saved branch beats gender");
            Check.Equal(false, selected[2].learned, "locked Dominate not granted by repair");
        }
    }

    static void Legacy(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var target = PawnWithComps(true, false); var caster = PawnWithComps(true, false);
        target.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Bard));
        target.magic.MagicData.MagicPowersB[3].learned = false; target.magic.MagicData.MagicPowersB[3].level = 2;
        Check.Equal(TorannMagicDefOf.TM_Lullaby_II, Copy(target.pawn, caster.pawn), "legacy unconditional Bard behavior retained");
        target.pawn.story.traits.allTraits.Clear(); target.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Lich));
        target.magic.MagicData.MagicPowersN[1].learned = true;
        Check.Equal<TMAbilityDef?>(null, Copy(target.pawn, caster.pawn), "legacy raw Lich remains excluded");
        target.pawn.story.traits.allTraits.Clear(); target.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Necromancer));
        target.magic.MagicData.MagicPowersN[1].level = 2;
        Check.Equal(TorannMagicDefOf.TM_DeathMark_II, Copy(target.pawn, caster.pawn), "legacy base necromancer still copies");
    }
}
