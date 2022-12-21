using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_HeavyBlow : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffHeavyBlow))
                {
                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.hediffs.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def == TorannMagicDefOf.TM_HediffHeavyBlow)
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                        }
                    }
                }
                else
                {
                    int lvl = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level;
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffHeavyBlow, .95f + (.19f * lvl));
                    FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
                }
            }
            return true;
        }
    }
}
