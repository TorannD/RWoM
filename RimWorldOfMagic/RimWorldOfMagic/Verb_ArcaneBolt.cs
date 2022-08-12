using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
    class Verb_ArcaneBolt : Verb_UseAbility  
    {

        bool validTarg;
        private int verVal;
        private int pwrVal;

        //public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        //{
        //    if (targ.Thing != null && targ.Thing == this.caster)
        //    {
        //        return this.verbProps.targetParams.canTargetSelf;
        //    }
        //    if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
        //    {
        //        if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
        //        {
        //            ShootLine shootLine;
        //            validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
        //        }
        //        else
        //        {
        //            validTarg = false;
        //        }
        //    }
        //    else
        //    {
        //        validTarg = false;
        //    }
        //    return validTarg;
        //}

        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn pawn = this.CasterPawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            int burstCountMin = 1;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            
            if (pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 2)
            {
                burstCountMin++;
                if (pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 7)
                {
                    burstCountMin++;
                }
            }
            if (!pawn.IsColonist && settingsRef.AIHardMode)
            {
                burstCountMin = 3;
            }

            Map map = this.CasterPawn.Map;
            IntVec3 targetVariation = this.currentTarget.Cell;
            targetVariation.x += Mathf.RoundToInt(Rand.Range(-.05f, .05f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3));// + Rand.Range(-1f, 1f));
            targetVariation.z += Mathf.RoundToInt(Rand.Range(-.05f, .05f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3));// + Rand.Range(-1f, 1f));
            this.TryLaunchProjectile(this.verbProps.defaultProjectile, targetVariation);
            this.burstShotsLeft--;
            //Log.Message("burst shots left " + this.burstShotsLeft);
            float burstCountFloat = (float)(15f - this.burstShotsLeft);
            float mageLevelFloat = (float)(burstCountMin + (comp.MagicUserLevel/10f));
            result = Rand.Chance(mageLevelFloat - burstCountFloat);            
            return result;
        }
    }
}
