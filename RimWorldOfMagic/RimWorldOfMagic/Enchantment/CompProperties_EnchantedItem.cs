using System;
using Verse;
using RimWorld;
using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class CompProperties_EnchantedItem : CompProperties, IExposable
    {
        public List<TMAbilityDef> MagicAbilities = new List<TMAbilityDef>();

        public Type AbilityUserClass;

        public bool hasEnchantment = false;
        public bool hasAbility = false;

        public EnchantmentTier maxMPTier;
        public EnchantmentTier mpRegenRateTier;
        public EnchantmentTier coolDownTier;
        public EnchantmentTier mpCostTier;
        public EnchantmentTier xpGainTier;
        public EnchantmentTier arcaneResTier;
        public EnchantmentTier arcaneDmgTier;

        //Magic Stats (%)
        public float maxMP = 0;
        public float mpRegenRate = 0;
        public float coolDown = 0;
        public float mpCost = 0;
        public float xpGain = 0;

        public float arcaneRes = 0;
        public float arcaneDmg = 0;

        public float arcalleumCooldown = 0f;

        //Might Stats (%)

        //Common Stats (%)        

        public float healthRegenRate = 0;

        //Special Abilities
        public EnchantmentTier skillTier = EnchantmentTier.Skill;
        public bool arcaneSpectre = false;
        public bool phantomShift = false;

        public EnchantmentAction enchantedAction = new EnchantmentAction();

        //Hediffs
        public HediffDef hediff = null;
        public float hediffSeverity = 0f;

        //Thoughts
        public ThoughtDef enchantmentThought = null;

        public void ExposeData()
        {
            Scribe_Values.Look<float>(ref this.maxMP, "maxMP", 0, false);
            Scribe_Values.Look<float>(ref this.mpRegenRate, "mpRegenRateP", 0, false);
            Scribe_Values.Look<float>(ref this.coolDown, "coolDown", 0, false);
            Scribe_Values.Look<float>(ref this.mpCost, "mpCost", 0, false);
            Scribe_Values.Look<float>(ref this.xpGain, "xpGain", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneRes, "arcaneRes", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 0, false);
            Scribe_Values.Look<bool>(ref this.arcaneSpectre, "arcaneSpectre", false, false);
            Scribe_Values.Look<bool>(ref this.phantomShift, "phantomShift", false, false);
            Scribe_Values.Look<float>(ref this.arcalleumCooldown, "arcalleumCooldown", 0f, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.maxMPTier, "maxMPTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.mpRegenRateTier, "mpRegenRateTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.coolDownTier, "coolDownTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.mpCostTier, "mpCostTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.xpGainTier, "xpGainTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.arcaneResTier, "arcaneResTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.arcaneDmgTier, "arcaneDmgTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<bool>(ref this.hasEnchantment, "hasEnchantment", false, false);
        }

        public CompProperties_EnchantedItem()
        {
            this.compClass = typeof(CompEnchantedItem);
            this.AbilityUserClass = typeof(GenericCompAbilityUser);
        }
    }
}
