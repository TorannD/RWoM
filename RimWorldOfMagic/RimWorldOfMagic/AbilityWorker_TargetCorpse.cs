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
            return PickClosestCorpse(abilityDef, pawn) ?? base.TargetAbilityFor(abilityDef, pawn);
        }

        public override bool CanPawnUseThisAbility(AbilityAIDef abilityDef, Pawn pawn, LocalTargetInfo target)
        {
            return PickClosestCorpse(abilityDef, pawn) != null && base.CanPawnUseThisAbility(abilityDef, pawn, target);
        }

        public virtual Corpse PickClosestCorpse(AbilityAIDef abilityDef, Pawn pawn)
        {
            foreach (IntVec3 curCell in GenRadial.RadialCellsAround(pawn.TargetCurrentlyAimingAt.Cell, 6f, true))
            {
                if (!curCell.InBoundsWithNullCheck(pawn.Map) || !curCell.IsValid) continue;

                Corpse corpse = curCell.GetThingList(pawn.Map).OfType<Corpse>().FirstOrDefault();
                if (corpse != null) return corpse;
            }

            foreach (IntVec3 curCell in GenRadial.RadialCellsAround(pawn.Position, 6f, true))
            {
                if (!curCell.InBoundsWithNullCheck(pawn.Map) || !curCell.IsValid) continue;

                Corpse corpse = curCell.GetThingList(pawn.Map).OfType<Corpse>().FirstOrDefault();
                if (corpse != null) return corpse;
            }

            return null;
        }
    }
}
