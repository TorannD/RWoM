using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Sprint : Verb_UseAbility
    {

        private int pwrVal = 0;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            //MightPowerSkill pwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Sprint.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Sprint_pwr");
            //pwrVal = pwr.level;
            //if(pwrVal == 0)
            //{
            //    pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level;
            //}
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            if (pawn == null || pawn.Dead) return true;

            CompAbilityUserMight comp = TM_Calc.GetCompAbilityUserMight(pawn);
            if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffSprint))
            {
                Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffSprint);
                pawn.health.RemoveHediff(hd);
                   
                if (comp == null || comp.MightData == null) return true;
                if (this.Ability.Def == TorannMagicDefOf.TM_Sprint_I)
                {
                    comp.Stamina.CurLevel += .2f;
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EnergyRegenHD, 1.2f);
                    HediffComp_SetDuration hd2 = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_SetDuration>();
                    hd2.duration = 5;
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Sprint_II)
                {
                    comp.Stamina.CurLevel += .3f;
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EnergyRegenHD, 1.3f);
                    HediffComp_SetDuration hd2 = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_SetDuration>();
                    hd2.duration = 8;
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Sprint_III)
                {
                    comp.Stamina.CurLevel += .4f;
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EnergyRegenHD, 1.4f);
                    HediffComp_SetDuration hd2 = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_SetDuration>();
                    hd2.duration = 10;
                }
            }
            else
            {
                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffSprint, .5f + pwrVal);
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
                if (comp == null || comp.MightData == null) return true;
                if (this.Ability.Def == TorannMagicDefOf.TM_Sprint_I)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EvasionHD, .25f);
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_AdrenalineHD, .2f);
                    HediffComp_SetDuration hd2 = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AdrenalineHD, false).TryGetComp<HediffComp_SetDuration>();
                    hd2.duration = 5;
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Sprint_II)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EvasionHD, .1f);
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_AdrenalineHD, .3f);
                    HediffComp_SetDuration hd2 = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AdrenalineHD, false).TryGetComp<HediffComp_SetDuration>();
                    hd2.duration = 8;
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(pawn.Map, pawn.Position, 5f, pawn.Faction, true);
                    if(pList != null && pList.Count > 0)
                    {
                        foreach(Pawn p in pList)
                        {
                            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_EvasionHD, .25f);
                        }
                    }
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Sprint_III)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EvasionHD, .1f);
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_AdrenalineHD, .55f);
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(pawn.Map, pawn.Position, 10f, pawn.Faction, true);
                    if (pList != null && pList.Count > 0)
                    {
                        foreach (Pawn p in pList)
                        {                                
                            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_EvasionHD, .3f);
                        }
                    }
                }
            }            
            return true;
        }
    }
}
