using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Reflection;
using HarmonyLib;

namespace TorannMagic
{
    public class Need_Spirit : Need  //Original code by Jecrell
    {
        public const float BaseGainPerTickRate = 150f;

        public const float BaseFallPerTick = 1E-05f;

        public const float ThreshVeryLow = 10f;

        public const float ThreshLow = 30f;

        public const float ThreshSatisfied = 50f;

        public const float ThreshHigh = 75f;

        public const float ThreshVeryHigh = 90f;

        public int ticksUntilBaseSet = 500;

        public float lastGainPct = 0f;

        private int lastGainTick;

        public float baseSpiritGain;
        public float modifiedSpiritGain;
        public float drainSkillUpkeep;

        public float InitSpiritLevel = 65f;

        public bool wasDead = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.wasDead, "wasDead", false);
        }

        public StaminaPoolCategory CurCategory
        {
            get
            {
                bool flag = this.CurLevel < 10f;
                StaminaPoolCategory result;
                if (flag)
                {
                    result = StaminaPoolCategory.Fatigued;
                }
                else
                {
                    bool flag2 = this.CurLevel < 30f;
                    if (flag2)
                    {
                        result = StaminaPoolCategory.Weakened;
                    }
                    else
                    {
                        bool flag3 = this.CurLevel < 50f;
                        if (flag3)
                        {
                            result = StaminaPoolCategory.Steady;
                        }
                        else
                        {
                            bool flag4 = this.CurLevel < 75f;
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
            set => base.CurLevel = Mathf.Clamp(value, 0f, MaxLevel);
        }

        public override float MaxLevel
        {
            get
            {
                float maxBase = 100f;
                if(this.pawn.story != null && this.pawn.story.adulthood.identifier == "tm_ancient_spirit")
                {
                    maxBase += 50f;
                }
                if (this.pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessorHD))
                {
                    Hediff_Possessor hdp = this.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessorHD) as Hediff_Possessor;
                    return maxBase + (hdp.SpiritLevel * 2f) + hdp.MaxLevelBonus;
                }
                if(this.pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD))
                {
                    HediffComp_SpiritPossession hdc = this.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD).TryGetComp<HediffComp_SpiritPossession>();
                    return maxBase + (hdc.SpiritLevel * 2f) + hdc.MaxLevelBonus;
                }
                return maxBase;
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

        static Need_Spirit()
        {
        }

        public Need_Spirit(Pawn pawn) : base(pawn)
		    {
            this.lastGainTick = -999;
            this.threshPercents = new List<float>();
            this.threshPercents.Add((25f / this.MaxLevel));
            this.threshPercents.Add((50f / this.MaxLevel));
            this.threshPercents.Add((75f / this.MaxLevel));
        }

        private void AdjustThresh()
        {
            this.threshPercents.Clear();
            this.threshPercents.Add((25f / this.MaxLevel));
            this.threshPercents.Add((50f / this.MaxLevel));
            this.threshPercents.Add((75f / this.MaxLevel));
            if (this.MaxLevel > 100)
            {
                this.threshPercents.Add((100f / this.MaxLevel));
            }
            if (this.MaxLevel > 125f)
            {
                this.threshPercents.Add((125f / this.MaxLevel));
            }
            if (this.MaxLevel > 150f)
            {
                this.threshPercents.Add((150f / this.MaxLevel));
            }
            if (this.MaxLevel > 175f)
            {
                this.threshPercents.Add((175f / this.MaxLevel));
            }
            if (this.MaxLevel > 200f)
            {
                this.threshPercents.Add((200f / this.MaxLevel));
            }
            if (this.MaxLevel > 250f)
            {
                this.threshPercents.Add((250f / this.MaxLevel));
            }
            if (this.MaxLevel > 300f)
            {
                this.threshPercents.Add((300f / this.MaxLevel));
            }
            if (this.MaxLevel > 400f)
            {
                this.threshPercents.Add((400f / this.MaxLevel));
            }
            if (this.MaxLevel > 500f)
            {
                this.threshPercents.Add((500f / this.MaxLevel));
            }
        }

        public override void SetInitialLevel()
        {
            this.CurLevel = InitSpiritLevel;           
        }

        public void GainNeed(float amount)
        {            
            Pawn pawn = base.pawn;                
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            CompAbilityUserMagic comp = this.pawn.GetCompAbilityUserMagic();
            float eff = 1f;
            if (comp != null && comp.MagicData != null && comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SpiritDrain) != null)
            {
                eff = (1f + (.1f * comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SpiritDrain).level));
            }
            this.baseSpiritGain = amount;                         
            this.modifiedSpiritGain = (amount * eff) - this.baseSpiritGain;
            amount = Mathf.Min(amount * eff, this.MaxLevel - this.CurLevel);
            this.CurLevel += amount;
            this.lastGainPct = amount/this.MaxLevel;
            this.lastGainTick = Find.TickManager.TicksGame;             
            
            if(amount > 0)
            {
                List<Hediff> afflictionList = TM_Calc.GetPawnAfflictions(this.pawn);
                List<Hediff> addictionList = TM_Calc.GetPawnAddictions(this.pawn);
                if (TM_Calc.IsPawnInjured(this.pawn, 0))
                {
                    TM_Action.DoAction_HealPawn(this.pawn, this.pawn, 1, Rand.Range(1f, 3f) * amount);                    
                }
                else if (afflictionList != null && afflictionList.Count > 0)
                {
                    Hediff hediff = afflictionList.RandomElement();
                    hediff.Severity -= .05f * amount;
                    if (hediff.Severity <= 0)
                    {
                        this.pawn.health.RemoveHediff(hediff);
                    }
                    HediffComp_Disappears hediffTicks = hediff.TryGetComp<HediffComp_Disappears>();
                    if (hediffTicks != null)
                    {
                        int ticksToDisappear = Traverse.Create(root: hediffTicks).Field(name: "ticksToDisappear").GetValue<int>();
                        ticksToDisappear -= Mathf.RoundToInt(50000 * amount);
                        Traverse.Create(root: hediffTicks).Field(name: "ticksToDisappear").SetValue(ticksToDisappear);
                    }
                }
                else if (addictionList != null && addictionList.Count > 0)
                {
                    Hediff hediff = addictionList.RandomElement();
                    hediff.Severity -= .15f * amount;
                    if (hediff.Severity <= 0)
                    {
                        this.pawn.health.RemoveHediff(hediff);
                    }
                }
            }
            AdjustThresh();
            if(this.CurLevel < 25f && !TM_Calc.IsSpirit(this.pawn))
            {
                HealthUtility.AdjustSeverity(this.pawn, TorannMagicDefOf.TM_SpiritDrainHD, .01f);
                Hediff sd = this.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritDrainHD);
                if(!(sd != null && sd.Severity >= .95f))
                {
                    this.CurLevel += .03f;
                }
            }
            if(this.CurLevel <= 0)
            {
                if(TM_Calc.IsSpirit(this.pawn))
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, this.pawn.DrawPos, this.pawn.Map, 1.2f, .25f, 0f, .25f, 0, Rand.Range(2f, 3f), 0, 0);
                    this.pawn.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    TM_Action.RemovePossession(this.pawn, this.pawn.Position, true);
                }
            }
        }

        public void UseMightPower(float amount)
        {
            this.curLevelInt = Mathf.Clamp(this.curLevelInt - amount, 0f, this.pawn.GetCompAbilityUserMight().maxSP); //change for max sp
        }

        public override void NeedInterval()
        {
            float lossModifier = 1f;

            //Hediff hd = this.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
            //if (hd != null)
            //{
            //    HediffComp_SpiritPossession hdc = hd.TryGetComp<HediffComp_SpiritPossession>() as HediffComp_SpiritPossession;
            //    if (hdc != null && hdc.SpiritPawn != null && hdc.SpiritPawn_Hediff.wasDead)
            //    {
            //        wasDead = true;
            //    }
            //}
            if (this.pawn.RaceProps.Animal || TM_Calc.IsSpirit(this.pawn) || this.wasDead)
            {
                lossModifier = 2f;
            }
            this.GainNeed(Rand.Range(-0.015f, -0.04f) * lossModifier);
        }

        public override string GetTipString()
        {
            //return base.GetTipString();
            return string.Concat(new string[]
            {
                this.LabelCap,
                ": ",
                (this.CurLevel).ToString("n2"),
                "\n",
                this.def.description
            });
        }    

        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = 2147483647, float customMargin = -1f, bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = default(Rect?))
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
            GUI.color = Color.grey;
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
