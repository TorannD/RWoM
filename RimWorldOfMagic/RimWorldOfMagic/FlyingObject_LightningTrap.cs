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
        private const float arcaneDmg = 1;  // Remove const if you need to change global dmg modifier during game

        // These three arrays must have same length
        private readonly IntVec3[] from = new IntVec3[10];
        private readonly Vector3[] to = new Vector3[10];
        private readonly int[] fadeTimer = new int[10];

        public float speed = .8f;
        protected new int ticksToImpact;

        protected Faction faction;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        private Pawn pawn;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion;

        public int timesToDamage = 3;

        private bool initialized = true;        

        protected new int StartingTicksToImpact => Math.Max(
            Mathf.RoundToInt((origin - destination).magnitude / (speed / 100f)), 1);

        protected new IntVec3 DestinationCell => new IntVec3(destination);

        public new Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (destination - origin) * (1f - ticksToImpact / (float)StartingTicksToImpact);
                return origin + b + Vector3.up * def.Altitude;
            }
        }

        public new Quaternion ExactRotation => Quaternion.LookRotation(destination - origin);

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref origin, "origin");
            Scribe_Values.Look<Vector3>(ref destination, "destination");
            Scribe_Values.Look<int>(ref ticksToImpact, "ticksToImpact");
            Scribe_Values.Look<int>(ref timesToDamage, "timesToDamage");
            Scribe_Values.Look<int>(ref searchDelay, "searchDelay", 210);
            //Scribe_Values.Look<int>(ref pwrVal, "pwrVal", 0, false);
            //Scribe_Values.Look<int>(ref verVal, "verVal", 0, false);
            Scribe_Values.Look<bool>(ref damageLaunched, "damageLaunched", true);
            Scribe_Values.Look<bool>(ref explosion, "explosion");
            Scribe_Values.Look<bool>(ref initialized, "initialized");
            Scribe_References.Look<Thing>(ref assignedTarget, "assignedTarget");
            //Scribe_References.Look<Thing>(ref launcher, "launcher");
            Scribe_References.Look<Pawn>(ref pawn, "pawn");
            Scribe_Deep.Look<Thing>(ref flyingThing, "flyingThing");
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(origin, Map, FleckDefOf.ExplosionFlash, 12f);
                FleckMaker.ThrowDustPuff(origin, Map, Rand.Range(1.2f, 1.8f));
            }
            flyingThing.ThingID += Rand.Range(0, 214).ToString();
            initialized = false;
        }

        public void Launch(
            Thing projectileLauncher,
            LocalTargetInfo target,
            Thing projectileFlyingThing,
            DamageInfo? projectileImpactDamage)
        {
            Launch(projectileLauncher, Position.ToVector3Shifted(), target, projectileFlyingThing, null,
                projectileImpactDamage);
        }

        public void Launch(Thing projectileLauncher, LocalTargetInfo target, Thing projectileFlyingThing)
        {
            Launch(projectileLauncher, Position.ToVector3Shifted(), target, projectileFlyingThing, null);
        }

        public void Launch(
            Thing projectileLauncher,
            Vector3 projectileOrigin,
            LocalTargetInfo target,
            Thing projectileFlyingThing,
            Faction projectileFaction,
            DamageInfo? newDamageInfo = null,
            float _speed = .8f)
        {
            pawn = launcher as Pawn;
            speed = _speed;
            if (flyingThing.Spawned)
            {
                flyingThing.DeSpawn();
            }
            launcher = projectileLauncher;
            origin = projectileOrigin;
            faction = projectileFaction;
            impactDamage = newDamageInfo;
            flyingThing = projectileFlyingThing;
            if (target.Thing != null)
            {
                assignedTarget = target.Thing;
            }
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
            if (!ExactPosition.InBoundsWithNullCheck(Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy();
            }
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
                    ImpactSomething();
                }
            }
        }

        public void DrawOrb(Vector3 orbVec, Map map)
        {
            Vector3 vector = orbVec;
            orbVec.x += Rand.Range(-0.6f, 0.6f);
            orbVec.z += Rand.Range(-0.6f, 0.6f);
            FleckMaker.ThrowLightningGlow(orbVec, map, 0.4f);
            vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Vector3 s = new Vector3(0.4f, 0.4f, 0.4f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, OrbMat, 0);
        }

        public void SearchForTargets(IntVec3 center, float radius)
        {
            if (faction == null)
            {
                faction = Faction.OfPlayer;
            }
            Pawn target = TM_Calc.FindNearbyEnemy(center, Map, faction, radius, 0f);
            if (target != null)
            {
                CellRect cellRect = CellRect.CenteredOn(target.Position, 2);
                cellRect.ClipInsideMap(Map);
                DrawStrike(center, target.Position.ToVector3());
                for (int k = 0; k < Rand.Range(1, 5); k++)
                {
                    IntVec3 randomCell = cellRect.RandomCell;
                    GenExplosion.DoExplosion(randomCell, Map, Rand.Range(.4f, .8f), TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(4, 6)), 0, SoundDefOf.Thunder_OnMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
                }
                GenExplosion.DoExplosion(target.Position, Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(5, 9) * arcaneDmg), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
            }            
            DrawStrikeFading();
        }

        public void DrawStrike(IntVec3 center, Vector3 dest)
        {
            TM_MeshBolt meshBolt = new TM_MeshBolt(center, dest, lightningMat);
            meshBolt.CreateBolt();
            for (int i = 0; i < fadeTimer.Length; i++)
            {
                if (fadeTimer[i] > 0) continue;

                from[i] = center;
                to[i] = dest;
                fadeTimer[i] = 30;
                break;
            }
        }

        public void DrawStrikeFading()
        {
            for (int i = 0; i < fadeTimer.Length; i++)
            {
                if (fadeTimer[i] <= 0) continue;

                TM_MeshBolt meshBolt = new TM_MeshBolt(from[i], to[i], lightningMat);
                meshBolt.CreateFadedBolt(fadeTimer[i]/30);
                fadeTimer[i]--;
                if (fadeTimer[i] != 0) continue;

                from[i] = default;
                to[i] = default;
            }            
        }

        private void ImpactSomething()
        {
            if (assignedTarget == null
                || (assignedTarget is Pawn p
                    && p.GetPosture() != PawnPosture.Standing
                    && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f
                    && Rand.Value > 0.2f))
            {
                Impact(null);
            }
            else
            {
                Impact(assignedTarget);
            }
        }

        protected new void Impact(Thing hitThing)
        {
            if (hitThing == null)
            {
                if (Position.GetThingList(Map).FirstOrDefault(t => t == assignedTarget) is Pawn hitPawn)
                {
                    hitThing = hitPawn;
                }
            }
            if (impactDamage.HasValue)
            {
                for (int i = 0; i < timesToDamage; i++)
                {
                    if (damageLaunched)
                    {
                        flyingThing.TakeDamage(impactDamage.Value);
                    }
                    else
                    {
                        hitThing?.TakeDamage(impactDamage.Value);
                    }
                }
                if (explosion)
                {
                    GenExplosion.DoExplosion(origin.ToIntVec3(), Map, 0.9f, DamageDefOf.Stun, this, -1, 0);
                }
            }

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
                    GenExplosion.DoExplosion(randomCell, Map, Rand.Range(.2f, .6f), TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(2, 6)), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
                }
            }
            GenExplosion.DoExplosion(origin.ToIntVec3(), Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, launcher, Mathf.RoundToInt(Rand.Range(4, 8)), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);

            Destroy();
        }        
    }
}
