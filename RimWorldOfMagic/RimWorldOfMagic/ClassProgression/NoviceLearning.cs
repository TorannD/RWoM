using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class NoviceLearning
    {
        private static readonly string[] ids = { "TM_ArcaneBolt", "TM_MageLight", "TM_Extinguish", "TM_Heater", "TM_Cooler", "TM_SmokeCloud" };
        public static IEnumerable<TMAbilityDef> Options => ids.Select(id => DefDatabase<TMAbilityDef>.GetNamedSilentFail(id)).Where(d => d != null);

        public static bool AlreadyKnown(Pawn pawn, TMAbilityDef ability)
        {
            var magic = pawn?.GetCompAbilityUserMagic();
            if (magic == null || ability == null) return false;
            if (magic.AbilityData.Powers.Any(p => p.Def == ability)
                || magic.MagicData.AllMagicPowers.Any(p => p.learned && p.TMabilityDefs.Contains(ability))) return true;
            switch (ability.defName)
            {
                case "TM_ArcaneBolt": return magic.spell_ArcaneBolt;
                case "TM_MageLight": return magic.spell_MageLight;
                case "TM_Extinguish": return magic.spell_Extinguish;
                case "TM_Heater": return magic.spell_Heater;
                case "TM_Cooler": return magic.spell_Cooler;
                case "TM_SmokeCloud": return magic.spell_SmokeCloud;
                default: return false;
            }
        }

        public static bool TryLearn(Pawn pawn, string id)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || !ids.Contains(id)
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return false;
            var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(id);
            var magic = pawn.GetCompAbilityUserMagic();
            if (ability == null || magic?.MagicData == null || state.NoviceSpells.Contains(id) || state.NoviceSpells.Count >= 2) return false;
            if (AlreadyKnown(pawn, ability) || !state.TryChooseNoviceSpell(id)) return false;
            var owner = magic.MagicData.AllMagicPowers.FirstOrDefault(p => p.TMabilityDefs.Contains(ability));
            if (owner != null) owner.learned = true;
            switch (id)
            {
                case "TM_ArcaneBolt": magic.spell_ArcaneBolt = true; break;
                case "TM_MageLight": magic.spell_MageLight = true; break;
                case "TM_Extinguish": magic.spell_Extinguish = true; break;
                case "TM_Heater": magic.spell_Heater = true; break;
                case "TM_Cooler": magic.spell_Cooler = true; break;
                case "TM_SmokeCloud": magic.spell_SmokeCloud = true; break;
            }
            ClassSpellAccess.RestoreLearned(magic);
            return true;
        }
    }
}
