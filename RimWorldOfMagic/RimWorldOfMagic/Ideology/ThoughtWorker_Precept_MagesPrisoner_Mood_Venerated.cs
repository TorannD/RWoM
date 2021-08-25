using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagesPrisoner_Mood_Venerated : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsSlave && !p.IsQuestLodger())
            {
                List<Pawn> tmpList = PawnsFinder.AllMaps_SpawnedPawnsInFaction(p.Faction);
                foreach(Pawn otherPawn in tmpList)
                {
                    if(otherPawn.IsPrisoner)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
