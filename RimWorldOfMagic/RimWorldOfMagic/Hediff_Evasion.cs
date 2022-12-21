using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Hediff_Evasion : HediffWithComps
    {
        public override string Label => base.Label + " (" + this.Severity.ToString("P0") + ") ";
    }
}
