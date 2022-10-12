using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using AbilityUser;
using Verse;
using RimWorld;
using TorannMagic.Utils;

namespace TorannMagic
{
    class Verb_AdvancedHeal : Verb_UseAbility
    {

        bool validTarg;
        private int verVal;
        private int pwrVal;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
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
            // DamageWorker.DamageResult result = DamageWorker.DamageResult.MakeNew();
            Pawn caster = this.CasterPawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            //MagicPowerSkill pwr = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AdvancedHeal_pwr");
            //MagicPowerSkill ver = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AdvancedHeal_ver");
            //pwrVal = pwr.level;
            //verVal = ver.level;
            //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}

            Pawn pawn = (Pawn)currentTarget;
            if (pawn == null) return true;
            if (pawn.Dead) return true;

            if (!TM_Calc.IsUndead(pawn))
            {
                int injuriesToHeal = 3 + verVal;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                int injuriesPerBodyPart = !CasterPawn.IsColonist && settingsRef.AIHardMode ? 5 : 1 + verVal;

                IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(injury => injury.CanHealNaturally())
                    .DistinctBy(injury => injury.Part, injuriesPerBodyPart);

                int timesHealed = 0;
                float baseHealAmount = CasterPawn.IsColonist ? 30.0f : 14.0f;
                float healAmount = baseHealAmount + pwrVal * 3f;
                // First go through any naturally healing injuries
                foreach (Hediff_Injury injury in injuries)
                {
                    injury.Heal(healAmount);
                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, 1f+.2f*pwrVal);
                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .8f+.1f*pwrVal);
                    timesHealed++;

                    if (timesHealed >= injuriesToHeal) return true;
                }

                // Heal missing parts next
                IEnumerable<Hediff_MissingPart> missingParts = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_MissingPart>()
                    .Where(missingPart => missingPart.IsFresh);
                foreach (Hediff_MissingPart missingPart in missingParts)
                {
                    missingPart.IsFresh = false;
                    timesHealed++;

                    if (timesHealed >= injuriesToHeal) return true;
                }
            }
            else
            {
                for (int i = 0; i < 2 + verVal; i++)
                {
                    TM_Action.DamageUndead(pawn, (8.0f + pwrVal * 5f) * comp.arcaneDmg, CasterPawn);
                }
            }
            return true;
        }
        
    }
}
