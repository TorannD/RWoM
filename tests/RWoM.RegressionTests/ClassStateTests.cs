using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
using AbilityDef = AbilityUser.AbilityDef;

namespace RWoM.RegressionTests;

internal static class ClassStateTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (bool addonClass in new[] { false, true })
            yield return new("ClassState.ScrollCantripWithSkillsAfterSave." + (addonClass ? "Addon" : "Builtin"), f => ScrollCantripAfterSave(f, addonClass));
        yield return new("ClassState.StockStormBornToHeartOfFrost", StockTransition);
        yield return new("ClassState.CustomToStockClearsStaleClass", CustomToStock);
        yield return new("ClassState.CustomToCustomPreservesOverlapAndStandalone", CustomToCustom);
        yield return new("ClassState.UnrelatedTraitsPreserveSavedInvestments", UnrelatedTraits);
        yield return new("ClassState.GenerationDoesNotGrantOrReroll", Generation);
        yield return new("ClassState.MagicResetRemovesDuplicatesChildrenAndUnlocks", MagicReset);
        yield return new("ClassState.MightResetPreservesHybridMagic", MightReset);
        yield return new("ClassState.SuperSoldierResetKeepsSpecializationsLocked", SuperSoldierReset);
        yield return new("ClassState.CustomLoadPreservesOwnersAndInvestments", CustomLoad);
        yield return new("ClassState.AdvancedPossessedLoadPreservesWarlock", PossessedLoad);
        yield return new("ClassState.SharedOwnerRecoveryIsBoundedAndIdempotent", SharedRecovery);
        yield return new("ClassState.ScribeLoadedSharedOwnerRecoveryPreservesSavedReferences", SavedRecovery);
        yield return new("ClassState.CustomSharedOwnerRecoveryRetainsCanonicalReference", CustomSharedRecovery);
        yield return new("ClassState.NoUnrelatedSharedOwnerRecovery", UnrelatedSharedRecovery);
        yield return new("ClassState.ReorderedDefinitionsRefreshCachedClass", RegistryRefresh);
        yield return new("ClassState.AdvancedTraitGrantDoesNotLearnBeforeCopy", AdvancedGrant);
        yield return new("ClassState.NestedTraitMutationReconcilesOnce", NestedMutation);
        yield return new("ClassState.AdvancedRemovalBeforeMagicDataExists", f =>
        {
            Prepare(f); var advanced = Custom("ClassStateRejectedPossession", abilities: [TorannMagicDefOf.TM_SpiritStorm]);
            advanced.isAdvancedClass = true; Registry(advanced);
            var p = PawnWithComps(false, false); Set(p.magic, "magicData", null!);
            p.magic.AdvancedClasses.Add(advanced);
            Add(p.magic, TorannMagicDefOf.TM_SpiritStorm);
            p.magic.RemoveAdvancedClass(advanced);
            Check.Same(null!, p.magic.MagicData, "removing rejected class does not create progression");
            Check.Equal(false, Has(p.magic, TorannMagicDefOf.TM_SpiritStorm), "runtime grant removed even without progression");
            Check.Equal(false, p.magic.AdvancedClasses.Contains(advanced), "advanced membership cleared");
            p.magic.RemoveAdvancedClass(advanced);
            p.magic.RemoveAdvancedClass(null!);
        });
    }

    static void Prepare(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        foreach (var ability in DefDatabase<TMAbilityDef>.AllDefsListForReading)
        {
            ability.abilityClass = typeof(PawnAbility);
            ability.shouldInitialize = true;
        }
    }

    static void ScrollCantripAfterSave(RuntimeFixture f, bool addonClass)
    {
        Prepare(f);
        var ability = f.AddAbility("TM_AuditMagicTraining");
        ability.learnItem = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        ability.learnItem.defName = "SpellOf_AuditMagicTraining";
        ability.shouldInitialize = true; ability.abilityClass = typeof(PawnAbility);
        DefDatabase<TM_CustomPowerDef>.Add(new TM_CustomPowerDef { defName = ability.defName,
            customPower = new TM_CustomPower { forMage = true, abilityDefs = [ability],
                skills = [new TM_CustomSkill { label = ability.defName + "_pwr", levelMax = 3, costToLevel = 1 }] } });
        var foreign = f.AddAbility("TM_AuditForeignClassSpell");
        foreign.learnItem = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        foreign.learnItem.defName = "SpellOf_AuditForeignClassSpell";
        foreign.shouldInitialize = true; foreign.abilityClass = typeof(PawnAbility);
        DefDatabase<TM_CustomPowerDef>.Add(new TM_CustomPowerDef { defName = foreign.defName,
            customPower = new TM_CustomPower { forMage = true, abilityDefs = [foreign],
                skills = [new TM_CustomSkill { label = foreign.defName + "_pwr", levelMax = 3, costToLevel = 1 }] } });
        var wind = Custom("TM_AuditWindMage", abilities: [TorannMagicDefOf.TM_ShadowBolt]);
        Registry(wind, Custom("TM_AuditForeignMage", abilities: [foreign]));
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(addonClass ? wind.classTrait : TorannMagicDefOf.TM_Golemancer));
        var data = p.magic.MagicData;
        data.MagicPowersCustom.Single(x => x.TMabilityDefs.Contains(ability)).learned = true;
        data.MagicPowersCustom.Single(x => x.TMabilityDefs.Contains(foreign)).learned = true;
        data.GetSkill_Power(ability).level = 2;
        data.MagicPowersStandalone.Single(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_Cooler)).learned = true;
        data.MagicAbilityPoints = 7; data.MagicUserLevel = 14;
        // This fixture saves the data container, not the enclosing world/pawn graph.
        Set(data, "magicPawn", null!);
        var loaded = f.Load(f.Save(data)); Set(p.magic, "magicData", loaded);
        Set(loaded, "magicPawn", p.pawn);
        TM_ClassState.RestoreCurrentMagicRuntime(p.magic);
        Check.Equal(true, Has(p.magic, ability), "learned shared addon spell with skill records restored");
        Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_Cooler), "builtin shared spell retained");
        Check.Equal(false, Has(p.magic, foreign), "foreign class spell is not recovered as a cantrip");
        var runtime = p.magic.AbilityData.Powers.Single(a => a.Def == ability); runtime.CooldownTicksLeft = 123;
        TM_ClassState.RestoreCurrentMagicRuntime(p.magic);
        Check.Same(runtime, p.magic.AbilityData.Powers.Single(a => a.Def == ability), "repeated restoration retains runtime");
        Check.Equal(123, runtime.CooldownTicksLeft, "repeated restoration preserves cooldown");
        Check.Equal(2, loaded.GetSkill_Power(ability).level, "saved upgrade retained");
        Check.Equal(7, loaded.MagicAbilityPoints, "no refund or extra payment");
        Check.Equal(14, loaded.MagicUserLevel, "level retained");
    }

    static TM_CustomClass Custom(string name, bool mage = true, bool fighter = false, params TMAbilityDef[] abilities)
    {
        var trait = DefDatabase<TraitDef>.GetNamedSilentFail(name);
        if (trait == null) { trait = new TraitDef { defName = name }; DefDatabase<TraitDef>.Add(trait); }
        return new TM_CustomClass { classTrait = trait, isMage = mage, isFighter = fighter,
            classMageAbilities = mage ? abilities.ToList() : [], classFighterAbilities = fighter ? abilities.ToList() : [] };
    }

    static void Registry(params TM_CustomClass[] classes)
    {
        TM_ClassUtility.CustomClasses = classes.ToList();
        TM_ClassUtility.CustomAdvancedClasses.Clear();
        TM_ClassUtility.CustomAdvancedClasses.AddRange(classes.Where(c => c.isAdvancedClass));
    }

    static void Ready(CompAbilityUserMagic magic, CompAbilityUserMight might, bool both = false)
    {
        magic.magicPowersInitialized = true;
        if (both) might.mightPowersInitialized = true;
    }

    static bool Has(CompAbilityUser comp, AbilityDef ability) => comp.AbilityData.Powers.Any(a => a.Def == ability);
    static void Add(CompAbilityUser comp, AbilityDef ability, int count = 1)
    {
        for (int i = 0; i < count; i++) comp.AddPawnAbility(ability);
    }
    static void Change(Pawn pawn, TraitDef oldClass, TraitDef newClass)
    {
        TM_ClassState.CaptureBeforeTraitsChanged(pawn);
        pawn.story.traits.allTraits.RemoveAll(t => t.def == oldClass);
        if (newClass != null) pawn.story.traits.allTraits.Add(new Trait(newClass));
        TM_ClassState.OnTraitsChanged(pawn);
    }

    static void StockTransition(RuntimeFixture f)
    {
        Prepare(f); Registry();
        var p = PawnWithComps(true, false); Ready(p.magic, p.might);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.StormBorn));
        var old = p.magic.MagicData.MagicPowersSB.Single(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningBolt));
        old.level = 2; old.learned = true;
        Add(p.magic, old.abilityDef, 2);
        p.magic.MagicData.MagicUserLevel = 17; p.magic.MagicData.MagicAbilityPoints = 9;
        Change(p.pawn, TorannMagicDefOf.StormBorn, TorannMagicDefOf.HeartOfFrost);
        Check.Equal(false, Has(p.magic, old.abilityDef), "all old lightning duplicates revoked");
        Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_Icebolt), "new frost starts granted");
        Check.Equal(2, old.level, "old saved investment retained");
        Check.Equal(true, old.learned, "old learned owner not erased");
        Check.Equal(17, p.magic.MagicData.MagicUserLevel, "live edit preserves level");
        Check.Equal(9, p.magic.MagicData.MagicAbilityPoints, "live edit preserves unspent points");
    }

    static void CustomToStock(RuntimeFixture f)
    {
        Prepare(f); var cls = Custom("ClassStateOldCustom", abilities: [TorannMagicDefOf.TM_ShadowBolt]); Registry(cls);
        var p = PawnWithComps(true, false); Ready(p.magic, p.might);
        p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait)); p.magic.SetIsMagicUser();
        TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_ShadowBolt).learned = true;
        Add(p.magic, TorannMagicDefOf.TM_ShadowBolt);
        Change(p.pawn, cls.classTrait, TorannMagicDefOf.HeartOfFrost);
        Check.Same(null!, p.magic.customClass, "removed custom cache does not mask stock class");
        Check.Equal(false, Has(p.magic, TorannMagicDefOf.TM_ShadowBolt), "custom-only runtime grant removed");
        Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_Icebolt), "stock starts granted");
    }

    static void CustomToCustom(RuntimeFixture f)
    {
        Prepare(f);
        var old = Custom("ClassStateCustomA", abilities: [TorannMagicDefOf.TM_ShadowBolt, TorannMagicDefOf.TM_Blink]);
        var next = Custom("ClassStateCustomB", abilities: [TorannMagicDefOf.TM_Icebolt, TorannMagicDefOf.TM_Blink]); Registry(old, next);
        var p = PawnWithComps(true, false); Ready(p.magic, p.might);
        p.pawn.story.traits.allTraits.Add(new Trait(old.classTrait)); p.magic.SetIsMagicUser();
        var blink = TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_Blink); blink.learned = true; blink.level = 2;
        var standalone = p.magic.MagicData.MagicPowersStandalone.First(); standalone.learned = true;
        Add(p.magic, TorannMagicDefOf.TM_ShadowBolt); Add(p.magic, blink.abilityDef); Add(p.magic, standalone.abilityDef);
        Change(p.pawn, old.classTrait, next.classTrait);
        Check.Same(next, p.magic.customClass, "new current registry custom class used");
        Check.Equal(false, Has(p.magic, TorannMagicDefOf.TM_ShadowBolt), "exclusive old custom ability removed");
        Check.Equal(true, Has(p.magic, blink.abilityDef), "overlapping learned rank retained");
        Check.Equal(true, Has(p.magic, standalone.abilityDef), "independently learned standalone remains");
        Check.Equal(2, blink.level, "overlap investment not reset");
        Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_Icebolt), "new custom starts added");
    }

    static void UnrelatedTraits(RuntimeFixture f)
    {
        Prepare(f); Registry(); var p = PawnWithComps(true, false); Ready(p.magic, p.might);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.StormBorn));
        var owner = p.magic.MagicData.MagicPowersSB.First(); owner.learned = true; owner.level = 2;
        var skill = p.magic.MagicData.AllMagicPowerSkills.First(); skill.level = 3;
        p.magic.MagicData.MagicAbilityPoints = 8;
        Add(p.magic, owner.abilityDef); var runtime = p.magic.AbilityData.Powers.Single(); runtime.CooldownTicksLeft = 123;
        var unrelated = new TraitDef { defName = "ClassStateUnrelated" };
        Change(p.pawn, null!, unrelated);
        Check.Same(runtime, p.magic.AbilityData.Powers.Single(), "unrelated edit does not recreate runtime ability");
        Check.Equal(123, runtime.CooldownTicksLeft, "cooldown preserved");
        Check.Equal(2, owner.level, "rank preserved"); Check.Equal(3, skill.level, "skill investment preserved");
        Check.Equal(8, p.magic.MagicData.MagicAbilityPoints, "points preserved");
    }

    static void Generation(RuntimeFixture f)
    {
        Prepare(f); Registry(); var p = PawnWithComps(true, false);
        Change(p.pawn, null!, TorannMagicDefOf.StormBorn);
        Check.Equal(0, p.magic.AbilityData.Powers.Count, "trait hook leaves first generation to normal initialization");
        Check.Equal(false, p.magic.MagicData.AllMagicPowers.Any(x => x.learned), "generation hook does not prelearn starts");
    }

    static void MagicReset(RuntimeFixture f)
    {
        Prepare(f);
        var cls = Custom("ClassStateResetCustom", abilities: [TorannMagicDefOf.TM_Icebolt]); Registry(cls);
        var p = PawnWithComps(true, true); Ready(p.magic, p.might, true);
        p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait));
        var child = TorannMagicDefOf.TM_ShadowCall;
        TorannMagicDefOf.TM_ShadowBolt.childAbilities.Add(child);
        var old = p.magic.MagicData.MagicPowersWD.Single(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt));
        old.level = 2; old.learned = true;
        Add(p.magic, old.abilityDef, 3); Add(p.magic, child, 2);
        p.magic.spell_Blizzard = true; p.magic.spell_Firestorm = true; p.magic.recallSpell = true;
        var mightData = p.might.MightData; mightData.MightAbilityPoints = 7;
        Add(p.might, TorannMagicDefOf.TM_Sprint); var otherRuntime = p.might.AbilityData.Powers.Single();
        p.magic.ResetSkills();
        Check.Equal(false, Has(p.magic, old.TMabilityDefs[2]), "all old duplicate variants removed");
        Check.Equal(false, Has(p.magic, child), "all child duplicates removed");
        Check.Equal(false, p.magic.spell_Blizzard || p.magic.spell_Firestorm || p.magic.recallSpell, "master and child unlock flags cleared");
        Check.Equal(0, old.level, "old owner rank reset"); Check.Equal(false, old.learned, "old owner learned reset");
        Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_Icebolt), "current class starts restored");
        Check.Equal(true, p.pawn.story.traits.HasTrait(cls.classTrait), "class trait retained");
        Check.Same(mightData, p.might.MightData, "other side data identity retained");
        Check.Equal(7, mightData.MightAbilityPoints, "other side points retained");
        Check.Same(otherRuntime, p.might.AbilityData.Powers.Single(), "other side runtime retained");
    }

    static void MightReset(RuntimeFixture f)
    {
        Prepare(f); var cls = Custom("ClassStateMightReset", false, true, TorannMagicDefOf.TM_Sprint); Registry(cls);
        var p = PawnWithComps(true, true); Ready(p.magic, p.might, true);
        p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait));
        Add(p.might, TorannMagicDefOf.TM_ThrowingKnife, 3); Add(p.might, TorannMagicDefOf.TM_Sprint);
        p.might.skill_ThrowingKnife = true; p.might.skill_Teach = true;
        p.magic.MagicData.MagicAbilityPoints = 11; Add(p.magic, TorannMagicDefOf.TM_Blink);
        p.might.ResetSkills();
        Check.Equal(false, Has(p.might, TorannMagicDefOf.TM_ThrowingKnife), "old might duplicate grants removed");
        Check.Equal(false, p.might.skill_ThrowingKnife || p.might.skill_Teach, "old might unlock flags cleared");
        Check.Equal(true, Has(p.might, TorannMagicDefOf.TM_Sprint), "current might start restored");
        Check.Equal(11, p.magic.MagicData.MagicAbilityPoints, "hybrid magic points preserved");
        Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_Blink), "hybrid magic runtime retained");
    }

    static void SuperSoldierReset(RuntimeFixture f)
    {
        Prepare(f); Registry();
        var p = PawnWithComps(false, true); Ready(p.magic, p.might, true);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_SuperSoldier));
        foreach (var owner in p.might.MightData.MightPowersSS)
        {
            owner.learned = true;
            Add(p.might, owner.abilityDef, 2);
        }
        p.might.ResetSkills();
        foreach (var owner in p.might.MightData.MightPowersSS)
        {
            bool starts = owner.TMabilityDefs.Any(a => a == TorannMagicDefOf.TM_CQC
                || a == TorannMagicDefOf.TM_FirstAid || a == TorannMagicDefOf.TM_60mmMortar);
            Check.Equal(starts, owner.learned, "reset only learns native starting abilities: " + owner.abilityDef.defName);
            Check.Equal(starts ? 1 : 0, p.might.AbilityData.Powers.Count(a => owner.TMabilityDefs.Contains(a.Def)),
                "reset removes specialization grants and duplicates: " + owner.abilityDef.defName);
        }
    }

    static void CustomLoad(RuntimeFixture f)
    {
        Prepare(f); var cls = Custom("ClassStateLoad", abilities: [TorannMagicDefOf.TM_ShadowBolt]); Registry(cls);
        var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait));
        var data = p.magic.MagicData; var owner = TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_ShadowBolt);
        owner.level = 2; owner.learned = true; data.MagicAbilityPoints = 6;
        var skill = data.AllMagicPowerSkills.First(); skill.level = 3;
        p.magic.LoadCustomClassAbilities(cls); p.magic.LoadCustomClassAbilities(cls);
        Check.Same(data, p.magic.MagicData, "ordinary class load retains data object");
        Check.Equal(2, owner.level, "ordinary class load preserves saved rank");
        Check.Equal(true, owner.learned, "ordinary class load preserves learned");
        Check.Equal(3, skill.level, "ordinary class load preserves exact skill object");
        Check.Equal(6, data.MagicAbilityPoints, "ordinary load never resets unspent points");
        Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == owner.abilityDef), "runtime grants deduplicated without clearing saved owners");
    }

    static void PossessedLoad(RuntimeFixture f)
    {
        Prepare(f); var possessed = Custom("TM_Possessed", abilities: [TorannMagicDefOf.TM_SpiritStorm]); possessed.isAdvancedClass = true; Registry(possessed);
        var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        p.pawn.story.traits.allTraits.Add(new Trait(possessed.classTrait));
        var saved = p.magic.MagicData.MagicPowersWD.Where(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_SoulBond || x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt || x.TMabilityDefs[0] == TorannMagicDefOf.TM_Dominate).ToList();
        foreach (var power in saved) { power.learned = true; power.level = Math.Min(2, power.TMabilityDefs.Count - 1); }
        p.magic.LoadCustomClassAbilities(possessed);
        Check.Equal(true, saved.All(x => x.learned), "loading advanced Possessed does not clear builtin Warlock trio");
        Check.Equal(2, saved.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt).level, "later-rank ShadowBolt owner retained");
    }

    static void SharedRecovery(RuntimeFixture f)
    {
        Prepare(f); Registry(); var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        var data = p.magic.MagicData;
        var source = data.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        var canonical = data.MagicPowersWD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        source.level = 2; source.learned = true; data.MagicAbilityPoints = 13; data.MagicUserLevel = 21;
        var skill = data.AllMagicPowerSkills.First(); skill.level = 3;
        TM_ClassState.RestoreSharedMagicProgression(p.magic); TM_ClassState.RestoreSharedMagicProgression(p.magic);
        Check.Same(canonical, TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_ShadowBolt), "builtin WD canonical owner used by runtime and UI");
        Check.Equal(2, canonical.level, "maximum saved rank recovered, not summed"); Check.Equal(true, canonical.learned, "learned union recovered");
        Check.Equal(2, source.level, "source saved rank retained"); Check.Equal(true, source.learned, "source learned retained");
        Check.Equal(13, data.MagicAbilityPoints, "recovery does not refund or spend points"); Check.Equal(21, data.MagicUserLevel, "level unchanged");
        Check.Equal(3, skill.level, "skill investment unchanged");
    }

    static void SavedRecovery(RuntimeFixture f)
    {
        Prepare(f); Registry(); var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        var input = new MagicData();
        var invested = input.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        invested.level = 2; invested.learned = true; input.MagicAbilityPoints = 17; input.MagicUserLevel = 22;
        input.MagicPowerSkill_ShadowBolt.First().level = 3;
        var loaded = f.Load(f.Save(input)); Set(p.magic, "magicData", loaded);
        var source = loaded.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        var canonical = loaded.MagicPowersWD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        var skill = loaded.MagicPowerSkill_ShadowBolt.First();
        TM_ClassState.RestoreSharedMagicProgression(p.magic);
        Check.Same(loaded, p.magic.MagicData, "Scribe-loaded data instance retained");
        Check.Same(source, loaded.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt), "Scribe-loaded source owner reference retained");
        Check.Same(canonical, TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_ShadowBolt), "Scribe-loaded canonical owner reference retained");
        Check.Equal(2, canonical.level, "asymmetric saved rank recovered"); Check.Equal(true, canonical.learned, "saved learned state recovered");
        Check.Same(skill, loaded.MagicPowerSkill_ShadowBolt.First(), "saved skill reference retained");
        Check.Equal(3, skill.level, "saved skill rank retained"); Check.Equal(17, loaded.MagicAbilityPoints, "saved points retained");
        Check.Equal(22, loaded.MagicUserLevel, "saved level retained");
    }

    static void CustomSharedRecovery(RuntimeFixture f)
    {
        Prepare(f); var cls = Custom("ClassStateSharedOverride", abilities: [TorannMagicDefOf.TM_ShadowBolt]); Registry(cls);
        var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait));
        var data = p.magic.MagicData;
        var canonical = data.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        var source = data.MagicPowersWD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        source.level = 3; source.learned = true;
        TM_ClassState.RestoreSharedMagicProgression(p.magic);
        Check.Same(canonical, TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_ShadowBolt), "custom fallback retains exact first aggregate SD owner");
        Check.Equal(3, canonical.level, "custom saved rank recovered without deleting WD");
        Check.Equal(true, canonical.learned, "custom learned state recovered");
        Check.Equal(3, source.level, "noncanonical source retained");
    }

    static void UnrelatedSharedRecovery(RuntimeFixture f)
    {
        Prepare(f); Registry(); var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.StormBorn));
        var source = p.magic.MagicData.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt); source.level = 3; source.learned = true;
        var wd = p.magic.MagicData.MagicPowersWD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        TM_ClassState.RestoreSharedMagicProgression(p.magic);
        Check.Equal(0, wd.level, "unauthorized family not migrated"); Check.Equal(false, wd.learned, "removed class is not revived");
    }

    static void RegistryRefresh(RuntimeFixture f)
    {
        Prepare(f); var stale = Custom("ClassStateRegistry", abilities: [TorannMagicDefOf.TM_ShadowBolt]); Registry(stale);
        var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(stale.classTrait)); p.magic.SetIsMagicUser();
        var replacement = Custom("ClassStateRegistry", abilities: [TorannMagicDefOf.TM_Icebolt]);
        Registry(Custom("ClassStateOther"), replacement); p.magic.SetIsMagicUser();
        Check.Same(replacement, p.magic.customClass, "registry rebuild replaces stale object using stable trait identity");
        Check.Equal(1, p.magic.customIndex, "cached index recalculated after reorder");
        p.pawn.story.traits.allTraits.Clear(); Check.Equal(false, p.magic.SetIsMagicUser(), "removing cached custom trait removes membership");
        Check.Same(null!, p.magic.customClass, "stale custom cache cleared");
    }

    static void AdvancedGrant(RuntimeFixture f)
    {
        Prepare(f); var advanced = Custom("ClassStateAdvanced", abilities: [TorannMagicDefOf.TM_ShadowBolt]); advanced.isAdvancedClass = true; Registry(advanced);
        var p = PawnWithComps(true, false); Ready(p.magic, p.might); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.StormBorn));
        Change(p.pawn, null!, advanced.classTrait);
        var owner = TM_ClassState.PreferredMagicOwner(p.magic, TorannMagicDefOf.TM_ShadowBolt);
        Check.Equal(false, owner.learned, "advanced fromPawn copy retains ownership of initial learning");
        Check.Equal(false, Has(p.magic, TorannMagicDefOf.TM_ShadowBolt), "trait hook does not preempt advanced transfer");
    }

    static void NestedMutation(RuntimeFixture f)
    {
        Prepare(f); Registry(); var p = PawnWithComps(true, false); Ready(p.magic, p.might); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.StormBorn));
        Add(p.magic, TorannMagicDefOf.TM_LightningBolt);
        TM_ClassState.CaptureBeforeTraitsChanged(p.pawn); TM_ClassState.CaptureBeforeTraitsChanged(p.pawn);
        p.pawn.story.traits.allTraits.Clear(); p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.HeartOfFrost));
        TM_ClassState.OnTraitsChanged(p.pawn); Check.Equal(true, Has(p.magic, TorannMagicDefOf.TM_LightningBolt), "inner hook waits for outer transition");
        TM_ClassState.OnTraitsChanged(p.pawn); Check.Equal(false, Has(p.magic, TorannMagicDefOf.TM_LightningBolt), "outer hook revokes old grant");
        Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Icebolt), "nested transition grants once");
    }
}
