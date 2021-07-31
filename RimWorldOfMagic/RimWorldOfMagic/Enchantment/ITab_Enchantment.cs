using RimWorld;
using System;
using System.Text;
using UnityEngine;
using Verse;

namespace TorannMagic.Enchantment
{
    [StaticConstructorOnStartup]
    internal class ITab_Enchantment : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(400f, 550f);

        private static CompEnchantedItem SelectedCompEnchantment
        {
            get
            {
                Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
                if (singleSelectedThing != null)
                {
                    return ThingCompUtility.TryGetComp<CompEnchantedItem>(singleSelectedThing);
                }
                return null;
            }
        }

        public override bool IsVisible
        {
            get
            {
                return ITab_Enchantment.SelectedCompEnchantment != null && ITab_Enchantment.SelectedCompEnchantment.HasEnchantment;  
            }
        }

        public ITab_Enchantment()
        {
            this.size = ITab_Enchantment.WinSize;
            this.labelKey = "TabEnchantment";
        }

        protected override void FillTab()
        {
            CompEnchantedItem enchantedItem = ThingCompUtility.TryGetComp<CompEnchantedItem>(Find.Selector.SingleSelectedThing);
            float enchantmentMultiplier = 1f;
            if (Find.Selector.SingleSelectedThing.Stuff != null && Find.Selector.SingleSelectedThing.Stuff.defName == "TM_Manaweave")
            {
                enchantmentMultiplier = 1.2f;
            }
            Rect rect = GenUI.ContractedBy(new Rect(0f, 0f, ITab_Enchantment.WinSize.x, ITab_Enchantment.WinSize.y), 10f);
            Rect rect2 = rect;
            Text.Font = GameFont.Small;
            string rectLabel = "Enchantments:"; 
            Widgets.Label(rect2, rectLabel);
            int num = 2;
            Text.Font = GameFont.Tiny;
            Rect rect3 = GetRowRect(rect2, num);            
            if(enchantedItem.maxMP !=0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.maxMPTier);
                rectLabel = enchantedItem.MaxMPLabel; 
                Widgets.Label(rect3, rectLabel);
                num++;
            }
            Rect rect4 = GetRowRect(rect3, num);
            if (enchantedItem.mpCost != 0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.mpCostTier);
                rectLabel = enchantedItem.MPCostLabel;
                Widgets.Label(rect4, rectLabel);
                num++;
            }
            Rect rect5 = GetRowRect(rect4, num);
            if (enchantedItem.mpRegenRate != 0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.mpRegenRateTier);
                rectLabel = enchantedItem.MPRegenRateLabel;
                Widgets.Label(rect5, rectLabel);
                num++;
            }
            Rect rect6 = GetRowRect(rect5,  num);
            if (enchantedItem.coolDown != 0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.coolDownTier);
                rectLabel = enchantedItem.CoolDownLabel;
                Widgets.Label(rect6, rectLabel);
                num++;
            }
            Rect rect7 = GetRowRect(rect6,  num);
            if (enchantedItem.xpGain != 0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.xpGainTier);
                rectLabel = enchantedItem.XPGainLabel;
                Widgets.Label(rect7, rectLabel);
                num++;
            }
            Rect rect71 = GetRowRect(rect7, num);
            if (enchantedItem.arcaneRes != 0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.arcaneResTier);
                rectLabel = enchantedItem.ArcaneResLabel;
                Widgets.Label(rect71, rectLabel);
                num++;
            }
            Rect rect72 = GetRowRect(rect71, num);
            if (enchantedItem.arcaneDmg != 0)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.arcaneDmgTier);
                rectLabel = enchantedItem.ArcaneDmgLabel;
                Widgets.Label(rect72, rectLabel);
                num++;
            }
            Rect rect8 = GetRowRect(rect72, num);
            if (enchantedItem.arcaneSpectre != false)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                rectLabel = enchantedItem.ArcaneSpectreLabel;
                Widgets.Label(rect8, rectLabel);
                num++;
            }
            Rect rect9 = GetRowRect(rect8, num);
            if (enchantedItem.phantomShift != false)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                rectLabel = enchantedItem.PhantomShiftLabel;
                Widgets.Label(rect9, rectLabel);
                num++;
            }
            Rect rect10 = GetRowRect(rect9, num);
            if (enchantedItem.hediff != null)
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                rectLabel = enchantedItem.HediffLabel;
                Widgets.Label(rect10, rectLabel);
                num++;
            }
            Rect rect11 = GetRowRect(rect10, num);
            if (enchantedItem.MagicAbilities != null && enchantedItem.MagicAbilities.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                string abilityLabels = "Abilities: ";
                stringBuilder.Append(abilityLabels);
                for (int i = 0; i < enchantedItem.MagicAbilities.Count; i++)
                {
                    if (i + 1 < enchantedItem.MagicAbilities.Count)
                    {
                        stringBuilder.Append(enchantedItem.MagicAbilities[i].LabelCap + ", ");
                    }
                    else
                    {
                        stringBuilder.Append(enchantedItem.MagicAbilities[i].LabelCap);
                    }                    
                }
                rectLabel = stringBuilder.ToString();
                Widgets.Label(rect11, rectLabel);
                num++;
            }
            Rect rect12 = GetRowRect(rect11, num);
            if (enchantedItem.SoulOrbTraits != null && enchantedItem.SoulOrbTraits.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                string abilityLabels = "Absorbed Traits: ";
                stringBuilder.Append(abilityLabels);
                for (int i = 0; i < enchantedItem.SoulOrbTraits.Count; i++)
                {
                    //abilityLabels = enchantedItem.SoulOrbTraits[i].LabelCap + "\n";
                    if (i + 1 < enchantedItem.SoulOrbTraits.Count)
                    {
                        stringBuilder.Append(enchantedItem.SoulOrbTraits[i].LabelCap + ", ");
                    }
                    else
                    {
                        stringBuilder.Append(enchantedItem.SoulOrbTraits[i].LabelCap);
                    }
                }
                rectLabel = stringBuilder.ToString();
                Widgets.Label(rect12, rectLabel);
                num++;
            }
            Rect rect13 = GetRowRect(rect12, num);
            if (enchantedItem.enchantmentAction != null && (enchantedItem.enchantmentAction.type != EnchantmentActionType.Null))
            {
                GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                rectLabel = enchantedItem.EnchantmentActionLabel;
                Widgets.Label(rect13, rectLabel);
                num++;
            }
            //rect3.yMin += Text.CalcHeight(rectLabel, rect.width);

            //QualityCategory qualityCategory;
            //QualityUtility.TryGetQuality(ITab_Enchantment.SelectedCompEnchantment.parent, out qualityCategory);
            //StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.Append(GenText.CapitalizeFirst(QualityUtility.GetLabel(qualityCategory))).Append(" ").Append(ResourceBank.StringQuality).Append(" ");
            //if (ITab_Enchantment.SelectedCompEnchantment.parent.Stuff != null)
            //{
            //    stringBuilder.Append(ITab_Enchantment.SelectedCompEnchantment.parent.Stuff.LabelAsStuff).Append(" ");
            //}
            //stringBuilder.Append(ITab_Enchantment.SelectedCompEnchantment.parent.def.label);
            //string text = stringBuilder.ToString();
            //Widgets.Label(rect3, text);
            //GUI.color = Color.white;
            //Text.Anchor = TextAnchor.UpperLeft;
            //Rect rect4 = rect;
            //rect4.yMin += rect3.yMin + Text.CalcHeight(text, rect.width);
            //Text.Font = GameFont.Tiny;
            //Widgets.Label(rect4, ITab_Enchantment.SelectedCompEnchantment.parent.GetInfusionDesc());
        }

        //private static string GetRectLabel()
        //{
        //    InfusionSet infusions = ITab_Enchantment.SelectedCompInfusion.Infusions;
        //    EnchantmentDef enchantment = infusions.enchantment;

        //    Color color;
        //    color = MathUtility.Max(enchantment.tier, 0).InfusionColor();
        //    GUI.color = color;
        //    return GenText.CapitalizeFirst(ITab_Enchantment.SelectedCompInfusion.parent.GetInfusedLabel(true, true));
        //}

        public static Rect GetRowRect(Rect inRect, int row)
        {
            float y = 20f * (float)row;
            Rect result = new Rect(inRect.x, y, inRect.width, 18f);
            return result;
        }
    }
}
