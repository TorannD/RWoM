using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_HateBoost : HediffComp_EnchantedItem
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
            float damageTaken = 0;
            float damageDealt = 0;
            if (this.Pawn.records != null)
            {
                damageTaken = this.Pawn.records.GetValue(RecordDefOf.DamageTaken);
                damageDealt = this.Pawn.records.GetValue(RecordDefOf.DamageDealt);
            }
            this.maxSeverity = Mathf.Min((damageDealt / 100) + (damageTaken / 10), 100);
            this.parent.Severity = this.maxSeverity;
        }

    }
}
