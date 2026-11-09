using System.Linq;
using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class LichTransformation
    {
        public static bool TryTransform(Pawn pawn, PawnAbility ability)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.health?.hediffSet == null
                || !ClassIdentity.TryGetState(pawn, out var state) || !ClassIdentity.IsNecromancer(pawn)
                || ClassIdentity.IsLich(pawn) || ClassInitiation.HasLegacyClass(pawn)) return false;
            var path = state.Find("rwom.necromancer");
            var magic = pawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (path.Stage != InitiationStage.Ascended || magic == null || ability?.Def != TorannMagicDefOf.TM_LichForm
                || ability.AbilityUser != magic || !magic.AbilityData.Powers.Any(a => ReferenceEquals(a, ability))
                || !magic.MagicData.MagicPowersN.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LichForm))
                || DefDatabase<HediffDef>.GetNamedSilentFail("TM_LichHD") == null) return false;
            if (!state.ActivateLichForm()) return false;
            // Commit form authority first. Restoration is repeatable and never edits
            // passions, timetable, work priorities, XP, ranks or existing points.
            ClassSpellAccess.RestoreLearned(magic);
            pawn.Notify_DisabledWorkTypesChanged();
            pawn.skills?.Notify_SkillDisablesChanged();
            pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            ClassIdentity.Component(pawn).SynchronizeHealthDisplay();
            return true;
        }

        public static void Restore(CompAbilityUserMagic magic)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(magic?.Pawn)) return;
            if (magic == null || !ClassIdentity.TryGetState(magic.Pawn, out _) || !ClassIdentity.IsLich(magic.Pawn)) return;
            RestoreHealth(magic.Pawn);
            SetFormPowers(magic);
        }

        private static MagicPower SetFormPowers(CompAbilityUserMagic magic)
        {
            var deathBolt = magic.MagicData.MagicPowersN.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_DeathBolt));
            if (deathBolt != null) deathBolt.learned = true;
            magic.spell_Flight = true;
            magic.spell_LichForm = false;
            if (magic.AbilityData.Powers.Any(a => a.Def == TorannMagicDefOf.TM_LichForm))
                magic.RemovePawnAbility(TorannMagicDefOf.TM_LichForm);
            return deathBolt;
        }

        // Only an explicit dev grant creates a completed classic form. Loading a
        // partial old transformation must not teach spells that were never earned.
        internal static bool GiveClassicDebugForm(CompAbilityUserMagic magic)
        {
            var pawn=magic?.Pawn;
            if (magic?.ExistingMagicData==null || pawn?.story?.traits?.HasTrait(TorannMagicDefOf.Lich)!=true ||
                ClassIdentity.TryGetState(pawn,out _) || TorannMagicDefOf.TM_LichHD==null) return false;
            var bolt=SetFormPowers(magic);
            if (bolt?.abilityDef==null) return false;
            if (!pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LichHD))
                HealthUtility.AdjustSeverity(pawn,TorannMagicDefOf.TM_LichHD,.5f);
            TM_ClassState.AddRuntimeAbility(magic,bolt.abilityDef);
            TM_ClassState.AddRuntimeAbility(magic,TorannMagicDefOf.TM_Flight);
            pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            return true;
        }

        public static void RestoreHealth(Pawn pawn)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(pawn)) return;
            if (!ClassIdentity.TryGetState(pawn, out _) || !ClassIdentity.IsLich(pawn) || pawn?.health?.hediffSet == null) return;
            var def = DefDatabase<HediffDef>.GetNamedSilentFail("TM_LichHD");
            if (def != null && !pawn.health.hediffSet.HasHediff(def)) HealthUtility.AdjustSeverity(pawn, def, .5f);
        }
    }
}
