using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;


namespace TorannMagic
{
    public class WorkGiver_DoCommanderMotivate : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();           
            if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CommanderAuraHD, false))
            {
                HediffComp_CommanderAura hdComp = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_CommanderAuraHD).TryGetComp<HediffComp_CommanderAura>();
                if (hdComp != null && hdComp.nextSpeechTick < Find.TickManager.TicksGame)
                {               
                    if (pawn2 != null && pawn2 != pawn && pawn2.RaceProps.Humanlike && pawn2.IsColonist && pawn2.Awake() && !pawn2.Drafted && !pawn.Drafted && !pawn2.Downed)
                    {
                        if (pawn2.InMentalState && (pawn.Position - pawn2.Position).LengthHorizontal < 50f && !GenAI.EnemyIsNear(pawn2, 20f))
                        {
                            hdComp.nextSpeechTick = Find.TickManager.TicksGame + 100;
                            LocalTargetInfo target = pawn2;
                            if (pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly, 1, -1, null, true))
                            {
                                hdComp.nextSpeechTick = Find.TickManager.TicksGame + (Rand.Range(12000, 15000) - (1200 * hdComp.pwrVal));
                                return true;
                            }                            
                        }
                    }
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Pawn pawn2 = t as Pawn;
            //CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            //if(comp.nextEntertainTick >= Find.TickManager.TicksGame)
            //{
            //    return null;
            //}
            return new Job(TorannMagicDefOf.JobDriver_TM_Command, t);        
        }
    }
}
