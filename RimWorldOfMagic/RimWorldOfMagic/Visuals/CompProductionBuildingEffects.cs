using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Visuals
{
    public enum ProductionBuildingEffect { Gemcutting, Scribing, Forge, MagicCircle, SmallMagicCircle }

    public class CompProperties_ProductionBuildingEffects : CompProperties
    {
        public ProductionBuildingEffect effect;
        public CompProperties_ProductionBuildingEffects() { compClass = typeof(CompProductionBuildingEffects); }

        // Graphic_Multi uses directional sprites, not a quaternion rotation of the north image.
        // These anchors are measured in each sprite's rectangle (including transparent padding).
        public Vector3 AnchorOffset(Rot4 direction, Vector2 drawnSize)
        {
            if (effect == ProductionBuildingEffect.Scribing) return new Vector3(0, 0, -.05f * drawnSize.y);
            if (effect != ProductionBuildingEffect.Gemcutting) return Vector3.zero;
            if (direction == Rot4.North) return new Vector3(0, 0, .14f * drawnSize.y);
            if (direction == Rot4.East) return new Vector3(.06f * drawnSize.x, 0, .03f * drawnSize.y);
            if (direction == Rot4.West) return new Vector3(-.06f * drawnSize.x, 0, .03f * drawnSize.y);
            return Vector3.zero;
        }
    }

    // Transient presentation state only. No serialized values, world scans or resource mutations.
    public class CompProductionBuildingEffects : ThingComp
    {
        private int workTick = -1, sampleTick = -1;
        private float visualStrength;
        private Faction owner;
        public int WorkTick => workTick;
        public float VisualStrength => visualStrength;
        public CompProperties_ProductionBuildingEffects Props => (CompProperties_ProductionBuildingEffects)props;
        bool Eligible => parent != null && parent.Spawned && !parent.Destroyed
            && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown();
        void Clear() { workTick = sampleTick = -1; visualStrength = 0; owner = null; }
        void CheckOwner()
        {
            if (owner == parent?.Faction) return;
            Clear(); owner = parent?.Faction;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad) { Clear(); owner = parent.Faction; }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish) { Clear(); }
        public override void PostExposeData() { if (Scribe.mode == LoadSaveMode.PostLoadInit) Clear(); }
        public void NotifyWork(int now)
        {
            CheckOwner();
            if (Eligible && now >= 0) workTick = now;
        }
        public float Sample(int now)
        {
            CheckOwner();
            if (now < 0 || now < sampleTick || now < workTick)
            {
                Clear(); owner = parent?.Faction;
            }
            bool eligible = Eligible;
            if (!eligible) workTick = -1;
            var circle = parent as Building_TMMagicCircleBase;
            bool active = eligible && (circle != null ? circle.IsActive : workTick >= 0 && (long)now - workTick <= 1);
            if (sampleTick < 0) sampleTick = now;
            float step = Math.Min(66L, Math.Max(0L, (long)now - sampleTick)) / 66f;
            visualStrength = Mathf.MoveTowards(visualStrength, active ? 1 : 0, step);
            sampleTick = now;
            return visualStrength;
        }
        public override void CompTick() { Sample(Find.TickManager.TicksGame); }
        public override void CompTickInterval(int delta) { CompTick(); }
        public override void PostDraw()
        {
            if (!parent.Spawned || parent.Map != Find.CurrentMap) return;
            int now = Find.TickManager.TicksGame;
            float strength = Sample(now);
            if (strength <= 0) return;
            Graphic graphic = parent.Graphic;
            Vector2 size = parent.def.graphicData.drawSize;
            if (parent.Rotation.IsHorizontal) size = new Vector2(size.y, size.x);
            Vector3 position = parent.DrawPos + graphic.DrawOffset(parent.Rotation) + Props.AnchorOffset(parent.Rotation, size);
            position.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + .012f;
            ProductionBuildingEffectsRenderer.Draw(Props.effect, position, size, parent.Rotation, now / 60f, strength);
        }
    }
}
