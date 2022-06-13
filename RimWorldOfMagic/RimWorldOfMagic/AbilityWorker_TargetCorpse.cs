using System.Collections.Generic;
using Verse;
using AbilityUserAI;
using System.Linq;

namespace TorannMagic
{
    public class AbilityWorker_TargetCorpse : AbilityWorker
    {
        public override LocalTargetInfo TargetAbilityFor(AbilityAIDef abilityDef, Pawn pawn)
        {
            Corpse corpse = this.PickClosestCorpse(abilityDef, pawn);
            bool flag = corpse == null;
            LocalTargetInfo result;
            if (flag)
            {
                result = base.TargetAbilityFor(abilityDef, pawn);
            }
            else
            {
                result = corpse;
            }
            return result;
        }

        public override bool CanPawnUseThisAbility(AbilityAIDef abilityDef, Pawn pawn, LocalTargetInfo target)
        {
            Corpse corpse = this.PickClosestCorpse(abilityDef, pawn);
            bool flag = corpse == null;
            return !flag && base.CanPawnUseThisAbility(abilityDef, pawn, target);
        }

        public virtual Corpse PickClosestCorpse(AbilityAIDef abilityDef, Pawn pawn)
        {
            Corpse corpse = null;
            Thing corpseThing = null;

            IntVec3 curCell;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(pawn.Position, 6f, true);
            for (int i = 0; i < targets.Count(); i++)
            {
                curCell = targets.ToArray<IntVec3>()[i];
                if (curCell.InBoundsWithNullCheck(pawn.Map) && curCell.IsValid)
                {
                    List<Thing> thingList;
                    thingList = curCell.GetThingList(pawn.Map);
                    int z = 0;
                    while (z < thingList.Count)
                    {
                        corpseThing = thingList[z];
                        if (corpseThing != null)
                        {
                            bool validator = corpseThing is Corpse;
                            if (validator)
                            {
                                corpse = corpseThing as Corpse;
                            }                            
                        }
                    }
                }
            }

            IEnumerable<IntVec3> targetsD = GenRadial.RadialCellsAround(pawn.TargetCurrentlyAimingAt.Cell, 6f, true);
            for (int i = 0; i < targets.Count(); i++)
            {
                curCell = targets.ToArray<IntVec3>()[i];
                if (curCell.InBoundsWithNullCheck(pawn.Map) && curCell.IsValid)
                {
                    List<Thing> thingList;
                    thingList = curCell.GetThingList(pawn.Map);
                    int z = 0;
                    while (z < thingList.Count)
                    {
                        corpseThing = thingList[z];
                        if (corpseThing != null)
                        {
                            bool validator = corpseThing is Corpse;
                            if (validator)
                            {
                                corpse = corpseThing as Corpse;
                            }
                        }
                    }
                }
            }

            return corpse;
        }
    }
}
