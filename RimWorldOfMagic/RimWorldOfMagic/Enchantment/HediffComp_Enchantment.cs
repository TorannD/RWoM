using Verse;
using RimWorld;
using System.Linq;
using System.Text;

namespace TorannMagic.Enchantment
{
    class HediffComp_Enchantment : HediffComp
    {
        private bool initializing = true;
        private bool removeNow = false;

        private string enchantment ="";

        CompAbilityUserMagic compMagic;
        CompAbilityUserMight compMight;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        public bool IsMagicUser
        {
            get
            {
                if(compMagic != null && compMagic.IsMagicUser)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsMightUser
        {
            get
            {
                if (compMight != null && compMight.IsMightUser)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsDualClass
        {
            get
            {
                if (IsMagicUser && IsMightUser)
                {
                    return true;
                }
                return false;
            }
        }

        public override string CompLabelInBracketsExtra => this.enchantment;

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                //FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.removeNow;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }
            if(Find.TickManager.TicksGame % 120 == 0)
            {
                compMagic = this.Pawn.GetCompAbilityUserMagic();
                compMight = this.Pawn.GetCompAbilityUserMight();
                DetermineEnchantments();
            }
            if(Find.TickManager.TicksGame % 480 == 0 && this.enchantment == "unknown")
            {
                this.removeNow = true;
            }
        }

        private void DetermineEnchantments()
        {
            if (this.parent.def.defName == "TM_HediffEnchantment_maxEnergy")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.maxMP, compMight.maxSP);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.maxMP, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.maxSP);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_coolDown")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.coolDown, compMight.coolDown);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.coolDown, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.coolDown);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_energyCost")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.mpCost, compMight.spCost);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.mpCost, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.spCost);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_energyRegen")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.mpRegenRate, compMight.spRegenRate);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.mpRegenRate, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.spRegenRate);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_xpGain")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.xpGain, compMight.xpGain);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.xpGain, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.xpGain);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_dmgResistance")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.arcaneRes, compMight.arcaneRes);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.arcaneRes, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.arcaneRes);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_dmgBonus")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.arcaneDmg, compMight.mightPwr);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.arcaneDmg, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.mightPwr);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_arcalleumCooldown")
            {
                if (IsDualClass)
                {
                    DisplayEnchantments(compMagic.arcalleumCooldown, compMight.arcalleumCooldown);
                }
                else if (IsMagicUser)
                {
                    DisplayEnchantments(compMagic.arcalleumCooldown, 1f);
                }
                else if (IsMightUser)
                {
                    DisplayEnchantments(1f, compMight.arcalleumCooldown);
                }
                else
                {
                    this.removeNow = true;
                }
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_arcaneSpectre")
            {
                this.enchantment = "TM_ArcaneSpectre".Translate();
            }
            else if (this.parent.def.defName == "TM_HediffEnchantment_phantomShift")
            {
                this.enchantment = "TM_PhantomShift".Translate();
            }
            else
            {
                Log.Message("enchantment unknkown");
                this.enchantment = "unknown";
            }           

        }

        private void DisplayEnchantments(float magVal = 1f, float mitVal = 1f)
        {
            string txtMagic = "";
            string txtMight = "";

            if (magVal != 1f)
            {
                txtMagic = (magVal * 100).ToString("0.##") + "%";
            }
            if (mitVal != 1f)
            {
                txtMight = (mitVal * 100).ToString("0.##") + "%";
            }

            if (txtMagic != "" && txtMight != "")
            {
                if (magVal != mitVal)
                { 
                    this.enchantment = txtMagic + " | " + txtMight;
                }
                else
                {
                    this.enchantment = txtMagic;
                }
            }
            else
            {
                this.enchantment = txtMagic + txtMight;
            }
            
            if(this.enchantment == "")
            {
                this.removeNow = true;
            }
        }
    }
}
