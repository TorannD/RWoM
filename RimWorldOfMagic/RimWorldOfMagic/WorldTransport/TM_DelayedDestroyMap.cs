using System;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.WorldTransport
{
    public class TM_DelayedDestroyMap : WorldObjectComp
    {
        public int delayTicks = 100;
        private int age = 0;

        public override void CompTick()
        {
            base.CompTick();
            age++;
            MapParent mapParent = parent as MapParent;
            Map map = mapParent?.Map;
            if (map == null)
            {
                // The map is already gone — nothing left to clean up.
                parent.AllComps.Remove(this);
                return;
            }
            // Cancel the delayed cleanup when colonists returned or the map
            // became home again; destroying it under them would lose the map.
            if (map.mapPawns.AnyColonistSpawned || map.IsPlayerHome)
            {
                parent.AllComps.Remove(this);
                return;
            }
            if (age >= delayTicks)
            {
                parent.AllComps.Remove(this);
                Current.Game.DeinitAndRemoveMap(map, false);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.delayTicks, "delayTicks", 100, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
        }

    }
}
