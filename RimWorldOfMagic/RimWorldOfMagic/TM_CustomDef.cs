using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class TM_CustomDef : Def
    {
        public List<string> BloodLossHediffs;

        public static TM_CustomDef Named(string defName)
        {
            return DefDatabase<TM_CustomDef>.GetNamed(defName);
        }

        public List<string> Get_BloodLossHediffs
        {
            get
            {
                return BloodLossHediffs;
            }        
        }
    }
}
