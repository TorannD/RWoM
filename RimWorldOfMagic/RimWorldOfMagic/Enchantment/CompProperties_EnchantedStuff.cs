using System;
using Verse;
using RimWorld;
using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class CompProperties_EnchantedStuff : CompProperties
    {
        /// <summary>
        /// Stuff made out of enchanted material - material does not need to be initialized as a comp, all variables pulled from the properties
        /// Fields:
        /// enchantedStuff - must be true to be added as a component related to a CompEnchantedItem
        /// enchantedBonusMultiplier - applies an enchantement multiplier like manaweave (display in compenchanteditem, application to pawn in compabilityusermagic/might)
        /// arcalleumCooldown - must be set to true for an item to be considered in arcalleum cooldown calculations
        /// arcalleumCooldownPerMass - applies arcalleum cooldown for each kg of mass equipped
        /// appliedThoughts - linked to ThoughtWorker ???; applies thoughts to any pawn when wearing an item with this stuff
        /// setBonus - applies additional bonuses (hediffs or abilities), value must be non-null for checks
        /// {
        ///     requiredSetCount - number of items with the same type of enchanted stuff OR equipped items matching listed defNames before setBonuses are applied
        ///     - setFromStuff - indicates setCount number is determined by items using this type of stuff
        ///     - setThingDefs - a list of thing defNames that count towards the setCount, only checks if setFromStuff is false
        ///     appliedAbility - applies an ability when setCount is met
        ///     appliedHediff - applies hediff when setCount is met
        ///     - appliedHediffSeverity - severity of any hediff applied
        ///     
        /// }
        /// </summary>
        public bool isEnchanted = true;             
        public float enchantmentBonusMultiplier = 1f;
        public float arcalleumCooldownPerMass = 0f;
        public HediffDef appliedHediff = null;
        public int applyHediffAtCount = 1;
        public float severityPerCount = 0f;

        public bool apparelOnly = false;

        public float maxEnergyOffset = 0f;
        public float energyRegenOffset = 0f;
        public float cooldownOffset = 0f;
        public float energyCostOffset = 0f;
        public float xpGainOffset = 0f;
        public float arcaneResOffset = 0f;
        public float arcaneDmgOffset = 0f;

        public CompProperties_EnchantedStuff()
        {
            this.compClass = typeof(CompEnchantedStuff);
        }
    }
}
