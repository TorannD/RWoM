using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using TorannMagic.Utils;

namespace TorannMagic
{
    class Verb_Heal : Verb_UseAbility
    {
        private int verVal;
        private int pwrVal;
        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            // power affects enumerator
            //DamageWorker.DamageResult result = DamageWorker.DamageResult.MakeNew();
            Pawn caster = this.CasterPawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Heal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Heal_pwr").level;
            //verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Heal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Heal_ver").level;
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            if(caster.story.traits.HasTrait(TorannMagicDefOf.Priest))
            {
                pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AdvancedHeal_pwr").level;
                verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AdvancedHeal_ver").level;
            }
            //else if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}
            //else if (caster.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || (comp.customClass != null && comp.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Cantrips)))
            //{
            //    int tmpPwrVal = (int)((caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level) / 5);
            //    int tmpVerVal = (int)((caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level) / 5);
            //    pwrVal = (tmpPwrVal > pwrVal) ? tmpPwrVal : pwrVal;
            //    verVal = (tmpVerVal > verVal) ? tmpVerVal : verVal;
            //}

            Pawn pawn = (Pawn)currentTarget;
            if (pawn == null || pawn.Dead) return true;

            if (!TM_Calc.IsUndead(pawn))
            {
                
                int injuriesPerBodyPart = !CasterPawn.IsColonist && ModOptions.Settings.Instance.AIHardMode ? 5 : 1 + verVal;

                IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(injury => injury.CanHealNaturally())
                    .DistinctBy(injury => injury.Part, injuriesPerBodyPart)
                    .Take(3 + verVal);

                float healAmount = CasterPawn.IsColonist ? (8.0f + pwrVal * 2f) * comp.arcaneDmg : 20.0f + pwrVal * 3f;
                foreach (Hediff_Injury injury in injuries)
                {
                    injury.Heal(healAmount);
                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
                }
            }
            else
            {
                for(int i = 0; i < 2+verVal; i++)
                {
                    TM_Action.DamageUndead(pawn, (5f + (3f * pwrVal)) * comp.arcaneDmg, this.CasterPawn);
                }
            }
            return true;
        }
    }
}
