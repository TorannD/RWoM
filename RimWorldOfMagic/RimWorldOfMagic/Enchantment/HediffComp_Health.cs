using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using TorannMagic.Utils;

namespace TorannMagic.Enchantment
{
    public class HediffComp_Health : HediffComp_EnchantedItem
    {

        public override void CompExposeData()
        {            
            base.CompExposeData();
        }

        public override void PostInitialize()
        {
            this.hediffActionRate = 1200;
        }

        public override void HediffActionTick()
        {
            IEnumerable<Hediff_Injury> injuries = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally() && !injury.IsPermanent())
                .DistinctBy(injury => injury.Part)
                .Take(2);

            foreach (Hediff_Injury injury in injuries)
            {
                injury.Heal(Rand.Range(.1f, .3f));
            }
        }
    }
}
