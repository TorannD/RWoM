using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public enum CrystalGlowState { Service, Reservoir, Power, Dome }

    public class CompProperties_CrystalGlow : CompProperties
    {
        public CrystalGlowState state = CrystalGlowState.Service;
        // Texture-space anchors, independent for each Graphic_Multi sprite (not a rotated world offset).
        public Vector2 north = new Vector2(.5f, .3f), east = new Vector2(.5f, .3f),
            south = new Vector2(.5f, .3f), west = new Vector2(.5f, .3f);
        public float diameter = .6f;
        public CompProperties_CrystalGlow() { compClass = typeof(CompCrystalGlow); }
        public Vector3 Offset(Rot4 rotation, Vector2 drawSize)
        {
            Vector2 anchor = rotation == Rot4.East ? east : rotation == Rot4.South ? south : rotation == Rot4.West ? west : north;
            return new Vector3((anchor.x - .5f) * drawSize.x, .04f, (.5f - anchor.y) * drawSize.y);
        }
        public Vector3 WorldOffset(Rot4 rotation, Vector2 drawSize, Quaternion graphicRotation, Vector3 graphicOffset)
            => graphicOffset + graphicRotation * Offset(rotation, drawSize);
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef)) yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(diameter) || diameter > 5f)
                yield return "Crystal glow diameter must be finite and between zero and five cells.";
            foreach (var anchor in new[] { north, east, south, west })
                if (!(anchor.x >= 0f && anchor.x <= 1f && anchor.y >= 0f && anchor.y <= 1f))
                    yield return "Crystal glow anchors must be normalized texture coordinates.";
            if (parentDef.drawerType == DrawerType.MapMeshOnly)
                yield return "Crystal glow requires dynamic comp drawing.";
        }
    }

    // No saved visual state, resource writes, world scans, or retained static things/maps.
    public class CompCrystalGlow : ThingComp
    {
        private int serviceTick = -1, serviceDuration, transferTick = -1, hitTick = -1, sampleTick = -1;
        private float level;
        private Faction owner;
        public CompProperties_CrystalGlow Props => (CompProperties_CrystalGlow)props;
        public float Level => level;
        public int TransferTick => transferTick;
        public int HitTick => hitTick;
        bool Eligible => parent != null && parent.Spawned && !parent.Destroyed && parent.Faction?.IsPlayer == true
            && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown();
        protected void CheckOwner()
        {
            if (owner == parent?.Faction) return;
            Clear();
            owner = parent?.Faction;
        }
        void Clear()
        {
            serviceTick = transferTick = hitTick = sampleTick = -1;
            serviceDuration = 0;
            level = 0f;
            owner = null;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad) { Clear(); owner = parent.Faction; }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish) { Clear(); }
        public override void PostExposeData() { if (Scribe.mode == LoadSaveMode.PostLoadInit) Clear(); }
        public void NotifyService(int now, int duration)
        {
            CheckOwner();
            if (!Eligible || now < 0 || duration <= 0) return;
            serviceTick = now;
            serviceDuration = duration;
        }
        public void NotifyTransfer(float actualMana, int now)
        {
            CheckOwner();
            if (Eligible && now >= 0 && CompMagofuelReservoir.PositiveFinite(actualMana)) transferTick = now;
        }
        public void NotifyHit(float absorbed, int now)
        {
            CheckOwner();
            if (Eligible && now >= 0 && CompMagofuelReservoir.PositiveFinite(absorbed)) hitTick = now;
        }
        public virtual bool Working(int now)
        {
            CheckOwner();
            if (!Eligible) return false;
            if (Props.state == CrystalGlowState.Reservoir)
            {
                var endpoint = parent.GetComp<CompManaNetworkEndpoint>();
                if (endpoint != null && endpoint.ManaMode == ManaNetworkMode.Off && endpoint.FuelMode == ManaNetworkMode.Off) return false;
                return (parent.GetComp<CompMagofuelReservoir>()?.StoredMana ?? 0f) > 0f || (parent.GetComp<CompRefuelable>()?.Fuel ?? 0f) > 0f;
            }
            if (Props.state == CrystalGlowState.Power) return (parent.GetComp<CompPowerTrader_Magofuel>()?.PowerOutput ?? 0f) > 0f;
            if (Props.state == CrystalGlowState.Dome) return parent.GetComp<CompGrandAtmosphereDome>()?.VisualCoverage == true;
            var oxygen = parent.GetComp<CompMagicalOxygen>();
            if (oxygen != null && !oxygen.CanOperate) return false;
            var combat = parent.GetComp<CompMagicalCombat>();
            if (combat != null && (!combat.CanOperate || combat.CurrentHitPoints <= 0f || now < combat.RebootUntilTick)) return false;
            var booster = parent.GetComp<CompMagicalCombatBooster>();
            if (booster != null && booster.CurrentHitPoints <= 0f) return false;
            return serviceTick >= 0 && now >= serviceTick && (long)now - serviceTick < serviceDuration;
        }
        public float Sample(int now)
        {
            bool working = Working(now);
            if (!Eligible) { serviceTick = transferTick = hitTick = -1; serviceDuration = 0; }
            if (sampleTick < 0 || now < sampleTick) { sampleTick = now; level = 0f; }
            float delta = Math.Min(120, Math.Max(0L, (long)now - sampleTick)) / 78f;
            level = working ? Math.Min(1f, level + delta) : Math.Max(0f, level - delta);
            sampleTick = now;
            return level;
        }
        public static float Envelope(int now, int started, int duration) => started < 0 || now < started || (long)now - started >= duration ? 0f : 1f - (now - started) / (float)duration;
        public override void CompTick() { Sample(Find.TickManager.TicksGame); }
        public override void CompTickInterval(int delta) { Sample(Find.TickManager.TicksGame); }
        public override void PostDraw()
        {
            if (!parent.Spawned || parent.Map != Find.CurrentMap) return;
            int now = Find.TickManager.TicksGame;
            float fade = Sample(now);
            float transfer = Eligible ? Envelope(now, transferTick, 51) : 0f;
            float hit = Eligible ? Envelope(now, hitTick, 33) : 0f;
            if (fade <= 0f && transfer <= 0f && hit <= 0f) return;
            float pulse = .7f + .12f * (float)Math.Sin((now + parent.thingIDNumber % 228) * (Math.PI * 2 / 228));
            // Single graphics rotate even when their building is not player-rotatable (e.g. a ship turn).
            // Fully directional Multi graphics provide identity here; their anchors already select the sprite.
            Graphic graphic = parent.Graphic;
            Vector3 position = parent.DrawPos + Props.WorldOffset(parent.Rotation, parent.def.graphicData.drawSize,
                graphic.QuatFromRot(parent.Rotation), graphic.DrawOffset(parent.Rotation));
            CrystalGlowRenderer.Draw(position, Props.diameter * (1f + .05f * pulse), fade * pulse * .45f, false);
            float flash = Math.Max(transfer, hit);
            if (flash > 0f) CrystalGlowRenderer.Draw(position, Props.diameter * (1.3f - .4f * flash), flash * .65f, false);
            if (hit > 0f) CrystalGlowRenderer.Draw(position, Props.diameter * (2f - 1.3f * hit), hit * .4f, true);
        }
    }

    [StaticConstructorOnStartup]
    public static class CrystalGlowRenderer
    {
        public const int Levels = 32;
        private static readonly Material[] halo = CreateMaterials(false), ring = CreateMaterials(true);
        static Material[] CreateMaterials(bool isRing)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = isRing ? "TM_CrystalGlowRing" : "TM_CrystalGlowHalo";
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                    float radius = (float)Math.Sqrt(dx * dx + dy * dy);
                    float alpha = isRing ? Mathf.Clamp01(1f - Math.Abs(radius - .72f) / .09f) : Mathf.Pow(Mathf.Clamp01(1f - radius), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            texture.Apply(false, true);
            var materials = new Material[Levels];
            for (int i = 0; i < Levels; i++)
                materials[i] = MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, new Color(.77f, .38f, 1f, i / (float)(Levels - 1)));
            return materials;
        }
        public static void Draw(Vector3 position, float diameter, float alpha, bool isRing)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(alpha * (Levels - 1)), 0, Levels - 1);
            if (index == 0) return;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(position, Quaternion.identity, new Vector3(diameter, 1f, diameter)), (isRing ? ring : halo)[index], 0);
        }
    }
}
