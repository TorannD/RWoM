using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public enum ManaNetworkMode
    {
        Off,
        Send,
        Receive
    }

    public class CompProperties_ManaBeacon : CompProperties
    {
        public CompProperties_ManaBeacon()
        {
            compClass = typeof(CompManaBeacon);
        }
    }

    public class CompProperties_ManaNetworkEndpoint : CompProperties
    {
        public bool supportsMana = true, supportsFuel = true;
        public bool directConsumer;
        public ManaNetworkMode defaultManaMode = ManaNetworkMode.Send, defaultFuelMode = ManaNetworkMode.Send;
        public CompProperties_ManaNetworkEndpoint()
        {
            compClass = typeof(CompManaNetworkEndpoint);
        }
    }

    public class CompManaBeacon : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map?.GetComponent<MapComponent_ManaNetwork>().Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map?.GetComponent<MapComponent_ManaNetwork>().Unregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (parent.Spawned)
            {
                ManaNetworkOverlay.Draw(parent.def, parent.Position, parent.Rotation, parent.Map);
                parent.Map.GetComponent<MapComponent_ManaNetwork>().DrawLinks(this);
            }
        }

        public override string CompInspectStringExtra() => "TM_ManaBeaconInspect".Translate(parent.Spawned ? parent.Map.GetComponent<MapComponent_ManaNetwork>().ConnectedBeaconCount(this) : 0);
    }

    public class CompManaNetworkEndpoint : ThingComp
    {
        private ManaNetworkMode manaMode, fuelMode;
        private float manaTarget = 1f;
        // Zero means the device default, including old saves without these fields.
        private int manaPriority, fuelPriority;
        public CompProperties_ManaNetworkEndpoint Props => (CompProperties_ManaNetworkEndpoint)props;
        public ManaNetworkMode ManaMode { get => Props.supportsMana ? manaMode : ManaNetworkMode.Off; set => manaMode = ValidMode(value); }
        public ManaNetworkMode FuelMode { get => Props.supportsFuel ? fuelMode : ManaNetworkMode.Off; set => fuelMode = ValidMode(value); }
        public float ManaTarget { get => manaTarget; set => manaTarget = float.IsNaN(value) || float.IsInfinity(value) ? 1f : Math.Max(.1f, Math.Min(1f, value)); }
        public int ManaPriority { get => ResolvePriority(manaPriority); set => manaPriority = Math.Max(1, Math.Min(3, value)); }
        public int FuelPriority { get => ResolvePriority(fuelPriority); set => fuelPriority = Math.Max(1, Math.Min(3, value)); }
        int ResolvePriority(int value)
        {
            if (value >= 1 && value <= 3) return value;
            string name = parent?.def?.defName;
            if (parent?.GetComp<CompMagicalOxygen>() != null || parent?.GetComp<CompMagicalThermal>() != null
                || parent?.GetComp<CompGrandAtmosphereDome>() != null || name == "TM_MagicalOxygen"
                || name == "TM_ThermalShieldGenerator" || name == "TM_GrandAtmosphereDome") return 3;
            if (parent?.GetComp<CompMagicalCombat>() != null || name == "TM_CombatShieldGenerator") return 2;
            return 1;
        }
        public bool AutomaticManaReceiver => !Props.directConsumer && ManaMode == ManaNetworkMode.Receive
            && parent?.GetComp<CompMagofuelReservoir>() != null;
        public bool AutomaticFuelReceiver => !Props.directConsumer && FuelMode == ManaNetworkMode.Receive
            && MapComponent_ManaNetwork.CompatibleFuel(parent?.GetComp<CompRefuelable>());

        public IEnumerable<ManaNetworkMode> AvailableModes => Enum.GetValues(typeof(ManaNetworkMode)).Cast<ManaNetworkMode>().Where(mode => !Props.directConsumer || mode != ManaNetworkMode.Send);
        ManaNetworkMode ValidMode(ManaNetworkMode mode) => Props.directConsumer && mode == ManaNetworkMode.Send ? ManaNetworkMode.Receive : Enum.IsDefined(typeof(ManaNetworkMode), mode) ? mode : ManaNetworkMode.Off;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ManaMode = Props.defaultManaMode;
            FuelMode = Props.defaultFuelMode;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map?.GetComponent<MapComponent_ManaNetwork>().Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map?.GetComponent<MapComponent_ManaNetwork>().Unregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref manaMode, "TM_networkManaMode", Props.defaultManaMode);
            Scribe_Values.Look(ref fuelMode, "TM_networkFuelMode", Props.defaultFuelMode);
            Scribe_Values.Look(ref manaTarget, "TM_networkManaTarget", 1f);
            Scribe_Values.Look(ref manaPriority, "TM_networkManaPriority", 0);
            Scribe_Values.Look(ref fuelPriority, "TM_networkFuelPriority", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ManaMode = manaMode;
                FuelMode = fuelMode;
                ManaTarget = manaTarget;
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (parent.Spawned)
            {
                ManaNetworkOverlay.Draw(parent.def, parent.Position, parent.Rotation, parent.Map);
                parent.Map.GetComponent<MapComponent_ManaNetwork>().DrawEndpointLink(this);
            }
        }

        public override string CompInspectStringExtra()
        {
            string text = (parent.Spawned && parent.Map.GetComponent<MapComponent_ManaNetwork>().AttachedBeacon(this) != null ? "TM_ManaNetworkConnected" : "TM_ManaNetworkDisconnected").Translate();
            if (Props.supportsMana)
            {
                text += "\n" + "TM_ManaNetworkManaMode".Translate(ModeLabel(ManaMode));
                if (!Props.directConsumer)
                    text += " · " + "TM_ManaNetworkTarget".Translate(ManaTarget.ToString("P0"));
            }
            if (Props.supportsFuel)
            {
                text += "\n" + "TM_ManaNetworkFuelMode".Translate(ModeLabel(FuelMode));
                if (!Props.directConsumer && !MapComponent_ManaNetwork.CompatibleFuel(parent.GetComp<CompRefuelable>()))
                    text += "\n" + "TM_ManaNetworkUnsupportedFuel".Translate();
            }

            return text;
        }

        static string ModeLabel(ManaNetworkMode mode) => ("TM_ManaNetworkMode" + mode).Translate();
        static string PriorityLabel(int priority) => ("TM_ManaNetworkPriority" + priority).Translate();
        Command_Action PriorityCommand(bool mana) => new Command_Action
        {
            defaultLabel = (mana ? "TM_ManaNetworkManaPriority" : "TM_ManaNetworkFuelPriority").Translate(PriorityLabel(mana ? ManaPriority : FuelPriority)),
            defaultDesc = "TM_ManaNetworkPriorityDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get(mana ? "UI/transfermana" : "Items/Magofuel"),
            action = () =>
            {
                var options = new List<FloatMenuOption>();
                for (int rank = 1; rank <= 3; rank++)
                {
                    int selected = rank;
                    options.Add(new FloatMenuOption(PriorityLabel(rank), () =>
                    {
                        if (mana) ManaPriority = selected;
                        else FuelPriority = selected;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        Command_Action ModeCommand(bool mana) => new Command_Action
        {
            defaultLabel = (mana ? "TM_ManaNetworkManaMode" : "TM_ManaNetworkFuelMode").Translate(ModeLabel(mana ? ManaMode : FuelMode)),
            defaultDesc = "TM_ManaNetworkModeDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get(mana ? "UI/transfermana" : "Items/Magofuel"),
            action = () =>
            {
                var options = new List<FloatMenuOption>();
                foreach (ManaNetworkMode mode in AvailableModes)
                {
                    var selected = mode;
                    options.Add(new FloatMenuOption(ModeLabel(mode), () =>
                    {
                        if (mana)
                            ManaMode = selected;
                        else
                            FuelMode = selected;
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == null || !parent.Faction.IsPlayer)
                yield break;
            if (Props.supportsMana)
            {
                yield return ModeCommand(true);
                if (AutomaticManaReceiver) yield return PriorityCommand(true);
                if (!Props.directConsumer) yield return new Command_Action
                {
                    defaultLabel = "TM_ManaNetworkTarget".Translate(ManaTarget.ToString("P0")),
                    defaultDesc = "TM_ManaNetworkTargetDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
                    action = () =>
                    {
                        var options = new List<FloatMenuOption>();
                        for (int i = 1; i <= 10; i++)
                        {
                            float target = i / 10f;
                            options.Add(new FloatMenuOption(target.ToString("P0"), () => ManaTarget = target));
                        }

                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }

            if (Props.supportsFuel)
            {
                yield return ModeCommand(false);
                if (AutomaticFuelReceiver) yield return PriorityCommand(false);
            }
        }
    }
}
