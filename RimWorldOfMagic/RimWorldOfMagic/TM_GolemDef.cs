using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class TM_GolemDef : Def
    {
        public List<TM_Golem> golems = new List<TM_Golem>();

        public static TM_GolemDef Named(string defName)
        {
            return DefDatabase<TM_GolemDef>.GetNamed(defName);
        }
    }
}
