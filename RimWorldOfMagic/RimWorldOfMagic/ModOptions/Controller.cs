using System;
using UnityEngine;
using Verse;

namespace TorannMagic.ModOptions
{
    public class Controller : Mod
    {
        public static Controller Instance;

        private bool reset = false;
        private bool challenge = false;
        private bool easy = false;
        private bool classOptions = false;
        private bool eventOptions = false;
        private bool factionOptions = false;
        private bool displayOptions = false;

        private Vector2 scrollPosition = Vector2.zero;

        private string deathExplosionDmgMin = "20.0";
        private string deathExplosionDmgMax = "50.0";

        private int deathExplosionDmgMinInt = 20;
        private int deathExplosionDmgMaxInt = 50;

        public override string SettingsCategory()
        {
            return "A RimWorld of Magic";
        }

        public Controller(ModContentPack content) : base(content)
        {
            Controller.Instance = this;
            Settings.Instance = base.GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            int num = 0;
            float rowHeight = 28f;
            Rect sRect = new Rect(canvas.x, canvas.y, canvas.width - 36f, canvas.height + 672f);
            scrollPosition = GUI.BeginScrollView(canvas, scrollPosition, sRect, false, true);

            Rect rect1 = new Rect(canvas);
            rect1.width -= 16f;
            // two rows of top margin: the settings viewport starts below the
            // window header, and GetRowRect lays rows out in scroll-local coords
            num++;
            num++;
            deathExplosionDmgMin = ModOptions.Settings.Instance.deathExplosionMin.ToString();
            deathExplosionDmgMax = ModOptions.Settings.Instance.deathExplosionMax.ToString();

            // sub-window buttons, one full-width row each
            Rect btn = UIHelper.GetRowRect(rect1, rowHeight, num);
            if (Widgets.ButtonText(btn, "TM_ClassOptions".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new ClassOptionsWindow());
            }
            TooltipHandler.TipRegion(btn, "TM_ClassOptionsDesc".Translate());
            num++;
            btn = UIHelper.GetRowRect(rect1, rowHeight, num);
            if (Widgets.ButtonText(btn, "TM_FactionOptions".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new FactionOptionsWindow());
            }
            TooltipHandler.TipRegion(btn, "TM_FactionOptionsDesc".Translate());
            num++;
            btn = UIHelper.GetRowRect(rect1, rowHeight, num);
            if (Widgets.ButtonText(btn, "TM_EventOptions".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new EventOptionsWindow());
            }
            TooltipHandler.TipRegion(btn, "TM_EventOptionsDesc".Translate());
            num++;
            btn = UIHelper.GetRowRect(rect1, rowHeight, num);
            if (Widgets.ButtonText(btn, "TM_GolemOptions".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new GolemOptionsWindow());
            }
            TooltipHandler.TipRegion(btn, "TM_GolemOptionsDesc".Translate());
            num++;
            btn = UIHelper.GetRowRect(rect1, rowHeight, num);
            if (Widgets.ButtonText(btn, "TM_DisplayOptions".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new DisplayOptionsWindow());
            }
            TooltipHandler.TipRegion(btn, "TM_DisplayOptionsDesc".Translate());

            num++;
            num++;
            Rect row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.xpMultiplier = Widgets.HorizontalSlider(row, Settings.Instance.xpMultiplier, .1f, 2f, false, "XPMultiplier".Translate() + " " + Settings.Instance.xpMultiplier, ".1", "2", .1f);
            TooltipHandler.TipRegion(row, "XPMultiplierDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.needMultiplier = Widgets.HorizontalSlider(row, Settings.Instance.needMultiplier, 0f, 10f, false, "NeedMultiplier".Translate() + " " + Settings.Instance.needMultiplier, "0", "10", .1f);
            TooltipHandler.TipRegion(row, "NeedMultiplierDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.abilityCooldownMultiplier = Widgets.HorizontalSlider(row, Compatibility.AbilityTiming.SafeMultiplier(Settings.Instance.abilityCooldownMultiplier), .1f, 5f, false, "TM_GlobalCooldown".Translate() + " " + Settings.Instance.abilityCooldownMultiplier.ToString("P0"), "10%", "500%", .1f);
            TooltipHandler.TipRegion(row, "TM_GlobalTimingDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.abilityWarmupMultiplier = Widgets.HorizontalSlider(row, Compatibility.AbilityTiming.SafeMultiplier(Settings.Instance.abilityWarmupMultiplier), .1f, 5f, false, "TM_GlobalWarmup".Translate() + " " + Settings.Instance.abilityWarmupMultiplier.ToString("P0"), "10%", "500%", .1f);
            TooltipHandler.TipRegion(row, "TM_GlobalTimingDesc".Translate());
            num++;
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_NewProgressionBeta".Translate(), ref Settings.Instance.newProgressionBeta, false);
            TooltipHandler.TipRegion(row, "TM_NewProgressionBetaDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            GUI.color = Color.yellow;
            Widgets.Label(row, "TM_RequiresRestart".Translate());
            GUI.color = Color.white;
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.magicyteChance = Widgets.HorizontalSlider(row, Settings.Instance.magicyteChance, 0, .05f, false, "MagicyteChance".Translate() + " " + Settings.Instance.magicyteChance, "0%", "5%", .001f);
            TooltipHandler.TipRegion(row, "MagicyteChanceDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_DeathRetaliationIsLethal".Translate(), ref Settings.Instance.deathRetaliationIsLethal, false);
            TooltipHandler.TipRegion(row, "TM_DeathRetaliationIsLethalDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.deathRetaliationChance = Widgets.HorizontalSlider(row, Settings.Instance.deathRetaliationChance, 0f, 1f, false, "TM_DeathRetaliationChance".Translate() + " " + Settings.Instance.deathRetaliationChance.ToString("P0"), "0", "1", .01f);
            TooltipHandler.TipRegion(row, "TM_DeathRetaliationChanceDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.deathRetaliationDelayFactor = Widgets.HorizontalSlider(row, Settings.Instance.deathRetaliationDelayFactor, .1f, 4f, false, "TM_DeathRetaliationDelay".Translate() + " " + Settings.Instance.deathRetaliationDelayFactor.ToString("P0"), "0", "4", .01f);
            TooltipHandler.TipRegion(row, "TM_DeathRetaliationDelayDesc".Translate());

            num++;
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_enableAutocast".Translate(), ref Settings.Instance.autocastEnabled, false);
            TooltipHandler.TipRegion(row, "TM_enableAutocastDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_queueAutocast".Translate(), ref Settings.Instance.autocastQueueing, false);
            TooltipHandler.TipRegion(row, "TM_queueAutocastDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.autocastMinThreshold = Widgets.HorizontalSlider(row, Settings.Instance.autocastMinThreshold, 0f, 1f, false, "TM_autocastUndraftedThreshold".Translate() + " " + (Settings.Instance.autocastMinThreshold * 100) + "%", "0", "1", .01f);
            TooltipHandler.TipRegion(row, "TM_autocastUndraftedThresholdDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.autocastCombatMinThreshold = Widgets.HorizontalSlider(row, Settings.Instance.autocastCombatMinThreshold, 0f, 1f, false, "TM_autocastDraftedThreshold".Translate() + " " + (Settings.Instance.autocastCombatMinThreshold * 100) + "%", "0", "1", .01f);
            TooltipHandler.TipRegion(row, "TM_autocastDraftedThresholdDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.autocastEvaluationFrequency = Mathf.RoundToInt(Widgets.HorizontalSlider(row, Settings.Instance.autocastEvaluationFrequency, 60, 600, false, "TM_autocastEvaluationFrequency".Translate() + " " + (Settings.Instance.autocastEvaluationFrequency / 60) + "seconds", "1", "10", .1f));
            TooltipHandler.TipRegion(row, "TM_autocastEvaluationFrequencyDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_autocastAnimals".Translate(), ref Settings.Instance.autocastAnimals, false);
            TooltipHandler.TipRegion(row, "TM_autocastAnimalsDesc".Translate());

            num++;
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "AICanCast".Translate(), ref Settings.Instance.AICasting, false);
            TooltipHandler.TipRegion(row, "AICanCastDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "AIHardMode".Translate(), ref Settings.Instance.AIHardMode, !ModOptions.Settings.Instance.AICasting);
            TooltipHandler.TipRegion(row, "AIHardModeDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_AggressiveAICasting".Translate(), ref Settings.Instance.AIAggressiveCasting, false);
            TooltipHandler.TipRegion(row, "TM_AggressiveAICastingDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_SlavesCanScribe".Translate(), ref Settings.Instance.SlavesCanScribe, false);
            TooltipHandler.TipRegion(row, "TM_SlavesCanScribeDesc".Translate());

            num++;
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.undeadUpkeepMultiplier = Widgets.HorizontalSlider(row, Settings.Instance.undeadUpkeepMultiplier, 0f, 5f, false, "TM_UndeadUpkeepMultiplier".Translate() + " " + Settings.Instance.undeadUpkeepMultiplier.ToString("P1"), "0", "5", .01f);
            TooltipHandler.TipRegion(row, "TM_UndeadUpkeepMultiplierDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.paracyteSoftCap = Widgets.HorizontalSlider(row, Settings.Instance.paracyteSoftCap, 0, 500, false, "TM_ParacyteSoftCap".Translate() + " " + (Settings.Instance.paracyteSoftCap), "0", "500", 1);
            TooltipHandler.TipRegion(row, "TM_ParacyteSoftCapDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_ParacyteMagesCount".Translate(), ref Settings.Instance.paracyteMagesCount, false);
            TooltipHandler.TipRegion(row, "TM_ParacyteMagesCountDesc".Translate());

            num++;
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "unrestrictedBloodTypesForBloodMagic".Translate(), ref Settings.Instance.unrestrictedBloodTypes, false);
            TooltipHandler.TipRegion(row, "unrestrictedBloodTypesForBloodMagicDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_UnrestrictedWeaponCopy".Translate(), ref Settings.Instance.unrestrictedWeaponCopy, false);
            TooltipHandler.TipRegion(row, "TM_UnrestrictedWeaponCopyDesc".Translate());
            num++;
            row = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(row, "TM_CreateAreas".Translate(), ref Settings.Instance.autoCreateAreas, false);
            TooltipHandler.TipRegion(row, "TM_CreateAreasDesc".Translate());

            num++;
            num++;
            Rect presetLeft = UIHelper.GetRowRect(rect1, rowHeight, num);
            presetLeft.width = rect1.width / 2f - 8f;
            Rect presetRight = new Rect(presetLeft.xMax + 16f, presetLeft.y, presetLeft.width, presetLeft.height);
            reset = Widgets.ButtonText(presetLeft, "Default", true, false, true);
            challenge = Widgets.ButtonText(presetRight, "Challenge me!", true, false, true);
            if (reset)
            {
                Settings.Instance.xpMultiplier = 1f;
                Settings.Instance.abilityCooldownMultiplier = Settings.Instance.abilityWarmupMultiplier = 1f;
                Settings.Instance.needMultiplier = 1f;
                Settings.Instance.deathExplosionRadius = 3f;
                Settings.Instance.deathExplosionMin = 20;
                Settings.Instance.deathExplosionMax = 50;
                Settings.Instance.AICasting = true;
                Settings.Instance.AIHardMode = false;
                Settings.Instance.baseMageChance = 1f;
                Settings.Instance.baseFighterChance = 1f;
                Settings.Instance.advMageChance = 0.5f;
                Settings.Instance.advFighterChance = 0.5f;
                Settings.Instance.magicyteChance = .005f;
                Settings.Instance.AIAggressiveCasting = true;
                Settings.Instance.paracyteSoftCap = 50f;
                Settings.Instance.undeadUpkeepMultiplier = 1f;
                Settings.Instance.paracyteMagesCount = true;
                Settings.Instance.riftChallenge = 1f;
                Settings.Instance.wanderingLichChallenge = 1f;
                Settings.Instance.demonAssaultChallenge = 1f;
                Settings.Instance.autocastAnimals = false;
                Settings.Instance.unrestrictedWeaponCopy = false;
                Settings.Instance.deathRetaliationIsLethal = true;
                Settings.Instance.deathRetaliationChance = 1f;
                Settings.Instance.deathRetaliationDelayFactor = 1f;

                Settings.Instance.autocastEnabled = true;
                Settings.Instance.autocastMinThreshold = .7f;
                Settings.Instance.autocastCombatMinThreshold = .2f;
                Settings.Instance.autocastEvaluationFrequency = 180;
            }

            if (challenge)
            {
                Settings.Instance.xpMultiplier = .75f;
                Settings.Instance.needMultiplier = .75f;
                Settings.Instance.deathExplosionRadius = 5f;
                Settings.Instance.deathExplosionMin = 30;
                Settings.Instance.deathExplosionMax = 60;
                Settings.Instance.AICasting = true;
                Settings.Instance.AIHardMode = true;
                Settings.Instance.baseMageChance = .8f;
                Settings.Instance.baseFighterChance = .8f;
                Settings.Instance.advMageChance = 1.5f;
                Settings.Instance.advFighterChance = 1.5f;
                Settings.Instance.magicyteChance = .003f;
                Settings.Instance.AIAggressiveCasting = true;
                Settings.Instance.paracyteSoftCap = 30f;
                Settings.Instance.undeadUpkeepMultiplier = 1.5f;
                Settings.Instance.paracyteMagesCount = true;
                Settings.Instance.riftChallenge = 3f;
                Settings.Instance.demonAssaultChallenge = 3f;
                Settings.Instance.wanderingLichChallenge = 3f;
                Settings.Instance.autocastAnimals = false;
                Settings.Instance.unrestrictedWeaponCopy = false;
                Settings.Instance.deathRetaliationIsLethal = true;
                Settings.Instance.deathRetaliationChance = 1f;
                Settings.Instance.deathRetaliationDelayFactor = .3f;

                Settings.Instance.autocastEnabled = true;
                Settings.Instance.autocastMinThreshold = .8f;
                Settings.Instance.autocastCombatMinThreshold = .2f;
                Settings.Instance.autocastEvaluationFrequency = 300;
            }

            GUI.EndScrollView();

        }

        public static class UIHelper
        {
            public static Rect GetRowRect(Rect inRect, float rowHeight, int row)
            {
                float y = rowHeight * (float)row;
                Rect result = new Rect(inRect.x, y, inRect.width, rowHeight);
                return result;
            }
        }

        private string Rarity(float val)
        {
            string rarity = "";
            if (val == 0)
            {
                rarity = "None";
            }
            else if (val < .2f)
            {
                rarity = "Very Rare";
            }
            else if (val < .5f)
            {
                rarity = "Rare";
            }
            else if (val < 1f)
            {
                rarity = "Uncommon";
            }
            else if (val < 2f)
            {
                rarity = "Common";
            }
            else
            {
                rarity = "Frequent";
            }
            return rarity;
        }

        private string Challenge(float val)
        {
            string rarity = "";
            if (val == 0)
            {
                rarity = "None (never happens)";
            }
            else if (val <= 1)
            {
                rarity = "Easy";
            }
            else if (val <= 2)
            {
                rarity = "Normal";
            }
            else
            {
                rarity = "Hard";
            }

            return rarity;
        }
    }
}
