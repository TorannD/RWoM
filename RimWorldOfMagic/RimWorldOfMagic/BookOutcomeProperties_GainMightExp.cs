using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic
{
    public class BookOutcomeProperties_GainMightExp : BookOutcomeProperties
    {
        public override Type DoerClass => typeof(ReadingOutcomeDoerGainMightExp);
    }
}
