using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class DemonOwnershipTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("DemonOwnership.DominateEffectRanksAndRefresh", DominateEffects);
        yield return new("DemonOwnership.NativeSoulBondSelectedFamilyAndToggle", NativeBond);
        yield return new("DemonOwnership.RestoreCanonicalRuntimeAndExactUltimateFlags", Restore);
        yield return new("DemonOwnership.PreparedSnapshotIsAtomicAndBounded", PreparedSnapshot);
        yield return new("DemonOwnership.SelectedOwnerAndOppositeLearningGate", SelectedOwners);
        yield return new("DemonOwnership.ExplicitIdentityOverridesGenderOnlyForNewProfiles", Identity);
        yield return new("DemonOwnership.LegacyOwnerPredicatesRemainNative", LegacyOwners);
    }

    static void DominateEffects(RuntimeFixture f)
    {
        var assembly = typeof(MagicAbility).Assembly;
        var select = assembly.GetType("TorannMagic.Verb_Dominate")!.GetMethod("Effect", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (bool mental in new[] { false, true })
        foreach (int rank in Enumerable.Range(0,4))
        {
            var name = (mental ? "TM_WDDominateHD" : "TM_SDDominateHD") + new[] { "", "_I", "_II", "_III" }[rank];
            var field = typeof(TorannMagicDefOf).GetField(name)!;
            var original = field.GetValue(null);
            var sentinel = new HediffDef { defName = name };
            try
            {
                field.SetValue(null,sentinel);
                Check.Same(sentinel,select.Invoke(null,[mental,rank])!, "selected branch and rank resolve to the exact native effect");
            }
            finally { field.SetValue(null,original); }
        }
        var type = assembly.GetType("TorannMagic.HediffComp_Dominate")!;
        var effect = Activator.CreateInstance(type)!;
        type.GetProperty("EffVal")!.SetValue(effect,3);
        type.GetProperty("VerVal")!.SetValue(effect,2);
        Check.Equal(true, Math.Abs((float)type.GetProperty("MinimumSeverity",BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(effect)! - .21f) < .001f,"saved efficiency determines spread cost");
        Check.Equal(5,(int)type.GetField("infectionRadius",BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(effect)!,"recast updates spread radius on the existing effect");
    }

    static ClassDisciplineDef Profile(bool warlock) => new() { defName = warlock ? "DemonOwnerWD" : "DemonOwnerSD",
        profileId = warlock ? "rwom.warlock" : "rwom.succubus", domain = DisciplineDomain.Magic,
        magicPowerList = warlock ? "magicPowerWD" : "magicPowerSD",
        legacyTrait = warlock ? TorannMagicDefOf.Warlock : TorannMagicDefOf.Succubus };

    static CompClassProgression Enroll(Pawn pawn, bool warlock)
    {
        var progression = new CompClassProgression { parent = pawn }; pawn.AllComps.Add(progression);
        progression.BeginNewProgression(Vocation.Mage);
        progression.State.TryAddFoundation(warlock ? "rwom.warlock" : "rwom.succubus", DisciplineDomain.Magic);
        return progression;
    }

    static MagicPower Owner(IEnumerable<MagicPower> powers, string name)
        => powers.Single(p => p.TMabilityDefs.Any(a => a.defName == name));

    static void PreparedSnapshot(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        foreach (bool warlock in new[] { true, false })
        {
            var p = PawnWithComps(true, false); var data = p.magic.MagicData;
            data.MagicAbilityPoints = 7; data.MagicUserXP = 123;
            var sourceSkills = data.MagicPowerSkill_SoulBond;
            var wd = data.MagicPowersWD.ToArray(); var sd = data.MagicPowersSD.ToArray();
            foreach (string name in new[] { "TM_SoulBond", "TM_ShadowBolt", "TM_Dominate" })
            { Owner(wd, name).level = 100; Owner(wd, name).learned = true; Owner(sd, name).level = 0; Owner(sd, name).learned = false; }
            var prepared = TM_ClassState.PrepareDemonOwners(p.magic, Profile(warlock));
            Check.Equal(true, prepared != null, "all three canonical families validated");
            Check.Equal(false, Owner(sd, "TM_SoulBond").learned, "prepare performs no storage mutation");
            prepared!.Apply();
            foreach (string name in new[] { "TM_SoulBond", "TM_ShadowBolt", "TM_Dominate" })
            {
                var owner = Owner(warlock ? wd : sd, name);
                Check.Equal(true, owner.learned, "learned union copied to selected owner");
                Check.Equal(owner.TMabilityDefs.Count - 1, owner.level, "rank is bounded maximum");
                Check.Same(owner, Owner(warlock ? data.MagicPowersWD : data.MagicPowersSD, name), "existing native owner retained");
            }
            Check.Same(sourceSkills, data.MagicPowerSkill_SoulBond, "shared skill storage retained");
            Check.Equal(7, data.MagicAbilityPoints, "points unchanged"); Check.Equal(123, data.MagicUserXP, "XP unchanged");
            // A broken final family must fail preparation before either earlier
            // family's destination changes; no partial reconciliation is allowed.
            var destination = warlock ? data.MagicPowersWD : data.MagicPowersSD;
            var missing = Owner(destination, "TM_Dominate"); destination.Remove(missing);
            var soul = Owner(destination, "TM_SoulBond"); soul.learned = false; soul.level = 0;
            Check.Same(null!, TM_ClassState.PrepareDemonOwners(p.magic, Profile(warlock))!, "missing final family rejects snapshot");
            Check.Equal(false, soul.learned, "invalid snapshot cannot mutate earlier family");
            Check.Equal(0, soul.level, "invalid snapshot cannot mutate earlier rank");
            destination.Add(missing);
            var invalid = Profile(warlock); invalid.magicPowerList = "magicPowerA";
            Check.Same(null!, TM_ClassState.PrepareDemonOwners(p.magic, invalid)!, "wrong profile owner fails before commit");
        }
    }

    static void SelectedOwners(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var old = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (bool warlock in new[] { true, false }) DefDatabase<ClassDisciplineDef>.Add(Profile(warlock));
            foreach (bool warlock in new[] { true, false })
            {
                var p = PawnWithComps(true, false); Enroll(p.pawn, warlock);
                foreach (string name in new[] { "TM_SoulBond", "TM_ShadowBolt", "TM_Dominate" })
                {
                    var selected = Owner(warlock ? p.magic.MagicData.MagicPowersWD : p.magic.MagicData.MagicPowersSD, name);
                    var other = Owner(warlock ? p.magic.MagicData.MagicPowersSD : p.magic.MagicData.MagicPowersWD, name);
                    other.learned = true;
                    Check.Same(selected, TM_ClassState.PreferredMagicOwner(p.magic, selected.TMabilityDefs[0]), "explicit native owner");
                    Check.Equal(false, ClassSpellAccess.CanLearn(p.pawn, other), "shared ability metadata cannot authorize opposite owner");
                    Check.Equal(true, selected.learned, "classification first restores learned union");
                }
            }
        }
        finally { DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in old) DefDatabase<ClassDisciplineDef>.Add(profile); }
    }

    static void Restore(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        var abilities = DefDatabase<TMAbilityDef>.AllDefsListForReading.Select(a => (a, a.abilityClass, a.shouldInitialize)).ToArray();
        try
        {
            foreach (var item in abilities) { item.a.abilityClass = typeof(AbilityUser.PawnAbility); item.a.shouldInitialize = true; }
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (bool branch in new[] { true, false }) DefDatabase<ClassDisciplineDef>.Add(Profile(branch));
            foreach (bool warlock in new[] { true, false })
            {
                var p = PawnWithComps(true, false); Enroll(p.pawn, warlock);
                var selected = Owner(warlock ? p.magic.MagicData.MagicPowersWD : p.magic.MagicData.MagicPowersSD, "TM_ShadowBolt");
                var other = Owner(warlock ? p.magic.MagicData.MagicPowersSD : p.magic.MagicData.MagicPowersWD, "TM_ShadowBolt");
                selected.level = 0; selected.learned = false; selected.autocast = true;
                other.level = 2; other.learned = true;
                var stale = new AbilityUser.PawnAbility(p.magic); Set(stale, "powerDef", selected.abilityDef); stale.CooldownTicksLeft = 91; p.magic.AbilityData.Powers.Add(stale);
                var preserved = new AbilityUser.PawnAbility(p.magic); Set(preserved, "powerDef", other.abilityDef); preserved.CooldownTicksLeft = 17; p.magic.AbilityData.Powers.Add(preserved);
                p.magic.spell_PsychicShock = true;
                Check.Equal(true, ClassSpellAccess.RestoreLearned(p.magic), "new-model restore handles selected class");
                Check.Equal(2, selected.level, "union restored before runtime classification");
                Check.Equal(true, selected.autocast, "selected autocast preserved");
                Check.Equal(91, preserved.CooldownTicksLeft, "maximum family cooldown retained across removed stale rank");
                Check.Equal(true, other.learned, "source learned record remains intact");
                var family = p.magic.AbilityData.Powers.Where(a => selected.TMabilityDefs.Contains(a.Def)).ToList();
                Check.Equal(1, family.Count, "one canonical runtime rank"); Check.Same(preserved, family.Single(), "canonical runtime instance and cooldown storage retained");
                Check.Equal(true, Owner(p.magic.MagicData.MagicPowersWD, "TM_PsychicShock").learned, "explicit flag repairs exact WD ultimate");
                Check.Equal(false, Owner(p.magic.MagicData.MagicPowersSD, "TM_Scorn").learned, "opposite ultimate never inferred");
                // Missing profile metadata must retain genuine shared ownership
                // while refusing new branch powers and opposite ultimates.
                DefDatabase<ClassDisciplineDef>.Clear();
                selected.learned = false; selected.level = 0; other.learned = true; other.level = 2;
                ClassSpellAccess.RestoreLearned(p.magic);
                Check.Equal(true, selected.learned, "missing profile recovers genuine saved learned union");
                Check.Equal(2, selected.level, "missing profile preserves genuine rank");
                Check.Equal(91, preserved.CooldownTicksLeft, "missing profile keeps family cooldown");
                var adept = Owner(warlock ? p.magic.MagicData.MagicPowersWD : p.magic.MagicData.MagicPowersSD, warlock ? "TM_Repulsion" : "TM_Attraction");
                Check.Equal(false, ClassSpellAccess.CanLearn(p.pawn, adept), "missing profile denies unlearned branch power");
                foreach (bool branch in new[] { true, false }) DefDatabase<ClassDisciplineDef>.Add(Profile(branch));
                ClassSpellAccess.RestoreLearned(p.magic);
                Check.Equal(1, p.magic.AbilityData.Powers.Count(a => selected.TMabilityDefs.Contains(a.Def)), "repeated restore idempotent");
            }
        }
        finally
        {
            foreach (var item in abilities) { item.a.abilityClass = item.abilityClass; item.a.shouldInitialize = item.shouldInitialize; }
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
        }
    }

    static bool SkipPhysiology() => false;

    static void NativeBond(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var rootField = typeof(Current).GetField("rootInt", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
        var oldRoot = rootField.GetValue(null);
        var oldGame = Current.Game;
        var oldDefs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "DemonBondCapacity" });
        var harmonyAssembly = Assembly.Load("0Harmony"); var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony")!;
        var harmonyId = "rwom.regression.demon-bond." + Guid.NewGuid(); var harmony = Activator.CreateInstance(harmonyType, harmonyId)!;

        var children = new[] { TorannMagicDefOf.TM_ShadowCall, TorannMagicDefOf.TM_ShadowStep };
        var oldTypes = children.Select(a => a.abilityClass).ToArray();
        try
        {
            // The headless fixture lacks native physiology databases. Keep actual
            // hediff construction/insertion/removal and native bonding untouched.
            var prefix = Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(DemonOwnershipTests).GetMethod(nameof(SkipPhysiology), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmonyType.GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange"), prefix, null, null, null]);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var root = (Root_Play)RuntimeHelpers.GetUninitializedObject(typeof(Root_Play));
            var ui = (UIRoot_Play)RuntimeHelpers.GetUninitializedObject(typeof(UIRoot_Play));
            ui.mapUI = (MapInterface)RuntimeHelpers.GetUninitializedObject(typeof(MapInterface));
            Set(ui.mapUI, "colonistBar", RuntimeHelpers.GetUninitializedObject(typeof(ColonistBar)));
            root.uiRoot = ui;
            rootField.SetValue(null, root);
            foreach (var child in children) child.abilityClass = typeof(AbilityUser.PawnAbility);
            DefDatabase<HediffDef>.Clear();
            var hostType = typeof(MagicAbility).Assembly.GetType("TorannMagic.HediffComp_SoulBondHost")!;
            foreach (string name in new[] { "TM_WDSoulBondMentalHD", "TM_SDSoulBondPhysicalHD", "TM_SoulBondMentalHD", "TM_SoulBondPhysicalHD" })
                DefDatabase<HediffDef>.Add(new HediffDef { defName = name, hediffClass = typeof(HediffWithComps),
                    comps = new List<HediffCompProperties> { new() { compClass = name.StartsWith("TM_SoulBond") ? hostType : typeof(MagicAbility).Assembly.GetType("TorannMagic.HediffComp_SoulBond")! } } });
            foreach (bool warlock in new[] { true, false })
            foreach (var gender in new[] { Gender.None, Gender.Male, Gender.Female })
            {
                var p = PawnWithComps(true, false); var target = PawnWithComps(false, false).pawn;
                foreach (var pawn in new[] { p.pawn, target })
                {
                    pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "DemonBondBody" } } };
                    pawn.mindState = new Verse.AI.Pawn_MindState(pawn); pawn.guest = new Pawn_GuestTracker(pawn);
                }
                p.pawn.gender = gender; Enroll(p.pawn, warlock);
                var faction = new Faction { def = new FactionDef { isPlayer = true } }; Set(p.pawn, "factionInt", faction); Set(target, "factionInt", faction);
                var ability = new AbilityUser.PawnAbility(p.magic); Set(ability, "powerDef", TorannMagicDefOf.TM_SoulBond);
                var type = typeof(MagicAbility).Assembly.GetType("TorannMagic.Verb_SoulBond")!;
                var verb = (AbilityUser.Verb_UseAbility)Activator.CreateInstance(type)!; verb.Ability = ability;
                typeof(Verb).GetField("caster", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(verb, p.pawn);
                Set(verb, "currentTarget", new LocalTargetInfo(target));
                var cast = type.GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!;
                try { cast.Invoke(verb, null); } catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
                string casterName = warlock ? "TM_WDSoulBondMentalHD" : "TM_SDSoulBondPhysicalHD";
                string hostName = warlock ? "TM_SoulBondMentalHD" : "TM_SoulBondPhysicalHD";
                Check.Equal(true, p.pawn.health.hediffSet.hediffs.Any(h => h.def.defName == casterName), "native cast applies selected caster family for every gender");
                Check.Equal(1, target.health.hediffSet.hediffs.Count(h => h.def.defName.StartsWith("TM_SoulBond")), "native cast applies exactly one host family");
                var host = (HediffWithComps)target.health.hediffSet.hediffs.Single(h => h.def.defName == hostName);
                Check.Same(p.pawn, hostType.GetProperty("BonderPawn")!.GetValue(host.comps.Single())!, "native host references caster");
                Check.Same(target, p.magic.soulBondPawn, "native bond stores target");
                foreach (var child in children)
                {
                    var power = new MagicAbility(p.magic) { Def = child };
                    Check.Equal(false, TorannMagic.Odyssey.ManualTravelSafety.TakePayment(power),
                        "native job callback cannot charge before bonded travel succeeds");
                }
                var retained = p.magic.AbilityData.Powers.Single(v => v.Def == children[0]);
                retained.CooldownTicksLeft = 91;
                p.magic.RemovePawnAbility(children[1]);
                var restore = typeof(TM_Action).Assembly.GetType("TorannMagic.ClassProgression.DemonSoulBond")!
                    .GetMethod("RestoreTravelControls", BindingFlags.Static | BindingFlags.NonPublic)!;
                restore.Invoke(null, [p.magic]);
                restore.Invoke(null, [p.magic]);
                Check.Same(retained, p.magic.AbilityData.Powers.Single(v => v.Def == children[0]), "load keeps the existing command");
                Check.Equal(91, retained.CooldownTicksLeft, "load preserves bonded ability cooldown");
                Check.Equal(1, p.magic.AbilityData.Powers.Count(v => v.Def == children[1]), "missing command restored once");
                var substitute = PawnWithComps(false, false).pawn;
                var replacement = hostType.GetField("polyHost")!;
                TM_Action.TransferSoulBond(target, substitute);
                Check.Same(substitute, replacement.GetValue(host.comps.Single())!, "polymorph updates the receiving half of the bond");
                var resolve = typeof(TM_Action).Assembly.GetType("TorannMagic.ClassProgression.DemonSoulBond")!
                    .GetMethod("TravelPartner", BindingFlags.Static | BindingFlags.NonPublic)!;
                Check.Same(null!, resolve.Invoke(null, [p.pawn])!, "unplaced polymorph cannot be a landing target");
                var caravan = new RimWorld.Planet.Caravan();
                Check.Equal(true, caravan.pawns.TryAdd(substitute, false), "caravan owns the replacement pawn");
                Check.Same(substitute, resolve.Invoke(null, [p.pawn])!, "bond resolves the caravan passenger");
                Check.Equal(true, caravan.pawns.TryAdd(target, false), "original partner rejoins the caravan");
                Check.Same(target, resolve.Invoke(null, [p.pawn])!, "original takes priority after returning");
                p.magic.soulBondPawn = null;
                TM_Action.TransferSoulBond(target, p.pawn);
                Check.Same(substitute, replacement.GetValue(host.comps.Single())!, "stale one-sided bond cannot redirect travel");
                p.magic.soulBondPawn = target;
                Check.Equal(true, p.magic.spell_ShadowCall && p.magic.spell_ShadowStep, "native children granted");
                try { cast.Invoke(verb, null); } catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
                Check.Same(null!, p.magic.soulBondPawn!, "native recast toggles off");
                Check.Equal(false, target.health.hediffSet.hediffs.Any(h => h.def.defName == hostName), "native toggle removes matching host");
                Check.Equal(false, p.pawn.health.hediffSet.hediffs.Any(h => h.def.defName == casterName), "native toggle removes matching caster");
            }
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, [harmonyId]);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var capacity in capacities) DefDatabase<PawnCapacityDef>.Add(capacity);
            rootField.SetValue(null, oldRoot);
            Current.Game = oldGame;
            DefDatabase<HediffDef>.Clear(); foreach (var def in oldDefs) DefDatabase<HediffDef>.Add(def);
            for (int i = 0; i < children.Length; i++) children[i].abilityClass = oldTypes[i];
        }
    }

    static void Identity(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        foreach (bool warlock in new[] { true, false })
        foreach (var gender in new[] { Gender.None, Gender.Male, Gender.Female })
        {
            var p = PawnWithComps(true, false); p.pawn.gender = gender; Enroll(p.pawn, warlock);
            Check.Equal(warlock, ClassIdentity.DemonEffect(p.pawn, true, gender == Gender.Male), "selected SoulBond warlock family");
            Check.Equal(!warlock, ClassIdentity.DemonEffect(p.pawn, false, gender == Gender.Female), "selected SoulBond succubus family");
            Check.Equal(warlock, ClassIdentity.DemonEffect(p.pawn, true, true), "Dominate cannot combine opposite family");
            Check.Equal(!warlock, ClassIdentity.DemonEffect(p.pawn, false, true), "Dominate selected family exclusive");
            Check.Equal(0, p.pawn.story.traits.allTraits.Count, "no class trait inserted");
        }
        var legacy = PawnWithComps(true, false).pawn;
        Check.Equal(true, ClassIdentity.DemonEffect(legacy, true, true), "legacy independent positive predicate preserved");
        Check.Equal(true, ClassIdentity.DemonEffect(legacy, false, true), "legacy contrary trait can still match both");
        Check.Equal(false, ClassIdentity.DemonEffect(legacy, true, false), "legacy negative predicate preserved");
    }

    static void LegacyOwners(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        foreach (bool warlock in new[] { true, false })
        {
            var p = PawnWithComps(true, false); p.pawn.story.traits.allTraits.Add(new Trait(warlock ? TorannMagicDefOf.Warlock : TorannMagicDefOf.Succubus));
            var owner = Owner(warlock ? p.magic.MagicData.MagicPowersWD : p.magic.MagicData.MagicPowersSD, "TM_SoulBond");
            Check.Same(owner, TM_ClassState.PreferredMagicOwner(p.magic, owner.TMabilityDefs[0]), "native trait selects existing native owner");
        }
    }
}
