using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.Sound;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_BattleHymn : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;


            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SingBattleHymnHD")))
                {
                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def.defName.Contains("TM_SingBattleHymnHD"))
                            {
                                pawn.health.RemoveHediff(rec);
                                TM_MoteMaker.ThrowSiphonMote(pawn.DrawPos, pawn.Map, 1f);
                            }
                        }
                    }
                }
                else
                {
                    SoundDef sound = TorannMagicDefOf.TM_BattleHymnSD;
                    sound.PlayOneShotOnCamera(caster.Map);
                    HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_SingBattleHymnHD"), .95f);
                    TM_MoteMaker.ThrowNoteMote(pawn.DrawPos, pawn.Map, .8f);
                    TM_MoteMaker.ThrowNoteMote(pawn.DrawPos, pawn.Map, .6f);
                    TM_MoteMaker.ThrowNoteMote(pawn.DrawPos, pawn.Map, .4f);
                }
            }
            return true;
        }
    }
}
