using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static class DamageTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static void Initialize()
    {
        RuntimeHelpers.RunClassConstructor(typeof(PawnKindDefOf).TypeHandle);
        RuntimeLog.AcceptHeadlessDefOfInitialization("PawnKindDefOf");
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Damage.UndeadFortitudeWithoutStaminaMitigatesExplosion", f => Fortitude(f, false, 20f));
        yield return new("Damage.LivingFortitudeRestoresExistingStamina", f => Fortitude(f, true, 20f));
        yield return new("Damage.UndeadFortitudeWithoutStaminaFullyAbsorbsExplosion", f => Fortitude(f, false, 4f));
        yield return new("Damage.LivingFortitudeFullyAbsorbsAndRestoresStamina", f => Fortitude(f, true, 4f));
    }

    private static void Set(object target, string name, object value)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Flags);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new InvalidOperationException("Missing damage fixture field " + name);
    }

    private static Pawn Pawn(bool spawned)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "DamageHuman";
        def.size = new IntVec2(1, 1);
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = def, Name = new NameSingle("DamagePawn") };
        pawn.kindDef = new PawnKindDef { defName = "DamageHumanKind", race = def };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        Set(pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000L);
        pawn.needs = new Pawn_NeedsTracker(pawn);
        Set(pawn, "comps", new List<ThingComp> { new CompCanBeDormant { parent = pawn } });
        if (spawned) Set(pawn, "mapIndexOrState", (sbyte)0);
        return pawn;
    }

    private static void Fortitude(RuntimeFixture f, bool living, float incoming)
    {
        Current.Game ??= (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
        Set(Current.Game, "tickManager", new TickManager());
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIHardMode = false, needMultiplier = 1f };
        TorannMagicDefOf.TM_HediffFortitude = new HediffDef { defName = "TM_HediffFortitude" };
        TorannMagicDefOf.TM_UndeadHD = DefDatabase<HediffDef>.GetNamedSilentFail("TM_UndeadHD") ?? new HediffDef { defName = "TM_UndeadHD" };
        if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_UndeadHD") == null) DefDatabase<HediffDef>.Add(TorannMagicDefOf.TM_UndeadHD);
        var staminaDef = new NeedDef { defName = "TM_Stamina", needClass = typeof(Need_Stamina) };
        var target = Pawn(false);
        target.health.hediffSet.hediffs.Add(new Hediff { def = TorannMagicDefOf.TM_HediffFortitude, pawn = target });
        var comp = new CompAbilityUserMight { parent = target, spRegenRate = 1f };
        target.AllComps.Add(comp);
        TM_Calc.InvalidateCompCache(target);
        Set(comp, "initializedIsMightUser", true);
        Set(comp, "isMightUser", living);
        var data = new MightData();
        Set(comp, "mightData", data);
        data.MightPowerSkill_Fortitude.Single(s => s.label == "TM_Fortitude_pwr").level = 2;
        data.MightPowerSkill_Fortitude.Single(s => s.label == "TM_Fortitude_ver").level = 3;
        Need_Stamina? stamina = null;
        if (living)
        {
            stamina = new Need_Stamina(target) { def = staminaDef, CurLevel = .5f };
            target.needs.AllNeeds.Add(stamina);
        }
        else
        {
            target.health.hediffSet.hediffs.Add(new Hediff { def = TorannMagicDefOf.TM_UndeadHD, pawn = target });
            bool shouldHaveNeed = true;
            bool runOriginal = TorannMagicMod.Pawn_NeedsTracker_Patch.Prefix(target.needs, staminaDef, target, ref shouldHaveNeed);
            Check.Equal(false, runOriginal, "Undead need policy overrides vanilla need eligibility");
            Check.Equal(false, shouldHaveNeed, "Undead are explicitly forbidden a stamina pool");
            Check.Equal<Need_Stamina?>(null, comp.Stamina, "Absent stamina is a legitimate undead state");
            Check.Equal(false, comp.IsMightUser, "Resurrection removes class traits while retaining the saved skill data");
            Check.Same(data, comp.MightData, "The preexisting MightData remains available after class loss");
        }
        var attacker = Pawn(true);
        var explosion = new DamageDef { defName = "Bomb", isExplosive = true };
        var damage = new DamageInfo(explosion, incoming, instigator: attacker);
        bool continueOriginal = TorannMagicMod.Pawn_PreApplyDamage.Prefix(target, ref damage, out bool absorbed);
        Check.Equal(incoming >= 7f, continueOriginal, "Fortitude stops fully absorbed hits and passes residual damage onward");
        Check.Equal(Math.Max(0f, incoming - 7f), damage.Amount, "Fortitude uses the existing invested power level for flat mitigation");
        Check.Equal(true, absorbed, "Fortitude preserves its absorption signal");
        if (stamina != null)
        {
            Check.Equal(true, stamina.CurLevel > .5f, "Living Fortitude still restores its existing stamina pool");
        }
        else Check.Equal<Need_Stamina?>(null, comp.Stamina, "Damage handling does not create a stamina need forbidden to undead");
    }
}
