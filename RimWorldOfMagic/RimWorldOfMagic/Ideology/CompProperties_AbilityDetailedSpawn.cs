using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class CompProperties_AbilityStartEvent : CompProperties_AbilityEffect
    {
        public IncidentDef incidentDef = null;
        public GameConditionDef gameConditionDef = null;

        public CompProperties_AbilityStartEvent()
        {
            compClass = typeof(CompAbilityEffect_StartEvent);
        }
    }
}
