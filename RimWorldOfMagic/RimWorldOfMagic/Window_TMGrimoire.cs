using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    /// <summary>
    /// Full character grimoire window, Isekai-styled: class levels, XP and mana at
    /// the top, then one card per spell/skill with its description, upgrade button
    /// and per-skill upgrade rows with visible explanations — the whole progression
    /// UI of the Magic/Might tabs in a single centered, scrolling window.
    /// </summary>
    public partial class Window_TMGrimoire : Window
    {
        private const float IconSize = 46f;

        private static readonly Color BarBg = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color BarMp = new Color(0.30f, 0.45f, 0.90f);
        private static readonly Color BarXp = new Color(0.90f, 0.75f, 0.25f);
        private static readonly Color TextGray = new Color(0.60f, 0.60f, 0.66f);
        private static readonly Color Purple = new Color(0.65f, 0.40f, 0.85f);
        private static readonly Color ResStamina = new Color(0.30f, 0.75f, 0.40f);
        private static readonly Color ResPsionic = new Color(0.65f, 0.40f, 0.85f);
        private static readonly Color ResHate = new Color(0.85f, 0.30f, 0.35f);
        private static readonly Color ResBlood = new Color(0.60f, 0.10f, 0.12f);
        private static readonly Color ResChi = new Color(0.95f, 0.60f, 0.25f);
        private static readonly Color ResSpirit = new Color(0.95f, 0.90f, 0.55f);

        private readonly Pawn pawn;
        private readonly CompAbilityUserMagic magic;
        private readonly CompAbilityUserMight might;
        private Vector2 scrollPos;

        public override Vector2 InitialSize => FitWindow(UI.screenWidth, UI.screenHeight);

        // Entry points share a single grimoire window, including tab and gizmo opens.
        public static void OpenFor(Pawn pawn)
        {
            System.Collections.Generic.IList<Window> windows = Find.WindowStack.Windows;
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                if (windows[i] is Window_TMGrimoire)
                {
                    Find.WindowStack.TryRemove(windows[i], doCloseSound: false);
                }
            }
            Find.WindowStack.Add(new Window_TMGrimoire(pawn));
        }

        public Window_TMGrimoire(Pawn pawn)
        {
            this.pawn = pawn;
            magic = pawn.GetCompAbilityUserMagic();
            might = pawn.GetCompAbilityUserMight();
            forcePause = false;
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnAccept = false;
            closeOnCancel = true;
        }

        private void DrawCompactContents(Rect inRect)
        {
            EnsureFilters();

            float viewWidth = inRect.width - 20f;

            // the whole header (title, portrait, pawn name, class description) scrolls
            // with the content — a fixed header left a tiny viewport on small screens
            string classDesc = ClassDescription();
            var details = LearningDetails();
            if (!details.NullOrEmpty()) classDesc += "\n\n" + details;
            Text.Font = GameFont.Tiny;
            float headerH = 34f + 96f + 12f;
            if (!classDesc.NullOrEmpty())
            {
                headerH += Text.CalcHeight(classDesc, viewWidth) + 8f;
            }
            Text.Font = GameFont.Small;

            var bars = GetResourceBars();
            Rect view = new Rect(0f, 0f, viewWidth, Mathf.Max(headerH + ResourcesPanelHeight(bars.Count) + MeasureViewHeightCached(viewWidth) + InitiationControlsHeight(), inRect.height));
            Widgets.BeginScrollView(inRect, ref scrollPos, view);
            try
            {
                float y = 0f;
                Text.Font = GameFont.Medium;
                Label(new Rect(0f, y, viewWidth, 32f), "Grimoire", TextAnchor.MiddleLeft,
                    TorannMagic.Utils.TMIsekaiUI.HeaderGold);
                Text.Font = GameFont.Small;
                RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(72f, 96f), Rot4.South);
                Rect portraitRect = new Rect(0f, y + 34f, 72f, 96f);
                TorannMagic.Utils.TMIsekaiUI.Panel(portraitRect);
                GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleToFit);
                float nameX = 80f;
                Label(new Rect(nameX, y + 40f, viewWidth - nameX, 18f),
                    pawn.LabelShortCap, TextAnchor.MiddleLeft, Color.white);
                Label(new Rect(nameX, y + 60f, viewWidth - nameX, 18f),
                    "— " + ClassLabel(), TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.LabelGold);
                y += 34f + 96f + 12f;

                if (!classDesc.NullOrEmpty())
                {
                    Text.Font = GameFont.Tiny;
                    float dh = Text.CalcHeight(classDesc, viewWidth);
                    Label(new Rect(0f, y, viewWidth, dh), classDesc, TextAnchor.UpperLeft, TextGray);
                    y += dh + 8f;
                }
                Text.Font = GameFont.Small;
                y += 4f;

                DrawInitiationControls(viewWidth, y);
                y += InitiationControlsHeight();
                y = DrawResourcesBlock(view, y, bars);

                if (magic != null && magic.IsMagicUser && magic.MagicData != null)
                {
                    y = DrawMagicSection(view, y);
                }

                if (might != null && might.IsMightUser && might.MightData != null)
                {
                    y = DrawMightSection(view, y);
                }

                y = DrawSleepingSection(view, y);
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        // Ф4 step 4: knowledge entries whose def is gone render as grey cards;
        // they wake up automatically if the def returns (or an XML spellId
        // remaps them).
        private float DrawSleepingSection(Rect view, float y)
        {
            List<SpellKnowledge> sleeping = GrimoireKnowledge.SleepingSpells(pawn);
            if (sleeping.Count == 0)
            {
                return y;
            }
            Text.Font = GameFont.Medium;
            Label(new Rect(0f, y, view.width, 26f), "TM_SleepingSpells".Translate().CapitalizeFirst(),
                TextAnchor.MiddleLeft, TextGray);
            y += 30f;
            Text.Font = GameFont.Small;
            foreach (SpellKnowledge spell in sleeping)
            {
                Rect card = new Rect(0f, y, view.width, 48f);
                TorannMagic.Utils.TMIsekaiUI.Panel(card);
                GUI.color = TextGray;
                Label(new Rect(card.x + 10f, card.y + 6f, card.width - 20f, 22f),
                    GrimoireKnowledge.DisplayName(spell.spellId), TextAnchor.MiddleLeft, TextGray);
                Text.Font = GameFont.Tiny;
                Label(new Rect(card.x + 10f, card.y + 28f, card.width - 20f, 16f),
                    "TM_SleepingSpellsDesc".Translate(), TextAnchor.MiddleLeft, TextGray);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += card.height + 8f;
            }
            return y + 12f;
        }

        #region Magic

        private float DrawMagicSection(Rect view, float y)
        {
            Text.Font = GameFont.Medium;
            Label(new Rect(0f, y, view.width, 26f), "TM_Header".Translate().CapitalizeFirst(),
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.HeaderGold);
            y += 30f;

            int points = magic.MagicData.MagicAbilityPoints;
            if (compactLayout) y = DrawClassProgress(view, y, true);

            // global stat skills (Clarity / Focus / Spirit), 1 point per level
            y = DrawStatBlock(view, y, "TM_Stats", magicStats);

            foreach (MagicPower power in magic.MagicData.AllMagicPowersWithSkills)
            {
                TMAbilityDef ability = power.abilityDef as TMAbilityDef;
                if (ability == null || !magicRenderable.Contains(power))
                {
                    continue;
                }
                y = DrawMagicPowerCard(view, y, power, points);
            }
            return y + 12f;
        }

        private float DrawMagicPowerCard(Rect view, float y, MagicPower power, int points)
        {
            TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
            string desc = power.abilityDescDef?.description;
            if (power.level > 0 && power.level < power.maxLevel && power.nextLevelAbilityDescDef?.description != null)
            {
                desc += "\n\n" + "TM_MCU_NextLevel".Translate() + "\n" + power.nextLevelAbilityDescDef.description;
            }
            float descWidth = view.width - 24f - IconSize - 10f;
            float descHeight = PowerDescriptionHeight(power, view.width);
            string title = MagicTitle(power);
            float headerHeight = PowerHeaderHeight(title, view.width);
            List<MagicPowerSkill> skills = GetMagicSkills(ability);
            float skillRows = 0f;
            foreach (MagicPowerSkill sk in skills)
            {
                skillRows += SkillRowHeight(SkillRowText(sk.label, sk.level, sk.levelMax, sk.desc), view.width - 16f - 50f) + 10f;
            }
            float cardHeight = Mathf.Max(IconSize, headerHeight + descHeight) + skillRows + 16f + 8f;

            Rect card = new Rect(0f, y, view.width, cardHeight);
            TorannMagic.Utils.TMIsekaiUI.Panel(card);
            Text.Font = GameFont.Small;

            Rect icon = new Rect(card.x + 10f, card.y + 10f, IconSize, IconSize);
            if (power.Icon != null)
            {
                GUI.color = power.learned ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                Widgets.DrawTextureFitted(icon, power.Icon, 1f);
                GUI.color = Color.white;
            }

            float titleX = icon.xMax + 10f;
            Label(new Rect(titleX, card.y + 8f, card.width - (titleX - card.x) - 150f, headerHeight - 12f),
                title,
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.LabelGold);

            Rect action = new Rect(card.xMax - 142f, card.y + 8f, 132f, 24f);
            DrawMagicPowerAction(action, power, points);

            if (!string.IsNullOrEmpty(desc))
            {
                Rect descRect = new Rect(titleX, card.y + headerHeight, descWidth, descHeight);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, desc);
            }

            float sy = card.y + Mathf.Max(IconSize, headerHeight + descHeight) + 6f;
            foreach (MagicPowerSkill skill in skills)
            {
                bool canUp = power.learned && skill.level < skill.levelMax && points >= skill.costToLevel;
                string skillText = SkillRowText(skill.label, skill.level, skill.levelMax, skill.desc);
                float rowH = SkillRowHeight(skillText, card.width - 16f - 50f);
                Rect skillLabel = new Rect(card.x + 16f, sy, card.width - 16f - 50f, rowH);
                Text.Font = GameFont.Tiny;
                Label(skillLabel, skillText, TextAnchor.UpperLeft, canUp ? Color.white : TextGray);
                if (canUp && Widgets.ButtonText(new Rect(card.xMax - 52f, sy - 2f, 36f, 20f), "+", true, false, true))
                {
                    skill.level++;
                    magic.MagicData.MagicAbilityPoints -= skill.costToLevel;
                    GrimoireKnowledge.SyncRank(pawn, ability, skill.label, skill.level);
                }
                else if (power.learned && skill.level >= skill.levelMax)
                {
                    Label(new Rect(card.xMax - 52f, sy - 2f, 36f, 20f), "max", TextAnchor.MiddleCenter, TextGray);
                }
                sy += rowH + 10f;
            }

            return y + cardHeight + 10f;
        }

        private void DrawMagicPowerAction(Rect rect, MagicPower power, int points)
        {
            if (!Odyssey.SpaceSpellProgression.Visible(power)) return;
            if (!ClassProgression.ClassSpellAccess.CanLearn(pawn, power))
            {
                // Full stage requirement is in the measured title, not a 24px action slot.
                TooltipHandler.TipRegion(rect, "TM_ProgressionStageLocked".Translate());
                return;
            }
            if (!power.learned)
            {
                if (points < power.learnCost)
                {
                    Label(rect, "TM_MCU_PointsToLearn".Translate(power.learnCost) + "TM_Learn".Translate(),
                        TextAnchor.MiddleCenter, TextGray);
                    return;
                }
                if (Widgets.ButtonText(rect, "TM_Learn".Translate() + " (" + power.learnCost + ")", true, false, true))
                {
                    power.learned = true;
                    GrimoireKnowledge.SyncEntry(pawn, power.abilityDef, power.level, true);
                    TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
                    if (!(power.abilityDef.defName == "TM_TechnoBit") && ability.shouldInitialize)
                    {
                        magic.AddPawnAbility(power.abilityDef);
                    }
                    if (power.abilityDef.defName == "TM_TechnoWeapon")
                    {
                        magic.AddPawnAbility(TorannMagicDefOf.TM_NanoStimulant);
                        magic.MagicData.MagicPowersStandalone
                            .FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_NanoStimulant).learned = true;
                    }
                    if (ability.childAbilities != null)
                    {
                        foreach (AbilityUser.AbilityDef childDef in ability.childAbilities)
                        {
                            if (childDef is TMAbilityDef child && child.shouldInitialize)
                            {
                                magic.AddPawnAbility(child);
                            }
                        }
                    }
                    magic.MagicData.MagicAbilityPoints -= power.learnCost;
                }
                return;
            }

            if (power.maxLevel <= 0)
            {
                Label(rect, "TM_GrimoireLearned".Translate(), TextAnchor.MiddleCenter, TextGray);
                return;
            }
            if (power.level >= power.maxLevel)
            {
                Label(rect, "TM_GrimoireRankMax".Translate(), TextAnchor.MiddleCenter, TextGray);
                return;
            }
            if (points < power.costToLevel)
            {
                Label(rect, "TM_MCU_PointsToLearn".Translate(power.costToLevel), TextAnchor.MiddleCenter, TextGray);
                return;
            }
            if (Widgets.ButtonText(rect, "TM_LevelUp".Translate() + " (" + power.costToLevel + ")", true, false, true))
            {
                magic.LevelUpPower(power);
                magic.MagicData.MagicAbilityPoints -= power.costToLevel;
                GrimoireKnowledge.SyncEntry(pawn, power.abilityDef, power.level, power.learned);
            }
        }

        private List<MagicPowerSkill> GetMagicSkills(TMAbilityDef ability)
        {
            List<MagicPowerSkill> list = new List<MagicPowerSkill>();
            MagicPowerSkill s = magic.MagicData.GetSkill_Power(ability);
            if (s != null) list.Add(s);
            s = magic.MagicData.GetSkill_Efficiency(ability);
            if (s != null) list.Add(s);
            s = magic.MagicData.GetSkill_Versatility(ability);
            if (s != null) list.Add(s);
            return list;
        }

        private List<MagicPower> GetMagicClassGroup()
        {
            return magic != null && magic.IsMagicUser
                ? TM_ClassGroups.GetMagicClassGroup(pawn, magic.MagicData) : null;
        }

        #endregion

        #region Might

        private float DrawMightSection(Rect view, float y)
        {
            Text.Font = GameFont.Medium;
            Label(new Rect(0f, y, view.width, 26f), "TM_HeaderMight".Translate().CapitalizeFirst(),
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.HeaderGold);
            y += 30f;

            int points = might.MightData.MightAbilityPoints;
            if (compactLayout) y = DrawClassProgress(view, y, false);

            y = DrawStatBlock(view, y, "TM_StatsMight", mightStats);

            foreach (MightPower power in might.MightData.AllMightPowersWithSkills)
            {
                TMAbilityDef ability = power.abilityDef as TMAbilityDef;
                if (ability == null || !mightRenderable.Contains(power))
                {
                    continue;
                }
                y = DrawMightPowerCard(view, y, power, points);
            }
            return y + 12f;
        }

        private float DrawMightPowerCard(Rect view, float y, MightPower power, int points)
        {
            TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
            string desc = MightPowerDescription(power);
            float descWidth = view.width - 24f - IconSize - 10f;
            float descHeight = PowerDescriptionHeight(power, view.width);
            string title = MightTitle(power);
            float headerHeight = PowerHeaderHeight(title, view.width);
            List<MightPowerSkill> skills = GetMightSkills(ability);
            float skillRows = 0f;
            foreach (MightPowerSkill sk in skills)
            {
                skillRows += SkillRowHeight(MightSkillRow(ability, sk), view.width - 16f - 50f) + 10f;
            }
            float cardHeight = Mathf.Max(IconSize, headerHeight + descHeight) + skillRows + 16f + 8f;

            Rect card = new Rect(0f, y, view.width, cardHeight);
            TorannMagic.Utils.TMIsekaiUI.Panel(card);
            Text.Font = GameFont.Small;

            Rect icon = new Rect(card.x + 10f, card.y + 10f, IconSize, IconSize);
            if (power.Icon != null)
            {
                GUI.color = power.learned ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                Widgets.DrawTextureFitted(icon, power.Icon, 1f);
                GUI.color = Color.white;
            }

            float titleX = icon.xMax + 10f;
            Label(new Rect(titleX, card.y + 8f, card.width - (titleX - card.x) - 150f, headerHeight - 12f),
                title,
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.LabelGold);

            Rect action = new Rect(card.xMax - 142f, card.y + 8f, 132f, 24f);
            DrawMightPowerAction(action, power, points);

            if (!string.IsNullOrEmpty(desc))
            {
                Rect descRect = new Rect(titleX, card.y + headerHeight, descWidth, descHeight);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, desc);
            }

            float sy = card.y + Mathf.Max(IconSize, headerHeight + descHeight) + 6f;
            foreach (MightPowerSkill skill in skills)
            {
                bool canUp = power.learned && skill.level < skill.levelMax && points >= skill.costToLevel;
                string skillText = MightSkillRow(ability, skill);
                float rowH = SkillRowHeight(skillText, card.width - 16f - 50f);
                Rect skillLabel = new Rect(card.x + 16f, sy, card.width - 16f - 50f, rowH);
                Text.Font = GameFont.Tiny;
                Label(skillLabel, skillText, TextAnchor.UpperLeft, canUp ? Color.white : TextGray);
                if (canUp && Widgets.ButtonText(new Rect(card.xMax - 52f, sy - 2f, 36f, 20f), "+", true, false, true))
                {
                    skill.level++;
                    might.MightData.MightAbilityPoints -= skill.costToLevel;
                    GrimoireKnowledge.SyncRank(pawn, ability, skill.label, skill.level);
                }
                else if (power.learned && skill.level >= skill.levelMax)
                {
                    Label(new Rect(card.xMax - 52f, sy - 2f, 36f, 20f), "max", TextAnchor.MiddleCenter, TextGray);
                }
                sy += rowH + 10f;
            }

            return y + cardHeight + 10f;
        }

        private void DrawMightPowerAction(Rect rect, MightPower power, int points)
        {
            if (!power.learned)
            {
                if (points < power.learnCost)
                {
                    Label(rect, "TM_MCU_PointsToLearn".Translate(power.learnCost) + "TM_Learn".Translate(),
                        TextAnchor.MiddleCenter, TextGray);
                    return;
                }
                if (Widgets.ButtonText(rect, "TM_Learn".Translate() + " (" + power.learnCost + ")", true, false, true))
                {
                    power.learned = true;
                    GrimoireKnowledge.SyncEntry(pawn, power.abilityDef, power.level, true);
                    might.AddPawnAbility((TMAbilityDef)power.abilityDef);
                    might.MightData.MightAbilityPoints -= power.learnCost;
                }
                return;
            }

            if (power.maxLevel <= 0)
            {
                Label(rect, "TM_GrimoireLearned".Translate(), TextAnchor.MiddleCenter, TextGray);
                return;
            }
            if (power.level >= power.maxLevel)
            {
                Label(rect, "TM_GrimoireRankMax".Translate(), TextAnchor.MiddleCenter, TextGray);
                return;
            }
            if (points < power.costToLevel)
            {
                Label(rect, "TM_MCU_PointsToLearn".Translate(power.costToLevel), TextAnchor.MiddleCenter, TextGray);
                return;
            }
            if (Widgets.ButtonText(rect, "TM_LevelUp".Translate() + " (" + power.costToLevel + ")", true, false, true))
            {
                might.LevelUpPower(power);
                might.MightData.MightAbilityPoints -= power.costToLevel;
                GrimoireKnowledge.SyncEntry(pawn, power.abilityDef, power.level, power.learned);
            }
        }

        private List<MightPowerSkill> GetMightSkills(TMAbilityDef ability)
        {
            List<MightPowerSkill> list = new List<MightPowerSkill>();
            MightPowerSkill s = might.MightData.GetSkill_Power(ability);
            if (s != null) list.Add(s);
            s = might.MightData.GetSkill_Efficiency(ability);
            if (s != null) list.Add(s);
            s = might.MightData.GetSkill_Versatility(ability);
            if (s != null) list.Add(s);
            return list;
        }

        private List<MightPower> GetMightClassGroup()
        {
            if (might != null && might.IsMightUser && ClassProgression.ClassIdentity.TryGetState(pawn, out var extraState) &&
                ClassProgression.AdditionalMartialRegistry.Current(extraState, out _) is ClassProgression.AdditionalMartialPath extra)
                return extra.Powers(might).Where(power => power.learned).ToList();
            if (might != null && might.IsMightUser && ClassProgression.RangerCareer.Path(pawn) != null) return might.MightData.MightPowersR.Where(p => p.learned).ToList();
            if (might != null && might.IsMightUser && ClassProgression.BladeCareer.Path(pawn) != null) return might.MightData.MightPowersB.Where(p => p.learned).ToList();
            if (might != null && might.IsMightUser && ClassProgression.ClassIdentity.TryGetState(pawn, out var sniper)
                && sniper.Find(ClassProgression.SniperFoundation.Id) != null) return might.MightData.MightPowersS.Where(p => p.learned).ToList();
            if (might != null && might.IsMightUser && ClassProgression.ClassIdentity.TryGetState(pawn, out var current)
                && current.Find("rwom.gladiator") != null) return might.MightData.MightPowersG.Where(p => p.learned).ToList();
            if (might != null && might.IsMightUser && ClassProgression.WandererLessons.Path(pawn) is ClassProgression.DisciplineProgress path)
                return might.MightData.MightPowersW.Concat(ClassProgression.WandererLessons.MartialPicks(might, path)).Distinct().ToList();
            if (might != null && might.IsMightUser)
            {
                var classic = TM_ClassGroups.GetMightClassGroup(pawn, might.MightData) ?? new List<MightPower>();
                // Beta-only path choices (Reforged Maneuver / Master Sweep) are
                // not classic learnable rows for trait gladiators without
                // progression state.
                if (!ClassProgression.ClassIdentity.TryGetState(pawn, out _))
                    return classic.Where(p => p?.TMabilityDefs?.Any(
                        d => d is TMAbilityDef tm && (tm.defName == "TM_GladiatorSweep" || tm.defName == "TM_GladiatorManeuver")) != true).ToList();
                return classic;
            }
            return null;
        }

        #endregion

        private float DrawStatBlock(Rect view, float y, string headerKey, List<MagicPowerSkill> stats)
        {
            Text.Font = GameFont.Small;
            Label(new Rect(0f, y, view.width, 22f), headerKey.Translate().CapitalizeFirst(),
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.HeaderGold);
            y += 26f;
            float statRows = 0f;
            foreach (MagicPowerSkill stat in stats)
            {
                statRows += SkillRowHeight(SkillRowText(stat.label, stat.level, stat.levelMax, stat.desc), view.width - 14f - 50f) + 12f;
            }
            Rect panel = new Rect(0f, y, view.width, statRows + 14f);
            TorannMagic.Utils.TMIsekaiUI.Panel(panel);
            int points = magic != null && magic.IsMagicUser && magic.MagicData != null ? magic.MagicData.MagicAbilityPoints : 0;
            float sy = panel.y + 7f;
            foreach (MagicPowerSkill stat in stats)
            {
                bool canUp = stat.level < stat.levelMax && points >= 1;
                Text.Font = GameFont.Tiny;
                string statText = SkillRowText(stat.label, stat.level, stat.levelMax, stat.desc);
                float rowH = SkillRowHeight(statText, panel.width - 14f - 50f);
                Rect row = new Rect(panel.x + 14f, sy, panel.width - 14f - 50f, rowH);
                Label(row, statText, TextAnchor.UpperLeft, canUp ? Color.white : TextGray);
                if (canUp && Widgets.ButtonText(new Rect(panel.xMax - 52f, sy - 2f, 36f, 20f), "+", true, false, true))
                {
                    stat.level++;
                    magic.MagicData.MagicAbilityPoints -= 1;
                    points -= 1;
                }
                else if (stat.level >= stat.levelMax)
                {
                    Label(new Rect(panel.xMax - 52f, sy - 2f, 36f, 20f), "max", TextAnchor.MiddleCenter, TextGray);
                }
                sy += rowH + 12f;
            }
            return y + panel.height + 12f;
        }

        private float DrawStatBlock(Rect view, float y, string headerKey, List<MightPowerSkill> stats)
        {
            Text.Font = GameFont.Small;
            Label(new Rect(0f, y, view.width, 22f), headerKey.Translate().CapitalizeFirst(),
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.HeaderGold);
            y += 26f;
            float statRows = 0f;
            foreach (MightPowerSkill stat in stats)
            {
                statRows += SkillRowHeight(SkillRowText(stat.label, stat.level, stat.levelMax, stat.desc), view.width - 14f - 50f) + 12f;
            }
            Rect panel = new Rect(0f, y, view.width, statRows + 14f);
            TorannMagic.Utils.TMIsekaiUI.Panel(panel);
            int points = might != null && might.IsMightUser && might.MightData != null ? might.MightData.MightAbilityPoints : 0;
            float sy = panel.y + 7f;
            foreach (MightPowerSkill stat in stats)
            {
                bool canUp = stat.level < stat.levelMax && points >= 1;
                Text.Font = GameFont.Tiny;
                string statText = SkillRowText(stat.label, stat.level, stat.levelMax, stat.desc);
                float rowH = SkillRowHeight(statText, panel.width - 14f - 50f);
                Rect row = new Rect(panel.x + 14f, sy, panel.width - 14f - 50f, rowH);
                Label(row, statText, TextAnchor.UpperLeft, canUp ? Color.white : TextGray);
                if (canUp && Widgets.ButtonText(new Rect(panel.xMax - 52f, sy - 2f, 36f, 20f), "+", true, false, true))
                {
                    stat.level++;
                    might.MightData.MightAbilityPoints -= 1;
                    points -= 1;
                }
                else if (stat.level >= stat.levelMax)
                {
                    Label(new Rect(panel.xMax - 52f, sy - 2f, 36f, 20f), "max", TextAnchor.MiddleCenter, TextGray);
                }
                sy += rowH + 12f;
            }
            return y + panel.height + 12f;
        }

        private List<(string label, float cur, float max, string regen, Color color)> GetResourceBars()
        {
            // Same sources the vanilla energy gizmo uses: the comp's own need
            // instances and the *100 display scale, plus the real regen rate.
            List<(string label, float cur, float max, string regen, Color color)> bars =
                new List<(string, float, float, string, Color)>();

            if (might != null && might.Stamina != null)
            {
                bars.Add((might.Stamina.def.LabelCap, might.Stamina.CurInstantLevel * 100f, might.Stamina.MaxLevel * 100f,
                    RegenString(might.Stamina.lastGainPct), ResStamina));
            }
            if (magic != null && magic.Mana != null)
            {
                bars.Add((magic.Mana.def.LabelCap, magic.Mana.CurInstantLevel * 100f, magic.Mana.MaxLevel * 100f,
                    RegenString(magic.Mana.lastGainPct), BarMp));
            }
            AddHediffResource(bars, TorannMagicDefOf.TM_PsionicHD, ResPsionic, TorannMagicDefOf.TM_Artifact_PsionicBoostHD);
            AddHediffResource(bars, TorannMagicDefOf.TM_HateHD, ResHate, TorannMagicDefOf.TM_Artifact_HateBoostHD);
            AddHediffResource(bars, TorannMagicDefOf.TM_BloodHD, ResBlood, TorannMagicDefOf.TM_Artifact_BloodBoostHD);
            AddHediffResource(bars, TorannMagicDefOf.TM_ChiHD, ResChi);
            AddHediffResource(bars, TorannMagicDefOf.TM_ApothecaryHerbsHD, ResCustom(Color.green));
            Need_Spirit spirit = pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
            if (spirit != null)
            {
                bars.Add((spirit.def.LabelCap, spirit.CurLevel, spirit.MaxLevel, "", ResSpirit));
            }
            var customDefs = new HashSet<HediffDef> {TorannMagicDefOf.TM_PsionicHD,TorannMagicDefOf.TM_HateHD,TorannMagicDefOf.TM_BloodHD,TorannMagicDefOf.TM_ChiHD,TorannMagicDefOf.TM_ApothecaryHerbsHD};
            foreach (var custom in new[] {magic?.customClass,might?.customClass})
            {
                if (custom?.showHediffOnGizmo == true && custom.classHediff != null && customDefs.Add(custom.classHediff))
                    AddHediffResource(bars, custom.classHediff, ResCustom(custom.classIconColor));
            }

            return bars;
        }

        private float DrawResourcesBlock(Rect view, float y, List<(string label, float cur, float max, string regen, Color color)> bars)
        {
            if (bars.Count == 0)
            {
                return y;
            }

            Text.Font = GameFont.Medium;
            Label(new Rect(0f, y, view.width, 26f), "TM_GrimoireResources".Translate().CapitalizeFirst(),
                TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.HeaderGold);
            y += 30f;
            Rect panel = new Rect(0f, y, view.width, bars.Count * 54f + 14f);
            TorannMagic.Utils.TMIsekaiUI.Panel(panel);
            float sy = panel.y + 7f;
            foreach ((string label, float cur, float max, string regen, Color color) in bars)
            {
                float frac = max <= 0f ? 0f : Mathf.Clamp01(cur / max);
                Text.Font = GameFont.Tiny;
                Label(new Rect(12f, sy, panel.width - 24f, 16f), label, TextAnchor.MiddleLeft, TorannMagic.Utils.TMIsekaiUI.LabelGold);
                Rect bar = new Rect(12f, sy + 19f, panel.width - 24f, 7f);
                Widgets.DrawBoxSolid(bar, BarBg);
                Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * frac, bar.height), color);
                string value = Mathf.RoundToInt(cur) + " / " + Mathf.RoundToInt(max);
                if (!regen.NullOrEmpty()) value += "  (" + regen + ")";
                Label(new Rect(12f, sy + 29f, panel.width - 24f, 16f), value, TextAnchor.MiddleLeft, TextGray);
                sy += 54f;
            }
            return y + panel.height + 12f;
        }

        private static string RegenString(float lastGainPct)
        {
            // lastGainPct is the smoothed per-tick fraction; *200 matches the rate the
            // legacy tabs show as "X% per second".
            return (lastGainPct >= 0f ? "+" : "") + (lastGainPct * 200f).ToString("0.###") + "%/s";
        }

        private void AddHediffResource(List<(string label, float cur, float max, string regen, Color color)> bars, HediffDef def, Color color, HediffDef boost = null)
        {
            if (def == null) return;
            Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(def, false);
            if (hediff != null)
            {
                var extra = hediff as HediffWithCompsExtra;
                float max = extra?.MaxSeverity ?? 100f;
                if (boost != null) max += pawn.health.hediffSet.GetFirstHediffOfDef(boost)?.Severity ?? 0f;
                bars.Add((extra?.GizmoLabel ?? def.LabelCap.ToString(), hediff.Severity, max, "", color));
            }
        }

        private Color ResCustom(Color iconColor)
        {
            return Color.Lerp(iconColor, Color.white, 0.25f);
        }

        #region shared

        private List<TMAbilityDef> MagicAbilityFilter(List<MagicPower> magicGroup)
        {
            var list = magicGroup == null ? new List<TMAbilityDef>()
                : magicGroup.SelectMany(p => p.TMabilityDefs).OfType<TMAbilityDef>().ToList();
            list.AddRange(OwnMagicClassDefs());
            // These scroll-taught support spells are shared by all mage classes.
            // Their definitions are absent when Odyssey is disabled.
            TMAbilityDef etherShell = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_EtherShell");
            if (etherShell != null && !list.Contains(etherShell)) list.Add(etherShell);
            TMAbilityDef breachSeal = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BreachSeal");
            if (breachSeal != null && !list.Contains(breachSeal)) list.Add(breachSeal);
            return list;
        }

        private List<TMAbilityDef> MightAbilityFilter(List<MightPower> mightGroup)
        {
            var list = mightGroup == null ? new List<TMAbilityDef>()
                : mightGroup.SelectMany(p => p.TMabilityDefs).OfType<TMAbilityDef>().ToList();
            list.AddRange(OwnMightClassDefs());
            return list;
        }


        // Height helpers mirror the drawing code exactly; the scroll view needs the
        // total up front or the bottom of the list is unreachable.
        // A spell's variant rank is distinct from its independently upgraded skills.
        // Single-variant spells have maxLevel == 0; that is not a maxed skill tree.
        private static string PowerRankProgress(int level, int maxLevel, bool learned)
        {
            return maxLevel > 0 ? (learned ? level + 1 : 0) + " / " + (maxLevel + 1) : "";
        }

        private static string PowerTitle(string label, int level, int maxLevel, bool learned)
        {
            string rank = PowerRankProgress(level, maxLevel, learned);
            return rank.Length == 0 ? label : label + "   " + "TM_GrimoireRank".Translate() + " " + rank;
        }

        private static string SkillRowText(string label, int level, int levelMax, string desc)
        {
            return label.Translate() + ": " + level + " / " + levelMax + "  —  " + desc.Translate();
        }

        private static float SkillRowHeight(string text, float width)
        {
            Text.Font = GameFont.Tiny;
            return Mathf.Max(20f, Text.CalcHeight(text, width) + 4f);
        }

        // DoWindowContents runs every GUI frame; the measurement walks every power
        // description and skill row with Text.CalcHeight. Cache by width + the
        // levels that can change the wrapped text (desc gains the next-level block).
        private long viewHeightKey = long.MinValue;
        private float viewHeightCache;

        private float MeasureViewHeightCached(float width)
        {
            long key = (long)(width * 4f) * 4 + (compactLayout ? 1 : 0) + (DebugSettings.godMode ? 2 : 0);
            unchecked
            {
                key = key * 31 + (AbilityKnowledgeComp.ActiveFor(pawn)?.Spells.Count ?? 0);
                if (magic != null && magic.IsMagicUser && magic.MagicData != null)
                {
                    foreach (MagicPower p in magic.MagicData.AllMagicPowersWithSkills)
                    {
                        key = key * 31 + p.level;
                    }
                }
                if (might != null && might.IsMightUser && might.MightData != null)
                {
                    foreach (MightPower p in might.MightData.AllMightPowersWithSkills)
                    {
                        key = key * 31 + p.level;
                    }
                }
            }
            if (key == viewHeightKey)
            {
                return viewHeightCache;
            }
            viewHeightKey = key;
            viewHeightCache = MeasureViewHeight(width);
            return viewHeightCache;
        }

        private float MeasureViewHeight(float width)
        {
            float h = 12f;
            if (magic != null && magic.IsMagicUser && magic.MagicData != null)
            {
                h += 30f + (compactLayout ? ClassProgressHeight : 0f) + 22f;                       // header + info panel + gap + section label
                h += StatBlockHeight(magicStats, width);
                foreach (MagicPower power in magic.MagicData.AllMagicPowersWithSkills)
                {
                    TMAbilityDef ability = power.abilityDef as TMAbilityDef;
                    if (ability == null || !magicRenderable.Contains(power))
                    {
                        continue;
                    }
                    h += MagicPowerCardHeight(width, power) + 10f;
                }
                h += 12f;
            }
            if (might != null && might.IsMightUser && might.MightData != null)
            {
                h += 30f + (compactLayout ? ClassProgressHeight : 0f) + 22f;
                h += StatBlockHeight(mightStats, width);
                foreach (MightPower power in might.MightData.AllMightPowersWithSkills)
                {
                    TMAbilityDef ability = power.abilityDef as TMAbilityDef;
                    if (ability == null || !mightRenderable.Contains(power))
                    {
                        continue;
                    }
                    h += MightPowerCardHeight(width, power) + 10f;
                }
                h += 12f;
            }
            var sleeping = GrimoireKnowledge.SleepingSpells(pawn);
            if (sleeping.Count > 0) h += 42f + sleeping.Count * 56f;
            return h;
        }

        private static float ResourcesPanelHeight(int bars) => bars == 0 ? 0f : 56f + bars * 54f;

        private List<MagicPowerSkill> magicStats;
        private List<MightPowerSkill> mightStats;
        private List<TMAbilityDef> magicFilter;
        private List<TMAbilityDef> mightFilter;
        private HashSet<MagicPower> magicRenderable;
        private HashSet<MightPower> mightRenderable;
        private bool filtersBuilt;
        private ClassProgression.ClassProgressionState progressionSnapshot;
        private int progressionRevision;
        private bool spaceResearchSnapshot;
        private HashSet<MagicPower> chaosSnapshot = new HashSet<MagicPower>();

        private void EnsureFilters()
        {
            var progression = ClassProgression.ClassIdentity.Component(pawn)?.State;
            var activeChaos = new HashSet<MagicPower>(TM_ChaosProgression.ActivePowers(magic));
            if (filtersBuilt && ReferenceEquals(progressionSnapshot, progression) && progressionRevision == (progression?.Revision ?? 0)
                && spaceResearchSnapshot == Odyssey.SpaceSpellProgression.ResearchComplete
                && chaosSnapshot.SetEquals(activeChaos))
            {
                return;
            }
            magicStats = magic != null && magic.IsMagicUser && magic.MagicData != null
                ? new List<MagicPowerSkill>
                {
                    magic.MagicData.MagicPowerSkill_global_regen[0],
                    magic.MagicData.MagicPowerSkill_global_eff[0],
                    magic.MagicData.MagicPowerSkill_global_spirit[0],
                }
                : new List<MagicPowerSkill>();
            mightStats = might != null && might.IsMightUser && might.MightData != null
                ? new List<MightPowerSkill>
                {
                    might.MightData.MightPowerSkill_global_refresh[0],
                    might.MightData.MightPowerSkill_global_seff[0],
                    might.MightData.MightPowerSkill_global_strength[0],
                    might.MightData.MightPowerSkill_global_endurance[0],
                }
                : new List<MightPowerSkill>();
            magicFilter = MagicAbilityFilter(GetMagicClassGroup());
            chaosSnapshot = activeChaos;
            magicFilter.AddRange(activeChaos.SelectMany(p => p.TMabilityDefs).OfType<TMAbilityDef>());
            viewHeightKey = long.MinValue;
            mightFilter = MightAbilityFilter(GetMightClassGroup());
            magicRenderable = RenderableMagicPowers();
            mightRenderable = RenderableMightPowers();
            filtersBuilt = true;
            progressionSnapshot = progression;
            progressionRevision = progression?.Revision ?? 0;
            spaceResearchSnapshot = Odyssey.SpaceSpellProgression.ResearchComplete;
        }

        // Shared abilities have separate saved owners in different class lists.
        // Keep the pawn's class owner, deduplicating by the base definition rather
        // than the currently selected rank variant.
        private HashSet<MagicPower> RenderableMagicPowers()
        {
            var allowed = new HashSet<MagicPower>();
            if (magic == null || !magic.IsMagicUser || magic.MagicData == null)
            {
                return allowed;
            }
            List<MagicPower> all = magic.MagicData.AllMagicPowersWithSkills;
            List<TMAbilityDef> ownDefs = OwnMagicClassDefs();
            var claimed = new HashSet<AbilityUser.AbilityDef>();
            AddRenderable(allowed, claimed, chaosSnapshot, magicFilter, null);
            List<MagicPower> classGroup = magic.customClass == null ? GetMagicClassGroup() : null;
            AddRenderable(allowed, claimed, classGroup, magicFilter, null);
            AddRenderable(allowed, claimed, all, magicFilter, ownDefs);
            AddRenderable(allowed, claimed, all, magicFilter, null);
            allowed.RemoveWhere(p => !Odyssey.SpaceSpellProgression.Visible(p));
            return allowed;
        }

        private static void AddRenderable(HashSet<MagicPower> allowed, HashSet<AbilityUser.AbilityDef> claimed,
            IEnumerable<MagicPower> powers, List<TMAbilityDef> filter, List<TMAbilityDef> ownDefs)
        {
            if (powers == null) return;
            foreach (MagicPower p in powers)
            {
                if (!(p?.TMabilityDefs?.FirstOrDefault() is TMAbilityDef ab))
                {
                    continue;
                }
                if (ownDefs != null && !p.TMabilityDefs.Any(d => d is TMAbilityDef tm && ownDefs.Contains(tm)))
                {
                    continue;
                }
                if (p.TMabilityDefs.Any(d => d is TMAbilityDef tm && filter.Contains(tm)) && claimed.Add(ab))
                {
                    allowed.Add(p);
                }
            }
        }

        private HashSet<MightPower> RenderableMightPowers()
        {
            var allowed = new HashSet<MightPower>();
            if (might == null || !might.IsMightUser || might.MightData == null)
            {
                return allowed;
            }
            List<MightPower> all = might.MightData.AllMightPowersWithSkills;
            List<TMAbilityDef> ownDefs = OwnMightClassDefs();
            var claimed = new HashSet<AbilityUser.AbilityDef>();
            List<MightPower> classGroup = might.customClass == null ? GetMightClassGroup() : null;
            AddRenderableMight(allowed, claimed, classGroup, mightFilter, null);
            AddRenderableMight(allowed, claimed, all, mightFilter, ownDefs);
            AddRenderableMight(allowed, claimed, all, mightFilter, null);
            return allowed;
        }

        private static void AddRenderableMight(HashSet<MightPower> allowed, HashSet<AbilityUser.AbilityDef> claimed,
            IEnumerable<MightPower> powers, List<TMAbilityDef> filter, List<TMAbilityDef> ownDefs)
        {
            if (powers == null) return;
            foreach (MightPower p in powers)
            {
                if (!(p?.TMabilityDefs?.FirstOrDefault() is TMAbilityDef ab))
                {
                    continue;
                }
                if (ownDefs != null && !p.TMabilityDefs.Any(d => d is TMAbilityDef tm && ownDefs.Contains(tm)))
                {
                    continue;
                }
                if (p.TMabilityDefs.Any(d => d is TMAbilityDef tm && filter.Contains(tm)) && claimed.Add(ab))
                {
                    allowed.Add(p);
                }
            }
        }

        private List<TMAbilityDef> OwnMagicClassDefs()
        {
            List<TMAbilityDef> defs = new List<TMAbilityDef>();
            if (magic == null)
            {
                return defs;
            }
            if (magic.customClass?.classMageAbilities != null)
            {
                defs.AddRange(magic.customClass.classMageAbilities);
            }
            if (magic.AdvancedClasses != null)
            {
                foreach (TMDefs.TM_CustomClass cc in magic.AdvancedClasses)
                {
                    if (cc?.classMageAbilities != null)
                    {
                        defs.AddRange(cc.classMageAbilities);
                    }
                }
            }
            return defs;
        }

        private List<TMAbilityDef> OwnMightClassDefs()
        {
            List<TMAbilityDef> defs = new List<TMAbilityDef>();
            if (might == null)
            {
                return defs;
            }
            if (might.customClass?.classFighterAbilities != null)
            {
                defs.AddRange(might.customClass.classFighterAbilities);
            }
            if (might.AdvancedClasses != null)
            {
                foreach (TMDefs.TM_CustomClass cc in might.AdvancedClasses)
                {
                    if (cc?.classFighterAbilities != null)
                    {
                        defs.AddRange(cc.classFighterAbilities);
                    }
                }
            }
            return defs;
        }

        private float StatBlockHeight(List<MagicPowerSkill> stats, float width)
        {
            if (stats.Count == 0)
            {
                return 0f;
            }
            float rows = 0f;
            foreach (MagicPowerSkill stat in stats)
            {
                rows += SkillRowHeight(SkillRowText(stat.label, stat.level, stat.levelMax, stat.desc), width - 14f - 50f) + 12f;
            }
            return 26f + rows + 14f + 12f;
        }

        private float StatBlockHeight(List<MightPowerSkill> stats, float width)
        {
            if (stats.Count == 0)
            {
                return 0f;
            }
            float rows = 0f;
            foreach (MightPowerSkill stat in stats)
            {
                rows += SkillRowHeight(SkillRowText(stat.label, stat.level, stat.levelMax, stat.desc), width - 14f - 50f) + 12f;
            }
            return 26f + rows + 14f + 12f;
        }

        private float MagicPowerCardHeight(float width, MagicPower power)
        {
            TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
            float descHeight = PowerDescriptionHeight(power, width);
            float headerHeight = PowerHeaderHeight(MagicTitle(power), width);
            float skillRows = 0f;
            MagicPowerSkill s = magic.MagicData.GetSkill_Power(ability);
            if (s != null) skillRows += SkillRowHeight(SkillRowText(s.label, s.level, s.levelMax, s.desc), width - 16f - 50f) + 10f;
            s = magic.MagicData.GetSkill_Efficiency(ability);
            if (s != null) skillRows += SkillRowHeight(SkillRowText(s.label, s.level, s.levelMax, s.desc), width - 16f - 50f) + 10f;
            s = magic.MagicData.GetSkill_Versatility(ability);
            if (s != null) skillRows += SkillRowHeight(SkillRowText(s.label, s.level, s.levelMax, s.desc), width - 16f - 50f) + 10f;
            return Mathf.Max(IconSize, headerHeight + descHeight) + skillRows + 16f + 8f;
        }

        private float MightPowerCardHeight(float width, MightPower power)
        {
            TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
            float descHeight = PowerDescriptionHeight(power, width);
            float headerHeight = PowerHeaderHeight(MightTitle(power), width);
            float skillRows = 0f;
            MightPowerSkill s = might.MightData.GetSkill_Power(ability);
            if (s != null) skillRows += SkillRowHeight(MightSkillRow(ability, s), width - 16f - 50f) + 10f;
            s = might.MightData.GetSkill_Efficiency(ability);
            if (s != null) skillRows += SkillRowHeight(MightSkillRow(ability, s), width - 16f - 50f) + 10f;
            s = might.MightData.GetSkill_Versatility(ability);
            if (s != null) skillRows += SkillRowHeight(MightSkillRow(ability, s), width - 16f - 50f) + 10f;
            return Mathf.Max(IconSize, headerHeight + descHeight) + skillRows + 16f + 8f;
        }

        private float PowerDescriptionHeight(MagicPower power, float width)
        {
            string desc = power.abilityDescDef?.description;
            if (power.level > 0 && power.level < power.maxLevel && power.nextLevelAbilityDescDef?.description != null)
            {
                desc += "\n\n" + "TM_MCU_NextLevel".Translate() + "\n" + power.nextLevelAbilityDescDef.description;
            }
            if (string.IsNullOrEmpty(desc))
            {
                return 0f;
            }
            Text.Font = GameFont.Tiny;
            float h = Text.CalcHeight(desc, width - 24f - IconSize - 10f);
            return h;
        }

        private bool TrailLore(TMAbilityDef ability) => ability == TorannMagicDefOf.TM_RangerTraining && ClassProgression.RangerCareer.Path(pawn)?.RangerTrailLore == true;

        private bool BladeMemory(TMAbilityDef ability) => ability == TorannMagicDefOf.TM_BladeFocus && ClassProgression.BladeCareer.Path(pawn)?.BladeMemory == true;

        private bool PatientAim(TMAbilityDef ability)
            => ability == TorannMagicDefOf.TM_SniperFocus && ClassProgression.ClassIdentity.TryGetState(pawn, out var state)
                && state.Find(ClassProgression.SniperFoundation.Id)?.SniperPatientAim == true;

        private string MightSkillRow(TMAbilityDef ability, MightPowerSkill skill)
            => TrailLore(ability) ? SkillRowText("TM_TrailLoreName",skill.level,skill.levelMax,"TM_TrailLoreTraining")
                : BladeMemory(ability) ? SkillRowText("TM_BladeMemoryName",skill.level,skill.levelMax,"TM_BladeMemoryTraining")
                : PatientAim(ability) ? SkillRowText("TM_PatientAimName",skill.level,skill.levelMax,"TM_PatientAimTraining")
                : SkillRowText(skill.label,skill.level,skill.levelMax,skill.desc);

        private string MightPowerDescription(MightPower power)
        {
            if (TrailLore(power.abilityDef as TMAbilityDef)) return "TM_TrailLoreTraining".Translate();
            if (BladeMemory(power.abilityDef as TMAbilityDef)) return "TM_BladeMemoryTraining".Translate();
            if (PatientAim(power.abilityDef as TMAbilityDef)) return "TM_PatientAimDescription".Translate();
            string desc = power.abilityDescDef?.description;
            if (power.level > 0 && power.level < power.maxLevel && power.nextLevelAbilityDescDef?.description != null)
                desc += "\n\n" + "TM_MCU_NextLevel".Translate() + "\n" + power.nextLevelAbilityDescDef.description;
            return desc;
        }

        private float PowerDescriptionHeight(MightPower power, float width)
        {
            string desc = MightPowerDescription(power);
            if (string.IsNullOrEmpty(desc))
            {
                return 0f;
            }
            Text.Font = GameFont.Tiny;
            return Text.CalcHeight(desc, width - 24f - IconSize - 10f);
        }

        private string ClassLabel()
        {
            if (ClassProgression.ClassIdentity.TryGetState(pawn, out var current)) return ClassProgression.ClassIdentity.SummaryWithStages(current);
            if (pawn.story?.traits == null)
            {
                return "unawakened";
            }
            List<TraitDef> magicTraits = TM_Data.MagicTraits;
            for (int i = 0; i < magicTraits.Count; i++)
            {
                if (pawn.story.traits.HasTrait(magicTraits[i]))
                {
                    return magicTraits[i].degreeDatas[0].label;
                }
            }
            List<TraitDef> mightTraits = TM_Data.MightTraits;
            for (int i = 0; i < mightTraits.Count; i++)
            {
                if (pawn.story.traits.HasTrait(mightTraits[i]))
                {
                    return mightTraits[i].degreeDatas[0].label;
                }
            }
            return "unawakened";
        }

        private string ClassDescription()
        {
            if (ClassProgression.ClassIdentity.TryGetState(pawn, out var current))
            {
                string text = ClassProgression.ClassIdentity.Stages(current);
                if (ClassProgression.ClassIdentity.IsPriest(pawn)) text += "\n\n" + "TM_ProgressionPriestVow".Translate();
                return text;
            }
            // full vanilla trait tip: description + stat offsets/effects, same text
            // the trait tooltip shows
            Trait trait = ClassTraitOnPawn();
            return trait?.TipString(pawn);
        }

        private Trait ClassTraitOnPawn()
        {
            if (pawn.story?.traits == null)
            {
                return null;
            }
            if (magic?.customClass?.classTrait != null && pawn.story.traits.HasTrait(magic.customClass.classTrait))
            {
                return pawn.story.traits.GetTrait(magic.customClass.classTrait, magic.customClass.traitDegree);
            }
            if (might?.customClass?.classTrait != null && pawn.story.traits.HasTrait(might.customClass.classTrait))
            {
                return pawn.story.traits.GetTrait(might.customClass.classTrait, might.customClass.traitDegree);
            }
            foreach (TraitDef td in TM_Data.MagicTraits)
            {
                if (pawn.story.traits.HasTrait(td))
                {
                    return pawn.story.traits.GetTrait(td);
                }
            }
            foreach (TraitDef td in TM_Data.MightTraits)
            {
                if (pawn.story.traits.HasTrait(td))
                {
                    return pawn.story.traits.GetTrait(td);
                }
            }
            return null;
        }

        private static void Label(Rect rect, string text, TextAnchor anchor, Color color)
        {
            GUI.color = color;
            GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = anchor, wordWrap = true };
            // match vanilla font metrics, otherwise Text.CalcHeight underestimates
            // and IMGUI labels clip the text
            switch (Text.Font)
            {
                case GameFont.Tiny: style.fontSize = 12; break;
                case GameFont.Small: style.fontSize = 14; break;
                case GameFont.Medium: style.fontSize = 16; break;
            }
            GUI.Label(rect, text, style);
            GUI.color = Color.white;
        }

        private static string IsekaiRank(int level)
        {
            if (level <= 0)
            {
                return null;
            }
            string[] ranks = { "F", "E", "D", "C", "B", "A", "S", "SS", "SSS", "Nation" };
            return ranks[Mathf.Min(level / 10, 9)];
        }

        #endregion
    }
}
