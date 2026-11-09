using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// ClassGrantTool: the public grant facade for editor integrations (Character
// Editor adapter). Each grant runs the same enrollment transaction as the
// book paths — fresh comp admission, vocation + foundation discipline,
// learned starting powers, consumed potentials — and rejects pawns that
// already own class data.
internal static class ClassGrantToolTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassGrantTool.MageFoundation", MageFoundation);
        yield return new("ClassGrantTool.MartialFoundation", MartialFoundation);
        yield return new("ClassGrantTool.UnknownIdRefused", UnknownIdRefused);
    }

    private static void MageFoundation(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var previousGame = Current.Game;
        var game = (Game)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(game, new TickManager());
        typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(game, new UniqueIDsManager());
        Current.Game = game;
        try
        {
            var (pawn, magic, _) = GrimoireProgressionTests.PawnWithComps(false, false);
            pawn.AllComps.Add(new CompClassProgression { parent = pawn });
            if (DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage") == null)
                DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationMage", degreeDatas = [new TraitDegreeData { degree = 0 }] });
            var node = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,
                "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml")).Root!.Elements()
                .Single(e => (string?)e.Element("profileId") == "rwom.fire");
            var profile = new ClassDisciplineDef
            {
                defName = "ClassGrantFire", profileId = "rwom.fire", domain = DisciplineDomain.Magic,
                legacyTrait = new TraitDef { defName = "ClassGrantFireTrait", degreeDatas = [new TraitDegreeData { degree = 0, label = "ClassGrantFire" }] },
                magicPowerList = node.Element("magicPowerList")!.Value,
                foundationBook = BookDef(node.Element("foundationBook")!.Value),
                adeptBook = BookDef(node.Element("adeptBook")!.Value),
                masterBook = BookDef(node.Element("masterBook")!.Value),
                adeptTrialId = node.Element("adeptTrialId")!.Value,
                masterTrialId = node.Element("masterTrialId")!.Value,
                spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell
                {
                    ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(e.Element("ability")!.Value)
                        ?? f.AddAbility( e.Element("ability")!.Value),
                    stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value)
                }).ToList()
            };
            DefDatabase<ClassDisciplineDef>.Add(profile);
            // The facade option and the grant must agree on the same id.
            var option = ClassGrantTool.Options().Single(o => o.Id == "ClassGrantFireTrait");
            Check.Equal("Magic", option.Domain, "fire profile listed as magic");
            if (DefDatabase<GeneDef>.GetNamedSilentFail("TM_HiddenMageGene") == null)
                DefDatabase<GeneDef>.Add(new GeneDef { defName = "TM_HiddenMageGene", geneClass = typeof(Gene) });
            pawn.genes = new Pawn_GeneTracker(pawn);
            pawn.genes.AddGene(DefDatabase<GeneDef>.GetNamed("TM_HiddenMageGene"), true);

            Check.Equal(true, ClassGrantTool.TryGrant(pawn, "ClassGrantFireTrait", out var error),
                "magic grant commits: " + (error ?? "ok"));
            var state = ClassIdentity.Component(pawn)!.State!;
            Check.NotNull(state.Find("rwom.fire"), "fire discipline committed");
            Check.Equal(true, pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("TM_VocationMage")), "mage vocation trait granted");
            Check.Equal(false, pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted), "gifted potential consumed");
            Check.Equal(false, pawn.genes.GenesListForReading.Any(g => g.def.defName == "TM_HiddenMageGene"),
                "latent hidden mage gene consumed by the chosen class");
            var learned = magic.MagicData.MagicPowersIF.Count(q => q?.learned == true);
            Check.Equal(true, learned >= 2, "foundation spells learned: " + learned);
        }
        finally { Current.Game = previousGame; }
    }

    private static void MartialFoundation(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var previousGame = Current.Game;
        var game = (Game)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(game, new TickManager());
        typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(game, new UniqueIDsManager());
        Current.Game = game;
        try
        {
            var (pawn, _, might) = GrimoireProgressionTests.PawnWithComps(false, true);
            // Fresh comp: the grant owns enrollment, so prior data must be absent.
            Set(might, "mightData", null);
            pawn.AllComps.Add(new CompClassProgression { parent = pawn });
            if (DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarrior") == null)
                DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationWarrior", degreeDatas = [new TraitDegreeData { degree = 0 }] });
            var sprint = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_Sprint") ?? f.AddAbility( "TM_Sprint");
            Check.NotNull(sprint, "sprint ability present");
            sprint.abilityClass = typeof(AbilityUser.PawnAbility);
            if (DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorSweep") == null)
            {
                var sweepStub = (TMAbilityDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
                sweepStub.defName = "TM_GladiatorSweep";
                sweepStub.MainVerb = new AbilityUser.VerbProperties_Ability { verbClass = typeof(AbilityUser.Verb_UseAbility) };
                DefDatabase<TMAbilityDef>.Add(sweepStub);
            }

            Check.Equal(true, ClassGrantTool.TryGrant(pawn, "Gladiator", out var error),
                "martial grant commits: " + (error ?? "ok"));
            var state = ClassIdentity.Component(pawn)!.State!;
            Check.NotNull(state.Find("rwom.gladiator"), "gladiator discipline committed");
            Check.Equal(true, might.MightData.MightPowersG.Single(q => q.TMabilityDefs.Contains(sprint)).learned,
                "classic power learned inside the grant");
            Check.Equal(true, pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("TM_VocationWarrior")), "warrior vocation trait granted");
        }
        finally { Current.Game = previousGame; }
    }

    private static ThingDef BookDef(string name)
    {
        var d = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = name; d.thingClass = typeof(Thing); d.category = ThingCategory.Item;
        d.destroyable = true; d.stackLimit = 75; d.drawerType = DrawerType.RealtimeOnly;
        return d;
    }

    private static void UnknownIdRefused(RuntimeFixture f)
    {
        var (pawn, _, _) = GrimoireProgressionTests.PawnWithComps(false, false);
        Check.Equal(false, ClassGrantTool.TryGrant(pawn, "ClassGrantNonexistent", out var error), "unknown id refused");
        Check.Equal("unknown class id ClassGrantNonexistent", error, "refusal names the id");
    }
}
