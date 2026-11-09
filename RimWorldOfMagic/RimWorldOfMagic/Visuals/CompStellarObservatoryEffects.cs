using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace TorannMagic.Visuals
{
    public class CompProperties_StellarObservatoryEffects : CompProperties
    {
        public CompProperties_StellarObservatoryEffects() { compClass = typeof(CompStellarObservatoryEffects); }
    }

    // Presentation reads gameplay state only. No saved particles, timers or resource consumption.
    public class CompStellarObservatoryEffects : ThingComp
    {
        public override void PostDraw()
        {
            base.PostDraw();
            if (parent == null || !parent.Spawned || parent.Destroyed || parent.Map != Find.CurrentMap) return;
            var observatory = parent.GetComp<CompStellarObservatory>();
            if (observatory?.CanOperate != true) return;
            StellarObservatoryEffects.Draw(parent, observatory.IsSearching, Find.TickManager.TicksGame, observatory.LastDiscoveryTick);
        }
    }

    public static class StellarObservatoryEffects
    {
        public const int FlashTicks = 180;
        public static float DiscoveryPulse(int now, int start)
        {
            long age = (long)now - start;
            return start < 0 || age < 0 || age >= FlashTicks ? 0f : 1f - age / (float)FlashTicks;
        }
        // Measured on the 1280px approved Graphic_Single. No cardinal rotation of these anchors.
        public static Vector3 CrystalAnchor(int index, Vector2 size)
            => new Vector3((index % 2 == 0 ? -.326f : .326f) * size.x, 0, (index < 2 ? .39f : -.28f) * size.y);

        public static void Draw(Thing building, bool searching, int now, int lastDiscovery)
        {
            var size = building.def.graphicData.drawSize;
            Vector3 origin = building.DrawPos + building.Graphic.DrawOffset(building.Rotation);
            origin.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + .014f;
            float seconds = (now % 360000) / 60f;
            float light = (searching ? .45f : .16f) * (1f + .13f * Mathf.Sin(seconds * .9f));
            for (int i = 0; i < 4; i++)
                LegacyBuildingEffectsRenderer.Mesh(MeshPool.plane10, origin + CrystalAnchor(i, size), Quaternion.identity,
                    size.x * .21f, size.y * .21f, light, true);
            Vector3 center = origin + new Vector3(0, .001f, .03f * size.y);
            if (searching)
            {
                LegacyBuildingEffectsRenderer.Mesh(MeshPool.plane10, center, Quaternion.identity, size.x * .56f, size.y * .56f, .13f, true);
                // Runes move along fixed ellipses. Rotating the whole ellipse would shift the apparent anchors.
                for (int ring = 0; ring < 2; ring++)
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4 + seconds * (ring == 0 ? .18f : -.13f);
                        float x = ring == 0 ? .29f : .15f, z = ring == 0 ? .15f : .30f;
                        Vector3 p = center + new Vector3(Mathf.Cos(angle) * size.x * x, ring * .001f, Mathf.Sin(angle) * size.y * z);
                        LegacyBuildingEffectsRenderer.Mesh(LegacyBuildingEffectsRenderer.rune, p, Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up),
                            size.x * .013f, size.y * .013f, .48f);
                    }
            }
            float pulse = DiscoveryPulse(now, lastDiscovery);
            if (pulse > 0)
            {
                float radius = .12f + (1f - pulse) * .34f;
                LegacyBuildingEffectsRenderer.Mesh(LegacyBuildingEffectsRenderer.circle, center, Quaternion.identity, size.x * radius, size.y * radius * .75f, pulse * .6f);
                LegacyBuildingEffectsRenderer.Mesh(MeshPool.plane10, center, Quaternion.identity, size.x * .7f, size.y * .7f, pulse * .45f, true);
            }
        }
    }
}
