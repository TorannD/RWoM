using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Hex_MentalAssault : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            int verVal = TM_Calc.GetSkillVersatilityLevel(CasterPawn, TorannMagicDefOf.TM_Hex);
            if (comp != null && comp.HexedPawns.Count > 0)
            {
                foreach(Pawn p in comp.HexedPawns)
                {
                    if (p.mindState != null && p.mindState.mentalStateHandler != null)
                    {
                        if (Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, p, true) * (.4f + (.1f * verVal))))
                        {
                            if (Rand.Chance(.3f) || p.RaceProps.Animal)
                            {
                                if (p.RaceProps.Animal)
                                {
                                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                                }
                                else
                                {
                                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                                }
                            }
                            else
                            {
                                if (p.Faction == Faction.OfPlayer)
                                {
                                    if(p.drafter != null && p.Drafted)
                                    {
                                        p.drafter.Drafted = false;                                        
                                    }
                                    p.jobs.EndCurrentJob(JobCondition.InterruptForced);
                                    Job job = new Job(JobDefOf.FleeAndCower, p.Position);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);

                                }
                                else
                                {
                                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                                }
                            }
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ScreamMote, p.DrawPos, p.Map, .4f, .2f, .2f, .3f, 0, Rand.Range(.5f, 1f), Rand.Range(-90, 90), 0);
                        }
                    }                    
                }
            }
            return true;
        }
    }
}
