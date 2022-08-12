using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_ProvisionerAura : HediffComp
    {

        private bool initialized = false;

        private int nextApplyTick = 0;

        private int pwrVal = 0;
        private int verVal = 0;
        private float radius = 0f;

        List<Pawn> speechPawns = new List<Pawn>();

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
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (spawned && comp != null && comp.IsMightUser)
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
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }

                if (Find.TickManager.TicksGame > this.nextApplyTick)
                {
                    this.nextApplyTick = Find.TickManager.TicksGame + Rand.Range(1000, 1200);
                    if (base.Pawn.Map != null)
                    {
                        speechPawns.Clear();
                        List<Pawn> mapPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
                        for (int i = 0; i < mapPawns.Count; i++)
                        {
                            if (mapPawns[i].RaceProps.Humanlike && mapPawns[i].Faction != null && mapPawns[i].Faction == base.Pawn.Faction && mapPawns[i] != this.Pawn)
                            {
                                if (!TM_Calc.IsUndeadNotVamp(mapPawns[i]))
                                {
                                    if (base.Pawn.Position.InHorDistOf(mapPawns[i].Position, radius))
                                    {
                                        ApplyHediff(mapPawns[i]);
                                    }
                                }
                            }
                        }
                        CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
                        comp.MightUserXP += Rand.Range(2, 5);                        
                    }
                    else //map null
                    {
                        if (this.Pawn.ParentHolder.ToString().Contains("Caravan"))
                        {
                            foreach (Pawn current in base.Pawn.holdingOwner)
                            {
                                if (current != null)
                                {
                                    if (current.RaceProps.Humanlike && current.Faction != null && current.Faction == this.Pawn.Faction && current != this.Pawn)
                                    {
                                        ApplyHediff(current);
                                    }
                                }
                            }
                        }
                    }
                }

                if(Find.TickManager.TicksGame % 1200 == 0)
                {
                    DetermineHediff();
                }
            }
        }     

        private void DetermineHediff()
        {           
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (parent.def == TorannMagicDefOf.TM_ProvisionerAuraHD && comp != null)
            {
                pwrVal = comp.MightData.MightPowerSkill_ProvisionerAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ProvisionerAura_pwr").level;
                verVal = comp.MightData.MightPowerSkill_ProvisionerAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ProvisionerAura_ver").level;
            }
            this.radius = 15f + (2f * verVal);
        }

        private void ApplyHediff(Pawn p)
        {
            if (p.health != null && p.health.hediffSet != null)
            {
                Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProvisionerHD);
                if (hd != null)
                {
                    if (hd.Severity < (.5f + pwrVal))
                    {
                        hd.Severity = .5f + pwrVal;
                    }
                }
                else
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_ProvisionerHD, .5f + pwrVal);
                    hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProvisionerHD);
                }
                if (hd != null)
                {
                    HediffComp_Provisioner hdComp = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProvisionerHD, false).TryGetComp<HediffComp_Provisioner>();
                    if (hdComp != null)
                    {
                        hdComp.duration += 2;
                        hdComp.pwrVal = pwrVal;
                        hdComp.verVal = verVal;
                    }
                }
                if(verVal >= 3)
                {
                    float sev = 1.15f;
                    bool flag = true;
                    if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnergyRegenHD, false))
                    {
                        Hediff hdRemove = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false);
                        if (hdRemove != null && hdRemove.Severity > sev)
                        {
                            //do not overwrite a better regeneration buff
                            flag = false;
                        }
                        else
                        {
                            HediffComp_EnergyRegen hd2 = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_EnergyRegen>();
                            if (hd2 != null)
                            {
                                hd2.duration += 10 + pwrVal;
                            }
                        }
                    }
                    if(flag)
                    {
                        HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_EnergyRegenHD, sev);
                        HediffComp_EnergyRegen hd2 = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_EnergyRegen>();
                        if (hd2 != null)
                        {
                            hd2.duration = 20 + pwrVal*2;
                        }

                    }
                }
            }            
        }
    }
}
