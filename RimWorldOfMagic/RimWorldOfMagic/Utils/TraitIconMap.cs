using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;

namespace TorannMagic.Utils
{
    public static class TraitIconMap
    {
        // Data for ColonistBarColonistDrawer_Patch
        private const string MageIcon = "TM_Icon_Mage";
        private const string FighterIcon = "TM_Icon_Fighter";
        // Class to hold Values for below dictionary
        public class TraitIconValue
        {
            public readonly Material IconMaterial;
            public readonly Texture2D IconTexture;
            public readonly string IconType;

            public TraitIconValue(Material iconMaterial, Texture2D iconTexture, string iconType)
            {
                IconMaterial = iconMaterial;
                IconTexture = iconTexture;
                IconType = iconType;
            }
        }
        // Dictionary that maps TraitDef to their appropriate Icon material and Icon typeCustom Classes are loaded
        // via ModOptions.ModClassOptions.InitializeCustomClassActions
        private static readonly Dictionary<ushort, TraitIconValue> TraitIconMapping = new Dictionary<ushort, TraitIconValue>()
        {
            { TorannMagicDefOf.InnerFire.index, new TraitIconValue(TM_RenderQueue.fireMarkMat, TM_MatPool.fireIcon, MageIcon) },
            { TorannMagicDefOf.HeartOfFrost.index, new TraitIconValue(TM_RenderQueue.iceMarkMat, TM_MatPool.iceIcon, MageIcon) },
            { TorannMagicDefOf.StormBorn.index, new TraitIconValue(TM_RenderQueue.lightningMarkMat, TM_MatPool.lightningIcon, MageIcon) },
            { TorannMagicDefOf.Arcanist.index, new TraitIconValue(TM_RenderQueue.arcanistMarkMat, TM_MatPool.arcanistIcon, MageIcon) },
            { TorannMagicDefOf.Paladin.index, new TraitIconValue(TM_RenderQueue.paladinMarkMat, TM_MatPool.paladinIcon, MageIcon) },
            { TorannMagicDefOf.Priest.index, new TraitIconValue(TM_RenderQueue.priestMarkMat, TM_MatPool.priestIcon, MageIcon) },
            { TorannMagicDefOf.Summoner.index, new TraitIconValue(TM_RenderQueue.summonerMarkMat, TM_MatPool.summonerIcon, MageIcon) },
            { TorannMagicDefOf.Druid.index, new TraitIconValue(TM_RenderQueue.druidMarkMat, TM_MatPool.druidIcon, MageIcon) },
            { TorannMagicDefOf.Necromancer.index, new TraitIconValue(TM_RenderQueue.necroMarkMat, TM_MatPool.necroIcon, MageIcon) },
            { TorannMagicDefOf.Lich.index, new TraitIconValue(TM_RenderQueue.necroMarkMat, TM_MatPool.necroIcon, MageIcon) },
            { TorannMagicDefOf.TM_Bard.index, new TraitIconValue(TM_RenderQueue.bardMarkMat, TM_MatPool.bardIcon, MageIcon) },
            { TorannMagicDefOf.Succubus.index, new TraitIconValue(TM_RenderQueue.demonkinMarkMat, TM_MatPool.demonkinIcon, MageIcon) },
            { TorannMagicDefOf.Warlock.index, new TraitIconValue(TM_RenderQueue.warlockMarkMat, TM_MatPool.demonkinIcon, MageIcon) },
            { TorannMagicDefOf.Geomancer.index, new TraitIconValue(TM_RenderQueue.earthMarkMat, TM_MatPool.earthIcon, MageIcon) },
            { TorannMagicDefOf.Technomancer.index, new TraitIconValue(TM_RenderQueue.technoMarkMat, TM_MatPool.technoIcon, MageIcon) },
            { TorannMagicDefOf.BloodMage.index, new TraitIconValue(TM_RenderQueue.bloodmageMarkMat, TM_MatPool.bloodmageIcon, MageIcon) },
            { TorannMagicDefOf.Enchanter.index, new TraitIconValue(TM_RenderQueue.enchanterMarkMat, TM_MatPool.enchanterIcon, MageIcon) },
            { TorannMagicDefOf.Chronomancer.index, new TraitIconValue(TM_RenderQueue.chronomancerMarkMat, TM_MatPool.chronoIcon, MageIcon) },
            { TorannMagicDefOf.Gladiator.index, new TraitIconValue(TM_RenderQueue.gladiatorMarkMat, TM_MatPool.gladiatorIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Sniper.index, new TraitIconValue(TM_RenderQueue.sniperMarkMat, TM_MatPool.sniperIcon, FighterIcon) },
            { TorannMagicDefOf.Bladedancer.index, new TraitIconValue(TM_RenderQueue.bladedancerMarkMat, TM_MatPool.bladedancerIcon, FighterIcon) },
            { TorannMagicDefOf.Ranger.index, new TraitIconValue(TM_RenderQueue.rangerMarkMat, TM_MatPool.rangerIcon, FighterIcon) },
            { TorannMagicDefOf.Faceless.index, new TraitIconValue(TM_RenderQueue.facelessMarkMat, TM_MatPool.facelessIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Psionic.index, new TraitIconValue(TM_RenderQueue.psionicMarkMat, TM_MatPool.psiIcon, FighterIcon) },
            { TorannMagicDefOf.DeathKnight.index, new TraitIconValue(TM_RenderQueue.deathknightMarkMat, TM_MatPool.deathknightIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Monk.index, new TraitIconValue(TM_RenderQueue.monkMarkMat, TM_MatPool.monkIcon, FighterIcon) },
            { TorannMagicDefOf.TM_Wanderer.index, new TraitIconValue(TM_RenderQueue.wandererMarkMat, TM_MatPool.wandererIcon, MageIcon) },
            { TorannMagicDefOf.TM_Wayfarer.index, new TraitIconValue(TM_RenderQueue.wayfarerMarkMat, TM_MatPool.wayfarerIcon, FighterIcon) },
            { TorannMagicDefOf.ChaosMage.index, new TraitIconValue(TM_RenderQueue.chaosMarkMat, TM_MatPool.chaosIcon, MageIcon) },
            { TorannMagicDefOf.TM_Commander.index, new TraitIconValue(TM_RenderQueue.commanderMarkMat, TM_MatPool.commanderIcon, FighterIcon) },
            { TorannMagicDefOf.TM_SuperSoldier.index, new TraitIconValue(TM_RenderQueue.supersoldierMarkMat, TM_MatPool.SSIcon, FighterIcon) }
        };

        public static TraitIconValue Get(TraitDef traitDef)
        {
            return TraitIconMapping[traitDef.index];
        }

        public static void Set(TraitDef traitDef, TraitIconValue traitIconValue)
        {
            TraitIconMapping[traitDef.index] = traitIconValue;
        }

        public static bool ContainsKey(TraitDef traitDef)
        {
            return TraitIconMapping.ContainsKey(traitDef.index);
        }
    }
}
