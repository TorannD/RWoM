using AbilityUser;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic 
{
    public abstract class TMPower : IExposable
    {
        public List<AbilityDef> TMabilityDefs;
        public TMDefs.TM_Autocast autocasting;
        public int level;

        public bool learned;
        public bool autocast;
        public int learnCost = 2;
        public int costToLevel = 1;

        private int ticksUntilNextCast = -1;
        private int interactionTick;

        protected TMPower() {}
        protected TMPower(List<AbilityDef> newAbilityDefs)
        {
            level = 0;
            TMabilityDefs = newAbilityDefs;
        }

        public bool AutoCast
        {
            get => autocast;
            set
            {
                if (interactionTick >= Find.TickManager.TicksGame) return;
                autocast = value;
                interactionTick = Find.TickManager.TicksGame + 5;
            }
        }

        public int MaxLevel => TMabilityDefs.Count - 1;

        public AbilityDef nextLevelAbilityDescDef => nextLevelAbilityDef;

        public AbilityDef abilityDef
        {
            get
            {
                if (TMabilityDefs is not { Count: > 0 }) return null;
                if (level <= 0) return TMabilityDefs[0];
                if (level >= MaxLevel) return TMabilityDefs[MaxLevel];
                return TMabilityDefs[level];
            }
        }

        public AbilityDef nextLevelAbilityDef
        {
            get
            {
                if (level + 1 >= MaxLevel) return TMabilityDefs[MaxLevel];
                return TMabilityDefs[level + 1];
            }
        }

        public Texture2D Icon => abilityDef.uiIcon;

        public AbilityDef GetAbilityDef(int index)
        {
            try
            {
                return TMabilityDefs[index];
            }
            catch
            {
                return TMabilityDefs[0];
            }
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look<bool>(ref learned, "learned", true);
            Scribe_Values.Look<bool>(ref autocast, "autocast");
            Scribe_Values.Look<int>(ref learnCost, "learnCost", 2);
            Scribe_Values.Look<int>(ref costToLevel, "costToLevel", 1);
            Scribe_Values.Look<int>(ref level, "level");
            Scribe_Values.Look<int>(ref ticksUntilNextCast, "ticksUntilNextCast", -1);
            Scribe_Collections.Look<AbilityDef>(ref TMabilityDefs, "TMabilityDefs", (LookMode)4, null);
            Scribe_Deep.Look<TMDefs.TM_Autocast>(ref autocasting, "autocasting");
        }
    }
}
