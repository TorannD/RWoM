using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace TorannMagic.ModCheck
{
    public class Validate
    {
        public static class PrisonLabor
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {                    
                    if (p.Name == "Prison Labor")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class PrisonLaborOutdated
        {
            public static bool IsInitialized()
            {
                bool initialized = false;                
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Prison Labor 0.9 (Outdated)")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class RimOfMadness_Vampires
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Rim of Madness - Vampires")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class ChildrenSchoolLearning
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {                    
                    if (p.Name == "Children, school and learning")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class AlienHumanoidRaces
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Humanoid Alien Races 2.0")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class GiddyUp
        {

            public static bool Core_IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Giddy-up! Core")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }

            public static bool BM_IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Giddy-up! Battle Mounts")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class SRTS_Expanded
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "SRTS Expanded")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class DualWield
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Dual Wield")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class SimpleSidearms
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Simple sidearms")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class SOS2
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "Save Our Ship 2")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }

        public static class NoJobAuthors
        {
            public static bool IsInitialized()
            {
                bool initialized = false;
                foreach (ModContentPack p in LoadedModManager.RunningMods)
                {
                    if (p.Name == "No Job Authors")
                    {
                        initialized = true;
                    }
                }
                return initialized;
            }
        }
    }
}
