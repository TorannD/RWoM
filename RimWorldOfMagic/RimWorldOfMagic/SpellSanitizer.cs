using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using UnityEngine;
using RimWorld;
using Verse;

namespace TorannMagic
{
    // Ф1 (Waylith адаптация): purge stale spell versions left in saved pawns by
    // older mod builds. Removed/renamed spell defs come back from the save as
    // null entries (LookMode.Def) and as runtime wrappers with a dead Def — the
    // source of the "red bugs autocasting healing" NRE family. Learned flags,
    // levels and points of surviving spells are never touched.
    public static class SpellSanitizer
    {
        public static void Sanitize(MagicPower power)
        {
            if (power?.TMabilityDefs == null) return;
            CompactRanks(power.TMabilityDefs, ref power.level, ref power.maxLevel, ref power.learned, ref power.autocast);
        }

        public static void Sanitize(MightPower power)
        {
            if (power?.TMabilityDefs == null) return;
            CompactRanks(power.TMabilityDefs, ref power.level, ref power.maxLevel, ref power.learned, ref power.autocast);
        }

        private static void CompactRanks(List<AbilityUser.AbilityDef> defs, ref int level, ref int maxLevel,
            ref bool learned, ref bool autocast)
        {
            int retained = 0;
            int selected = Mathf.Clamp(level, 0, defs.Count - 1);
            for (int i = 0; i < defs.Count && i <= selected; i++)
                if (defs[i] != null) retained++;
            defs.RemoveAll(def => def == null);
            maxLevel = System.Math.Max(0, defs.Count - 1);
            level = System.Math.Max(0, retained - 1);
            if (retained == 0) { learned = false; autocast = false; }
        }

        // Runtime wrappers whose spell def died with an old version: dropped so
        // the autocast dispatch and UI never dereference them.
        public static void Sanitize(List<PawnAbility> powers)
        {
            if (powers == null) return;
            powers.RemoveAll(p => p == null || p.Def == null);
        }

        // Missing skill-label entries resolve to a default level instead of the
        // unguarded FirstOrDefault(...).level NRE pattern.
        public static int SkillLevelOrDefault(List<MagicPowerSkill> skills, string label, int defaultLevel = 0)
        {
            var skill = skills?.FirstOrDefault(x => x != null && x.label == label);
            return skill?.level ?? defaultLevel;
        }
    }
}
