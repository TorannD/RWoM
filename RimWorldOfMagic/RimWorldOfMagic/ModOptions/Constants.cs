using System;
using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using AbilityUser;

namespace TorannMagic.ModOptions
{ 
    public abstract class Constants
    {
        private static bool pawnInFlight = false;

        public static bool GetPawnInFlight()
        {
            return pawnInFlight;
        }

        public static bool SetPawnInFlight(bool inFlight)
        {
            pawnInFlight = inFlight;
            return true;
        }

        static List<IntVec3> growthCells = new List<IntVec3>();

        public static List<IntVec3> GetGrowthCells()
        {
            return growthCells;
        }

        public static List<IntVec3> SetGrowthCells(List<IntVec3> cells)
        {
            growthCells.AddRange(cells);
            return growthCells;
        }

        public static void RemoveGrowthCell(IntVec3 cell)
        {
            growthCells.Remove(cell);
        }

        private static int lastGrowthMoteTick;

        public static void SetLastGrowthMoteTick(int value)
        {
            lastGrowthMoteTick = value;
        }

        public static int GetLastGrowthMoteTick()
        {
            return lastGrowthMoteTick;
        }

        private static int technoWeaponCount;

        public static void SetTechnoWeaponCount(int value)
        {
            technoWeaponCount = value;
        }

        public static int GetTechnoWeaponCount()
        {
            return technoWeaponCount;
        }

        private static bool bypassPrediction = false;

        public static bool GetBypassPrediction()
        {
            return bypassPrediction;
        }

        public static bool SetBypassPrediction(bool value)
        {
            bypassPrediction = value;
            return bypassPrediction;
        }

        static List<Pawn> overdrivePawns = new List<Pawn>();

        public static bool ClearOverdrivePawns()
        {
            overdrivePawns.Clear();
            return overdrivePawns.Count == 0;
        }

        public static List<Pawn> SetOverdrivePawnList(List<Pawn> value)
        {            
            if(overdrivePawns == null)
            {
                overdrivePawns = new List<Pawn>();
            }
            for(int i = 0; i < value.Count; i++)
            {
                overdrivePawns.AddDistinct<Pawn>(value[i]);
            }
            return overdrivePawns;
        }

        public static List<Pawn> GetOverdrivePawnList()
        {
            if(overdrivePawns == null)
            {
                overdrivePawns = new List<Pawn>();
                overdrivePawns.Clear();
            }
            return overdrivePawns;
        }

        private static int pistolSpecCount;

        public static void SetPistolSpecCount(int value)
        {
            pistolSpecCount = value;
            if (pistolSpecCount >= 20)
            {
                pistolSpecCount = 0;
            }
        }

        public static int GetPistolSpecCount()
        {
            return pistolSpecCount;
        }

        private static int rifleSpecCount;

        public static void SetRifleSpecCount(int value)
        {
            rifleSpecCount = value;
            if (rifleSpecCount >= 20)
            {
                rifleSpecCount = 0;
            }
        }

        public static int GetRifleSpecCount()
        {
            return rifleSpecCount;
        }

        private static int shotgunSpecCount;

        public static void SetShotgunSpecCount(int value)
        {
            shotgunSpecCount = value;
            if(shotgunSpecCount >= 20)
            {
                shotgunSpecCount = 0;
            }
        }

        public static int GetShotgunSpecCount()
        {
            return shotgunSpecCount;
        }

        private static List<Texture> cloaks;
        public static List<Texture> GetCloaks()
        {
            return cloaks;            
        }

        public static List<Texture> cloaksNorth;
        public static List<Texture> GetCloaksNorth()
        {
            return cloaksNorth;
        }

        public static void InitializeCloaks()
        {
            Constants.cloaks = new List<Texture>();
            Constants.cloaks.Clear();
            Constants.cloaksNorth = new List<Texture>();
            Constants.cloaksNorth.Clear();
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/opencloak_Female_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/opencloak_Hulk_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/opencloak_Male_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/opencloak_Thin_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/opencloak_Fat_north").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Female_east").mainTexture);            
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Female_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Fat_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Fat_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Hulk_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Hulk_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Thin_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Thin_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Male_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_Male_south").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Female_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Hulk_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Male_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Thin_north").mainTexture);
            Constants.cloaksNorth.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Fat_north").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Female_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Female_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Fat_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Fat_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Hulk_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Hulk_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Thin_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Thin_south").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Male_east").mainTexture);
            Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_Male_south").mainTexture);
            if(ModsConfig.IsActive("ssulunge.BBBodySupport"))
            {
                Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_FemaleBB_east").mainTexture);
                Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_FemaleBB_south").mainTexture);
                Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/demonlordcloakc_FemaleBB_north").mainTexture);
                Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_FemaleBB_east").mainTexture);
                Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_FemaleBB_south").mainTexture);
                Constants.cloaks.Add(MaterialPool.MatFrom("Equipment/opencloak_FemaleBB_south").mainTexture);
            }
            Constants.cloaks.AddRange(cloaksNorth);
        }

        public static Dictionary<string,IEnumerable<Gizmo>> reducedGizmoList;
        public static IEnumerable<Gizmo> GetReducedDraftGizmos(string hash, IEnumerable<Gizmo> list)
        {            
            if(reducedGizmoList == null)
            {
                reducedGizmoList = new Dictionary<string, IEnumerable<Gizmo>>();
            }
            if (hash == "clear")
            {
                reducedGizmoList.Clear();
                return null;
            }
            if (!reducedGizmoList.ContainsKey(hash))
            {
                List<Gizmo> glist = list.ToList();
                for (int i = 0; i < glist.Count; i++)
                {
                    if (glist[i].GetType() == typeof(Command_PawnAbility))
                    {
                        glist.Remove(glist[i]);
                        i--;
                    }
                }
                reducedGizmoList.Add(hash, glist);
            }
            if(reducedGizmoList != null)
            {
                return reducedGizmoList[hash];
            }
            else
            {
                return null;
            }
        }

        private static Pawn iconPawn = null;
        private static Dictionary<PawnAbility, int> iconOffset = new Dictionary<PawnAbility, int>();
        public static int GetGizmoCount(Pawn p, PawnAbility pa)
        {
            if(iconPawn != p)
            {
                iconPawn = p;
                iconOffset.Clear();
                iconAnchorY = 0;
                iconAnchorX = 0;
            }

            if (iconOffset.ContainsKey(pa))
            {
                return iconOffset[pa];
            }
            else
            {
                int count = iconOffset.Count;
                iconOffset.Add(pa, count);
                return count;
            }            
        }

        private static float iconAnchorX = 0f;
        private static float iconAnchorY = 0f;
        public static void IconAnchor(Rect r)
        {
            if(iconAnchorX == 0)
            {
                iconAnchorX = r.x;
            }
            else if (iconAnchorX > r.x)
            {
                iconAnchorX = r.x;                
            }
            if(iconAnchorY == 0)
            {
                iconAnchorY = r.y;
            }
            else if(iconAnchorY > r.y)
            {
                iconAnchorY = r.y;
            }
        }

        public static Vector2 GetIconVector()
        {
            return new Vector2(iconAnchorX + Settings.Instance.iconPosition.x, iconAnchorY + Settings.Instance.iconPosition.y);
        }
    }
}
