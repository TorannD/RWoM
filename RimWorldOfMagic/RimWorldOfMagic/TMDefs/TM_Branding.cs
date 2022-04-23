using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.TMDefs
{
    public class TM_Branding : IExposable
    {

        public Pawn brandedPawn = null;
        public HediffDef brandHediff = null;

        public void ExposeData()
        {
            Scribe_Defs.Look<HediffDef>(ref brandHediff, "brandHediff");
            Scribe_Deep.Look<Pawn>(ref brandedPawn, "brandedPawn");
        }

        public TM_Branding(Pawn p, HediffDef def)
        {
            brandedPawn = p;
            brandHediff = def;
        }

    }
}
