using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public static class TM_TextPool
    {
        public static string TM_PromoteWanderer = "TM_PromoteWanderer".Translate();
        public static string TM_PromoteWandererDesc = "TM_PromoteWandererDesc".Translate();
        public static string TM_PromoteWayfarer = "TM_PromoteWayfarer".Translate();
        public static string TM_PromoteWayfarerDesc = "TM_PromoteWayfarerDesc".Translate();
        public static string TM_RemovePossess = "TM_RemovePossession".Translate();
        public static string TM_RemovePossessDesc = "TM_RemovePossessionDesc".Translate();
    }
}
