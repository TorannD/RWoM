using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_ExtenderOrbit : CompProperties_CrystalGlow
    {
        public CompProperties_ExtenderOrbit()
        {
            compClass = typeof(CompExtenderOrbit);
            north = east = south = west = new Vector2(134.5f / 256f, 97.5f / 256f);
            diameter = 104f / 230f * 1.8f;
        }
    }

    // Native facility registration owns the extender limit. Only the accepted links contribute
    // to GetStatOffset; merely being in range or having a footprint is insufficient.
    public class CompExtenderOrbit : CompCrystalGlow
    {
        private CompGravshipFacility facility;
        private int activityTick = -1;
        private bool active;
        private bool Eligible(Thing thing) => thing != null && thing.Spawned && !thing.Destroyed
            && thing.Faction?.IsPlayer == true && FlickUtility.WantsToBeOn(thing) && !thing.IsBrokenDown();

        public bool HasAcceptedLink()
        {
            var engine = facility?.engine as Building_MagicalGravEngine;
            return Eligible(parent) && Eligible(engine) && engine.Map == parent.Map
                && engine.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading.Contains(parent) == true;
        }
        // Tick-only: native CanBeActive may enumerate engines; it must never be called by drawing.
        public void RefreshActivity(int now)
        {
            active = false;
            activityTick = now;
            if (!HasAcceptedLink()) return;
            float support = 0f;
            var offsets = facility.StatOffsets;
            if (offsets != null)
                for (int i = 0; i < offsets.Count; i++)
                    if (offsets[i].stat == StatDefOf.SubstructureSupport) support += offsets[i].value;
            active = CompMagofuelReservoir.PositiveFinite(support)
                && facility.engine.GetComp<CompAffectedByFacilities>().IsFacilityActive(parent);
        }
        public override bool Working(int now)
        {
            CheckOwner();
            return active && activityTick >= 0 && now >= activityTick
                && (long)now - activityTick <= 60 && HasAcceptedLink();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            facility = parent.GetComp<CompGravshipFacility>();
            activityTick = -1; active = false;
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            facility = null; activityTick = -1; active = false;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { activityTick = -1; active = false; }
        }
        public override void CompTick()
        {
            int now = Find.TickManager.TicksGame;
            if (activityTick < 0 || now < activityTick || (long)now - activityTick >= 60) RefreshActivity(now);
            Sample(now);
        }
        public override void CompTickInterval(int delta) { CompTick(); }
        public override void PostDraw()
        {
            if (!parent.Spawned || parent.Map != Find.CurrentMap) return;
            int now = Find.TickManager.TicksGame;
            float fade = Sample(now);
            if (fade <= 0f) return;
            Graphic graphic = parent.Graphic;
            Quaternion rotation = graphic.QuatFromRot(parent.Rotation);
            Vector3 position = parent.DrawPos + Props.WorldOffset(parent.Rotation, parent.def.graphicData.drawSize,
                rotation, graphic.DrawOffset(parent.Rotation));
            float seconds = (now + parent.thingIDNumber % 228) / 60f;
            CrystalGlowRenderer.Draw(position, Props.diameter, fade * 1.4f * (.4f + .09f * (float)Math.Sin(seconds * 1.7f)), false);
            ExtenderOrbitRenderer.Draw(position, rotation, seconds, fade);
        }
    }

    [StaticConstructorOnStartup]
    public static class ExtenderOrbitRenderer
    {
        public const int Levels = 32;
        public const float Brightness = 1.4f;
        private static readonly Mesh orbit = Ring(50f / 230f * 1.8f, 22f / 230f * 1.8f, 1.1f / 230f * 1.8f);
        private static readonly Mesh rune = Diamond(3f / 230f * 1.8f, 1.1f / 230f * 1.8f);
        private static readonly Material[] materials = Materials();
        private static Material[] Materials()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "TM_ExtenderOrbit" };
            texture.SetPixel(0, 0, Color.white); texture.Apply(false, true);
            var result = new Material[Levels];
            for (int i = 0; i < Levels; i++)
                result[i] = MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, new Color(192f / 255f, 129f / 255f, 1f, i / (float)(Levels - 1)));
            return result;
        }
        private static Mesh Ring(float rx, float rz, float width, int segments = 96, float phase = 0f)
        {
            var vertices = new Vector3[segments * 2]; var triangles = new int[segments * 6]; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < segments; i++)
            {
                float a = phase + i * Mathf.PI * 2f / segments;
                vertices[i * 2] = new Vector3(Mathf.Cos(a) * (rx - width / 2), 0, Mathf.Sin(a) * (rz - width / 2));
                vertices[i * 2 + 1] = new Vector3(Mathf.Cos(a) * (rx + width / 2), 0, Mathf.Sin(a) * (rz + width / 2));
                int n = (i + 1) % segments * 2, j = i * 6;
                triangles[j] = i * 2; triangles[j + 1] = n; triangles[j + 2] = i * 2 + 1;
                triangles[j + 3] = i * 2 + 1; triangles[j + 4] = n; triangles[j + 5] = n + 1;
            }
            var mesh = new Mesh { name = "TM_ExtenderOrbit", vertices = vertices, triangles = triangles, uv = uv };
            mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh Diamond(float radius, float width) => Ring(radius * 1.414214f, radius * 1.414214f, width, 4);
        public static void Draw(Vector3 position, Quaternion graphicRotation, float seconds, float fade)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(fade * .6f * Brightness * (Levels - 1)), 0, Levels - 1);
            if (index == 0) return;
            for (int i = 0; i < 2; i++)
            {
                Quaternion rotation = graphicRotation * Quaternion.AngleAxis((i == 0 ? 1f : -1f) * (.25f + seconds * .15f) * Mathf.Rad2Deg, Vector3.up);
                Graphics.DrawMesh(orbit, Matrix4x4.TRS(position, rotation, Vector3.one), materials[index], 0);
                for (int k = 0; k < 3; k++)
                {
                    float a = seconds * .65f + k * Mathf.PI * 2 / 3 + i;
                    Vector3 offset = rotation * new Vector3(Mathf.Cos(a) * 50f / 230f * 1.8f, .001f, Mathf.Sin(a) * 22f / 230f * 1.8f);
                    Graphics.DrawMesh(rune, Matrix4x4.TRS(position + offset, rotation, Vector3.one), materials[index], 0);
                }
            }
        }
    }
}
