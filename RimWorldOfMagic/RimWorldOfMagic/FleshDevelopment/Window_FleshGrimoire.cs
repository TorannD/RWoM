using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    public class Window_FleshGrimoire : Window
    {
        private readonly Pawn pawn;
        private Vector2 scroll;
        private static readonly FleshSkill[] Skills = { FleshSkill.Devour, FleshSkill.Restore, FleshSkill.Empower, FleshSkill.Harden, FleshSkill.Mobility, FleshSkill.Strength, FleshSkill.Industry, FleshSkill.Capacity, FleshSkill.Feast };
        private static readonly string[] Icons = { "01-devour-flesh", "02-restore-flesh", "03-empower-flesh", "04-harden-skin", "06-mobile-flesh", "07-mighty-flesh", "08-industrious-flesh", "09-flesh-capacity", "05-feast-of-flesh" };
        public override Vector2 InitialSize => new Vector2(880f, 720f);

        public Window_FleshGrimoire(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            closeOnCancel = true;
            closeOnAccept = false;
            absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            var data = FleshRepository.TryGet(pawn);
            if (data == null || !FleshRepository.IsRaisedUndead(pawn)) { Close(); return; }
            bool control = pawn.Faction == Faction.OfPlayer && !pawn.Dead;
            int now = Find.TickManager.TicksGame;
            var viewport = rect;
            float width = rect.width - 20;
            var view = new Rect(0, 0, width, Skills.Length * 250f + 500f);
            Widgets.BeginScrollView(viewport, ref scroll, view);
            rect.width = width;
            Utils.TMIsekaiUI.Panel(new Rect(0, 36, width, 96), FleshPresentation.Border, FleshPresentation.Corner);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, rect.width - 35, 36), "TM_Flesh_Title".Translate() + " — " + pawn.LabelShortCap);
            Text.Font = GameFont.Small;
            bool debugLevel = DebugSettings.godMode && control;
            Widgets.Label(new Rect(12, 40, rect.width - 24 - (debugLevel ? 150 : 0), 26), "TM_Flesh_Stats".Translate(data.Level, data.Experience.ToString("0.#"), data.RequiredExperience.ToString("0.#"), data.SkillPoints));
            if (debugLevel && Widgets.ButtonText(new Rect(rect.width - 152, 40, 140, 26), "TM_Flesh_DebugLevelUp".Translate())) FleshActions.DebugLevelUp(pawn);
            Bar(new Rect(12, 72, rect.width - 24, 24), data.Experience / data.RequiredExperience, new Color(.75f, .58f, .2f), "TM_Flesh_Experience".Translate());
            Bar(new Rect(12, 102, rect.width - 24, 24), data.Thirst / data.MaxThirst, new Color(.65f, .16f, .2f), "TM_Flesh_Thirst".Translate(data.Thirst.ToString("0.#"), data.MaxThirst.ToString("0")));
            var owner = FleshRepository.Owner(pawn);
            bool canStop = owner != null && owner.Faction == Faction.OfPlayer && !owner.Dead && (data.FeastUntil > now || data.FeastRage);
            GUI.enabled = canStop;
            if (Widgets.ButtonText(new Rect(0, 137, 280, 30), "TM_Flesh_Stop".Translate())) FleshMultiplayer.StopFeast(pawn);
            GUI.enabled = true;
            Widgets.Label(new Rect(295, 138, rect.width - 295, 36), data.FeastRage ? "TM_Flesh_Rage".Translate() : "TM_Flesh_StopDesc".Translate());
            for (int i = 0; i < Skills.Length; i++) Card(new Rect(0, 190 + i * 250, width, 240), Skills[i], Icons[i], data, now, control);
            float y = 190 + Skills.Length * 250;
            Widgets.Label(new Rect(0, y, width, 30), "TM_Flesh_Gate".Translate());
            for (int i = 0; i < Skills.Length - 1; i++) {
                var skill = Skills[i]; int cap = FleshProgress.MaxRank(skill);
                Widgets.Label(new Rect(10, y + 32 + i * 27, width - 20, 27), (data.GetRank(skill) >= cap ? "✓ " : "○ ") + FleshPresentation.SkillLabel(skill) + "  " + data.GetRank(skill) + "/" + cap);
            }
            Widgets.EndScrollView();
        }

        private void Card(Rect rect, FleshSkill skill, string icon, FleshProgress data, int now, bool control)
        {
            Utils.TMIsekaiUI.Panel(rect, FleshPresentation.Border, FleshPresentation.Corner);
            GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 10, 54, 54), FleshGizmoPatches.Icon(icon));
            int rank = data.GetRank(skill), cap = FleshProgress.MaxRank(skill);
            Widgets.Label(new Rect(76, rect.y + 10, rect.width - 92, 48), FleshPresentation.SkillLabel(skill) + "  " + rank + "/" + cap);
            Widgets.Label(new Rect(76, rect.y + 62, rect.width - 92, 94), ("TM_Flesh_" + skill + "Desc").Translate());
            bool upgrade = control && data.SkillPoints > 0 && rank < cap && (skill != FleshSkill.Feast || data.CanUnlockFeast);
            string upgradeKey = FleshPresentation.UpgradeLabelKey(data, skill);
            Rect upgradeRect = new Rect(14, rect.y + 160, rect.width - 205, 36);
            if (upgradeKey == "TM_Flesh_Upgrade") {
                GUI.enabled = upgrade;
                if (Widgets.ButtonText(upgradeRect, upgradeKey.Translate())) FleshActions.Upgrade(pawn, skill);
                GUI.enabled = true;
            } else Widgets.Label(upgradeRect, upgradeKey.Translate());
            if (skill == FleshSkill.Devour || skill == FleshSkill.Restore || skill == FleshSkill.Empower || skill == FleshSkill.Harden || skill == FleshSkill.Feast) {
                int remaining = Math.Max(0, data.GetReadyTick(skill) - now);
                int until = skill == FleshSkill.Empower ? data.EmpowerUntil : skill == FleshSkill.Harden ? data.HardenUntil : skill == FleshSkill.Feast ? data.FeastUntil : 0;
                string status = remaining > 0 ? "TM_Flesh_Cooldown".Translate((remaining / 60f).ToString("0.#")).ToString() : "TM_Flesh_Ready".Translate().ToString();
                if (until > now) status += "  •  " + "TM_Flesh_Active".Translate(((until - now) / 60f).ToString("0.#"));
                Widgets.Label(new Rect(14, rect.y + 198, rect.width - 28, 40), status);
                string rejection = FleshActions.Rejection(pawn, skill);
                if (rejection != null) TooltipHandler.TipRegion(new Rect(rect.width - 164, rect.y + 162, 150, 30), rejection.Translate());
                GUI.enabled = control && rejection == null;
                if (Widgets.ButtonText(new Rect(rect.width - 164, rect.y + 162, 150, 30), "TM_Flesh_Use".Translate())) {
                    if (skill == FleshSkill.Devour) Close();
                    FleshPresentation.Activate(pawn, skill);
                }
                GUI.enabled = true;
            }
        }

        private static void Bar(Rect rect, float value, Color color, string label)
        {
            Widgets.DrawBoxSolid(rect, new Color(.18f, .18f, .2f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), color);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
