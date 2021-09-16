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

    public struct Branding : IExposable
    {
        public Pawn pawn;
        public HediffDef hediffDef;

        public Branding(Pawn p, HediffDef hd_def)
        {
            pawn = p;
            hediffDef = hd_def;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<HediffDef>(ref this.hediffDef, "hediffDef");
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn");
        }
    }

    public struct GolemUpgrades : IExposable
    {
        public TM_GolemUpgrade upgrade;
        public int upgradeLevel;

        public GolemUpgrades(TM_GolemUpgrade name, int level)
        {
            upgrade = name;
            upgradeLevel = level;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look<TM_GolemUpgrade>(ref this.upgrade, "upgrade");
            Scribe_Values.Look<int>(ref this.upgradeLevel, "upgradeLevel");
        }
    }

}
