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

namespace TorannMagic
{
    public static class TM_GolemUtility
    {

        public static List<TMDefs.TM_Golem> Golems()
        {
            return TM_GolemDef.Named("TM_Golems").golems;
        }

        public static int IndexOfGolemDef(ThingDef thing)
        {
            for (int i = 0; i < Golems().Count; i++)
            {
                if (Golems()[i].golemDef == thing)
                {
                    return i;
                }
            }
            return -2;
        }

        public static int IndexOfWorkstationDef(ThingDef thing)
        {
            for (int i = 0; i < Golems().Count; i++)
            {
                if (Golems()[i].golemWorkstationDef == thing)
                {
                    return i;
                }
            }
            return -2;
        }

        public static TM_Golem GetGolemFromThingDef(ThingDef def)
        {
            int index = -2;
            index = IndexOfGolemDef(def);
            if(index != -2)
            {
                return Golems()[index];
            }
            return Golems()[IndexOfWorkstationDef(def)];
        }
    }
}
