using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Hex_CriticalFail : Verb_HexControl
    {
        protected override void Apply(Pawn p, Pawn caster, int verVal)
        {
            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_Hex_CriticalFailHD, (.6f + (.1f * verVal)));
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, p.DrawPos, p.Map, .7f, .1f, .1f, .2f, Rand.Range(-50, 50), Rand.Range(.5f, 1f), Rand.Range(-90, 90), Rand.Range(0, 360));
        }
    }
}
