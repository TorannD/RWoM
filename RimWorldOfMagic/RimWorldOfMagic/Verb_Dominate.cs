using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_Dominate : Verb_UseAbility  
    {

        private int verVal;
        private int pwrVal;
        private int effVal;

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    //out of range
                    validTarg = true;
                }
                else
                {
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
            bool result = false;
            Pawn p = this.CasterPawn;
            Pawn hitPawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            verVal = TM_Calc.GetSkillVersatilityLevel(p, this.Ability.Def as TMAbilityDef, true);
            pwrVal = TM_Calc.GetSkillPowerLevel(p, this.Ability.Def as TMAbilityDef);
            effVal = TM_Calc.GetSkillEfficiencyLevel(p, this.Ability.Def as TMAbilityDef);
            //MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Dominate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Dominate_pwr");
            //MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Dominate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Dominate_ver");
            //MagicPowerSkill eff = comp.MagicData.MagicPowerSkill_Dominate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Dominate_eff");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            //effVal = eff.level;
            //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //if(settingsRef.AIHardMode && !p.IsColonist)
            //{
            //    verVal = 3;
            //    pwrVal = 3;
            //    effVal = 3;
            //}
            //if (p.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = p.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = p.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    MightPowerSkill meff = p.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_eff");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //    effVal = meff.level;
            //}

            if (this.currentTarget != null && p != null)
            {                
                Map map = this.CasterPawn.Map;
                
                if (hitPawn != null && hitPawn is Pawn && !hitPawn.Dead)
                {
                    if (Rand.Chance(TM_Calc.GetSpellSuccessChance(p, hitPawn, true)))
                    {
                        Hediff hediff = new Hediff();
                        if (p.gender == Gender.Female || p.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                        {
                            if (pwrVal == 3)
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_SDDominateHD_III, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SDDominateHD_III);
                            }
                            else if (pwrVal == 2)
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_SDDominateHD_II, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SDDominateHD_II);
                            }
                            else if (pwrVal == 1)
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_SDDominateHD_I, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SDDominateHD_I);
                            }
                            else
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_SDDominateHD, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SDDominateHD);
                            }
                            for (int i = 0; i < 4; i++)
                            {
                                TM_MoteMaker.ThrowShadowMote(hitPawn.Position.ToVector3(), map, Rand.Range(.6f, 1f));
                            }
                        }
                        if (p.gender == Gender.Male || p.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                        {
                            if (pwrVal == 3)
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_WDDominateHD_III, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WDDominateHD_III);
                            }
                            else if (pwrVal == 2)
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_WDDominateHD_II, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WDDominateHD_II);
                            }
                            else if (pwrVal == 1)
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_WDDominateHD_I, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WDDominateHD_I);
                            }
                            else
                            {
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_WDDominateHD, Rand.Range(1.5f + verVal, 3f + verVal));
                                hediff = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WDDominateHD);
                            }
                        }
                        HediffComp_Dominate hediffComp = hediff.TryGetComp<HediffComp_Dominate>();
                        hediffComp.EffVal = effVal;
                        hediffComp.VerVal = verVal;
                        if (hitPawn.IsColonist && !p.IsColonist)
                        {
                            TM_Action.SpellAffectedPlayerWarning(hitPawn);
                        }
                    }
                    else
                    {
                        MoteMaker.ThrowText(hitPawn.DrawPos, hitPawn.Map, "TM_ResistedSpell".Translate(), -1);
                    }
                }
                else
                {
                    Messages.Message("TM_NothingToDominate".Translate(), MessageTypeDefOf.NeutralEvent);
                }
                result = true;
            }

            this.burstShotsLeft = 0;
            return result;
        }        
    }
}
