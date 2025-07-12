using RimWorld;
using System;
using Verse;
using Verse.Sound;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    class Verb_NanoStimulant : Verb_UseAbility  
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;

            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //MagicPowerSkill eff = comp.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoWeapon_eff");
            int effVal = TM_Calc.GetSkillEfficiencyLevel(caster, TorannMagicDefOf.TM_TechnoWeapon, false);

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_NanoStimulantHD"), (effVal + .01f)*comp.arcaneDmg);
                SoundInfo info = SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map, false), MaintenanceType.None);
                info.pitchFactor = 1.0f;
                info.volumeFactor = 1.0f;
                TorannMagicDefOf.TM_FastReleaseSD.PlayOneShot(info);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, caster.DrawPos, caster.Map, .8f, .2f, .1f, .1f, 0, 1f, 0, Rand.Chance(.5f) ? 0 : 180);
            }
            return true;
        }
    }
}
