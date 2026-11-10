using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Hex_Pain : Verb_HexControl
    {
        protected override void Apply(Pawn p, Pawn caster, int verVal)
        {
            TM_Action.DamageEntities(p, null, Rand.Range(2f, 4f) * (1f + (.1f * verVal)), 2, TMDamageDefOf.DamageDefOf.TM_PainDD, caster);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, p.DrawPos, p.Map, .7f, .2f, .2f, .3f, Rand.Range(-50, 50), Rand.Range(.5f, 1f), Rand.Range(-90, 90), Rand.Range(0, 360));
        }
    }
}
