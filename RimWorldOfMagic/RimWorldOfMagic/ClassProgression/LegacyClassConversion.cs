using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class LegacyClassConversion
    {
        // The original trait is retained on imported pawns so a beta-off save
        // still has its classic identity and the exact same power owners.
        internal static bool IsRetainedSource(Pawn pawn, TraitDef trait)
            => trait != null && ClassIdentity.TryGetState(pawn, out var state)
                && state.Disciplines.Any(d => d.ImportedTrait == trait.defName ||
                    (d.ClassId == "rwom.wanderer" && d.Domain == DisciplineDomain.WarriorMage &&
                     d.ImportedTrait == "TM_Wanderer" && trait.defName == "TM_Wayfarer"));

        internal static bool TryImport(Pawn pawn)
        {
            var comp = ClassIdentity.Component(pawn);
            if (comp == null || comp.DebugKeepClassic || comp.SpiritRebirth != null || pawn.Dead ||
                pawn.story?.traits == null || pawn.IsGhoul || pawn.IsShambler) return false;
            if (comp.State != null) return ClassIdentity.TryGetState(pawn,out var active) && RecognizePaidUltimates(pawn,active);
            var traits = pawn.story.traits.allTraits;
            if (traits.Any(t => t.def == TorannMagicDefOf.TM_TheShadow))
                return TryImportShadow(pawn, comp);
            if (traits.Any(t => t.def == TorannMagicDefOf.TM_Wanderer || t.def == TorannMagicDefOf.TM_Wayfarer))
                return TryImportWanderer(pawn, comp);
            if (traits.Any(t => TM_ClassGroups.MightEntriesField.Any(e => e.trait == t.def.defName))) return LegacyMartialConversion.TryImport(pawn,comp);
            var magic = pawn.GetCompAbilityUserMagic();
            if (magic?.ExistingMagicData == null) return false;
            var sources = traits.Where(t => TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def.defName)
                || (TM_ClassUtility.CustomClasses?.Any(c => c?.classTrait == t.def) ?? false)).Take(3).ToList();
            if (sources.Count == 0 || sources.Count > 2 || sources.Any(t => t.Suppressed)) return false;
            if (sources.Count == 1 && sources[0].def == TorannMagicDefOf.Lich)
            {
                // The classic transformation removes Necromancer and leaves Lich.
                // Restore its already-active form without replaying the ritual or its physical changes.
                var profiles = ClassInitiation.FoundationProfiles.Where(p => p.profileId == "rwom.necromancer"
                    && p.legacyTrait == TorannMagicDefOf.Necromancer).Take(2).ToList();
                if (profiles.Count != 1 || TorannMagicDefOf.TM_DeathBolt == null ||
                    !magic.ExistingMagicData.MagicPowersN.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_DeathBolt))
                    || !comp.BeginNewProgression(Vocation.Mage)) return false;
                comp.State.TryAddFoundation(profiles[0].profileId, DisciplineDomain.Magic);
                comp.State.Find(profiles[0].profileId).ImportLegacyStage(sources[0].def.defName, InitiationStage.Ascended);
                return comp.State.ActivateLichForm();
            }
            var plan = new List<(ClassDisciplineDef profile, InitiationStage earned)>();
            foreach (var source in sources)
            {
                if (!TryReadMagicPath(magic.ExistingMagicData, source, out var profile, out var earned)) return false;
                plan.Add((profile, earned));
            }
            if (plan.Count == 2 && !(plan.Any(p => p.profile.profileId == "rwom.fire")
                && plan.Any(p => p.profile.profileId == "rwom.ice"))) return false;
            if (!comp.BeginNewProgression(Vocation.Mage)) return false;
            foreach (var item in plan)
            {
                comp.State.TryAddFoundation(item.profile.profileId, DisciplineDomain.Magic);
                comp.State.Find(item.profile.profileId).ImportLegacyStage(item.profile.legacyTrait.defName, item.earned);
            }
            RecognizePaidUltimates(pawn,comp.State);
            return true;
        }

        private static bool RecognizePaidUltimates(Pawn pawn, ClassProgressionState state)
        {
            List<DisciplineProgress> paid = null;
            foreach (var path in state.Disciplines)
            {
                if (path?.Domain != DisciplineDomain.Magic || path.ImportedTrait == null ||
                    path.AuditedLegacyUltimate || path.Stage == InitiationStage.Ascended) continue;
                var profiles = ClassInitiation.FoundationProfiles.Where(p=>p.profileId==path.ClassId).Take(2).ToList();
                if (profiles.Count != 1) continue;
                var profile = profiles[0];
                var ultimate = ClassAscension.Ultimate(path.ClassId);
                if (profile?.legacyTrait?.defName != path.ImportedTrait || !pawn.story.traits.HasTrait(profile.legacyTrait) ||
                    ultimate == null || profile.spells?.Count(s => s?.ability == ultimate && s.stage == InitiationStage.Ascended) != 1) continue;
                var magic = pawn.GetCompAbilityUserMagic();
                var powers = ClassInitiation.Powers(magic?.ExistingMagicData,profile);
                var owners = powers?.Where(p => p?.TMabilityDefs?.Contains(ultimate) == true).Take(2).ToList();
                if (owners?.Count != 1) continue;
                // Absent learned nodes deserialize as true. An explicit spell flag
                // or an invested rank is stronger evidence than that default.
                bool savedGrant = ultimate == TorannMagicDefOf.TM_Recall ? magic.spell_Recall :
                    ClassSpellAccess.FlagAbilities(magic).Contains(ultimate);
                path.AuditedLegacyUltimate = true;
                if (!savedGrant && !(owners[0].learned && owners[0].level > 0 && owners[0].level < owners[0].TMabilityDefs.Count)) continue;
                (paid ??= new List<DisciplineProgress>()).Add(path);
            }
            return paid != null && state.RecognizeImportedAscensions(paid);
        }

        private static bool TryImportShadow(Pawn pawn, CompClassProgression comp)
        {
            var source = TorannMagicDefOf.TM_TheShadow;
            var traits = pawn.story.traits.allTraits;
            var registration = TM_ClassUtility.CustomClasses?.Where(c => c?.classTrait == source).Take(2).ToList();
            if (registration?.Count != 1 || !traits.Any(t => t.def == source && !t.Suppressed && t.Degree == registration[0].traitDegree)
                || traits.Any(t => t.def != source && (TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def.defName)
                    || TM_ClassGroups.MightEntriesField.Any(e => e.trait == t.def.defName)
                    || TM_ClassUtility.CustomClasses.Any(c => c?.classTrait == t.def)))) return false;
            var martial = AdditionalMartialRegistry.ForId(ShadowWalkerMartialPath.ClassId);
            var arcane = ClassIdentity.Profile("rwom.shadow");
            if (martial == null || arcane?.legacyTrait != source || arcane.foundationBook == null ||
                registration[0].fullScript != arcane.foundationBook || !martial.SupportsBook(arcane.foundationBook)) return false;
            var magic = pawn.GetCompAbilityUserMagic()?.ExistingMagicData;
            var might = pawn.GetCompAbilityUserMight()?.ExistingMightData;
            // Missing either saved owner is ambiguous; do not create a resource bank or transfer ranks.
            if (magic == null || might == null || !comp.BeginNewProgression(Vocation.WarriorMage)) return false;
            comp.State.TryAddFoundation("rwom.shadow", DisciplineDomain.Magic);
            comp.State.Find("rwom.shadow").ImportLegacyStage(source.defName, InitiationStage.Foundation);
            comp.State.TryAddFoundation(martial.Id, DisciplineDomain.Might);
            var path = comp.State.Find(martial.Id);
            var owners = might.mightPowerShadow;
            bool Learned(TMAbilityDef def) => def != null && owners?.Any(p => p?.learned == true && p.TMabilityDefs?.Contains(def) == true) == true;
            var stage = Learned(TorannMagicDefOf.TM_VeilOfShadows) ? InitiationStage.Master :
                Learned(TorannMagicDefOf.TM_Nightshade) && Learned(TorannMagicDefOf.TM_ShadowStrike) ? InitiationStage.Adept : InitiationStage.Foundation;
            path.MartialAlternate = Learned(TorannMagicDefOf.TM_Nightshade) && !Learned(TorannMagicDefOf.TM_ShadowStrike);
            path.ImportLegacyStage(source.defName, stage);
            path.ImportedMartialStage = stage;
            return true;
        }

        private static bool TryImportWanderer(Pawn pawn, CompClassProgression comp)
        {
            var traits = pawn.story.traits.allTraits;
            if (traits.Any(t => (TM_ClassUtility.CustomClasses?.Any(c => c?.classTrait == t.def) ?? false)
                || (t.def != TorannMagicDefOf.TM_Wanderer && t.def != TorannMagicDefOf.TM_Wayfarer &&
                    (TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def.defName) ||
                     TM_ClassGroups.MightEntriesField.Any(e => e.trait == t.def.defName))))) return false;
            var role = Vocation.None;
            foreach (var trait in traits.Where(t => t.def == TorannMagicDefOf.TM_Wanderer || t.def == TorannMagicDefOf.TM_Wayfarer))
            {
                if (trait.Suppressed) return false;
                role |= trait.def == TorannMagicDefOf.TM_Wanderer ? Vocation.Mage : Vocation.Warrior;
            }
            var might = pawn.GetCompAbilityUserMight()?.ExistingMightData;
            if (((role & Vocation.Mage) != 0 && pawn.GetCompAbilityUserMagic()?.ExistingMagicData == null)
                || ((role & Vocation.Warrior) != 0 && might == null)) return false;
            var picked = (role & Vocation.Warrior) != 0
                ? might.AllMightPowersWithSkills.Where(p => p?.learned == true && !might.MightPowersW.Contains(p)).ToList()
                : new List<MightPower>();
            if (!comp.BeginNewProgression(role)) return false;
            comp.State.ChooseWanderer(role);
            var path = comp.State.Find("rwom.wanderer");
            path.ImportLegacyStage((role & Vocation.Mage) != 0 ? "TM_Wanderer" : "TM_Wayfarer", InitiationStage.Foundation);
            path.EnsureLessons().ImportMartialPicks(picked);
            return true;
        }

        private static bool TryReadMagicPath(MagicData data, Trait source, out ClassDisciplineDef profile, out InitiationStage earned)
        {
            profile = null;
            earned = InitiationStage.Foundation;
            var profiles = ClassInitiation.FoundationProfiles.Where(p => p.legacyTrait == source.def).Take(2).ToList();
            if (profiles.Count != 1) return false;
            profile = profiles[0];
            var registrations = TM_ClassUtility.CustomClasses?.Where(c => c?.classTrait == source.def).ToList();
            if (registrations?.Count > 0 && (registrations.Count != 1 ||
                !(profile.profileId == "rwom.brightmage" || profile.profileId == "rwom.shaman" ||
                  profile.profileId == "rwom.golemancer" || profile.profileId == "rwom.empath") ||
                registrations[0].traitDegree != source.Degree || !registrations[0].isMage ||
                registrations[0].isAdvancedClass || registrations[0].isFighter)) return false;
            var powers = ClassInitiation.Powers(data, profile);
            if (profile.domain != DisciplineDomain.Magic || powers == null || profile.spells == null) return false;
            var spells = profile.spells.Where(s => s != null && s.stage >= InitiationStage.Foundation && s.stage <= InitiationStage.Master).ToList();
            if (!spells.Any(s => s.stage == InitiationStage.Foundation) || spells.Any(s => s.ability == null
                || powers.Count(p => p?.TMabilityDefs?.Contains(s.ability) == true) != 1)) return false;
            foreach (var stage in new[] { InitiationStage.Adept, InitiationStage.Master })
            {
                var tier = spells.Where(s => s.stage == stage).ToList();
                if (tier.Count == 0 || tier.Any(s => !powers.Single(p => p.TMabilityDefs.Contains(s.ability)).learned)) break;
                earned = stage;
            }
            return true;
        }
    }
}
