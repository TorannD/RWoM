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
            return GenRadial.RadialCellsAround(pawn.Position, 6f, true)
                .Where(cell => cell.InBoundsWithNullCheck(pawn.Map) && cell.IsValid)
                .Select(cell => cell.GetThingList(pawn.Map).OfType<Corpse>().FirstOrDefault())
                .FirstOrDefault(corpse => corpse != null);
        }
    }
}
