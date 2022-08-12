using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class StatPart_Undead : StatPart
    {
        private const float Factor = 0.0f;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn reqPawn)
            {
                if (reqPawn != null && TM_Calc.IsUndeadNotVamp(reqPawn))
                {
                    val *= 0f;
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn reqPawn)
            {
                if (reqPawn != null && TM_Calc.IsUndeadNotVamp(reqPawn))
                {
                    return "TM_StatsReport_Undead".Translate();
                }
            }
            return null;
        }
    }
}
