using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using AbilityUser;

namespace TorannMagic.TMDefs
{
    public class TM_GolemUpgrade : IExposable
    {
        //Golem upgrade 
        public string name;
        public RecipeDef recipe;
        public BodyPartDef bodypart;
        public HediffDef hediff;
        public TM_GolemAbility ability;

        public string graphicsPath;
        public IntVec2 drawOffsetSouth = new IntVec2(0, 0);
        public IntVec2 drawOffsetNorth = new IntVec2(0, 0);
        public IntVec2 drawOffsetEast = new IntVec2(0, 0);
        public IntVec2 drawOffsetWest = new IntVec2(0, 0);

        public bool partRequiresUpgrade = false;
        public bool appliesToWorkstation = true;
        public int currentLevel = 0;
        public int maxLevel = 1;
        public WorkstationCapacity workstationCapacity = WorkstationCapacity.None;

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref this.name, "name");
            Scribe_Defs.Look<RecipeDef>(ref this.recipe, "recipe");
            Scribe_Defs.Look<BodyPartDef>(ref this.bodypart, "bodypart");
            Scribe_Defs.Look<HediffDef>(ref this.hediff, "hediff");
            Scribe_Deep.Look<TM_GolemAbility>(ref this.ability, "ability");

            Scribe_Values.Look<bool>(ref this.partRequiresUpgrade, "partRequiresUpgrade", false);
            Scribe_Values.Look<bool>(ref this.appliesToWorkstation, "appliesToWorkstation", true);
            Scribe_Values.Look<int>(ref this.currentLevel, "currentLevel", 0);
            Scribe_Values.Look<int>(ref this.maxLevel, "maxLevel", 1);
            Scribe_Values.Look<WorkstationCapacity>(ref this.workstationCapacity, "workstationCapacity", WorkstationCapacity.None);
        }
    }

    public enum WorkstationCapacity
    {
        None,           //default value
        EnergyMax,      //increases max energy of golem
        Efficiency,     //Improves energy efficiency
        Effect          //effect varies by golem, coded into building_name
    }
}