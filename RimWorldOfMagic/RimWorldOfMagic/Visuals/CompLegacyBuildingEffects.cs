using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Visuals
{
    public enum LegacyBuildingEffect { Capacitor, Pocket, Portal }

    public class CompProperties_LegacyBuildingEffects : CompProperties
    {
        public LegacyBuildingEffect effect;
        public CompProperties_LegacyBuildingEffects() { compClass = typeof(CompLegacyBuildingEffects); }

        // Coordinates are from the approved preview's original sprite rectangles.
        public Vector3 RuneOffset(int index, Vector2 size, bool mirrored = false)
        {
            float x = index == 0 ? 119 : index == 1 ? 148 : index == 2 ? 180 : index == 3 ? 212 : 242;
            float y = index == 0 || index == 4 ? 128 : index == 1 ? 95 : index == 2 ? 106 : 94;
            return new Vector3((x - 180) / 210f * size.x * (mirrored ? -1 : 1), 0, (160 - y) / 210f * size.y);
        }
        public Vector3 WorldOffset(Vector3 localOffset, Quaternion rotation, Vector3 graphicOffset)
            => graphicOffset + rotation * localOffset;
    }

    // Visual-only transient state; no resource mutation, saved state, or static world references.
    public class CompLegacyBuildingEffects : ThingComp
    {
        private int transferTick = -1;
        private int sampleTick = -1;
        private float visualCharge;
        private Faction owner;
        public int TransferTick => transferTick;
        public float VisualCharge => visualCharge;
        public CompProperties_LegacyBuildingEffects Props => (CompProperties_LegacyBuildingEffects)props;
        bool Eligible => parent != null && parent.Spawned && !parent.Destroyed
            && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown();
        void CheckOwner()
        {
            if (owner == parent?.Faction) return;
            Clear(); owner = parent?.Faction;
        }
        void Clear() { transferTick = sampleTick = -1; visualCharge = 0; owner = null; }
        public override void PostSpawnSetup(bool respawningAfterLoad) { Clear(); owner = parent.Faction; }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish) { Clear(); }
        public override void PostExposeData() { if (Scribe.mode == LoadSaveMode.PostLoadInit) Clear(); }
        public float Charge
        {
            get
            {
                CheckOwner();
                if (!Eligible) { transferTick = -1; return 0; }
                var portal = parent as Building_TMPortal;
                float value = portal != null ? (portal.IsPaired ? portal.ArcaneEnergyCur : 0) : parent.GetComp<CompRefuelable>()?.FuelPercentOfMax ?? 0;
                return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp01(value);
            }
        }
        public void NotifyTransfer(float actualMana, int now)
        {
            CheckOwner();
            if (Eligible && now >= 0 && actualMana > 0 && !float.IsInfinity(actualMana)) transferTick = now;
        }
        public void NotifyTeleport(int now)
        {
            if (parent is Building_TMPortal portal && portal.IsPaired) NotifyTransfer(1, now);
        }
        public float Pulse(int now)
        {
            CheckOwner();
            if (!Eligible || parent is Building_TMPortal portal && !portal.IsPaired) { transferTick = -1; return 0; }
            if (now < transferTick) { transferTick = -1; return 0; }
            return transferTick < 0 || (long)now - transferTick >= 66 ? 0 : 1 - (now - transferTick) / 66f;
        }
        public float Sample(int now)
        {
            float target = Charge;
            if (sampleTick < 0 || now < sampleTick) { sampleTick = now; visualCharge = 0; }
            float step = Math.Min(66L, Math.Max(0L, (long)now - sampleTick)) / 66f;
            visualCharge = Mathf.MoveTowards(visualCharge, target, step);
            sampleTick = now;
            return visualCharge;
        }
        public override void CompTick() { Sample(Find.TickManager.TicksGame); }
        public override void CompTickInterval(int delta) { CompTick(); }
        public override void PostDraw()
        {
            if (!parent.Spawned || parent.Map != Find.CurrentMap) return;
            int now = Find.TickManager.TicksGame;
            float charge = Sample(now), pulse = Pulse(now);
            if (charge <= 0 && pulse <= 0) return;
            Graphic graphic = parent.Graphic;
            Quaternion rotation = graphic.QuatFromRot(parent.Rotation);
            Vector3 position = parent.DrawPos + graphic.DrawOffset(parent.Rotation);
            position.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + .01f;
            LegacyBuildingEffectsRenderer.Draw(Props, position, rotation, parent.def.graphicData.drawSize,
                parent.Rotation == Rot4.West && graphic.WestFlipped || parent.Rotation == Rot4.East && graphic.EastFlipped,
                now / 60f, charge, pulse);
        }
    }
}
