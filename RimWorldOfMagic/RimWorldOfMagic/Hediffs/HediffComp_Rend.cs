using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_Rend : HediffComp
    {

        private bool initialized = false;
        private int initializeDelay = 0;
        private bool removeNow = false;

        private int eventFrequency = 20;

        private int rendPwr = 0;  //increased amount blood levels affect ability power
        private int rendVer = 0;  //increased blood per bleed rate and blood gift use
        private float arcaneDmg = 1;

        private float bleedRate = 0;

        public Pawn linkedPawn = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref linkedPawn, "linkedPawn", false);
            Scribe_Values.Look<int>(ref this.rendVer, "rendVer", 0, false);
            Scribe_Values.Look<int>(ref this.rendPwr, "rendPwr", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
        }

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

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            CompAbilityUserMagic comp = this.linkedPawn.GetCompAbilityUserMagic();
            if (spawned && comp != null && comp.IsMagicUser)
            {
                MagicPowerSkill bpwr = comp.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_pwr");
                rendPwr = comp.MagicData.MagicPowerSkill_Rend.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Rend_pwr").level;
                rendVer = comp.MagicData.MagicPowerSkill_Rend.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Rend_ver").level;
                arcaneDmg = comp.arcaneDmg;
                arcaneDmg *= (1 + .1f * bpwr.level);
            }
            else
            {
                this.removeNow = true;
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null && this.initializeDelay > 5;
            if (flag)
            {
                if (!initialized && this.linkedPawn != null)
                {
                    initialized = true;
                    this.Initialize();
                }

                if(this.Pawn.DestroyedOrNull() || this.Pawn.Dead || this.Pawn.Map == null || this.Pawn.RaceProps.BloodDef == null)
                {
                    this.removeNow = true;
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 0 && !this.removeNow)
                {
                    DamagePawn();
                    severityAdjustment -= Rand.Range(.1f, .15f);
                }
            }
            else
            {
                this.initializeDelay++;
            }
        }
        
        public void DamagePawn()
        {
            if(this.Pawn != null && this.Pawn.Map != null)
            { 
                TM_Action.DamageEntities(this.Pawn, null, Mathf.RoundToInt(2f * (this.arcaneDmg + (.15f * this.rendPwr))), TMDamageDefOf.DamageDefOf.TM_BloodyCut, this.linkedPawn);

                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CrossStrike, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.3f, 0.45f), .45f, .05f, .20f, 0, 0, 0, Rand.Range(0, 360));
                for (int j = 0; j < (1 + rendVer); j++)
                {
                    IntVec3 rndPos = this.Pawn.Position;
                    rndPos.x += Mathf.RoundToInt(Rand.Range(-2f, 2f));
                    rndPos.z += Mathf.RoundToInt(Rand.Range(-2f, 2f));
                    if (this.Pawn.RaceProps != null && this.Pawn.RaceProps.BloodDef != null && this.Pawn.Map != null)
                    {
                        FilthMaker.TryMakeFilth(rndPos, this.Pawn.Map, this.Pawn.RaceProps.BloodDef, 1);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.6f, 1.0f), .15f, .05f, .66f, Rand.Range(-100, 100), Rand.Range(1, 2), Rand.Range(0, 360), Rand.Range(0f, 360f));
                    }                    
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }        
    }
}
