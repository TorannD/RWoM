using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using RimWorld;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using AbilityUser;
using TorannMagic.Enchantment;
using System.Text;
using TorannMagic.TMDefs;
using TorannMagic.ModOptions;

namespace TorannMagic.Golems
{
    public static class TM_GolemUtility
    {

        public static List<TM_GolemDef> GolemTypes()
        {
            IEnumerable<TM_GolemDef> tmpGolemDefs = from def in DefDatabase<TM_GolemDef>.AllDefs
                                             where (def.upgrades != null && def.upgrades.Count > 0)
                                             select def;
            return tmpGolemDefs.ToList();            
        }

        private static List<ThingDef> workstationDefs;
        public static List<ThingDef> GolemWorkstation
        {
            get
            {
                if(workstationDefs == null)
                {
                    workstationDefs = new List<ThingDef>();
                    workstationDefs.Clear();
                    foreach(TM_GolemDef gd in GolemTypes())
                    {
                        workstationDefs.Add(gd.golemWorkstationDef);
                    }
                }
                return workstationDefs;
            }
        }

        private static List<ThingDef> golemRaceDefs;
        public static List<ThingDef> GolemPawns
        {
            get
            {
                if (golemRaceDefs == null)
                {
                    golemRaceDefs = new List<ThingDef>();
                    golemRaceDefs.Clear();
                    foreach (TM_GolemDef gd in GolemTypes())
                    {
                        golemRaceDefs.Add(gd.golemDef);
                    }
                }
                return golemRaceDefs;
            }
        }

        public static TM_GolemDef GetGolemDefFromThing(Thing thing)
        {
            IEnumerable<TM_GolemDef> tmpGolemDefs = from def in DefDatabase<TM_GolemDef>.AllDefs
                                                    where (true)
                                                    select def;
            foreach(TM_GolemDef t in tmpGolemDefs)
            {
                if(t.golemDef == thing.def)
                {
                    return t;
                }
                if(t.golemWorkstationDef == thing.def)
                {
                    return t;
                }
            }
            return null;
        }
    }
}
