using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using AbilityUser;
using TorannMagic.Golems;

namespace TorannMagic.TMDefs
{
    public class TM_GolemUpgradeDef : Def
    {
        //Golem upgrade 
        public RecipeDef recipe;
        public BodyPartDef bodypart;
        public HediffDef hediff;
        public float hediffSeverityPerLevel = 1f;
        public TM_GolemAbilityDef ability;
        public int abilityUsedTick = 0;
        public BodyPartDef occupiedPart = null;
        public List<LifeStageDef> lifeStages = new List<LifeStageDef>();
        public bool abilityToggle = false;

        public string graphicsPath;
        public List<string> animationPath = new List<string>();
        public bool randomAnimation = false;
        public int changeAnimationTicks = 60;
        public ThingDef drawThing = null;
        public bool levelIncrementsGraphics = false;
        public Vector3 drawOffsetSouth = new Vector3(0, 0, 0);
        public Vector3 drawOffsetNorth = new Vector3(0, 0, 0);
        public Vector3 drawOffsetEast = new Vector3(0, 0, 0);
        public Vector3 drawOffsetWest = new Vector3(0, 0, 0);
        public bool drawNorth = true;
        public bool drawSouth = true;
        public bool drawWest = true;
        public bool drawEast = true;
        public bool drawUndrafted = true;
        public bool drawOnlyWhenActive = false;
        public float drawSize = 1f;
        public bool shouldRotate = false;

        public bool partRequiresUpgrade = false;
        public ThingDef verbProjectile = null;
        public int maxLevel = 1;
        public WorkstationCapacity workstationCapacity = WorkstationCapacity.None;
        public List<GolemWorkstationEffect> workstationEffects = new List<GolemWorkstationEffect>();
        public GolemAbilityUpgrade abilityModifiers = null;

        public BodyPartDef OccupiedPart => occupiedPart != null ? occupiedPart : bodypart;        
    }

    public enum WorkstationCapacity
    {
        None,           //default value
        EnergyMax,      //increases max energy of golem
        Efficiency,     //Improves energy efficiency
        EnergyRegeneration, //increases speed energy is gained
        Effect          //effect varies by golem, coded into building_name
    }
}