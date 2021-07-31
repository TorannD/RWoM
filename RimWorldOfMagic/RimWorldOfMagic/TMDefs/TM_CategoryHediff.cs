using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TorannMagic.TMDefs
{
    public class TM_CategoryHediff
    {
        //Custom actions for purify, cure disease, and mechanite reprogramming
        public string hediffDefname = "";
        public bool containsDefnameString = false;
        public string replacementHediffDefname = "";
        public float replacementHediffSeverity = .5f;
        public bool removeOnCure = true;
        public float chanceToRemove = 1f;
        public float severityReduction = 0f;
        public int requiredSkillLevel = 0;
        public string requiredSkillName = "TM_Purify_ver";
        public float powerSkillAdjustment = 0.0f;
        public string powerSkillName = "TM_Purify_pwr";

    }
}
