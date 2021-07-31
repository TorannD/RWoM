using System;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public static class GenEnchantmentColor
    {
        public static readonly Color Uncommon = new Color(0.12f, 1f, 0f);

        public static readonly Color Rare = new Color(0f, 0.44f, 1f);

        public static readonly Color Epic = new Color(0.64f, 0.21f, 0.93f);

        public static readonly Color Legendary = new Color(1f, 0.5f, 0f);

        public static readonly Color Artifact = new Color(0.92f, 0.84f, 0.56f);

        public static readonly Color Negative = new Color(0.8f, 0.15f, 0.15f);

        public static Color EnchantmentColor(this EnchantmentTier et)
        {
            switch (et)
            {
                case EnchantmentTier.Minor:
                    return Color.white;
                case EnchantmentTier.Standard:
                    return GenEnchantmentColor.Uncommon;
                case EnchantmentTier.Major:
                    return GenEnchantmentColor.Epic;
                case EnchantmentTier.Crafted:
                    return GenEnchantmentColor.Artifact;
                case EnchantmentTier.Skill:
                    return GenEnchantmentColor.Legendary;
                case EnchantmentTier.Negative:
                    return GenEnchantmentColor.Negative;
                default:
                    return Color.white;
            }
        }
    }
}
