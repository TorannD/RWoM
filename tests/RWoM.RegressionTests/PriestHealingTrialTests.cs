using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class PriestHealingTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("PriestHealingTrial.ThresholdAndSuccessfulHealingOnce", Thresholds);
        yield return new("PriestHealingTrial.RejectsForeignTargetsAndAbilityOwners", Ownership);
        yield return new("PriestHealingTrial.NativeHookAttaches", Hook);
    }

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public DisciplineProgress Path;
        public PawnAbility Ability;
        public Hediff_Injury Injury;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TrialPriest", profileId = "rwom.priest", domain = DisciplineDomain.Magic });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic;
            Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.kindDef = new PawnKindDef { defName = "TrialPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            var comp = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(comp);
            comp.BeginNewProgression(Vocation.Mage); comp.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
            Path = comp.State.Find("rwom.priest");
            Ability = new PawnAbility(Magic); Set(Ability, "powerDef", TorannMagicDefOf.TM_AdvancedHeal);
            Ability.CooldownTicksLeft = 91; Magic.AbilityData.Powers.Add(Ability);
            Injury = new Hediff_Injury { pawn = Target, def = new HediffDef { defName = "TrialInjury" } };
            Target.health.hediffSet.hediffs.Add(Injury); Severity(20f);
        }
        public void Severity(float value) => Set(Injury, "severityInt", value);
        public PriestHealingTrial.Snapshot Capture() => PriestHealingTrial.Capture(Caster, Target, Ability);
        public void Dispose()
        {
            Current.Game = previous;
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
        }
    }

    static void Thresholds(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        w.Magic.MagicData.MagicUserXP = 44; w.Magic.MagicData.MagicAbilityPoints = 3;
        var skill = w.Magic.MagicData.MagicPowerSkill_AdvancedHeal[0]; skill.level = 2;
        w.Severity(19.99f); Check.Same(null!, w.Capture()!, "requires at least twenty severity before cast");
        w.Severity(20f); var unchanged = w.Capture();
        Check.Equal(false, PriestHealingTrial.Complete(unchanged, true), "successful no-op cast earns nothing");
        var failed = w.Capture(); w.Severity(5f);
        Check.Equal(false, PriestHealingTrial.Complete(failed, false), "failed cast cannot credit external healing");
        w.Severity(20f); var insufficient = w.Capture(); w.Severity(10.01f);
        Check.Equal(false, PriestHealingTrial.Complete(insufficient, true), "requires ten actual severity reduction");
        w.Severity(20f); var healed = w.Capture(); w.Severity(10f);
        Check.Equal(true, PriestHealingTrial.Complete(healed, true), "boundary healing completes trial");
        Check.Equal(false, PriestHealingTrial.Complete(healed, true), "snapshot may only be consumed once");
        w.Severity(30f); Check.Same(null!, w.Capture()!, "completed trial stores no further cast snapshots");
        Check.Equal(1, w.Path.CompletedTrials.Count, "one persistent completion marker only");
        Check.Equal(PriestHealingTrial.TrialId, w.Path.CompletedTrials[0], "stable trial identifier");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "trial never advances stage");
        Check.Equal(44, w.Magic.MagicData.MagicUserXP, "no XP reward");
        Check.Equal(3, w.Magic.MagicData.MagicAbilityPoints, "no point reward");
        Check.Equal(2, skill.level, "skill investment preserved");
        Check.Equal(91, w.Ability.CooldownTicksLeft, "trial does not alter cooldown");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Same(null!, PriestHealingTrial.Capture(w.Caster, w.Caster, w.Ability)!, "self healing excluded");
        var faction = w.Target.Faction;
        Set(w.Target, "factionInt", null); Check.Same(null!, w.Capture()!, "factionless targets excluded");
        Set(w.Target, "factionInt", faction);
        Set(w.Target, "mapIndexOrState", (sbyte)1); Check.Same(null!, w.Capture()!, "cross-map targets excluded");
        Set(w.Target, "mapIndexOrState", (sbyte)0);
        var enemy = new Faction { def = new FactionDef() };
        Set(enemy, "relations", new List<FactionRelation> { new() { other = faction, kind = FactionRelationKind.Hostile } });
        Set(faction, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
        Set(w.Target, "factionInt", enemy); Check.Same(null!, w.Capture()!, "hostile targets excluded");
        Set(w.Target, "factionInt", faction);
        w.Target.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Undead));
        Check.Same(null!, w.Capture()!, "undead damage cannot earn healing trial");
        w.Target.story.traits.allTraits.Clear();
        var fake = new PawnAbility(w.Magic); Set(fake, "powerDef", TorannMagicDefOf.TM_AdvancedHeal);
        Check.Same(null!, PriestHealingTrial.Capture(w.Caster, w.Target, fake)!, "same definition without permanent owner is excluded");
        w.Magic.AbilityData.Powers.Clear(); w.Magic.AbilityData.TemporaryWeaponPowers.Add(w.Ability);
        Check.Same(null!, w.Capture()!, "item or temporary ability is excluded");
        w.Magic.AbilityData.TemporaryWeaponPowers.Clear(); w.Magic.AbilityData.Powers.Add(w.Ability);
        var stale = w.Capture(); w.Severity(1f); w.Magic.AbilityData.Powers.Clear();
        Check.Equal(false, PriestHealingTrial.Complete(stale, true), "lost ownership before completion is excluded");
        Check.Equal(0, w.Path.CompletedTrials.Count, "rejected events never record completion");
    }

    static void Hook(RuntimeFixture f)
    {
        var native = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Verb_AdvancedHeal")!
            .GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
        Check.Equal(typeof(bool), native.ReturnType, "hook targets original successful-cast result");
        var harmony = Assembly.Load("0Harmony"); var ht = harmony.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.priest-trial." + Guid.NewGuid(); var instance = Activator.CreateInstance(ht, id)!;
        try
        {
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(instance, [typeof(PriestHealingTrialPatch)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            var patched = ((IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!).ToList();
            Check.Equal(true, patched.Contains(native), "prefix/postfix compile and attach to original heal verb");
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(instance, [id]); }
    }
}
