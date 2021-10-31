using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemAbilityUpgrade : IExposable
    {
        public float damageModifier = 0f;
        public float cooldownModifier = 0f;
        public float energyCostModifier = 0f;
        public float durationModifier = 0f;
        public float healingModifier = 0f;
        public float processingModifier = 0f;

        public virtual void ExposeData()
        {
            Scribe_Values.Look<float>(ref this.energyCostModifier, "energyCostModifier", 0f);
            Scribe_Values.Look<float>(ref this.cooldownModifier, "cooldownModifier", 0f);
            Scribe_Values.Look<float>(ref this.damageModifier, "damageModifier", 0f);
            Scribe_Values.Look<float>(ref this.durationModifier, "durationModifier", 0f);
            Scribe_Values.Look<float>(ref this.healingModifier, "healingModifier", 0f);
            Scribe_Values.Look<float>(ref this.processingModifier, "processingModifier", 0f);
        }        
    }
}
