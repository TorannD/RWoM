using UnityEngine;
using Verse;

namespace TorannMagic.Visuals
{
    // No simulation state or asset allocations: all meshes/materials are shared with the existing effects.
    public static class EtherSummonAnimation
    {
        public const int DurationTicks = 360;
        public static float Progress(int now, int start)
        {
            long elapsed = (long)now - start;
            return start < 0 || elapsed < 0 || elapsed > DurationTicks ? -1f : elapsed / (float)DurationTicks;
        }
        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }
        static void Halo(Vector3 p, float size, float alpha) => LegacyBuildingEffectsRenderer.Mesh(MeshPool.plane10, p, Quaternion.identity, size, size, alpha, true);
        public static void Draw(Thing gate, int start)
        {
            if (gate == null || !gate.Spawned || gate.Destroyed || gate.Map != Find.CurrentMap) return;
            float q = Progress(Find.TickManager.TicksGame, start);
            if (q < 0) return;
            float t = q * 6, fade = 1 - Smooth((t - 3.8f) / 1.6f);
            float vortex = Smooth((t - .8f) / 1.1f) * (1 - Smooth((t - 3.4f) / 1.5f));
            Vector3 center = gate.DrawPos + gate.Graphic.DrawOffset(gate.Rotation);
            center.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + .013f;
            Vector2 size = gate.def.graphicData.drawSize;
            for (int i = 0; i < 4; i++)
            {
                // Anchors measured on the approved single sprite; rotations do not rotate its image.
                Vector3 offset = i == 0 ? new Vector3(0, 0, .37f) : i == 1 ? new Vector3(.31f, 0, .01f)
                    : i == 2 ? new Vector3(0, 0, -.30f) : new Vector3(-.31f, 0, .01f);
                offset.x *= size.x; offset.z *= size.y;
                Halo(center + offset, size.x * .20f, Smooth((t - i * .22f) / .45f) * fade * .7f);
            }
            center.z += size.y * -.01f;
            Halo(center, size.x * .6f, vortex * .35f);
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 2.399f + t * (1 + t * .2f);
                float radius = size.x * (.07f + (i * 31 % 110) / 620f);
                Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius * .68f);
                Halo(p, size.x * (.018f + i % 3 * .006f), vortex * .85f);
            }
            for (int i = 0; i < 3; i++)
            {
                float r = size.x * .24f;
                LegacyBuildingEffectsRenderer.Mesh(EtherSummonMeshes.Spiral, center,
                    Quaternion.AngleAxis(t * t * 63 + i * 120, Vector3.up), r, r * .68f, vortex * .55f);
            }
            Halo(center, size.x * .52f, Mathf.Max(0, 1 - Mathf.Abs(t - 2.6f) / .45f) * .55f);
        }
    }

    [StaticConstructorOnStartup]
    public static class EtherSummonMeshes
    {
        public static readonly Mesh Spiral = BuildSpiral();
        static Mesh BuildSpiral()
        {
            const int segments = 48;
            var vertices = new Vector3[(segments + 1) * 2];
            var indices = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float q = i / (float)segments, angle = q * Mathf.PI * 2;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i * 2] = direction * (.15f + .85f * q - .012f);
                vertices[i * 2 + 1] = direction * (.15f + .85f * q + .012f);
                if (i == segments) continue;
                int n = i * 2, j = i * 6;
                indices[j] = n; indices[j + 1] = n + 2; indices[j + 2] = n + 1;
                indices[j + 3] = n + 1; indices[j + 4] = n + 2; indices[j + 5] = n + 3;
            }
            var mesh = new Mesh { name = "TM_EtherSummonSpiral", vertices = vertices, triangles = indices, uv = new Vector2[vertices.Length] };
            mesh.RecalculateBounds(); return mesh;
        }
    }
}
