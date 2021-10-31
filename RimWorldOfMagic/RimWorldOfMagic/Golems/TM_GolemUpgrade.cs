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
    public class TM_GolemUpgrade : IExposable
    {
        public int currentLevel = 0;
        public TM_GolemUpgradeDef golemUpgradeDef;

        public TM_GolemUpgrade()
        {

        }

        public TM_GolemUpgrade(TM_GolemUpgradeDef def)
        {
            golemUpgradeDef = def;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.currentLevel, "currentLevel", 0);
            Scribe_Defs.Look<TM_GolemUpgradeDef>(ref this.golemUpgradeDef, "golemUpgradeDef");
        }
    }
}
