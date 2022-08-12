using RimWorld;
using System;
using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_Invisibility : Verb_UseAbility  
    {
        
        protected override bool TryCastShot()
        {
            bool result = false;
            bool arg_40_0;

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;

            if (pawn != null && !pawn.Downed)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD, false))
                {
                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def == TorannMagicDefOf.TM_InvisibilityHD)
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                        }
                    }
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowSiphonMote(pawn.DrawPos, pawn.Map, 1f);
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_InvisibilityHD, Mathf.RoundToInt(20f * pawn.GetCompAbilityUserMagic().arcaneDmg));
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, 1);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    List<Pawn> allPawns = this.CasterPawn.Map.mapPawns.AllPawnsSpawned;
                    for(int i =0; i < allPawns.Count; i++)
                    {
                        if(allPawns[i].Faction != null && allPawns[i].HostileTo(this.CasterPawn.Faction) && allPawns[i].CurJob != null && allPawns[i].CurJob.targetA != null && allPawns[i].CurJob.targetA.Thing == this.CasterPawn)
                        {
                            allPawns[i].jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, true);
                        }
                    }
                    if (this.CasterPawn.GetCompAbilityUserMagic()?.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 12)
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_InvisibilityHD, 5f);
                        TM_Action.DoAction_HealPawn(this.CasterPawn, this.CasterPawn, 3, 7);
                    }
                }                
                arg_40_0 = true;
            }
            else
            {
                arg_40_0 = false;
            }
            bool flag = arg_40_0;
            if (flag)
            {
                
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;

            return result;
        }
    }
}
