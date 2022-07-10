using System;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public static class CompAbilityUserExtensions
    {
        public static bool IsCastInRange(this CompAbilityUser abilityUser, Power power, LocalTargetInfo localTarget, PawnAbility ability)
        {
            if (!power.autocasting.ValidType(power.autocasting.GetTargetType, localTarget))
            {
                return false;
            }
            if (power.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(abilityUser.Pawn.Position, localTarget.Thing, abilityUser.Pawn, power.autocasting.minRange, ability.Def.MainVerb.range))
            {
                return false;
            }
            if (power.autocasting.maxRange != 0f && power.autocasting.maxRange < (abilityUser.Pawn.Position - localTarget.Thing.Position).LengthHorizontal)
            {
                return false;
            }
      
            return true;
        }
            
        //I'm not particularly thrilled with this delegate approach, but it seems to be the best performance with
        //the least amount of repeated code I can think of
        public static bool CanTargetOtherPawn(
            this CompAbilityUser abilityUser, 
            Power power, 
            LocalTargetInfo localTarget, 
            PawnAbility ability, 
            Func<Pawn, bool> enemyInvalidCondition
            )
        { 
            if (!abilityUser.IsCastInRange(power, localTarget, ability))
            {
                return false;
            }
            
            Thing targetThing = localTarget.Thing;
            bool isHostile = targetThing.Faction != null && targetThing.Faction.HostileTo(abilityUser.Pawn.Faction);
            bool targetEnemy = power.autocasting.targetEnemy && isHostile;
            
            if (targetThing is Pawn targetPawn)
            {
                if (targetEnemy && enemyInvalidCondition(targetPawn))
                {
                    return false;
                }
            }
            
            bool targetNeutral = power.autocasting.targetNeutral && !isHostile;
            bool targetFriendly = power.autocasting.targetFriendly && targetThing.Faction == abilityUser.Pawn.Faction;
            return (targetEnemy || targetNeutral || targetFriendly) && power.autocasting.ValidConditions(abilityUser.Pawn, targetThing);
        }

        public static bool CanTargetOtherPawn(
            this CompAbilityUser abilityUser, 
            Power power, 
            LocalTargetInfo localTarget, 
            PawnAbility ability,
            Func<Pawn, bool> enemyInvalidCondition,
            Func<Pawn, Power, bool> neutralInvalidCondition)
        {
            if (!abilityUser.IsCastInRange(power, localTarget, ability))
            {
                return false;
            }
            
            Thing targetThing = localTarget.Thing;
            bool isHostile = targetThing.Faction != null && targetThing.Faction.HostileTo(abilityUser.Pawn.Faction);
            bool targetEnemy = power.autocasting.targetEnemy && isHostile;
            bool targetNeutral = power.autocasting.targetNeutral && !isHostile;
            
            if (targetThing is Pawn targetPawn)
            {
                if (targetEnemy && enemyInvalidCondition(targetPawn))
                {
                    return false;
                }

                if (targetNeutral && neutralInvalidCondition(targetPawn, power))
                {
                    if (targetPawn.Downed || targetPawn.IsPrisoner)
                    {
                        return false;
                    }
                    if (power.abilityDef.MainVerb.isViolent && targetThing.Faction != null && !targetPawn.InMentalState)
                    {
                        return false;
                    }
                }
            }
            
            bool targetFriendly = power.autocasting.targetFriendly && targetThing.Faction == abilityUser.Pawn.Faction;
            return (targetEnemy || targetNeutral || targetFriendly) && power.autocasting.ValidConditions(abilityUser.Pawn, targetThing);
        }
    }
}