using Verse;
using RimWorld;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemEnergyHandler : CompProperties_Battery
    {        

        public float storedEnergyMax = 1000f;
        public float storedEnergyMaxUpgradeIncrease = 500f;
        public int storedEnergyMaxUpgrades = 3;

        public float conversionEfficiency = .5f;
        public float conversionEfficiencyUpgradeIncrease = .1f;
        public int conversionEfficiencyMaxUpgrades = 3;

        public float selfChargePerHour = 0f;

        public Color energyColor = Color.blue;

        public bool electricalConverter = false;
        public bool manaConverter = false;

        public CompProperties_GolemEnergyHandler()
        {
            this.compClass = typeof(CompGolemEnergyHandler);
        }
    }
}
