using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_StrongBack : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffStrongBack))
                {
                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def.defName.Contains("TM_HediffStrongBack"))
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                        }
                    }
                }
                else
                {
                    float val = .5f;
                    if (caster.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 3)
                    {
                        val = 1.5f;
                    }
                    if (caster.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 8)
                    {
                        val = 2.5f;
                    }
                    
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffStrongBack, val);
                    FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
                }
            }
            return true;
        }
    }
}
