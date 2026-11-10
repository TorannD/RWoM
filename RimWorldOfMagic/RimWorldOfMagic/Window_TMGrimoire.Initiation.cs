using System.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public partial class Window_TMGrimoire
    {
        private string InitiationRequirements()
        {
            if (!ClassIdentity.TryGetState(pawn, out var state)) return "TM_GrimoireClassicRequirements".Translate();
            string text = ClassIdentity.Stages(state);
            if (AdditionalMartialRegistry.Current(state, out var extraPath) is AdditionalMartialPath extra)
                text += "\n" + extra.Requirements(pawn, extraPath);
            var blade = state.Find(BladeCareer.Id);
            if (blade?.Stage == InitiationStage.Foundation)
            {
                text += "\n" + "TM_BladeServiceHint".Translate(TorannMagicDefOf.TM_BladeSpin.LabelCap);
                text += "\n" + "TM_BladeServiceProgress".Translate(BladeCareer.Passes(pawn), BladeCareer.Serviced(pawn));
            }
            else if (blade?.Stage == InitiationStage.Adept)
            {
                text += "\n" + "TM_BladeMasteryHint".Translate(TorannMagicDefOf.TM_SeismicSlash.LabelCap);
                text += "\n" + "TM_BladeMasteryProgress".Translate(BladeMastery.Replies(pawn), BladeMastery.Rescues(pawn));
            }
            else if (blade != null && blade.Stage >= InitiationStage.Master)
            {
                text += "\n" + "TM_BladeMasteryLearned".Translate(TorannMagicDefOf.TM_SeismicSlash.LabelCap);
                if (blade.Stage == InitiationStage.Master)
                {
                    text += "\n" + "TM_BladeAscensionProgress".Translate(pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel ?? 0,
                        BladeAscension.Routes(pawn), BladeAscension.Deliveries(pawn));
                    text += "\n" + "TM_BladeRouteHint".Translate();
                    text += "\n" + "TM_BladeSupplyHint".Translate();
                }
                else
                {
                    var ultimate = BladeAscension.HasForm(pawn) ? DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladeSevenSteps") : TorannMagicDefOf.TM_PhaseStrike;
                    if (ultimate != null) text += "\n" + "TM_BladeUltimateLearned".Translate(ultimate.LabelCap);
                }
            }
            var ranger = state.Find(RangerCareer.Id);
            if (ranger?.Stage == InitiationStage.Foundation)
                text += "\n" + "TM_RangerCareHint".Translate(RangerCareer.Days(pawn));
            else if (ranger?.Stage == InitiationStage.Adept)
            {
                text += "\n" + "TM_RangerMasteryHint".Translate(RangerMastery.Traps(pawn), RangerMastery.Clearances(pawn));
                text += "\n" + "TM_RangerFireHint".Translate();
            }
            else if (ranger?.Stage == InitiationStage.Master)
            {
                text += "\n" + "TM_RangerAscensionHint".Translate(pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel ?? 0,
                    RangerAscension.Retreats(pawn), RangerAscension.Cares(pawn));
                text += "\n" + "TM_RangerWoundHint".Translate();
            }
            else if (ranger?.Stage == InitiationStage.Ascended)
            {
                var ultimate = RangerAscension.HasHunt(pawn) ? DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerBreakthrough") : TorannMagicDefOf.TM_ArrowStorm;
                if (ultimate != null) text += "\n" + "TM_RangerEarnedUltimate".Translate(ultimate.LabelCap);
            }
            var profile = ClassIdentity.Profile("rwom.priest");
            foreach (var available in ClassInitiation.FoundationProfiles.Where(p => state.CanChooseFoundation(p.profileId)))
                    text += "\n" + "TM_ProgressionFoundationBook".Translate(ClassIdentity.DisciplineLabel(available.profileId), available.foundationBook.LabelCap);
            if (state.ElementalPair) text += "\n" + "TM_ElementalHybridAscension".Translate();
            if (state.NeedsLegacyElementChoice) text += "\n" + "TM_ImportedElementChoiceHint".Translate();
            foreach (var mastery in SpellMastery.Choices)
                if (SpellMastery.Eligible(pawn, mastery, out _) && state.Mastery(mastery.Spell) == mastery.Id)
                    text += "\n" + "TM_MasteryCurrent".Translate(mastery.Ability.LabelCap, ("TM_Mastery_" + mastery.Id).Translate());
            if (SpellMastery.Choices.Any(c => SpellMastery.Eligible(pawn, c, out _)))
                text += "\n" + "TM_MasteryHint".Translate();
            if (state.Find(JobDriver_GladiatorInitiation.ClassId)?.Ultimate == GladiatorUltimate.LastCircle)
                text += "\n" + "TM_LastCircleDetails".Translate();
            var sniper = state.Find(SniperFoundation.Id);
            if (sniper?.Stage == InitiationStage.Foundation)
                text += "\n" + "TM_SniperReconHint".Translate();
            else if (sniper?.Stage == InitiationStage.Adept)
            {
                int ticks = SniperMastery.ResearchTicks(pawn);
                text += "\n" + "TM_SniperMasteryHint".Translate(TorannMagicDefOf.TM_DisablingShot.LabelCap);
                text += "\n" + "TM_SniperMasteryHits".Translate(SniperMastery.Hits(pawn), SniperMastery.RequiredHits);
                text += "\n" + "TM_SniperMasteryResearch".Translate(ticks / SniperMastery.ShiftTicks, SniperMastery.RequiredShifts);
                if (ticks % SniperMastery.ShiftTicks > 0)
                    text += "\n" + "TM_SniperMasteryPartial".Translate(100 * (ticks % SniperMastery.ShiftTicks) / SniperMastery.ShiftTicks);
            }
            else if (sniper != null && sniper.Stage >= InitiationStage.Master)
            {
                text += "\n" + "TM_SniperMasteryLearned".Translate(TorannMagicDefOf.TM_DisablingShot.LabelCap);
                if (sniper.Stage == InitiationStage.Master)
                {
                    text += "\n" + "TM_SniperAscensionRequirements".Translate(
                        pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel ?? 0,
                        SniperAscension.Rescues(pawn), SniperAscension.Drills(pawn));
                    text += "\n" + "TM_SniperAscensionRescueHint".Translate();
                    text += "\n" + "TM_SniperAscensionDrillHint".Translate();
                }
                else if (sniper.Stage == InitiationStage.Ascended)
                {
                    var ability = SniperAscension.HasOpening(pawn) ? DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_SniperSingleOpening") : TorannMagicDefOf.TM_ShadowSlayer;
                    if (ability != null) text += "\n" + "TM_SniperAscensionLearned".Translate(ability.LabelCap);
                }
            }
            if (state.Find("rwom.wanderer") != null)
                text += "\n" + "TM_WandererSurveyInfo".Translate();
            var shadow = state.Find("rwom.shadow");
            if (shadow != null)
                text += "\n" + (shadow.Stage == InitiationStage.Foundation ? "TM_ShadowCrossingTrial" : "TM_ShadowCrossingReward").Translate();
            var chaos = state.Find("rwom.chaos");
            if (chaos != null)
            {
                var details = chaos.Chaos;
                text += "\n" + "TM_ChaosProgress".Translate(details?.Practiced ?? 0, details?.DiverseSets ?? 0);
                var profileChaos = ClassIdentity.Profile("rwom.chaos");
                var nextTome = chaos.Stage == InitiationStage.Foundation ? profileChaos?.adeptBook : chaos.Stage == InitiationStage.Adept ? profileChaos?.masterBook : null;
                if (nextTome != null) text += "\n" + "TM_ProgressionNextBook".Translate(nextTome.LabelCap);
                if (chaos.Stage == InitiationStage.Master) text += "\n" + "TM_ChaosAscensionInfo".Translate();
            }
            var path = state.Find("rwom.priest");
            if (path?.Stage == InitiationStage.Foundation && profile?.adeptBook != null)
            {
                text += "\n\n" + "TM_ProgressionPriestHealingTrial".Translate();
                text += "\n" + (path.CompletedTrials.Contains(profile.adeptTrialId) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                text += "\n" + "TM_ProgressionNextBook".Translate(profile.adeptBook.LabelCap);
            }
            if (path?.Stage == InitiationStage.Adept && profile?.masterBook != null)
            {
                text += "\n\n" + "TM_ProgressionPriestPurificationTrial".Translate();
                text += "\n" + (path.CompletedTrials.Contains(profile.masterTrialId) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                text += "\n" + "TM_ProgressionNextBook".Translate(profile.masterBook.LabelCap);
            }
            if (path?.Stage == InitiationStage.Master)
                text += "\n\n" + "TM_ProgressionAscensionRequirements".Translate();
            var necromancer = state.Find("rwom.necromancer");
            var necromancerProfile = ClassIdentity.Profile("rwom.necromancer");
            if (necromancer?.Stage == InitiationStage.Foundation && necromancerProfile?.adeptBook != null)
            {
                text += "\n\n" + "TM_ProgressionNecromancerControlTrial".Translate();
                text += "\n" + "TM_ProgressionServantTime".Translate(((necromancer.ServantControl?.ControlledTicks ?? 0) / 2500f).ToString("0.0"));
                text += "\n" + (necromancer.CompletedTrials.Contains(NecromancerControlTrial.TrialId)
                    ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                text += "\n" + "TM_ProgressionNextBook".Translate(necromancerProfile.adeptBook.LabelCap);
            }
            if (necromancer?.Stage == InitiationStage.Adept && necromancerProfile?.masterBook != null)
            {
                text += "\n\n" + "TM_ProgressionNecromancerDeathMarkTrial".Translate();
                text += "\n" + (necromancer.CompletedTrials.Contains(NecromancerDeathMarkTrial.TrialId)
                    ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                text += "\n" + "TM_ProgressionNextBook".Translate(necromancerProfile.masterBook.LabelCap);
            }
            if (necromancer?.Stage == InitiationStage.Master)
                text += "\n\n" + "TM_ProgressionNecromancerAscensionRequirements".Translate();
            var fire = state.Find("rwom.fire");
            var fireProfile = ClassIdentity.Profile("rwom.fire");
            if (fire?.Stage == InitiationStage.Foundation && fireProfile?.adeptBook != null)
            {
                text += "\n\n" + "TM_ProgressionFireboltTrial".Translate();
                text += "\n" + "TM_ProgressionFireboltDamage".Translate(fire.FireboltDamage.ToString("0.#"));
                text += "\n" + (fire.CompletedTrials.Contains(FireboltDamageTrial.TrialId) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                text += "\n" + "TM_ProgressionNextBook".Translate(fireProfile.adeptBook.LabelCap);
            }
            if (fire?.Stage == InitiationStage.Adept && fireProfile?.masterBook != null)
            {
                text += "\n\n" + "TM_ProgressionFireclawTrial".Translate();
                text += "\n" + (fire.CompletedTrials.Contains(FireclawTrial.TrialId) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                text += "\n" + "TM_ProgressionNextBook".Translate(fireProfile.masterBook.LabelCap);
            }
            if (fire?.Stage == InitiationStage.Master && state.Find("rwom.ice")?.Stage != InitiationStage.Ascended)
                text += "\n\n" + "TM_ProgressionFireAscensionRequirements".Translate();
            var ice = state.Find("rwom.ice");
            var iceProfile = ClassIdentity.Profile("rwom.ice");
            if (ice != null && (ice.Stage == InitiationStage.Foundation || ice.Stage == InitiationStage.Adept))
            {
                bool first = ice.Stage == InitiationStage.Foundation;
                string trial = first ? IceTrials.FoundationTrialId : IceTrials.MasteryTrialId;
                text += "\n\n" + (first ? "TM_ProgressionIceFoundationTrial" : "TM_ProgressionIceMasteryTrial").Translate();
                text += "\n" + "TM_ProgressionIceTrialProgress".Translate(ice.IceTrials.IceboltDamage.ToString("0.#"), ice.IceTrials.SnowballVictims.Count);
                text += "\n" + (ice.CompletedTrials.Contains(trial) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                var book = first ? iceProfile?.adeptBook : iceProfile?.masterBook;
                if (book != null) text += "\n" + "TM_ProgressionNextBook".Translate(book.LabelCap);
                text += "\n" + "TM_ProgressionIceAttunementHint".Translate();
            }
            if (ice?.Stage == InitiationStage.Master && state.Find("rwom.fire")?.Stage != InitiationStage.Ascended)
                text += "\n\n" + "TM_ProgressionIceAscensionRequirements".Translate();
            var lightning = state.Find("rwom.lightning");
            var lightningProfile = ClassIdentity.Profile("rwom.lightning");
            if (lightning != null && (lightning.Stage == InitiationStage.Foundation || lightning.Stage == InitiationStage.Adept))
            {
                bool first = lightning.Stage == InitiationStage.Foundation;
                string trial = first ? LightningTrials.FoundationTrialId : LightningTrials.MasteryTrialId;
                text += "\n\n" + (first ? "TM_ProgressionLightningFoundationTrial" : "TM_ProgressionLightningMasteryTrial").Translate();
                text += "\n" + "TM_ProgressionLightningTrialProgress".Translate(lightning.LightningTrials.BoltDamage.ToString("0.#"), lightning.LightningTrials.CloudVictims.Count);
                text += "\n" + (lightning.CompletedTrials.Contains(trial) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                var book = first ? lightningProfile?.adeptBook : lightningProfile?.masterBook;
                if (book != null) text += "\n" + "TM_ProgressionNextBook".Translate(book.LabelCap);
                text += "\n" + "TM_ProgressionLightningAttunementHint".Translate();
            }
            if (lightning?.Stage == InitiationStage.Master)
                text += "\n\n" + "TM_ProgressionLightningAscensionRequirements".Translate();
            var arcanist = state.Find("rwom.arcanist");
            var arcanistProfile = ClassIdentity.Profile("rwom.arcanist");
            if (arcanist != null && (arcanist.Stage == InitiationStage.Foundation || arcanist.Stage == InitiationStage.Adept))
            {
                bool first = arcanist.Stage == InitiationStage.Foundation;
                string trial = first ? ArcanistTrials.FoundationTrialId : ArcanistTrials.MasteryTrialId;
                text += "\n\n" + (first ? "TM_ProgressionArcanistFoundationTrial" : "TM_ProgressionArcanistMasteryTrial").Translate();
                if (first) text += "\n" + "TM_ProgressionArcanistTrialProgress".Translate(arcanist.ArcanistTrials.MissileDamage.ToString("0.#"));
                text += "\n" + (arcanist.CompletedTrials.Contains(trial) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                var book = first ? arcanistProfile?.adeptBook : arcanistProfile?.masterBook;
                if (book != null) text += "\n" + "TM_ProgressionNextBook".Translate(book.LabelCap);
                text += "\n" + "TM_ProgressionArcanistAttunementHint".Translate();
            }
            if (arcanist?.Stage == InitiationStage.Master)
                text += "\n\n" + "TM_ProgressionArcanistAscensionRequirements".Translate();
            foreach (var classId in new[] { "rwom.paladin", "rwom.summoner", "rwom.druid", "rwom.bard", "rwom.warlock", "rwom.succubus", "rwom.geomancer", "rwom.technomancer", "rwom.bloodmage", "rwom.enchanter", "rwom.chronomancer", "rwom.brightmage", "rwom.shaman", "rwom.golemancer", "rwom.empath" })
            {
                var discipline = state.Find(classId);
                var disciplineProfile = ClassIdentity.Profile(classId);
                string prefix = classId == "rwom.empath" ? "TM_ProgressionEmpath" : classId == "rwom.golemancer" ? "TM_ProgressionGolemancer" : classId == "rwom.shaman" ? "TM_ProgressionShaman" : classId == "rwom.brightmage" ? "TM_ProgressionBrightmage" : classId == "rwom.chronomancer" ? "TM_ProgressionChronomancer" : classId == "rwom.enchanter" ? "TM_ProgressionEnchanter" : classId == "rwom.bloodmage" ? "TM_ProgressionBloodMage" : classId == "rwom.technomancer" ? "TM_ProgressionTechnomancer" : classId == "rwom.geomancer" ? "TM_ProgressionGeomancer" : classId == "rwom.warlock" ? "TM_ProgressionWarlock" : classId == "rwom.succubus" ? "TM_ProgressionSuccubus" : classId == "rwom.druid" ? "TM_ProgressionDruid" : classId == "rwom.bard" ? "TM_ProgressionBard" : classId == "rwom.paladin" ? "TM_ProgressionPaladin" : "TM_ProgressionSummoner";
                if (discipline != null && (discipline.Stage == InitiationStage.Foundation || discipline.Stage == InitiationStage.Adept))
                {
                    bool first = discipline.Stage == InitiationStage.Foundation;
                    string trial = classId + (first ? ".foundation" : ".mastery");
                    text += "\n\n" + (prefix + (first ? "FoundationTrial" : "MasteryTrial")).Translate();
                    if (first && classId == "rwom.summoner")
                        text += "\n" + "TM_ProgressionSummonerTrialProgress".Translate(discipline.SummonerTrials.ControlledTicks);
                    if (first && classId == "rwom.brightmage")
                        text += "\n" + "TM_ProgressionBrightmageTrialProgress".Translate(discipline.BrightmageTrials.LanceDamage.ToString("0.#"));
                    if (first && classId == "rwom.shaman")
                        text += "\n" + "TM_ProgressionShamanTrialProgress".Translate(discipline.ShamanTrials.Healing.ToString("0.#"));
                    if (!first && classId == "rwom.bloodmage")
                        text += "\n" + "TM_ProgressionBloodMageTrialProgress".Translate(discipline.BloodMageTrials.IgniteDamage.ToString("0.#"));
                    text += "\n" + (discipline.CompletedTrials.Contains(trial) ? "TM_ProgressionTrialDone" : "TM_ProgressionTrialPending").Translate();
                    var book = first ? disciplineProfile?.adeptBook : disciplineProfile?.masterBook;
                    if (book != null) text += "\n" + "TM_ProgressionNextBook".Translate(book.LabelCap);
                    text += "\n" + (prefix + "AttunementHint").Translate();
                }
                if (discipline?.Stage == InitiationStage.Master)
                    text += "\n\n" + (prefix + "AscensionRequirements").Translate();
            }
            return text;
        }

        private float InitiationControlsHeight()
        {
            if (!ClassIdentity.TryGetState(pawn, out var state)) return ClassEnrollment.UnenrolledWarrior(pawn) ? 184f + 36f * AdditionalMartialRegistry.Paths.Length : 0f;
            var sniper = state.Find(SniperFoundation.Id);
            float sniperHeight = sniper?.Stage == InitiationStage.Foundation ? 40f
                : sniper?.ShotChoice == SniperShotChoice.Aimed ? 104f
                : sniper?.ShotChoice == SniperShotChoice.Headshot ? 52f : 0f;
            if (sniper?.Stage == InitiationStage.Adept && SniperMastery.Ready(pawn)) sniperHeight += 40f;
            if (sniper?.Stage == InitiationStage.Master) sniperHeight += 40f;
            return 68f + (AdditionalMartialRegistry.Current(state, out var extraPath)?.ControlsHeight(pawn,extraPath) ?? 0f) + (state.Find(RangerCareer.Id)?.Stage == InitiationStage.Foundation || RangerMastery.Ready(pawn) || RangerAscension.Ready(pawn) ? 48f : 0f) + (state.Find(BladeCareer.Id) != null ? 52f : 0f) + (state.Find(BladeCareer.Id)?.Stage == InitiationStage.Foundation || BladeMastery.Ready(pawn) || BladeAscension.Ready(pawn) ? 40f : 0f) + sniperHeight + (JobDriver_GladiatorDefense.Path(pawn) is DisciplineProgress gladiator ? (gladiator.Stage >= InitiationStage.Adept && gladiator.Defense == GladiatorDefense.Cover ? 80f : 40f) : 0f) + (WandererLessons.Path(pawn)?.Lessons?.Remaining > 0 ? 40f : 0f) + (JobDriver_WandererSurvey.Eligible(pawn) ? 40f : 0f) + (ClassEnrollment.CanChooseWanderer(pawn) ? 184f + 36f * AdditionalMartialRegistry.Paths.Length : 0f) + (state.Find("rwom.chaos")?.Stage == InitiationStage.Ascended ? 40f : 0f) + FoundationBookChoices().Count() * 36f + (state.NoviceSpells.Count < 2 ? NoviceLearning.Options.Count() * 36f : 0f)
                + (state.NeedsLegacyElementChoice ? 40f : 0f)
                + (ClassTomeQuestOffer.Candidate(pawn) != null || ClassTomeQuestOffer.Existing(pawn) != null ? 40f : 0f);
        }

        private System.Collections.Generic.IEnumerable<System.Tuple<Thing, ClassDisciplineDef>> FoundationBookChoices()
        {
            if (pawn.Map == null) yield break;
            if (!ClassIdentity.TryGetState(pawn, out var state)) yield break;
            foreach (var profile in ClassInitiation.FoundationProfiles)
            {
                var id = profile.profileId;
                if (!state.CanChooseFoundation(id) || (id != "rwom.warlock" && id != "rwom.succubus"
                    && id != "rwom.shadow" && !ClassProgressionState.Elemental(id))) continue;
                var book = pawn.Map.listerThings.ThingsOfDef(profile.foundationBook).FirstOrDefault(t =>
                    !t.IsForbidden(pawn) && ClassInitiation.CanUseFoundation(pawn, t, id, out _)
                    && pawn.CanReserveAndReach(t, Verse.AI.PathEndMode.Touch, Danger.Deadly));
                if (book != null) yield return System.Tuple.Create(book, profile);
            }
        }

        private void OpenTomeRequest(string id)
        {
            var pending = ClassTomeQuestOffer.Existing(pawn);
            if (pending != null) { ClassTomeQuestOffer.Open(pending.quest); Close(); return; }
            var budget = TomeElementalRift.Budget(StorytellerUtility.DefaultThreatPointsNow(pawn.Map));
            var riftAvailable = TomeElementalRift.Faction() != null && TomeElementalRift.Kinds(id,budget).Count > 0;
            Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption> {
                new FloatMenuOption("TM_TomeRaidChoice".Translate(), () => CreateTomeEncounter(id,false)),
                new FloatMenuOption((riftAvailable ? "TM_TomeRiftChoice" : "TM_TomeRiftLocked").Translate(),
                    riftAvailable ? () => CreateTomeEncounter(id,true) : (System.Action)null)
            }));
        }

        private void CreateTomeEncounter(string id,bool rift)
        {
            var quest = ClassTomeQuestOffer.RequestEncounter(pawn, id, rift);
            if (quest != null) { ClassTomeQuestOffer.Open(quest); Close(); }
            else Messages.Message("TM_TomeQuestUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
        }

        private float DrawWandererChoice(float width, float y)
        {
            if (pawn.Map != null && ClassEnrollment.CanChooseWanderer(pawn))
            {
                foreach (var form in new[] { TorannMagicDefOf.BookOfGladiator, TorannMagicDefOf.BookOfSniper, TorannMagicDefOf.BookOfBladedancer, TorannMagicDefOf.BookOfRanger }.Concat(AdditionalMartialRegistry.Paths.Select(profile => DefDatabase<ThingDef>.GetNamedSilentFail(profile.Book))))
                {
                    if (form == null) continue;
                    var book = pawn.Map.listerThings.ThingsOfDef(form).FirstOrDefault(t => JobDriver_GladiatorInitiation.Eligible(pawn,t)
                        && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t,PathEndMode.Touch,Danger.Some));
                    if (book == null) continue;
                    if (Widgets.ButtonText(new Rect(0f,y,width,32f), form.LabelCap))
                        Find.WindowStack.Add(new FloatMenu(JobDriver_GladiatorInitiation.Choices(pawn,book).ToList()));
                    y += 36f;
                }
            }
            if (ClassEnrollment.CanChooseWanderer(pawn))
            {
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_ChooseUnifiedWanderer".Translate()))
                    Find.WindowStack.Add(new FloatMenu(new[] { Vocation.Mage, Vocation.Warrior, Vocation.WarriorMage }
                        .Select(route => new FloatMenuOption(("TM_ProgressionVocation_" + route).Translate(), () =>
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "TM_ChooseUnifiedWanderer".Translate() + " — " + ("TM_ProgressionVocation_" + route).Translate(), () =>
                                {
                                    if (ClassEnrollment.ChooseWanderer(pawn, route)) Close();
                                })))).ToList()));
                y += 40f;
            }
            return y;
        }

        private void DrawSniperTraining(float width, ref float y, bool breach)
        {
            int rank = SniperTraining.Rank(pawn, breach);
            if (rank == 0) return;
            var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(breach ? "TM_SniperArmorFracture" : "TM_SniperAimedShot");
            if (ability == null) return;
            int value = breach ? SniperTraining.BreachPercent(rank) : SniperTraining.DamagePercent(rank);
            var line = new Rect(0f, y, width - 46f, 48f);
            Widgets.Label(line, ability.LabelCap + "  " + rank + "/3 (" + value + "%)");
            TooltipHandler.TipRegion(line, ability.description);
            var upgrade = new Rect(width - 46f, y, 46f, 30f);
            if (rank < 3) TooltipHandler.TipRegion(upgrade, "TM_SniperTrainRank".Translate());
            if (rank < 3 && pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightAbilityPoints >= 1 &&
                Widgets.ButtonText(upgrade, "+1"))
                SniperTraining.Upgrade(pawn, breach, rank);
            y += 52f;
        }

        private void DrawInitiationControls(float width, float y)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state)) { DrawWandererChoice(width, y); return; }
            if (state.NeedsLegacyElementChoice)
            {
                if (Widgets.ButtonText(new Rect(0f,y,width,32f),"TM_ImportedElementChoice".Translate()))
                    Find.WindowStack.Add(new FloatMenu(state.Disciplines.Select(d=>new FloatMenuOption(ClassIdentity.DisciplineLabel(d.ClassId),()=>{
                        if (ClassIdentity.TryGetState(pawn,out var current) && current==state && current.ChooseImportedElement(d.ClassId))
                            ClassIdentity.Component(pawn).SynchronizeHealthDisplay();
                    })).ToList()));
                y+=40f;
            }
            AdditionalMartialRegistry.Current(state,out var extraPath)?.DrawControls(pawn,extraPath,width,ref y);
            if (state.Find(RangerCareer.Id)?.Stage == InitiationStage.Foundation)
            {
                var pet = RangerCareer.Companion(pawn);
                if (RangerCareer.Ready(pawn))
                {
                    if (Widgets.ButtonText(new Rect(0f,y,width,40f), "TM_RangerClaimTrap".Translate())) RangerCareer.Claim(pawn);
                }
                else if (RangerCareer.Eligible(pawn, pet) && Widgets.ButtonText(new Rect(0f,y,width,40f), "TM_RangerCareOrder".Translate(pet.LabelShort)))
                {
                    RangerCareer.OrderCare(pawn,pet);
                }
                y += 48f;
            }
            else if (RangerMastery.Ready(pawn))
            {
                var trail = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerSafeTrail");
                if (trail != null && Widgets.ButtonText(new Rect(0f,y,width,40f), "TM_RangerMasteryClaim".Translate(trail.LabelCap)))
                    RangerMastery.Claim(pawn);
                y += 48f;
            }
            else if (RangerAscension.Ready(pawn))
            {
                if (Widgets.ButtonText(new Rect(0f,y,width,40f), "TM_RangerChooseUltimate".Translate()))
                {
                    var hunt = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerBreakthrough");
                    var storm = TorannMagicDefOf.TM_ArrowStorm;
                    if (hunt != null && storm != null)
                        Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption>
                        {
                            new FloatMenuOption("TM_BladeUltimateOption".Translate(storm.LabelCap), () => RangerAscension.Choose(pawn,false)),
                            new FloatMenuOption("TM_BladeUltimateOption".Translate(hunt.LabelCap), () => RangerAscension.Choose(pawn,true))
                        }));
                }
                y += 48f;
            }
            Text.Font = GameFont.Small;
            var blade = state.Find(BladeCareer.Id);
            if (blade != null)
            {
                var passage = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladePassage");
                int rank = BladeCareer.PassageRank(pawn);
                if (passage != null)
                {
                    var line = new Rect(0f, y, width - 46f, 48f);
                    Widgets.Label(line, "TM_BladePassageRank".Translate(passage.LabelCap, rank, BladeCareer.PassageRange(pawn)));
                    TooltipHandler.TipRegion(line, passage.description);
                    var train = new Rect(width - 46f, y, 46f, 30f);
                    if (rank < 3)
                    {
                        TooltipHandler.TipRegion(train, "TM_SniperTrainRank".Translate());
                        if (pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightAbilityPoints >= 1 && Widgets.ButtonText(train, "+1"))
                            BladeCareer.TrainPassage(pawn, rank);
                    }
                }
                y += 52f;
            }
            if (blade?.Stage == InitiationStage.Foundation)
            {
                bool ready = BladeCareer.Ready(pawn);
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), ready ? "TM_BladeClaimAdept".Translate(TorannMagicDefOf.TM_BladeSpin.LabelCap) : "TM_BladeServiceOrder".Translate()))
                {
                    if (ready) BladeCareer.Claim(pawn);
                    else { Close(); JobDriver_BladeWeaponService.Target(pawn); }
                }
                y += 40f;
            }
            else if (BladeMastery.Ready(pawn))
            {
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_BladeMasteryClaim".Translate(TorannMagicDefOf.TM_SeismicSlash.LabelCap)))
                    BladeMastery.Claim(pawn);
                y += 40f;
            }
            else if (BladeAscension.Ready(pawn))
            {
                if (Widgets.ButtonText(new Rect(0f,y,width,32f), "TM_BladeAscend".Translate()))
                {
                    var form = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladeSevenSteps");
                    var phase = TorannMagicDefOf.TM_PhaseStrike;
                    if (form != null && phase != null)
                        Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption>
                        {
                            new FloatMenuOption("TM_BladeUltimateOption".Translate(phase.LabelCap), () => BladeAscension.Choose(pawn,false)),
                            new FloatMenuOption("TM_BladeUltimateOption".Translate(form.LabelCap), () => BladeAscension.Choose(pawn,true))
                        }));
                }
                y += 40f;
            }
            var sniper = SniperSurvey.Path(pawn);
            if (sniper?.Stage == InitiationStage.Foundation)
            {
                bool ready = sniper.CompletedTrials.Contains(SniperSurvey.Trial);
                string label = ready ? "TM_SniperChooseShot".Translate().ToString()
                    : "TM_SniperObserve".Translate(sniper.Survey?.Enemies ?? 0,sniper.Survey?.Sectors ?? 0).ToString();
                if (Widgets.ButtonText(new Rect(0f,y,width,32f), label))
                {
                    if (!ready) { Close(); JobDriver_SniperObservation.Target(pawn); }
                    else Find.WindowStack.Add(new FloatMenu(new[] { SniperShotChoice.Headshot, SniperShotChoice.Aimed }
                        .Select(option => new FloatMenuOption(("TM_SniperPick" + option).Translate(), () => SniperSurvey.Choose(pawn,option))).ToList()));
                }
                y += 40f;
            }
            if (sniper != null && sniper.Stage >= InitiationStage.Adept)
            {
                DrawSniperTraining(width, ref y, breach: false);
                DrawSniperTraining(width, ref y, breach: true);
                if (sniper.Stage == InitiationStage.Master)
                {
                    if (SniperAscension.Ready(pawn))
                    {
                        if (Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_SniperAscend".Translate()))
                        {
                            var opening = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_SniperSingleOpening");
                            var cloak = TorannMagicDefOf.TM_ShadowSlayer;
                            if (opening != null && cloak != null)
                                Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption>
                                {
                                    new FloatMenuOption("TM_SniperChooseCloak".Translate(cloak.LabelCap), () => SniperAscension.Choose(pawn, false)),
                                    new FloatMenuOption("TM_SniperChooseOpening".Translate(opening.LabelCap), () => SniperAscension.Choose(pawn, true))
                                }));
                        }
                    }
                    else if (SniperAscension.DrillAllowed(pawn) && Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_SniperBeginDrill".Translate()))
                    {
                        Close();
                        JobDriver_SniperEvacuationDrill.Target(pawn);
                    }
                    y += 40f;
                }
                if (sniper.Stage == InitiationStage.Adept && SniperMastery.Ready(pawn))
                {
                    if (Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_SniperMasteryClaim".Translate()))
                        SniperMastery.Claim(pawn);
                    y += 40f;
                }
            }
            var gladiator = JobDriver_GladiatorDefense.Path(pawn);
            if (gladiator?.Stage == InitiationStage.Foundation)
            {
                bool ready = JobDriver_GladiatorDefense.CompletedTrial(pawn);
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), (ready ? "TM_GladiatorDefenseChoice" : "TM_GladiatorReliefDrill").Translate()))
                {
                    if (!ready) { Close(); JobDriver_GladiatorDefense.Target(pawn, true); }
                    else Find.WindowStack.Add(new FloatMenu(new[] { GladiatorDefense.Fortitude, GladiatorDefense.Cover }
                        .Select(option => new FloatMenuOption(("TM_GladiatorChoose" + option).Translate(), () => JobDriver_GladiatorDefense.Choose(pawn,option))).ToList()));
                }
                y += 40f;
            }
            else if (gladiator?.Defense == GladiatorDefense.Cover)
            {
                if (Widgets.ButtonText(new Rect(0f,y,width,32f), "TM_GladiatorUseCover".Translate()))
                { Close(); JobDriver_GladiatorDefense.Target(pawn, false); }
                y += 40f;
            }
            if (gladiator?.Stage == InitiationStage.Adept)
            {
                bool ready = gladiator.CompletedTrials.Contains(GladiatorMastery.Trial);
                if (Widgets.ButtonText(new Rect(0f,y,width,32f), (ready ? "TM_GladiatorAttackChoice" : "TM_GladiatorHoldDrill").Translate()))
                {
                    if (!ready) { Close(); JobDriver_GladiatorDefense.Target(pawn,true); }
                    else Find.WindowStack.Add(new FloatMenu(new[] { GladiatorAttack.Cleave, GladiatorAttack.Sweep }
                        .Select(option => new FloatMenuOption(("TM_GladiatorChoose" + option).Translate(), () => GladiatorMastery.Choose(pawn,option))).ToList()));
                }
                y += 40f;
            }
            if (gladiator?.Stage == InitiationStage.Master)
            {
                if (!GladiatorAscension.Qualified(pawn))
                    Widgets.Label(new Rect(0f,y,width,32f),"TM_GladiatorLevelGate".Translate());
                else if (!gladiator.CompletedTrials.Contains(GladiatorAscension.Trial))
                {
                    if (Widgets.ButtonText(new Rect(0f,y,width,32f),"TM_GladiatorEvacTrial".Translate()))
                    { Close(); JobDriver_GladiatorDefense.Target(pawn,true); }
                }
                else if (Widgets.ButtonText(new Rect(0f,y,width,32f),"TM_GladiatorUltimateChoice".Translate()))
                    Find.WindowStack.Add(new FloatMenu(new[] {GladiatorUltimate.Whirlwind,GladiatorUltimate.LastCircle}
                        .Select(option => new FloatMenuOption(("TM_GladiatorPick"+option).Translate(),() => GladiatorAscension.Choose(pawn,option))).ToList()));
                y += 40f;
            }
            if (gladiator?.Ultimate == GladiatorUltimate.LastCircle)
            {
                var rect = new Rect(0f,y,width,32f);
                bool enabled = GUI.enabled;
                try
                {
                    GUI.enabled = enabled && JobDriver_LastCircle.Ready(pawn);
                    if (Widgets.ButtonText(rect,"TM_GladiatorCastCircle".Translate()))
                    { Close(); pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_GladiatorLastCircle"),pawn),JobTag.Misc); }
                }
                finally { GUI.enabled = enabled; }
                y += 40f;
            }
            var lessons = WandererLessons.Path(pawn)?.Lessons;
            if (lessons?.Remaining > 0)
            {
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_WandererLessons".Translate(lessons.Remaining)))
                    Find.WindowStack.Add(new FloatMenu(lessons.Offer.Select(id => new FloatMenuOption(
                        DefDatabase<TMAbilityDef>.GetNamedSilentFail(id.Substring(2))?.LabelCap.ToString() ?? id,
                        () => { if (WandererLessons.Pick(pawn, id)) Close(); })).ToList()));
                y += 40f;
            }
            if (JobDriver_WandererSurvey.Eligible(pawn))
            {
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), "TM_WandererSurvey".Translate()))
                {
                    Close();
                    JobDriver_WandererSurvey.SelectSite(pawn);
                }
                y += 40f;
            }
            y = DrawWandererChoice(width, y);
            foreach (var choice in FoundationBookChoices())
            {
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), ClassInitiationMenu.Label(choice.Item2, choice.Item1))
                    && ClassInitiationMenu.Begin(pawn, choice.Item1, choice.Item2.profileId)) Close();
                y += 36f;
            }
            var chaos = state.Find("rwom.chaos");
            if (chaos?.Stage == InitiationStage.Ascended)
            {
                var ledger = chaos.Chaos;
                string label = ledger?.Anchor == null ? "—" : GrimoireKnowledge.DisplayName(ledger.Anchor);
                if (Widgets.ButtonText(new Rect(0f,y,width,32f), "TM_ChaosAnchor".Translate(label)) && ledger != null)
                {
                    var options = ChaosDiscipline.Active(pawn.GetCompAbilityUserMagic(),chaos).Select(power =>
                    {
                        var id = ChaosDiscipline.Id(power);
                        return new FloatMenuOption(power.abilityDef.LabelCap, () => ledger.SelectAnchor(id,chaos));
                    }).ToList();
                    options.Add(new FloatMenuOption("TM_ChaosNoAnchor".Translate(), () => ledger.SelectAnchor(null,chaos)));
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                y += 40f;
            }
            var request = ClassTomeQuestOffer.Existing(pawn);
            if (ClassTomeQuestOffer.Candidate(pawn) != null || request != null)
            {
                var rect = new Rect(0f, y, width, 32f);
                string reason = request == null ? ClassTomeQuestOffer.UnavailableReason(pawn) : null;
                TooltipHandler.TipRegion(rect, (reason ?? "TM_TomeQuestRequestDesc").Translate());
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && reason == null;
                try
                {
                    if (Widgets.ButtonText(rect, (request == null ? "TM_TomeQuestRequest" : "TM_TomeQuestView").Translate()))
                    {
                        var choices = ClassTomeQuestOffer.Candidates(pawn).ToList();
                        if (request == null && choices.Count > 1)
                        {
                            Find.WindowStack.Add(new FloatMenu(choices.Select(profile => new FloatMenuOption(
                                profile.masterBook.LabelCap, () => OpenTomeRequest(profile.profileId))).ToList()));
                            return;
                        }
                        var quest = request?.quest ?? ClassTomeQuestOffer.Request(pawn);
                        if (quest != null) { ClassTomeQuestOffer.Open(quest); Close(); }
                        else Messages.Message("TM_TomeQuestUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
                finally { GUI.enabled = previousEnabled; }
                y += 40f;
            }
            if ((state.Vocation & Vocation.Mage) == 0) return;
            Widgets.Label(new Rect(0f, y, width, 48f), "TM_ProgressionNoviceChoices".Translate(state.NoviceSpells.Count, 2));
            y += 52f;
            if (state.NoviceSpells.Count >= 2) return;
            foreach (var ability in NoviceLearning.Options)
            {
                if (state.NoviceSpells.Contains(ability.defName) || NoviceLearning.AlreadyKnown(pawn, ability)) continue;
                TooltipHandler.TipRegion(new Rect(0f, y, width, 32f), ability.description);
                if (Widgets.ButtonText(new Rect(0f, y, width, 32f), ability.LabelCap)
                    && NoviceLearning.TryLearn(pawn, ability.defName)) filtersBuilt = false;
                y += 36f;
            }
        }
    }
}
