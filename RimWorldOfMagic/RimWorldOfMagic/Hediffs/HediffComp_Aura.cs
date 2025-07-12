using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_Aura : HediffComp
    {

        private bool initialized = false;

        private int nextApplyTick = 0;

        private HediffDef hediffDef = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
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
            CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
            if (spawned && comp != null && comp.IsMagicUser)
            {
                DetermineHediff();
            }
            else
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }

                if (Find.TickManager.TicksGame > this.nextApplyTick && this.hediffDef != null)
                {
                    Pawn pawn = TM_Calc.FindNearbyFactionPawn(this.Pawn, this.Pawn.Faction, 100);
                    if (pawn != null && pawn.health != null)
                    {
                        if (pawn.health.hediffSet.HasHediff(this.hediffDef, false) || pawn.Faction != this.Pawn.Faction || pawn.RaceProps.Animal)
                        {
                            this.nextApplyTick = Find.TickManager.TicksGame + Rand.Range(80, 150);
                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(pawn, this.hediffDef, 1f);
                            this.nextApplyTick = Find.TickManager.TicksGame + Rand.Range(4800, 5600);
                            FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 1f);
                            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, .8f);
                            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                            {
                                CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
                                comp.MightUserXP += Rand.Range(10, 15);
                            }
                            else
                            {
                                CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
                                comp.MagicUserXP += Rand.Range(10, 15);
                            }
                            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_UsedMagic, this.Pawn.Named(HistoryEventArgsNames.Doer), this.Pawn.Named(HistoryEventArgsNames.Subject), this.Pawn.Named(HistoryEventArgsNames.AffectedFaction), this.Pawn.Named(HistoryEventArgsNames.Victim)), true);
                        }
                    }
                }

                if(Find.TickManager.TicksGame % 1200 == 0)
                {
                    DetermineHediff();
                }
            }
        }     

        public void DetermineHediff()
        {
            MagicPower abilityPower = null;            
            CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
            if (parent.def == TorannMagicDefOf.TM_Shadow_AuraHD && comp != null)
            {
                abilityPower = comp.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow);                
                if (abilityPower == null)
                {
                    abilityPower = comp.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_I);                    
                    if (abilityPower == null)
                    {
                        abilityPower = comp.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_II);                        
                        if (abilityPower == null)
                        {                            
                            abilityPower = comp.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_III);
                        }
                    }
                }
                if (abilityPower.level == 0)
                {
                    this.hediffDef = TorannMagicDefOf.Shadow;
                }
                else if (abilityPower.level == 1)
                {
                    this.hediffDef = TorannMagicDefOf.Shadow_I;
                }
                else if (abilityPower.level == 2)
                {
                    this.hediffDef = TorannMagicDefOf.Shadow_II;
                }
                else
                {
                    this.hediffDef = TorannMagicDefOf.Shadow_III;
                }
            }
            if (parent.def == TorannMagicDefOf.TM_RayOfHope_AuraHD && comp != null)
            {
                abilityPower = comp.MagicData.MagicPowersP.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope);
                if (abilityPower == null)
                {
                    abilityPower = comp.MagicData.MagicPowersP.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_I);
                    if (abilityPower == null)
                    {
                        abilityPower = comp.MagicData.MagicPowersP.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_II);
                        if (abilityPower == null)
                        {
                            abilityPower = comp.MagicData.MagicPowersP.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_III);
                        }
                    }
                }
                if (abilityPower.level == 0)
                {
                    this.hediffDef = TorannMagicDefOf.RayofHope;
                }
                else if (abilityPower.level == 1)
                {
                    this.hediffDef = TorannMagicDefOf.RayofHope_I;
                }
                else if (abilityPower.level == 2)
                {
                    this.hediffDef = TorannMagicDefOf.RayofHope_II;
                }
                else
                {
                    this.hediffDef = TorannMagicDefOf.RayofHope_III;
                }
            }
            if (parent.def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD && comp != null)
            {
                abilityPower = comp.MagicData.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe);
                if (abilityPower == null)
                {
                    abilityPower = comp.MagicData.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_I);
                    if (abilityPower == null)
                    {
                        abilityPower = comp.MagicData.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_II);
                        if (abilityPower == null)
                        {
                            abilityPower = comp.MagicData.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_III);
                        }
                    }
                }
                if (abilityPower.level == 0)
                {
                    this.hediffDef = TorannMagicDefOf.SoothingBreeze;
                }
                else if (abilityPower.level == 1)
                {
                    this.hediffDef = TorannMagicDefOf.SoothingBreeze_I;
                }
                else if (abilityPower.level == 2)
                {
                    this.hediffDef = TorannMagicDefOf.SoothingBreeze_II;
                }
                else
                {
                    this.hediffDef = TorannMagicDefOf.SoothingBreeze_III;
                }
            }
            if (parent.def == TorannMagicDefOf.TM_InnerFire_AuraHD && comp != null)
            {
                abilityPower = comp.MagicData.MagicPowersIF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope);
                if (abilityPower == null)
                {
                    abilityPower = comp.MagicData.MagicPowersIF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_I);
                    if (abilityPower == null)
                    {
                        abilityPower = comp.MagicData.MagicPowersIF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_II);
                        if (abilityPower == null)
                        {
                            abilityPower = comp.MagicData.MagicPowersIF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_III);
                        }
                    }
                }
                if (abilityPower.level == 0)
                {
                    this.hediffDef = TorannMagicDefOf.InnerFireHD;
                }
                else if (abilityPower.level == 1)
                {
                    this.hediffDef = TorannMagicDefOf.InnerFire_IHD;
                }
                else if (abilityPower.level == 2)
                {
                    this.hediffDef = TorannMagicDefOf.InnerFire_IIHD;
                }
                else
                {
                    this.hediffDef = TorannMagicDefOf.InnerFire_IIIHD;
                }
            }
            if (abilityPower != null)
            {
                this.parent.Severity = .5f + abilityPower.level;
            }
            else
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }
    }
}
