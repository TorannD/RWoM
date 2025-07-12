using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public static class TM_RenderQueue
    {
        public static readonly Texture2D losIcon = ContentFinder<Texture2D>.Get("Other/cantsee");

        public static readonly Material bitMat = MaterialPool.MatFrom("Other/bit");
        public static readonly Material mageLightMat = MaterialPool.MatFrom("Other/magelight");

        public static readonly Material scornWingsNS = MaterialPool.MatFrom("Other/scornwings_north");
        public static readonly Material scornWingsE = MaterialPool.MatFrom("Other/scornwings_east");
        public static readonly Material scornWingsW = MaterialPool.MatFrom("Other/scornwings_west");
        //public static readonly Material scornWingsW = MaterialPool.MatFrom("Other/angelwings_west", ShaderDatabase.Transparent, HediffComp_Shield.shieldColor);

        public static readonly Material mc_north = MaterialPool.MatFrom("Items/magic_circle_north", ShaderDatabase.MoteGlow, Color.white);
        public static readonly Material mc_south = MaterialPool.MatFrom("Items/magic_circle_south", ShaderDatabase.MoteGlow, Color.white);
        public static readonly Material mc_east = MaterialPool.MatFrom("Items/magic_circle_east", ShaderDatabase.MoteGlow, Color.white);
        public static readonly Material mc_west = MaterialPool.MatFrom("Items/magic_circle_west",ShaderDatabase.MoteGlow, Color.white);

        public static readonly Material smc = MaterialPool.MatFrom("Items/small_circle_centered", ShaderDatabase.MoteGlow, Color.white);

        //Magic
        public static readonly Material enchantMark = MaterialPool.MatFrom("Items/Gemstones/arcane_minor");

        public static readonly Color redShieldColor = new Color(90f, 0f, 0f);
        public static readonly Color whiteShieldColor = new Color(1f, 1f, 1f);
        public static readonly Material whiteShieldMat = MaterialPool.MatFrom("Other/Shield", ShaderDatabase.Transparent, TM_RenderQueue.whiteShieldColor);
        public static readonly Material redShieldMat = MaterialPool.MatFrom("Other/Shield", ShaderDatabase.Transparent, TM_RenderQueue.redShieldColor);

        public static readonly Color burningFuryColor = new Color(1f, .4f, .25f);
        public static readonly Material burningFuryMat = MaterialPool.MatFrom("Other/Shield", ShaderDatabase.Transparent, TM_RenderQueue.burningFuryColor);

        public static readonly Color manaShieldColor = new Color(127f, 0f, 255f);
        public static readonly Material manaShieldMat = MaterialPool.MatFrom("Other/Shield", ShaderDatabase.Transparent, TM_RenderQueue.manaShieldColor);

        public static readonly Color demonShieldColor = new Color(150f, 0f, 75f);
        public static readonly Material demonShieldMat = MaterialPool.MatFrom("Other/Shield", ShaderDatabase.Transparent, TM_RenderQueue.demonShieldColor);

        public static readonly Material wandererMarkMat = MaterialPool.MatFrom("Other/wandererFlame");
        public static readonly Material wayfarerMarkMat = MaterialPool.MatFrom("Other/wayfarerFlame");

        public static readonly Material mageMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.black);
        public static readonly Color arcanistMarkColor = new Color(1, 0, .5f);
        public static readonly Material arcanistMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.arcanistMarkColor);
        public static readonly Color necroMarkColor = new Color(.4f, .5f, .25f);
        public static readonly Material necroMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.necroMarkColor);
        public static readonly Color summonerMarkColor = new Color(.8f, .4f, .0f);
        public static readonly Material summonerMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.summonerMarkColor);
        public static readonly Material druidMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.green);
        public static readonly Material paladinMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.white);
        public static readonly Material warlockMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.magenta);
        public static readonly Material lightningMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.yellow);
        public static readonly Material iceMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.blue);
        public static readonly Material fireMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, Color.red);
        public static readonly Color priestMarkColor = new Color(1f, 1f, .55f);
        public static readonly Material priestMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.priestMarkColor);
        public static readonly Color bardMarkColor = new Color(.8f, .8f, 0f);
        public static readonly Material bardMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.bardMarkColor);
        public static readonly Color demonkinMarkColor = new Color(.6f, 0, .25f);
        public static readonly Material demonkinMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.demonkinMarkColor);
        public static readonly Color earthMarkColor = new Color(.4f, .2f, 0f);
        public static readonly Material earthMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.earthMarkColor);
        public static readonly Color technoMarkColor = new Color(0, .8f, 0f);
        public static readonly Material technoMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.technoMarkColor);
        public static readonly Color enchanterMarkColor = new Color(1, .6f, .6f);
        public static readonly Material enchanterMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.enchanterMarkColor);
        public static readonly Color bloodmageMarkColor = new Color(0.6f, 0f, 0f);
        public static readonly Material bloodmageMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.bloodmageMarkColor);
        public static readonly Color chronomancerMarkColor = new Color(.4f, .65f, 1f);
        public static readonly Material chronomancerMarkMat = MaterialPool.MatFrom("Other/MageMark", ShaderDatabase.Transparent, TM_RenderQueue.chronomancerMarkColor);
        public static readonly Material chaosMarkMat = MaterialPool.MatFrom("Other/chaosMark");       

        //Might
        public static readonly Material deceptionEye = MaterialPool.MatFrom("Motes/DeceptionMote");
        public static readonly Material possessionMask = MaterialPool.MatFrom("Motes/PossessMote");

        public static readonly Material fighterMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, Color.black);
        public static readonly Color facelessMarkColor = new Color(1f, .5f, .25f);
        public static readonly Material facelessMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, TM_RenderQueue.facelessMarkColor);
        public static readonly Color rangerMarkColor = new Color(.3f, .6f, .0f);
        public static readonly Material rangerMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, TM_RenderQueue.rangerMarkColor);
        public static readonly Color gladiatorMarkColor = new Color(0f, .35f, .75f);
        public static readonly Material gladiatorMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, TM_RenderQueue.gladiatorMarkColor);
        public static readonly Material bladedancerMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, Color.gray);
        public static readonly Color sniperMarkColor = new Color(.7f, 0f, 0f);
        public static readonly Material sniperMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, TM_RenderQueue.sniperMarkColor);
        public static readonly Color psionicMarkColor = new Color(0f, .5f, 1f);
        public static readonly Material psionicMarkMat = MaterialPool.MatFrom("Other/PsiMark", ShaderDatabase.Transparent, TM_RenderQueue.psionicMarkColor);
        public static readonly Color monkMarkColor = new Color(1f, 1f, 0f);
        public static readonly Material monkMarkMat = MaterialPool.MatFrom("Other/PsiMark", ShaderDatabase.Transparent, TM_RenderQueue.monkMarkColor);
        public static readonly Color deathknightMarkColor = new Color(.01f, .01f, .01f);
        public static readonly Material deathknightMarkMat = MaterialPool.MatFrom("Other/FighterMark", ShaderDatabase.Transparent, TM_RenderQueue.deathknightMarkColor);

        public static readonly Material supersoldierMarkMat = MaterialPool.MatFrom("Other/SSMark");
        public static readonly Material commanderMarkMat = MaterialPool.MatFrom("Other/CommanderMark");

        //Custom
        public static readonly Material customClassMarkMat = MaterialPool.MatFrom("Other/CustomMark");

    }
}
