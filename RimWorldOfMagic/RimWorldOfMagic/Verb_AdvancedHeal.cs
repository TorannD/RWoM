using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using AbilityUser;
using Verse;
using RimWorld;

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
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            CompAbilityUserMagic comp = caster.GetComp<CompAbilityUserMagic>();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            //MagicPowerSkill pwr = caster.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AdvancedHeal_pwr");
            //MagicPowerSkill ver = caster.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AdvancedHeal_ver");
            //pwrVal = pwr.level;
            //verVal = ver.level;
            //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}

            Pawn pawn = (Pawn)this.currentTarget;
            bool flag = pawn != null && !pawn.Dead && !TM_Calc.IsUndead(pawn);
            bool undeadFlag = pawn != null && !pawn.Dead && TM_Calc.IsUndead(pawn);
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
                                            current.Heal(30.0f + (float)pwrVal * 3f); // power affects how much to heal
                                        }
                                        else
                                        {
                                            current.Heal((14.0f + (float)pwrVal * 3f)*comp.arcaneDmg); // power affects how much to heal
                                        }
                                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, 1f+.2f*pwrVal);
                                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .8f+.1f*pwrVal);
                                        num--;
                                        num2--;
                                    }
                                }
                            }
                            using (IEnumerator<Hediff> enumerator1 = pawn.health.hediffSet.GetHediffsTendable().GetEnumerator())
                            {
                                while (enumerator1.MoveNext())
                                {
                                    if (num > 0)
                                    {
                                        Hediff rec1 = enumerator1.Current;
                                        if (rec1.TendableNow() && rec1.Bleeding && rec1 is Hediff_MissingPart)
                                        {
                                            Traverse.Create(root: rec1).Field(name: "isFreshInt").SetValue(false);
                                            num--;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if(undeadFlag)
            {
                for (int i = 0; i < 2 + verVal; i++)
                {
                    TM_Action.DamageUndead(pawn, (8.0f + (float)pwrVal * 5f) * comp.arcaneDmg, this.CasterPawn);
                }
            }
            return true;
        }
        
    }
}
