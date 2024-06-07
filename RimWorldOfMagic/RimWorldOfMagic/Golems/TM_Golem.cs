using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class TM_Golem : IExposable
    {
        //Golem Core Defs
        public int activationTicks = 240;
        public int processorEvaluationTicks = 100;
        public int shardLevel = 0;
        public float minimumEnergyPctToActivate = .1f;

        public ThingDef workstationDef = null;
        public ThingDef pawnDef = null;
        public TM_GolemDef golemDef = null;

        public List<TM_GolemUpgrade> upgrades = new List<TM_GolemUpgrade>();
        public List<NeedDef> needs = new List<NeedDef>();
        public List<HediffDef> hediffs = new List<HediffDef>();

        //dormat material
        public Material frameMat = null;

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref activationTicks, "activationTicks", 240);
            Scribe_Values.Look<int>(ref processorEvaluationTicks, "processorEvaluationTicks", 100);
            Scribe_Values.Look<int>(ref shardLevel, "shardLevel", 0);
            Scribe_Values.Look<float>(ref minimumEnergyPctToActivate, "minimumEnergyPctToActivate", .1f);
            Scribe_Collections.Look<TM_GolemUpgrade>(ref upgrades, "upgrades", LookMode.Deep);
            Scribe_Collections.Look<NeedDef>(ref needs, "needs", LookMode.Def);
            Scribe_Collections.Look<HediffDef>(ref hediffs, "hediffs", LookMode.Def);
            Scribe_Defs.Look<TM_GolemDef>(ref golemDef, "golemDef");
        }

        public TM_Golem()
        {

        }

        public TM_Golem(Thing thing)
        {
            golemDef = TM_GolemUtility.GetGolemDefFromThing(thing);
            activationTicks = golemDef.activationTicks;
            processorEvaluationTicks = golemDef.processorEvaluationTicks;
            minimumEnergyPctToActivate = golemDef.minimumEnergyPctToActivate;
            workstationDef = golemDef.golemWorkstationDef;
            pawnDef = golemDef.golemDef;
            if (golemDef.golemFramePath != null)
            {
                frameMat = MaterialPool.MatFrom(golemDef.golemFramePath, true);
            }
            upgrades.Clear();
            foreach(TM_GolemUpgradeDef gud in golemDef.upgrades)
            {
                TM_GolemUpgrade gu = new TM_GolemUpgrade(gud);
                upgrades.Add(gu);
            }
            needs.Clear();
            needs.AddRange(golemDef.needs);
            hediffs.Clear();
            hediffs.AddRange(golemDef.hediffs);
        }

        public TM_Golem(TM_Golem fromGolem, Thing thing)
        {
            golemDef = TM_GolemUtility.GetGolemDefFromThing(thing);
            activationTicks = golemDef.activationTicks;
            processorEvaluationTicks = golemDef.processorEvaluationTicks;
            minimumEnergyPctToActivate = golemDef.minimumEnergyPctToActivate;
            workstationDef = golemDef.golemWorkstationDef;
            pawnDef = golemDef.golemDef;
            if (golemDef.golemFramePath != null)
            {
                frameMat = MaterialPool.MatFrom(golemDef.golemFramePath, true);
            }
            upgrades.Clear();
            upgrades.AddRange(fromGolem.upgrades);
            needs.Clear();
            needs.AddRange(golemDef.needs);
            hediffs.Clear();
            hediffs.AddRange(golemDef.hediffs);            
        }

        public Material GetGolemFrameMat(Thing thing)
        {
            if (frameMat == null)
            {
                TM_GolemDef golemDef = TM_GolemUtility.GetGolemDefFromThing(thing);
                if (golemDef.golemFramePath != null && golemDef.golemFramePath != "")
                {
                    frameMat = MaterialPool.MatFrom(golemDef.golemFramePath, true);
                }
            }
            return frameMat;
        }
    }
}