using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;


namespace TorannMagic
{
    public class WorkGiver_DoEntertain : WorkGiver_Scanner
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
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_EntertainingHD"), false) && comp.nextEntertainTick < Find.TickManager.TicksGame)
            {
                if (pawn2 != null && pawn2 != pawn && pawn2.RaceProps.Humanlike && pawn2.IsColonist && pawn2.Awake() && !pawn2.Drafted && !pawn.Drafted && !pawn2.Downed && pawn2.CanCasuallyInteractNow())
                {
                    if ((pawn.Position - pawn2.Position).LengthHorizontal < 50f && !GenAI.EnemyIsNear(pawn2, 40f))
                    {
                        IEnumerable<Pawn> breakingPawnsExtreme = BreakRiskAlertUtility.PawnsAtRiskExtreme;
                        foreach (Pawn current in breakingPawnsExtreme)
                        {
                            if(current == pawn2)
                            {
                                bool flag = true;
                                LocalTargetInfo target = pawn2;
                                List<Thought_Memory> pawn2Memories = pawn.needs.mood.thoughts.memories.Memories;
                                for(int i = 0; i < pawn2Memories.Count; i++)
                                {
                                    if (pawn2Memories[i].def.defName == "TM_EntertainedTD" && pawn2Memories[i].MoodOffset() > 11f)
                                    {
                                        flag = false;                                    
                                    }
                                }   
                                if(flag && pawn.CanReserve(target, 1, -1, null, forced))
                                {
                                    return true;
                                }
                                return false;
                            }
                        }
                        IEnumerable<Pawn> breakingPawnsMajor = BreakRiskAlertUtility.PawnsAtRiskMajor;
                        foreach (Pawn current in breakingPawnsMajor)
                        {
                            if (current == pawn2)
                            {
                                bool flag = true;
                                List<Thought_Memory> pawn2Memories = pawn.needs.mood.thoughts.memories.Memories;
                                for (int i = 0; i < pawn2Memories.Count; i++)
                                {
                                    if (pawn2Memories[i].def.defName == "TM_EntertainedTD")
                                    {
                                        flag = false;
                                    }
                                }
                                if (flag)
                                {
                                    return true;
                                }
                                return false;
                            }
                        }
                        IEnumerable<Pawn> breakingPawnsMinor = BreakRiskAlertUtility.PawnsAtRiskMinor;
                        foreach (Pawn current in breakingPawnsMinor)
                        {
                            if (current == pawn2)
                            {
                                bool flag = true;
                                List<Thought_Memory> pawn2Memories = pawn.needs.mood.thoughts.memories.Memories;
                                for (int i = 0; i < pawn2Memories.Count; i++)
                                {
                                    if (pawn2Memories[i].def.defName == "TM_EntertainedTD")
                                    {
                                        flag = false;
                                    }
                                }
                                if (flag)
                                {
                                   
                                    return true;
                                }
                                return false;
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
            return new Job(TorannMagicDefOf.JobDriver_Entertain, t);        
        }
    }
}
