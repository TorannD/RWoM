using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_ShieldManaBuffer : CompProperties_MagofuelReservoir
    {
        public CompProperties_ShieldManaBuffer()
        {
            compClass = typeof(CompShieldManaBuffer);
            manaCapacity = 100f;
            maxTransfer = 10f;
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (!CompMagofuelReservoir.PositiveFinite(manaCapacity) || !CompMagofuelReservoir.PositiveFinite(maxTransfer))
                yield return "Shield mana buffer capacity and transfer rate must be finite and positive.";
        }
    }

    public class CompShieldManaBuffer : CompMagofuelReservoir
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            yield break;
        }

        public override string CompInspectStringExtra() => "TM_ShieldManaBuffer".Translate(StoredMana.ToString("0.##"), Props.manaCapacity.ToString("0.##"));
    }

    public class CompProperties_MagicalThermal : CompProperties
    {
        public float manaPerCellSecond = .001f;
        public float minTemperature = 21f;
        public int pulseInterval = 60;
        public CompProperties_MagicalThermal()
        {
            compClass = typeof(CompMagicalThermal);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(manaPerCellSecond) || float.IsNaN(minTemperature) || float.IsInfinity(minTemperature) || pulseInterval != MapComponent_MagicalThermal.Interval)
                yield return "Thermal generator requires finite temperature, positive mana rate and a 60-tick pulse.";
            if (!parentDef.HasComp(typeof(CompShieldManaBuffer)))
                yield return "Thermal generator requires a shield mana buffer.";
        }
    }

    public class CompMagicalThermal : ThingComp
    {
        private int lastSupplyTick = -1;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastSupplyTick, "TM_thermalLastSupplyTick", -1);
        }

        public float SupplyPulse(float demand)
        {
            if (!CanOperate || !CompMagofuelReservoir.PositiveFinite(demand))
                return 0f;
            int now = Find.TickManager.TicksGame;
            if (now < 0 || (lastSupplyTick < 0 ? now < Props.pulseInterval : (long)now - lastSupplyTick < Props.pulseInterval))
                return 0f;
            // Keep the receipt on the source across minification and map/gravship transfers.
            lastSupplyTick = now;
            return parent.GetComp<CompMagofuelReservoir>()?.WithdrawMana(demand) ?? 0f;
        }

        public CompProperties_MagicalThermal Props => (CompProperties_MagicalThermal)props;
        public bool CanOperate => parent != null && parent.Spawned && !parent.Destroyed && parent.Faction != null && parent.Faction.IsPlayer && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown() && Props.pulseInterval == MapComponent_MagicalThermal.Interval && CompMagofuelReservoir.PositiveFinite(Props.manaPerCellSecond) && !float.IsNaN(Props.minTemperature) && !float.IsInfinity(Props.minTemperature);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<MapComponent_MagicalThermal>().Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<MapComponent_MagicalThermal>().Unregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            var field = parent.Map?.GetComponent<MapComponent_MagicalThermal>();
            if (CanOperate && field != null && field.IsStandby(this))
                return "TM_ThermalStatusStandby".Translate();
            return "TM_ThermalFieldStrength".Translate((field?.StrengthFor(this) ?? 0f).ToStringPercent());
        }
    }
}
