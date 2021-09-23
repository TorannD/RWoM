using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using AbilityUser;
using System.Reflection;
using HarmonyLib;

namespace TorannMagic.Golems
{
    public class CompGolemEnergyHandler : CompPowerBattery
    {
        private static readonly Vector2 EnergyBarSize = new Vector2(.32f, 0.18f);
        private float storedEnergyUpgradeCount = 0;
        //private float storedEnergy = ;
        private float conversionEfficiencyUpgradeCount = 0;

        public static FieldInfo storedEnergyRef = typeof(CompGolemEnergyHandler).GetField("storedEnergy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

        public override void PostExposeData()
        {
            base.PostExposeData();
            //Scribe_Values.Look(ref storedEnergy, "storedEnergy", 0f);
            Scribe_Values.Look(ref storedEnergyUpgradeCount, "storedEnergyUpgradeCount", 0f);
            Scribe_Values.Look(ref conversionEfficiencyUpgradeCount, "conversionEfficiencyUpgradeCount", 0f);
        }

        //public float StoredEnergy
        //{
        //    get
        //    {
        //        return StoredEnergyRef = Mathf.Clamp(StoredEnergyRef, 0f, StoredEnergyMax);
        //    }
        //}
        public float StoredEnergyMax => Props.storedEnergyMax + (Props.storedEnergyMaxUpgradeIncrease * storedEnergyUpgradeCount);
        public new float StoredEnergyPct => StoredEnergy / StoredEnergyMax;
        public bool CanUpgrade_EnergyMax => storedEnergyUpgradeCount < Props.storedEnergyMaxUpgrades;
        public void Upgrade_StoredEnergy()
        {
            storedEnergyUpgradeCount++;
        }

        public new void AddEnergy(float amount)
        {
            Traverse.Create(root: this).Field(name: "storedEnergy").SetValue(Mathf.Clamp(StoredEnergy + (amount * ConversionEfficiency), 0f, StoredEnergyMax));
        }

        public float ConversionEfficiency => Props.conversionEfficiency + (Props.conversionEfficiencyUpgradeIncrease * conversionEfficiencyUpgradeCount);
        public void Upgrade_ConversionEfficiency()
        {
            conversionEfficiencyUpgradeCount++;
        }

        public new CompProperties_GolemEnergyHandler Props
        {
            get
            {
                return (CompProperties_GolemEnergyHandler)this.props;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);            
        }

        public override void CompTick()
        {
            base.CompTick();
            RegenPowerSelf();
        }

        public void RegenPowerSelf()
        {
            AddEnergy(Props.selfChargePerHour / 2500f);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = parent.DrawPos;
            r.center.x += .01f;
            r.center.z += -.25f;
            r.size = EnergyBarSize;
            r.fillPercent = StoredEnergyPct;
            r.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Props.energyColor);
            r.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.clear);// new Color(0.4f, 0.4f, 0.4f));
            r.margin = 0.15f;
            Rot4 rotation = parent.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
        }

        //public override string CompInspectStringExtra()
        //{
        //    string text = "PowerBatteryStored".Translate() + ": " + StoredEnergy.ToString("F0") + " / " + StoredEnergyMax.ToString("F0") + " Wd";
        //    text += "\n" + "PowerBatteryEfficiency".Translate() + ": " + (ConversionEfficiency * 100f).ToString("F0") + "%";
        //    return text + "\n" + base.CompInspectStringExtra();
        //}

        //public override IEnumerable<Gizmo> CompGetGizmosExtra()
        //{
        //    foreach (Gizmo item in base.CompGetGizmosExtra())
        //    {
        //        yield return item;
        //    }
        //    if (Prefs.DevMode)
        //    {
        //        Command_Action command_Action = new Command_Action();
        //        command_Action.defaultLabel = "DEBUG: Fill";
        //        command_Action.action = delegate
        //        {
        //            AddEnergy(StoredEnergyMax);
        //        };
        //        yield return command_Action;
        //        Command_Action command_Action2 = new Command_Action();
        //        command_Action2.defaultLabel = "DEBUG: Empty";
        //        command_Action2.action = delegate
        //        {
        //            StoredEnergy = 0f;
        //        };
        //        yield return command_Action2;
        //    }
        //}
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //initializes after reload
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
        }

    }
}
