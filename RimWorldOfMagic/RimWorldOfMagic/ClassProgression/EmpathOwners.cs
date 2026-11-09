using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class EmpathOwners
    {
        private static readonly string[] Names = { "TM_Empathy", "TM_MindKiller", "TM_SuppressiveAura", "TM_HarvestPassion", "TM_Symbiosis" };
        public static bool IsFamily(MagicPower power) => power?.TMabilityDefs?.Any(a => a != null && Names.Contains(a.defName)) == true;
        public static bool IsFade(MagicPower power) => power?.TMabilityDefs?.Contains(TorannMagicDefOf.TM_FadeEmotions) == true;
        private static bool ValidDefinition(string name, bool skilled)
        {
            var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
            var spec = DefDatabase<TM_CustomPowerDef>.GetNamedSilentFail(name)?.customPower;
            return ability != null && spec?.forMage == true && spec.abilityDefs?.Count == 1 && spec.abilityDefs[0] == ability
                && DefDatabase<TM_CustomPowerDef>.AllDefsListForReading.Count(d => d.customPower?.abilityDefs?.Contains(ability) == true) == 1
                && (skilled ? spec.skills?.Count == 3 && new[] { "_pwr", "_eff", "_ver" }.All(suffix => spec.skills.Count(s => s != null
                    && s.label == name + suffix && s.levelMax == (name == "TM_Symbiosis" ? 3 : 5) && s.costToLevel == 1) == 1)
                    : spec.skills == null || spec.skills.Count == 0);
        }
        public static List<MagicPower> Resolve(MagicData data)
        {
            if (data == null || Names.Any(name => !ValidDefinition(name, true))) return null;
            var pool = data.MagicPowersCustom;
            var result = new List<MagicPower>();
            foreach (string name in Names)
            {
                var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
                var matches = pool.Where(p => p?.TMabilityDefs?.Count == 1 && p.TMabilityDefs[0] == ability).ToList();
                if (matches.Count != 1) return null;
                result.Add(matches[0]);
            }
            return result;
        }
        public static bool IsOwner(MagicData data, MagicPower power) => Resolve(data)?.Any(p => ReferenceEquals(p, power)) == true;
        public static MagicPower FadeOwner(MagicData data)
        {
            if (data == null || !ValidDefinition("TM_FadeEmotions", false)) return null;
            var initialize = data.MagicPowersCustom;
            var matches = data.MagicPowersCustomStandalone.Where(p => p?.TMabilityDefs?.Count == 1 && p.TMabilityDefs[0] == TorannMagicDefOf.TM_FadeEmotions).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }
        public static bool CanLearn(Pawn pawn, MagicPower power)
        {
            if (!ClassIdentity.IsEmpath(pawn) || !ClassIdentity.TryGetState(pawn, out var state)) return false;
            var profile = ClassIdentity.Profile("rwom.empath"); var path = state.Find("rwom.empath");
            return profile?.domain == DisciplineDomain.Magic && IsOwner(pawn.GetCompAbilityUserMagic()?.MagicData, power)
                && profile.spells?.Any(s => s?.ability != null && power.TMabilityDefs.Contains(s.ability)
                    && s.stage >= InitiationStage.Foundation && s.stage <= InitiationStage.Ascended && path.Stage >= s.stage) == true;
        }
        public static bool CanLearnFade(Pawn pawn, MagicPower power)
            => ClassIdentity.IsEmpath(pawn) && ClassIdentity.Profile("rwom.empath")?.domain == DisciplineDomain.Magic
                && ReferenceEquals(FadeOwner(pawn.GetCompAbilityUserMagic()?.MagicData), power) && power != null;
    }
}
