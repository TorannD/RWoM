using RimWorld;
using System;
using Verse;
using Verse.Sound;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    class Verb_NanoStimulant : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            Pawn caster = CasterPawn;
            var source = ClassProgression.SpiritRebirthCasting.SkillOwner(caster,Ability);
            var comp = source?.GetCompAbilityUserMagic();
            if (caster?.Spawned != true || caster.Dead || comp == null) return false;
            int effVal = TM_Calc.GetSkillEfficiencyLevel(source, TorannMagicDefOf.TM_TechnoWeapon, false);
            HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_NanoStimulantHD"), (effVal + .01f)*comp.arcaneDmg);
            SoundInfo info = SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map, false), MaintenanceType.None);
            info.pitchFactor = 1.0f;
            info.volumeFactor = 1.0f;
            TorannMagicDefOf.TM_FastReleaseSD.PlayOneShot(info);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, caster.DrawPos, caster.Map, .8f, .2f, .1f, .1f, 0, 1f, 0, Rand.Chance(.5f) ? 0 : 180);
            return true;
        }
    }
}
