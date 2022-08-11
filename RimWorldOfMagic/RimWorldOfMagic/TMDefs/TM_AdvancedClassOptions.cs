using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace TorannMagic.TMDefs
{
    public class TM_AdvancedClassOptions
    {
        public bool requiresBaseClass = false;
        public bool canSpawnWithClass = false;
        public bool removesRequiredTrait = false;
        public List<TraitDef> requiredTraits = new List<TraitDef>();
        public List<TraitDef> disallowedTraits = new List<TraitDef>();

    }
}