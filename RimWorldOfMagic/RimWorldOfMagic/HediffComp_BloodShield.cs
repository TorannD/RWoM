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
    public class HediffComp_BloodShield : HediffComp
    {

        private bool initialized = false;
        private int initializeDelay = 0;
        private bool removeNow = false;
        private bool woundsHealed = false;

        private int eventFrequency = 180;

        //private int bfbPwr = 0;  //increased amount blood levels affect ability power
        private int bloodshieldVer = 0;  //increased blood per bleed rate and blood gift use

        private float lastSeverity = 0;

        public Pawn linkedPawn = null;
        Vector3 directionToLinkedPawn = default(Vector3);

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref linkedPawn, "linkedPawn", false);
            Scribe_Values.Look<int>(ref this.bloodshieldVer, "bloodshieldVer", 0, false);
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
            this.lastSeverity = this.parent.Severity;
            CompAbilityUserMagic comp = this.linkedPawn.GetCompAbilityUserMagic();
            if (spawned && comp != null && comp.IsMagicUser)
            {
                bloodshieldVer = comp.MagicData.MagicPowerSkill_BloodShield.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodShield_ver").level;
                //directionToLinkedPawn = TM_Calc.GetVector(this.Pawn.DrawPos, linkedPawn.DrawPos);
            }
            else
            {
                this.removeNow = true;
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null && this.initializeDelay > 1;
            if (flag)
            {
                if (!initialized && this.linkedPawn != null)
                {
                    initialized = true;
                    this.Initialize();
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 0)
                {
                    if(!this.linkedPawn.DestroyedOrNull() && !this.linkedPawn.Dead)
                    {

                        //directionToLinkedPawn = TM_Calc.GetVector(this.Pawn.DrawPos, linkedPawn.DrawPos);
                        float severityChange = this.lastSeverity - this.parent.Severity;
                        if (severityChange > 0)
                        {
                            HealWounds((severityChange / 2) * (1 + .15f *this.bloodshieldVer));
                            ReverseHealLinkedPawn(severityChange);
                        }
                        severityAdjustment -= Rand.Range(2.5f, 4f);
                        this.lastSeverity = this.parent.Severity;
                    }                    
                    else
                    {
                        this.removeNow = true;
                    }
                }
            }
            else
            {
                this.initializeDelay++;
            }
        }

        public void ReverseHealLinkedPawn(float severity)
        {
            Hediff bloodHediff = this.linkedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_BloodHD"), false);
            if(bloodHediff != null)
            {
                if (this.woundsHealed)
                {
                    if (bloodHediff.Severity < 1)
                    {
                        TM_Action.DamageEntities(this.linkedPawn, null, severity, TMDamageDefOf.DamageDefOf.TM_BloodBurn, this.Pawn);
                    }
                    else
                    {
                        bloodHediff.Severity -= severity / 3;
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, this.linkedPawn.DrawPos, this.Pawn.Map, Rand.Range(.6f, .7f), .2f, 0.05f, 1f, Rand.Range(-50, 50), Rand.Range(1.5f, 2f), (Quaternion.AngleAxis(-90, Vector3.up) * this.directionToLinkedPawn).ToAngleFlat(), Rand.Range(0, 360));
                    }
                }
            }
            else
            {
                this.removeNow = true;
            }

            if (severity > 1.25f)
            {
                Effecter BloodShieldEffect = TorannMagicDefOf.TM_BloodShieldEffecter.Spawn();
                BloodShieldEffect.Trigger(new TargetInfo(this.linkedPawn.Position, this.linkedPawn.Map, false), new TargetInfo(this.linkedPawn.Position, this.linkedPawn.Map, false));
                BloodShieldEffect.Cleanup();
            }
        }

        public void HealWounds(float healAmount)
        {
            this.woundsHealed = false;
            int num = 1;
            using (IEnumerator<BodyPartRecord> enumerator = this.Pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    bool flag2 = num > 0;

                    if (flag2)
                    {
                        int num2 = 1;
                        IEnumerable<Hediff_Injury> arg_BB_0 = this.Pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;

                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {
                            bool flag4 = num2 > 0;
                            if (flag4)
                            {                                
                                bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                if (flag5)
                                {
                                    current.Heal(healAmount);
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.5f, .75f), .2f, 0.05f, 1f, Rand.Range(-50, 50), Rand.Range(.5f, .7f), Rand.Range(30,40), Rand.Range(0, 360));
                                    num--;
                                    num2--;
                                    this.woundsHealed = true;
                                }
                            }
                        }
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
