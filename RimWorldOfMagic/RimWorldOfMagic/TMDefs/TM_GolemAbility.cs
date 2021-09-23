using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace TorannMagic.TMDefs
{
    public class TM_GolemAbility : Def //, IExposable
    {
        //Golem Abilities - why not use another ability structure??? They are weak.
        public int priority;
        public int warmupTicks; //ticks to 'charge' the ability
        public int cooldownTicks; //ticks until the ability is usable again
        public int drawTicks = 0; //number of ticks the item is displayed when using an ability; any value initiates after warmup, a value of 0 draws the item for the entire period
        public bool warmupStuns; //determine whether the pawn is issues a job to activate the attack
        public ThingDef displayWeapon; //displays a weapon when attack is made
        public float displaySize = 1f;
        public IntVec2 displayOffsetSouth = new IntVec2(0, 0);
        public IntVec2 displayOffsetNorth = new IntVec2(0, 0);
        public IntVec2 displayOffsetEast = new IntVec2(0, 0);
        public IntVec2 displayOffsetWest = new IntVec2(0, 0);
        public float rotationOffset = 0f;
        public int burstCount = 1; //number of attacks to make
        public int ticksBetweenBurstShots;
        public float maxRange; //max range of ability
        public float minRange = 0f; //min range of ability
        public ThingDef projectile; //projectile fired
        public NeedDef requiredNeed = null; //pawn has to have this need to use the ability
        public float needCost; //amount of need consumed to use ability
        public HediffDef requiredHediff = null; //golem has to have this need to use the ability
        public float hediffCost; //amount of hediff severity consumed to use ability

        public int lastUsedTick = 0;

        public bool IsRangedAttack => maxRange > 1f;

        //public void ExposeData()
        //{
        //    Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTicks");
        //    Scribe_Values.Look<int>(ref this.cooldownTicks, "cooldownTicks");
        //    Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTakes");
        //    Scribe_Values.Look<bool>(ref this.warmupStuns, "warmupStuns", false);
        //    Scribe_Values.Look<int>(ref this.burstCount, "burstCount", 1);
        //    Scribe_Values.Look<int>(ref this.ticksBetweenBurstShots, "ticksBetweenBurstShots");
        //    Scribe_Values.Look<float>(ref this.maxRange, "maxRange");
        //    Scribe_Values.Look<float>(ref this.minRange, "minRange");
        //    Scribe_Defs.Look<ThingDef>(ref this.displayWeapon, "displayWeapon");
        //    Scribe_Defs.Look<ThingDef>(ref this.projectile, "projectile");
        //    Scribe_Defs.Look<NeedDef>(ref this.requiredNeed, "requiredNeed");
        //    Scribe_Values.Look<float>(ref this.needCost, "needCost");
        //    Scribe_Defs.Look<HediffDef>(ref this.requiredHediff, "requiredHediff");
        //    Scribe_Values.Look<float>(ref this.hediffCost, "hediffCost");            
        //}


    }
}