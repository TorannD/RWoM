using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Metadata only at this stage; a profile does not grant a trait or any abilities.
    public sealed class ClassDisciplineDef : Def
    {
        public string profileId;
        public DisciplineDomain domain;
        public TraitDef legacyTrait;
        public string magicPowerList;
        public ThingDef foundationBook;
        public ThingDef adeptBook;
        public string adeptTrialId;
        public ThingDef masterBook;
        public string masterTrialId;
        public System.Collections.Generic.List<DisciplineSpell> spells = new System.Collections.Generic.List<DisciplineSpell>();
    }

    public sealed class DisciplineSpell
    {
        public TMAbilityDef ability;
        public InitiationStage stage;
    }

    public static class ClassIdentity
    {
        // Native 1.7 GetComp<T> assumes a non-empty comp list. Stat/repair hooks
        // also see editor-created and partially initialized pawns, so scan safely.
        public static CompClassProgression Component(Pawn pawn)
        {
            return ClassProgressionFeature.Enabled ? StoredComponent(pawn) : null;
        }

        internal static CompClassProgression StoredComponent(Pawn pawn)
        {
            var comps = pawn?.AllComps;
            if (comps == null) return null;
            for (int i = 0; i < comps.Count; i++)
                if (comps[i] is CompClassProgression progression) return progression;
            return null;
        }

        public static bool TryGetState(Pawn pawn, out ClassProgressionState state)
        {
            // Beta progression kill switch: even a pawn that somehow carries progression
            // state (edge cases around old feature-on saves) must resolve as classic
            // while the feature is disabled.
            if (!ClassProgressionFeature.Enabled) { state = null; return false; }
            state = Component(pawn)?.State;
            return state != null && state.Version == ClassProgressionState.CurrentVersion
                && state.Vocation != Vocation.None && (state.Vocation & ~Vocation.WarriorMage) == 0;
        }

        public static bool TryGetRole(Pawn pawn, Vocation role, out bool enabled)
        {
            enabled = false;
            if (!TryGetState(pawn, out var state)) return false;
            enabled = (state.Vocation & role) == role;
            return true;
        }

        public static bool IsPriest(Pawn pawn)
        {
            if (TryGetState(pawn, out var state)) return (state.Vocation & Vocation.Mage) != 0 && state.Find("rwom.priest")?.Domain == DisciplineDomain.Magic;
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Priest) == true;
        }

        public static bool IsChaosMage(Pawn pawn)
            => TryGetState(pawn, out var state) ? state.Find("rwom.chaos")?.Domain == DisciplineDomain.Magic
                : pawn?.story?.traits?.HasTrait(TorannMagicDefOf.ChaosMage) == true;

        public static bool IsFireMage(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.fire");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.InnerFire) == true;
        }

        public static bool IsIceMage(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.ice");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.HeartOfFrost) == true;
        }

        public static bool IsLightningMage(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.lightning");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.StormBorn) == true;
        }

        public static bool IsArcanist(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.arcanist");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Arcanist) == true;
        }

        public static bool IsPaladin(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.paladin");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Paladin) == true;
        }

        public static bool IsSummoner(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.summoner");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Summoner) == true;
        }

        public static bool IsDruid(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.druid");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Druid) == true;
        }

        public static bool IsTechnomancer(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.technomancer");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Technomancer) == true;
        }

        public static bool IsGeomancer(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.geomancer");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Geomancer) == true;
        }

        public static bool IsBloodMage(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.bloodmage");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.BloodMage) == true;
        }

        public static bool IsEnchanter(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.enchanter");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Enchanter) == true;
        }

        public static bool IsChronomancer(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.chronomancer");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Chronomancer) == true;
        }

        public static bool IsBrightmage(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.brightmage");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Brightmage) == true;
        }

        public static bool IsGolemancer(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.golemancer");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Golemancer) == true;
        }

        public static bool IsEmpath(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.empath");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Empath) == true;
        }

        public static bool IsShaman(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.shaman");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Shaman) == true;
        }

        public static bool IsBard(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.bard");
                return (state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Bard) == true;
        }

        public static bool IsNecromancer(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.necromancer");
                return state.Vocation == Vocation.Mage && state.Disciplines.Count == 1
                    && path?.Domain == DisciplineDomain.Magic
                    && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended;
            }
            if (pawn?.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Necromancer
                || t.def == TorannMagicDefOf.Lich) == true) return true;
            var comps = pawn?.AllComps;
            return comps != null && comps.Any(c =>
                (c is CompAbilityUserMagic magic && magic.customClass?.isNecromancer == true)
                || (c is CompAbilityUserMight might && might.customClass?.isNecromancer == true));
        }

        // Preserve callers that historically matched only the base trait, not
        // Lich or addon necromancers. New transformation handling is separate.
        public static bool IsBaseNecromancer(Pawn pawn)
            => TryGetState(pawn, out _) ? IsNecromancer(pawn) && !IsLich(pawn)
                : pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Necromancer) == true;

        public static bool IsLich(Pawn pawn)
        {
            if (TryGetState(pawn, out var state))
            {
                var path = state.Find("rwom.necromancer");
                return IsNecromancer(pawn) && path.Stage == InitiationStage.Ascended && path.LichFormActive;
            }
            return pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Lich) == true;
        }

        // New-model identity is explicit and never inferred from pawn gender.
        public static bool TryGetDemonBranch(Pawn pawn, out bool warlock)
        {
            warlock = false;
            if (!TryGetState(pawn, out var state) || state.Vocation != Vocation.Mage || state.Disciplines.Count != 1) return false;
            var path = state.Disciplines[0];
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || (path.ClassId != "rwom.warlock" && path.ClassId != "rwom.succubus")) return false;
            warlock = path.ClassId == "rwom.warlock";
            return true;
        }

        public static bool DemonEffect(Pawn pawn, bool warlock, bool legacyPredicate)
            => TryGetDemonBranch(pawn, out bool selected) ? selected == warlock : legacyPredicate;

        public static bool IsWarlock(Pawn pawn)
            => DemonEffect(pawn, true, pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Warlock) == true);

        public static bool IsSuccubus(Pawn pawn)
            => DemonEffect(pawn, false, pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Succubus) == true);

        public static ClassDisciplineDef Profile(string id)
            => DefDatabase<ClassDisciplineDef>.AllDefsListForReading.FirstOrDefault(d => d.profileId == id);

        public static string DisciplineLabel(string id)
        {
            var profile = Profile(id);
            // Reuse the translated original class name, not its trait membership.
            return profile?.legacyTrait?.degreeDatas?.FirstOrDefault()?.label ?? profile?.label ?? id ?? "?";
        }

        public static string Summary(ClassProgressionState state)
        {
            string vocation = ("TM_ProgressionVocation_" + state.Vocation).Translate();
            var names = state.Disciplines.Where(p => p != null).Select(DisplayLabel);
            return state.Disciplines.Count == 0 ? vocation : vocation + " — " + string.Join(" / ", names);
        }

        public static string Stages(ClassProgressionState state)
            => string.Join("\n", state.Disciplines.Where(p => p != null).Select(p =>
                DisplayLabel(p) + ": " + ("TM_ProgressionStage_" + p.Stage).Translate()));

        internal static string SummaryWithStages(ClassProgressionState state)
            => state.Disciplines.Count == 0 ? Summary(state) : string.Join(" / ",
                state.Disciplines.Where(p => p != null).Select(p => DisplayLabel(p)
                    + " (" + ("TM_ProgressionStage_" + p.Stage).Translate() + ")"));

        private static string DisplayLabel(DisciplineProgress path)
            => path.ClassId == "rwom.necromancer" && path.Domain == DisciplineDomain.Magic
                && path.Stage == InitiationStage.Ascended && path.LichFormActive
                ? TorannMagicDefOf.Lich?.DataAtDegree(0)?.label ?? DisciplineLabel(path.ClassId)
                : DisciplineLabel(path.ClassId);
    }
}
