using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.TMDefs
{
    public class DefModExtension_TraitEnchantments : DefModExtension
    {
        public float maxMP = 0;
        public float mpRegenRate = 0;
        public float magicCooldown = 0;
        public float mpCost = 0;        
        public float arcaneDmg = 0;

        public float maxSP = 0;
        public float spRegenRate = 0;
        public float mightCooldown = 0;
        public float spCost = 0;
        public float combatDmg = 0;

        public float xpGain = 0;
        public float arcaneRes = 0;

    }
}
