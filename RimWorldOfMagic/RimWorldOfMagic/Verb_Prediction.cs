using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Prediction : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot() => ApplyHostedEffect();

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var target = currentTarget.Pawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (target != body || target?.Spawned != true || target.Dead || comp == null) return false;
            var data = comp.MagicData;
            var power = data.MagicPowersC.FirstOrDefault(p => p.abilityDef == TorannMagicDefOf.TM_Prediction);
            if (power == null) return false;
            var hediff = target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD);
            if (hediff != null)
            {
                if (hediff.TryGetComp<HediffComp_Prediction>()?.Owner != owner) return false;
                target.health.RemoveHediff(hediff);
                AutoCast.AutocastPolicy.SetAutocast(comp,power,false);
            }
            else
            {
                int rank = ClassProgression.SpiritRebirthRuntime.SavedRank(data,"TM_Prediction_pwr");
                if (ModOptions.Settings.Instance.AIHardMode && !body.IsColonist) rank = 4;
                else if (!body.IsColonist && body.Faction?.HostileTo(Faction.OfPlayerSilentFail) == true) rank = 5;
                var trial = ClassProgression.ChronomancerTrials.CapturePrediction(Ability,target);
                HealthUtility.AdjustSeverity(target,TorannMagicDefOf.TM_PredictionHD,.5f + rank);
                var effect = target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD)?.TryGetComp<HediffComp_Prediction>();
                if (effect == null) return false;
                effect.projectedOwner = owner == body ? null : owner;
                ClassProgression.ChronomancerTrials.CompletePrediction(trial);
                AutoCast.AutocastPolicy.SetAutocast(comp,power,true);
            }
            return true;
        }
    }
}
