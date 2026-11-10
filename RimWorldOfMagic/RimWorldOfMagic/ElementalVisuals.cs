#if RW17
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    // This node belongs only to TM_Elemental. Native animal graphics, children, transforms,
    // material variants and corpse rendering remain owned by the vanilla render tree.
    public class PawnRenderNode_ElementalBody : PawnRenderNode_AnimalPart_Body
    {
        public PawnRenderNode_ElementalBody(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree) { }
        protected override void EnsureMeshesInitialized()
        {
            base.EnsureMeshesInitialized();
            ElementalVisuals.EnsureAssets(); // Main-thread initialization, before ParallelPreDraw.
        }
    }

    public class PawnRenderNodeWorker_ElementalBody : PawnRenderNodeWorker_AnimalBody
    {
        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            // A null-material request keeps the native transform and post-draw hook without
            // drawing the undeformed body underneath. Selection of frames happens on the main
            // thread in PostDraw; cached render requests must not freeze the animation.
            requests.Add(new PawnGraphicDrawRequest(node, null, null));
        }

        // Native request recaching may ask again when animation graphics exist.
        public override Material GetFinalizedMaterial(PawnRenderNode node, PawnDrawParms parms) => null;

        public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            Material material = base.GetFinalizedMaterial(node, parms);
            if (material == null) return;
            bool animate = ElementalVisuals.CanAnimate(parms);
            int family = ElementalVisuals.Family(parms.pawn.def.defName);
            float time = Find.TickManager == null ? 0f : Find.TickManager.TicksGame / 60f;
            Vector2 size = GetGraphic(node, parms).drawSize;
            Matrix4x4 scaled = matrix * Matrix4x4.Scale(new Vector3(size.x, 1f, size.y));
            PreDraw(node, material, parms);
            MaterialPropertyBlock block = GetMaterialPropertyBlock(node, material, parms);
            foreach (PawnRenderSubWorker subWorker in node.Props.SubWorkers)
                subWorker.EditMaterialPropertyBlock(node, material, parms, ref block);
            if (animate)
            {
                float inhale = ElementalVisuals.Inhale(family, time, parms.pawn.thingIDNumber);
                Mesh body = ElementalVisuals.BodyMesh(family, inhale, node.FlipGraphic(parms));
                GenDraw.DrawMeshNowOrLater(body, scaled, material, parms.DrawNow, block);
                ElementalVisuals.DrawEffects(family, time, inhale, parms, scaled);
            }
            else
                GenDraw.DrawMeshNowOrLater(node.GetMesh(parms), matrix, material, parms.DrawNow, block);
            block?.Clear();
            base.PostDraw(node, parms, mesh, matrix);
        }
    }

    [StaticConstructorOnStartup]
    public static class ElementalVisuals
    {
        private const int Frames = 48;
        private const float EffectBrightness = 1.4f; // Approved preview default.
        private static Mesh[,,] bodies;
        private static Mesh rock, dot;
        private static Mesh[,,] airMeshes;
        private static Material effectMaterial;
        private static Material airMaterial;
        private static MaterialPropertyBlock effectBlock;
        private static readonly Color Earth = new Color(.67f, .47f, .88f);
        private static readonly Color Stone = new Color(.40f, .36f, .44f);
        private static readonly Color Fire = new Color(1f, .36f, .13f);
        private static readonly Color Water = new Color(.50f, .85f, .96f);
        private static readonly Color Air = new Color(.83f, .95f, 1f);

        public static int Family(string name)
        {
            switch (name)
            {
                case "TM_LesserEarth_ElementalR": case "TM_Earth_ElementalR": case "TM_GreaterEarth_ElementalR": return 0;
                case "TM_LesserFire_ElementalR": case "TM_Fire_ElementalR": case "TM_GreaterFire_ElementalR": return 1;
                case "TM_LesserWater_ElementalR": case "TM_Water_ElementalR": case "TM_GreaterWater_ElementalR": return 2;
                case "TM_LesserWind_ElementalR": case "TM_Wind_ElementalR": case "TM_GreaterWind_ElementalR": return 3;
                default: return -1;
            }
        }
        public static bool CanAnimate(PawnDrawParms parms)
        {
            Pawn pawn = parms.pawn;
            return pawn != null && Family(pawn.def?.defName) >= 0 && pawn.Spawned && !pawn.Dead && !pawn.Destroyed
                && !parms.DrawNow && !parms.Portrait && !parms.Statue && !parms.flags.FlagSet(PawnRenderFlags.Invisible)
                && parms.rotDrawMode == RotDrawMode.Fresh && pawn.Map == Find.CurrentMap
                && !pawn.Position.Fogged(pawn.Map) && Find.CameraDriver != null
                && Find.CameraDriver.CurrentViewRect.ExpandedBy(2).Contains(pawn.Position);
        }

        public static float Inhale(int family, float seconds, int id)
        {
            float period = family == 0 ? 5.8f : family == 1 ? 4.3f : family == 2 ? 5.1f : 4.7f;
            return (1f - (float)Math.Cos(seconds * Math.PI * 2 / period + (id % 17) * .37f)) * .5f;
        }

        // Coordinates are normalized within the original square body quad. Only the chest
        // expands: the tail, head and native shadow keep their original footprint.
        public static Vector3 Breathe(Vector3 point, int family, float inhale)
        {
            float w = (float)Math.Exp(-Math.Pow((point.z - .129f) / .185f, 2));
            point.x *= 1f + (family == 0 ? .023f : .035f) * inhale * w;
            point.z += .0106f * inhale * w;
            return point;
        }

        public static void EnsureAssets()
        {
            if (bodies != null) return;
            bodies = new Mesh[2, Frames + 1, 2];
            for (int earth = 0; earth < 2; earth++)
                for (int frame = 0; frame <= Frames; frame++)
                    for (int flip = 0; flip < 2; flip++)
                        bodies[earth, frame, flip] = MakeBody(earth == 0 ? 0 : 1, frame / (float)Frames, flip != 0);
            rock = Polygon(5, .19f);
            dot = Polygon(8, 0f);
            effectMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.white);
            airMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.white, true);
            effectBlock = new MaterialPropertyBlock();
            // Immutable shared phases avoid both per-frame uploads and node-owned Unity
            // resources (render nodes have no disposal hook when their trees are rebuilt).
            airMeshes = new Mesh[64, 5, 2];
            for (int phase = 0; phase < 64; phase++)
                for (int breath = 0; breath < 5; breath++)
                    for (int side = 0; side < 2; side++)
                        airMeshes[phase, breath, side] = MakeAir(phase * Mathf.PI * 2 / 64, breath / 4f, side != 0);
        }

        private static Mesh MakeAir(float phase, float inhale, bool front)
        {
            var vertices = new List<Vector3>(216);
            var colors = new List<Color>(216);
            var triangles = new List<int>(324);
            for (int strand = 0; strand < 3; strand++)
                for (int k = 0; k < 36; k++)
                {
                    float u = k / 36f, v = (k + 1) / 36f;
                    float a = phase + strand * 2.094f + u * 7.5f, b = phase + strand * 2.094f + v * 7.5f;
                    float z = Mathf.Sin(a);
                    if ((z >= 0) != front) continue;
                    Vector3 start = Breathe(At(Mathf.Cos(a) * (25 + u * 80), 342 - u * 180 + z * 16, front), 3, inhale);
                    Vector3 end = Breathe(At(Mathf.Cos(b) * (25 + v * 80), 342 - v * 180 + Mathf.Sin(b) * 16, front), 3, inhale);
                    // Depth belongs to the whole batch transform for transparent sorting.
                    start.y = end.y = 0;
                    Vector3 delta = end - start;
                    Vector3 width = new Vector3(-delta.z, 0, delta.x).normalized * (1.25f / 340f);
                    Color color = Air;
                    color.a = Mathf.Clamp01(EffectBrightness * Mathf.Sin(u * Mathf.PI) * (.36f + .25f * Mathf.Max(0, z)));
                    int i = vertices.Count;
                    vertices.Add(start - width); vertices.Add(start + width);
                    vertices.Add(end - width); vertices.Add(end + width);
                    for (int n = 0; n < 4; n++) colors.Add(color);
                    triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
                    triangles.Add(i + 2); triangles.Add(i + 1); triangles.Add(i + 3);
                }
            var mesh = new Mesh { name = "TM_ElementalAirPhase" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            return mesh;
        }

        public static Mesh BodyMesh(int family, float inhale, bool flip) => bodies[family == 0 ? 0 : 1, Mathf.Clamp(Mathf.RoundToInt(inhale * Frames), 0, Frames), flip ? 1 : 0];

        private static Mesh MakeBody(int family, float inhale, bool flip)
        {
            const int rows = 32;
            var vertices = new Vector3[(rows + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[rows * 6];
            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                vertices[row * 2] = Breathe(new Vector3(-.5f, 0, v - .5f), family, inhale);
                vertices[row * 2 + 1] = Breathe(new Vector3(.5f, 0, v - .5f), family, inhale);
                uv[row * 2] = new Vector2(flip ? 1 : 0, v);
                uv[row * 2 + 1] = new Vector2(flip ? 0 : 1, v);
                if (row == rows) continue;
                int i = row * 6, j = row * 2;
                triangles[i] = j; triangles[i + 1] = j + 2; triangles[i + 2] = j + 1;
                triangles[i + 3] = j + 1; triangles[i + 4] = j + 2; triangles[i + 5] = j + 3;
            }
            var mesh = new Mesh { name = "TM_ElementalBreath", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh Polygon(int sides, float irregularity)
        {
            var vertices = new Vector3[sides + 1];
            var triangles = new int[sides * 3];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, r = .5f * (1f - (i % 2) * irregularity);
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % sides + 1; triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "TM_ElementalParticle", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            return mesh;
        }

        private static float Phase(float t) => t - Mathf.Floor(t);
        private static Vector3 At(float x, float y, bool front) => new Vector3(x / 340f, front ? .012f : -.012f, (190f - y) / 340f);
        private static void Particle(Matrix4x4 matrix, PawnDrawParms parms, int family, float inhale, float x, float y, float diameter, Color color, float alpha, bool front, bool stone = false, float angle = 0)
        {
            Vector3 p = Breathe(At(x, y, front), family, inhale);
            color.a = Mathf.Clamp01(alpha * EffectBrightness) * parms.tint.a;
            effectBlock.SetColor(ShaderPropertyIDs.Color, color);
            GenDraw.DrawMeshNowOrLater(stone ? rock : dot, matrix * Matrix4x4.TRS(p, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(diameter / 340f, 1, diameter / 340f)), effectMaterial, parms.DrawNow, effectBlock);
        }
        private static void Line(Matrix4x4 matrix, PawnDrawParms parms, int family, float inhale, float x1, float y1, float x2, float y2, float width, Color color, float alpha, bool front)
        {
            Vector3 a = Breathe(At(x1, y1, front), family, inhale), b = Breathe(At(x2, y2, front), family, inhale);
            Vector3 delta = b - a;
            color.a = Mathf.Clamp01(alpha * EffectBrightness) * parms.tint.a;
            effectBlock.SetColor(ShaderPropertyIDs.Color, color);
            GenDraw.DrawMeshNowOrLater(MeshPool.plane10, matrix * Matrix4x4.TRS((a + b) * .5f, Quaternion.AngleAxis(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, Vector3.up), new Vector3(width / 340f, 1, delta.magnitude)), effectMaterial, parms.DrawNow, effectBlock);
        }

        public static void DrawEffects(int family, float t, float inhale, PawnDrawParms parms, Matrix4x4 matrix)
        {
            if (family == 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = t * (1.65f + i % 3 * .12f) + i * 2.399f, z = Mathf.Sin(a);
                    float x = Mathf.Cos(a) * (58 + i % 4 * 12), y = 303 + z * 24 + Mathf.Sin(t * 1.3f + i) * 9;
                    Particle(matrix, parms, family, inhale, x, y, (5 + i % 4) * 2, i % 3 == 0 ? Earth : Stone, .88f, z >= 0, true, a * 34);
                    Particle(matrix, parms, family, inhale, x - 1, y - 2, 4 + i % 4, i % 3 == 0 ? new Color(.83f,.67f,1) : new Color(.70f,.65f,.72f), .8f, z >= 0, true, a * 34);
                }
            }
            else if (family == 1)
            {
                for (int i = 0; i < 18; i++)
                {
                    float p = Phase(t * .42f + i / 18f);
                    Particle(matrix, parms, family, inhale, Mathf.Sin(i * 3.4f) * 120 + Mathf.Sin(t * 2 + i) * 8, 350 - p * 290, 3 + i % 3 * 2, Fire, Mathf.Sin(p * Mathf.PI) * .55f, true);
                }
            }
            else if (family == 2)
            {
                bool profile = parms.facing.IsHorizontal;
                float dir = parms.facing == Rot4.West ? -1 : 1;
                for (int j = 0; j < (profile ? 3 : 4); j++)
                {
                    float side = j % 2 == 0 ? -1 : 1;
                    float ex = profile ? (j == 2 ? 0 : j == 0 ? dir * 23 : -dir * 23) : j < 2 ? side * 94 : j == 2 ? -5 : 9;
                    float ey = profile ? (j == 0 ? 257 : j == 1 ? 248 : 316) : j < 2 ? 254 : j == 2 ? 309 : 319;
                    if (profile) side = j == 0 ? dir : -dir;
                    bool front = profile ? j != 1 : j != 2;
                    for (int i = 0; i < 5; i++)
                    {
                        float p = Phase(t * .9f + i * .2f + j * .27f), q = Mathf.Max(0, p - .10f), spread = 15 + i % 3 * 10;
                        float x = ex + side * (p * spread + Mathf.Sin(p * 4 + i) * p * 4), y = ey - 12 * p + 53 * p * p;
                        float x0 = ex + side * (q * spread + Mathf.Sin(q * 4 + i) * q * 4), y0 = ey - 12 * q + 53 * q * q;
                        float alpha = Mathf.Sin(p * Mathf.PI) * .66f;
                        Line(matrix, parms, family, inhale, x0, y0, x, y, 4 * (1-p) + 1, Water, alpha * .65f, front);
                        Particle(matrix, parms, family, inhale, x, y, 3.6f + (1-p) * 3, new Color(.71f,.95f,1), alpha, front);
                    }
                }
                for (int i = 0; i < 14; i++)
                {
                    float p = Phase(t * .52f + i * .618f);
                    Particle(matrix, parms, family, inhale, (i % 2 == 0 ? -1 : 1) * (13 + p * (28 + i % 4 * 6)), 321 + 9*p + 15*p*p, (1-p)*5.6f+2, Air, Mathf.Sin(p*Mathf.PI)*.46f, true);
                }
            }
            else if (family == 3)
            {
                int phase = Mathf.FloorToInt(Phase(t * 4.5f / (Mathf.PI * 2)) * 64);
                int breath = Mathf.Clamp(Mathf.RoundToInt(inhale * 4), 0, 4);
                effectBlock.SetColor(ShaderPropertyIDs.Color, parms.tint);
                for (int side = 0; side < 2; side++)
                    GenDraw.DrawMeshNowOrLater(airMeshes[phase, breath, side], matrix * Matrix4x4.Translate(new Vector3(0, side == 0 ? -.012f : .012f, 0)), airMaterial, false, effectBlock);
            }
        }
    }
}
#endif






