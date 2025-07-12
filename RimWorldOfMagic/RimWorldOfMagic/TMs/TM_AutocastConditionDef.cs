using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class TM_AutocastConditionDef : Def
    {
        public TM_AutocastCondition autocastCondition = new TM_AutocastCondition();

        public static TM_AutocastConditionDef Named(string defName)
        {
            return DefDatabase<TM_AutocastConditionDef>.GetNamed(defName);
        }
    }
}
