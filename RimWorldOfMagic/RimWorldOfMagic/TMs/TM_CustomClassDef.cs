using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class TM_CustomClassDef : Def
    {
        public List<TM_CustomClass> customClasses = new List<TM_CustomClass>();

        public static TM_CustomClassDef Named(string defName)
        {
            return DefDatabase<TM_CustomClassDef>.GetNamed(defName);
        }
    }
}
