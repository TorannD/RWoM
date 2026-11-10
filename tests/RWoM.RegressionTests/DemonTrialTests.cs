using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class DemonTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("DemonTrials.SharedSummonRequiresTwoReciprocalOwners", RitualPair);
        yield return new("DemonTrials.RankedRepulsionUsesExactProjectileAndDisjointRings", RankedRepulsion);
        yield return new("DemonTrials.ReciprocalNewBondAndOneShot", Bond);
        yield return new("DemonTrials.RejectOccupiedBranchBeforeChangingCurrentBond", Occupied);
        yield return new("DemonTrials.RefreshKeepsEffectInstancesAndRejectsForeignBond", Refresh);
        yield return new("DemonTrials.OwnershipStageAndTargetGuards", Guards);
        yield return new("DemonTrials.AttractionActualSeverityAndSavedSource", Attraction);
        yield return new("DemonTrials.RepulsionActualFlyingLaunch", Repulsion);
        yield return new("DemonTrials.NativeEffectsRemainSingleCalls", Hooks);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game old = Current.Game;
        readonly HediffDef oldSlow = TorannMagicDefOf.TM_GravitySlowHD;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        public Pawn Caster, Target; public CompAbilityUserMagic Magic; public DisciplineProgress Path; public Verb_BLOS Verb;
        public readonly string Profile;
        public Fixture(RuntimeFixture f, bool warlock, bool mastery = false)
        {
            LoadBaseDefinitions(f); TorannMagicDefOf.TM_GravitySlowHD = new HediffDef { defName = "DemonTrialSlow" }; Profile = warlock ? "rwom.warlock" : "rwom.succubus";
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var ally = new Faction { def = new FactionDef { isPlayer = true } }; var enemy = new Faction { def = new FactionDef() };
            Set(ally, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
            Set(enemy, "relations", new List<FactionRelation> { new() { other = ally, kind = FactionRelationKind.Hostile } });
            int id = 35000;
            foreach (var p in new[] { Caster, Target }) { p.thingIDNumber = id++; p.guest = new Pawn_GuestTracker(p); p.mindState = new Verse.AI.Pawn_MindState(p); p.kindDef = new PawnKindDef { defName = "DemonTrialPawn" }; Set(p, "factionInt", ally); Set(p, "mapIndexOrState", (sbyte)0); }
            if (mastery) Set(Target, "factionInt", enemy);
            var progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(progression); progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation(Profile, DisciplineDomain.Magic); Path = progression.State.Find(Profile);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "DemonTrialProfile", profileId = Profile, domain = DisciplineDomain.Magic, magicPowerList = warlock ? "magicPowerWD" : "magicPowerSD" });
            if (mastery) Set(Path, "stage", InitiationStage.Adept);
            var spell = mastery ? warlock ? TorannMagicDefOf.TM_Repulsion : TorannMagicDefOf.TM_Attraction : TorannMagicDefOf.TM_SoulBond;
            var owner = (warlock ? Magic.MagicData.MagicPowersWD : Magic.MagicData.MagicPowersSD).Single(p => p.TMabilityDefs.Contains(spell)); owner.learned = true;
            var ability = new PawnAbility(Magic); Set(ability, "powerDef", spell); Magic.AbilityData.Powers.Add(ability);
            Verb = new Verb_BLOS { Ability = ability }; Set(Verb, "caster", Caster);
        }
        public void Pair()
        {
            string casterDef = Profile == "rwom.warlock" ? "TM_WDSoulBondMentalHD" : "TM_SDSoulBondPhysicalHD", hostDef = Profile == "rwom.warlock" ? "TM_SoulBondMentalHD" : "TM_SoulBondPhysicalHD";
            Caster.health.hediffSet.hediffs.Add(new Hediff { pawn = Caster, def = new HediffDef { defName = casterDef } });
            var host = new HediffWithComps { pawn = Target, def = new HediffDef { defName = hostDef } };
            var hostComp = (HediffComp)Activator.CreateInstance(typeof(MagicAbility).Assembly.GetType("TorannMagic.HediffComp_SoulBondHost")!)!;
            hostComp.parent = host; Set(hostComp, "bonderPawn", Caster); host.comps = new List<HediffComp> { hostComp }; Target.health.hediffSet.hediffs.Add(host); Magic.soulBondPawn = Target;
        }
        public TestProjectile Shot(bool owned = true)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = Profile == "rwom.warlock" ? "Projectile_Repulsion" : "Projectile_Attraction";
            var shot = new TestProjectile { def = def, localAbilityDef = Verb.Ability.Def }; shot.Seed(Caster);
            if (!owned) Magic.AbilityData.Powers.Clear();
            var prior = DemonTrials.BeginLaunch(Verb, def); try { DemonTrials.Launched(shot, Caster, Verb.Ability.Def); } finally { DemonTrials.EndLaunch(prior); }
            return shot;
        }
        public void Dispose() { DemonTrials.EndLaunch(null!); TorannMagicDefOf.TM_GravitySlowHD = oldSlow; Current.Game = old; DefDatabase<ClassDisciplineDef>.Clear(); foreach (var p in profiles) DefDatabase<ClassDisciplineDef>.Add(p); }
    }
    public sealed class TestProjectile : Projectile_AbilityBase { public void Seed(Pawn p) => launcher = p; }
    public sealed class SourceEnvelope : IExposable { public Projectile_AbilityBase shot = null!; public void ExposeData() => DemonTrials.ExposeProjectile(shot); }
    static void Refresh(RuntimeFixture f)
    {
        foreach (bool warlock in new[] { true, false })
        {
            using var w = new Fixture(f,warlock);
            w.Pair();
            var recipient = w.Target.health.hediffSet.hediffs.Last();
            var host = ((HediffWithComps)recipient).comps.Single();
            var old = w.Caster.health.hediffSet.hediffs.Last();
            var source = new HediffWithComps { def = old.def, pawn = w.Caster };
            source.def.maxSeverity = recipient.def.maxSeverity = 4f;
            w.Caster.health.hediffSet.hediffs.Remove(old);
            w.Caster.health.hediffSet.hediffs.Add(source);
            var emitter = (HediffComp)Activator.CreateInstance(typeof(MagicAbility).Assembly.GetType("TorannMagic.HediffComp_SoulBond")!)!;
            source.comps = new List<HediffComp>{emitter}; emitter.parent = source;
            Set(emitter,"initializing",false);
            Set(Find.TickManager,"ticksGameInt",600);
            var skills = w.Magic.MagicData.MagicPowerSkill_SoulBond;
            skills.Single(s=>s.label=="TM_SoulBond_pwr").level=2;
            skills.Single(s=>s.label=="TM_SoulBond_ver").level=3;
            float adjustment=0;
            emitter.CompPostTick(ref adjustment);
            Check.Same(recipient,w.Target.health.hediffSet.hediffs.Last(),"partner instance survives rank refresh");
            Check.Same(source,w.Caster.health.hediffSet.hediffs.Last(),"source instance survives rank refresh");
            Check.Equal(3.5f,recipient.Severity,"recipient uses versatility");
            Check.Equal(2.5f,source.Severity,"source uses power");
            var other=PawnWithComps(false,false).pawn;
            // The other caster has a source marker of the same kind, but its partner is not this pawn.
            other.GetCompAbilityUserMagic().soulBondPawn = w.Caster;
            other.health.hediffSet.hediffs.Add(new Hediff { pawn=other, def=source.def });
            Set(host,"initialized",true);
            Set(host,"bonderPawn",other);
            host.CompPostTick(ref adjustment);
            Check.Equal(true,host.CompShouldRemove,"recipient cannot survive on another partner's source marker");
            emitter.CompPostTick(ref adjustment);
            Check.Equal(true,emitter.CompShouldRemove,"stale source releases its own effect");
            Check.Same(other,host.GetType().GetField("bonderPawn")!.GetValue(host)!,"refresh cannot steal another caster's link");
        }
    }

    static void Occupied(RuntimeFixture f)
    {
        foreach (bool warlock in new[] { true, false })
        {
            using var w = new Fixture(f, warlock);
            w.Pair();
            var originalCaster = w.Caster.health.hediffSet.hediffs.Last();
            var originalHost = w.Target.health.hediffSet.hediffs.Last();
            var other = PawnWithComps(false, false).pawn;
            var occupied = new Hediff { pawn = other, def = originalHost.def };
            other.health.hediffSet.hediffs.Add(occupied);
            var allows = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Verb_SoulBond")!.GetMethod("CanReplace", BindingFlags.Static | BindingFlags.NonPublic);
            Check.Equal(true, allows != null, "native bond checks the new recipient before old effects are removed");
            bool Allowed(Pawn target) => (bool)allows!.Invoke(null, [w.Magic, target, !warlock, warlock])!;
            Check.Equal(false, Allowed(other), "reject an occupied recipient of this branch");
            Check.Equal(true, Allowed(w.Target), "casting on the same partner is a release");
            Check.Same(w.Target, w.Magic.soulBondPawn, "prior partner is retained");
            Check.Same(originalCaster, w.Caster.health.hediffSet.hediffs.Last(), "prior caster effect retained");
            Check.Same(originalHost, w.Target.health.hediffSet.hediffs.Last(), "prior partner effect retained");
            Check.Same(occupied, other.health.hediffSet.hediffs.Last(), "foreign effect retained");
            other.health.hediffSet.hediffs.Remove(occupied);
            Check.Equal(true, Allowed(other), "a free recipient can replace the old partner");
            other.health.hediffSet.hediffs.Add(new Hediff { pawn = other, def = new HediffDef { defName = warlock ? "TM_SoulBondPhysicalHD" : "TM_SoulBondMentalHD" } });
            Check.Equal(true, Allowed(other), "the other branch does not conflict");
        }
    }
    static void Bond(RuntimeFixture f)
    {
        foreach (bool warlock in new[] { true, false }) {
            using var w = new Fixture(f, warlock);
            Check.Equal(false, DemonTrials.AfterBond(DemonTrials.BeforeBond(w.Verb, w.Target)), "no native effect excluded");
            var snapshot = DemonTrials.BeforeBond(w.Verb, w.Target); Check.Equal(true, snapshot != null, "owned foundation eligible");
            w.Pair(); Check.Equal(true, DemonTrials.AfterBond(snapshot), "actual reciprocal selected family qualifies"); Check.Equal(false, DemonTrials.AfterBond(snapshot), "snapshot one shot");
            Check.Equal(1, w.Path.CompletedTrials.Count, "one persistent trial"); Set(w.Path, "completedTrials", new List<string>()); Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Target) == null, "prior reciprocal bond excluded"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "no automatic advance");
        }
    }
    static void Guards(RuntimeFixture f)
    {
        using var w = new Fixture(f, true);
        Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Caster) == null, "self excluded");
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Target) == null, "unfree excluded"); } w.Target.guest.guestStatusInt = GuestStatus.Guest;
        w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless)); Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Target) == null, "copied excluded"); w.Caster.story.traits.allTraits.Clear();
        w.Magic.AbilityData.Powers.Clear(); Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Target) == null, "item excluded"); w.Magic.AbilityData.Powers.Add(w.Verb.Ability);
        w.Magic.MagicData.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SoulBond)).learned = false;
        w.Magic.MagicData.MagicPowersSD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SoulBond)).learned = true;
        Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Target) != null, "hidden genuine shared investment reconciled before source classification");
        w.Magic.MagicData.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SoulBond)).learned = false;
        w.Magic.MagicData.MagicPowersSD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SoulBond)).learned = false;
        Check.Equal(true, DemonTrials.BeforeBond(w.Verb, w.Target) == null, "unowned family excluded");
    }
    static void Attraction(RuntimeFixture f)
    {
        using var w = new Fixture(f, false, true); var shot = w.Shot();
        Check.Equal(true, DemonTrials.BeforeEffect(w.Shot(false), w.Target, false) == null, "item projectile excluded"); w.Magic.AbilityData.Powers.Add(w.Verb.Ability);
        w.Target.guest.guestStatusInt = GuestStatus.Prisoner; Check.Equal(true, DemonTrials.BeforeEffect(shot, w.Target, false) == null, "prisoner effect excluded"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        var slow = new Hediff { pawn = w.Target, def = TorannMagicDefOf.TM_GravitySlowHD }; Set(slow, "severityInt", .5f); w.Target.health.hediffSet.hediffs.Add(slow);
        Check.Equal(false, DemonTrials.AfterEffect(DemonTrials.BeforeEffect(shot, w.Target, false)), "visual/no severity excluded");
        Check.Equal(true, DemonTrials.BeforeEffect(shot, w.Target, true) == null, "wrong branch effect excluded");
        string file = System.IO.Path.Combine(f.WorkingDirectory, "demon-source.xml"); var envelope = new SourceEnvelope { shot = shot };
        try { Scribe.saver.InitSaving(file, "regression"); envelope.ExposeData(); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Check.Equal(true, System.IO.File.ReadAllText(file).Contains("TM_demonTrialSource"), "projectile source saved");
        try { Scribe.loader.InitLoading(file); envelope.ExposeData(); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        var snapshot = DemonTrials.BeforeEffect(shot, w.Target, false); Check.Equal(true, snapshot != null, "saved branch source valid"); Set(slow, "severityInt", .8f);
        Check.Equal(true, DemonTrials.AfterEffect(snapshot), "actual severity increase qualifies"); Check.Equal(false, DemonTrials.AfterEffect(snapshot), "one effect consumed once");
    }
    static void RitualPair(RuntimeFixture f)
    {
        using var w = new Fixture(f,true);
        var oldDefs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        var spell=TorannMagicDefOf.TM_SummonDemon;
        var oldType=spell.abilityClass;
        var refresh=typeof(TM_Action).Assembly.GetType("TorannMagic.ClassProgression.DemonRitual")!
            .GetMethod("RefreshControl",BindingFlags.Static|BindingFlags.NonPublic)!;
        try
        {
            DefDatabase<HediffDef>.Clear();
            spell.abilityClass=typeof(PawnAbility);
            var other=PawnWithComps(true,false);
            var state=new CompClassProgression {parent=other.pawn};other.pawn.AllComps.Add(state);
            state.BeginNewProgression(Vocation.Mage);state.State.TryAddFoundation("rwom.succubus",DisciplineDomain.Magic);
            void Link(string name,Pawn owner)
            {
                var def=new HediffDef {defName=name};
                DefDatabase<HediffDef>.Add(def);
                var effect=new HediffWithComps {def=def,pawn=w.Target};
                var comp=(HediffComp)Activator.CreateInstance(typeof(TM_Action).Assembly.GetType("TorannMagic.HediffComp_SoulBondHost")!)!;
                comp.parent=effect;Set(comp,"bonderPawn",owner);effect.comps=[comp];
                w.Target.health.hediffSet.hediffs.Add(effect);
            }
            w.Magic.soulBondPawn=w.Target;
            Link("TM_SoulBondMentalHD",w.Caster);
            refresh.Invoke(null,[w.Magic]);
            Check.Equal(false,w.Magic.spell_SummonDemon,"one bond cannot unlock the joint ritual");
            Link("TM_SoulBondPhysicalHD",other.pawn);
            refresh.Invoke(null,[w.Magic]);
            Check.Equal(false,w.Magic.spell_SummonDemon,"one-sided receiver cannot unlock the joint ritual");
            other.magic.soulBondPawn=w.Target;
            refresh.Invoke(null,[w.Magic]);refresh.Invoke(null,[other.magic]);
            var power=w.Magic.AbilityData.Powers.Single(a=>a.Def==spell);power.CooldownTicksLeft=89;
            refresh.Invoke(null,[w.Magic]);
            Check.Same(power,w.Magic.AbilityData.Powers.Single(a=>a.Def==spell),"rechecking preserves the runtime spell");
            Check.Equal(89,power.CooldownTicksLeft,"rechecking preserves the cooldown");
            Check.Equal(true,other.magic.spell_SummonDemon,"both actual owners gain the command");
            var debt=new HediffDef {defName="TM_DemonicPriceHD"};
            DefDatabase<HediffDef>.Add(debt);
            w.Target.health.hediffSet.hediffs.Add(new Hediff {def=debt,pawn=w.Target});
            refresh.Invoke(null,[w.Magic]);refresh.Invoke(null,[other.magic]);
            Check.Equal(false,w.Magic.AbilityData.Powers.Any(a=>a.Def==spell)||other.magic.AbilityData.Powers.Any(a=>a.Def==spell),"demonic price revokes both ritual commands");
        }
        finally
        {
            spell.abilityClass=oldType;
            DefDatabase<HediffDef>.Clear();
            foreach(var def in oldDefs)DefDatabase<HediffDef>.Add(def);
        }
    }

    static void RankedRepulsion(RuntimeFixture f)
    {
        using var w = new Fixture(f,true,true);
        var spell = TorannMagicDefOf.TM_Repulsion_III;
        Set(w.Verb.Ability,"powerDef",spell);
        TestProjectile Fire(string name)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name;
            var shot = new TestProjectile { def=def, localAbilityDef=spell };
            shot.Seed(w.Caster);
            var prior=DemonTrials.BeginLaunch(w.Verb,def);
            try { DemonTrials.Launched(shot,w.Caster,spell); }
            finally { DemonTrials.EndLaunch(prior); }
            return shot;
        }
        Check.Equal(true,DemonTrials.BeforeEffect(Fire("Projectile_Repulsion_III"),w.Target,true)!=null,"rank III displacement can earn mastery");
        Check.Equal(true,DemonTrials.BeforeEffect(Fire("Projectile_Repulsion_II"),w.Target,true)==null,"a mismatched projectile cannot earn mastery");
        var ring = typeof(Verb_SB).Assembly.GetType("TorannMagic.Projectile_Repulsion")!.GetMethod("Ring",BindingFlags.NonPublic|BindingFlags.Static)!;
        var seen=new HashSet<IntVec3>();
        var center=new IntVec3(23,0,17);
        for (int radius=1;radius<=10;radius++)
        {
            var cells=(List<IntVec3>)ring.Invoke(null,[center,radius])!;
            Check.Equal(true,cells.All(seen.Add),"successive rings never hit a prior cell twice");
            Check.Equal(true,seen.SetEquals(GenRadial.RadialCellsAround(center,radius,false)),"rings cover the native radius after reload");
        }
    }

    static void Repulsion(RuntimeFixture f)
    {
        using var w = new Fixture(f, true, true); var shot = w.Shot();
        Check.Equal(false, DemonTrials.AfterEffect(DemonTrials.BeforeEffect(shot, w.Target, true)), "attempted launch excluded");
        var snapshot = DemonTrials.BeforeEffect(shot, w.Target, true);
        var flying = (Thing)Activator.CreateInstance(typeof(MagicAbility).Assembly.GetType("TorannMagic.FlyingObject_Spinning")!)!;
        Set(flying, "mapIndexOrState", (sbyte)0); Set(flying, "flyingThing", w.Target); Set(w.Target, "mapIndexOrState", (sbyte)-1);
        Check.Equal(true, DemonTrials.AfterEffect(snapshot, flying), "actual flying object stores launched victim"); Check.Equal(false, DemonTrials.AfterEffect(snapshot, flying), "one launch consumed once");
    }
    static int Calls(MethodBase caller, MethodInfo target)
    {
        var processor = Assembly.Load("0Harmony").GetType("HarmonyLib.PatchProcessor")!;
        var read = processor.GetMethods().Single(m => m.Name == "GetOriginalInstructions" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        int count = 0;
        foreach (var instruction in (System.Collections.IEnumerable)read.Invoke(null, [caller, null])!)
            if (Equals(instruction.GetType().GetField("operand")!.GetValue(instruction), target)) count++;
        return count;
    }
    static void Hooks(RuntimeFixture f)
    {
        var assembly = typeof(DemonTrials).Assembly;
        var attraction = assembly.GetType("TorannMagic.Projectile_Attraction")!.GetMethod("TickField", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Check.Equal(3, Calls(attraction, typeof(DemonTrials).GetMethod("AdjustSlow")!), "all three native adjustment sites observed");
        var nativeAdjust = typeof(HealthUtility).GetMethod("AdjustSeverity")!;
        Check.Equal(1, Calls(typeof(DemonTrials).GetMethod("AdjustSlow")!, nativeAdjust), "wrapper calls native severity once");
        var repulsion = assembly.GetType("TorannMagic.Projectile_Repulsion")!.GetMethod("LaunchFlyingObect")!;
        Check.Equal(1, Calls(repulsion, typeof(DemonTrials).GetMethod("BeforeEffect")!), "one before launch capture");
        Check.Equal(1, Calls(repulsion, typeof(DemonTrials).GetMethod("AfterEffect")!), "one after launch observer");
        var launch = assembly.GetType("TorannMagic.FlyingObject_Spinning")!.GetMethod("Launch", new[] { typeof(Thing), typeof(LocalTargetInfo), typeof(Thing) })!;
        Check.Equal(1, Calls(repulsion, launch), "native flying launch remains once");
    }
}


