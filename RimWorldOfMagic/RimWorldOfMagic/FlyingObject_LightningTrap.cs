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

        private static readonly Color lightningColor = new Color(160f, 160f, 160f);
        private static readonly Material lightningMat = MaterialPool.MatFrom("Spells/LightningBolt_w", false);
        private static readonly Material OrbMat = MaterialPool.MatFrom("Spells/eyeofthestorm", false);

        protected new Vector3 origin;
        protected new Vector3 destination;

        private int searchDelay = 10;
        private int maxStrikeDelay = 100;
        private int maxStrikeDelayBldg = 60;
        private int lastStrike = 0;
        private int lastStrikeBldg = 0;
        private int age = -1;
        private float arcaneDmg = 1;
        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        public Vector3 drawingScale;
        public Vector3 drawingPosition;
        IntVec3[] from = new IntVec3[10];
        Vector3[] to = new Vector3[10];
        int[] fadeTimer = new int[10];

        //private int pwrVal = 0;
        //private int verVal = 0;

        public float speed = .8f;
        protected new int ticksToImpact;

        protected Faction faction = null;
        //protected new Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        Pawn pawn;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int timesToDamage = 3;

        public int weaponDmg = 0;

        private bool initialized = true;        

        protected new int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((this.origin - this.destination).magnitude / (this.speed / 100f));
                bool flag = num < 1;
                if (flag)
                {
                    num = 1;
                }
                return num;
            }
        }

        protected new IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(this.destination);
            }
        }

        public new Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * this.def.Altitude;
            }
        }

        public new Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return this.ExactPosition;
            }
        }       

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.timesToDamage, "timesToDamage", 0, false);
            Scribe_Values.Look<int>(ref this.searchDelay, "searchDelay", 210, false);
            //Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            //Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            //Scribe_References.Look<Thing>(ref this.launcher, "launcher", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            flyingThing.ThingID += Rand.Range(0, 214).ToString();
            this.initialized = false;
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null, null);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, Faction faction, DamageInfo? newDamageInfo = null, float _speed = .8f)
        {
            bool spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            this.speed = _speed;
            //CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            //this.arcaneDmg = comp.arcaneDmg;
            //MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EyeOfTheStorm.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EyeOfTheStorm_pwr");
            //MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EyeOfTheStorm.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EyeOfTheStorm_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //if (settingsRef.AIHardMode && !pawn.IsColonist)
            //{
            //    pwrVal = 1;
            //    verVal = 1;
            //}
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.launcher = launcher;
            this.origin = origin;
            this.faction = faction;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.destination = targ.Cell.ToVector3Shifted();
            this.ticksToImpact = this.StartingTicksToImpact;

            this.Initialize();
        }

        public override void Tick()
        {
            //base.Tick();
            age++;
            this.searchDelay--;
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();                
                DrawOrb(exactPosition, base.Map);
                if(this.searchDelay < 0)
                {
                    this.searchDelay = Rand.Range(20, 35);
                    SearchForTargets(this.origin.ToIntVec3(), 6f);
                }                
                bool flag2 = this.ticksToImpact <= 0;
                if (flag2)
                {
                    bool flag3 = this.DestinationCell.InBoundsWithNullCheck(base.Map);
                    if (flag3)
                    {
                        base.Position = this.DestinationCell;
                    }
                    this.ImpactSomething();
                }
            }
        }

        public void DrawOrb(Vector3 orbVec, Map map)
        {
            Vector3 vector = orbVec;
            float xOffset = Rand.Range(-0.6f, 0.6f);
            float zOffset = Rand.Range(-0.6f, 0.6f);
            orbVec.x += xOffset;
            orbVec.z += zOffset;
            FleckMaker.ThrowLightningGlow(orbVec, map, 0.4f);
            float num = Mathf.Lerp(1.2f, 1.55f, 5f);            
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = (float)Rand.Range(0, 360);
            Vector3 s = new Vector3(0.4f, 0.4f, 0.4f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, FlyingObject_LightningTrap.OrbMat, 0);  
        }

        public void SearchForTargets(IntVec3 center, float radius)
        {
            Pawn target = null;
            if (faction == null)
            {
                faction = Faction.OfPlayer;
            }
            target = TM_Calc.FindNearbyEnemy(center, this.Map, this.faction, radius, 0f);
            if (target != null)
            {
                CellRect cellRect = CellRect.CenteredOn(target.Position, 2);
                cellRect.ClipInsideMap(base.Map);
                DrawStrike(center, target.Position.ToVector3());
                for (int k = 0; k < Rand.Range(1, 5); k++)
                {
                    IntVec3 randomCell = cellRect.RandomCell;
                    GenExplosion.DoExplosion(randomCell, base.Map, Rand.Range(.4f, .8f), TMDamageDefOf.DamageDefOf.TM_Lightning, this.launcher, Mathf.RoundToInt(Rand.Range(4, 6)), 0, SoundDefOf.Thunder_OnMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
                }
                GenExplosion.DoExplosion(target.Position, base.Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, this.launcher, Mathf.RoundToInt(Rand.Range(5, 9) * this.arcaneDmg), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
                this.lastStrike = this.age;
            }            
            DrawStrikeFading();
        }

        public void DrawStrike(IntVec3 center, Vector3 dest)
        {
            TM_MeshBolt meshBolt = new TM_MeshBolt(center, dest, FlyingObject_LightningTrap.lightningMat);
            meshBolt.CreateBolt();
            for (int i = 0; i < 10; i++)
            {
                if (fadeTimer[i] <= 0)
                {
                    from[i] = center;
                    to[i] = dest;
                    fadeTimer[i] = 30;
                    i = 10;
                }
            }
        }

        public void DrawStrikeFading()
        {
            for(int i = 0; i < 10; i ++)
            {
                if (fadeTimer[i] > 0)
                {
                    TM_MeshBolt meshBolt = new TM_MeshBolt(from[i], to[i], FlyingObject_LightningTrap.lightningMat);
                    meshBolt.CreateFadedBolt(fadeTimer[i]/30);
                    fadeTimer[i]--;
                    if (fadeTimer[i] == 0)
                    {
                        from[i] = default(IntVec3);
                        to[i] = default(Vector3);
                    }
                }
            }            
        }

        private void ImpactSomething()
        {
            bool flag = this.assignedTarget != null;
            if (flag)
            {
                Pawn pawn = this.assignedTarget as Pawn;
                bool flag2 = pawn != null && pawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f;
                if (flag2)
                {
                    this.Impact(null);
                }
                else
                {
                    this.Impact(this.assignedTarget);
                }
            }
            else
            {
                this.Impact(null);
            }
        }

        protected new void Impact(Thing hitThing)
        {
            bool flag = hitThing == null;
            if (flag)
            {
                Pawn hitPawn;
                bool flag2 = (hitPawn = (base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn)) != null;
                if (flag2)
                {
                    hitThing = hitPawn;
                }
            }
            bool hasValue = this.impactDamage.HasValue;
            if (hasValue)
            {
                for (int i = 0; i < this.timesToDamage; i++)
                {
                    bool flag3 = this.damageLaunched;
                    if (flag3)
                    {
                        this.flyingThing.TakeDamage(this.impactDamage.Value);
                    }
                    else
                    {
                        hitThing.TakeDamage(this.impactDamage.Value);
                    }
                }
                bool flag4 = this.explosion;
                if (flag4)
                {
                    GenExplosion.DoExplosion(this.origin.ToIntVec3(), base.Map, 0.9f, DamageDefOf.Stun, this, -1, 0, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                }
            }

            List<IntVec3> dissipationList = GenRadial.RadialCellsAround(this.origin.ToIntVec3(), 5, false).ToList();
            for (int i = 0; i < 4; i++)
            {
                IntVec3 strikeCell = dissipationList.RandomElement();
                if (strikeCell.InBoundsWithNullCheck(base.Map) && strikeCell.IsValid && !strikeCell.Fogged(this.Map))
                {
                    DrawStrike(this.ExactPosition.ToIntVec3(), strikeCell.ToVector3Shifted());
                    for (int k = 0; k < Rand.Range(1, 8); k++)
                    {
                        CellRect cellRect = CellRect.CenteredOn(strikeCell, 2);
                        cellRect.ClipInsideMap(base.Map);
                        IntVec3 randomCell = cellRect.RandomCell;
                        GenExplosion.DoExplosion(randomCell, base.Map, Rand.Range(.2f, .6f), TMDamageDefOf.DamageDefOf.TM_Lightning, this.launcher, Mathf.RoundToInt(Rand.Range(2, 6)), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
                    }
                }
            }
            GenExplosion.DoExplosion(this.origin.ToIntVec3(), base.Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, this.launcher, Mathf.RoundToInt(Rand.Range(4, 8)), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);


            this.Destroy(DestroyMode.Vanish);
        }        
    }
}
