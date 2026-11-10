using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    public static class FleshPresentation
    {
        public static readonly Color Border = new Color(.60f, .18f, .20f, .9f);
        public static readonly Color Corner = new Color(.95f, .35f, .38f, .95f);
        public static bool Eligible(Pawn pawn) => pawn != null && !pawn.Dead && pawn.Faction?.IsPlayer == true && FleshRepository.IsRaisedUndead(pawn) && FleshRepository.TryGet(pawn) != null;
        public static bool ShowOnBar(Pawn pawn) => Eligible(pawn) && ModOptions.Settings.Instance.grimoireOnAbilityBar;
        public static bool ShowTab(Pawn pawn) => Eligible(pawn) && !ModOptions.Settings.Instance.grimoireOnAbilityBar;
        public static bool IsActive(FleshSkill skill) => skill >= FleshSkill.Devour && skill <= FleshSkill.Feast;
        public static string SkillKindKey(FleshSkill skill) => IsActive(skill) ? "TM_Flesh_ActiveKind" : "TM_Flesh_PassiveKind";
        public static string SkillLabel(FleshSkill skill) => ("TM_Flesh_" + skill).Translate() + " (" + SkillKindKey(skill).Translate() + ")";
        public static string UpgradeLabelKey(FleshProgress data, FleshSkill skill) => data.GetRank(skill) >= FleshProgress.MaxRank(skill) ? "TM_Flesh_MaxRank" : data.SkillPoints <= 0 ? "TM_Flesh_NoPoints" : "TM_Flesh_Upgrade";
        public static float ThirstFraction(FleshProgress data) => data == null ? 0f : Mathf.Clamp01(data.Thirst / data.MaxThirst);
        public static IEnumerable<FleshSkill> ActiveSkills(Pawn pawn)
        {
            if (!Eligible(pawn)) yield break;
            var data = FleshRepository.TryGet(pawn);
            for (FleshSkill skill = FleshSkill.Devour; skill <= FleshSkill.Feast; skill++)
                if (data.GetRank(skill) > 0) yield return skill;
        }
        public static void Open(Pawn pawn)
        {
            if (!Eligible(pawn)) return;
            Find.WindowStack.Windows.OfType<Window_FleshGrimoire>().FirstOrDefault()?.Close();
            Find.WindowStack.Add(new Window_FleshGrimoire(pawn));
        }
        public static void Activate(Pawn pawn, FleshSkill skill)
        {
            if (!Eligible(pawn) || !FleshActions.CanActivate(pawn, skill)) return;
            if (skill == FleshSkill.Devour)
                Find.Targeter.BeginTargeting(new TargetingParameters {
                    canTargetPawns = true, canTargetCorpses = true, canTargetItems = false,
                    canTargetBuildings = false, canTargetLocations = false,
                    mapObjectTargetsMustBeAutoAttackable = false,
                    validator = target => FleshActions.ValidTarget(pawn, target.Thing)
                }, target => FleshActions.TryDevour(pawn, target.Thing));
            else if (JobDriver_FleshCast.PreparationTicks(skill) > 0) JobDriver_FleshCast.TryStart(pawn, skill);
            else FleshActions.TryActivate(pawn, skill);
        }
        public static string ActiveIcon(FleshSkill skill)
        {
            switch (skill) {
                case FleshSkill.Devour: return "01-devour-flesh";
                case FleshSkill.Restore: return "02-restore-flesh";
                case FleshSkill.Empower: return "03-empower-flesh";
                case FleshSkill.Harden: return "04-harden-skin";
                default: return "05-feast-of-flesh";
            }
        }
    }
}
