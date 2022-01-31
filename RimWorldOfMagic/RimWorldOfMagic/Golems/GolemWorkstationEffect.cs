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
    public class GolemWorkstationEffect : IExposable
    {
        public int ticksTillNextEffect80 = 100;
        public int effectDuration = 0;
        public SoundDef effectSound = null;
        public float effectFlashScale = 0f;
        public bool doEffectEachBurst = false;
        public float energyCost = 0f;
        public bool alwaysDraw = false;
        public bool requiresTarget = false;
        public float effectLevelModifier = 0f;
        public Vector3 drawOffset = new Vector3(0, 0, 0);
        public List<TM_GolemItemRecipeDef> recipes = new List<TM_GolemItemRecipeDef>();

        public LocalTargetInfo target = null;
        public Building_TMGolemBase parent;
        public TM_GolemUpgrade parentUpgrade;
        public int startTick = 0;
        public int nextEffectTick = 0;
        public float currentLevel;
        public int effectFrequency = 0;
        public int chargesRequired = 0;

        public bool EffectActive => (startTick + effectDuration) > Find.TickManager.TicksGame;

        public virtual float LevelModifier => (1f + (effectLevelModifier * currentLevel));

        public virtual void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.startTick, "startTick");
            Scribe_Values.Look<int>(ref this.nextEffectTick, "nextEffectTick");
            Scribe_Values.Look<float>(ref this.currentLevel, "currentLevel");

            Scribe_Values.Look<int>(ref this.ticksTillNextEffect80, "ticksTillNextEffect80");
            Scribe_Values.Look<int>(ref this.effectDuration, "effectDuration");
            Scribe_Values.Look<float>(ref this.effectFlashScale, "effectFlashScale");
            Scribe_Values.Look<float>(ref this.energyCost, "energyCost");
            Scribe_Values.Look<float>(ref this.effectLevelModifier, "effectLevelModifier");
            Scribe_Values.Look<bool>(ref this.alwaysDraw, "alwaysDraw", false);
            Scribe_Values.Look<bool>(ref this.requiresTarget, "requiresTarget");
            Scribe_Defs.Look<SoundDef>(ref this.effectSound, "effectSound");
        }

        public virtual void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            parent = golem_building;
            parentUpgrade = upgrade;
            currentLevel = effectLevel;
            if(effectFlashScale > 0.01f)
            {
                FleckMaker.Static(golem_building.Position, golem_building.Map, FleckDefOf.ShotFlash, effectFlashScale);
            }
            this.startTick = Find.TickManager.TicksGame;
            nextEffectTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(.8f, 1.2f) * ticksTillNextEffect80 * golem_building.GolemComp.ProcessingModifier);
            effectSound?.PlayOneShot(new TargetInfo(golem_building.Position, golem_building.Map));
            CompGolemEnergyHandler cgeh = golem_building.TryGetComp<CompGolemEnergyHandler>();
            if (energyCost > 0 && cgeh != null)
            {
                cgeh.AddEnergy(-energyCost);
            }            
        }

        public virtual void ContinueEffect(Building_TMGolemBase golem_building)
        {
            if (doEffectEachBurst && effectFlashScale > 0.01f)
            {
                FleckMaker.Static(golem_building.Position, golem_building.Map, FleckDefOf.ShotFlash, effectFlashScale);
            }
        }

        public virtual bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            CompGolemEnergyHandler cgeh = golem_building.TryGetComp<CompGolemEnergyHandler>();
            if(energyCost > 0 && cgeh != null)
            {
                if(cgeh.StoredEnergy < golem_building.ActualEnergyCost(energyCost))
                {
                    return false;
                }
            }
            return nextEffectTick <= Find.TickManager.TicksGame;
        }
    }
}
