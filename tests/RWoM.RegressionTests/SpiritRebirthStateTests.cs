using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthStateTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritProjectedCast.BloodGiftUsesOriginalInstanceAndSuppressesDuplicate", r =>
        {
            using var f = new Fixture(r);
            var prior = TorannMagicDefOf.TM_BloodGift;
            try
            {
                var def = TorannMagicDefOf.TM_BloodGift = r.AddAbility("SdkSpiritGift");
                var ability = new MagicAbility { Def = def, AbilityUser = f.Magic, CooldownTicksLeft = 120 };
                f.Magic.AbilityData.Powers.Add(ability); f.Magic.AbilityData.AllPowers = null;
                var state = f.Begin(); var possession = f.Hold(f.Host, f.Original);
                Check.Equal(true, Invoke(state,"TryEnterHost", f.Original, f.Host), "reciprocal host");
                var type = typeof(SpiritRebirthCasting).Assembly.GetType("TorannMagic.ClassProgression.SpiritBloodSacrifice")!;
                var find = type.GetMethod("Find", BindingFlags.Static | BindingFlags.NonPublic)!;
                object? FindOwner() => find.Invoke(null, [f.Host, f.Original]);
                Check.Same(ability, FindOwner()!, "uses original cooldown owner");
                var hostMagic = f.Host.GetCompAbilityUserMagic();
                var local = new MagicAbility { Def = def, AbilityUser = hostMagic };
                hostMagic.AbilityData.Powers.Add(local); hostMagic.AbilityData.AllPowers = null;
                Check.Equal(true, FindOwner() == null, "host already has same spell");
                hostMagic.AbilityData.Powers.Remove(local); hostMagic.AbilityData.AllPowers = null;
                Check.Same(ability, FindOwner()!, "own ability removal reveals original");
                possession.GetDirectlyHeldThings().Remove(f.Original);
                Check.Equal(true, FindOwner() == null, "stale job cannot pay or cast");
                Check.Equal(120, ability.CooldownTicksLeft, "query and rejected replay preserve cooldown");
            }
            finally { TorannMagicDefOf.TM_BloodGift = prior; }
        });
        yield return new("SpiritProjectedCast.OwnerMustStillBeHeldByExactBody", r =>
        {
            using var f = new Fixture(r);
            var ability = new MagicAbility { AbilityUser = f.Magic };
            var read = typeof(SpiritRebirthCasting).GetMethod("SkillOwner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            Pawn? Read(Pawn body) => (Pawn?)read.Invoke(null, [body, ability]);
            Check.Same(f.Original, Read(f.Original)!, "ordinary spell uses actual owner");
            Check.Equal(true, Read(f.Host) == null, "unpossessed pawn cannot borrow");
            var state = f.Begin();
            var possession = f.Hold(f.Host, f.Original);
            Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "reciprocal valid possession");
            Check.Same(f.Original, Read(f.Host)!, "ranks come from soul");
            Check.Equal(true, Read(f.Other) == null, "foreign body rejected");
            possession.GetDirectlyHeldThings().Remove(f.Original);
            Check.Equal(true, Read(f.Host) == null, "stale delayed cast cannot use old body");
        });
        yield return new("SpiritRebirthState.ExplicitMarkerPreservesOriginalData", Identity);
        yield return new("SpiritRebirthState.ExactNativeHostCustody", Custody);
        yield return new("SpiritRebirthState.NativeScribeReferencesAndLegacyNull", Save);
        yield return new("SpiritRebirthState.UnsupportedStateNeverAuthorizesHost", Invalid);
        yield return new("SpiritRebirthState.StoneRecoveryLivingTicksAndDeathReset", Recovery);
        yield return new("SpiritRebirthState.RecoveryScribeAndFoundationMigration", RecoverySave);
        yield return new("SpiritRebirthState.MalformedRecoverySaveCannotUnlock", MalformedRecoverySave);
    }

    internal static bool Invoke(object target, string name, params object[] args)
        => (bool)target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args)!;

    internal sealed class Fixture : IDisposable
    {
        readonly HediffDef oldPossession = TorannMagicDefOf.TM_SpiritPossessionHD;
        readonly Game previousGame = Current.Game;
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        public readonly Pawn Original, Host, Other;
        public readonly CompClassProgression Comp;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompAbilityUserMight Might;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            if (DefDatabase<DesignationDef>.DefCount == 0) DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "SpiritRebirthTestDesignation" });
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.spawnedThings = new ThingOwner<Thing>(map);
            map.designationManager = new DesignationManager(map);
            Set(Current.Game, "maps", new List<Map> { map });
            var original = PawnWithComps(true, true);
            Original = original.pawn; Magic = original.magic; Might = original.might;
            Original.def.stackLimit = 1;
            Host = PawnWithComps(true, true).pawn; Other = PawnWithComps(false, false).pawn;
            Original.thingIDNumber = 67101; Host.thingIDNumber = 67102; Other.thingIDNumber = 67103;
            Comp = new CompClassProgression { parent = Original }; Original.AllComps.Add(Comp);
            TorannMagicDefOf.TM_SpiritPossessionHD = new HediffDef { defName = "SpiritRebirthTestPossession" };
        }
        public SpiritRebirthState Begin()
        {
            Check.Equal(true, Invoke(Comp, "BeginSpiritRebirth"), "explicit transaction creates marker once");
            return Comp.SpiritRebirth;
        }
        public void PlaceForCustody(Pawn pawn)
        {
            // Structural placement fixture: actual 1.7 map holder, not a full SpawnSetup.
            Check.Equal(true, Current.Game.Maps[0].spawnedThings.TryAdd(pawn, false), "native map owner accepts original");
            Set(pawn, "mapIndexOrState", (sbyte)0);
        }
        public void UnplaceForCustody(Pawn pawn)
        {
            Current.Game.Maps[0].spawnedThings.Remove(pawn);
            Set(pawn, "mapIndexOrState", (sbyte)-1);
        }
        public HediffComp_SpiritPossession Hold(Pawn host, Pawn original)
        {
            var hediff = new HediffWithComps { def = TorannMagicDefOf.TM_SpiritPossessionHD, pawn = host };
            var possession = new HediffComp_SpiritPossession { parent = hediff };
            hediff.comps = [possession]; host.health.hediffSet.hediffs.Add(hediff);
            var holder = new ThingOwner<Thing>(possession);
            Set(possession, "innerContainer", holder);
            Check.Equal(true, holder.TryAdd(original, false), "native holder accepts original pawn");
            return possession;
        }
        public void Dispose()
        {
            TorannMagicDefOf.TM_SpiritPossessionHD = oldPossession; Current.Game = previousGame;
            DefDatabase<DesignationDef>.Clear(); foreach (var def in designations) DefDatabase<DesignationDef>.Add(def);
        }
    }

    static void Identity(RuntimeFixture r)
    {
        using var f = new Fixture(r);
        f.Comp.BeginNewProgression(Vocation.WarriorMage);
        f.Comp.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
        var progression = f.Comp.State; var magic = f.Magic.MagicData; var might = f.Might.MightData;
        var power = magic.MagicPowersN[0]; power.learned = true; power.level = 1;
        magic.MagicAbilityPoints = 17; magic.MagicUserXP = 123;
        var skill = new SkillRecord(f.Original, new SkillDef { defName = "SpiritIdentitySkill" }) { passion = Passion.Major, levelInt = 12 };
        f.Original.skills = new Pawn_SkillTracker(f.Original); f.Original.skills.skills = [skill];
        var traits = f.Original.story.traits; var name = f.Original.Name; var race = f.Original.def; string id = f.Original.GetUniqueLoadID();
        var state = f.Begin();
        Check.Equal(false, Invoke(f.Comp, "BeginSpiritRebirth"), "cannot reset existing marker");
        Check.Equal(true, SpiritRebirthState.TryGet(f.Original, out var found), "original resolves explicit marker");
        Check.Same(state, found, "same marker"); Check.Equal(SpiritRebirthForm.Ethereal, found.Form, "initial form");
        Check.Same(race, f.Original.def, "race untouched"); Check.Equal(id, f.Original.GetUniqueLoadID(), "native load ID untouched");
        Check.Same(name, f.Original.Name, "name identity untouched"); Check.Same(traits, f.Original.story.traits, "trait owner untouched");
        Check.Same(skill, f.Original.skills.skills.Single(), "ordinary skill owner untouched"); Check.Equal(Passion.Major, skill.passion, "passion untouched"); Check.Equal(12, skill.levelInt, "ordinary level untouched");
        Check.Same(progression, f.Comp.State, "progression owner untouched"); Check.Same(magic, f.Magic.MagicData, "magic data untouched"); Check.Same(might, f.Might.MightData, "might data untouched");
        Check.Same(power, magic.MagicPowersN[0], "class power owner untouched"); Check.Equal(true, power.learned, "learned retained"); Check.Equal(1, power.level, "rank retained");
        Check.Equal(17, magic.MagicAbilityPoints, "points retained"); Check.Equal(123, magic.MagicUserXP, "XP retained");
        Check.Equal(false, state.HasValidHost(f.Original), "free form cannot authorize a host");
    }

    static void Custody(RuntimeFixture r)
    {
        using var f = new Fixture(r); var state = f.Begin();
        Check.Equal(false, Invoke(state, "TryEnterHost", f.Original, f.Host), "host field alone cannot authorize entry");
        var possession = f.Hold(f.Host, f.Original);
        Check.Equal(false, SpiritRebirthState.TryGetPossessingSpirit(f.Host, out _), "container alone cannot authorize new possession");
        Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "actual custody permits explicit entry");
        Check.Equal(true, state.HasValidHost(f.Original), "reciprocal state and holder agree");
        Check.Equal(true, SpiritRebirthState.TryGetPossessingSpirit(f.Host, out var original), "reverse lookup validated"); Check.Same(f.Original, original, "actual original resolved");
        Check.Equal(false, Invoke(state, "TryEnterHost", f.Original, f.Other), "cannot rebind active host");
        Check.Equal(false, Invoke(state, "TryLeaveHost", f.Original, f.Host), "cannot leave while still held");
        var holder = possession.GetDirectlyHeldThings(); holder.Remove(f.Original);
        Check.Equal(false, state.HasValidHost(f.Original), "stale saved host loses authority immediately");
        Check.Equal(false, SpiritRebirthState.TryGetPossessingSpirit(f.Host, out _), "empty actual host rejected");
        Check.Equal(false, Invoke(state, "TryLeaveHost", f.Original, f.Host), "unplaced original cannot complete release");
        f.PlaceForCustody(f.Original);
        Check.Equal(false, Invoke(state, "TryLeaveHost", f.Original, f.Other), "foreign release rejected");
        Check.Equal(true, Invoke(state, "TryLeaveHost", f.Original, f.Host), "placed original explicitly returns to ethereal");
        Check.Equal(SpiritRebirthForm.Ethereal, state.Form, "release form"); Check.Equal(true, state.Host == null, "release clears host only");
        f.UnplaceForCustody(f.Original);
        holder.TryAdd(f.Original, false); Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "repeated entry uses actual custody");
        possession.parent.pawn = f.Other;
        Check.Equal(false, state.HasValidHost(f.Original), "foreign hediff parent rejected");
        possession.parent.pawn = f.Host;
        holder.Remove(f.Original);
        var ownerless = new ThingOwner<Thing>(); ownerless.TryAdd(f.Original, false); Set(possession, "innerContainer", ownerless);
        Check.Equal(false, state.HasValidHost(f.Original), "legacy ownerless container cannot authorize new possession");
    }

    public sealed class Envelope(Pawn original) : IExposable
    {
        public CompClassProgression comp = new() { parent = original };
        public void ExposeData() => comp.PostExposeData();
    }

    static Envelope RoundTrip(RuntimeFixture r, Fixture f, string name, Action<XDocument>? edit = null)
    {
        string path = Path.Combine(r.WorkingDirectory, name + ".xml");
        var data = new Envelope(f.Original) { comp = f.Comp };
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        if (edit != null) { var xml = XDocument.Load(path); edit(xml); xml.Save(path); }
        Envelope loaded = null!;
        try
        {
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "data", f.Original);
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(f.Original); directory.RegisterLoaded(f.Host);
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        RuntimeLog.AssertClean(); return loaded;
    }

    static void Save(RuntimeFixture r)
    {
        using (var legacy = new Fixture(r))
        {
            var loaded = RoundTrip(r, legacy, "spirit-legacy-null");
            Check.Equal(true, loaded.comp.SpiritRebirth == null, "old absent state remains absent");
            Check.Equal(true, loaded.comp.UsesClassicProgression, "old progression unchanged");
            Check.Equal(false, XDocument.Load(Path.Combine(r.WorkingDirectory, "spirit-legacy-null.xml")).Descendants("TM_spiritRebirth").Any(), "no marker written for old pawn");
            legacy.Hold(legacy.Host, legacy.Original);
            Check.Equal(false, SpiritRebirthState.TryGetPossessingSpirit(legacy.Host, out _), "legacy native possession remains outside new system");
        }
        foreach (bool possessed in new[] { false, true })
        {
            using var f = new Fixture(r); var state = f.Begin(); var magic = f.Magic.MagicData; var might = f.Might.MightData;
            if (possessed) { f.Hold(f.Host, f.Original); Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "entry before save"); }
            var loaded = RoundTrip(r, f, "spirit-state-" + possessed);
            f.Original.AllComps.Remove(f.Comp); f.Original.AllComps.Add(loaded.comp);
            Check.Equal(true, SpiritRebirthState.TryGet(f.Original, out var restored), "native original pawn cross reference resolves");
            Check.Equal(SpiritRebirthState.CurrentVersion, restored.Version, "version survives");
            Check.Equal(possessed ? SpiritRebirthForm.Possessed : SpiritRebirthForm.Ethereal, restored.Form, "form survives");
            Check.Equal(possessed, restored.HasValidHost(f.Original), "loaded record still requires actual holder");
            if (possessed) Check.Same(f.Host, restored.Host, "native host pawn cross reference resolves");
            Check.Same(magic, f.Magic.MagicData, "state save never copies magic data"); Check.Same(might, f.Might.MightData, "state save never copies might data");
            var xml = XDocument.Load(Path.Combine(r.WorkingDirectory, "spirit-state-" + possessed + ".xml"));
            Check.Equal(false, xml.Descendants("magicData").Any() || xml.Descendants("mightData").Any(), "state serialization owns no class snapshot");
        }
    }

    static SpiritRebirthState Recovering(Fixture f)
    {
        var state = f.Begin(); var possession = f.Hold(f.Host, f.Original);
        Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "ordinary ethereal possession");
        Check.Equal(false, state.RecoveryRequired, "ordinary possession has no stone recovery debt");
        Check.Equal(true, state.CanVoluntarilyLeaveHost(f.Original), "ordinary possession can leave");
        var stone = new Thing_SoulStone();
        Check.Equal(true, Invoke(stone, "TryCapture", f.Original, f.Host, false), "native host transfers actual spirit to stone");
        Check.Equal(true, state.HasValidStone(f.Original), "stone custody is reciprocal");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 50), "stone never recovers");
        Check.Equal(true, Invoke(stone, "TryImplant", f.Original, possession), "actual spirit reimplants");
        Check.Equal(true, state.RecoveryRequired, "stone assigns full week debt");
        return state;
    }

    static void Recovery(RuntimeFixture r)
    {
        using var f = new Fixture(r); var state = Recovering(f);
        Check.Equal(false, state.CanVoluntarilyLeaveHost(f.Original), "recovery blocks voluntary departure");
        var originalHolder = f.Original.holdingOwner; originalHolder.Remove(f.Original);
        f.PlaceForCustody(f.Original);
        Check.Equal(false, Invoke(state, "TryLeaveHost", f.Original, f.Host), "placing spirit cannot bypass recovery release gate");
        f.UnplaceForCustody(f.Original); originalHolder.TryAdd(f.Original, false);
        Invoke(state, "TickRecovery", f.Original, f.Host, 10);
        Invoke(state, "TickRecovery", f.Original, f.Host, 11);
        Check.Equal(1, state.RecoveryTicks, "partial progress exists before depletion");
        var depleted = new Thing_SoulStone();
        Check.Equal(true, Invoke(depleted, "TryCapture", f.Original, f.Host, false), "forced depletion can safely collapse during recovery lock");
        Check.Equal(0, state.RecoveryTicks, "forced depletion starts full week again");
        var possession = f.Host.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD).TryGetComp<HediffComp_SpiritPossession>();
        Check.Equal(true, Invoke(depleted, "TryImplant", f.Original, possession), "forced collapse retains original for implantation");
        Check.Equal(false, state.CanVoluntarilyLeaveHost(f.Original), "forced collapse never removes voluntary departure lock");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 100), "first tick establishes baseline");
        Check.Equal(true, Invoke(state, "TickRecovery", f.Original, f.Host, 101), "actual next living tick recovers");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 101), "duplicate tick does not recover");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 9000), "offline gap grants nothing");
        Check.Equal(1, state.RecoveryTicks, "only observed adjacent tick credited");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Other, 9001), "foreign host cannot advance");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 9002), "invalid observation breaks continuity");
        Set(f.Host.health, "healthState", PawnHealthState.Dead);
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 9003), "dead host cannot recover");
        var stone = new Thing_SoulStone();
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Host, false), "dead host rejected on voluntary path");
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Other, true), "wrong death source rejected");
        Check.Equal(true, Invoke(stone, "TryCapture", f.Original, f.Host, true), "death bypasses lock preserving native spirit");
        Check.Equal(0, state.RecoveryTicks, "host death resets accumulated progress");
        var next = f.Hold(f.Other, f.Host); next.GetDirectlyHeldThings().Remove(f.Host);
        Check.Equal(true, Invoke(stone, "TryImplant", f.Original, next), "same soul implants in next host");
        Invoke(state, "TickRecovery", f.Original, f.Other, 10000);
        var tick = typeof(SpiritRebirthState).GetMethod("TickRecovery", BindingFlags.Instance | BindingFlags.NonPublic)!;
        for (int i = 1; i < SpiritRebirthState.RecoveryDurationTicks; i++) tick.Invoke(state, [f.Original, f.Other, 10000 + i]);
        Check.Equal(false, state.CanVoluntarilyLeaveHost(f.Original), "one tick short remains locked");
        Invoke(state, "TickRecovery", f.Original, f.Other, 10000 + SpiritRebirthState.RecoveryDurationTicks);
        Check.Equal(true, state.CanVoluntarilyLeaveHost(f.Original), "full seven days unlocks departure");
        Invoke(state, "TickRecovery", f.Original, f.Other, 10001 + SpiritRebirthState.RecoveryDurationTicks);
        Check.Equal(SpiritRebirthState.RecoveryDurationTicks, state.RecoveryTicks, "recovery saturates");
        next.GetDirectlyHeldThings().Remove(f.Original); f.PlaceForCustody(f.Original);
        Check.Equal(true, Invoke(state, "TryLeaveHost", f.Original, f.Other), "actual placed spirit can depart after week");
    }

    static void RecoverySave(RuntimeFixture r)
    {
        using var f = new Fixture(r); var state = Recovering(f);
        Invoke(state, "TickRecovery", f.Original, f.Host, 100); Invoke(state, "TickRecovery", f.Original, f.Host, 101);
        var loaded = RoundTrip(r, f, "spirit-recovery");
        f.Original.AllComps.Remove(f.Comp); f.Original.AllComps.Add(loaded.comp);
        state = loaded.comp.SpiritRebirth;
        Check.Equal(1, state.RecoveryTicks, "save retains partial progress");
        Check.Equal(true, state.RecoveryRequired, "save retains recovery debt");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 101), "saved same tick cannot credit twice");
        Check.Equal(true, Invoke(state, "TickRecovery", f.Original, f.Host, 102), "reload preserves adjacent baseline");
        Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 1000000), "load jump grants no offline credit");
        Check.Equal(2, state.RecoveryTicks, "load never hard resets progress");
        var migrated = RoundTrip(r, f, "spirit-foundation-version", xml =>
        {
            var marker = xml.Descendants("TM_spiritRebirth").Single(); marker.Element("version")!.Value = "1";
            marker.Element("recoveryRequired")?.Remove(); marker.Element("recoveryTicks")?.Remove(); marker.Element("lastRecoveryTick")?.Remove();
        });
        Check.Equal(SpiritRebirthState.CurrentVersion, migrated.comp.SpiritRebirth.Version, "foundation version migrates");
        Check.Equal(false, migrated.comp.SpiritRebirth.RecoveryRequired, "foundation never acquires stone debt");
        Check.Equal(0, migrated.comp.SpiritRebirth.RecoveryTicks, "foundation has no fabricated recovery progress");
    }

    static void MalformedRecoverySave(RuntimeFixture r)
    {
        using var f = new Fixture(r); Recovering(f);
        foreach (int malformed in new[] { -1, SpiritRebirthState.RecoveryDurationTicks + 1 })
        {
            var loaded = RoundTrip(r, f, "spirit-recovery-malformed-" + malformed, xml =>
            {
                var marker = xml.Descendants("TM_spiritRebirth").Single();
                marker.SetElementValue("recoveryTicks", malformed);
                marker.SetElementValue("lastRecoveryTick", -5);
            });
            var current = f.Original.AllComps.OfType<CompClassProgression>().Single();
            f.Original.AllComps.Remove(current); f.Original.AllComps.Add(loaded.comp);
            var state = loaded.comp.SpiritRebirth;
            Check.Equal(0, state.RecoveryTicks, "malformed progress grants no recovered time");
            Check.Equal(false, state.CanVoluntarilyLeaveHost(f.Original), "malformed progress cannot unlock release");
            Check.Equal(false, Invoke(state, "TickRecovery", f.Original, f.Host, 50), "malformed baseline restarts observation");
            Check.Equal(true, Invoke(state, "TickRecovery", f.Original, f.Host, 51), "repaired recovery can make actual progress");
        }
    }

    static void Invalid(RuntimeFixture r)
    {
        using var f = new Fixture(r); var state = f.Begin(); f.Hold(f.Host, f.Original); Invoke(state, "TryEnterHost", f.Original, f.Host);
        foreach (int version in new[] { 0, SpiritRebirthState.CurrentVersion + 1 })
        {
            Set(state, "version", version); Check.Equal(false, SpiritRebirthState.TryGet(f.Original, out _), "unsupported version denied");
            Check.Equal(false, SpiritRebirthState.TryGetPossessingSpirit(f.Host, out _), "unknown version cannot borrow host authority");
        }
        Set(state, "version", SpiritRebirthState.CurrentVersion); Set(state, "form", SpiritRebirthForm.Dormant);
        Check.Equal(false, state.HasValidHost(f.Original), "reserved Dormant form has no host authority");
        Set(state, "form", (SpiritRebirthForm)99); Check.Equal(false, SpiritRebirthState.TryGet(f.Original, out _), "invalid form rejected");
        Set(state, "form", SpiritRebirthForm.Possessed); Set(state, "original", f.Other);
        Check.Equal(false, SpiritRebirthState.TryGet(f.Original, out _), "marker copied to different original is rejected");
        Check.Equal(false, state.HasValidHost(f.Original), "foreign original loses host authority");
    }
}
