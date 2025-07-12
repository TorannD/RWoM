using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    public class HediffComp_ManaShield : HediffComp
    {
        //mana shield has a maximum asborb amount and can only absorb 1 hit every 2 ticks; keeps it from absorbing residual damage that occurs on the same tick
        public int lastHitTick = 0;
    }
}
