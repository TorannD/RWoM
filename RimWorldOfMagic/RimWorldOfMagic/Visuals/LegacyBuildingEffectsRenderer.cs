using UnityEngine;
using Verse;

namespace TorannMagic.Visuals
{
    [StaticConstructorOnStartup]
    public static class LegacyBuildingEffectsRenderer
    {
        public const float Brightness = 1.4f;
        public const int Levels = 32;
        private static readonly Material[] materials = Materials();
        internal static readonly Mesh circle = Arc(Mathf.PI * 2, 96);
        private static readonly Mesh thread = Arc(1.15f, 24);
        internal static readonly Mesh rune = Arc(Mathf.PI * 2, 4, .22f);
        private static readonly Material[] halos = Halos();

        static Material[] Materials()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "TM_LegacyRunes" };
            texture.SetPixel(0, 0, Color.white); texture.Apply(false, true);
            return Palette(texture);
        }
        static Material[] Halos()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "TM_LegacyRuneHalo", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                    float a = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy)), 2);
                    texture.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            texture.Apply(false, true); return Palette(texture);
        }
        static Material[] Palette(Texture2D texture)
        {
            var result = new Material[Levels];
            for (int i = 0; i < Levels; i++)
                result[i] = MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, new Color( .80f, .55f, 1, i / (float)(Levels - 1)));
            return result;
        }
        static Mesh Arc(float length, int segments, float width = .015f)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * length / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[2 * i] = direction * (1 - width / 2);
                vertices[2 * i + 1] = direction * (1 + width / 2);
                if (i == segments) continue;
                int j = i * 6, n = i * 2;
                triangles[j] = n; triangles[j + 1] = n + 2; triangles[j + 2] = n + 1;
                triangles[j + 3] = n + 1; triangles[j + 4] = n + 2; triangles[j + 5] = n + 3;
            }
            var mesh = new Mesh { name = "TM_LegacyRuneGeometry", vertices = vertices, triangles = triangles, uv = uv };
            mesh.RecalculateBounds(); return mesh;
        }
        internal static void Mesh(Mesh mesh, Vector3 position, Quaternion rotation, float x, float z, float alpha, bool halo = false)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(alpha * Brightness * (Levels - 1)), 0, Levels - 1);
            if (index != 0) Graphics.DrawMesh(mesh, Matrix4x4.TRS(position, rotation, new Vector3(x, 1, z)), (halo ? halos : materials)[index], 0);
        }
        public static void Draw(CompProperties_LegacyBuildingEffects props, Vector3 position, Quaternion rotation,
            Vector2 size, bool mirrored, float seconds, float charge, float pulse)
        {
            if (props.effect == LegacyBuildingEffect.Capacitor)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 p = props.WorldOffset(props.RuneOffset(i, size, mirrored), rotation, position);
                    float lit = Mathf.Clamp01(charge * 5 - i);
                    Mesh(MeshPool.plane10, p, rotation, 32f / 210 * size.x, 32f / 210 * size.y, lit * .65f + pulse * .3f, true);
                    Mesh(rune, p, rotation, 3.5f / 210 * size.x, 5f / 210 * size.y, lit * .4f);
                }
                return;
            }
            if (props.effect == LegacyBuildingEffect.Pocket)
            {
                // Rotate the arc inside the fixed ellipse, rather than rotating its axes.
                for (int i = 0; i < 3; i++)
                {
                    float angle = seconds * .4f + i * Mathf.PI * 2 / 3;
                    DrawThread(position, rotation, size, angle, charge * .65f);
                }
                if (pulse > 0) Mesh(circle, position, rotation, (75 + (1 - pulse) * 35) / 230 * size.x,
                    (84 + (1 - pulse) * 35) / 230 * size.y, pulse * .55f);
                return;
            }
            Mesh(circle, position, rotation, 82f / 250 * size.x, 96f / 250 * size.y, charge * .48f);
            for (int i = 0; i < 10; i++)
            {
                float angle = seconds * .12f + i * Mathf.PI * 2 / 10;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 82f / 250 * size.x, .001f, Mathf.Sin(angle) * 96f / 250 * size.y);
                Mesh(rune, position + rotation * offset, rotation * Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up),
                    5f / 250 * size.x, 3.5f / 250 * size.y, charge * .7f + pulse * .25f);
            }
            if (pulse > 0)
            {
                float radius = (25 + (1 - pulse) * 110) / 250;
                Mesh(circle, position, rotation, radius * size.x, radius * size.y, pulse * .65f);
                Mesh(MeshPool.plane10, position, rotation, 130f / 250 * size.x, 130f / 250 * size.y, pulse * .38f, true);
            }
        }
        static void DrawThread(Vector3 position, Quaternion rotation, Vector2 size, float angle, float alpha)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(alpha * Brightness * (Levels - 1)), 0, Levels - 1);
            if (index == 0) return;
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, new Vector3(79.2f / 230 * size.x, 1, 88f / 230 * size.y))
                * Matrix4x4.Rotate(Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up));
            Graphics.DrawMesh(thread, matrix, materials[index], 0);
        }
    }
}
