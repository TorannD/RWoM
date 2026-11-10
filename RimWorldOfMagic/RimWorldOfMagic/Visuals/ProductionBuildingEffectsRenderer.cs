using UnityEngine;
using Verse;

namespace TorannMagic.Visuals
{
    [StaticConstructorOnStartup]
    public static class ProductionBuildingEffectsRenderer
    {
        public const float Brightness = LegacyBuildingEffectsRenderer.Brightness;
        private static readonly Mesh thread = MakeThread();
        static Mesh MakeThread()
        {
            const int segments = 24;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                Vector3 p = ThreadPoint(i / (float)segments);
                vertices[2 * i] = p + new Vector3(0, 0, -.0025f);
                vertices[2 * i + 1] = p + new Vector3(0, 0, .0025f);
                if (i == segments) continue;
                int n = 2 * i, j = 6 * i;
                triangles[j] = n; triangles[j + 1] = n + 1; triangles[j + 2] = n + 2;
                triangles[j + 3] = n + 1; triangles[j + 4] = n + 3; triangles[j + 5] = n + 2;
            }
            var mesh = new Mesh { name = "TM_ProductionThread", vertices = vertices, triangles = triangles, uv = new Vector2[vertices.Length] };
            mesh.RecalculateBounds(); return mesh;
        }
        static Vector3 ThreadPoint(float q) => new Vector3((77 * (1-q) * (1-q) + 70 * q * (1-q)) / 260f, 0, 60 * q * (1-q) / 260f);
        static void Halo(Vector3 p, float diameter, float alpha) => LegacyBuildingEffectsRenderer.Mesh(MeshPool.plane10, p, Quaternion.identity, diameter, diameter, alpha, true);
        static void Rune(Vector3 p, float radius, float angle, float alpha) => LegacyBuildingEffectsRenderer.Mesh(LegacyBuildingEffectsRenderer.rune, p, Quaternion.AngleAxis(angle, Vector3.up), radius * .75f, radius, alpha);
        public static void Draw(ProductionBuildingEffect effect, Vector3 position, Vector2 size, Rot4 direction, float seconds, float strength)
        {
            // Pixel-to-world scale follows the preview's aspect-fitted source rectangle.
            float pixel = Mathf.Max(size.x / 260f, size.y / 245f);
            if (effect == ProductionBuildingEffect.Gemcutting)
            {
                for (int i = 0; i < 7; i++)
                {
                    float q = Mathf.Repeat(seconds * .45f + i / 7f, 1);
                    Halo(position + new Vector3(Mathf.Sin(i * 7) * 18 * q * pixel, 0, q * 32 * pixel), 6 * pixel, strength * (1-q) * .9f);
                }
            }
            else if (effect == ProductionBuildingEffect.Scribing)
            {
                for (int i = 0; i < 3; i++)
                {
                    float q = Mathf.Repeat(seconds * .25f + i / 3f, 1);
                    Rune(position + new Vector3(Mathf.Sin(i * 8) * 14 * pixel, 0, q * 30 * pixel), 4 * pixel, q * .4f * Mathf.Rad2Deg, strength * Mathf.Sin(q * Mathf.PI));
                }
            }
            else if (effect == ProductionBuildingEffect.Forge)
            {
                float width = direction.IsHorizontal ? size.y : size.x;
                Quaternion rotation = Quaternion.AngleAxis(direction.AsAngle, Vector3.up);
                for (int side = -1; side <= 1; side += 2)
                {
                    LegacyBuildingEffectsRenderer.Mesh(thread, position, rotation, side * width, width, strength * .5f);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 point = ThreadPoint(Mathf.Repeat(seconds * .45f + i / 3f, 1)) * width;
                        point.x *= side;
                        Halo(position + rotation * point, 8 * pixel, strength * .7f);
                    }
                }
            }
            else
            {
                bool small = effect == ProductionBuildingEffect.SmallMagicCircle;
                float radius = Mathf.Min(size.x, size.y) * (small ? .24f : .47f);
                LegacyBuildingEffectsRenderer.Mesh(LegacyBuildingEffectsRenderer.circle, position, Quaternion.identity, radius, radius, strength * (small ? .25f : .23f));
                int count = small ? 3 : 12;
                for (int i = 0; i < count; i++)
                {
                    float angle = seconds * (small ? .45f : .12f) + i * 2 * Mathf.PI / count;
                    Vector3 p = position + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    if (small) Halo(p, 14 * pixel, strength * .75f);
                    else Rune(p, 4 * pixel, -angle * Mathf.Rad2Deg, strength * .75f);
                }
            }
        }
    }
}
