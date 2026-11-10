using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// The "undead corpse" storage filters (TM_AllowUndeadCorpse under
// CorpsesHumanlike, TM_AllowUndeadAnimalCorpse under CorpsesAnimal): the
// scope worker, the vanilla ThingFilter wiring, and the postfix that keeps
// the vanilla colonist/stranger/slave corpse filters from matching undead
// corpses. Disallowed by default, the toggles keep necromancy-raised
// corpses out of hauling and butcher bills until the player opts in.
internal static class SpecialThingFilterTests
{
    private static readonly BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpecialFilter.UndeadCorpseScope", UndeadCorpseScope);
        yield return new("SpecialFilter.ThingFilterGate", ThingFilterGate);
        yield return new("SpecialFilter.AnimalCorpseGate", AnimalCorpseGate);
        yield return new("SpecialFilter.VanillaCorpseFiltersHandsOff", VanillaCorpseFiltersHandsOff);
    }

    // Hediff PostAdd and the filter scan both touch the game singleton
    // (TickManager), so the cases run under the same minimal-game swap the
    // grant tool tests use.
    private static Game SwapInMinimalGame(RuntimeFixture f)
    {
        var previousGame = Current.Game;
        var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("tickManager", Flags)!.SetValue(game, new TickManager());
        typeof(Game).GetField("uniqueIDsManager", Flags)!.SetValue(game, new UniqueIDsManager());
        Current.Game = game;
        return previousGame;
    }

    private static HediffDef UndeadHediff(string defName)
    {
        var def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
        if (def == null)
        {
            def = new HediffDef { defName = defName, hediffClass = typeof(HediffWithComps) };
            DefDatabase<HediffDef>.Add(def);
        }
        // The DefOf binds once per process; stub it before any pawn needs it.
        var field = typeof(TorannMagicDefOf).GetField(defName, Flags);
        if (field != null && field.GetValue(null) == null) field.SetValue(null, def);
        return def;
    }

    private static ThingCategoryDef CorpseCategory(string defName)
    {
        var category = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(defName);
        if (category == null)
        {
            // The ThingCategoryDef constructor initializes graphics content;
            // the uninitialized stub keeps the tree walk (def -> parent) intact.
            category = (ThingCategoryDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingCategoryDef));
            category.defName = defName;
            category.label = defName;
            DefDatabase<ThingCategoryDef>.Add(category);
        }
        return category;
    }

    private static ThingDef CorpseDef(string defName, ThingCategoryDef category)
    {
        var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        if (def != null) return def;
        // The ThingDef constructor initializes graphics content; the
        // uninitialized stub carries only the fields the filter path reads.
        def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = defName;
        def.thingClass = typeof(Corpse);
        def.category = ThingCategory.Item;
        def.thingCategories = [category];
        DefDatabase<ThingDef>.Add(def);
        return def;
    }

    private static Corpse MakeCorpse(string defName, ThingCategoryDef category, Pawn inner)
    {
        var corpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse));
        corpse.def = CorpseDef(defName, category);
        // 1.7 corpses keep their pawn in an innerContainer ThingOwner, so the
        // container itself is built reflectively and filled with the pawn.
        var container = (ThingOwner<Pawn>)System.Activator.CreateInstance(
            typeof(ThingOwner<Pawn>), (IThingHolder)null)!;
        container.TryAdd(inner);
        typeof(Corpse).GetField("innerContainer", Flags)!.SetValue(corpse, container);
        return corpse;
    }

    private static (Corpse humanUndead, Corpse animalUndead, Corpse normalHuman, Corpse normalAnimal) MakeCorpses()
    {
        var (carrier, _, _) = PawnWithComps(false, false);
        carrier.health.hediffSet.hediffs.Add(new HediffWithComps { def = UndeadHediff("TM_UndeadHD"), pawn = carrier });
        var (beast, _, _) = PawnWithComps(false, false);
        beast.health.hediffSet.hediffs.Add(new HediffWithComps { def = UndeadHediff("TM_UndeadAnimalHD"), pawn = beast });
        var (plain, _, _) = PawnWithComps(false, false);
        var (plainBeast, _, _) = PawnWithComps(false, false);
        return (
            MakeCorpse("SpecialFilterHumanCorpse", CorpseCategory("SpecialFilterCorpsesHumanlike"), carrier),
            MakeCorpse("SpecialFilterAnimalCorpse", CorpseCategory("SpecialFilterCorpsesAnimal"), beast),
            MakeCorpse("SpecialFilterHumanCorpse", CorpseCategory("SpecialFilterCorpsesHumanlike"), plain),
            MakeCorpse("SpecialFilterAnimalCorpse", CorpseCategory("SpecialFilterCorpsesAnimal"), plainBeast));
    }

    private static SpecialThingFilterDef UndeadFilterDef(string defName, string categoryDefName, string saveKey)
    {
        var def = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(defName);
        if (def != null) return def;
        def = new SpecialThingFilterDef
        {
            defName = defName,
            parentCategory = CorpseCategory(categoryDefName),
            allowedByDefault = false,
            saveKey = saveKey,
            workerClass = typeof(TorannMagic.SpecialThingFilterWorker_UndeadCorpse),
            configurable = true
        };
        DefDatabase<SpecialThingFilterDef>.Add(def);
        return def;
    }

    private static void AllowDef(ThingFilter filter, ThingDef def)
    {
        // SetAllow(ThingDef) walks the category tree, which the stubbed
        // category lacks; adding straight to the allowed set is equivalent.
        ((HashSet<ThingDef>)typeof(ThingFilter).GetField("allowedDefs", Flags)!.GetValue(filter)!)
            .Add(def);
    }

    private static void UndeadCorpseScope(RuntimeFixture f)
    {
        var previousGame = SwapInMinimalGame(f);
        try
        {
            var (humanUndead, animalUndead, normalHuman, _) = MakeCorpses();
            var worker = new TorannMagic.SpecialThingFilterWorker_UndeadCorpse();
            Check.Equal(true, worker.Matches(humanUndead), "undead hediff corpse matches the scope");
            Check.Equal(true, worker.Matches(animalUndead), "undead animal hediff corpse matches the scope");
            Check.Equal(false, worker.Matches(normalHuman), "ordinary corpse stays outside the scope");
            Check.Equal(true, worker.CanEverMatch(humanUndead.def), "corpse defs can ever match");
            var notCorpse = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            notCorpse.defName = "SpecialFilterNotCorpse";
            notCorpse.thingClass = typeof(Thing);
            Check.Equal(false, worker.CanEverMatch(notCorpse), "non-corpse defs never match");
        }
        finally { Current.Game = previousGame; }
    }

    private static void ThingFilterGate(RuntimeFixture f)
    {
        var previousGame = SwapInMinimalGame(f);
        try
        {
            // Must exist before ResolveReferences scans the database.
            var filterDef = UndeadFilterDef("TM_AllowUndeadCorpse", "SpecialFilterCorpsesHumanlike", "allowUndeadCorpses");
            var (humanUndead, _, normalHuman, _) = MakeCorpses();
            var filter = new ThingFilter();
            AllowDef(filter, humanUndead.def);
            filter.ResolveReferences(); // allowedByDefault=false -> toggle starts disallowed

            Check.Equal(false, filter.Allows(humanUndead), "unchecked toggle blocks the undead corpse");
            Check.Equal(true, filter.Allows(normalHuman), "unchecked toggle leaves ordinary corpses alone");

            filter.SetAllow(filterDef, true);
            Check.Equal(true, filter.Allows(humanUndead), "checked toggle admits the undead corpse");
        }
        finally { Current.Game = previousGame; }
    }

    private static void AnimalCorpseGate(RuntimeFixture f)
    {
        var previousGame = SwapInMinimalGame(f);
        try
        {
            var filterDef = UndeadFilterDef("TM_AllowUndeadAnimalCorpse", "SpecialFilterCorpsesAnimal", "allowUndeadAnimalCorpses");
            var (_, animalUndead, _, normalAnimal) = MakeCorpses();
            var filter = new ThingFilter();
            AllowDef(filter, animalUndead.def);
            filter.ResolveReferences();

            Check.Equal(false, filter.Allows(animalUndead), "unchecked animal toggle blocks the undead animal corpse");
            Check.Equal(true, filter.Allows(normalAnimal), "animal toggle never touches ordinary corpses");

            filter.SetAllow(filterDef, true);
            Check.Equal(true, filter.Allows(animalUndead), "checked animal toggle admits the undead animal corpse");
        }
        finally { Current.Game = previousGame; }
    }

    private static void VanillaCorpseFiltersHandsOff(RuntimeFixture f)
    {
        var previousGame = SwapInMinimalGame(f);
        try
        {
            var (humanUndead, animalUndead, normalHuman, _) = MakeCorpses();
            // Shared postfix on the vanilla colonist/stranger/slave corpse
            // workers: undead corpses leave their scope entirely, so the
            // undead toggles are the only remaining gate.
            var matched = true;
            TorannMagic.TorannMagicMod.CorpseFilter_ExcludeUndead_Postfix(humanUndead, ref matched);
            Check.Equal(false, matched, "undead corpse removed from the vanilla corpse filter scope");
            var beastMatched = true;
            TorannMagic.TorannMagicMod.CorpseFilter_ExcludeUndead_Postfix(animalUndead, ref beastMatched);
            Check.Equal(false, beastMatched, "undead animal corpse removed from the vanilla corpse filter scope");
            var untouched = true;
            TorannMagic.TorannMagicMod.CorpseFilter_ExcludeUndead_Postfix(normalHuman, ref untouched);
            Check.Equal(true, untouched, "ordinary corpse stays in the vanilla scope");
            var excluded = false;
            TorannMagic.TorannMagicMod.CorpseFilter_ExcludeUndead_Postfix(humanUndead, ref excluded);
            Check.Equal(false, excluded, "already-negative verdict stays negative");
        }
        finally { Current.Game = previousGame; }
    }
}
