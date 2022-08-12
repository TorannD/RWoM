using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class HediffComp_LightCapacitance : HediffComp
    {
        private bool initialized = false;
        private float lightEnergy = 10f;
        private string lightPowerString = "";

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.lightEnergy, "lightEnergy", 10f, false);
        }

        public override string CompLabelInBracketsExtra => this.lightPowerString;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        public float LightEnergyMax
        {
            get
            {
                float val = 100f;
                return val;
            }
        }

        public float LightEnergy
        {
            get
            {                
                return this.lightEnergy;
            }
            set
            {
                this.lightEnergy = Mathf.Clamp(value, 0f, LightEnergyMax);
            }
        }

        public float LightPotency
        {
            get
            {
                return Mathf.Clamp((2f * (this.LightEnergy / 100f)), .5f, 2.5f);
            }
        }

        private FlyingObject_SpiritOfLight SoL
        {
            get
            {
                if (base.Pawn.Spawned && base.Pawn.Map != null)
                {
                    CompAbilityUserMagic comp = base.Pawn.GetCompAbilityUserMagic();
                    if (comp != null && comp.SoL != null)
                    {
                        return comp.SoL;
                    }
                }
                return null;
            }
        }

        public float Get_SoLEnergy
        {
            get
            {
                if(SoL != null)
                {
                    return SoL.LightEnergy;
                }
                return 0f;
            }
        }


        public float ChargeAmount
        {
            get
            {
                float val = .01f;
                if (this.Pawn.Map != null)
                {
                    if (this.Pawn.Map.weatherManager?.curWeather?.defName != "Clear")
                    {
                        val *= .8f;
                    }
                    if (this.Pawn.Map.GameConditionManager?.ActiveConditions?.Count > 0)
                    {
                        List<GameCondition> gcList = this.Pawn.Map.GameConditionManager.ActiveConditions;
                        for (int i = 0; i < gcList.Count; i++)
                        {
                            if (gcList[i].def == GameConditionDefOf.Aurora)
                            {
                                val += .004f;
                            }
                            if (gcList[i].def == GameConditionDefOf.VolcanicWinter)
                            {
                                val *= .8f;
                            }
                            if (gcList[i].def == GameConditionDefOf.ToxicFallout)
                            {
                                val *= .85f;
                            }
                            if (gcList[i].def == GameConditionDefOf.SolarFlare)
                            {
                                val *= 1.5f;
                            }
                            if (gcList[i].def == GameConditionDefOf.Eclipse)
                            {
                                val *= .7f;
                            }
                            if (gcList[i].def == TorannMagicDefOf.DarkClouds)
                            {
                                val *= .8f;
                            }
                        }
                    }
                    if (this.Pawn.Position.Roofed(this.Pawn.Map))
                    {
                        val -= .004f;
                    }
                    int mapTime = GenLocalDate.HourOfDay(this.Pawn.Map);
                    if (mapTime < 20 && mapTime > 5)
                    {
                        float amt = 0;
                        if (mapTime >= 13)
                        {
                            amt = ((float)Mathf.Abs(24f - mapTime) * val);
                        }
                        else if (mapTime <= 11)
                        {
                            amt = ((float)Mathf.Abs(mapTime) * val);
                        }
                        else
                        {
                            amt = (val * 12f);
                        }
                        return (amt/1.5f);
                    }
                    return ((val * 2f) - .05f);
                }
                return (val * 2f);
            }
        }

        public int Get_ChargeFrequency
        {
            get
            {
                int val = 60;
                return val;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned && base.Pawn.Map != null)
            {

            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if(!initialized)
                {
                    this.initialized = true;
                    Initialize();
                }

                if(Find.TickManager.TicksGame % Get_ChargeFrequency == 0)
                {
                    UpdateCharge();
                }

                if(Find.TickManager.TicksGame % 345 == 0)
                {
                    Initialize();
                }
            }            
        }

        private void UpdateCharge()
        {
            LightEnergy += ChargeAmount;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if(Get_SoLEnergy != 0f)
            {
                this.lightPowerString = LightEnergy.ToString("0.#") + " | " + Get_SoLEnergy.ToString("0.#");
            }
            else
            {
                this.lightPowerString = LightEnergy.ToString("0.#");
            }
        }

    }
}
