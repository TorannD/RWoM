using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// Ф4 step 4: the grimoire's knowledge adapter. Sleeping knowledge — learned
// entries whose def no longer resolves — is surfaced for a grey section;
// convention ids prettify into readable names. Classic cards keep reading
// their legacy owners.
internal static class GrimoireKnowledgeTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GrimoireKnowledge.DisplayNamePrettifies", DisplayNamePrettifies);
        yield return new("GrimoireKnowledge.SleepingFiltersByResolvability", SleepingFiltersByResolvability);
        yield return new("GrimoireKnowledge.SleepingNullSafe", SleepingNullSafe);
        yield return new("GrimoireKnowledge.SyncMirrorsWrites", SyncMirrorsWrites);
    }

    // Ф4 step 4 stage 2: grimoire writes (learn/level-up/skill rank) mirror
    // into the knowledge store so it stays a complete mirror after the
    // one-shot import. Role comes from the skill label suffix.
    private static void SyncMirrorsWrites(RuntimeFixture _)
    {
        if (Current.Game == null)
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        }
        // An existing shell from an earlier suite may lack the managers the
        // Pawn ctor needs — patch them in individually.
        if (Current.Game.tickManager == null)
            typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Current.Game, new TickManager());
        if (Current.Game.uniqueIDsManager == null)
            typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Current.Game, new UniqueIDsManager());
        var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawnDef.defName = "Regression_GrimSync";
        pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = pawnDef };
        for (Type type = pawn.GetType(); type != null; type = type.BaseType)
        {
            var compsField = type.GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (compsField == null) continue;
            compsField.SetValue(pawn, new List<ThingComp>());
            break;
        }
        pawn.AllComps.Add(new AbilityKnowledgeComp { parent = pawn });
        var knowledge = pawn.TryGetComp<AbilityKnowledgeComp>();

        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = "TM_GrimSyncA";
        def.label = "TM_GrimSyncA";

        GrimoireKnowledge.SyncEntry(pawn, def, 2, true);
        var entry = knowledge.Find(SpellIds.Of(def));
        Check.NotNull(entry, "sync creates the entry");
        Check.Equal(true, entry.learned, "learned mirrored");
        Check.Equal(2, entry.tier, "tier mirrored");

        GrimoireKnowledge.SyncRank(pawn, def, "TM_grimsynca_pwr", 3);
        GrimoireKnowledge.SyncRank(pawn, def, "TM_grimsynca_eff", 1);
        GrimoireKnowledge.SyncRank(pawn, def, "TM_grimsynca_ver", 2);
        Check.Equal(3, knowledge.GetSkillRank(SpellIds.Of(def), 0), "pwr rank mirrored");
        Check.Equal(1, knowledge.GetSkillRank(SpellIds.Of(def), 1), "eff rank mirrored");
        Check.Equal(2, knowledge.GetSkillRank(SpellIds.Of(def), 2), "ver rank mirrored");

        // Global stat skills are not per-spell knowledge — a non-suffix label is a no-op.
        GrimoireKnowledge.SyncRank(pawn, def, "TM_global_regen", 5);
        Check.Equal(3, knowledge.GetSkillRank(SpellIds.Of(def), 0), "unknown label suffix is a no-op");

        GrimoireKnowledge.SyncEntry(pawn, null, 0, true);
        GrimoireKnowledge.SyncRank(pawn, def, "TM_grimsynca_pwr", 4);
        Check.Equal(4, knowledge.GetSkillRank(SpellIds.Of(def), 0), "null-def sync is a no-op for ranks");
        var next = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        next.defName = "TM_GrimSyncA_III";
        GrimoireKnowledge.ChangeTier(pawn, def, next, 3, true, true);
        Check.Equal(true, knowledge.Find(SpellIds.Of(def)) == null, "obsolete tier retired");
        var upgraded = knowledge.Find(SpellIds.Of(next));
        Check.Equal(true, upgraded.autocast && upgraded.learned, "new tier keeps the toggles");
        Check.Equal(4, upgraded.pwr, "new tier keeps the rank");
    }

    // RED 1: convention ids read as spell names; unknown shapes come back sane.
    private static void DisplayNamePrettifies(RuntimeFixture _)
    {
        Check.Equal("Heal I", GrimoireKnowledge.DisplayName("rwom.tm_heal_i"), "variant suffix becomes a roman numeral word");
        Check.Equal("Advanced Heal II", GrimoireKnowledge.DisplayName("rwom.tm_advanced_heal_ii"), "multi-word base prettifies");
        Check.Equal("Magic Missile", GrimoireKnowledge.DisplayName("rwom.tm_magic_missile"), "plain base prettifies");
        Check.Equal("custom.family", GrimoireKnowledge.DisplayName("custom.family"), "non-convention id returned as-is");
        Check.Equal("???", GrimoireKnowledge.DisplayName(null), "null id has a placeholder");
        Check.Equal("???", GrimoireKnowledge.DisplayName(""), "empty id has a placeholder");
    }

    // RED 2: only learned entries whose def fails to resolve are sleeping.
    private static void SleepingFiltersByResolvability(RuntimeFixture f)
    {
        var coreDll = CoreSdkTests.AssemblyPath(f.Repository);
        bool hasCore = File.Exists(coreDll);
        if (hasCore) Assembly.LoadFrom(coreDll);

        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = "TM_KnowledgeGrim";
        def.label = "TM_KnowledgeGrim";
        def.MainVerb = new AbilityUser.VerbProperties_Ability();
        CoreBridge.RegisterSpell(def); // resolvable

        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        var alive = knowledge.GetOrCreate("rwom.tm_knowledgegrim", 0);
        alive.learned = true;
        var sleeping = knowledge.GetOrCreate("rwom.tm_removed_spell_ii", 0);
        sleeping.learned = true;
        var forgotten = knowledge.GetOrCreate("rwom.tm_forgotten_spell", 0);
        forgotten.learned = false; // unlearned entries are never listed

        var result = GrimoireKnowledge.SleepingSpells(knowledge);
        Check.Equal(hasCore ? 1 : 2, result.Count, "without the Core resolver both learned entries remain sleeping");
        Check.Equal(true, result.Contains(sleeping), "removed spell remains sleeping");
        Check.Equal(!hasCore, result.Contains(alive), "registered spell resolves only with Core available");
        Check.Equal(true, alive.learned && sleeping.learned, "missing resolver does not erase learned knowledge");
    }

    // RED 3: missing pawn/comp/store never throws.
    private static void SleepingNullSafe(RuntimeFixture _)
    {
        Check.Equal(0, GrimoireKnowledge.SleepingSpells((AbilityKnowledgeComp)null).Count, "null comp yields an empty list");
        var empty = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        Check.Equal(0, GrimoireKnowledge.SleepingSpells(empty).Count, "empty store yields an empty list");
    }
}
