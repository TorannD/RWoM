using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_PsionicBoost : HediffComp_EnchantedItem
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
            float sensitivity = this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            this.maxSeverity = Mathf.Clamp((sensitivity - 1) * 100, 0, 100);
            this.parent.Severity = this.maxSeverity;
        }

    }
}
