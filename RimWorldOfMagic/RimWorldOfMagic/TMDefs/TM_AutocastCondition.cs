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
    public class TM_AutocastCondition
    {
        //variables to be used with the condition checks
        public AutocastConditionClass conditionClass = AutocastConditionClass.Null;
        public bool invert = false;
        public bool onlyAppliesToCaster = false;
        public List<HediffDef> hediffDefs = null;
        public List<NeedDef> needDefs = null;
        public float valueA = 0f;
        public float valueB = 0f;
        public float valueC = 0f;        
    }    
}
