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

        private Pawn projectedHost;
        private TMAbilityDef projectedSpell;
        private bool projected;
        private bool originalAutocast;
        internal bool Projected => projected;
        internal Pawn ProjectedHost => projectedHost;
        internal void BindHost(Pawn host, TMAbilityDef spell)
        {
            projected = true;
            projectedHost = host;
            projectedSpell = spell;
            originalAutocast = ProjectedPower()?.autocast ?? false;
        }

        private MagicPower ProjectedPower()
        {
            var data = Pawn?.GetCompAbilityUserMagic()?.ExistingMagicData;
            if (projectedSpell == null || data == null) return null;
            var aura = projectedSpell.abilityHediff;
            var powers = aura == TorannMagicDefOf.TM_RayOfHope_AuraHD ? data.magicPowerP
                : aura == TorannMagicDefOf.TM_InnerFire_AuraHD ? data.magicPowerIF
                : aura == TorannMagicDefOf.TM_Shadow_AuraHD ? data.magicPowerA
                : aura == TorannMagicDefOf.TM_SoothingBreeze_AuraHD ? data.magicPowerHoF : null;
            return powers?.FirstOrDefault(power => power?.TMabilityDefs?.Contains(projectedSpell) == true);
        }

        internal void EndProjection()
        {
            // A forced release ends the toggle just like recasting it. A player can also change autocast while the aura runs.
            var power = ProjectedPower();
            if (power != null && power.autocast != originalAutocast) power.autocast = originalAutocast;
            Pawn.health.RemoveHediff(parent);
        }

        internal void TickHosted()
        {
            if (!Projected) return;
            int now = Find.TickManager.TicksGame;
            if (initialized && now <= nextApplyTick && now % 60 != 0) return;
            if (!ClassProgression.SpiritHostedAura.CanMaintain(projectedHost, Pawn, projectedSpell))
            {
                EndProjection();
                return;
            }
            if (projectedHost.Spawned) TickAura(projectedHost);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref projected, "TM_hostedAura");
            if (Projected)
            {
                Scribe_Values.Look(ref originalAutocast, "TM_hostedAuraOriginalAutocast");
                Scribe_Defs.Look(ref projectedSpell, "TM_hostedAuraSpell");
                Scribe_References.Look(ref projectedHost, "TM_hostedAuraBody");
                Scribe_Values.Look(ref nextApplyTick, "TM_hostedAuraNextPulse");
            }
            ClassProgression.ArcanistTrials.ExposeAura(parent);
            ClassProgression.PaladinTrials.ExposeAura(parent);
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

        private void Initialize(Pawn emitter)
        {
            bool spawned = emitter.Spawned;
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
            if (!Projected) TickAura(Pawn);
        }

        private void TickAura(Pawn emitter)
        {
            bool flag = emitter?.Map != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize(emitter);
                }

                if (Find.TickManager.TicksGame > this.nextApplyTick && this.hediffDef != null)
                {
                    Pawn pawn = TM_Calc.FindNearbyFactionPawn(emitter, emitter.Faction, 100);
                    if (Projected && pawn == null) nextApplyTick = Find.TickManager.TicksGame + 80;
                    if (pawn != null && pawn.health != null)
                    {
                        if (pawn.health.hediffSet.HasHediff(this.hediffDef, false) || pawn.Faction != emitter.Faction || pawn.RaceProps.Animal)
                        {
                            this.nextApplyTick = Find.TickManager.TicksGame + Rand.Range(80, 150);
                        }
                        else
                        {
                            ClassProgression.PaladinTrials.ApplyAura(this.parent, pawn, this.hediffDef);
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
