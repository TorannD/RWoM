using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using TorannMagic.Compatibility;
using TorannMagic.Utils;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ClassEnrollment
    {
        private static readonly HashSet<Pawn> inFlight = new HashSet<Pawn>();
        private static readonly FieldInfo magicDataField = AccessTools.Field(typeof(CompAbilityUserMagic), "magicData");
        private static readonly FieldInfo mightDataField = AccessTools.Field(typeof(CompAbilityUserMight), "mightData");
        private static readonly FieldInfo runtimeDataField = AccessTools.Field(typeof(CompAbilityUser), "abilityData");
        private static readonly FieldInfo[] magicFlags = OwnershipFlags(typeof(CompAbilityUserMagic));
        private static readonly FieldInfo[] mightFlags = OwnershipFlags(typeof(CompAbilityUserMight));
        private static FieldInfo[] OwnershipFlags(System.Type type) => type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(bool) && (f.Name.StartsWith("spell_") || f.Name.StartsWith("skill_") || f.Name == "recallSpell")).ToArray();

        public static bool CanEnrollMage(Pawn pawn) => FreshPotential(pawn, TorannMagicDefOf.TM_Gifted);
        internal static bool UnenrolledWarrior(Pawn pawn) => FreshPotential(pawn, TorannMagicDefOf.PhysicalProdigy);

        private static bool FreshPotential(Pawn pawn, TraitDef potential)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.RaceProps?.Humanlike != true
                || pawn.story?.traits == null || !pawn.IsColonistPlayerControlled
                || pawn.IsShambler || pawn.IsGhoul || pawn.health == null || pawn.needs == null
                || ClassIdentity.Component(pawn) == null || ClassIdentity.Component(pawn).State != null
                || DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage") == null) return false;
            var traits = pawn.story.traits.allTraits;
            if (potential == null || !traits.Any(t => t.def == potential && !t.Suppressed)
                || traits.Any(t => t.def?.defName?.StartsWith("TM_Vocation") == true)) return false;
            // Match declarations regardless of degree, suppression or enabled settings:
            // this is enrollment, never a migration of a partially recognized class.
            if (traits.Any(t => TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def?.defName)
                || TM_ClassGroups.MightEntriesField.Any(e => e.trait == t.def?.defName)
                || (TM_ClassUtility.CustomClasses?.Any(c => c?.classTrait == t.def) ?? false))) return false;
            return HasNoPriorOwnership(pawn);
        }

        internal static bool HasNoPriorOwnership(Pawn pawn)
        {
            foreach (var comp in pawn.AllComps.Where(c => c is CompAbilityUserMagic || c is CompAbilityUserMight))
            {
                var field = comp is CompAbilityUserMagic ? magicDataField : mightDataField;
                if (field == null || runtimeDataField == null || field.GetValue(comp) != null) return false;
                var runtime = runtimeDataField.GetValue(comp) as AbilityData;
                if (runtime?.Count > 0) return false;
                if ((comp is CompAbilityUserMagic ? magicFlags : mightFlags).Any(f => (bool)f.GetValue(comp))) return false;
            }
            return true;
        }

        internal static bool HasChosenClass(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return false;
            // The beta switch suspends a discipline, but the saved owner still exists.
            var state = pawn.AllComps?.OfType<CompClassProgression>().FirstOrDefault()?.State;
            return state?.Disciplines.Count > 0 || ClassInitiation.HasLegacyClass(pawn)
                || pawn.story.traits.allTraits.Any(t => t.def?.defName == "TM_Empath" || t.def == TorannMagicDefOf.TM_Possessor
                    || (TM_ClassUtility.CustomClasses?.Any(c => c?.classTrait == t.def) ?? false));
        }

        internal static bool CanBestow(Pawn pawn, bool magic)
        {
            if (pawn?.story?.traits == null || pawn.Dead || HasChosenClass(pawn)) return false;
            var traits = pawn.story.traits;
            var gift = magic ? TorannMagicDefOf.TM_Gifted : TorannMagicDefOf.PhysicalProdigy;
            var vocation = magic ? "TM_VocationMage" : "TM_VocationWarrior";
            return gift != null && !traits.allTraits.Any(t => t.def == gift || t.def.defName == vocation || t.def.defName == "TM_VocationWarriorMage");
        }

        internal static void ConsumePotentials(Pawn pawn)
        {
            var traits = pawn.story?.traits;
            if (traits == null) return;
            foreach (var potential in traits.allTraits.Where(t => t.def == TorannMagicDefOf.TM_Gifted
                || t.def == TorannMagicDefOf.PhysicalProdigy).ToList())
                traits.RemoveTrait(potential);
        }

        internal static void FinishMageFoundation(Pawn pawn)
        {
            var traits = pawn.story?.traits;
            if (traits == null) return;
            bool pendingGift = traits.HasTrait(TorannMagicDefOf.TM_Gifted)
                || traits.HasTrait(TorannMagicDefOf.PhysicalProdigy);
            ConsumePotentials(pawn);
            if (!pendingGift) return;
            var vocation = DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage");
            if (vocation != null && !traits.HasTrait(vocation)) traits.GainTrait(new Trait(vocation));
        }

        internal static bool CanChooseWanderer(Pawn pawn)
            => pawn?.Spawned == true && !pawn.Dead && !pawn.Downed && pawn.IsColonistPlayerControlled
                && pawn.story?.traits != null && (ClassIdentity.TryGetState(pawn, out var state)
                    ? state.Disciplines.Count == 0 && !ClassInitiation.HasLegacyClass(pawn) : UnenrolledWarrior(pawn))
                && DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarriorMage") != null;

        internal static bool ChooseWanderer(Pawn pawn, Vocation route)
            => StartWanderer(pawn, route, false);

        internal static bool ChooseGeneratedWanderer(Pawn pawn, Vocation route)
            => StartWanderer(pawn, route, true);

        private static bool EligibleGeneratedWanderer(Pawn pawn)
            => ClassIdentity.Component(pawn) is CompClassProgression comp && comp.State == null
                && pawn.story?.traits != null && pawn.needs != null && pawn.health != null && !pawn.Dead
                && !HasChosenClass(pawn) && HasNoPriorOwnership(pawn);

        private static bool StartWanderer(Pawn pawn, Vocation route, bool generated)
        {
            bool Available() => generated ? EligibleGeneratedWanderer(pawn) : CanChooseWanderer(pawn);
            if (!Available() || route == Vocation.None || (route & ~Vocation.WarriorMage) != 0) return false;
            var chosenTrait = DefDatabase<TraitDef>.GetNamedSilentFail("TM_Vocation" + route);
            if (chosenTrait == null) return false;
            TM_PawnTracker.EnsureClassComps(pawn);
            var magic = pawn.GetCompAbilityUserMagic();
            var might = pawn.GetCompAbilityUserMight();
            if (magic == null || might == null || !Available()) return false;
            var magicDefs = new[] { TorannMagicDefOf.TM_WandererCraft, TorannMagicDefOf.TM_Cantrips };
            var mightDefs = new[] { TorannMagicDefOf.TM_WayfarerCraft, TorannMagicDefOf.TM_FieldTraining };
            if (magicDefs.Any(d => d == null) || mightDefs.Any(d => d == null)) return false;
            var existingMagic = magic.ExistingMagicData?.MagicPowersW;
            var existingMight = might.ExistingMightData?.MightPowersW;
            if (existingMagic != null && magicDefs.Any(d => existingMagic.Count(p => p?.abilityDef == d) != 1)
                || existingMight != null && mightDefs.Any(d => existingMight.Count(p => p?.abilityDef == d) != 1)) return false;
            var progression = ClassIdentity.Component(pawn);
            if (progression.State == null && !progression.BeginNewProgression(route)) return false;
            if (!progression.State.ChooseWanderer(route)) return false;
            if ((route & Vocation.Mage) != 0) foreach (var power in magic.MagicData.MagicPowersW.Where(p => magicDefs.Contains(p.abilityDef))) power.learned = true;
            if ((route & Vocation.Warrior) != 0) foreach (var power in might.MightData.MightPowersW.Where(p => mightDefs.Contains(p.abilityDef))) power.learned = true;
            ConsumePotentials(pawn);
            var traits = pawn.story.traits;
            foreach (var old in traits.allTraits.Where(t => t.def != chosenTrait && t.def.defName.StartsWith("TM_Vocation")).ToList())
                traits.RemoveTrait(old);
            if (!traits.HasTrait(chosenTrait)) traits.GainTrait(new Trait(chosenTrait));
            TM_PawnTracker.ResolveComps(pawn);
            TM_LegacyRecovery.Repair(pawn);
            ClassIdentity.Component(pawn).SynchronizeHealthDisplay();
            return true;
        }

        public static bool TryEnrollMage(Pawn pawn)
        {
            if (!CanEnrollMage(pawn) || !inFlight.Add(pawn)) return false;
            try
            {
                if (!pawn.AllComps.OfType<CompAbilityUserMagic>().Any()) TM_PawnTracker.EnsureClassComps(pawn);
                if (!CanEnrollMage(pawn)) return false;
                var magic = pawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
                if (magic == null) return false;
                var vocation = DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage");
                var gifted = pawn.story.traits.allTraits.First(t => t.def == TorannMagicDefOf.TM_Gifted && !t.Suppressed);
                // Commit identity before trait hooks; they must see a new empty Mage,
                // not infer a lost legacy class and seed its default spells.
                if (!ClassIdentity.Component(pawn).BeginNewProgression(Vocation.Mage)) return false;
                if (!pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
                {
                    pawn.story.traits.RemoveTrait(gifted);
                    pawn.story.traits.GainTrait(new Trait(vocation));
                }
                TM_PawnTracker.ResolveComps(pawn);
                // Missing data initialization is allowed only after the read-only
                // preflight proved there was no previous progression to migrate.
                TM_LegacyRecovery.Repair(pawn);
                pawn.Notify_DisabledWorkTypesChanged();
                pawn.skills?.Notify_SkillDisablesChanged();
                return true;
            }
            finally { inFlight.Remove(pawn); }
        }
    }
}
