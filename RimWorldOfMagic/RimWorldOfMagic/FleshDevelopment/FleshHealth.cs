using System;
using System.Linq;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    // Descriptive only: gameplay bonuses remain exclusively in FleshStatPatches.
    public sealed class Hediff_FleshDevelopment : Hediff
    {
        public FleshSkill? Skill => FleshHealth.SkillFor(def?.defName);
        private FleshProgress Progress => FleshRepository.TryGet(pawn);
        public int DisplayLevel => Skill.HasValue ? Progress?.GetRank(Skill.Value) ?? 0 : Progress?.Level ?? 1;
        public override bool ShouldRemove => !FleshRepository.IsRaisedUndead(pawn) || (Skill.HasValue && (DisplayLevel <= 0 || (FleshHealth.IsActiveSkill(Skill.Value) && !FleshHealth.IsActive(Progress, Skill.Value))));
        public override bool Visible => !ShouldRemove;
        public override string SeverityLabel => null;
        public override string LabelBase => Skill.HasValue ? ("TM_Flesh_" + Skill.Value).Translate().ToString() : "TM_Flesh_Title".Translate().ToString();
        public override string LabelInBrackets
        {
            get
            {
                if (!Skill.HasValue) return "TM_Flesh_HealthLevel".Translate(DisplayLevel).ToString();
                string rank = "TM_Flesh_HealthRank".Translate(DisplayLevel, FleshProgress.MaxRank(Skill.Value)).ToString();
                if (FleshHealth.IsActiveSkill(Skill.Value)) return rank + ", " + FleshHealth.Duration(Progress, Skill.Value);
                return rank + ", +" + FleshHealth.Bonus(Skill.Value, DisplayLevel) + (Skill == FleshSkill.Capacity ? "" : "%");
            }
        }
        public override string TipStringExtra
        {
            get
            {
                var p = Progress;
                if (p == null) return "";
                if (!Skill.HasValue) return "TM_Flesh_Stats".Translate(p.Level, p.Experience.ToString("0.#"), p.RequiredExperience.ToString("0.#"), p.SkillPoints) + "\n" + "TM_Flesh_ThirstTip".Translate() + "\n\n" + "TM_Flesh_BiteRules".Translate();
                if (FleshHealth.IsActiveSkill(Skill.Value))
                    return LabelInBrackets + "\n" + ("TM_Flesh_Health" + Skill.Value).Translate(
                        Skill == FleshSkill.Empower ? 10 * DisplayLevel : 5 + 15 * DisplayLevel,
                        p.Thirst.ToString("0.#"), p.MaxThirst) + "\n\n" + ("TM_Flesh_" + Skill.Value + "Desc").Translate();
                string bonus = ("TM_Flesh_Health" + Skill.Value).Translate(FleshHealth.Bonus(Skill.Value, DisplayLevel), p.MaxThirst).ToString();
                return LabelInBrackets + "\n" + bonus + "\n\n" + ("TM_Flesh_" + Skill.Value + "Desc").Translate();
            }
        }
    }

    public static class FleshHealth
    {
        public const string LevelDef = "TM_FleshProgressHD";
        public static readonly FleshSkill[] Passives = { FleshSkill.Mobility, FleshSkill.Strength, FleshSkill.Industry, FleshSkill.Capacity };
        public static readonly FleshSkill[] Actives = { FleshSkill.Empower, FleshSkill.Harden, FleshSkill.Feast };
        public static bool IsActiveSkill(FleshSkill skill) => skill == FleshSkill.Empower || skill == FleshSkill.Harden || skill == FleshSkill.Feast;
        public static bool IsActive(FleshProgress p, FleshSkill skill) => p != null && (skill == FleshSkill.Empower ? p.EmpowerUntil > FleshActions.Now : skill == FleshSkill.Harden ? p.HardenUntil > FleshActions.Now : skill == FleshSkill.Feast && (p.FeastUntil > FleshActions.Now || p.FeastRage));
        public static string Duration(FleshProgress p, FleshSkill skill)
        {
            if (p == null) return "";
            if (skill == FleshSkill.Feast && p.FeastRage) return "TM_Flesh_HealthRage".Translate().ToString();
            int until = skill == FleshSkill.Empower ? p.EmpowerUntil : skill == FleshSkill.Harden ? p.HardenUntil : p.FeastUntil;
            return "TM_Flesh_HealthRemaining".Translate(Math.Max(0, (int)Math.Ceiling((until - FleshActions.Now) / 60f))).ToString();
        }
        public static string DefName(FleshSkill skill) => "TM_Flesh" + skill + "HD";
        public static FleshSkill? SkillFor(string name)
        {
            foreach (var skill in Passives) if (name == DefName(skill)) return skill;
            foreach (var skill in Actives) if (name == DefName(skill)) return skill;
            return null;
        }
        // Percent for the three stat bonuses, points for capacity. No rounding drift in tooltips.
        public static int Bonus(FleshSkill skill, int rank) => rank * (skill == FleshSkill.Strength ? 3 : skill == FleshSkill.Capacity ? 10 : 2);
        public static void Sync(Pawn pawn)
        {
            var p = FleshRepository.TryGet(pawn);
            FleshStatPatches.Refresh(pawn);
            if (p == null || pawn?.health?.hediffSet == null || !FleshRepository.IsRaisedUndead(pawn)) return;
            Ensure(pawn, LevelDef);
            foreach (var skill in Passives)
                if (p.GetRank(skill) > 0) Ensure(pawn, DefName(skill));
            foreach (var skill in Actives)
                if (p.GetRank(skill) > 0 && IsActive(p, skill)) Ensure(pawn, DefName(skill));
            // Reconcile accidental duplicates/removed ranks without touching unrelated conditions.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var h in pawn.health.hediffSet.hediffs.OfType<Hediff_FleshDevelopment>().ToList())
                if (h.ShouldRemove || !seen.Add(h.def.defName)) pawn.health.RemoveHediff(h);
        }
        private static void Ensure(Pawn pawn, string name)
        {
            var def = DefDatabase<HediffDef>.GetNamedSilentFail(name);
            if (def == null || pawn.health.hediffSet.HasHediff(def)) return;
            pawn.health.AddHediff(def);
        }
    }
}
