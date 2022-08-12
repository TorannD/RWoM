using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    class Verb_Reversal : Verb_UseAbility  
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            MightPowerSkill pwr = comp.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Reversal_pwr");

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_ReversalHD"), (8 + (2 * pwr.level))*comp.mightPwr);
            }
            return true;
        }
    }
}
