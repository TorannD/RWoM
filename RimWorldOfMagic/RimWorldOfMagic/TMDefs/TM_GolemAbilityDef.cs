using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using TorannMagic.Golems;

namespace TorannMagic.TMDefs
{
    public class TM_GolemAbilityDef : Def //, IExposable
    {
        //Golem Abilities - why not use another ability structure??? They are weak.
        public int priority = 3;
        public int warmupTicks; //ticks to 'charge' the ability
        public int cooldownTicks; //ticks until the ability is usable again        
        public bool warmupStuns; //determine whether the pawn is issues a job to activate the attack
        public bool interruptsJob = false;
        public int burstCount = 1; //number of attacks to make
        public int ticksBetweenBurstShots;      
        public TM_Autocast autocasting; //allows autocast condition checks
        public NeedDef requiredNeed = null; //pawn has to have this need to use the ability
        public float needCost; //amount of need consumed to use ability
        public HediffDef requiredHediff = null; //golem has to have this need to use the ability
        public float hediffCost; //amount of hediff severity consumed to use ability
        public JobDef jobDef = null; //initiates a job
        public int jobDuration = 60;
        public int jobBurstCount = 1;
        public int jobTicksBetweenBursts = 1;
        public List<GolemAbilityEffect> jobEffect = new List<GolemAbilityEffect>(); //effect that occurs each burst of an ability job
        public ThingDef warmupMote; //throws this mote when the ability starts
        public ThingDef tickMote = null; //throws this mote each tick frequency
        public float tickMoteSize = 1f;
        public int tickMoteFrequency = 10;
        public float tickMoteVelocityTowardsTarget = 0;
        public bool isViolent = true;
        public List<CompProperties_GolemAbilityEffect> effects = new List<CompProperties_GolemAbilityEffect>();

        public bool IsRangedAttack => autocasting != null ? autocasting.maxRange > 1f : false;


        //public void ExposeData()
        //{
        //    Scribe_Values.Look<int>(ref this.priority, "priority");
        //    Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTicks");
        //    Scribe_Values.Look<int>(ref this.cooldownTicks, "cooldownTicks");
        //    Scribe_Values.Look<int>(ref this.lastUsedTick, "lastUsedTick");
        //    Scribe_Values.Look<int>(ref this.burstCount, "burstCount", 1);
        //    Scribe_Values.Look<int>(ref this.ticksBetweenBurstShots, "ticksBetweenBurstShots");
        //    Scribe_Values.Look<bool>(ref this.interruptsJob, "interruptsJob", false);
        //    Scribe_Values.Look<bool>(ref this.warmupStuns, "warmupStuns", false);
        //    Scribe_Values.Look<bool>(ref this.isViolent, "isViolent", true);
        //    Scribe_Deep.Look<TM_Autocast>(ref this.autocasting, "autocasting");
        //    Scribe_Defs.Look<NeedDef>(ref this.requiredNeed, "requiredNeed");
        //    Scribe_Values.Look<float>(ref this.needCost, "needCost");
        //    Scribe_Defs.Look<HediffDef>(ref this.requiredHediff, "requiredHediff");
        //    Scribe_Values.Look<float>(ref this.hediffCost, "hediffCost");
        //    Scribe_Defs.Look<JobDef>(ref this.jobDef, "jobDef");
        //    Scribe_Collections.Look<GolemAbilityEffect>(ref this.effects, "effects", LookMode.Deep);
        //}
    }
}