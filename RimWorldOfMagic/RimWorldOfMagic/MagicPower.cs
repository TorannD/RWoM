using AbilityUser;
using System.Collections.Generic;
using Verse;

namespace TorannMagic 
{
    public class MagicPower : Power
    {
        public bool requiresScroll = false;

        public MagicPower()
        {
        }

        public MagicPower(List<AbilityDef> newAbilityDefs, bool requireScrollToLearn = false)
        {
            this.level = 0;
            this.requiresScroll = requireScrollToLearn;
            this.TMabilityDefs = newAbilityDefs;
            this.maxLevel = newAbilityDefs.Count - 1;

            if (this.abilityDef.defName == "TM_TechnoBit" || this.abilityDef.defName == "TM_TechnoTurret" || this.abilityDef.defName == "TM_TechnoWeapon")
            {
                this.learnCost = 0;
            }

            if (this.abilityDef.defName == "TM_TechnoShield" || this.abilityDef.defName == "TM_Sabotage" || this.abilityDef.defName == "TM_Overdrive")
            {
                this.learnCost = 99;
            }

            if (this.abilityDef.defName == "TM_Firebolt" || this.abilityDef.defName == "TM_Icebolt" || this.abilityDef.defName == "TM_Rainmaker" || this.abilityDef.defName == "TM_LightningBolt" ||
                this.abilityDef.defName == "TM_Blink" || this.abilityDef.defName == "TM_Summon" || this.abilityDef.defName == "TM_Heal" || this.abilityDef.defName == "TM_SummonExplosive" ||
                this.abilityDef.defName == "TM_SummonPylon" || this.abilityDef.defName == "TM_Poison" || this.abilityDef.defName == "TM_FogOfTorment" || this.abilityDef.defName == "TM_AdvancedHeal" ||
                this.abilityDef.defName == "TM_CorpseExplosion" || this.abilityDef.defName == "TM_Entertain" || this.abilityDef.defName == "TM_Encase" || this.abilityDef.defName == "TM_EarthernHammer")
            {
                this.learnCost = 1;
            }

            if(this.abilityDef.defName == "TM_Fireball" || this.abilityDef.defName == "TM_LightningStorm" || this.abilityDef.defName == "TM_SummonElemental" || this.abilityDef == TorannMagicDefOf.TM_DeathBolt ||
                this.abilityDef == TorannMagicDefOf.TM_Sunfire || this.abilityDef == TorannMagicDefOf.TM_Refraction || this.abilityDef == TorannMagicDefOf.TM_ChainLightning)
            {
                this.learnCost = 3;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.requiresScroll, "requiresScroll", false, false);
        }
    }
}
