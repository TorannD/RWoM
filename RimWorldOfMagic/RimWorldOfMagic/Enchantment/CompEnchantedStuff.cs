using System;
using Verse;
using RimWorld;
using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class CompEnchantedStuff : ThingComp
    {
        public CompProperties_EnchantedStuff Props
        {
            get
            {
                return (CompProperties_EnchantedStuff)this.props;
            }
        }
    }
}
