using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public partial class Window_TMGrimoire
    {
        private Vector2 classScroll, detailsScroll;
        private bool compactLayout;
        private float ClassProgressHeight => DebugSettings.godMode ? 150f : 120f;

        private static Vector2 FitWindow(float width, float height)
            => new Vector2(Mathf.Min(1500f, Mathf.Max(1f, width - 30f)),
                Mathf.Min(900f, Mathf.Max(1f, height - 30f)));

        private static Rect[] Columns(Rect rect)
        {
            float side = Mathf.Clamp(rect.width * .22f, 220f, 290f);
            return new[] {
                new Rect(rect.x, rect.y, side, rect.height),
                new Rect(rect.x + side + 12f, rect.y, rect.width - side * 2f - 24f, rect.height),
                new Rect(rect.xMax - side, rect.y, side, rect.height)
            };
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (pawn == null || pawn.Destroyed) { Close(); return; }
            EnsureFilters();
            compactLayout = inRect.width < 1040f;
            try
            {
                if (compactLayout) { DrawCompactContents(inRect); return; }
                Rect[] columns = Columns(inRect);
                DrawClassColumn(columns[0]);
                Rect center = new Rect(0f, 0f, columns[1].width - 20f,
                    Mathf.Max(columns[1].height, MeasureViewHeightCached(columns[1].width - 20f)));
                Widgets.BeginScrollView(columns[1], ref scrollPos, center);
                try
                {
                    float y = 0f;
                    if (magic != null && magic.IsMagicUser) y = DrawMagicSection(center, y);
                    if (might != null && might.IsMightUser) y = DrawMightSection(center, y);
                    DrawSleepingSection(center, y);
                }
                finally { Widgets.EndScrollView(); }
                DrawDetailsColumn(columns[2]);
            }
            finally { Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft; GUI.color = Color.white; }
        }

        private void DrawClassColumn(Rect rect)
        {
            float width = rect.width - 20f;
            string description = ClassDescription() ?? "";
            // The progression-era mode line belongs to the beta progression only;
            // with the feature disabled the classic grimoire renders without it.
            string mode = ClassProgression.ClassProgressionFeature.Enabled
                ? (ClassProgression.ClassIdentity.TryGetState(pawn, out _)
                    ? "TM_ProgressionSharedLearning" : "TM_GrimoireClassicRules").Translate()
                : "";
            Text.Font = GameFont.Small;
            string title = pawn.LabelShortCap + "\n" + ClassLabel();
            float titleHeight = Text.CalcHeight(title, width) + 12f;
            Text.Font = GameFont.Tiny;
            float descriptionHeight = Text.CalcHeight(description, width) + 16f;
            float modeHeight = Text.CalcHeight(mode, width) + 16f;
            int owners = (magic != null && magic.IsMagicUser ? 1 : 0) + (might != null && might.IsMightUser ? 1 : 0);
            // Community idea #46: the pawn's full-height portrait sits at the very
            // top of the class column, between the window title and the name card.
            float portraitHeight = Mathf.Clamp(width * 1.35f, 130f, 280f);
            Rect view = new Rect(0f, 0f, width, Mathf.Max(rect.height,
                34f + portraitHeight + 12f + titleHeight + descriptionHeight + modeHeight
                + owners * ClassProgressHeight + InitiationControlsHeight()));
            Widgets.BeginScrollView(rect, ref classScroll, view);
            try
            {
                Text.Font = GameFont.Medium;
                Label(new Rect(0f, 0f, width, 30f), "Grimoire", TextAnchor.UpperLeft, Utils.TMIsekaiUI.HeaderGold);
                float y = 34f;
                RenderTexture portrait = RimWorld.PortraitsCache.Get(pawn, new Vector2(width, portraitHeight), Rot4.South);
                Rect portraitRect = new Rect(0f, y, width, portraitHeight);
                Utils.TMIsekaiUI.Panel(portraitRect);
                GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleToFit);
                y += portraitHeight + 12f;
                Text.Font = GameFont.Small;
                Label(new Rect(0f, y, width, titleHeight), title, TextAnchor.UpperLeft, Utils.TMIsekaiUI.LabelGold);
                y += titleHeight;
                if (magic != null && magic.IsMagicUser) y = DrawClassProgress(view, y, true);
                if (might != null && might.IsMightUser) y = DrawClassProgress(view, y, false);
                Text.Font = GameFont.Tiny;
                Label(new Rect(0f, y, width, descriptionHeight), description, TextAnchor.UpperLeft, TextGray);
                y += descriptionHeight;
                Label(new Rect(0f, y, width, modeHeight), mode, TextAnchor.UpperLeft, TextGray);
                DrawInitiationControls(width, y + modeHeight);
            }
            finally { Widgets.EndScrollView(); }
        }

        private float DrawClassProgress(Rect view, float y, bool mage)
        {
            int level = mage ? magic.MagicData.MagicUserLevel : might.MightData.MightUserLevel;
            int points = mage ? magic.MagicData.MagicAbilityPoints : might.MightData.MightAbilityPoints;
            int xp = mage ? magic.MagicData.MagicUserXP : might.MightData.MightUserXP;
            int required = mage ? magic.GetXPForLevel(Mathf.Max(0, level)) : might.GetXPForLevel(Mathf.Max(0, level));
            Rect panel = new Rect(0f, y, view.width, ClassProgressHeight - 10f);
            Utils.TMIsekaiUI.Panel(panel);
            Text.Font = GameFont.Tiny;
            Label(new Rect(10f, y + 6f, view.width - 20f, 20f),
                (mage ? "TM_Header" : "TM_HeaderMight").Translate(), TextAnchor.UpperLeft, Utils.TMIsekaiUI.LabelGold);
            Label(new Rect(10f, y + 28f, view.width - 20f, 20f),
                "TM_Level".Translate() + ": " + level, TextAnchor.UpperLeft, Color.white);
            Label(new Rect(10f, y + 50f, view.width - 20f, 20f),
                "TM_PointsAvail".Translate() + ": " + points, TextAnchor.UpperLeft, Utils.TMIsekaiUI.LabelGold);
            Rect bar = new Rect(10f, y + 75f, view.width - 20f, 7f);
            Widgets.DrawBoxSolid(bar, BarBg);
            Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * (required <= 0 ? 0f : Mathf.Clamp01((float)xp / required)), bar.height), BarXp);
            Label(new Rect(10f, y + 87f, view.width - 20f, 18f), "XP " + xp + " / " + required, TextAnchor.UpperLeft, TextGray);
            if (DebugSettings.godMode)
            {
                if (Widgets.ButtonText(new Rect(10f, y + 110f, 45f, 22f), "+1"))
                { if (mage) magic.LevelUp(true); else might.LevelUp(true); }
                if (!ClassProgression.ClassIdentity.TryGetState(pawn, out _)
                    && Widgets.ButtonText(new Rect(60f, y + 110f, view.width - 70f, 22f), "Reset Class"))
                { if (mage) magic.ResetSkills(); else might.ResetSkills(); }
            }
            return y + ClassProgressHeight;
        }

        // Read the same visible owners as the central cards; never infer scrolls from names.
        private List<TMAbilityDef> RequiredLearningItems()
        {
            var result = new List<TMAbilityDef>();
            if (magicRenderable != null)
                foreach (var power in magicRenderable)
                    if (!power.learned && power.abilityDef is TMAbilityDef def && (power.requiresScroll || def.learnItem != null)) result.Add(def);
            if (mightRenderable != null)
                foreach (var power in mightRenderable)
                    if (!power.learned && power.abilityDef is TMAbilityDef def && def.learnItem != null) result.Add(def);
            return result.Distinct().OrderBy(def => def.label).ToList();
        }

        private string LearningDetails()
        {
            if (!ClassProgression.ClassProgressionFeature.Enabled) return "";
            var requirements = RequiredLearningItems();
            string text = InitiationRequirements();
            if (ClassProgression.ClassIdentity.TryGetState(pawn, out var state) &&
                state.Find("rwom.necromancer")?.Stage == ClassProgression.InitiationStage.Ascended)
                text += "\n\n" + "TM_GrimoireRebirthCircle".Translate(ClassProgression.SpiritRebirthRitualEligibility.MagicyteCost,
                    ClassProgression.SpiritRebirthRitualEligibility.SilverCost, ClassProgression.SpiritRebirthRitualEligibility.RitualTicks.ToStringTicksToPeriod());
            bool occupied = ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(pawn, out var soul);
            if (ClassProgression.SpiritRebirthState.TryGet(occupied ? soul : pawn, out var spirit))
            {
                text += "\n\n" + "TM_GrimoireSpiritStone".Translate(ClassProgression.SpiritRebirthState.RecoveryDurationTicks.ToStringTicksToPeriod());
                if (spirit.RecoveryRequired && spirit.Form == ClassProgression.SpiritRebirthForm.Possessed &&
                    spirit.RecoveryTicks < ClassProgression.SpiritRebirthState.RecoveryDurationTicks)
                    text += "\n" + "TM_SoulStoneRecoveryRemaining".Translate((ClassProgression.SpiritRebirthState.RecoveryDurationTicks - spirit.RecoveryTicks).ToStringTicksToPeriod());
            }
            text += "\n\n" + "TM_GrimoireLearningItems".Translate();
            if (requirements.Count == 0) text += "\n" + "TM_GrimoireNoLearningItems".Translate();
            foreach (var ability in requirements)
                text += "\n\n" + ability.LabelCap + "\n" + (ability.learnItem != null
                    ? ability.learnItem.LabelCap.ToString() : "TM_GrimoireScrollUnspecified".Translate().ToString());
            return text;
        }

        private void DrawDetailsColumn(Rect rect)
        {
            float width = rect.width - 20f;
            string text = LearningDetails();
            Text.Font = GameFont.Small;
            float textHeight = Text.CalcHeight(text, width) + 20f;
            var bars = GetResourceBars();
            float resourcesHeight = ResourcesPanelHeight(bars.Count);
            Rect view = new Rect(0f, 0f, width, Mathf.Max(rect.height, resourcesHeight + textHeight));
            Widgets.BeginScrollView(rect, ref detailsScroll, view);
            try
            {
                float y = DrawResourcesBlock(view, 0f, bars);
                Text.Font = GameFont.Small;
                Label(new Rect(0f, y, width, textHeight), text, TextAnchor.UpperLeft, TextGray);
            }
            finally { Widgets.EndScrollView(); }
        }
    }
}
