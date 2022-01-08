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
    public class TM_GolemAbility : IExposable
    {
        public int lastUsedTick = 0;
        public int currentLevel = 1;
        public TM_GolemAbilityDef golemAbilityDef;

        public bool AbilityReady
        {
            get
            {
                if (lastUsedTick > Find.TickManager.TicksGame)
                {
                    lastUsedTick = Find.TickManager.TicksGame;
                }
                return Find.TickManager.TicksGame >= lastUsedTick + golemAbilityDef?.cooldownTicks;
            }
        }

        public TM_GolemAbility()
        {

        }

        public TM_GolemAbility(TM_GolemAbilityDef def, int level)
        {
            golemAbilityDef = def;
            currentLevel = level;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.lastUsedTick, "lastUsedTick", 0);
            Scribe_Values.Look<int>(ref this.currentLevel, "currentLevel", 1);
            Scribe_Defs.Look<TM_GolemAbilityDef>(ref this.golemAbilityDef, "golemAbilityDef");
        }
    }
}
