using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class TM_CustomPowerDef : Def
    {
        public TM_CustomPower customPower = new TM_CustomPower();

        public static TM_CustomPowerDef Named(string defName)
        {
            return DefDatabase<TM_CustomPowerDef>.GetNamed(defName);
        }
    }
}
