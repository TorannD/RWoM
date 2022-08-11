using System.Text;
using AbilityUser;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace TorannMagic
{
	public class TMAbilityDef : AbilityUser.AbilityDef
	{
        //Add new variables here to control skill levels
        public float manaCost = 0f;                             //hardcoded mana cost of ability
        public float staminaCost = 0f;                          //hardcoded stamina cost of ability
        public float bloodCost = 0f;                            //hardcoded blood cost of ability
        public float chiCost = 0f;                              //hardcoded chi cost of ability 
        public bool consumeEnergy = true;                       //does not perform energy validation or reduction if false
        public int abilityPoints = 1;                           //ability points to level - only applies to tiered abilities
        public float learnChance = 1f;                          //chance a pawn learns this ability as a class ability if a torn book is used; doesn't apply to legacy classes
        public float efficiencyReductionPercent = 0f;           //reduces the energy cost of this ability when the efficiency skill is leveled
        public float upkeepEnergyCost = 0f;                     //reduces maximum energy to maintain this ability
        public float upkeepRegenCost = 0f;                      //reduces energy regen
        public float upkeepEfficiencyPercent = 0f;              //lowers upkeep costs (both max energy and energy regen)
        public bool shouldInitialize = true;                    //loads the ability gizmo - set to false for passive abilities
        public float weaponDamageFactor = 1f;                   //calculates ability damage based on equipped weapon - only applies to abilities that use this value
        public HediffDef abilityHediff = null;                  //*do not use* - old custom energy cost
        public ThingDef learnItem = null;                       //allows ability to be learned via this item - usually a scroll, commonly required to acquire cantrips
        public bool canCopy = false;                            //whether this ability is allowed to be copied by legion or faceless 'mimic' abilities
        public List<string> requiredWeaponsOrCategories = null; //Unarmed, Melee, Ranged, Bows, Rifles, Shotguns, Pistols, MagicalFoci or defName, must be equipped to use this ability
        public int relationsAdjustment = 0;                     //modified relationships by this much if used on a non-hostile, non-wild, non-colonist pawn
        public bool restrictedAbility = false;                  //identifies the ability as restricted, requiring a specific trait/class which is set in custom class learnable skills/spells

        public NeedDef requiredNeed = null;                     //identifies a need required to be present on the pawn to use this ability
        public float needCost = 0f;                             //how much of the required need is required to use the ability and how much need will be reduced after using the ability
        public float needXPFactor = 100f;                       //multiplier for amount of need consumed when applying xp gain; needs 0.0-1.0 will typically have a factor of 100
        public HediffDef requiredHediff = null;                 //identifes a hediff required to be present on the pawn to use this ability
        public float hediffCost = 0f;                           //severity of the hediff required to use the ability and how much severity will be reduced after using the ability
        public float hediffXPFactor = 100f;                     //multiplier for hediff severity consumed when applying xp gain; hediff ranges from 0.0-1.0 will typically have a factor of 100
        public InspirationDef requiredInspiration = null;       //pawn must have this inspiriation to use the ability
        public bool requiresAnyInspiration = false;             //pawn must have a (any) inspiration to use this ability
        public bool consumesInspiration = true;                 //whether the inspiration is consumed after the ability is used

        public List<TMAbilityDef> childAbilities = new List<TMAbilityDef>();    //child abilities are also gained with the parent ability, an example is super soldier pistol spec

        public TMAbilityDef chainedAbility = null;              //another ability that becomes available after using this ability
        public int chainedAbilityExpiresAfterTicks = -1;        //chained ability is available for this many ticks after it appears; set to -1 to never expire
        public bool chainedAbilityExpiresAfterCooldown = true;  //links the cooldown of this ability to the removal of the chained ability; accounts for cooldown reduction
        public bool removeAbilityAfterUse = false;              //ability is removed after use   
        public List<TMAbilityDef> abilitiesRemovedWhenUsed = new List<TMAbilityDef>();       //removes all listed abilities when this ability is used; useful for resetting an ability chain

        public string GetPointDesc()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(this.GetDescription());
			return stringBuilder.ToString();
		}

        private bool cacheRestriction = true;
        private int nextRestrictionCheckTick = 0;
        public bool IsRestrictedByEquipment(Pawn p)
        {             
            if (requiredWeaponsOrCategories != null && requiredWeaponsOrCategories.Count > 0)
            {                
                if (Find.TickManager.TicksGame >= nextRestrictionCheckTick)
                {
                    nextRestrictionCheckTick = Find.TickManager.TicksGame + Rand.Range(59, 61);
                    cacheRestriction = true;
                    foreach (string str in requiredWeaponsOrCategories)
                    {
                        if (str == "Unarmed")
                        {
                            if (p.equipment != null && p.equipment.Primary == null)
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "MeleeAndUnarmed")
                        {
                            if (TM_Calc.IsUsingMelee(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "Melee")
                        {
                            if (p.equipment != null && p.equipment.Primary == null)
                            {
                                return true;
                            }
                            if (TM_Calc.IsUsingMelee(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "Ranged")
                        {
                            if (TM_Calc.IsUsingRanged(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "Bows")
                        {
                            if (TM_Calc.IsUsingBow(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "Pistols")
                        {
                            if (TM_Calc.IsUsingPistol(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "Rifles")
                        {
                            if (TM_Calc.IsUsingRifle(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "Shotguns")
                        {
                            if (TM_Calc.IsUsingShotgun(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else if (str == "MagicalFoci")
                        {
                            if (TM_Calc.IsUsingMagicalFoci(p))
                            {
                                return (cacheRestriction = false);
                            }
                            return true;
                        }
                        else
                        {
                            if (p.equipment != null && p.equipment.Primary != null)
                            {
                                if (p.equipment.Primary.def.defName == str)
                                {
                                    return (cacheRestriction = false);
                                }
                                else if(TM_Calc.IsUsingCustomWeaponCategory(p, str))
                                {

                                }
                            }
                        }
                    }
                    return true;
                }
                return cacheRestriction;
            }
            return false;
        }
    }
}
