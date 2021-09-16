using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace TorannMagic.TMDefs
{
    public class TM_GolemUpgrade //: IExposable
    {
        //Golem upgrade 
        public string name;
        public RecipeDef recipe;
        public BodyPartDef bodypart;
        public HediffDef hediff;

        public bool partRequiresUpgrade = false;
        public bool appliesToWorkstation = true;
        public int currentLevel = 0;
        public int maxLevel = 1;
        public WorkstationCapacity workstationCapacity = WorkstationCapacity.None;

        //public void ExposeData()
        //{
            
        //}
    }

    public enum WorkstationCapacity
    {
        None,           //default value
        EnergyMax,      //increases max energy of golem
        Efficiency,     //Improves energy efficiency
        Effect          //effect varies by golem, coded into building_name
    }
}