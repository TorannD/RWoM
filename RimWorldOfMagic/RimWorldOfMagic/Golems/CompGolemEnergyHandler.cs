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
        private float conversionEfficiencyUpgradeCount = 0;
        private float energyRegenerationFactor = 1f;
        private float storedEnergySaved;
        public bool canDrawPower = false;

        public bool CanDrawPower => canDrawPower || Props.canDrawPower;
        public static FieldInfo storedEnergyRef = typeof(CompGolemEnergyHandler).GetField("storedEnergy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

        public override void PostExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                storedEnergySaved = this.StoredEnergy;
            }
            base.PostExposeData();
            Scribe_Values.Look(ref storedEnergyUpgradeCount, "storedEnergyUpgradeCount", 0f);
            Scribe_Values.Look(ref conversionEfficiencyUpgradeCount, "conversionEfficiencyUpgradeCount", 0f);
            Scribe_Values.Look(ref energyRegenerationFactor, "energyRegenerationFactor", 1f);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                SetEnergy(storedEnergySaved);
            }
        }

        public float StoredEnergyMax => Props.storedEnergyMax + (Props.storedEnergyMaxUpgradeIncrease * storedEnergyUpgradeCount);
        public new float StoredEnergyPct => StoredEnergy / StoredEnergyMax;
        public bool CanUpgrade_EnergyMax => storedEnergyUpgradeCount < Props.storedEnergyMaxUpgrades;
        public void Upgrade_StoredEnergy(int level)
        {
            storedEnergyUpgradeCount = level;
        }

        public float ChargePerHour => Props.selfChargePerHour * energyRegenerationFactor;
        public void Upgrade_RegenerationFactor(int level)
        {
            energyRegenerationFactor = 1f + ((float)level * Props.selfChargeUpgradeFactor);
        }

        public new void AddEnergy(float amount)
        {
            float adjAmount;
            if(amount < 0)
            {
                adjAmount = amount / ConversionEfficiency;
            }
            else
            {
                adjAmount = amount * ConversionEfficiency;
            }
            Traverse.Create(root: this).Field(name: "storedEnergy").SetValue(Mathf.Clamp(StoredEnergy + adjAmount, 0f, StoredEnergyMax));
        }

        public void AddEnergyFlat(float amount)
        {
            Traverse.Create(root: this).Field(name: "storedEnergy").SetValue(Mathf.Clamp(StoredEnergy + amount, 0f, StoredEnergyMax));
        }

        public void SetEnergy(float amount)
        {
            Traverse.Create(root: this).Field(name: "storedEnergy").SetValue(Mathf.Clamp(amount, 0f, StoredEnergyMax));            
        }

        public float ConversionEfficiency => Props.conversionEfficiency + (Props.conversionEfficiencyUpgradeIncrease * conversionEfficiencyUpgradeCount);
        public void Upgrade_ConversionEfficiency(int level)
        {
            conversionEfficiencyUpgradeCount = level;
        }

        public new float AmountCanAccept
        {
            get
            {
                if(parent.IsBrokenDown())
                {
                    return 0f;
                }
                if(!Props.electricalConverter)
                {
                    return 0f;
                }
                return (StoredEnergyMax - StoredEnergy) / ConversionEfficiency;
            }
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
            if (CanDrawPower)
            {                
                base.CompTick();              
            }
            RegenPowerSelf();
        }

        public void DrawPowerNew(float amount)
        {
            SetEnergy(StoredEnergy - amount);
            if (StoredEnergy < 0f)
            {
                Log.Error("Drawing power we don't have from " + parent);
                SetEnergy(0);
            }
            if(StoredEnergy < 5)
            {
                if(this.canDrawPower)
                {
                    this.canDrawPower = false;
                    parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(this);
                }
            }
            if(!parent.GetComp<CompFlickable>().SwitchIsOn)
            {
                if (this.canDrawPower)
                {
                    this.canDrawPower = false;
                    parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(this);
                }
            }
        }

        public void RegenPowerSelf()
        {
            AddEnergy(ChargePerHour / 2500f);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = parent.DrawPos;
            r.center.x += Props.energyBarOffsetX;
            r.center.z += Props.energyBarOffsetY;
            r.size = EnergyBarSize;
            r.fillPercent = StoredEnergyPct;
            r.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Props.energyColor);
            r.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.clear);
            r.margin = 0.15f;
            Rot4 rotation = parent.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
        }

        public override string CompInspectStringExtra()
        {
            string text = "PowerBatteryStored".Translate() + ": " + StoredEnergy.ToString("F0") + " / " + StoredEnergyMax.ToString("F0") + " Wd";
            text += "\n" + "PowerBatteryEfficiency".Translate() + ": " + (ConversionEfficiency * 100f).ToString("F0") + "%";
            return text;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
        }

    }
}
