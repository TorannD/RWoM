using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class PawnColumnWorker_GolemAge : PawnColumnWorker_Text
    {
        protected override GameFont DefaultHeaderFont => GameFont.Small;

        public override int Compare(Pawn a, Pawn b)
        {
            return a.ageTracker.AgeBiologicalYears.CompareTo(b.ageTracker.AgeBiologicalYears);
        }

        protected override string GetTextFor(Pawn pawn)
        {
            return pawn.ageTracker.AgeBiologicalYears.ToString();
        }
    }
}
