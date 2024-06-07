using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.TMDefs
{
    public class TM_GolemDef : Def
    {
        //public List<TM_Golem> golems = new List<TM_Golem>();
        public ThingDef golemDef = new ThingDef();
        public PawnKindDef golemKindDef = new PawnKindDef();
        public ThingDef golemWorkstationDef = new ThingDef();
        public ThingDef golemStuff = new ThingDef();
        public string golemFramePath = null;
        public int activationTicks = 240;
        public int processorEvaluationTicks = 100;
        public float minimumEnergyPctToActivate = .1f;

        public List<TM_GolemUpgradeDef> upgrades = new List<TM_GolemUpgradeDef>();
        public List<NeedDef> needs = new List<NeedDef>();
        public List<HediffDef> hediffs = new List<HediffDef>();

        public class GolemWorkTypes
        {
            public WorkTypeDef workTypeDef;
            public int priority = 0;
            public TM_GolemUpgradeDef golemUpgradeDef = null;
            public SkillDef upgradedSkill;
            public int initialSkillLevel = 0;
            public int skillBonusPerUpgrade = 1;
            public bool requiresUpgrade = false;
            public bool enabled = true;
        }

        public List<GolemWorkTypes> golemWorkTypes = new List<GolemWorkTypes>();

        public static TM_GolemDef Named(string defName)
        {
            return DefDatabase<TM_GolemDef>.GetNamed(defName);
        }
    }
}
