using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_BloodBoost : HediffComp_EnchantedItem
    {

        public float maxSeverity = 0;

        public override void CompExposeData()
        {
            Scribe_Values.Look<float>(ref this.maxSeverity, "maxSeverity", 0, false);
            base.CompExposeData();            
        }

        public override void PostInitialize()
        {
            this.hediffActionRate = 300;            
        }

        public override void HediffActionTick()
        {
            float pawnDownCount = 0;
            float pawnKillCount = 0;
            if (this.Pawn.records != null)
            {
                pawnDownCount = this.Pawn.records.GetValue(RecordDefOf.PawnsDownedHumanlikes);
                pawnKillCount = this.Pawn.records.GetValue(RecordDefOf.KillsHumanlikes);
            }
            this.maxSeverity = Mathf.Min((2 * pawnKillCount) + pawnDownCount, 100);
            this.parent.Severity = this.maxSeverity;
        }

    }
}
