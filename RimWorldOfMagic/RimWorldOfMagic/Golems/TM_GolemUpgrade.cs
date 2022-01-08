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
        public bool enabled = true;
        public TM_GolemUpgradeDef golemUpgradeDef;
        public List<Material> animationMats = null;

        public TM_GolemUpgrade()
        {

        }

        public TM_GolemUpgrade(TM_GolemUpgradeDef def)
        {
            golemUpgradeDef = def;
        }

        public void PopulateAnimationMaterial()
        {
            animationMats = new List<Material>();
            animationMats.Clear();
            for(int i = 0; i < golemUpgradeDef.animationPath.Count; i++)
            {
                animationMats.Add(MaterialPool.MatFrom("Golems/" + golemUpgradeDef.animationPath[i], ShaderDatabase.Transparent, Color.white));
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.currentLevel, "currentLevel", 0);
            Scribe_Values.Look<bool>(ref this.enabled, "enabled", true);
            Scribe_Defs.Look<TM_GolemUpgradeDef>(ref this.golemUpgradeDef, "golemUpgradeDef");
        }
    }
}
