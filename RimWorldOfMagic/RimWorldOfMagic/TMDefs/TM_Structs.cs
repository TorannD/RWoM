using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TorannMagic.TMDefs
{
    public struct Encase : IExposable
    {
        public IntVec3 position;
        public TerrainDef terrain;

        public Encase(IntVec3 pos, TerrainDef ter)
        {
            position = pos;
            terrain = ter;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<TerrainDef>(ref this.terrain, "terrain");
            Scribe_Values.Look<IntVec3>(ref this.position, "position", default(IntVec3), false);
        }
    }

    //public struct Branding : IExposable
    //{
    //    public Pawn pawn;
    //    public HediffDef hediffDef;

    //    public Branding(Pawn p, HediffDef hd_def)
    //    {
    //        pawn = p;
    //        hediffDef = hd_def;
    //    }

    //    public void ExposeData()
    //    {
    //        Scribe_Defs.Look<HediffDef>(ref this.hediffDef, "hediffDef");
    //        Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
    //    }
    //}

    //Path Struct
    public struct TPath
    {
        public List<IntVec3> pathList;
        public int pathParent;
        public int pathParentSplitIndex;
        public bool ended;
        public IntVec3 currentCell;

        public TPath(int parent, int splitIndex, bool end, IntVec3 curCell, List<IntVec3> list)
        {
            pathParent = parent;
            ended = false;
            pathList = list;
            currentCell = curCell;
            pathParentSplitIndex = splitIndex;
        }
    }

    //public struct GolemUpgrades : IExposable
    //{
    //    public TM_GolemUpgrade upgrade;
    //    public int upgradeLevel;

    //    public GolemUpgrades(TM_GolemUpgrade name, int level)
    //    {
    //        upgrade = name;
    //        upgradeLevel = level;
    //    }

    //    public void ExposeData()
    //    {
    //        Scribe_Deep.Look<TM_GolemUpgrade>(ref this.upgrade, "upgrade");
    //        Scribe_Values.Look<int>(ref this.upgradeLevel, "upgradeLevel");
    //    }
    //}

    public class DrawMesh 
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 vector;
        public int fadeInTicks;
        public int fadeOutTicks;
        public int solidTicks;
        public int age;
        public float angle;
        public float length;
        public Material mat;

        public DrawMesh(Material _mat, Vector3 _start, Vector3 _end, int _fadeIn, int _fadeOut, int _solid)
        {
            start = _start;
            end = _end;
            fadeInTicks = _fadeIn;
            fadeOutTicks = _fadeOut;
            solidTicks = _solid;
            age = 0;
            angle = 0f;          
            length = (end - start).magnitude;
            mat = _mat;
            GetVector(start, end);
            vector = start + (Vector3Utility.FromAngleFlat(this.angle - 90) * length * 0.25f);
        }

        public int Duration => fadeInTicks + solidTicks + fadeOutTicks;

        public float MeshBrightness
        {
            get
            {
                float result;
                if (this.age <= this.fadeInTicks)
                {
                    result = (float)this.age / this.fadeInTicks;
                }
                else if (this.age < solidTicks)
                {
                    result = 1f;
                }
                else
                {
                    result = 1f - (float)(this.age - this.solidTicks) / (float)this.fadeOutTicks;
                }
                return result;
            }
        }

        public Vector3 GetVector(Vector3 start, Vector3 end)
        {
            Vector3 heading = (end - start);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            this.angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
            return direction;
        }

        public void Draw()
        {
            age++;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.Euler(0f, this.angle, 0f), new Vector3(1f, 1f, length * .5f));   //drawer for beam
            if (this.start != default(Vector3))
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, FadedMaterialPool.FadedVersionOf(mat, this.MeshBrightness), 0);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, this.start, Quaternion.identity, FadedMaterialPool.FadedVersionOf(mat, this.MeshBrightness), 0);
            }
        }
    }
}
