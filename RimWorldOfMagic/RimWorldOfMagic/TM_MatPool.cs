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
    public static class TM_MatPool
    {
        public static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        public static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        public static readonly Material LightBarrier = MaterialPool.MatFrom("Other/LightBarrier", ShaderDatabase.MoteGlow);
        

        public static readonly Material blackLightning = MaterialPool.MatFrom("Other/ArcaneBolt", true);
        public static readonly Material redLightning = MaterialPool.MatFrom("Other/DemonBolt", true);
        public static readonly Texture2D Icon_Undead = ContentFinder<Texture2D>.Get("UI/undead_icon", true);
        public static readonly Material PsionicBarrier = MaterialPool.MatFrom("Other/PsionicBarrier", ShaderDatabase.Transparent);
        public static readonly Material TimeBubble = MaterialPool.MatFrom("Other/TimeBubble", ShaderDatabase.Transparent);
        public static readonly Material psiLightning = MaterialPool.MatFrom("Other/PsiBolt", ShaderDatabase.Transparent);
        public static readonly Material chiLightning = MaterialPool.MatFrom("Other/ChiBolt", ShaderDatabase.Transparent);

        public static readonly Material psiMote = MaterialPool.MatFrom("Motes/PsiMote", ShaderDatabase.MoteGlow);
        public static readonly Material singleForkLightning = MaterialPool.MatFrom("Spells/LightningBolt_back1", ShaderDatabase.MoteGlow);
        public static readonly Material doubleForkLightning = MaterialPool.MatFrom("Spells/LightningBolt", ShaderDatabase.MoteGlow);
        public static readonly Material multiForkLightning = MaterialPool.MatFrom("Spells/LightningBolt_w", ShaderDatabase.MoteGlow);
        public static readonly Material standardLightning = MatLoader.LoadMat("Weather/LightningBolt", -1);
        public static readonly Material thinLightning = MaterialPool.MatFrom("Other/ThinLightningBolt", true);
        public static readonly Material thindeathlightning = MaterialPool.MatFrom("Other/ThinDeathBolt", true);
        public static readonly Material deathlightning2 = MaterialPool.MatFrom("Other/green_bolt", true);
        public static readonly Material deathlightning3 = MaterialPool.MatFrom("Other/green_bolt2", true);
        public static readonly Material deathbeam = MaterialPool.MatFrom("Other/death_beam_endpoints", true);
        public static readonly Material light_laser_long = MaterialPool.MatFrom("Other/light_laser_long", ShaderDatabase.MoteGlow);

        public static readonly Material opencloak_Female_north = MaterialPool.MatFrom("Equipment/opencloak_Female_north"); 
        public static readonly Material opencloak_Female_south = MaterialPool.MatFrom("Equipment/opencloak_Female_south");
        public static readonly Material opencloak_Female_east = MaterialPool.MatFrom("Equipment/opencloak_Female_east");
       
        //public static readonly Material maesterCloak_east = MatLoader.LoadMat("Equipment/opencloak_Female_north", -1);

        public static readonly Material barrier_Mote_Mat = MaterialPool.MatFrom("Motes/BarrierMote", ShaderDatabase.MoteGlow);

        //Colonist bar icons        
        public static readonly Texture2D bardIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/bardMageMark", true);
        public static readonly Texture2D arcanistIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/arcanistMageMark", true);
        public static readonly Texture2D bloodmageIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/bloodMageMark", true);
        public static readonly Texture2D demonkinIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/demonMageMark", true);
        public static readonly Texture2D druidIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/druidMageMark", true);
        public static readonly Texture2D earthIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/earthMageMark", true);
        public static readonly Texture2D enchanterIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/enchanterMageMark", true);
        public static readonly Texture2D fireIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/fireMageMark", true);
        public static readonly Texture2D iceIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/iceMageMark", true);
        public static readonly Texture2D lightningIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/lightningMageMark", true);
        public static readonly Texture2D necroIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/necroMageMark", true);
        public static readonly Texture2D paladinIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/paladinMageMark", true);
        public static readonly Texture2D priestIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/priestMageMark", true);
        public static readonly Texture2D summonerIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/summonerMageMark", true);
        public static readonly Texture2D technoIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/technoMageMark", true);
        public static readonly Texture2D chronoIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/chronomancerMageMark", true);
        public static readonly Texture2D chaosIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/chaosMageMark", true);

        public static readonly Texture2D deathknightIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/deathknightFighterMark", true);
        public static readonly Texture2D bladedancerIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/bladedancerFighterMark", true);
        public static readonly Texture2D facelessIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/facelessFighterMark", true);
        public static readonly Texture2D gladiatorIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/gladiatorFighterMark", true);
        public static readonly Texture2D rangerIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/rangerFighterMark", true);
        public static readonly Texture2D sniperIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/sniperFighterMark", true);

        public static readonly Texture2D psiIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/psiPsiMark", true);
        public static readonly Texture2D monkIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/monkPsiMark", true);

        public static readonly Texture2D commanderIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/CommanderMark", true);
        public static readonly Texture2D SSIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/supersoldierFighterMark", true);

        public static readonly Texture2D wandererIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/wandererFlame", true);
        public static readonly Texture2D wayfarerIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/wayfarerFlame", true);

        public static readonly Texture2D DefaultCustomMageIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/CustomMageMark", true);
        public static readonly Texture2D DefaultCustomFighterIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/CustomFighterMark", true);


        //skeleton chain
        public static readonly Material circleChain = MaterialPool.MatFrom("PawnKind/skeleton_chain_circle", ShaderDatabase.MoteGlow);
        //public static readonly Texture2D circleChain = ContentFinder<Texture2D>.Get("PawnKind/skeleton_chain_circle", true);
        //public static readonly Texture2D lineChain = ContentFinder<Texture2D>.Get("PawnKind/skeleton_chain_line", true);
        public static readonly Material lineChain = MaterialPool.MatFrom("PawnKind/skeleton_chain_line", ShaderDatabase.MoteGlow);
    }
}
