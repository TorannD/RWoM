using System.Runtime.CompilerServices;
using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthRitualEligibilityTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRebirthRitualEligibility.AscendedCreatorNativeOwnerAndHybrid", Creator);
        yield return new("SpiritRebirthRitualEligibility.ExactLargeCircleBusyAndOwnership", Circle);
        yield return new("SpiritRebirthRitualEligibility.NativeCorpseIdentityAndEligibility", CorpseEligibility);
    }
    static T Blank<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    public static bool SkipGraphics() => false;
    internal sealed class Fixture : IDisposable
    {
        readonly object graphicsHarmony;
        readonly Type harmonyType;
        readonly string harmonyId;
        readonly SpiritRebirthStateTests.Fixture shared;
        readonly ClassDisciplineDef[] profiles;
        readonly List<TM_CustomClass> classes;
        public readonly Pawn Caster, Dead;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public readonly Building_TMMagicCircle Circle;
        public readonly Corpse Corpse;
        public readonly Faction Player;
        public Fixture(RuntimeFixture r)
        {
            var assembly = Assembly.Load("0Harmony"); harmonyType = assembly.GetType("HarmonyLib.Harmony")!;
            harmonyId = "rwom.regression.spirit-eligibility-graphics." + Guid.NewGuid();
            graphicsHarmony = Activator.CreateInstance(harmonyType, harmonyId)!;
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthRitualEligibilityTests).GetMethod(nameof(SkipGraphics))!)!;
            foreach (var method in typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial"))
                harmonyType.GetMethod("Patch")!.Invoke(graphicsHarmony, [method, prefix, null, null, null]);
            shared = new SpiritRebirthStateTests.Fixture(r);
            profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray(); classes = TM_ClassUtility.CustomClasses;
            TM_ClassUtility.CustomClasses = [];
            var world = Blank<RimWorld.Planet.World>(); world.factionManager = new FactionManager();
            Player = new Faction { def = new FactionDef { isPlayer = true } };
            world.factionManager.AllFactionsListForReading.Add(Player); Set(world.factionManager, "ofPlayer", Player); Set(Current.Game, "worldInt", world);
            Set(Current.Game, "tickManager", new TickManager());
            Caster = shared.Original; Magic = shared.Magic; Progression = shared.Comp; Dead = shared.Host;
            _ = Magic.MagicData.MagicPowersN; // Existing initialized native owner, as on an ascended caster.
            foreach (var pawn in new[] { Caster, Dead })
            {
                pawn.def.stackLimit = 1; Set(pawn.def.race, "fleshType", new FleshTypeDef { defName = "SpiritRitualOrganic", isOrganic = true });
                pawn.kindDef = new PawnKindDef { defName = "SpiritRitualColonist", race = pawn.def };
                pawn.mindState = new Pawn_MindState(pawn); Set(pawn, "factionInt", Player);
            }
            Caster.carryTracker = new Pawn_CarryTracker(Caster); Set(Caster, "mapIndexOrState", (sbyte)0);
            Caster.Map.events = new MapEvents(Caster.Map);
            Caster.Map.lordManager = new Verse.AI.Group.LordManager(Caster.Map);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
            Set(Progression.State.Find("rwom.necromancer"), "stage", InitiationStage.Ascended);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "SpiritRitualNecromancer", profileId = "rwom.necromancer", domain = DisciplineDomain.Magic,
                spells = [new DisciplineSpell { stage = InitiationStage.Ascended, ability = TorannMagicDefOf.TM_LichForm }] });
            Circle = Blank<Building_TMMagicCircle>(); var circleDef = Blank<ThingDef>();
            circleDef.defName = "SpiritRitualCircle"; circleDef.thingClass = typeof(Building_TMMagicCircle);
            Circle.def = circleDef; Set(Circle, "factionInt", Player); Set(Circle, "mapIndexOrState", (sbyte)0);
            Set(Circle, "mageList", new List<Pawn>());
            Set(Dead.health, "healthState", PawnHealthState.Dead);
            var corpseDef = Blank<ThingDef>(); corpseDef.defName = "SpiritRitualCorpse"; corpseDef.stackLimit = 1; corpseDef.category = ThingCategory.Item;
            Corpse = new Corpse { def = corpseDef, InnerPawn = Dead };
            Set(Corpse, "mapIndexOrState", (sbyte)0);
        }
        public SpiritRebirthRitualRejection Rejection() => SpiritRebirthRitualEligibility.Rejection(Caster, Circle, Corpse, Dead);
        public void Dispose()
        {
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            TM_ClassUtility.CustomClasses = classes; shared.Dispose();
            harmonyType.GetMethod("UnpatchAll")!.Invoke(graphicsHarmony, [harmonyId]);
        }
    }
    static void Creator(RuntimeFixture r)
    {
        using var f = new Fixture(r);
        Check.Equal(SpiritRebirthRitualRejection.None, f.Rejection(), "ascended necromancer accepted without active Lich form");
        Check.Equal(false, f.Progression.State.Find("rwom.necromancer").LichFormActive, "preflight never enters Lich form");
        var path = f.Progression.State.Find("rwom.necromancer");
        Set(path, "stage", InitiationStage.Master);
        Check.Equal(SpiritRebirthRitualRejection.CreatorNotAscendedNecromancer, f.Rejection(), "Master cannot perform ritual");
        Set(path, "stage", InitiationStage.Ascended);
        Set(f.Progression.State, "vocation", Vocation.WarriorMage);
        Check.Equal(SpiritRebirthRitualRejection.None, f.Rejection(), "hybrid ascended necromancer qualifies");
        Set(f.Progression.State, "vocation", Vocation.Warrior);
        Check.Equal(SpiritRebirthRitualRejection.CreatorNotAscendedNecromancer, f.Rejection(), "warrior does not borrow magic path");
        Set(f.Progression.State, "vocation", Vocation.Mage);
        var owners = f.Magic.MagicData.MagicPowersN;
        var owner = owners.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LichForm)); owners.Remove(owner);
        Check.Equal(SpiritRebirthRitualRejection.CreatorPowerOwnerInvalid, f.Rejection(), "missing native ultimate owner rejected");
        owners.Add(owner); owners.Add(owner);
        Check.Equal(SpiritRebirthRitualRejection.CreatorPowerOwnerInvalid, f.Rejection(), "ambiguous native ultimate owner rejected");
        owners.Remove(owner);
        var magicData = f.Magic.MagicData;
        Set(magicData, "magicPowerN", null!);
        Check.Equal(SpiritRebirthRitualRejection.CreatorPowerOwnerInvalid, f.Rejection(), "absent owner list is rejected without repair");
        Check.Equal(true, magicData.magicPowerN == null, "preflight never initializes native owner list");
        Set(magicData, "magicPowerN", owners); Set(f.Magic, "magicData", null!);
        Check.Equal(SpiritRebirthRitualRejection.CreatorPowerOwnerInvalid, f.Rejection(), "absent magic data is rejected without repair");
        Check.Equal(true, typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Magic) == null, "preflight never initializes magic data");
        Set(f.Magic, "magicData", magicData);
        Check.Equal(true, SpiritRebirthStateTests.Invoke(f.Progression, "BeginSpiritRebirth"), "explicit original Spirit marker created");
        Check.Equal(SpiritRebirthRitualRejection.CreatorUnavailable, f.Rejection(), "ethereal original cannot use former-class ritual");
        Set(f.Progression.SpiritRebirth, "version", 999);
        Check.Equal(SpiritRebirthRitualRejection.CreatorUnavailable, f.Rejection(), "unsupported Spirit marker cannot bypass creator gate");
        Set(f.Progression, "spiritRebirth", null!);
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(SpiritRebirthRitualRejection.CreatorProfileInvalid, f.Rejection(), "missing profile rejected");
    }
    static void Circle(RuntimeFixture r)
    {
        using var f = new Fixture(r);
        f.Circle.IsPending = true; Check.Equal(SpiritRebirthRitualRejection.CircleBusy, f.Rejection(), "pending native ritual excludes new ritual");
        f.Circle.IsPending = false; Set(f.Circle, "isActive", true);
        Check.Equal(SpiritRebirthRitualRejection.CircleBusy, f.Rejection(), "active native ritual excluded");
        Set(f.Circle, "isActive", false); f.Circle.MageList.Add(f.Caster);
        Check.Equal(SpiritRebirthRitualRejection.CircleBusy, f.Rejection(), "occupied mage list excluded"); f.Circle.MageList.Clear();
        Set(f.Circle, "factionInt", null!); Check.Equal(SpiritRebirthRitualRejection.CircleWrongOwner, f.Rejection(), "foreign circle rejected");
        Set(f.Circle, "factionInt", f.Player); Set(f.Circle, "mapIndexOrState", (sbyte)-1);
        Check.Equal(SpiritRebirthRitualRejection.CircleUnavailable, f.Rejection(), "unspawned circle rejected");
        Set(f.Circle, "mapIndexOrState", (sbyte)0);
        Check.Equal(SpiritRebirthRitualRejection.CircleUnavailable, SpiritRebirthRitualEligibility.Rejection(f.Caster, new Thing { def = f.Circle.def }, f.Corpse, f.Dead), "definition spoof is not native large circle");
        Check.Equal(SpiritRebirthRitualRejection.None, f.Rejection(), "rechecks leave idle circle usable");
    }
    static void CorpseEligibility(RuntimeFixture r)
    {
        using var f = new Fixture(r); var owner = f.Corpse.GetDirectlyHeldThings(); var data = f.Magic.MagicData;
        Check.Equal(SpiritRebirthRitualRejection.None, f.Rejection(), "actual dead colonist in native corpse accepted");
        Set(f.Dead, "mapIndexOrState", (sbyte)-2);
        Check.Equal(true, f.Dead.Destroyed && !f.Dead.Discarded, "native KillFinalize keeps recoverable identity");
        Check.Equal(SpiritRebirthRitualRejection.None, f.Rejection(), "native kill-finalized colonist remains eligible inside exact corpse");
        Check.Equal(SpiritRebirthRitualRejection.CorpseIdentityChanged, SpiritRebirthRitualEligibility.Rejection(f.Caster, f.Circle, f.Corpse, f.Caster), "selected identity cannot be substituted");
        Set(f.Dead.health, "healthState", PawnHealthState.Mobile);
        Check.Equal(SpiritRebirthRitualRejection.CorpseNotDeadColonist, f.Rejection(), "living pawn rejected"); Set(f.Dead.health, "healthState", PawnHealthState.Dead);
        Set(f.Dead, "factionInt", null!); Check.Equal(SpiritRebirthRitualRejection.CorpseNotDeadColonist, f.Rejection(), "noncolonist rejected"); Set(f.Dead, "factionInt", f.Player);
        var undead = new Hediff { def = TorannMagicDefOf.TM_UndeadHD, pawn = f.Dead }; f.Dead.health.hediffSet.hediffs.Add(undead);
        Check.Equal(SpiritRebirthRitualRejection.CorpseUndead, f.Rejection(), "undead corpse excluded"); f.Dead.health.hediffSet.hediffs.Remove(undead);
        Set(f.Dead, "mapIndexOrState", (sbyte)-3);
        Check.Equal(SpiritRebirthRitualRejection.CorpseNotDeadColonist, f.Rejection(), "discarded identity rejected"); Set(f.Dead, "mapIndexOrState", (sbyte)-1);
        var unnatural = Blank<UnnaturalCorpse>(); unnatural.def = f.Corpse.def; Set(unnatural, "mapIndexOrState", (sbyte)0);
        Check.Equal(SpiritRebirthRitualRejection.CorpseUnnatural, SpiritRebirthRitualEligibility.Rejection(f.Caster, f.Circle, unnatural, f.Dead), "native unnatural corpse type rejected");
        Set(f.Corpse, "mapIndexOrState", (sbyte)-1);
        Check.Equal(SpiritRebirthRitualRejection.CorpseWrongCustody, f.Rejection(), "unowned off-map corpse rejected");
        Check.Equal(true, f.Caster.carryTracker.GetDirectlyHeldThings().TryAdd(f.Corpse, false), "native carry tracker takes corpse");
        Check.Equal(SpiritRebirthRitualRejection.None, f.Rejection(), "creator-carried exact corpse accepted");
        f.Caster.carryTracker.GetDirectlyHeldThings().Remove(f.Corpse); Set(f.Corpse, "mapIndexOrState", (sbyte)0);
        var comp = new CompClassProgression { parent = f.Dead }; f.Dead.AllComps.Add(comp);
        Set(f.Dead.health, "healthState", PawnHealthState.Mobile);
        Check.Equal(true, SpiritRebirthStateTests.Invoke(comp, "BeginSpiritRebirth"), "original explicit marker created before later death");
        Set(f.Dead.health, "healthState", PawnHealthState.Dead);
        Check.Equal(SpiritRebirthRitualRejection.CorpseAlreadyReborn, f.Rejection(), "existing marker rejects repeat conversion");
        Check.Same(f.Dead, f.Corpse.InnerPawn, "every preflight retains actual original");
        Check.Same(owner, f.Dead.holdingOwner, "native corpse custody never altered by eligibility");
        Check.Same(data, f.Magic.MagicData, "creator progression never replaced");
    }
}
