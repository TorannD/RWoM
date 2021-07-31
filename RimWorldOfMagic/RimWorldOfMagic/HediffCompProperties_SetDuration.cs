using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffCompProperties_SetDuration : HediffCompProperties
    {
        public int duration = 10;

        public HediffCompProperties_SetDuration()
        {
            base.compClass = typeof(HediffComp_SetDuration);
        }
    }
}
