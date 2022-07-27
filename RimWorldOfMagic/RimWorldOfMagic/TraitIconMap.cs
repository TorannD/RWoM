using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    public static class TraitIconMap
    {
        // Data for ColonistBarColonistDrawer_Patch
        private const string MageIcon = "TM_Icon_Mage";
        private const string FighterIcon = "TM_Icon_Fighter";
        // Class to hold Values for below dictionary
        public class TraitIconValue
        {
            public readonly Texture2D IconMaterial;
            public readonly string IconType;

            public TraitIconValue(Texture2D iconMaterial, string iconType)
            {
                IconMaterial = iconMaterial;
                IconType = iconType;
            }
        }
        // Dictionary that maps TraitDef to their appropriate Icon material and Icon type. Custom Classes are loaded
        // via ModOptions.ModClassOptions.InitializeCustomClassActions
        private static readonly Dictionary<TraitDef, TraitIconValue> TraitIconMapping = new Dictionary<TraitDef, TraitIconValue>()
        {
            { TorannMagicDefOf.InnerFire, new TraitIconValue(TM_MatPool.fireIcon, MageIcon) },
            { TorannMagicDefOf.HeartOfFrost, new TraitIconValue(TM_MatPool.iceIcon, MageIcon) },
            { TorannMagicDefOf.StormBorn, new TraitIconValue(TM_MatPool.lightningIcon, MageIcon) },
            { TorannMagicDefOf.Arcanist, new TraitIconValue(TM_MatPool.arcanistIcon, MageIcon) },
            { TorannMagicDefOf.Paladin, new TraitIconValue(TM_MatPool.paladinIcon, MageIcon) },
            { TorannMagicDefOf.Summoner, new TraitIconValue(TM_MatPool.summonerIcon, MageIcon) },
            { TorannMagicDefOf.Druid, new TraitIconValue(TM_MatPool.druidIcon, MageIcon) },
            { TorannMagicDefOf.Necromancer, new TraitIconValue(TM_MatPool.necroIcon, MageIcon) },
            { TorannMagicDefOf.Lich, new TraitIconValue(TM_MatPool.necroIcon, MageIcon) },
            { TorannMagicDefOf.TM_Bard, new TraitIconValue(TM_MatPool.bardIcon, MageIcon) },
            { TorannMagicDefOf.Succubus, new TraitIconValue(TM_MatPool.demonkinIcon, MageIcon) },
            { TorannMagicDefOf.Warlock, new TraitIconValue(TM_MatPool.demonkinIcon, MageIcon) },
            { TorannMagicDefOf.Geomancer, new TraitIconValue(TM_MatPool.earthIcon, MageIcon) },
            { TorannMagicDefOf.Technomancer, new TraitIconValue(TM_MatPool.technoIcon, MageIcon) },
            { TorannMagicDefOf.BloodMage, new TraitIconValue(TM_MatPool.bloodmageIcon, MageIcon) },
            { TorannMagicDefOf.Enchanter, new TraitIconValue(TM_MatPool.enchanterIcon, MageIcon) },
            { TorannMagicDefOf.Chronomancer, new TraitIconValue(TM_MatPool.chronoIcon, MageIcon) },
            { TorannMagicDefOf.Gladiator, new TraitIconValue(TM_MatPool.gladiatorIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Sniper, new TraitIconValue(TM_MatPool.sniperIcon, FighterIcon) },
            { TorannMagicDefOf.Bladedancer, new TraitIconValue(TM_MatPool.bladedancerIcon, FighterIcon) },
            { TorannMagicDefOf.Ranger, new TraitIconValue(TM_MatPool.rangerIcon, FighterIcon) },
            { TorannMagicDefOf.Faceless, new TraitIconValue(TM_MatPool.facelessIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Psionic, new TraitIconValue(TM_MatPool.psiIcon, FighterIcon) },
            { TorannMagicDefOf.DeathKnight, new TraitIconValue(TM_MatPool.deathknightIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Monk, new TraitIconValue(TM_MatPool.monkIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Wanderer, new TraitIconValue(TM_MatPool.wandererIcon, MageIcon) },
            { TorannMagicDefOf.TM_Wayfarer, new TraitIconValue(TM_MatPool.wayfarerIcon, FighterIcon) },
            { TorannMagicDefOf.ChaosMage, new TraitIconValue(TM_MatPool.chaosIcon, MageIcon) },
            { TorannMagicDefOf.TM_Commander, new TraitIconValue(TM_MatPool.commanderIcon, FighterIcon) },
            { TorannMagicDefOf.TM_SuperSoldier, new TraitIconValue(TM_MatPool.SSIcon, FighterIcon) }
        };

        public static TraitIconValue Get(TraitDef traitDef)
        {
            return TraitIconMapping[traitDef];
        }

        public static void Set(TraitDef traitDef, TraitIconValue traitIconValue)
        {
            TraitIconMapping[traitDef] = traitIconValue;
        }

        public static bool ContainsKey(TraitDef traitDef)
        {
            return TraitIconMapping.ContainsKey(traitDef);
        }
    }
}