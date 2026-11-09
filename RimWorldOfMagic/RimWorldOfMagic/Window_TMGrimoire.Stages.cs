using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public partial class Window_TMGrimoire
    {
        // Presentation only: keep access, imported ownership and saved ranks unchanged.
        internal static string StageSuffix(ClassProgressionState state, IEnumerable<AbilityDef> family, bool learned)
        {
            if (state == null || family == null) return "";
            var definitions = new HashSet<AbilityDef>(family);
            var matches = state.Disciplines.Where(p => p != null)
                .SelectMany(path => (ClassIdentity.Profile(path.ClassId)?.spells ?? new List<DisciplineSpell>())
                    .Where(s => s?.ability != null && definitions.Contains(s.ability))
                    .Select(s => new { path, s.stage }))
                .Where(s => s.stage >= InitiationStage.Foundation && s.stage <= InitiationStage.Ascended)
                .ToList();
            if (matches.Count == 0) return "";
            // A shared spell can be unlocked through either of the pawn's paths.
            var available = matches.Where(m => m.path.Stage >= m.stage).ToList();
            var required = (available.Count > 0 ? available : matches).Min(m => m.stage);
            string stage = ("TM_ProgressionStage_" + required).Translate();
            return " (" + stage + (!learned && available.Count == 0
                ? "; " + "TM_GrimoireStageTooLow".Translate() : "") + ")";
        }

        private string StageTitle(string label, IEnumerable<AbilityDef> family, int level, int maxLevel, bool learned)
        {
            ClassIdentity.TryGetState(pawn, out var state);
            return PowerTitle(label + StageSuffix(state, family, learned), level, maxLevel, learned);
        }

        private string MagicTitle(MagicPower power) => StageTitle(power.abilityDef.LabelCap,
            power.TMabilityDefs, power.level, power.maxLevel, power.learned);

        private string MightTitle(MightPower power)
        {
            var ability = (TMAbilityDef)power.abilityDef;
            string label = TrailLore(ability) ? "TM_TrailLoreName".Translate() : BladeMemory(ability)
                ? "TM_BladeMemoryName".Translate() : PatientAim(ability) ? "TM_PatientAimName".Translate() : ability.LabelCap;
            return StageTitle(label, power.TMabilityDefs, power.level, power.maxLevel, power.learned);
        }

        private static float PowerHeaderHeight(string title, float cardWidth)
        {
            Text.Font = GameFont.Small;
            return Mathf.Max(32f, Text.CalcHeight(title, Mathf.Max(40f, cardWidth - IconSize - 170f)) + 16f);
        }
    }
}
