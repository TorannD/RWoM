using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ClassInitiation
    {
        private static readonly System.Collections.Generic.HashSet<Pawn> inFlight = new System.Collections.Generic.HashSet<Pawn>();
        public static System.Collections.Generic.IEnumerable<ClassDisciplineDef> FoundationProfiles
            => DefDatabase<ClassDisciplineDef>.AllDefsListForReading.Where(p =>
                (p.profileId == "rwom.shadow" || p.profileId == "rwom.chaos" || p.profileId == "rwom.priest" || p.profileId == "rwom.necromancer" || p.profileId == "rwom.fire" || p.profileId == "rwom.ice" || p.profileId == "rwom.lightning" || p.profileId == "rwom.arcanist" || p.profileId == "rwom.paladin" || p.profileId == "rwom.summoner" || p.profileId == "rwom.technomancer" || p.profileId == "rwom.geomancer" || p.profileId == "rwom.shaman" || p.profileId == "rwom.empath" || p.profileId == "rwom.golemancer" || p.profileId == "rwom.brightmage" || p.profileId == "rwom.chronomancer" || p.profileId == "rwom.enchanter" || p.profileId == "rwom.bloodmage" || p.profileId == "rwom.druid" || p.profileId == "rwom.bard" || p.profileId == "rwom.warlock" || p.profileId == "rwom.succubus") && p.foundationBook != null);

        internal static System.Collections.Generic.List<MagicPower> Powers(MagicData data, ClassDisciplineDef profile)
        {
            if (data == null || profile == null) return null;
            if (profile.profileId == "rwom.shadow" && profile.magicPowerList == "magicPowerShadow") return data.MagicPowersShadow;
            if (profile.profileId == "rwom.chaos" && profile.magicPowerList == "magicPowerCM") return data.MagicPowersCM;
            if (profile.profileId == "rwom.priest" && profile.magicPowerList == "magicPowerPR") return data.MagicPowersPR;
            if (profile.profileId == "rwom.necromancer" && profile.magicPowerList == "magicPowerN") return data.MagicPowersN;
            if (profile.profileId == "rwom.fire" && profile.magicPowerList == "magicPowerIF") return data.MagicPowersIF;
            if (profile.profileId == "rwom.ice" && profile.magicPowerList == "magicPowerHoF") return data.MagicPowersHoF;
            if (profile.profileId == "rwom.lightning" && profile.magicPowerList == "magicPowerSB") return data.MagicPowersSB;
            if (profile.profileId == "rwom.arcanist" && profile.magicPowerList == "magicPowerA") return data.MagicPowersA;
            if (profile.profileId == "rwom.paladin" && profile.magicPowerList == "magicPowerP") return data.MagicPowersP;
            if (profile.profileId == "rwom.summoner" && profile.magicPowerList == "magicPowerS") return data.MagicPowersS;
            if (profile.profileId == "rwom.technomancer" && profile.magicPowerList == "magicPowerT") return data.MagicPowersT;
            if (profile.profileId == "rwom.shaman" && profile.magicPowerList == "magicPowerShaman") return data.MagicPowersShaman;
            if (profile.profileId == "rwom.empath" && profile.magicPowerList == "magicPowerCustom") return EmpathOwners.Resolve(data);
            if (profile.profileId == "rwom.golemancer" && profile.magicPowerList == "magicPowerGolemancer") return data.MagicPowersGolemancer;
            if (profile.profileId == "rwom.brightmage" && profile.magicPowerList == "magicPowerBrightmage") return data.MagicPowersBrightmage;
            if (profile.profileId == "rwom.chronomancer" && profile.magicPowerList == "magicPowerC") return data.MagicPowersC;
            if (profile.profileId == "rwom.enchanter" && profile.magicPowerList == "magicPowerE") return data.MagicPowersE;
            if (profile.profileId == "rwom.bloodmage" && profile.magicPowerList == "magicPowerBM") return data.MagicPowersBM;
            if (profile.profileId == "rwom.geomancer" && profile.magicPowerList == "magicPowerG") return data.MagicPowersG;
            if (profile.profileId == "rwom.druid" && profile.magicPowerList == "magicPowerD") return data.MagicPowersD;
            if (profile.profileId == "rwom.bard" && profile.magicPowerList == "magicPowerB") return data.MagicPowersB;
            if (profile.profileId == "rwom.warlock" && profile.magicPowerList == "magicPowerWD") return data.MagicPowersWD;
            if (profile.profileId == "rwom.succubus" && profile.magicPowerList == "magicPowerSD") return data.MagicPowersSD;
            return null;
        }
        // Only creation-time callers and the explicit dev command enter here.
        // Build and validate the real persistent owners before publishing the class.
        internal static bool TryGrantStartingFoundation(Pawn pawn, TraitDef requested)
        {
            if (requested != null && requested == TorannMagicDefOf.TM_Wanderer)
                return ClassEnrollment.ChooseGeneratedWanderer(pawn, Vocation.Mage);
            var comp = ClassIdentity.Component(pawn);
            var vocation = DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage");
            if (comp == null || comp.State != null || requested == null || vocation == null ||
                pawn.Dead || pawn.Destroyed || pawn.story?.traits == null || pawn.health == null || pawn.needs == null ||
                ClassEnrollment.HasChosenClass(pawn) || !ClassEnrollment.HasNoPriorOwnership(pawn)) return false;
            var matches = FoundationProfiles.Where(p => p.legacyTrait == requested && p.domain == DisciplineDomain.Magic).Take(2).ToList();
            if (matches.Count != 1) return false;
            if (pawn.GetCompAbilityUserMagic() == null) Utils.TM_PawnTracker.EnsureClassComps(pawn);
            var magic = pawn.GetCompAbilityUserMagic();
            if (magic == null || !ClassEnrollment.HasNoPriorOwnership(pawn)) return false;
            var profile = matches[0];
            var data = new MagicData(magic) { MagicUserXP = 0 };
            var powers = Powers(data, profile);
            var spells = profile.spells?.Where(s => s != null && s.stage == InitiationStage.Foundation).ToList();
            int expected = profile.profileId == "rwom.shadow" || profile.profileId == "rwom.chaos" ? 1 : 2;
            if (powers == null || spells == null || spells.Count != expected ||
                spells.Select(s => s.ability).Distinct().Count() != expected ||
                spells.Any(s => s.ability == null || powers.Count(p => p?.TMabilityDefs?.Contains(s.ability) == true) != 1)) return false;
            if (!comp.BeginNewProgression(Vocation.Mage)) return false;
            comp.State.TryAddFoundation(profile.profileId, DisciplineDomain.Magic);
            magic.AttachStartingData(data);
            foreach (var spell in spells) powers.Single(p => p.TMabilityDefs.Contains(spell.ability)).learned = true;
            magic.magicPowersInitialized = true;
            ClassEnrollment.ConsumePotentials(pawn);
            if (!pawn.story.traits.HasTrait(vocation)) pawn.story.traits.GainTrait(new Trait(vocation));
            Utils.TM_PawnTracker.ResolveMagicComp(magic);
            magic.RecoverMissingProgression();
            ClassSpellAccess.RestoreLearned(magic);
            Compatibility.TM_LegacyRecovery.RestoreNeed(pawn, TorannMagicDefOf.TM_MagicUserHD, TorannMagicDefOf.TM_Mana);
            comp.SynchronizeHealthDisplay();
            return true;
        }

        internal static bool HasLegacyClass(Pawn pawn)
            => TM_ClassState.CurrentCustomClasses(pawn, true).Any(c => !LegacyClassConversion.IsRetainedSource(pawn, c.classTrait))
                || TM_ClassState.CurrentCustomClasses(pawn, false).Any(c => !LegacyClassConversion.IsRetainedSource(pawn, c.classTrait))
                || pawn.story?.traits?.allTraits.Any(t =>
                    (TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def.defName)
                    || TM_ClassGroups.MightEntriesField.Any(e => e.trait == t.def.defName))
                    && !LegacyClassConversion.IsRetainedSource(pawn, t.def)) == true;
        // Only audited foundation profiles are enabled. Metadata for other paths
        // is not permission to grant their spells or mix unported passive effects.
        public static System.Collections.Generic.IEnumerable<ClassDisciplineDef> FoundationCandidates(Pawn pawn, Thing book)
            => FoundationProfiles.Where(p => CanUseFoundation(pawn, book, p.profileId, out _));

        public static bool CanUseFoundation(Pawn pawn, Thing book, out ClassDisciplineDef profile)
        {
            var candidates = FoundationCandidates(pawn, book).Take(2).ToList();
            profile = candidates.Count == 1 ? candidates[0] : null;
            return profile != null;
        }

        public static bool CanUseFoundation(Pawn pawn, Thing book, string selectedProfileId, out ClassDisciplineDef profile)
        {
            profile = null;
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || book == null || book.Destroyed || book.def?.destroyable != true || book.stackCount < 1
                || !ClassIdentity.TryGetState(pawn, out var state) || state.Vocation != Vocation.Mage || !state.CanChooseFoundation(selectedProfileId)) return false;
            var candidates = FoundationProfiles.Where(p => p.profileId == selectedProfileId && p.foundationBook == book.def).Take(2).ToList();
            if (candidates.Count != 1) return false;
            var candidate = candidates[0];
            if (candidate.domain != DisciplineDomain.Magic || candidate.legacyTrait == null || HasLegacyClass(pawn)) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            var powers = Powers(magic?.MagicData, candidate);
            var foundations = candidate.spells?.Where(s => s != null && s.stage == InitiationStage.Foundation).ToList();
            if (powers == null || foundations == null || foundations.Count != (candidate.profileId == "rwom.shadow" || candidate.profileId == "rwom.chaos" ? 1 : 2) || foundations.Any(s => s.ability == null
                || powers.Count(p => p.TMabilityDefs.Contains(s.ability)) != 1)) return false;
            if ((candidate.profileId == "rwom.warlock" || candidate.profileId == "rwom.succubus")
                && TM_ClassState.PrepareDemonOwners(magic, candidate) == null) return false;
            profile = candidate;
            return true;
        }

        public static bool CanUseAdept(Pawn pawn, Thing book, out ClassDisciplineDef profile)
            => CanUseAdvancement(pawn, book, InitiationStage.Adept, out profile);

        public static bool CanUseMaster(Pawn pawn, Thing book, out ClassDisciplineDef profile)
            => CanUseAdvancement(pawn, book, InitiationStage.Master, out profile);

        private static bool CanUseAdvancement(Pawn pawn, Thing book, InitiationStage next, out ClassDisciplineDef profile)
        {
            profile = null;
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || book == null || book.Destroyed
                || book.def?.destroyable != true || book.stackCount < 1 || !ClassIdentity.TryGetState(pawn, out var state)
                || state.Vocation != Vocation.Mage || (state.Disciplines.Count != 1 && !state.ElementalPair)) return false;
            var path = state.ElementalPair ? state.Disciplines.FirstOrDefault(d =>
            {
                var p = ClassIdentity.Profile(d.ClassId);
                return (next == InitiationStage.Adept ? p?.adeptBook : p?.masterBook) == book.def;
            }) : state.Disciplines[0];
            if (path == null || (path.ClassId != "rwom.chaos" && path.ClassId != "rwom.priest"
                && path.ClassId != "rwom.necromancer" && path.ClassId != "rwom.fire" && path.ClassId != "rwom.ice" && path.ClassId != "rwom.lightning" && path.ClassId != "rwom.arcanist" && path.ClassId != "rwom.paladin" && path.ClassId != "rwom.summoner" && path.ClassId != "rwom.technomancer" && path.ClassId != "rwom.shaman" && path.ClassId != "rwom.empath" && path.ClassId != "rwom.golemancer" && path.ClassId != "rwom.brightmage" && path.ClassId != "rwom.chronomancer" && path.ClassId != "rwom.enchanter" && path.ClassId != "rwom.bloodmage" && path.ClassId != "rwom.geomancer" && path.ClassId != "rwom.druid" && path.ClassId != "rwom.bard" && path.ClassId != "rwom.warlock" && path.ClassId != "rwom.succubus")) return false;
            var candidate = ClassIdentity.Profile(path.ClassId);
            bool adept = next == InitiationStage.Adept;
            string trial = adept ? candidate?.adeptTrialId : candidate?.masterTrialId;
            if (path?.Stage != (InitiationStage)((int)next - 1) || path.Domain != DisciplineDomain.Magic
                || candidate?.domain != DisciplineDomain.Magic || (adept ? candidate.adeptBook : candidate.masterBook) != book.def
                || trial != ((path.ClassId == "rwom.chaos" || path.ClassId == "rwom.warlock" || path.ClassId == "rwom.succubus") ? path.ClassId + (adept ? ".foundation" : ".mastery") : path.ClassId == "rwom.technomancer" ? (adept ? "rwom.technomancer.foundation" : "rwom.technomancer.mastery") : path.ClassId == "rwom.shaman" ? (adept ? "rwom.shaman.foundation" : "rwom.shaman.mastery") : path.ClassId == "rwom.empath" ? (adept ? "rwom.empath.foundation" : "rwom.empath.mastery") : path.ClassId == "rwom.golemancer" ? (adept ? "rwom.golemancer.foundation" : "rwom.golemancer.mastery") : path.ClassId == "rwom.brightmage" ? (adept ? "rwom.brightmage.foundation" : "rwom.brightmage.mastery") : path.ClassId == "rwom.chronomancer" ? (adept ? "rwom.chronomancer.foundation" : "rwom.chronomancer.mastery") : path.ClassId == "rwom.enchanter" ? (adept ? "rwom.enchanter.foundation" : "rwom.enchanter.mastery") : path.ClassId == "rwom.bloodmage" ? (adept ? "rwom.bloodmage.foundation" : "rwom.bloodmage.mastery") : path.ClassId == "rwom.geomancer" ? (adept ? "rwom.geomancer.foundation" : "rwom.geomancer.mastery") : path.ClassId == "rwom.druid" ? (adept ? "rwom.druid.foundation" : "rwom.druid.mastery") : path.ClassId == "rwom.bard" ? (adept ? "rwom.bard.foundation" : "rwom.bard.mastery") : path.ClassId == "rwom.summoner" ? (adept ? "rwom.summoner.foundation" : "rwom.summoner.mastery") : path.ClassId == "rwom.paladin" ? (adept ? "rwom.paladin.foundation" : "rwom.paladin.mastery") : path.ClassId == "rwom.arcanist" ? (adept ? "rwom.arcanist.foundation" : "rwom.arcanist.mastery") : path.ClassId == "rwom.lightning" ? (adept ? "rwom.lightning.foundation" : "rwom.lightning.mastery") : path.ClassId == "rwom.ice" ? (adept ? "rwom.ice.foundation" : "rwom.ice.mastery") : path.ClassId == "rwom.fire" ? (adept ? FireboltDamageTrial.TrialId : FireclawTrial.TrialId)
                    : path.ClassId == "rwom.necromancer" ? (adept ? NecromancerControlTrial.TrialId : NecromancerDeathMarkTrial.TrialId)
                    : adept ? "rwom.priest.heal-ally" : "rwom.priest.purify-ally")
                || !path.CompletedTrials.Contains(trial)) return false;
            if (HasLegacyClass(pawn)) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            var spells = candidate.spells?.Where(s => s != null && s.stage == next).ToList();
            var powers = Powers(magic?.MagicData, candidate);
            int expectedSpells = path.ClassId == "rwom.chaos" ? 0 : (path.ClassId == "rwom.technomancer" || (path.ClassId == "rwom.necromancer" && !adept) || ((path.ClassId == "rwom.ice" || path.ClassId == "rwom.arcanist" || path.ClassId == "rwom.paladin" || path.ClassId == "rwom.geomancer" || path.ClassId == "rwom.bloodmage" || path.ClassId == "rwom.enchanter" || path.ClassId == "rwom.chronomancer" || path.ClassId == "rwom.brightmage" || path.ClassId == "rwom.shaman" || path.ClassId == "rwom.golemancer") && adept)) ? 2 : 1;
            if (powers == null || spells == null || spells.Count != expectedSpells || spells.Any(s => s.ability == null
                || powers.Count(p => p.TMabilityDefs.Contains(s.ability)) != 1)) return false;
            if (path.ClassId == "rwom.golemancer" && adept && !GolemancerBrands.CanPrepare(magic, path)) return false;
            profile = candidate;
            return true;
        }

        internal static void RestoreUnpaidBook(Pawn pawn, Thing payment, ThingOwner holder)
        {
            if (payment.Destroyed || payment.Spawned || payment.holdingOwner != null) return;
            if (holder != null)
            {
                try { holder.TryAdd(payment, false); }
                catch { if (payment.holdingOwner != null || payment.Spawned) return; }
                if (payment.holdingOwner != null || payment.Spawned) return;
            }
            if (pawn.inventory == null) pawn.inventory = new Pawn_InventoryTracker(pawn);
            pawn.inventory.innerContainer.TryAdd(payment, false);
            if (payment.holdingOwner == null) throw new System.InvalidOperationException("Could not retain unpaid class book.");
        }

        // Called only at the end of the existing UseArtifact job. An interrupted
        // approach/reading never gets here and therefore never pays or teaches.
        public static bool CompleteBook(Pawn pawn, Thing book)
            => CompleteBook(pawn, book, null);

        public static bool CompleteBook(Pawn pawn, Thing book, string selectedProfileId)
        {
            if (pawn == null || !inFlight.Add(pawn)) return false;
            try { if (selectedProfileId == null && WandererLessons.Path(pawn) != null) return WandererLessons.Read(pawn, book); return CompleteBookOnce(pawn, book, selectedProfileId); }
            finally { inFlight.Remove(pawn); }
        }

        private static bool CompleteBookOnce(Pawn pawn, Thing book, string selectedProfileId)
        {
            InitiationStage next = InitiationStage.Foundation;
            ClassDisciplineDef profile;
            bool foundation = selectedProfileId == null ? CanUseFoundation(pawn, book, out profile) : CanUseFoundation(pawn, book, selectedProfileId, out profile);
            if (!foundation)
            {
                if (selectedProfileId != null) return false;
                if (CanUseAdept(pawn, book, out profile)) next = InitiationStage.Adept;
                else if (CanUseMaster(pawn, book, out profile)) next = InitiationStage.Master;
                else return false;
            }
            if (book.Spawned)
            {
                if (book.Map != pawn.Map || !pawn.Position.InHorDistOf(book.Position, 1.5f)) return false;
            }
            else if (book.ParentHolder != pawn.carryTracker && book.ParentHolder != pawn.inventory) return false;
            var state = ClassIdentity.Component(pawn).State;
            var magic = pawn.GetCompAbilityUserMagic();
            var owners = profile.spells.Where(s => s != null && s.stage == next)
                .Select(s => Powers(magic.MagicData, profile).Single(p => p.TMabilityDefs.Contains(s.ability))).ToList();
            var shared = (profile.profileId == "rwom.warlock" || profile.profileId == "rwom.succubus")
                ? TM_ClassState.PrepareDemonOwners(magic, profile) : null;
            if ((profile.profileId == "rwom.warlock" || profile.profileId == "rwom.succubus") && shared == null) return false;
            var brands = profile.profileId == "rwom.golemancer" && next == InitiationStage.Adept
                ? GolemancerBrands.Prepare(magic, state.Find(profile.profileId)) : null;
            if (profile.profileId == "rwom.golemancer" && next == InitiationStage.Adept && brands == null) return false;
            // Native item destruction is the only payment; no XP or points are spent.
            // Commit persistent owners before runtime/UI callbacks so a refresh failure
            // cannot allow repeating payment or lose the purchased class on reload.
            var holder = book.holdingOwner;
            var payment = book.SplitOff(1);
            if (payment == null) return false;
            try { payment.Destroy(DestroyMode.Vanish); }
            catch
            {
                if (!payment.Destroyed) { RestoreUnpaidBook(pawn, payment, holder); return false; }
            }
            if (!payment.Destroyed) { RestoreUnpaidBook(pawn, payment, holder); return false; }
            // No external callbacks between this validated storage commit and learned flags.
            bool committed = next == InitiationStage.Foundation
                ? state.TryAddFoundation(profile.profileId, DisciplineDomain.Magic)
                : state.TryAdvanceAfterTrial(profile.profileId, (InitiationStage)((int)next - 1),
                    next == InitiationStage.Adept ? profile.adeptTrialId : profile.masterTrialId);
            if (!committed)
                throw new System.InvalidOperationException("Class initiation changed during book payment.");
            shared?.Apply();
            foreach (var owner in owners) owner.learned = true;
            if (next == InitiationStage.Foundation) ClassEnrollment.FinishMageFoundation(pawn);
            if (brands != null) GolemancerBrands.Commit(state.Find(profile.profileId), brands);
            ClassSpellAccess.RestoreLearned(magic);
            pawn.Notify_DisabledWorkTypesChanged();
            pawn.skills?.Notify_SkillDisablesChanged();
            ClassIdentity.Component(pawn).SynchronizeHealthDisplay();
            return true;
        }
    }
}
