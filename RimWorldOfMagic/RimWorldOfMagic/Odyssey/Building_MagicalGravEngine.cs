using System.Linq;
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    // Only presentation differs: launch, cooldown, capture and save state stay native.
    public class Building_MagicalGravEngine : Building_GravEngine
    {
        // Weak references include packaged engines without retaining caravans or old games.
        private static readonly List<System.WeakReference<Building_MagicalGravEngine>> knownEngines = new List<System.WeakReference<Building_MagicalGravEngine>>();
        public Building_MagicalGravEngine()
        {
            for (int i = knownEngines.Count - 1; i >= 0; i--)
                if (!knownEngines[i].TryGetTarget(out var existing) || existing.Destroyed) knownEngines.RemoveAt(i);
            knownEngines.Add(new System.WeakReference<Building_MagicalGravEngine>(this));
        }

        internal static bool PlayerHasWorldEngine()
        {
            var objects = Find.WorldObjects.AllWorldObjects;
            for (int i = knownEngines.Count - 1; i >= 0; i--)
            {
                if (!knownEngines[i].TryGetTarget(out var engine) || engine.Destroyed)
                { knownEngines.RemoveAt(i); continue; }
                if (engine.Faction?.IsPlayer != true) continue;
                IThingHolder holder = engine.ParentHolder;
                while (holder != null)
                {
                    if (holder is RimWorld.Planet.WorldObject worldObject && !(worldObject is RimWorld.Planet.MapParent)
                        && worldObject.Faction?.IsPlayer == true && objects.Contains(worldObject)) return true;
                    holder = holder.ParentHolder;
                }
            }
            return false;
        }

        public override Graphic Graphic => DefaultGraphic;
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!AllComps.Any(comp => comp.DontDrawParent()))
            {
                if (def.drawerType == DrawerType.RealtimeOnly || !Spawned)
                    Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this);
                SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
            }
            Comps_DrawAt(drawLoc, flip);
            Comps_PostDraw();
        }
    }
}
