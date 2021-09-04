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
	public class CompAbilityEffect_SpawnDetailed : CompAbilityEffect
	{
		public new CompProperties_AbilitySpawnDetailed Props => (CompProperties_AbilitySpawnDetailed)props;

		public override bool HideTargetPawnTooltip => true;

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			if(Props.spawnThingDef != null)
            {

				//Building_TMMagicCircleBase.TrySpawnThing(CasterPawn, Props.spawnThingDef, null, target.Cell, 10000, true, 1, 1, false);

				AbilityUser.SpawnThings spawnables = new AbilityUser.SpawnThings();
				spawnables.def = Props.spawnThingDef;
				spawnables.spawnCount = Props.spawnCount;
				spawnables.temporary = Props.temporary;		

				Building_TMElementalRift_Defenders thing = (Building_TMElementalRift_Defenders)TM_Action.SingleSpawnLoop(this.parent.pawn, spawnables, target.Cell, this.parent.pawn.Map, Props.durationTicks, Props.temporary, false, this.parent.pawn.Faction, false, ThingDef.Named("BlocksMarble"));
				thing.duration = Props.durationTicks;
			}
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			//if (!target.IsValid)
			//{
			//	return false;
			//}
			if (target.Cell.Filled(parent.pawn.Map) || (target.Cell.GetFirstBuilding(parent.pawn.Map) != null && !Props.allowOnBuildings))
			{
				if (throwMessages)
				{
					Messages.Message("AbilityOccupiedCells".Translate(parent.def.LabelCap), target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
			if (Props.requiresLineOfSight && !GenSight.LineOfSight(this.parent.pawn.Position, target.Cell, parent.pawn.Map))
			{
				return false;
			}
			return true;
		}

	}
}
