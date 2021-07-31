using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TorannMagic.TMDefs
{
    public sealed class TM_Hediff
    {
        //Hediff the recipe generates
        public HediffDef resultHediff = new HediffDef();
        //What the hediff applies to
        public bool applyFriendly = false;
        public bool applyEnemy = false;
        public bool applyNeutral = false;
        public bool applyNullFaction = false;
        //Checks spell penetration (of lead mage) and spell resistance of target
        //Fails count against the total application number
        public bool checkResistance = false;
        //Number of pawns the hediff applies to, 0 applies to all        
        public int maxHediffCount = 0;
        public float hediffSeverity = .5f;
        //Mote to display on target
        public ThingDef moteDef = null;

        //How many times to execute
        public IntRange countRange = new IntRange(1, 1);
    }
}
