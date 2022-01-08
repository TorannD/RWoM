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
            Rect sRect = new Rect(canvas.x, canvas.y, canvas.width - 36f, canvas.height + 360f);
            scrollPosition = GUI.BeginScrollView(canvas, scrollPosition, sRect, false, true);
            //Widgets.BeginScrollView(canvas, ref scrollPosition, canvas, true);

            Rect rect1 = new Rect(canvas);
            rect1.width /= 2.4f;
            num++;
            num++;
            SettingsRef settingsRef = new SettingsRef();
            deathExplosionDmgMin = settingsRef.deathExplosionMin.ToString();
            deathExplosionDmgMax = settingsRef.deathExplosionMax.ToString();
            Rect rowRect = UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.xpMultiplier = Widgets.HorizontalSlider(rowRect, Settings.Instance.xpMultiplier, .1f, 2f, false, "XPMultiplier".Translate() + " " + Settings.Instance.xpMultiplier, ".1", "2", .1f);
            Rect rowRectShiftRight = UIHelper.GetRowRect(rowRect, rowHeight, num);
            rowRectShiftRight.x += rowRect.width + 56f;
            rowRectShiftRight.width /= 3;
            classOptions = Widgets.ButtonText(rowRectShiftRight, "Class Options", true, false, true);
            if (classOptions)
            {
                Rect rect = new Rect(64f, 64f, 480, 640);
                ClassOptionsWindow newWindow = new ClassOptionsWindow();                
                Find.WindowStack.Add(newWindow);
            }
            Rect rowRectShiftRightPlus = UIHelper.GetRowRect(rowRect, rowHeight, num);
            rowRectShiftRightPlus.x += (rowRect.width+rowRectShiftRight.width) + 56f;
            rowRectShiftRightPlus.width /= 3;
            factionOptions = Widgets.ButtonText(rowRectShiftRightPlus, "Faction Options", true, false, true);
            if (factionOptions)
            {
                Rect rect = new Rect(64f, 64f, 480, 640);
                FactionOptionsWindow newWindow = new FactionOptionsWindow();
                Find.WindowStack.Add(newWindow);
            }
            num++;
            Rect rowRect2 = UIHelper.GetRowRect(rowRect, rowHeight, num);
            Settings.Instance.needMultiplier = Widgets.HorizontalSlider(rowRect2, Settings.Instance.needMultiplier, 0f, 10f, false, "NeedMultiplier".Translate() + " " + Settings.Instance.needMultiplier, "0", "10", .1f);
            Rect rowRect2ShiftRight = UIHelper.GetRowRect(rowRect2, rowHeight, num);
            rowRect2ShiftRight.x += rowRect.width + 56f;
            rowRect2ShiftRight.width /= 3;
            eventOptions = Widgets.ButtonText(rowRect2ShiftRight, "Event Options", true, false, true);
            if (eventOptions)
            {
                Rect rect = new Rect(64f, 64f, 400, 400);
                EventOptionsWindow newWindow = new EventOptionsWindow();
                Find.WindowStack.Add(newWindow);
            }
            Rect rowRect2ShiftRightPlus = UIHelper.GetRowRect(rowRect2, rowHeight, num);
            rowRect2ShiftRightPlus.x += (rowRect2.width + rowRect2ShiftRight.width) + 56f;
            rowRect2ShiftRightPlus.width /= 3;
            factionOptions = Widgets.ButtonText(rowRect2ShiftRightPlus, "Golem Options", true, false, true);
            if (factionOptions)
            {
                Rect rect = new Rect(64f, 64f, 480, 640);
                GolemOptionsWindow newWindow = new GolemOptionsWindow();
                Find.WindowStack.Add(newWindow);
            }
            num++;
            Rect rowRect21 = UIHelper.GetRowRect(rowRect2, rowHeight, num);
            Settings.Instance.magicyteChance = Widgets.HorizontalSlider(rowRect21, Settings.Instance.magicyteChance, 0, .05f, false, "MagicyteChance".Translate() + " " + Settings.Instance.magicyteChance, "0%", "5%", .001f);
            num++;            
            Rect rowRect3 = UIHelper.GetRowRect(rowRect21, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect3, "TM_DeathRetaliationIsLethal".Translate(), ref Settings.Instance.deathRetaliationIsLethal, false);
            //rowRect3.width = rowRect3.width * .7f;
            //Settings.Instance.deathExplosionRadius = Widgets.HorizontalSlider(rowRect3, Settings.Instance.deathExplosionRadius, .1f, 6f, false, "DeathRadius".Translate() + " " + Settings.Instance.deathExplosionRadius, ".1", "6", .1f);
            //Rect rowRect31 = new Rect(rowRect3.xMax + 4f, rowRect3.y, rowRect2.width/2, rowRect3.height);
            //Widgets.TextFieldNumericLabeled<int>(rowRect31, "DeathExplosionMin".Translate(), ref Settings.Instance.deathExplosionMin, ref this.deathExplosionDmgMin, 0, 100);            
            //Rect rowRect32 = new Rect(rowRect31.xMax + 4f, rowRect3.y, rowRect2.width/2, rowRect3.height);
            //Widgets.TextFieldNumericLabeled<int>(rowRect32, "DeathExplosionMax".Translate(), ref Settings.Instance.deathExplosionMax, ref this.deathExplosionDmgMax, 0, 200);
            num++;
            Rect rowRect4 = UIHelper.GetRowRect(rowRect3, rowHeight, num);
            Settings.Instance.deathRetaliationChance = Widgets.HorizontalSlider(rowRect4, Settings.Instance.deathRetaliationChance, 0f, 1f, false, "TM_DeathRetaliationChance".Translate() + " " + Settings.Instance.deathRetaliationChance.ToString("P0"), "0", "1", .01f);
            Rect rowRect4ShiftRight = UIHelper.GetRowRect(rowRect4, rowHeight, num);
            rowRect4ShiftRight.x += rowRect4.width + 56f;
            Widgets.CheckboxLabeled(rowRect4ShiftRight, "TM_enableAutocast".Translate(), ref Settings.Instance.autocastEnabled, false);
            num++;
            Rect rowRect5 = UIHelper.GetRowRect(rowRect4, rowHeight, num);
            Settings.Instance.deathRetaliationDelayFactor = Widgets.HorizontalSlider(rowRect5, Settings.Instance.deathRetaliationDelayFactor, .1f, 4f, false, "TM_DeathRetaliationDelay".Translate() + " " + Settings.Instance.deathRetaliationDelayFactor.ToString("P0"), "0", "4", .01f);
            Rect rowRect5ShiftRight = UIHelper.GetRowRect(rowRect5, rowHeight, num);
            rowRect5ShiftRight.x += rowRect5.width + 56f;
            Widgets.CheckboxLabeled(rowRect5ShiftRight, "TM_queueAutocast".Translate(), ref Settings.Instance.autocastQueueing, false);
            TooltipHandler.TipRegion(rowRect5ShiftRight, "TM_queueAutocastDesc".Translate());
            num++;
            Rect rowRect6 = UIHelper.GetRowRect(rowRect5, rowHeight, num);
            //Settings.Instance.advMageChance = Widgets.HorizontalSlider(rowRect6, Settings.Instance.advMageChance, 0f, 2f, false, "advMageChance".Translate() + " " + Rarity(Settings.Instance.advMageChance) + " " + TM_Calc.GetMageSpawnChance().ToString("P1"), "0", "2", .01f);
            Rect rowRect6ShiftRight = UIHelper.GetRowRect(rowRect6, rowHeight, num);
            rowRect6ShiftRight.x += rowRect6.width + 56f;
            Settings.Instance.autocastMinThreshold = Widgets.HorizontalSlider(rowRect6ShiftRight, Settings.Instance.autocastMinThreshold, 0f, 1f, false, "TM_autocastUndraftedThreshold".Translate() + " " + (Settings.Instance.autocastMinThreshold * 100) + "%", "0", "1", .01f);
            num++;
            Rect rowRect66 = UIHelper.GetRowRect(rowRect6, rowHeight, num);
            Settings.Instance.undeadUpkeepMultiplier = Widgets.HorizontalSlider(rowRect66, Settings.Instance.undeadUpkeepMultiplier, 0f, 5f, false, "TM_UndeadUpkeepMultiplier".Translate() + " " + Settings.Instance.undeadUpkeepMultiplier.ToString("P1"), "0", "5", .01f);
            Rect rowRect66ShiftRight = UIHelper.GetRowRect(rowRect66, rowHeight, num);
            rowRect66ShiftRight.x += rowRect66.width + 56f;
            Settings.Instance.autocastCombatMinThreshold = Widgets.HorizontalSlider(rowRect66ShiftRight, Settings.Instance.autocastCombatMinThreshold, 0f, 1f, false, "TM_autocastDraftedThreshold".Translate() + " " + (Settings.Instance.autocastCombatMinThreshold * 100) + "%", "0", "1", .01f);
            num++;
            Rect rowRect67 = UIHelper.GetRowRect(rowRect66, rowHeight, num);
            Settings.Instance.paracyteSoftCap = Widgets.HorizontalSlider(rowRect67, Settings.Instance.paracyteSoftCap, 0, 500, false, "TM_ParacyteSoftCap".Translate() + " " + (Settings.Instance.paracyteSoftCap), "0", "500", 1);
            Rect rowRect67ShiftRight = UIHelper.GetRowRect(rowRect67, rowHeight, num);
            rowRect67ShiftRight.x += rowRect67.width + 56f;
            Settings.Instance.autocastEvaluationFrequency = Mathf.RoundToInt(Widgets.HorizontalSlider(rowRect67ShiftRight, Settings.Instance.autocastEvaluationFrequency, 60, 600, false, "TM_autocastEvaluationFrequency".Translate() + " " + (Settings.Instance.autocastEvaluationFrequency / 60) + "seconds", "1", "10", .1f));
            num++;
            Rect rowRect68 = UIHelper.GetRowRect(rowRect67, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect68, "TM_ParacyteMagesCount".Translate(), ref Settings.Instance.paracyteMagesCount, false);
            Rect rowRect68ShiftRight = UIHelper.GetRowRect(rowRect68, rowHeight, num);
            rowRect68ShiftRight.x += rowRect68.width + 56f;
            Widgets.CheckboxLabeled(rowRect68ShiftRight, "TM_autocastAnimals".Translate(), ref Settings.Instance.autocastAnimals, false);
            num++;
            Rect rowRect7 = UIHelper.GetRowRect(rowRect68, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect7, "AICanCast".Translate(), ref Settings.Instance.AICasting, false);
            Rect rowRect7ShiftRight = UIHelper.GetRowRect(rowRect7, rowHeight, num);
            rowRect7ShiftRight.x += rowRect7.width + 56f;
            Widgets.CheckboxLabeled(rowRect7ShiftRight, "AIHardMode".Translate(), ref Settings.Instance.AIHardMode, !settingsRef.AICasting);            
            num++;
            Rect rowRect9 = UIHelper.GetRowRect(rowRect7, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect9, "AIMarking".Translate(), ref Settings.Instance.AIMarking, false);
            Rect rowRect91 = UIHelper.GetRowRect(rowRect9, rowHeight, num);
            rowRect91.x += rowRect9.width + 56f;
            Widgets.CheckboxLabeled(rowRect91, "AIFighterMarking".Translate(), ref Settings.Instance.AIFighterMarking, false);
            num++;
            Rect rowRect92 = UIHelper.GetRowRect(rowRect9, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect92, "AIFriendlyMarking".Translate(), ref Settings.Instance.AIFriendlyMarking, false);
            Rect rowRect92ShiftRight = UIHelper.GetRowRect(rowRect92, rowHeight, num);
            rowRect92ShiftRight.x += rowRect92.width + 56f;
            Widgets.CheckboxLabeled(rowRect92ShiftRight, "showLevelUpMessage".Translate(), ref Settings.Instance.showLevelUpMessage, false);
            num++;
            Rect rowRect93 = UIHelper.GetRowRect(rowRect92, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect93, "showMagicGizmo".Translate(), ref Settings.Instance.showGizmo, false);
            Rect rowRect93ShiftRight = UIHelper.GetRowRect(rowRect93, rowHeight, num);
            rowRect93ShiftRight.x += rowRect93.width + 56f;
            Widgets.CheckboxLabeled(rowRect93ShiftRight, "showUndeadPawnChange".Translate(), ref Settings.Instance.changeUndeadPawnAppearance, false);
            num++;
            Rect rowRect10 = UIHelper.GetRowRect(rowRect93, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect10, "TM_shrinkIcons".Translate(), ref Settings.Instance.shrinkIcons, false);
            Rect rowRect10ShiftRight = UIHelper.GetRowRect(rowRect10, rowHeight, num);
            rowRect10ShiftRight.x += rowRect10.width + 56f;
            Widgets.CheckboxLabeled(rowRect10ShiftRight, "showUndeadAnimalChange".Translate(), ref Settings.Instance.changeUndeadAnimalAppearance, false);
            num++;
            Rect rowRect11 = UIHelper.GetRowRect(rowRect10, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect11, "unrestrictedBloodTypesForBloodMagic".Translate(), ref Settings.Instance.unrestrictedBloodTypes, false);
            Rect rowRect11ShiftRight = UIHelper.GetRowRect(rowRect11, rowHeight, num);
            rowRect11ShiftRight.x += rowRect11.width + 56f;
            Widgets.CheckboxLabeled(rowRect11ShiftRight, "showClassIconOnColonistBar".Translate(), ref Settings.Instance.showClassIconOnColonistBar, false);
            num++;
            Rect rowRect12 = UIHelper.GetRowRect(rowRect11, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect12, "TM_AggressiveAICasting".Translate(), ref Settings.Instance.AIAggressiveCasting, false);
            Rect rowRect12ShiftRight = UIHelper.GetRowRect(rowRect12, rowHeight, num);
            rowRect12ShiftRight.x += rowRect12.width + 56f;
            if (Settings.Instance.showClassIconOnColonistBar)
            {
                Settings.Instance.classIconSize = Widgets.HorizontalSlider(rowRect12ShiftRight, Settings.Instance.classIconSize, .5f, 2.5f, false, "classIconSize".Translate() + " " + Settings.Instance.classIconSize.ToString("P1"), "0", "2.5", .01f);
            }
            num++;
            Rect rowRect13 = UIHelper.GetRowRect(rowRect12, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect13, "TM_UnrestrictedWeaponCopy".Translate(), ref Settings.Instance.unrestrictedWeaponCopy, false);
            Rect rowRect13ShiftRight = UIHelper.GetRowRect(rowRect13, rowHeight, num);
            rowRect13ShiftRight.x += rowRect13.width + 56f;
            Widgets.CheckboxLabeled(rowRect13ShiftRight, "TM_CameraSnap".Translate(), ref Settings.Instance.cameraSnap, false);
            num++;
            Rect rowRect14 = UIHelper.GetRowRect(rowRect13, rowHeight, num);
            //Widgets.CheckboxLabeled(rowRect13, "TM_UnrestrictedWeaponCopy".Translate(), ref Settings.Instance.unrestrictedWeaponCopy, false);
            //Rect rowRect14ShiftRight = UIHelper.GetRowRect(rowRect14, rowHeight, num);
            //rowRect14ShiftRight.x += rowRect13.width + 56f;
            //Settings.Instance.iconPosition.y = Widgets.HorizontalSlider(rowRect14ShiftRight, Settings.Instance.iconPosition.y, -UI.screenHeight/(5f), UI.screenHeight/(5f), false, "y offset " + Settings.Instance.iconPosition.y, "-", "+", 1f);
            //num++;
            num++;
            Rect rowRect20 = UIHelper.GetRowRect(rowRect10, rowHeight, num);
            rowRect20.width = 120f;
            Rect rowRect20ShiftRight1 = UIHelper.GetRowRect(rowRect20, rowHeight, num);
            rowRect20ShiftRight1.x = rowRect20.width + 40f;
            Rect rowRect20ShiftRight2 = UIHelper.GetRowRect(rowRect20ShiftRight1, rowHeight, num);
            rowRect20ShiftRight2.x = rowRect20.width + rowRect20ShiftRight1.width + 40f;
            //GUI.color = Color.yellow;
            //GUI.backgroundColor = Color.yellow;
            reset = Widgets.ButtonText(rowRect20, "Default", true, false, true);
            if (reset)
            {
                Settings.Instance.xpMultiplier = 1f;
                Settings.Instance.needMultiplier = 1f;
                Settings.Instance.deathExplosionRadius = 3f;
                Settings.Instance.deathExplosionMin = 20;
                Settings.Instance.deathExplosionMax = 50;
                Settings.Instance.AICasting = true;
                Settings.Instance.AIHardMode = false;
                Settings.Instance.AIMarking = true;
                Settings.Instance.AIFighterMarking = false;
                Settings.Instance.AIFriendlyMarking = false;
                Settings.Instance.baseMageChance = 1f;
                Settings.Instance.baseFighterChance = 1f;
                Settings.Instance.advMageChance = 0.5f;
                Settings.Instance.advFighterChance = 0.5f;
                Settings.Instance.magicyteChance = .005f;
                Settings.Instance.showIconsMultiSelect = true;
                Settings.Instance.showGizmo = true;
                Settings.Instance.showLevelUpMessage = true;
                Settings.Instance.changeUndeadPawnAppearance = true;
                Settings.Instance.changeUndeadAnimalAppearance = true;
                Settings.Instance.showClassIconOnColonistBar = true;
                Settings.Instance.classIconSize = 1f;
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
                Settings.Instance.shrinkIcons = false;
                Settings.Instance.cameraSnap = true;

                this.deathExplosionDmgMax = "50.0";
                this.deathExplosionDmgMin = "20.0";

                Settings.Instance.autocastEnabled = true;
                Settings.Instance.autocastMinThreshold = .7f;
                Settings.Instance.autocastCombatMinThreshold = .2f;
                Settings.Instance.autocastEvaluationFrequency = 180;
            }

            challenge = Widgets.ButtonText(rowRect20ShiftRight1, "Challenge me!", true, false, true);
            if (challenge)
            {
                Settings.Instance.xpMultiplier = .75f;
                Settings.Instance.needMultiplier = .75f;
                Settings.Instance.deathExplosionRadius = 5f;
                Settings.Instance.deathExplosionMin = 30;
                Settings.Instance.deathExplosionMax = 60;
                Settings.Instance.AICasting = true;
                Settings.Instance.AIHardMode = true;
                Settings.Instance.AIMarking = false;
                Settings.Instance.AIFighterMarking = false;
                Settings.Instance.AIFriendlyMarking = false;
                Settings.Instance.baseMageChance = .8f;
                Settings.Instance.baseFighterChance = .8f;
                Settings.Instance.advMageChance = 1.5f;
                Settings.Instance.advFighterChance = 1.5f;
                Settings.Instance.magicyteChance = .003f;
                Settings.Instance.showIconsMultiSelect = true;
                Settings.Instance.showGizmo = true;
                Settings.Instance.showLevelUpMessage = false;
                Settings.Instance.changeUndeadPawnAppearance = true;
                Settings.Instance.changeUndeadAnimalAppearance = true;
                Settings.Instance.showClassIconOnColonistBar = true;
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
                this.deathExplosionDmgMax = "60.0";
                this.deathExplosionDmgMin = "30.0";

                Settings.Instance.autocastEnabled = true;
                Settings.Instance.autocastMinThreshold = .8f;
                Settings.Instance.autocastCombatMinThreshold = .2f;
                Settings.Instance.autocastEvaluationFrequency = 300;
            }

            //easy = Widgets.ButtonText(rowRect20ShiftRight1, "Easy", true, false, true);
            if (easy)
            {
                Settings.Instance.xpMultiplier = 1.5f;
                Settings.Instance.needMultiplier = 2f;
                Settings.Instance.deathExplosionRadius = 1f;
                Settings.Instance.deathExplosionMin = 5;
                Settings.Instance.deathExplosionMax = 10;
                Settings.Instance.AICasting = false;
                Settings.Instance.AIHardMode = false;
                Settings.Instance.AIMarking = false;
                Settings.Instance.AIFighterMarking = false;
                Settings.Instance.AIFriendlyMarking = false;
                Settings.Instance.baseMageChance = 2f;
                Settings.Instance.baseFighterChance = 2f;
                Settings.Instance.advMageChance = 1f;
                Settings.Instance.advFighterChance = 1f;
                Settings.Instance.magicyteChance = .01f;
                Settings.Instance.showIconsMultiSelect = true;
                Settings.Instance.showGizmo = true;
                Settings.Instance.paracyteSoftCap = 75f;
                Settings.Instance.undeadUpkeepMultiplier = 1f;
                Settings.Instance.showLevelUpMessage = true;
                Settings.Instance.changeUndeadPawnAppearance = true;
                Settings.Instance.changeUndeadAnimalAppearance = true;
                Settings.Instance.showClassIconOnColonistBar = true;
                Settings.Instance.AIAggressiveCasting = false;
                Settings.Instance.riftChallenge = 1f;
                Settings.Instance.autocastAnimals = false;
                Settings.Instance.unrestrictedWeaponCopy = false;
                this.deathExplosionDmgMax = "5.0";
                this.deathExplosionDmgMin = "10.0";

                Settings.Instance.autocastEnabled = true;
                Settings.Instance.autocastMinThreshold = .6f;
                Settings.Instance.autocastCombatMinThreshold = .05f;
                Settings.Instance.autocastEvaluationFrequency = 180;
            }

            //Widgets.EndScrollView();
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
