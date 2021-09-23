using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace TorannMagic.TMDefs
{
    public class TM_Golem
    {
        //Golem Core Defs
        public ThingDef golemDef;
        public PawnKindDef golemKindDef;
        public ThingDef golemWorkstationDef;
        public ThingDef golemStuff;
        public int activationTicks = 240;

        public List<TM_GolemUpgrade> upgrades;
        public List<NeedDef> needs;
    }
}