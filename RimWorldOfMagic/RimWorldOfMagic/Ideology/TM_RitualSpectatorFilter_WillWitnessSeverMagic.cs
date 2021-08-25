using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace TorannMagic.Ideology
{
    public class TM_RitualSpectatorFilter_WillWitnessSeverMagic : RitualSpectatorFilter
    {
        public override bool Allowed(Pawn p)
        {
            if (p.IsSlave || p.Ideo == null)
            {
                return true;
            }
            return p.Ideo.MemberWillingToDo(new HistoryEvent(TorannMagicDefOf.TM_SeverMagicEvent, "SUBJECT"));
        }
    }
}
