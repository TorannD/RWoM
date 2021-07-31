using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_CatEyes : HediffComp_EnchantedItem
    {
        CompProperties_Glower gProps = new CompProperties_Glower();
        CompGlower glower = new CompGlower();
        IntVec3 oldPos = default(IntVec3);

        public ColorInt glowColor = new ColorInt(0, 205, 102, 1);

        public override void CompExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && this.oldPos != default(IntVec3))
            {
                this.Pawn.Map.mapDrawer.MapMeshDirty(oldPos, MapMeshFlag.Things);
                this.Pawn.Map.glowGrid.DeRegisterGlower(glower);
                this.glower = null;
            }
            base.CompExposeData();
            
        }

        public override void PostInitialize()
        {
            this.glower = new CompGlower();
            this.hediffActionRate = 10;
            gProps.glowColor = glowColor;
            gProps.glowRadius = 1.4f;
            glower.parent = this.Pawn;
            glower.Initialize(gProps);
        }

        public override void HediffActionTick()
        {
            if (this.glower != null && glower.parent != null)
            {
                if (this.Pawn != null && this.Pawn.Map != null && this.Pawn.Position != oldPos)
                {
                    if (oldPos != default(IntVec3))
                    {
                        this.Pawn.Map.mapDrawer.MapMeshDirty(oldPos, MapMeshFlag.Things);
                        this.Pawn.Map.glowGrid.DeRegisterGlower(glower);
                    }
                    if (this.Pawn.Map.skyManager.CurSkyGlow < 0.4f)
                    {
                        this.Pawn.Map.mapDrawer.MapMeshDirty(this.Pawn.Position, MapMeshFlag.Things);
                        this.Pawn.Map.glowGrid.RegisterGlower(glower);
                        oldPos = this.Pawn.Position;
                    }
                }
            }
            else
            {
                PostInitialize();
            }

        }

        public override void CompPostPostRemoved()
        {
            if (oldPos != default(IntVec3))
            {
                this.Pawn.Map.mapDrawer.MapMeshDirty(oldPos, MapMeshFlag.Things);
                this.Pawn.Map.glowGrid.DeRegisterGlower(glower);
            }
            base.CompPostPostRemoved();
        }
    }
}
