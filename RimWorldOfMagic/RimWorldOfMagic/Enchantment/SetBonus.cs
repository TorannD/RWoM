using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using System.Xml;
using Verse;

namespace TorannMagic.Enchantment
{
    public class SetBonus
    {
        public HediffDef appliedHediff = null;
        public float appliedHediffSeverity = 1f;
        public TMAbilityDef appliedAbility = null;
        public bool setFromStuff = false;
        public List<ThingDef> setThingDefs = null;
        public int requiredSetCount = 1;
    }
}
