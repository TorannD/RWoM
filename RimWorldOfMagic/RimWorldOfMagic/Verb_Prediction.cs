using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Prediction : Verb_UseAbility
    {
        private int pwrVal = 0;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            MagicPower magicPower = caster.GetCompAbilityUserMagic().MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Prediction);
            MagicPowerSkill pwr = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Prediction_pwr");
            pwrVal = pwr.level;
            CompAbilityUserMagic comp = base.CasterPawn.GetCompAbilityUserMagic();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.AIHardMode && !this.CasterPawn.IsColonist)
            {
                pwrVal = 4;
            }
            else if(!this.CasterPawn.IsColonist && this.CasterPawn.Faction != null && this.CasterPawn.Faction.HostileTo(Faction.OfPlayerSilentFail))
            {
                pwrVal = 5;
            }
            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD))
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD);
                    pawn.health.RemoveHediff(hediff);                    
                    magicPower.AutoCast = false;
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_PredictionHD, .5f + pwrVal);
                    //TM_MoteMaker.ThrowNoteMote(pawn.DrawPos, pawn.Map, .8f);
                    magicPower.AutoCast = true;
                }
            }
            return true;
        }
    }
}
