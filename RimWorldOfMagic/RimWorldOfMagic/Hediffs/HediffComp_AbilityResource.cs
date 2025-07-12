using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class HediffCompProperties_AbilityResource : HediffCompProperties
    {
        public float maximumBase = 100f;
        public float maximumPerUpgrade = 0f;
        public string maximumUpgradeName;
        public float regenPerTickBase = 0f;
        public float regenPerTickPerUpgrade = 0f;
        public string regenPerTickUpgradeName;

        public TMAbilityDef linkedAbility;

        public HediffCompProperties_AbilityResource()
        {
            this.compClass = typeof(HediffComp_AbilityResource);
        }

    }

    [StaticConstructorOnStartup]
    public class HediffComp_AbilityResource : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 300;

        private HediffCompProperties_AbilityResource Props { get => this.props as HediffCompProperties_AbilityResource; }

        private string maximumCachedUpgradeName;
        private float maximumCached;
        private string regenPerTickCachedUpgradeName;
        private float regenPerTickCached;

        public override string CompLabelInBracketsExtra => string.Concat(this.parent.Severity.ToString("0."), "/", maximumCached.ToString("0."));
        public override bool CompShouldRemove => this.removeNow || base.CompShouldRemove;

        private void Initialize()
        {
            UpdateCachedValues();
            this.initialized = true;
        }

        private void UpdateCachedValues()
        {
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (comp != null)
            {
                int lvlMax = 0;
                MightPowerSkill mps = TM_ClassUtility.GetMightPowerSkillFromLabel(comp, this.Props.maximumUpgradeName);
                if (mps != null)
                {
                    lvlMax = mps.level;
                }
                maximumCached = Props.maximumBase + (Props.maximumPerUpgrade * lvlMax);
                int lvlRegen = 0;
                mps = TM_ClassUtility.GetMightPowerSkillFromLabel(comp, this.Props.regenPerTickUpgradeName);
                if (mps != null)
                {
                    lvlRegen = mps.level;
                }
                regenPerTickCached = Props.regenPerTickBase + (Props.regenPerTickPerUpgrade * lvlRegen);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (!this.initialized)
                {
                    Initialize();
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 3)
                {
                    this.UpdateCachedValues();
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 3)
                {
                    float newValue = this.parent.Severity + eventFrequency * regenPerTickCached;
                    this.parent.Severity = Mathf.Clamp(newValue, 0f, maximumCached);
                }
            }
        }
    }
}
