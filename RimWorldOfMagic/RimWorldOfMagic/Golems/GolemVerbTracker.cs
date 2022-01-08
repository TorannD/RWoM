using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;

namespace TorannMagic.Golems
{
    public class GolemVerbTracker
    {
        public Verb verb = null;
        public int lastUsedTick = 0;

        public GolemVerbTracker(Verb v, int tick)
        {
            verb = v;
            lastUsedTick = tick;
        }
    }
}
