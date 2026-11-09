using System.Runtime.CompilerServices;
using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class BardTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BardTrials.ActualNewEntertainmentMemoryOnce", Entertainment);
        yield return new("BardTrials.RejectsForeignOwnersAndRecipients", Ownership);
        yield return new("BardTrials.InspireRequiresActualNewEffect", Inspire);
        yield return new("BardTrials.NativeObserversPreserveSingleMutation", Hooks);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public DisciplineProgress Path;
        public Hediff Entertainment, Aura;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TrialBard", profileId = "rwom.bard", domain = DisciplineDomain.Magic });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic;
            Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.guest = new Pawn_GuestTracker(pawn);
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.kindDef = new PawnKindDef { defName = "TrialPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
                pawn.needs.mood = new Need_Mood(pawn);
            }
            var comp = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(comp);
            comp.BeginNewProgression(Vocation.Mage); comp.State.TryAddFoundation("rwom.bard", DisciplineDomain.Magic);
            Path = comp.State.Find("rwom.bard");
            foreach (var power in Magic.MagicData.MagicPowersB) power.learned = true;
            Entertainment = new Hediff { pawn = Caster, def = new HediffDef { defName = "TM_EntertainingHD" } };
            Aura = new Hediff { pawn = Caster, def = new HediffDef { defName = "TM_InspirationalHD" } };
            Caster.health.hediffSet.hediffs.Add(Entertainment); Caster.health.hediffSet.hediffs.Add(Aura);
        }
        public Thought_Memory Memory(Pawn? owner = null)
        {
            var memory = new Thought_MemorySocial { def = new ThoughtDef { defName = "TM_EntertainedTD" }, pawn = Target, otherPawn = owner ?? Caster };
            Target.needs.mood.thoughts.memories.Memories.Add(memory); return memory;
        }
        public void Dispose()
        {
            Current.Game = previous;
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
        }
    }
    static void Entertainment(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var unchanged = BardTrials.CaptureEntertainment(w.Caster, w.Target);
        Check.Equal(false, BardTrials.CompleteEntertainment(unchanged, true), "successful no-effect interaction excluded");
        var old = w.Memory(); old.age = 30;
        var preexisting = BardTrials.CaptureEntertainment(w.Caster, w.Target);
        Check.Equal(false, BardTrials.CompleteEntertainment(preexisting, true), "preexisting memory excluded");
        var failed = BardTrials.CaptureEntertainment(w.Caster, w.Target); w.Memory();
        Check.Equal(false, BardTrials.CompleteEntertainment(failed, false), "failed interaction excluded");
        var wrongSender = BardTrials.CaptureEntertainment(w.Caster, w.Target); w.Memory(w.Target);
        Check.Equal(false, BardTrials.CompleteEntertainment(wrongSender, true), "another sender cannot credit caster");
        var fresh = BardTrials.CaptureEntertainment(w.Caster, w.Target); w.Memory();
        Check.Equal(true, BardTrials.CompleteEntertainment(fresh, true), "new real recipient memory credits");
        Check.Equal(false, BardTrials.CompleteEntertainment(fresh, true), "snapshot consumed once");
        Check.Equal(1, w.Path.CompletedTrials.Count, "single persisted completion");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "no stage advance");
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
        var assembly = typeof(BardTrials).Assembly;
        var aura = assembly.GetType("TorannMagic.HediffComp_Inspirational")!.GetMethod("CompPostTick")!;
        Check.Equal(4, Calls(aura, typeof(BardTrials).GetMethod("Inspire")!), "all four native versatility branches observed");
        var driver = assembly.GetType("TorannMagic.JobDriver_Entertain")!;
        var callbacks = new[] { driver }.Concat(driver.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)).SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)).Where(m => m.GetMethodBody() != null);
        Check.Equal(1, callbacks.Sum(m => Calls(m, typeof(BardTrials).GetMethod("Entertain")!)), "one native job interaction site observed");
        var tryInspire = typeof(InspirationHandler).GetMethods().Single(m => m.Name == "TryStartInspiration");
        Check.Equal(1, Calls(typeof(BardTrials).GetMethod("Inspire")!, tryInspire), "one original native inspiration mutation");
        var tryEntertain = typeof(Pawn_InteractionsTracker).GetMethods().Single(m => m.Name == "TryInteractWith");
        Check.Equal(1, Calls(typeof(BardTrials).GetMethod("Entertain")!, tryEntertain), "one original native interaction mutation");
    }
    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Caster)!, "self excluded");
        w.Target.guest.guestStatusInt = GuestStatus.Prisoner;
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "prisoners excluded"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "mapIndexOrState", (sbyte)1);
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "cross-map excluded"); Set(w.Target, "mapIndexOrState", (sbyte)0);
        w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless));
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "copied owner excluded"); w.Caster.story.traits.allTraits.Clear();
        var training = w.Magic.MagicData.MagicPowersB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BardTraining));
        training.learned = false;
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "native training owner required"); training.learned = true;
        var faction = w.Target.Faction; Set(w.Target, "factionInt", new Faction { def = new FactionDef() });
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "foreign faction excluded"); Set(w.Target, "factionInt", faction);
        w.Target.guest.guestStatusInt = GuestStatus.Slave;
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "slaves excluded"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        w.Target.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Undead));
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "undead excluded"); w.Target.story.traits.allTraits.Clear();
        var owner = w.Magic.MagicData.MagicPowersB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Entertain)); owner.learned = false;
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "native unlearned owner excluded"); owner.learned = true;
        var lost = BardTrials.CaptureEntertainment(w.Caster, w.Target); w.Memory(); owner.learned = false;
        Check.Equal(false, BardTrials.CompleteEntertainment(lost, true), "ownership lost during interaction excluded");
        owner.learned = true; w.Caster.health.hediffSet.hediffs.Remove(w.Entertainment);
        Check.Same(null!, BardTrials.CaptureEntertainment(w.Caster, w.Target)!, "active upkeep required");
    }
    static void Inspire(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Same(null!, BardTrials.CaptureInspiration(w.Aura, w.Target)!, "wrong stage excluded");
        Set(w.Path, "stage", InitiationStage.Adept);
        var noEffect = BardTrials.CaptureInspiration(w.Aura, w.Target);
        Check.Equal(true, noEffect != null, "eligible native aura capture");
        Check.Equal(false, BardTrials.CompleteInspiration(noEffect), "no actual new inspiration excluded");
        var fresh = BardTrials.CaptureInspiration(w.Aura, w.Target);
        var handler = w.Target.mindState.inspirationHandler;
        var inspirationType = typeof(Inspiration).Assembly.GetTypes().First(t => typeof(Inspiration).IsAssignableFrom(t) && !t.IsAbstract);
        var inspiration = (Inspiration)RuntimeHelpers.GetUninitializedObject(inspirationType);
        var field = handler.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single(fi => fi.FieldType == typeof(Inspiration));
        field.SetValue(handler, inspiration);
        Check.Equal(true, BardTrials.CompleteInspiration(fresh), "actual newly started inspiration credits");
        Check.Equal(false, BardTrials.CompleteInspiration(fresh), "snapshot cannot repeat");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "no automatic stage change");
        Set(w.Path, "completedTrials", new List<string>());
        Check.Same(null!, BardTrials.CaptureInspiration(w.Aura, w.Target)!, "preexisting inspiration excluded");
        field.SetValue(handler, null);
        var lost = BardTrials.CaptureInspiration(w.Aura, w.Target);
        w.Caster.health.hediffSet.hediffs.Remove(w.Aura); field.SetValue(handler, inspiration);
        Check.Equal(false, BardTrials.CompleteInspiration(lost), "lost source excludes credit");
    }
}






