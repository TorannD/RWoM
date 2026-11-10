using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ClassTomeQuestOffer
    {
        private static readonly HashSet<Pawn> generating = new HashSet<Pawn>();
        public static ClassDisciplineDef Candidate(Pawn pawn) => Candidates(pawn).FirstOrDefault();

        internal static IEnumerable<ClassDisciplineDef> Candidates(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || state.Vocation != Vocation.Mage
                || (state.Disciplines.Count != 1 && !state.ElementalPair) || ClassInitiation.HasLegacyClass(pawn)) yield break;
            foreach (var path in state.Disciplines)
            {
                if (path.Stage != InitiationStage.Adept || path.Domain != DisciplineDomain.Magic
                    || (path.ClassId != "rwom.chaos" && path.ClassId != "rwom.fire" && path.ClassId != "rwom.ice" && path.ClassId != "rwom.lightning" && path.ClassId != "rwom.paladin" && path.ClassId != "rwom.summoner" && path.ClassId != "rwom.arcanist" && path.ClassId != "rwom.priest" && path.ClassId != "rwom.necromancer" && path.ClassId != "rwom.technomancer" && path.ClassId != "rwom.shaman" && path.ClassId != "rwom.golemancer" && path.ClassId != "rwom.empath" && path.ClassId != "rwom.brightmage" && path.ClassId != "rwom.chronomancer" && path.ClassId != "rwom.enchanter" && path.ClassId != "rwom.bloodmage" && path.ClassId != "rwom.geomancer" && path.ClassId != "rwom.druid" && path.ClassId != "rwom.bard" && path.ClassId != "rwom.warlock" && path.ClassId != "rwom.succubus")) continue;
                var profile = ClassIdentity.Profile(path.ClassId);
                if (profile?.domain == DisciplineDomain.Magic && profile.masterBook != null) yield return profile;
            }
        }

        public static IEnumerable<QuestPart_ClassTomeRaid> Requests()
            => Find.QuestManager.ActiveQuestsListForReading.Where(q => !q.Historical)
                .SelectMany(q => q.PartsListForReading.OfType<QuestPart_ClassTomeRaid>());
        public static QuestPart_ClassTomeRaid Existing(Pawn pawn)
            => Requests().FirstOrDefault(p => p.Initiator == pawn && p.IsOutstanding);

        public static float Budget(float threat)
            => float.IsNaN(threat) || float.IsInfinity(threat) || threat <= 0 ? 0 : Math.Min(1000f, threat * 0.6f);

        public static string UnavailableReason(Pawn pawn)
        {
            if (pawn?.Spawned != true || pawn.Dead || pawn.Downed || !pawn.IsColonistPlayerControlled
                || pawn.Map?.IsPlayerHome != true || (pawn.Map.Tile.Valid && pawn.Map.Tile.LayerDef.isSpace)
                || Candidate(pawn) == null) return "TM_TomeQuestUnavailable";
            if (!Find.Storyteller.difficulty.allowViolentQuests) return "TM_TomeQuestPeaceful";
            if (ClassIdentity.Component(pawn).TomeQuestAvailableAtTick > Find.TickManager.TicksGame) return "TM_TomeQuestCooldown";
            if (Existing(pawn) != null || Requests().Any(p => p.TargetMapParent == pawn.Map.Parent && p.BlocksMap)) return "TM_TomeQuestBusy";
            return null;
        }

        // Request creates only a quest offer. Native Accept is the sole raid activation.
        public static Quest Request(Pawn pawn) => RequestFor(pawn, Candidate(pawn)?.profileId);

        internal static Quest RequestFor(Pawn pawn, string id) => RequestEncounter(pawn,id,false);
        internal static Quest RequestEncounter(Pawn pawn, string id, bool rift)
        {
            if (pawn == null || !generating.Add(pawn)) return null;
            var owned = new List<Pawn>();
            Quest quest = null;
            bool registered = false;
            try
            {
                var existing = Existing(pawn);
                if (existing != null) return existing.quest;
                if (UnavailableReason(pawn) != null || QuestGen.Working) return null;
                var root = DefDatabase<QuestScriptDef>.GetNamedSilentFail("TM_ClassMasteryTome");
                if (root == null) return null;
                var profile = Candidates(pawn).FirstOrDefault(p => p.profileId == id);
                if (profile == null) return null;
                var map = pawn.Map;
                float threat = StorytellerUtility.DefaultThreatPointsNow(map);
                float budget = rift ? TomeElementalRift.Budget(threat) : Budget(threat);
                var faction = rift ? TomeElementalRift.Faction() : Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f =>
                    !f.Hidden && !f.temporary && !f.defeated && f.def.humanlikeFaction && !f.def.raidsForbidden
                    && f.HostileTo(Faction.OfPlayer) && f.def.pawnGroupMakers != null
                    && f.def.pawnGroupMakers.Any(g => g.kindDef == PawnGroupKindDefOf.Combat)
                    && f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat) <= budget);
                var kinds = rift ? TomeElementalRift.Kinds(profile.profileId,budget) : new[] { "Tribal_Penitent", "Tribal_Archer", "Tribal_Warrior" }
                    .Select(n => DefDatabase<PawnKindDef>.GetNamedSilentFail(n))
                    .Where(k => k?.race?.race?.Humanlike == true && k.combatPower > 0 && k.combatPower <= budget).ToList();
                if (faction == null || kinds.Count == 0) return null;
                var arrival = new IncidentParms { target = map, faction = faction, points = budget };
                if (!PawnsArrivalModeDefOf.EdgeWalkIn.Worker.CanUseOnMap(map)
                    || !PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(arrival)) return null;
                float spent = 0;
                while (owned.Count < 12)
                {
                    var choices = kinds.Where(k => k.combatPower <= budget - spent).ToList();
                    if (choices.Count == 0) break;
                    var kind = choices.RandomElement();
                    var created = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction,
                        tile: map.Tile, forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                        mustBeCapableOfViolence: true, forceBaselinerChance: 1f, developmentalStages: DevelopmentalStage.Adult));
                    if (created == null) return null;
                    owned.Add(created); // Record ownership before callbacks or world registration.
                    if (created.Dead || created.Downed || created.DevelopmentalStage != DevelopmentalStage.Adult) return null;
                    if (rift)
                    {
                        if (created is not TMPawnSummoned elemental || created.Faction != faction) return null;
                        elemental.validSummoning = true; elemental.Temporary = false;
                    }
                    else if (!created.RaceProps.Humanlike
                        || created.GetStatValue(StatDefOf.Flammability) <= 0) return null;
                    if (!created.IsWorldPawn()) Find.WorldPawns.PassToWorld(created);
                    spent += kind.combatPower;
                }
                if (owned.Count == 0) return null;
                var context = new ClassTomeQuestContext { initiator = pawn, profileId = profile.profileId,
                    reward = profile.masterBook, mapParent = map.Parent, attackers = owned, rift = rift };
                var slate = new Slate();
                slate.Set("tomeContext", context);
                slate.Set("points", spent);
                slate.Set("resolvedQuestName", "TM_TomeQuestTitle".Translate(profile.masterBook.LabelCap).ToString());
                slate.Set("resolvedQuestDescription", (rift ? "TM_TomeRiftDescription" : "TM_TomeQuestDescription").Translate(
                    map.Parent.LabelCap, owned.Count, spent.ToString("0"), profile.masterBook.LabelCap).ToString());
                quest = QuestGen.Generate(root, slate);
                if (!GenerationCompleted(quest, context)) return null;
                Find.QuestManager.Add(quest);
                registered = RegistrationAcquired(quest);
                return registered ? quest : null;
            }
            catch (Exception ex)
            {
                registered = RegistrationAcquired(quest);
                Compatibility.TM_Log.Warning("Class tome quest generation failed: " + ex);
                return registered ? quest : null;
            }
            finally
            {
                try
                {
                    if (!registered)
                        foreach (var created in owned)
                            if (CanDiscardUnregisteredPawn(created))
                                try { Find.WorldPawns.RemoveAndDiscardPawnViaGC(created); }
                                catch (Exception ex) { Compatibility.TM_Log.Warning("Class tome pawn cleanup failed: " + ex); }
                }
                finally { generating.Remove(pawn); }
            }
        }

        // QuestNode.Run catches exceptions, so a returned Quest alone is not a successful generation.
        internal static bool GenerationCompleted(Quest quest, ClassTomeQuestContext context)
            => quest != null && context?.completed == true
                && quest.PartsListForReading.OfType<QuestPart_ClassTomeRaid>().Count() == 1;

        // Native Add takes ownership before invoking callbacks which may throw.
        internal static bool RegistrationAcquired(Quest quest)
            => quest != null && Find.QuestManager.Contains(quest);

        // A callback may already have transferred a generated pawn into another real holder.
        internal static bool CanDiscardUnregisteredPawn(Pawn pawn)
            => pawn != null && !pawn.Spawned && !pawn.Destroyed && !pawn.Discarded && pawn.ParentHolder == null;

        public static void Open(Quest quest)
        {
            if (quest == null) return;
            Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests);
            ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(quest);
        }
    }

    public sealed class ClassTomeQuestContext
    {
        public Pawn initiator;
        public string profileId;
        public ThingDef reward;
        public MapParent mapParent;
        public List<Pawn> attackers;
        public bool completed;
        public bool rift;
    }
    public sealed class QuestNode_ClassMasteryTome : QuestNode
    {
        protected override bool TestRunInt(Slate slate) => slate.Get<ClassTomeQuestContext>("tomeContext") != null;
        protected override void RunInt()
        {
            var c = QuestGen.slate.Get<ClassTomeQuestContext>("tomeContext");
            if (c?.attackers == null || c.attackers.Count == 0) throw new InvalidOperationException("Missing tome encounter.");
            var part = new QuestPart_ClassTomeRaid();
            part.Configure(c.initiator, c.profileId, c.reward, c.mapParent, c.attackers, QuestGen.quest.InitiateSignal);
            part.ElementalRift = c.rift;
            QuestGen.quest.AddPart(part);
            QuestGen.quest.ReservePawns(c.attackers);
            c.completed = true;
        }
    }
}

