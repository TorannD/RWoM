using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_SeverityPercent : HediffComp
    {
        public override string CompLabelInBracketsExtra => this.parent.Severity.ToString("P0");
     
    }
}
