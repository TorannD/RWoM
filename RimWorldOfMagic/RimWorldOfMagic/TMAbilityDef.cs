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
        public float manaCost = 0f;
        public float staminaCost = 0f;
        public float bloodCost = 0f;
        public float chiCost = 0f;
        public bool consumeEnergy = true;
        public int abilityPoints = 1;
        public float learnChance = 1f;
        public float efficiencyReductionPercent = 0f;
        public float upkeepEnergyCost = 0f;
        public float upkeepRegenCost = 0f;
        public float upkeepEfficiencyPercent = 0f;
        public bool shouldInitialize = true;
        public float weaponDamageFactor = 1f;
        public HediffDef abilityHediff = null;
        public ThingDef learnItem = null;
        public bool canCopy = false;
        public List<string> requiredWeaponsOrCategories = null; //Unarmed, Melee, Ranged, Bows, Rifles, Shotguns, Pistols, MagicalFoci or defName
        public int relationsAdjustment = 0;
        public bool restrictedAbility = false;

        public NeedDef requiredNeed = null;
        public float needCost = 0f;
        public float needXPFactor = 100f;
        public HediffDef requiredHediff = null;
        public float hediffCost = 0f;
        public float hediffXPFactor = 100f;
        public InspirationDef requiredInspiration = null;
        public bool requiresAnyInspiration = false;
        public bool consumesInspiration = true;

        public List<TMAbilityDef> childAbilities = new List<TMAbilityDef>();

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
