using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.Golems;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class GolemancerLifecycleTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GolemancerLifecycle.SourcePairsRepairControlsWithoutChangingEffects", Pairs);
        yield return new("GolemancerLifecycle.MalformedPairsAndTransferredSourcesAreRemovedTogether", Invalid);
        yield return new("GolemancerLifecycle.DespawnedWallAndClassicStatePreserved", Wall);
        yield return new("GolemancerLifecycle.NativeScribePreservesBrandsWallAndDormantGolem", Save);
        yield return new("GolemancerLifecycle.ExactNativeOwnershipAndMissingMasterOwner", Owners);
        yield return new("GolemancerLifecycle.WorkstationFramesMatchAssetCase", r =>
        {
            string textures = Path.Combine(r.Repository,"Textures");
            var files = Directory.EnumerateFiles(Path.Combine(textures,"Golems"),"*.png",SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(textures,f).Replace('\\','/')).ToHashSet(StringComparer.Ordinal);
            var defs = System.Xml.Linq.XDocument.Load(Path.Combine(r.Repository,"v1.7","Defs","GolemDefs","TM_GolemDef.xml"));
            foreach (var frame in defs.Descendants("golemFramePath"))
                Check.Equal(true,files.Contains(frame.Value+".png"),"exact frame texture: "+frame.Value);
        });
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly NeedDef[] needs = DefDatabase<NeedDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool init)> abilities = new();
        public readonly Pawn Caster, Target, GolemPawn;
        public readonly CompAbilityUserMagic Magic;
        public readonly HediffDef BrandDef;
        public readonly HediffWithComps Brand;
        public readonly CompGolem Golem;
        public readonly FlyingObject_LivingWall Wall;
        public readonly Thing Material;
        public readonly Need_GolemEnergy Energy;
        public Fixture(RuntimeFixture runtime, bool modern = true)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            LoadBaseDefinitions(runtime);
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map>());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic; Caster.thingIDNumber = 77201;
            Caster.ageTracker = new Pawn_AgeTracker(Caster); Set(Caster.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
            Caster.needs.AllNeeds.Add(new Need_Mana(Caster));
            Target = PawnWithComps(false, false).pawn; Target.thingIDNumber = 77202;
            GolemPawn = PawnWithComps(false, false).pawn; GolemPawn.thingIDNumber = 77203;
            if (modern)
            {
                var progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(progression);
                progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation("rwom.fire", DisciplineDomain.Magic);
            }
            BrandDef = new HediffDef { defName = "GolemLifecycleBrand", hediffClass = typeof(HediffWithComps),
                comps = [new HediffCompProperties { compClass = typeof(HediffComp_BrandingBase) }] };
            DefDatabase<HediffDef>.Add(BrandDef);
            Brand = (HediffWithComps)HediffMaker.MakeHediff(BrandDef, Target);
            Brand.TryGetComp<HediffComp_BrandingBase>().BranderPawn = Caster;
            Target.health.hediffSet.hediffs.Add(Brand);
            Golem = new CompGolem { parent = GolemPawn, pawnMaster = Caster, dormantPosition = new IntVec3(11, 0, 13),
                dormantRotation = Rot4.West, energyPctShouldRest = .27f, followsMaster = false, followsMasterDrafted = true };
            GolemPawn.AllComps.Add(Golem);
            Set(Golem, "golem", new TM_Golem { shardLevel = 3, upgrades = [new TM_GolemUpgrade { currentLevel = 7, enabled = false }] });
            Set(Golem, "innerContainer", new ThingOwner<Thing>(Golem));
            Wall = new FlyingObject_LivingWall { def = ThingDefinition("GolemLifecycleWall", typeof(FlyingObject_LivingWall)), thingIDNumber = 77204 };
            Material = new Thing { def = ThingDefinition("GolemLifecycleMaterial", typeof(Thing)), thingIDNumber = 77205, stackCount = 1 };
            Golem.GetDirectlyHeldThings().TryAdd(new Thing { def = Material.def, thingIDNumber = 77206, stackCount = 1 });
            var energyDef = new NeedDef { defName = "GolemLifecycleEnergy", needClass = typeof(Need_GolemEnergy) };
            DefDatabase<NeedDef>.Add(energyDef);
            Energy = new Need_GolemEnergy(GolemPawn) { def = energyDef, maxEnergy = 1200f, energyEfficiency = .73f };
            Energy.CurLevel = 328f;
            Set(Wall, "pawn", Caster); Set(Wall, "flyingThing", Material); Set(Wall, "ticksToImpact", 23);
            Set(Wall, "curvePoints", new List<UnityEngine.Vector3> { UnityEngine.Vector3.zero, UnityEngine.Vector3.one });
        }
        static ThingDef ThingDefinition(string name, Type type)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name; def.thingClass = type; def.category = ThingCategory.Item; def.stackLimit = 1;
            def.comps = new List<CompProperties>(); DefDatabase<ThingDef>.Add(def); return def;
        }
        public void AddPair() { Magic.BrandPawns.Add(Target); Magic.BrandDefs.Add(BrandDef); }
        public void Dispose()
        {
            Current.Game = previous; System.Globalization.CultureInfo.CurrentCulture = culture;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.init; }
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<ThingDef>.Clear(); foreach (var d in things) DefDatabase<ThingDef>.Add(d);
            DefDatabase<NeedDef>.Clear(); foreach (var d in needs) DefDatabase<NeedDef>.Add(d);
        }
    }
    static int Count(CompAbilityUserMagic c, TMAbilityDef d) => c.AbilityData.Powers.Count(p => p.Def == d);
    static void Pairs(RuntimeFixture r)
    {
        using var f = new Fixture(r); f.AddPair(); f.AddPair();
        f.Magic.sigilSurging = true; f.Magic.sigilDraining = true; f.Magic.Mana.CurLevel = .8f;
        Set(f.Magic, "dispelBrandings", true); f.Magic.ResolveSustainers();
        var button = f.Magic.AbilityData.Powers.Single(p => p.Def == TorannMagicDefOf.TM_DispelBranding); button.CooldownTicksLeft = 87;
        f.Magic.ResolveSustainers(); GolemancerLifecycle.Restore(f.Magic);
        Check.Equal(2, f.Magic.BrandPawns.Count, "valid duplicate native pairs are not reordered or deduplicated");
        Check.Same(f.Brand, f.Target.health.hediffSet.hediffs.Single(), "real effect retained");
        Check.Same(f.Caster, f.Brand.TryGetComp<HediffComp_BrandingBase>().BranderPawn, "source retained");
        Check.Equal(87, button.CooldownTicksLeft, "existing control cooldown retained");
        Check.Equal(1, Count(f.Magic, TorannMagicDefOf.TM_DispelBranding), "one repaired control");
        Check.Equal(true, f.Magic.sigilSurging && f.Magic.sigilDraining, "toggles retained");
        Check.Equal(.8f, f.Magic.Mana.CurLevel, "restore makes no payment");
    }
    static void Invalid(RuntimeFixture r)
    {
        using var f = new Fixture(r); f.AddPair();
        f.Magic.BrandPawns.Insert(0, null!); f.Magic.BrandDefs.Insert(0, f.BrandDef); f.Magic.BrandPawns.Add(f.Target);
        GolemancerLifecycle.Restore(f.Magic);
        Check.Same(f.Target, f.Magic.BrandPawns.Single(), "valid aligned pair survives malformed entries");
        Check.Same(f.BrandDef, f.Magic.BrandDefs.Single(), "paired definition retained");
        f.Brand.TryGetComp<HediffComp_BrandingBase>().BranderPawn = f.GolemPawn;
        GolemancerLifecycle.Restore(f.Magic);
        Check.Equal(0, f.Magic.BrandPawns.Count, "stale transferred pair removed");
        Check.Equal(0, f.Magic.BrandDefs.Count, "paired definition removed together");
        Check.Same(f.Brand, f.Target.health.hediffSet.hediffs.Single(), "foreign actual brand never removed");
        Check.Equal(0, Count(f.Magic, TorannMagicDefOf.TM_DispelBranding), "no foreign source control");
    }
    static void Wall(RuntimeFixture r)
    {
        using (var f = new Fixture(r))
        {
            f.Magic.livingWall = f.Wall; Set(f.Magic, "dispelLivingWall", true);
            f.Magic.ResolveSustainers(); f.Magic.ResolveSustainers();
            Check.Same(f.Wall, f.Magic.livingWall, "despawned owned wall retained");
            Check.Same(f.Material, typeof(FlyingObject_LivingWall).GetField("flyingThing", Fields)!.GetValue(f.Wall)!, "deep owned material retained");
            Check.Equal(1, Count(f.Magic, TorannMagicDefOf.TM_DispelLivingWall), "missing cached control repaired once");
            Set(f.Wall, "pawn", f.Target); GolemancerLifecycle.Restore(f.Magic);
            Check.Same(f.Wall, f.Magic.livingWall, "foreign reference not destroyed or replaced");
            Check.Equal(0, Count(f.Magic, TorannMagicDefOf.TM_DispelLivingWall), "foreign wall gets no control");
        }
        using (var f = new Fixture(r, false))
        { f.AddPair(); Check.Equal(false, GolemancerLifecycle.Restore(f.Magic), "classic state remains native"); Check.Equal(1, f.Magic.BrandPawns.Count, "classic data untouched"); }
    }
    public sealed class Envelope : IExposable
    {
        public static CompAbilityUserMagic Magic = null!;
        public static CompGolem Golem = null!;
        public static HediffComp_BrandingBase Source = null!;
        public static Need_GolemEnergy Energy = null!;
        public FlyingObject_LivingWall Wall = null!;
        public void ExposeData()
        {
            if (Scribe.mode != LoadSaveMode.PostLoadInit) { Scribe.EnterNode("magic"); Magic.PostExposeData(); Scribe.ExitNode(); }
            Scribe.EnterNode("source"); Source.CompExposeData(); Scribe.ExitNode();
            Scribe.EnterNode("golem"); Golem.PostExposeData(); Scribe.ExitNode();
            Scribe.EnterNode("energy"); Energy.ExposeData(); Scribe.ExitNode();
            Scribe_Deep.Look(ref Wall, "wall");
        }
    }
    static void Save(RuntimeFixture r)
    {
        using var f = new Fixture(r); f.AddPair(); f.Magic.livingWall = f.Wall; f.Magic.sigilDraining = true;
        Envelope.Magic = f.Magic; Envelope.Golem = f.Golem; Envelope.Source = f.Brand.TryGetComp<HediffComp_BrandingBase>();
        Envelope.Energy = f.Energy;
        var envelope = new Envelope { Wall = f.Wall }; string path = Path.Combine(r.WorkingDirectory, "golem-native-lifecycle.xml");
        try
        {
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "native"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            f.Magic.BrandPawns.Clear(); f.Magic.BrandDefs.Clear(); f.Magic.livingWall = null; f.Golem.pawnMaster = null; Envelope.Source.BranderPawn = null; envelope = null!;
            f.Energy.CurLevel = 0f;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "native");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", Fields)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(f.Caster); directory.RegisterLoaded(f.Target); directory.RegisterLoaded(f.GolemPawn);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            RuntimeLog.AssertClean();
            Check.Same(envelope.Wall, f.Magic.livingWall, "native comp reference resolves actual deep saved wall");
            var material = (Thing)typeof(FlyingObject_LivingWall).GetField("flyingThing", Fields)!.GetValue(envelope.Wall)!;
            Check.Equal(f.Material.thingIDNumber, material.thingIDNumber, "native deep wall material identity retained");
            Check.Same(f.Caster, envelope.Wall.CasterPawn, "native wall source reference retained");
            Check.Same(f.Target, f.Magic.BrandPawns.Single(), "native paired target retained");
            Check.Same(f.BrandDef, f.Magic.BrandDefs.Single(), "native paired def retained");
            Check.Same(f.Caster, Envelope.Source.BranderPawn, "native hediff source reference retained");
            Check.Same(f.Caster, f.Golem.pawnMaster, "native golem master bond retained");
            var state = f.Golem.Golem; var container = f.Golem.GetDirectlyHeldThings();
            GolemancerLifecycle.Restore(f.Magic); GolemancerLifecycle.Restore(f.Magic);
            Check.Same(state, f.Golem.Golem, "maintenance does not reconstruct golem"); Check.Same(container, f.Golem.GetDirectlyHeldThings(), "native dormant container retained");
            Check.Equal(3, state.shardLevel, "native saved shard level retained"); Check.Equal(7, state.upgrades.Single().currentLevel, "native upgrade retained");
            Check.Equal(false, state.upgrades.Single().enabled, "native upgrade toggle retained");
            Check.Equal(new IntVec3(11, 0, 13), f.Golem.dormantPosition, "native dormant position retained");
            Check.Equal(.27f, f.Golem.energyPctShouldRest, "native energy threshold retained");
            Check.Equal(1, container.Count, "native dormant container deep item retained");
            Check.Equal(77206, container[0].thingIDNumber, "native container item identity retained");
            Check.Equal(328f, f.Energy.CurLevel, "native saved energy not refilled or spent");
            Check.Equal(1200f, f.Energy.maxEnergy, "native saved energy capacity retained");
            Check.Equal(.73f, f.Energy.energyEfficiency, "native saved efficiency retained");
            Check.Equal(true, f.Magic.sigilDraining, "native sigil toggle retained");
            Check.Equal(1, Count(f.Magic, TorannMagicDefOf.TM_DispelBranding), "loaded brand control repaired");
            Check.Equal(1, Count(f.Magic, TorannMagicDefOf.TM_DispelLivingWall), "loaded wall control repaired");
        }
        finally { Envelope.Magic = null!; Envelope.Golem = null!; Envelope.Source = null!; Envelope.Energy = null!; }
    }
    static void Owners(RuntimeFixture r)
    {
        using var f = new Fixture(r);
        var owner = f.Magic.MagicData.MagicPowersGolemancer.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Golemancy));
        f.Magic.MagicData.MagicPowersStandalone.Add(new MagicPower([TorannMagicDefOf.TM_Golemancy]) { learned = true });
        Check.Equal(true, GolemancerLifecycle.GetLearnedNativeOwner(f.Magic, TorannMagicDefOf.TM_Golemancy) == null, "unlearned native owner cannot bind");
        f.Golem.UpdateGolemancerStatus(); Check.Same(f.Caster, f.Golem.pawnMaster, "unlearned living master retains existing bond");
        owner.learned = true;
        var skills = f.Magic.MagicData.MagicPowerSkill_Golemancy;
        var nativePower = skills.Single(s => s.label == "TM_Golemancy_pwr"); nativePower.level = 4;
        f.Magic.MagicData.AllMagicPowerSkills.Add(new MagicPowerSkill("TM_Golemancy_pwr", "addon_overlap") { level = 1 });
        Check.Same(nativePower, f.Magic.MagicData.GetSkill_Power(TorannMagicDefOf.TM_Golemancy), "native skill wins overlapping addon label");
        Check.Same(owner, GolemancerLifecycle.GetLearnedNativeOwner(f.Magic, TorannMagicDefOf.TM_Golemancy), "exact inherited native owner authorized without profile");
        Check.Equal(true, GolemancerLifecycle.GetLearnedNativeOwner(f.Magic, TorannMagicDefOf.TM_RuneCarving) == null, "golemancy does not imply rune carving");
        GolemancerLifecycle.Restore(f.Magic);
        Check.Equal(0, Count(f.Magic, TorannMagicDefOf.TM_DispelLivingWall), "learned passive creates no wall");
        Check.Equal(0, Count(f.Magic, TorannMagicDefOf.TM_DispelBranding), "learned passive creates no brand");
    }
}
