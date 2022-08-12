using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class GameCondition_ManaDrain : GameCondition
    {
        List<Pawn> victims;

        public override void Init()
        {
            Map map = base.SingleMap;
            victims = new List<Pawn>();
            victims.Clear();
            
            if (map != null)
            {
                victims = map.mapPawns.FreeColonistsAndPrisoners.ToList();
            }
            else
            {
                List<Map> allMaps = base.AffectedMaps;
                for(int i = 0; i < allMaps.Count; i++)
                {
                    victims.AddRange(allMaps[i].mapPawns.AllPawnsSpawned);
                }
                
            }
            int num = victims.Count<Pawn>();
            Pawn pawn;
            for (int i = 0; i < num; i++)
            {
                pawn = victims.ToArray<Pawn>()[i];
                if (pawn != null)
                {
                    CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                    if (comp != null && comp.IsMagicUser && comp.Mana != null)
                    {
                        if ( comp.Mana.CurLevel == 1)
                        {
                            comp.Mana.CurLevel -= .01f;
                        }
                    }
                }
                victims.GetEnumerator().MoveNext();
            }
        }

        public GameCondition_ManaDrain()
        {

        }

    }
}
