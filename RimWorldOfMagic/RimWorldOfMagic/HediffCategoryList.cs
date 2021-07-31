using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class HediffCategoryList : Def
    {
        public List<TM_CategoryHediff> ailments;
        public List<TM_CategoryHediff> addictions;
        public List<TM_CategoryHediff> diseases;
        public List<TM_CategoryHediff> mechanites;

        public static HediffCategoryList Named(string defName)
        {
            return DefDatabase<HediffCategoryList>.GetNamed(defName);
        }
    }
}
