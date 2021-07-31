using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_ArcaneRecovery : HediffComp_EnchantedItem
    {

        public override void CompExposeData()
        {            
            base.CompExposeData();
        }

        public override void PostInitialize()
        {
            this.hediffActionRate = 900;            
        }

        public override void HediffActionTick()
        {
            Hediff weakness = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness, false);
            if(weakness != null)
            {
                weakness.Severity -= Rand.Range(.01f, .04f);
            }
        }
    }
}
