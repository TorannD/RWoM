using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_Mind : HediffComp_EnchantedItem
    {

        public override void CompExposeData()
        {            
            base.CompExposeData();
        }

        public override void PostInitialize()
        {
            this.hediffActionRate = 1800;            
        }

        public override void HediffActionTick()
        {
            if (this.Pawn.mindState.mentalStateHandler.InMentalState && Rand.Chance(.08f))
            {
                Messages.Message("TM_BrokenOutOfMentalState".Translate(this.Pawn.LabelShort, this.Pawn.mindState.mentalStateHandler.CurState.def.label), this.Pawn, MessageTypeDefOf.PositiveEvent);
                this.Pawn.mindState.mentalStateHandler.CurState.RecoverFromState();                
            }
        }

    }
}
