using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TorannMagic.FleshDevelopment
{
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValue), new[] { typeof(StatRequest), typeof(bool) })]
    public static class FleshStatPatches
    {
        public static readonly string[] AffectedStats = { "MoveSpeed", "WorkSpeedGlobal", "MeleeDamageFactor", "ArmorRating_Sharp", "ArmorRating_Blunt" };
        private sealed class Stamp { public int Value = -1; }
        private static readonly ConditionalWeakTable<Pawn, Stamp> stamps = new ConditionalWeakTable<Pawn, Stamp>();
        public static float ThirstFactor(FleshProgress p) => p.Thirst >= p.MaxThirst ? .8f : p.Thirst >= .8f * p.MaxThirst ? .9f : 1f;
        // Only discrete stat changes invalidate caches; the normal per-tick thirst increase does not.
        public static void Refresh(Pawn pawn)
        {
            var p = FleshRepository.TryGet(pawn);
            if (pawn == null || p == null) return;
            int value = p.GetRank(FleshSkill.Mobility) | (p.GetRank(FleshSkill.Industry) << 4) | (p.GetRank(FleshSkill.Strength) << 8)
                | ((p.EmpowerUntil > FleshActions.Now ? p.GetRank(FleshSkill.Empower) : 0) << 12)
                | ((p.HardenUntil > FleshActions.Now ? p.GetRank(FleshSkill.Harden) : 0) << 14)
                | ((p.Thirst >= p.MaxThirst ? 2 : p.Thirst >= .8f * p.MaxThirst ? 1 : 0) << 16)
                | (FleshRepository.IsRaisedUndead(pawn) ? 1 << 18 : 0);
            var stamp = stamps.GetOrCreateValue(pawn);
            if (stamp.Value == value) return;
            stamp.Value = value;
            var names = new HashSet<string>(AffectedStats);
            // Derived work stats cache their final result too. Invalidate dependents, but never apply a second multiplier.
            bool added;
            do
            {
                added = false;
                foreach (var def in DefDatabase<StatDef>.AllDefsListForReading)
                    if (!names.Contains(def.defName) && def.postProcessStatFactors != null && def.postProcessStatFactors.Exists(s => names.Contains(s.defName)))
                        added |= names.Add(def.defName);
            } while (added);
            foreach (var def in DefDatabase<StatDef>.AllDefsListForReading)
                if (names.Contains(def.defName)) def.Worker.ClearCacheForThing(pawn);
        }
        public static float Adjust(FleshProgress p, string stat, float value, int now)
        {
            float thirstPenalty = ThirstFactor(p);
            float empower = p.EmpowerUntil > now ? .1f * p.GetRank(FleshSkill.Empower) : 0f;
            switch (stat)
            {
                case "MoveSpeed": return value * (1f + .02f * p.GetRank(FleshSkill.Mobility) + empower) * thirstPenalty;
                case "WorkSpeedGlobal": return value * (1f + .02f * p.GetRank(FleshSkill.Industry) + empower) * thirstPenalty;
                case "MeleeDamageFactor": return value * (1f + .03f * p.GetRank(FleshSkill.Strength));
                case "ArmorRating_Sharp": case "ArmorRating_Blunt":
                    return value + (p.HardenUntil > now ? .05f + .15f * p.GetRank(FleshSkill.Harden) : 0f);
                default: return value;
            }
        }
        public static void Postfix(StatRequest req, StatDef ___stat, ref float __result)
        {
            if (!(req.Thing is Pawn pawn) || !FleshRepository.IsRaisedUndead(pawn)) return;
            var p = FleshRepository.TryGet(pawn);
            if (p != null) __result = Adjust(p, ___stat.defName, __result, FleshActions.Now);
        }
        public static string Explanation(Pawn pawn, string stat)
        {
            if (!FleshRepository.IsRaisedUndead(pawn)) return null;
            var p = FleshRepository.TryGet(pawn);
            if (p == null || Array.IndexOf(AffectedStats, stat) < 0) return null;
            var lines = new List<string>();
            if (stat == "MoveSpeed" || stat == "WorkSpeedGlobal" || stat == "MeleeDamageFactor")
            {
                var skill = stat == "MoveSpeed" ? FleshSkill.Mobility : stat == "WorkSpeedGlobal" ? FleshSkill.Industry : FleshSkill.Strength;
                int rank = p.GetRank(skill);
                if (rank > 0) lines.Add(("TM_Flesh_" + skill).Translate() + " (" + rank + "): +" + FleshHealth.Bonus(skill, rank) + "%");
                if (stat != "MeleeDamageFactor")
                {
                    if (p.EmpowerUntil > FleshActions.Now) lines.Add("TM_Flesh_Empower".Translate() + " (" + p.GetRank(FleshSkill.Empower) + "): +" + 10 * p.GetRank(FleshSkill.Empower) + "%");
                    float penalty = ThirstFactor(p);
                    if (penalty < 1f) lines.Add("TM_Flesh_ThirstLabel".Translate() + ": ×" + penalty.ToStringPercent());
                }
                lines.Add("TM_Flesh_StatTotal".Translate(Adjust(p, stat, 1f, FleshActions.Now).ToStringPercent()).ToString());
            }
            else if (p.HardenUntil > FleshActions.Now)
                lines.Add("TM_Flesh_Harden".Translate() + " (" + p.GetRank(FleshSkill.Harden) + "): +" + Adjust(p, stat, 0f, FleshActions.Now).ToStringPercent());
            return lines.Count == 0 ? null : "TM_Flesh_Title".Translate() + ":\n  " + string.Join("\n  ", lines);
        }
        [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetExplanationFinalizePart))]
        public static class ExplanationPatch
        {
            public static void Postfix(StatRequest req, StatDef ___stat, ref string __result)
            {
                string explanation = Explanation(req.Thing as Pawn, ___stat.defName);
                if (explanation != null) __result = explanation + "\n\n" + __result;
            }
        }
    }
}
