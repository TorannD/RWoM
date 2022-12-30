using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_LightningTrap : Projectile
    {
        private static readonly Material lightningMat = MaterialPool.MatFrom("Spells/LightningBolt_w", false);
        private static readonly Material OrbMat = MaterialPool.MatFrom("Spells/eyeofthestorm", false);

        protected new Vector3 origin;
        protected new Vector3 destination;

        private int searchDelay = 10;
        private const float arcaneDmg = 1;  // Currently not used. Could be used for difficulty damage changes.

        private class Strike
        {
            public IntVec3 from;
            public Vector3 to;
            public int fadeTimer;

            public void CountDown()
            {
                fadeTimer--;
                if (fadeTimer > 0) return;

                from = default;
                to = default;
            }
        }
        private readonly Strike[] strikeArray = Enumerable.Range(0, 10).Select(i => new Strike()).ToArray();

        public float speed = .8f;
        protected new int ticksToImpact;

        protected Faction faction;
        protected Thing flyingThing;

        protected new int StartingTicksToImpact => Math.Max(1,
            Mathf.RoundToInt((origin - destination).magnitude / (speed / 100f)));

        protected new IntVec3 DestinationCell => new IntVec3(destination);

        public new Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (destination - origin) * (1f - ticksToImpact / (float)StartingTicksToImpact);
                return origin + b + Vector3.up * def.Altitude;
            }
        }

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref origin, "origin");
            Scribe_Values.Look<Vector3>(ref destination, "destination");
            Scribe_Values.Look<int>(ref ticksToImpact, "ticksToImpact");
            Scribe_Values.Look<int>(ref searchDelay, "searchDelay", 210);
            Scribe_Deep.Look<Thing>(ref flyingThing, "flyingThing");
        }

        private void Initialize()
        {
            FleckMaker.Static(origin, Map, FleckDefOf.ExplosionFlash, 12f);
            FleckMaker.ThrowDustPuff(origin, Map, Rand.Range(1.2f, 1.8f));
            flyingThing.ThingID = flyingThing.ThingID;  // Get a new thingIDNumber
        }

        public void Launch(
            Thing projectileLauncher,
            Vector3 projectileOrigin,
            LocalTargetInfo target,
            Faction projectileFaction = null,
            float _speed = .8f)
        {
            // This is functioning essentially as a constructor
            speed = _speed;
            launcher = projectileLauncher;
            origin = projectileOrigin;
            faction = projectileFaction ?? Faction.OfPlayer;
            flyingThing = new Thing { def = TorannMagicDefOf.FlyingObject_LightningTrap };
            destination = target.Cell.ToVector3Shifted();
            ticksToImpact = StartingTicksToImpact;

            Initialize();
        }

        public override void Tick()
        {
            //base.Tick();
            searchDelay--;
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBoundsWithNullCheck(Map)) Destroy();
            else
            {
                Position = ExactPosition.ToIntVec3();
                DrawOrb(exactPosition, Map);
                if(searchDelay < 0)
                {
                    searchDelay = Rand.Range(20, 35);
                    SearchForTargets(origin.ToIntVec3(), 6f);
                }
                if (ticksToImpact <= 0)
                {
                    if (DestinationCell.InBoundsWithNullCheck(Map))
                    {
                        Position = DestinationCell;
                    }
                    Impact();
                }
            }
        }

        public void DrawOrb(Vector3 orbVec, Map map)
        {
            orbVec.x += Rand.Range(-0.6f, 0.6f);
            orbVec.z += Rand.Range(-0.6f, 0.6f);
            FleckMaker.ThrowLightningGlow(orbVec, map, 0.4f);
            orbVec.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(orbVec, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(0.4f, 0.4f, 0.4f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, OrbMat, 0);
        }

        public void SearchForTargets(IntVec3 center, float radius)
        {
            Pawn target = TM_Calc.FindNearbyEnemy(center, Map, faction, radius, 0f);
            if (target != null)
            {
                CellRect cellRect = CellRect.CenteredOn(target.Position, 2);
                cellRect.ClipInsideMap(Map);
                DrawStrike(center, target.Position.ToVector3());
                for (int k = 0; k < Rand.Range(1, 5); k++)
                {
                    IntVec3 randomCell = cellRect.RandomCell;
                    GenExplosion.DoExplosion(randomCell, Map, Rand.Range(.4f, .8f), TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(4, 6)), 0, SoundDefOf.Thunder_OnMap, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0.1f, true);
                }
                GenExplosion.DoExplosion(target.Position, Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(5, 9) * arcaneDmg), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0.1f, true);
            }            
            DrawStrikeFading();
        }

        public void DrawStrike(IntVec3 center, Vector3 dest)
        {
            new TM_MeshBolt(center, dest, lightningMat).CreateBolt();

            Strike strike = strikeArray.FirstOrDefault(s => s.fadeTimer <= 0);
            if (strike == default) return;

            strike.from = center;
            strike.to = dest;
            strike.fadeTimer = 30;
        }

        public void DrawStrikeFading()
        {
            foreach (Strike strike in strikeArray.Where(s => s.fadeTimer > 0))
            {
                new TM_MeshBolt(strike.from, strike.to, lightningMat).CreateFadedBolt(strike.fadeTimer/30);
                strike.CountDown();
            }
        }

        protected void Impact()
        {
            List<IntVec3> dissipationList = GenRadial.RadialCellsAround(origin.ToIntVec3(), 5, false).ToList();
            for (int i = 0; i < 4; i++)
            {
                IntVec3 strikeCell = dissipationList.RandomElement();
                if (!strikeCell.InBoundsWithNullCheck(Map) || !strikeCell.IsValid || strikeCell.Fogged(Map)) continue;

                DrawStrike(ExactPosition.ToIntVec3(), strikeCell.ToVector3Shifted());
                for (int k = 0; k < Rand.Range(1, 8); k++)
                {
                    CellRect cellRect = CellRect.CenteredOn(strikeCell, 2);
                    cellRect.ClipInsideMap(Map);
                    IntVec3 randomCell = cellRect.RandomCell;
                    GenExplosion.DoExplosion(randomCell, Map, Rand.Range(.2f, .6f), TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(2, 6)), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0.1f, true);
                }
            }
            GenExplosion.DoExplosion(origin.ToIntVec3(), Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(4, 8)), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0.1f, true);

            Destroy();
        }        
    }
}
