using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_LightLance : Projectile
    {

        private static readonly Color lightningColor = new Color(160f, 160f, 160f);
        private static readonly Material OrbMat = MaterialPool.MatFrom("Spells/deathbolt", false);

        protected new Vector3 origin;
        protected new Vector3 destination;

        private int age = -1;
        private float arcaneDmg = 1;
        private bool powered = false;
        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        public Vector3 drawingScale;
        public Vector3 drawingPosition;

        private int pwrVal = 0;
        private int verVal = 0;
        float radius = 1.4f;
        private int scanFrequency = 1;
        private float lightPotency = .5f;

        protected float speed = 30f;
        protected new int ticksToImpact;
        private bool impacted = false;

        protected Thing assignedTarget;
        protected Thing flyingThing;
        Pawn pawn;
        private List<Pawn> filteredTargets = new List<Pawn>();

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

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
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 1.4f, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.impacted, "impacted", false, false);
            Scribe_Values.Look<bool>(ref this.powered, "powered", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            //Scribe_References.Look<Thing>(ref this.flyingThing, "flyingThing", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                GetFilteredList();
                CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                if(comp != null)
                {
                    //pwrVal = comp.MagicData.MagicPowerSkill_LightLance.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_LightLance_pwr").level;
                    //verVal = comp.MagicData.MagicPowerSkill_LightLance.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_LightLance_ver").level;
                    pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_LightLance, true);
                    verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_LightLance, true);
                    this.radius += (verVal * .2f);
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD))
                    {
                        HediffComp_LightCapacitance hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD).TryGetComp<HediffComp_LightCapacitance>();
                        this.lightPotency = hd.LightPotency;
                    }

                }
                for (int i = 0; i < 3; i++)
                {
                    Vector3 rndPos = pawn.DrawPos;
                    rndPos.x += Rand.Range(-.75f, .75f);
                    rndPos.z += Rand.Range(-.75f, .75f);
                    FleckMaker.Static(rndPos, pawn.Map, FleckDefOf.FireGlow, 1f);
                }
            }            
            flyingThing.ThingID += Rand.Range(0, 214).ToString();            
        }

        private void GetFilteredList()
        {
            filteredTargets = new List<Pawn>();
            filteredTargets.Clear();
            IEnumerable<Pawn> pList = from p in this.Map.mapPawns.AllPawnsSpawned
                                      where (!p.DestroyedOrNull() && p != pawn && (p.Position - pawn.Position).LengthHorizontal < ((this.destination - this.origin).magnitude * 1.5f))
                                      select p;
            filteredTargets = pList.ToList();
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            bool spawned = flyingThing.Spawned;
            this.pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.speed = this.def.projectile.speed;
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
            if (this.ticksToImpact >= 0)
            {
                DrawEffects(this.ExactPosition, base.Map);
            }
            if(Find.TickManager.TicksGame % this.scanFrequency == 0)
            {
                DamageScan();
            }
            this.ticksToImpact--;            
            base.Position = this.ExactPosition.ToIntVec3();
            bool flag = !this.ExactPosition.InBounds(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {                                           
                bool flag2 = this.ticksToImpact <= 0 && !impacted;
                if (flag2)
                {
                    bool flag3 = this.DestinationCell.InBounds(base.Map);
                    if (flag3)
                    {
                        base.Position = this.DestinationCell;
                    }
                    this.ImpactSomething();
                }
            }
        }

        public void DamageScan()
        {
            if(filteredTargets == null || filteredTargets.Count <= 0)
            {
                GetFilteredList();
            }
            int num = filteredTargets.Count;
            List<Pawn> targets = new List<Pawn>();
            targets.Clear();
            for (int i = 0; i < num; i++)
            {
                if ((filteredTargets[i].DrawPos - this.ExactPosition).magnitude < this.radius)
                {
                    if (!(filteredTargets[i].Faction == Faction.OfPlayer && (filteredTargets[i].DrawPos - origin).magnitude < 2f))
                    {
                        targets.Add(filteredTargets[i]);
                    }
                }
            }
            if(targets.Count > 0)
            {
                for(int k =0; k < targets.Count; k++)
                {
                    TM_Action.DamageEntities(targets[k], null, (4 + (.6f*pwrVal)) * arcaneDmg * this.lightPotency, TMDamageDefOf.DamageDefOf.TM_BurningLight, pawn);
                }           
            }
        }

        public void DrawEffects(Vector3 effectVec, Map map)
        {
            //effectVec.x += Rand.Range(-0.4f, 0.4f);
            //effectVec.z += Rand.Range(-0.4f, 0.4f);
            //TM_MoteMaker.ThrowDiseaseMote(effectVec, map, 0.4f, 0.1f, .01f, 0.35f);
        }

        public override void Draw()
        {
            bool flag = this.flyingThing != null && !this.impacted;
            if (flag)
            {
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                base.Comps_PostDraw();
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
            this.Destroy(DestroyMode.Vanish);
        }        
    }
}
