using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;

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

            Pawn pawn = (Pawn)this.currentTarget;
            bool flag = pawn != null && !pawn.Dead && !TM_Calc.IsUndead(pawn);
            bool flagUndead = pawn != null && !pawn.Dead && TM_Calc.IsUndead(pawn);
            if (flag)
            {
                int num = 3 + verVal;
                using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;
                        bool flag2 = num > 0;
                        
                        if (flag2)
                        {
                            int num2 = 1 + verVal;
                            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                            if (!this.CasterPawn.IsColonist && settingsRef.AIHardMode)
                            {
                                num2 = 5;
                            }
                            IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                            Func<Hediff_Injury, bool> arg_BB_1;

                            arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                            foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                            {
                                bool flag4 = num2 > 0;
                                if (flag4)
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                    if (flag5)
                                    {
                                        //current.Heal((float)((int)current.Severity + 1));
                                        if (!this.CasterPawn.IsColonist)
                                        {
                                            current.Heal(20.0f + (float)pwrVal * 3f); // power affects how much to heal                                            
                                        }
                                        else
                                        {
                                            current.Heal((8.0f + (float)pwrVal * 2f)*comp.arcaneDmg); // power affects how much to heal
                                        }
                                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
                                        num--;
                                        num2--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if(flagUndead)
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
