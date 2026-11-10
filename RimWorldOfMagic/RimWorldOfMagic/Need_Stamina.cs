using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;


namespace TorannMagic
{
    public class Need_Stamina : Need  //Original code by Jecrell
    {
        public const float BaseGainPerTickRate = 150f;

        public const float BaseFallPerTick = 1E-05f;

        public const float ThreshVeryLow = 0.1f;

        public const float ThreshLow = 0.3f;

        public const float ThreshSatisfied = 0.5f;

        public const float ThreshHigh = 0.7f;

        public const float ThreshVeryHigh = 0.9f;

        public int ticksUntilBaseSet = 500;

        public float lastGainPct = 0f;

        private int lastGainTick;
        private int lastSpiritIntervalTick = -1;

        public float baseStaminaGain;
        public float modifiedStaminaGain;
        public float drainSkillUpkeep;

        public StaminaPoolCategory CurCategory
        {
            get
            {
                bool flag = this.CurLevel < 0.1f;
                StaminaPoolCategory result;
                if (flag)
                {
                    result = StaminaPoolCategory.Fatigued;
                }
                else
                {
                    bool flag2 = this.CurLevel < 0.3f;
                    if (flag2)
                    {
                        result = StaminaPoolCategory.Weakened;
                    }
                    else
                    {
                        bool flag3 = this.CurLevel < 0.5f;
                        if (flag3)
                        {
                            result = StaminaPoolCategory.Steady;
                        }
                        else
                        {
                            bool flag4 = this.CurLevel < 0.7f;
                            if (flag4)
                            {
                                result = StaminaPoolCategory.Energetic;
                            }
                            else
                            {
                                result = StaminaPoolCategory.Surging;
                            }
                        }
                    }
                }
                return result;
            }
        }

        public override float CurLevel
        {
            get => base.CurLevel;
            set
            {
                // the might comp is added to pawns at runtime — see Need_Mana
                CompAbilityUserMight comp = this.pawn?.GetCompAbilityUserMight();
                float max = (comp != null ? comp.maxSP : 1f) - ClassProgression.SpiritHostedStance.Cost(pawn, false);
                base.CurLevel = Mathf.Clamp(value, 0f, max);
            }
        }

        public override float MaxLevel
        {
            get
            {
                CompAbilityUserMight comp = this.pawn?.GetCompAbilityUserMight();
                float maxSP = (comp != null ? comp.maxSP : 1f) - ClassProgression.SpiritHostedStance.Cost(pawn, false);
                if (this.pawn.ageTracker.AgeBiologicalYears < 13)
                {
                    maxSP -= (12f - this.pawn.ageTracker.AgeBiologicalYearsFloat) / 10f;
                }
                return maxSP;
            }
        }

        public override int GUIChangeArrow
        {
            get
            {
                return this.GainingNeed ? 1 : -1;
            }
        }

        public override float CurInstantLevel
        {
            get
            {
                return this.CurLevel;
            }
        }

        private bool GainingNeed
        {
            get
            {
                return Find.TickManager.TicksGame < this.lastGainTick + 10;
            }
        }

        static Need_Stamina()
        {
        }

        public Need_Stamina(Pawn pawn) : base(pawn)
		    {
            this.lastGainTick = -999;
            this.threshPercents = new List<float>();
            this.threshPercents.Add((0.25f / this.MaxLevel));
            this.threshPercents.Add((0.5f / this.MaxLevel));
            this.threshPercents.Add((0.75f / this.MaxLevel));
        }

        private void AdjustThresh()
        {
            this.threshPercents.Clear();
            this.threshPercents.Add((0.25f / this.MaxLevel));
            this.threshPercents.Add((0.5f / this.MaxLevel));
            this.threshPercents.Add((0.75f / this.MaxLevel));
            if (this.MaxLevel > 1)
            {
                this.threshPercents.Add((1f / this.MaxLevel));
            }
            if (this.MaxLevel > 1.25f)
            {
                this.threshPercents.Add((1.25f / this.MaxLevel));
            }
            if (this.MaxLevel > 1.5f)
            {
                this.threshPercents.Add((1.5f / this.MaxLevel));
            }
            if (this.MaxLevel > 1.75f)
            {
                this.threshPercents.Add((1.75f / this.MaxLevel));
            }
            if (this.MaxLevel > 2f)
            {
                this.threshPercents.Add((2f / this.MaxLevel));
            }
            if (this.MaxLevel > 2.5f)
            {
                this.threshPercents.Add((2.5f / this.MaxLevel));
            }
            if (this.MaxLevel > 3f)
            {
                this.threshPercents.Add((3f / this.MaxLevel));
            }
            if (this.MaxLevel > 4f)
            {
                this.threshPercents.Add((4f / this.MaxLevel));
            }
            if (this.MaxLevel > 5f)
            {
                this.threshPercents.Add((5f / this.MaxLevel));
            }
        }

        public override void SetInitialLevel()
        {
            this.CurLevel = ClassProgression.SpiritRebirthEnergy.Marked(pawn) ? 0f : 0.8f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastSpiritIntervalTick, "TM_spiritStaminaInterval", -1);
        }

        public void GainNeed(float amount)
        {
            if (!base.pawn.DestroyedOrNull() && !base.pawn.Dead && base.pawn.story != null && base.pawn.story.traits != null)
            {
                if (!base.pawn.NonHumanlikeOrWildMan())
                {
                    Pawn pawn = base.pawn;
                    CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                    
                    amount = amount * (0.015f);
                    this.baseStaminaGain = amount * ModOptions.Settings.Instance.needMultiplier;
                    amount *= comp.spRegenRate - ClassProgression.SpiritHostedStance.Cost(pawn, true);
                    if (pawn.health != null && pawn.health.hediffSet != null)
                    {
                        Hediff hdRegen = ClassProgression.SpiritHostedStance.Regen(pawn, pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD));
                        if (hdRegen != null)
                        {
                            amount *= hdRegen.Severity;
                        }
                    }
                    this.modifiedStaminaGain = amount - this.baseStaminaGain;
                    // Upkeep can reduce capacity between need ticks (e.g. a new animal bond).
                    // Clamp the excess once, before applying the ordinary regeneration rate.
                    float maxLevel = Mathf.Max(0f, MaxLevel);
                    curLevelInt = Mathf.Min(curLevelInt, maxLevel);
                    amount = Mathf.Min(amount, maxLevel - curLevelInt);
                    this.lastGainPct = amount;
                    this.lastGainTick = Find.TickManager.TicksGame;
                    curLevelInt = Mathf.Clamp(curLevelInt + 2f * amount, 0f, maxLevel);

                }
                AdjustThresh();
            }
        }

        public void UseMightPower(float amount)
        {
            float max = this.pawn.GetCompAbilityUserMight().maxSP - ClassProgression.SpiritHostedStance.Cost(pawn, false);
            this.curLevelInt = Mathf.Clamp(this.curLevelInt - amount, 0f, Mathf.Max(0f, max));
        }

        public override void NeedInterval()
        {
            if (ClassProgression.SpiritRebirthEnergy.Marked(pawn))
            {
                if (!ClassProgression.SpiritRebirthBody.RetainsNeeds(pawn)) return;
                int now = Find.TickManager.TicksGame;
                if (lastSpiritIntervalTick >= 0 && now - lastSpiritIntervalTick < 150) return;
                lastSpiritIntervalTick = now;
            }
            this.GainNeed(1f);
        }

        public override string GetTipString()
        {
            //return base.GetTipString();
            return string.Concat(new string[]
            {
                this.LabelCap,
                ": ",
                (this.CurLevel / .01f).ToString("n2"),
                "\n",
                this.def.description
            });
        }

        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = int.MaxValue, float customMargin = -1, bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = null, bool drawLabel = true)
        {
            bool flag = rect.height > 70f;
            if (flag)
            {
                float num = (rect.height - 70f) / 2f;
                rect.height = 70f;
                rect.y += num;
            }
            Rect rect2 = rectForTooltip ?? rect;
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            if (doTooltip && Mouse.IsOver(rect2))
            {
                TooltipHandler.TipRegion(rect2, new TipSignal(() => GetTipString(), rect2.GetHashCode()));
            }
            float num2 = 14f;
            float num3 = num2 + 15f;
            bool flag3 = rect.height < 50f;
            if (flag3)
            {
                num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
            }
            Text.Font = ((rect.height <= 55f) ? GameFont.Tiny : GameFont.Small);
            Text.Anchor = TextAnchor.LowerLeft;
            Rect _rect2 = new Rect(rect.x + num3 + rect.width * 0.1f, rect.y, rect.width - num3 - rect.width * 0.1f, rect.height / 2f);
            Widgets.Label(_rect2, base.LabelCap);
            GUI.color = Color.yellow;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect3 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height / 2f);
            rect3 = new Rect(rect3.x + num3, rect3.y, rect3.width - num3 * 2f, rect3.height - num2);
            Widgets.FillableBar(rect3, base.CurLevelPercentage);
            bool flag4 = this.threshPercents != null;
            if (flag4)
            {
                for (int i = 0; i < this.threshPercents.Count; i++)
                {
                    this.DrawBarThreshold(rect3, this.threshPercents[i]);
                }
            }
            float curInstantLevelPercentage = Mathf.Clamp(this.CurLevel / this.MaxLevel, 0f, 1f);
            bool flag5 = curInstantLevelPercentage >= 0f;
            if (flag5)
            {
                base.DrawBarInstantMarkerAt(rect3, curInstantLevelPercentage);
            }
            bool flag6 = !this.def.tutorHighlightTag.NullOrEmpty();
            if (flag6)
            {
                UIHighlighter.HighlightOpportunity(rect, this.def.tutorHighlightTag);
            }
            Text.Font = GameFont.Small;
        }

        private void DrawBarThreshold(Rect barRect, float threshPct)
        {
            float num = (float)((barRect.width <= 60f) ? 1 : 2);
            Rect position = new Rect(barRect.x + barRect.width * threshPct - (num - 1f), barRect.y + barRect.height / 2f, num, barRect.height / 2f);
            bool flag = threshPct < base.CurLevelPercentage;
            Texture2D image;
            if (flag)
            {
                image = BaseContent.BlackTex;
                GUI.color = new Color(1f, 1f, 1f, 0.9f);
            }
            else
            {
                image = BaseContent.GreyTex;
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
            }
            GUI.DrawTexture(position, image);
            GUI.color = Color.white;
        }
    }
}
