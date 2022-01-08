using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_BrandingBase : HediffComp
    {
        private bool initialized = false;
        private bool shouldRemove = false;
        private int nextUpdateTick = 0;
        private Pawn branderPawn = null;
        float pwrVal = 0f;
        float verVal = 0f;

        public override string CompLabelInBracketsExtra => branderPawn != null ? branderPawn.LabelShort + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public virtual int AverageUpdateTick => 350;

        public Pawn BranderPawn
        {
            get => branderPawn;
            set
            {
                branderPawn = value;
            }
        }

        public bool ShouldRemove            
        {
            get => shouldRemove;
            set
            {
                shouldRemove = value;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.branderPawn, "branderPawn");
        }
        //public string GetCompLabel
        //{
        //    get
        //    {
        //        string tmp = "";
        //        if (surging)
        //        {
        //            tmp += "[+]";
        //        }
        //        else if (draining)
        //        {
        //            tmp += "[-]";
        //        }
        //        tmp += base.CompLabelInBracketsExtra;
        //        return tmp;
        //    }
        //}

        public string labelCap
        {
            get
            {
                if(branderPawn != null)
                {
                    return base.Def.LabelCap + "(" + branderPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (branderPawn != null)
                {
                    return base.Def.label + "(" + branderPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            this.nextUpdateTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(.8f, 1.2f) * AverageUpdateTick); ;
            if (spawned)
            {
                Vector3 rndPos = base.Pawn.DrawPos;
                rndPos.x += Rand.Range(-.3f, .3f);
                rndPos.z += Rand.Range(-.3f, .3f);
                FleckMaker.ThrowLightningGlow(rndPos, base.Pawn.Map, Rand.Range(.2f, .4f));
            }            
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);            
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }
            if (Find.TickManager.TicksGame >= this.nextUpdateTick)
            {
                this.nextUpdateTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(.8f, 1.2f) * AverageUpdateTick);
                Pawn pawn = base.Pawn;
                CompAbilityUserMagic comp = null;
                if(branderPawn != null)
                {
                    comp = branderPawn.TryGetComp<CompAbilityUserMagic>();
                    if (!BranderPawn.DestroyedOrNull() && !BranderPawn.Dead && comp != null && comp.Mana != null)
                    {
                        if(BranderPawn.Downed)
                        {
                            comp.sigilDraining = false;
                            comp.sigilSurging = false;                            
                        }
                        if (comp.Mana.CurLevel >= .01f)
                        {
                            float brandSeverity = .125f;
                            //pwrVal = TM_Calc.GetMagicSkillLevel(branderPawn, comp.MagicData.MagicPowerSkill_Branding, "TM_Branding", "_pwr", true); 
                            pwrVal = TM_Calc.GetSkillPowerLevel(BranderPawn, TorannMagicDefOf.TM_Branding, true);
                            brandSeverity += (.05f * pwrVal);
                            if (comp.sigilSurging)
                            {
                                pwrVal = TM_Calc.GetSkillPowerLevel(BranderPawn, TorannMagicDefOf.TM_SigilSurge, true);
                                verVal = TM_Calc.GetSkillVersatilityLevel(BranderPawn, TorannMagicDefOf.TM_SigilSurge);
                                brandSeverity += .4f + (.05f * pwrVal);
                                HealthUtility.AdjustSeverity(branderPawn, TorannMagicDefOf.TM_SigilPainHD, brandSeverity * (.4f - (.04f * verVal)));                                    
                            }                                

                            if (comp.sigilDraining)
                            {
                                pwrVal = TM_Calc.GetSkillPowerLevel(BranderPawn, TorannMagicDefOf.TM_SigilDrain);
                                verVal = TM_Calc.GetSkillVersatilityLevel(BranderPawn, TorannMagicDefOf.TM_SigilDrain);
                                this.parent.Severity = .05f;
                                HealthUtility.AdjustSeverity(branderPawn, this.Def, brandSeverity * .2f);
                                HealthUtility.AdjustSeverity(base.Pawn, TorannMagicDefOf.TM_SigilPainHD, (.4f - (.04f * verVal)));
                            }
                            else
                            {
                                this.parent.Severity = brandSeverity;
                            }
                            DoSigilAction(comp.sigilSurging, comp.sigilDraining);
                        }
                        else
                        {
                            this.parent.Severity = .05f;
                        }                        
                    }
                    else
                    {
                        this.shouldRemove = true;
                    }
                }
                else
                {
                    severityAdjustment += -(.02f + .3f*this.parent.Severity);
                    if(parent.Severity < .01f)
                    {
                        this.shouldRemove = true;
                    }
                }
            }
        }

        public virtual void DoSigilAction(bool surging = false, bool draining = false)
        {

        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.shouldRemove;
            }
        }
    }
}
