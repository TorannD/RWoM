using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_FightersFocus: Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffFightersFocus))
                {
                    Hediff rec = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffFightersFocus, false);
                    pawn.health.RemoveHediff(rec);
                }
                else
                {
                    float val = .5f;
                    if(caster.story != null)
                    {
                        if (caster.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 1)
                        {
                            val = 1.5f;
                        }
                    }
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffFightersFocus, val);
                    FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
                }
            }
            return true;
        }
    }
}
