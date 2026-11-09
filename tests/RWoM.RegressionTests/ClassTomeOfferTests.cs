using System.Runtime.CompilerServices;
using System.Reflection;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static class ClassTomeOfferTests
{
    public sealed class Envelope : IExposable
    {
        public CompClassProgression progression = new();
        public void ExposeData() => progression.PostExposeData();
    }

    static (Pawn pawn, CompClassProgression comp) Enrolled(string id, InitiationStage stage, Vocation vocation = Vocation.Mage)
    {
        var pawn = GrimoireProgressionTests.PawnWithComps(true, false).pawn;
        var comp = new CompClassProgression { parent = pawn };
        pawn.AllComps.Add(comp);
        comp.BeginNewProgression(vocation);
        comp.State.TryAddFoundation(id, DisciplineDomain.Magic);
        var path = comp.State.Find(id);
        while (path.Stage < stage)
        {
            string trial = "offer-fixture." + path.Stage;
            path.RecordCompletedTrial(trial);
            Check.Equal(true, comp.State.TryAdvanceAfterTrial(id, path.Stage, trial), "fixture advances through recorded trial");
        }
        return (pawn, comp);
    }

    static bool GenerationCompleted(Quest quest, ClassTomeQuestContext context)
        => (bool)typeof(ClassTomeQuestOffer).GetMethod("GenerationCompleted", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { quest, context })!;
    static bool RegistrationAcquired(Quest quest)
        => (bool)typeof(ClassTomeQuestOffer).GetMethod("RegistrationAcquired", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { quest })!;
    static bool CanDiscardUnregisteredPawn(Pawn pawn)
        => (bool)typeof(ClassTomeQuestOffer).GetMethod("CanDiscardUnregisteredPawn", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { pawn })!;

    sealed class GameScope : IDisposable
    {
        readonly Game previous = Current.Game;
        public GameScope()
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            GrimoireProgressionTests.Set(Current.Game, "tickManager", new TickManager());
            GrimoireProgressionTests.Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            GrimoireProgressionTests.Set(Current.Game, "signalManager", new SignalManager());
            GrimoireProgressionTests.Set(Current.Game, "questManager", new QuestManager());
            GrimoireProgressionTests.Set(Current.Game, "maps", new List<Map>());
        }
        public void Dispose() => Current.Game = previous;
    }

    sealed class FailingAddedPart : QuestPart
    {
        public int calls;
        public override void PostQuestAdded() { calls++; throw new InvalidOperationException("tome-added-fixture"); }
    }

    static Slate SlateFor(ClassTomeQuestContext context)
    {
        var slate = new Slate();
        slate.Set("tomeContext", context);
        slate.Set("resolvedQuestName", "Tome offer fixture");
        slate.Set("resolvedQuestDescription", "Exactly one mastery tome after repelling the band.");
        return slate;
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassTomeOffer.NativeGenerationCompletionAndPartialFailure", f =>
        {
            using var game = new GameScope();
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var pawn = GrimoireProgressionTests.PawnWithComps(true, false).pawn;
            var attacker = GrimoireProgressionTests.PawnWithComps(false, false).pawn;
            var root = new QuestScriptDef { defName = "TomeOfferFixture", root = new QuestNode_ClassMasteryTome(), everAcceptableInSpace = true };
            var context = new ClassTomeQuestContext { initiator = pawn, profileId = "rwom.fire",
                reward = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)),
                mapParent = (MapParent)RuntimeHelpers.GetUninitializedObject(typeof(MapParent)), attackers = [attacker] };
            var quest = QuestGen.Generate(root, SlateFor(context));
            Check.Equal(true, GenerationCompleted(quest, context), "native generation attached coordinator and committed context");
            Check.Equal(QuestState.NotYetAccepted, quest.State, "generation does not accept");
            Check.Equal(false, attacker.Spawned, "generation does not spawn attacker");
            Check.Equal(0, Find.QuestManager.QuestsListForReading.Count, "generation does not register");
            Check.Equal(true, quest.PartsListForReading.OfType<QuestPart_ReservePawns>().Single().pawns.Contains(attacker), "native reservation owns pawn reference");
            Check.Equal(quest.InitiateSignal, quest.PartsListForReading.OfType<QuestPart_ClassTomeRaid>().Single().inSignalEnable, "native acceptance signal configures activation");
            Check.Equal("Tome offer fixture", quest.name, "resolved offer title retained");
            Check.Equal(true, quest.description.ToString().Contains("Exactly one mastery tome"), "guarantee retained in native description");
            Check.Equal(false, GenerationCompleted(quest, new ClassTomeQuestContext()), "non-null partial quest is rejected");
            Check.Equal(false, GenerationCompleted(new Quest(), context), "completed marker without coordinator is rejected");
            RuntimeLog.AssertClean();

            var failed = new ClassTomeQuestContext { attackers = [] };
            var partial = QuestGen.Generate(root, SlateFor(failed));
            // Native QuestNode.Run logs and swallows this precise deliberate error.
            var logs = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Check.Equal(1, logs.Count, "only deliberate missing encounter error is allowed");
            Check.Equal(true, logs[0].StartsWith("Exception running QuestNode_ClassMasteryTome:") && logs[0].Contains("Missing tome encounter."), "native swallowed root failure was observed");
            logs.Clear();
            Check.Equal(true, partial != null, "native Generate really returned a partial quest");
            Check.Equal(false, GenerationCompleted(partial, failed), "actual partial result cannot be registered");
            Check.Equal(false, QuestGen.Working, "native generator state released after failure");
        });
        yield return new("ClassTomeOffer.NativeRegistrationCallbackAcquiresOwnership", _ =>
        {
            using var game = new GameScope();
            var quest = Quest.MakeRaw();
            var part = new FailingAddedPart(); quest.AddPart(part);
            Check.Equal(false, RegistrationAcquired(quest), "generation alone retains caller rollback ownership");
            bool threw = false;
            try { Find.QuestManager.Add(quest); }
            catch (InvalidOperationException ex) when (ex.Message == "tome-added-fixture") { threw = true; }
            Check.Equal(true, threw, "native Add really reached throwing callback");
            Check.Equal(true, RegistrationAcquired(quest), "same guard used by request catch preserves acquired quest ownership");
            Check.Equal(1, Find.QuestManager.QuestsListForReading.Count(q => q == quest), "failed callback has exactly one registered quest");
            Check.Equal(1, part.calls, "no re-add or replay after callback failure");
        });
        yield return new("ClassTomeOffer.RollbackRespectsActualHolderTransfer", f =>
        {
            using var game = new GameScope();
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var pawn = GrimoireProgressionTests.PawnWithComps(false, false).pawn;
            Check.Equal(true, CanDiscardUnregisteredPawn(pawn), "unregistered loose generated pawn can be rolled back");
            var holder = new QuestPart_ClassTomeRaid();
            Check.Equal(true, holder.GetDirectlyHeldThings().TryAdd(pawn, false), "native ThingOwner accepts transferred pawn");
            Check.Same(holder, pawn.ParentHolder, "actual parent holder is visible");
            Check.Equal(false, CanDiscardUnregisteredPawn(pawn), "rollback cannot destroy external holder contents");
            Check.Equal(1, holder.GetDirectlyHeldThings().Count, "transferred identity remains owned");
            holder.GetDirectlyHeldThings().Remove(pawn);
            Check.Equal(true, CanDiscardUnregisteredPawn(pawn), "released loose pawn returns to rollback eligibility");
            Current.Game.Maps.Add((Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)));
            GrimoireProgressionTests.Set(pawn, "mapIndexOrState", (sbyte)0);
            Check.Equal(false, CanDiscardUnregisteredPawn(pawn), "spawned pawn cannot be rollback-deleted");
        });
        yield return new("ClassTomeOffer.SupportedAdeptEligibility", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var oldProfiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var oldClasses = TM_ClassUtility.CustomClasses;
            try
            {
                DefDatabase<ClassDisciplineDef>.Clear();
                TM_ClassUtility.CustomClasses = [];
                foreach (string id in new[] { "rwom.fire", "rwom.priest", "rwom.necromancer", "rwom.ice", "rwom.lightning", "rwom.arcanist", "rwom.paladin", "rwom.summoner", "rwom.druid", "rwom.bard", "rwom.warlock", "rwom.succubus", "rwom.geomancer", "rwom.technomancer", "rwom.bloodmage", "rwom.enchanter", "rwom.chronomancer", "rwom.brightmage", "rwom.shaman", "rwom.golemancer", "rwom.empath", "addon.test" })
                {
                    var book = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    book.defName = "OfferBook_" + id;
                    var profile = new ClassDisciplineDef { defName = "Offer_" + id, profileId = id,
                        domain = DisciplineDomain.Magic, masterBook = book };
                    DefDatabase<ClassDisciplineDef>.Add(profile);
                    foreach (var stage in new[] { InitiationStage.Foundation, InitiationStage.Adept, InitiationStage.Master, InitiationStage.Ascended })
                    {
                        var p = Enrolled(id, stage);
                        int revision = p.comp.State.Revision;
                        Check.Equal(stage == InitiationStage.Adept && id != "addon.test", ClassTomeQuestOffer.Candidate(p.pawn) == profile, "only supported Adept gets matching tome: " + id + "/" + stage);
                        Check.Equal(revision, p.comp.State.Revision, "checking offer never advances progression");
                    }
                    var eligible = Enrolled(id, InitiationStage.Adept);
                    profile.masterBook = null!;
                    Check.Equal(true, ClassTomeQuestOffer.Candidate(eligible.pawn) == null, "missing tome cannot create offer");
                    profile.masterBook = book;
                }
                Check.Equal(true, ClassTomeQuestOffer.Candidate(GrimoireProgressionTests.PawnWithComps(true, false).pawn) == null, "classic pawn remains classic");
                Check.Equal(true, ClassTomeQuestOffer.Candidate(null) == null, "absent pawn has no offer");
                var hybrid = Enrolled("rwom.fire", InitiationStage.Adept, Vocation.WarriorMage);
                Check.Equal(true, ClassTomeQuestOffer.Candidate(hybrid.pawn) == null, "unaudited hybrid excluded");
                var multiple = Enrolled("rwom.fire", InitiationStage.Adept);
                multiple.comp.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
                Check.Equal(true, ClassTomeQuestOffer.Candidate(multiple.pawn) == null, "multiple paths excluded");
                var legacy = Enrolled("rwom.fire", InitiationStage.Adept);
                legacy.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.InnerFire));
                Check.Equal(true, ClassTomeQuestOffer.Candidate(legacy.pawn) == null, "mixed legacy class excluded");
                var addonTrait = new TraitDef { defName = "OfferAddon", degreeDatas = [new TraitDegreeData { degree = 0 }] };
                TM_ClassUtility.CustomClasses = [new TM_CustomClass { classTrait = addonTrait, isMage = true }];
                var addon = Enrolled("rwom.priest", InitiationStage.Adept);
                addon.pawn.story.traits.allTraits.Add(new Trait(addonTrait));
                Check.Equal(true, ClassTomeQuestOffer.Candidate(addon.pawn) == null, "addon trait prevents offer even with supported state");
            }
            finally
            {
                TM_ClassUtility.CustomClasses = oldClasses;
                DefDatabase<ClassDisciplineDef>.Clear();
                foreach (var profile in oldProfiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            }
        });
        yield return new("ClassTomeOffer.BudgetNeverRaisesThreat", _ =>
        {
            foreach (float input in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f })
                Check.Equal(0f, ClassTomeQuestOffer.Budget(input), "invalid threat cannot buy attackers");
            Check.Equal(true, Math.Abs(30f - ClassTomeQuestOffer.Budget(50f)) < 0.0001f, "low threat has no forced floor");
            Check.Equal(true, Math.Abs(60f - ClassTomeQuestOffer.Budget(100f)) < 0.0001f, "conservative strength factor");
            foreach (float input in new[] { 0.1f, 40f, 500f, 2000f, float.MaxValue })
            {
                float value = ClassTomeQuestOffer.Budget(input);
                Check.Equal(true, !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1000f && value <= input, "finite bounded budget");
            }
        });
        yield return new("ClassTomeOffer.CooldownNativeSavePreservesProgression", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var p = Enrolled("rwom.fire", InitiationStage.Adept);
            p.comp.State.TryChooseNoviceSpell("TM_Extinguish");
            p.comp.State.Find("rwom.fire").RecordCompletedTrial("retained-trial");
            int revision = p.comp.State.Revision;
            p.comp.DelayNextTomeQuest(1234);
            p.comp.DelayNextTomeQuest(100);
            Check.Equal(61234, p.comp.TomeQuestAvailableAtTick, "older terminal notice cannot shorten cooldown");
            Check.Equal(revision, p.comp.State.Revision, "cooldown does not mutate class progression");
            var data = new Envelope { progression = p.comp };
            string path = Path.Combine(f.WorkingDirectory, "tome-offer-cooldown.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref data, "pawnData"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            Envelope loaded = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "pawnData"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Check.Equal(61234, loaded.progression.TomeQuestAvailableAtTick, "cooldown survives native save/load");
            Check.Equal(InitiationStage.Adept, loaded.progression.State.Find("rwom.fire").Stage, "no stage reset");
            Check.Equal("TM_Extinguish", loaded.progression.State.NoviceSpells.Single(), "novice choice retained");
            Check.Equal(true, XDocument.Load(path).Descendants("li").Any(e => e.Value == "retained-trial"), "completed trial serialized alongside cooldown");
            RuntimeLog.AssertClean();
        });
        yield return new("ClassTomeOffer.DefinitionRequiresConsentAndExpires", f =>
        {
            var def = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/QuestScriptDefs/TM_ClassMasteryTome.xml")).Root!.Element("QuestScriptDef")!;
            Check.Equal("TM_ClassMasteryTome", def.Element("defName")!.Value, "explicit quest root");
            Check.Equal("TorannMagic.ClassProgression.QuestNode_ClassMasteryTome", (string?)def.Element("root")!.Attribute("Class"), "bounded dedicated root");
            Check.Equal(false, bool.Parse(def.Element("randomlySelectable")!.Value), "storyteller cannot request this quest");
            Check.Equal(false, bool.Parse(def.Element("autoAccept")!.Value), "offer requires native acceptance");
            Check.Equal("5", def.Element("expireDaysRange")!.Value, "unaccepted offer releases lock after five days");
            Check.Equal(false, bool.Parse(def.Element("endOnColonyMove")!.Value), "coordinator owns exact-map and pending-reward policy");
        });
    }
}



