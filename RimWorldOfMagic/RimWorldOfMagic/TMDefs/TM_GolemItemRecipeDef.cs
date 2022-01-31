using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.TMDefs
{
    public class TM_GolemItemRecipeDef : Def
    {
        public ThingDef inputThing;
        public int inputCharges;
        public ThingDef outputThing;
        public int outputCount;

        public static TM_GolemItemRecipeDef Named(string defName)
        {
            return DefDatabase<TM_GolemItemRecipeDef>.GetNamed(defName);
        }
    }
}
