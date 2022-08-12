using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class HediffComp_Prediction : HediffComp
    {
        private bool initialized = false;

        private int pwrVal = 0;
        public bool removeNow = false;

        public int blurTick = 0;
        private int predictionFrequency = 120;

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

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned && base.Pawn.Map != null && this.Pawn.story != null)
            {
                //FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
                Pawn caster = this.Pawn;
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                if (comp != null)
                {
                    pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Prediction_pwr").level;
                    this.parent.Severity = .5f + pwrVal;
                }
            }
            else
            {
                this.removeNow = true;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
                else if(initialized && !base.Pawn.Dead && !base.Pawn.Downed && base.Pawn.Spawned)
                {
                    //if(Find.TickManager.TicksGame % this.predictionFrequency == 0)
                    //{
                    //    IncidentQueue iq = Find.Storyteller.incidentQueue;

                    //    Log.Message("incidents count is  " + iq.Count + " with incident queue containing: " + iq.DebugQueueReadout);
                    //}

                    if (Find.TickManager.TicksGame % 60 == 0)
                    {
                        UpdateSeverity();
                    }
                }                
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }

        public void UpdateSeverity()
        {
            float sev = this.parent.Severity;
            Pawn caster = this.Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            if (comp != null)
            {
                pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Prediction_pwr").level;
                if (sev <= 0)
                {
                    this.removeNow = true;
                }
                else if(!this.Pawn.IsColonist && settingsRef.AIHardMode)
                {
                    this.parent.Severity = 5;
                }
                else if(sev != pwrVal + .5f)
                {
                    this.parent.Severity = pwrVal + .5f;
                }
            }
            else
            {
                this.removeNow = true;
            }
        }
    }
}
