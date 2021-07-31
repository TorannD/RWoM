using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.WorldTransport
{
    public class TM_DropPodUtility
    {
        public static void MakeDropPodAt(IntVec3 c, Map map, ActiveDropPodInfo info, ThingDef makePodThing, ThingDef makeSkyfallerThing, bool draftFlag = false)
        {
            TM_ActiveDropPod activeDropPod = (TM_ActiveDropPod)ThingMaker.MakeThing(makePodThing);
            activeDropPod.Contents = info;
            activeDropPod.Contents.openDelay = 5;
            activeDropPod.draftFlag = draftFlag;
            TM_DropPodIncoming dpi = (TM_DropPodIncoming)SkyfallerMaker.SpawnSkyfaller(makeSkyfallerThing, activeDropPod, c, map);
            dpi.draftFlag = draftFlag;
            foreach (Thing item in (IEnumerable<Thing>)activeDropPod.Contents.innerContainer)
            {
                Pawn pawn;
                if ((pawn = (item as Pawn)) != null && pawn.IsWorldPawn())
                {
                    Find.WorldPawns.RemovePawn(pawn);
                    pawn.psychicEntropy?.SetInitialPsyfocusLevel();
                }
            }
        }
    }
}
