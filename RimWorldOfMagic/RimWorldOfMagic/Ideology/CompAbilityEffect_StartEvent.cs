using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Sound;

namespace TorannMagic.Ideology
{
	public class CompAbilityEffect_StartEvent : CompAbilityEffect
	{
		public new CompProperties_AbilityStartEvent Props => (CompProperties_AbilityStartEvent)props;

		public override bool HideTargetPawnTooltip => true;

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			if(Props.incidentDef != null)
            {
				Building_TMMagicCircleBase.TryGenerateIncident(Props.incidentDef, target.Pawn.Map);
            }
			if(Props.gameConditionDef != null)
            {
				Building_TMMagicCircleBase.TryGenerateMapCondition(Props.gameConditionDef, target.Pawn.Map);
            }
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			Pawn pawn = target.Pawn;
			if (pawn == null)
			{
				return false;
			}
			if(Props.incidentDef == null && Props.gameConditionDef == null)
            {
				Log.Error("No event found to start");
				return false;
            }
			return true;
		}
		
	}
}
