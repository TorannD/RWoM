using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace TorannMagic.ModCheck
{
    public class Validate
    {
        public static class PrisonLabor { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("PrisonLabor"); }

        public static class PrisonLaborOutdated { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("PrisonLaborOutdated"); }

        public static class RimOfMadness_Vampires { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("RimOfMadness_Vampires"); }

        public static class ChildrenSchoolLearning { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("ChildrenSchoolLearning"); }

        public static class AlienHumanoidRaces { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("AlienHumanoidRaces"); }

        public static class GiddyUp
        {

            public static bool Core_IsInitialized()
            {
                return Compatibility.EquipmentCompatibility.MountingAvailable();
            }

            public static bool BM_IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("GiddyUpBattleMounts");
        }

        public static class SRTS_Expanded { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("SRTS_Expanded"); }

        public static class DualWield { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("DualWield"); }

        public static class SimpleSidearms
        {
            public static bool IsInitialized()
            {
                return Compatibility.EquipmentCompatibility.WeaponMemoryAvailable();
            }
        }

        public static class SOS2 { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("SOS2"); }

        public static class NoJobAuthors { public static bool IsInitialized() => Compatibility.OptionalCompatibility.IsEnabled("NoJobAuthors"); }
    }
}


