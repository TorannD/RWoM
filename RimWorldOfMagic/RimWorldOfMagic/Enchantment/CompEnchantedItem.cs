using System;
using Verse;
using RimWorld;
using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class CompEnchantedItem : ThingComp
    {
        public List<AbilityUser.AbilityDef> MagicAbilities = new List<AbilityUser.AbilityDef>();

        public List<Trait> SoulOrbTraits = new List<Trait>();

        public CompAbilityUserMagic CompAbilityUserMagicTarget = null;

        public CompProperties_EnchantedItem Props
        {
            get
            {
                return (CompProperties_EnchantedItem)this.props;
            }
        }

        public CompProperties_EnchantedStuff EnchantedStuff
        {
            get
            {
                return this.parent.Stuff.GetCompProperties<CompProperties_EnchantedStuff>();
            }
        }

        public bool MadeFromEnchantedStuff
        {
            get
            {
                if (this.parent != null && this.parent.def.MadeFromStuff && this.parent.Stuff != null && this.parent.Stuff.GetCompProperties<CompProperties_EnchantedStuff>() != null)
                {                 
                    return EnchantedStuff.isEnchanted;
                }
                return false;
            }
        }

        public HediffDef GetEnchantedStuff_HediffDef
        {
            get
            {
                if(MadeFromEnchantedStuff && EnchantedStuff != null)
                {
                    return EnchantedStuff.appliedHediff;
                }
                return null;
            }
        }

        public Pawn WearingPawn
        {
            get
            {
                Apparel ap = this.parent as Apparel;
                if(ap != null)
                {
                    if(ap.Wearer != null)
                    {
                        return ap.Wearer;
                    }
                }
                ThingWithComps twc = this.parent as ThingWithComps;
                if(twc != null)
                {
                    Pawn_EquipmentTracker p_et = twc.ParentHolder as Pawn_EquipmentTracker;
                    if(p_et != null && p_et.pawn != null)
                    {
                        return p_et.pawn;
                    }
                }
                return null;
            }
        }

        public void GetOverlayGraphic()
        {
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Pawn pawn = this.parent as Pawn;

            if(initialized && !abilitiesInitialized)
            {
                InitializeAbilities(this.parent);
            }

            if (!initialized)
            {
                this.hasEnchantment = this.Props.hasEnchantment;
                if(!this.hasEnchantment)
                {
                    this.hasEnchantment = this.MadeFromEnchantedStuff;
                }

                this.arcaneDmg = this.Props.arcaneDmg;
                this.arcaneDmgTier = this.Props.arcaneDmgTier;
                this.arcaneRes = this.Props.arcaneRes;
                this.arcaneResTier = this.Props.arcaneResTier;

                this.maxMP = this.Props.maxMP;
                this.maxMPTier = this.Props.maxMPTier;
                this.mpRegenRate = this.Props.mpRegenRate;
                this.mpRegenRateTier = this.Props.mpRegenRateTier;
                this.coolDown = this.Props.coolDown;
                this.coolDownTier = this.Props.coolDownTier;
                this.mpCost = this.Props.mpCost;
                this.mpCostTier = this.Props.mpCostTier;
                this.xpGain = this.Props.xpGain;
                this.xpGainTier = this.Props.xpGainTier;

                if(MadeFromEnchantedStuff && this.EnchantedStuff != null)
                {
                    this.maxMP += this.EnchantedStuff.maxEnergyOffset;
                    this.mpRegenRate += this.EnchantedStuff.energyRegenOffset;
                    this.coolDown += this.EnchantedStuff.cooldownOffset;
                    this.mpCost += this.EnchantedStuff.energyCostOffset;
                    this.xpGain += this.EnchantedStuff.xpGainOffset;
                    this.arcaneRes += this.EnchantedStuff.arcaneResOffset;
                    this.arcaneDmg += this.EnchantedStuff.arcaneDmgOffset;
                }

                this.healthRegenRate = this.Props.healthRegenRate;

                this.enchantmentAction = this.Props.enchantedAction;
                this.arcaneSpectre = this.Props.arcaneSpectre;
                this.phantomShift = this.Props.phantomShift;
                this.arcalleumCooldown = this.Props.arcalleumCooldown;
                this.enchantmentThought = this.Props.enchantmentThought;

                this.skillTier = this.Props.skillTier;

                this.hediff = this.Props.hediff;
                this.hediffSeverity = this.Props.hediffSeverity;

                if (this.parent.def.tickerType == TickerType.Rare)
                {
                    Find.TickManager.RegisterAllTickabilityFor(this.parent);
                }

                if(this.parent.def.tickerType == TickerType.Never)
                {
                    this.parent.def.tickerType = TickerType.Rare;
                    Find.TickManager.RegisterAllTickabilityFor(this.parent);
                }

                if(this.Props.hasAbility && !abilitiesInitialized)
                {
                    InitializeAbilities(this.parent as Apparel);
                }

                if(this.parent.def == TorannMagicDefOf.TM_MagicArtifact_MagicEssence && this.magicEssence == 0)
                {
                    this.magicEssence = Rand.Range(200, 500);
                }
                if(this.parent.def == TorannMagicDefOf.TM_MagicArtifact_MightEssence && this.mightEssence == 0)
                {
                    this.mightEssence = Rand.Range(200, 500);
                }

                this.initialized = true;
            }
        }        

        private void InitializeAbilities(ThingWithComps abilityThing)
        {
            if (abilityThing is Apparel abilityApparel)
            {
                if (abilityApparel.Wearer != null)
                {
                    CompAbilityItem comp = abilityApparel.TryGetComp<CompAbilityItem>();
                    if (comp != null)
                    {
                        comp.Notify_Equipped(abilityApparel.Wearer);
                    }
                    //AbilityUserMod.Notify_ApparelRemoved_PostFix(abilityApparel.Wearer.apparel, abilityApparel);
                    //AbilityUserMod.Notify_ApparelAdded_PostFix(abilityApparel.Wearer.apparel, abilityApparel);
                    this.abilitiesInitialized = true;
                }
            }
            else
            {
                if (abilityThing != null)
                {
                    CompAbilityItem comp = abilityThing.TryGetComp<CompAbilityItem>();
                    if (comp != null)
                    {
                        comp.Notify_Equipped(WearingPawn);
                    }
                    this.abilitiesInitialized = true;
                }
            }
            
        }

        public override void CompTickRare()
        {
            if (this.hediff != null)
            {
                Apparel artifact = this.parent as Apparel;                
                if (artifact != null)
                {
                    if (artifact.Wearer != null)
                    {                       
                        //Log.Message("" + artifact.LabelShort + " has holding owner " + artifact.Wearer.LabelShort);
                        if (artifact.Wearer.health.hediffSet.GetFirstHediffOfDef(hediff, false) != null)
                        {

                        }
                        else
                        {                            
                            HealthUtility.AdjustSeverity(artifact.Wearer, hediff, hediffSeverity);
                            HediffComp_EnchantedItem hdc = artifact.Wearer.health.hediffSet.GetFirstHediffOfDef(hediff, false).TryGetComp<HediffComp_EnchantedItem>();
                            if (hdc != null)
                            {
                                hdc.enchantedItem = artifact;
                            }
                            //HediffComp_EnchantedItem comp = diff.TryGetComp<HediffComp_EnchantedItem>();
                        }
                    }
                }
                Thing primary = this.parent as Thing;
                if (primary != null && primary.ParentHolder is Pawn_EquipmentTracker pet)
                {
                    if (pet.pawn != null && pet.pawn.equipment != null && pet.pawn.equipment.Primary == primary)
                    {
                        if (pet.pawn.health.hediffSet.GetFirstHediffOfDef(hediff, false) != null)
                        {

                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(pet.pawn, hediff, hediffSeverity);
                            HediffComp_EnchantedItem hdc = pet.pawn.health.hediffSet.GetFirstHediffOfDef(hediff, false).TryGetComp<HediffComp_EnchantedItem>();
                            if (hdc != null)
                            {
                                hdc.enchantedWeapon = primary;
                            }
                        }
                    }
                }                
            }
            if (this.Props.hasAbility && !this.abilitiesInitialized)
            {
                Apparel artifact = this.parent as Apparel;
                if (artifact != null)
                {
                    if (artifact.Wearer != null)
                    {
                        //Log.Message("" + artifact.LabelShort + " has holding owner " + artifact.Wearer.LabelShort);
                        this.InitializeAbilities(artifact);                        
                    }

                    this.MagicAbilities = artifact.GetComp<CompAbilityItem>().Props.Abilities;
                    //this.MagicAbilities = new List<AbilityDef>();
                    //this.MagicAbilities.Clear();
                    // abilities;
                }
                ThingWithComps primary = this.parent as ThingWithComps;
                if (primary != null && primary.ParentHolder is Pawn_EquipmentTracker pet)
                {
                    if (pet.pawn != null && pet.pawn.equipment != null && pet.pawn.equipment.Primary == primary)
                    {
                        this.InitializeAbilities(primary);
                    }
                    this.MagicAbilities = primary.GetComp<CompAbilityItem>().Props.Abilities;
                }
            }
            if (GetEnchantedStuff_HediffDef != null)
            {
                if (WearingPawn != null)
                {
                    hediffStuff.Clear();
                    List<Apparel> wornApparel = WearingPawn.apparel.WornApparel;
                    for (int i = 0; i < wornApparel.Count; i++)
                    {
                        CompEnchantedItem itemComp = wornApparel[i].TryGetComp<CompEnchantedItem>();
                        if(itemComp != null && itemComp.GetEnchantedStuff_HediffDef != null)
                        {
                            int hdCount = GetStuffCount_Hediff(itemComp.EnchantedStuff.appliedHediff);
                            if (hdCount >= itemComp.EnchantedStuff.applyHediffAtCount)
                            {
                                if(WearingPawn.health.hediffSet.HasHediff(itemComp.EnchantedStuff.appliedHediff))
                                {
                                    Hediff hd = WearingPawn.health.hediffSet.GetFirstHediffOfDef(itemComp.EnchantedStuff.appliedHediff);
                                    if(hd.Severity < (hdCount * itemComp.EnchantedStuff.severityPerCount))
                                    {
                                        WearingPawn.health.RemoveHediff(hd);
                                        HealthUtility.AdjustSeverity(WearingPawn, itemComp.EnchantedStuff.appliedHediff, hdCount * itemComp.EnchantedStuff.severityPerCount);
                                    }
                                }
                                else
                                {
                                    HealthUtility.AdjustSeverity(WearingPawn, itemComp.EnchantedStuff.appliedHediff, hdCount * itemComp.EnchantedStuff.severityPerCount);
                                }
                            }
                        }
                    }
                    if (WearingPawn.equipment != null && WearingPawn.equipment.Primary != null && !EnchantedStuff.apparelOnly)
                    {
                        ThingWithComps eq = WearingPawn.equipment.Primary;
                        CompEnchantedItem itemComp = eq.TryGetComp<CompEnchantedItem>();
                        if (itemComp != null && itemComp.GetEnchantedStuff_HediffDef != null)
                        {
                            int hdCount = GetStuffCount_Hediff(itemComp.EnchantedStuff.appliedHediff);
                            if (hdCount >= itemComp.EnchantedStuff.applyHediffAtCount)
                            {
                                if (WearingPawn.health.hediffSet.HasHediff(itemComp.EnchantedStuff.appliedHediff))
                                {
                                    Hediff hd = WearingPawn.health.hediffSet.GetFirstHediffOfDef(itemComp.EnchantedStuff.appliedHediff);
                                    if (hd.Severity < (hdCount * itemComp.EnchantedStuff.severityPerCount))
                                    {
                                        WearingPawn.health.RemoveHediff(hd);
                                        HealthUtility.AdjustSeverity(WearingPawn, itemComp.EnchantedStuff.appliedHediff, hdCount * itemComp.EnchantedStuff.severityPerCount);
                                    }
                                }
                                else
                                {
                                    HealthUtility.AdjustSeverity(WearingPawn, itemComp.EnchantedStuff.appliedHediff, hdCount * itemComp.EnchantedStuff.severityPerCount);
                                }
                            }
                        }
                    }

                }
            }
            base.CompTickRare();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            bool flag = this.parent.def.tickerType == TickerType.Never;
            if (flag)
            {
                //this.parent.def.tickerType = TickerType.Rare;
                //Find.TickManager.RegisterAllTickabilityFor(this.parent);
            }
            base.PostSpawnSetup(respawningAfterLoad);
            
        }

        private Dictionary<HediffDef, int> hediffStuff = new Dictionary<HediffDef, int>();
        public int GetStuffCount_Hediff(HediffDef hd)
        {
            if (!hediffStuff.ContainsKey(hd))
            {
                hediffStuff.Add(hd, 1);
            }
            else
            {
                int count = 0;
                hediffStuff.TryGetValue(hd, out count);
                if(count != 0)
                {
                    hediffStuff.SetOrAdd(hd, count + 1);
                }
            }
            return hediffStuff[hd];
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.maxMP, "maxMP", 0, false);
            Scribe_Values.Look<float>(ref this.mpRegenRate, "mpRegenRateP", 0, false);
            Scribe_Values.Look<float>(ref this.coolDown, "coolDown", 0, false);
            Scribe_Values.Look<float>(ref this.mpCost, "mpCost", 0, false);
            Scribe_Values.Look<float>(ref this.xpGain, "xpGain", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneRes, "arcaneRes", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 0, false);
            Scribe_Values.Look<float>(ref this.necroticEnergy, "necroticEnergy", 0f, false);
            Scribe_Values.Look<bool>(ref this.arcaneSpectre, "arcaneSpectre", false, false);
            Scribe_Values.Look<bool>(ref this.phantomShift, "phantomShift", false, false);
            //Scribe_Deep.Look<EnchantmentAction>(ref this.enchantmentAction, "enchantmentAction", new object[0]);
            Scribe_Defs.Look<ThoughtDef>(ref this.enchantmentThought, "enchantmentThought");
            Scribe_Values.Look<float>(ref this.arcalleumCooldown, "arcalleumCooldown", 0f, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.maxMPTier, "maxMPTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.mpRegenRateTier, "mpRegenRateTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.coolDownTier, "coolDownTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.mpCostTier, "mpCostTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.xpGainTier, "xpGainTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.arcaneResTier, "arcaneResTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<EnchantmentTier>(ref this.arcaneDmgTier, "arcaneDmgTier", (EnchantmentTier)0, false);
            Scribe_Values.Look<bool>(ref this.hasEnchantment, "hasEnchantment", false, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Collections.Look<Trait>(ref this.SoulOrbTraits, "SoulOrbTraits", LookMode.Deep);
            Scribe_Values.Look<int>(ref this.mightEssence, "mightEssence", 0, false);
            Scribe_Values.Look<int>(ref this.magicEssence, "magicEssence", 0, false);
            //this.Props.ExposeData();
        }

        public override string GetDescriptionPart()
        {
            string text = string.Empty;
            bool flag = this.Props.MagicAbilities.Count == 1;
            if (flag)
            {
                text += "Item Ability:";
            }
            else
            {
                bool flag2 = this.Props.MagicAbilities.Count > 1;
                if (flag2)
                {
                    text += "Item Abilities:";
                }
            }
            foreach (TMAbilityDef current in this.Props.MagicAbilities)
            {
                text += "\n\n";
                text = text + current.label.CapitalizeFirst() + " - ";
                text += current.GetDescription();
            }
            bool flag3 = this.SoulOrbTraits != null && this.SoulOrbTraits.Count > 0;
            if (flag3)
            {
                text += "Absorbed Traits:";
                foreach (Trait current in this.SoulOrbTraits)
                {
                    text += "\n";
                    text = text + current.LabelCap;
                }

            }
            bool flag4 = this.necroticEnergy != 0;
            if(flag4)
            {
                text += "Necrotic Energy: " + this.NecroticEnergy.ToString("N1");
            }
            bool flag5 = this.mightEssence != 0;
            if(flag5)
            {
                text += "Might Essence: " + this.mightEssence;
            }
            bool flag6 = this.magicEssence != 0;
            if (flag6)
            {
                text += "Magic Essence: " + this.magicEssence;
            }
            return text;
        }

        private bool initialized = false;
        private bool abilitiesInitialized = false;
        private bool hasEnchantment = false;

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

        //Might Stats (%)

        //Common Stats (%)        

        public float healthRegenRate = 0;
        private float necroticEnergy = 0f;

        //Special Abilities
        public EnchantmentAction enchantmentAction = null;
        public EnchantmentTier skillTier = EnchantmentTier.Skill;
        public bool arcaneSpectre = false;
        public bool phantomShift = false;

        public float arcalleumCooldown = 0f;

        public int mightEssence = 0;
        public int magicEssence = 0;

        //Hediffs
        public HediffDef hediff = null;
        public float hediffSeverity = 0f;

        //Abilities

        //Thoughts
        public ThoughtDef enchantmentThought = null;

        public float NecroticEnergy
        {
            get
            {
                return Mathf.Clamp(this.necroticEnergy, 0f, 100f);
            }
            set
            {
                this.necroticEnergy = Mathf.Clamp(value, 0f, 100f);
            }
        }

        private float StuffMultiplier
        {
            get
            {
                //if(this.parent.Stuff != null && this.parent.Stuff.defName == "TM_Manaweave")
                //{
                //    return 120f;
                //}
                if(this.parent.Stuff != null && MadeFromEnchantedStuff && EnchantedStuff.enchantmentBonusMultiplier != 1f)
                {
                    return 100f * EnchantedStuff.enchantmentBonusMultiplier;
                }
                else
                {
                    return 100f;
                }
            }
        }

        public string MaxMPLabel
        {
            get
            {
                return "TM_MaxMPLabel".Translate(
                    this.maxMP * StuffMultiplier
                );
            }
        }

        public string MPRegenRateLabel
        {
            get
            {
                return "TM_MPRegenRateLabel".Translate(
                    this.mpRegenRate * StuffMultiplier
                );
            }
        }

        public string CoolDownLabel
        {
            get
            {
                return "TM_CoolDownLabel".Translate(
                    this.coolDown * StuffMultiplier
                );
            }
        }

        public string MPCostLabel
        {
            get
            {
                return "TM_MPCostLabel".Translate(
                    this.mpCost * StuffMultiplier
                );
            }
        }

        public string XPGainLabel
        {
            get
            {
                return "TM_XPGainLabel".Translate(
                    this.xpGain * StuffMultiplier
                );
            }
        }

        public string ArcaneResLabel
        {
            get
            {
                return "TM_ArcaneResLabel".Translate(
                    this.arcaneRes * StuffMultiplier
                );
            }
        }

        public string ArcaneDmgLabel
        {
            get
            {
                return "TM_ArcaneDmgLabel".Translate(
                    this.arcaneDmg * StuffMultiplier
                );
            }
        }

        public string ArcaneSpectreLabel
        {
            get
            {
                return "TM_ArcaneSpectre".Translate();
            }
        }

        public string PhantomShiftLabel
        {
            get
            {
                return "TM_PhantomShift".Translate();
            }
        }

        public string ArcalleumCooldownLabel
        {
            get
            {
                return "TM_ArcalleumCooldown".Translate(
                    this.arcalleumCooldown);
            }
        }

        public string EnchantmentActionLabel
        {
            get
            {
                if (enchantmentAction.type == EnchantmentActionType.ApplyHediff)
                {
                    return "TM_EA_Hediff".Translate(
                        this.enchantmentAction.actionLabel,
                        this.enchantmentAction.hediffDef.label,
                        this.enchantmentAction.hediffChance * 100f);
                }
                if(enchantmentAction.type == EnchantmentActionType.ApplyDamage)
                {
                    return "TM_EA_Damage".Translate(
                        this.enchantmentAction.actionLabel,
                        this.enchantmentAction.damageAmount - this.enchantmentAction.damageVariation,
                        this.enchantmentAction.damageAmount + this.enchantmentAction.damageVariation,
                        this.enchantmentAction.damageDef.label,
                        this.enchantmentAction.damageChance * 100f);
                }
                return "";
            }
        }

        public string HediffLabel
        {
            get
            {
                return this.hediff.LabelCap;
            }
        }

        public bool HasMagic
        {
            get
            {
                return MagicAbilities.Count > 0;
            }
        }

        public EnchantmentTier SetTier(float mod)
        {
            if (mod < 0)
            {
                return EnchantmentTier.Negative;
            }
            if (mod <= .05f)
            {
                return EnchantmentTier.Minor;
            }
            else if (mod <= .1f)
            {
                return EnchantmentTier.Standard;
            }
            else if (mod <= .15f)
            {
                return EnchantmentTier.Major;
            }
            else if (mod > .15f)
            {
                return EnchantmentTier.Crafted;
            }
            else
            {
                return EnchantmentTier.Undefined;
            }
        }

        public bool HasEnchantment
        {
            get
            {
                return hasEnchantment;
            }
            set
            {
                hasEnchantment = value;
            }
        }        
    }
}
