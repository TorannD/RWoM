using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_BrandingProtection : HediffComp_BrandingBase
    {
        public override int AverageUpdateTick => 1000;

        public bool canProtect => this.parent.Severity > .1f;

        public void TakeHit()
        {
            this.parent.Severity = Mathf.Clamp(this.parent.Severity - Rand.Range(.2f, .6f), .015f, 1f);
        }
    }
}
