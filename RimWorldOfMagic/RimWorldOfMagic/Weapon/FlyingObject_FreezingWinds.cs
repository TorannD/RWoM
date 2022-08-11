using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic.Weapon
{
    [StaticConstructorOnStartup]
    public class FlyingObject_FreezingWinds : FlyingObject_Advanced
    {
        int actionTick = 10;
        int age = 0;
        int rotationOffset = 0;
        float rotationRate = 1f;
        float moteAngle = 0;
        Thing hitThing = null;

        public override void PreInitialize()
        {
            actionTick = Rand.Range(0, 15);
            rotationOffset = Rand.RangeInclusive(0, 360);
            if (Rand.Chance(.5f))
            {
                rotationRate = Rand.Range(-15f, -8f);
            }
            else
            {
                rotationRate = Rand.Range(8, 15f);
            }
            moteAngle = (Quaternion.AngleAxis(90, Vector3.up) * travelVector).ToAngleFlat();
            base.PreInitialize();
        }

        public override void PreTick()
        {
            age++;
            if(age >= actionTick)
            {
                actionTick = age + Rand.Range(15, 25);                
                DamageThingsAtPosition();
                SnowUtility.AddSnowRadial(this.ExactPosition.ToIntVec3(), this.Map, .4f, .2f);                
            }           
        }

        public override void PostTick()
        {
            if (hitThing != null)
            {
                this.Impact(hitThing);
            }
        }

        public void DamageThingsAtPosition()
        {
            IntVec3 curCell = this.ExactPosition.ToIntVec3();
            List<Thing> hitList = new List<Thing>();
            hitList = curCell.GetThingList(base.Map);
            List<Fire> destroyFires = new List<Fire>();
            destroyFires.Clear();            
            for (int j = 0; j < hitList.Count; j++)
            {
                if (hitList[j] is Pawn && hitList[j] != this.launcher)
                {
                    DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, this.def.projectile.GetDamageAmount(this.weaponDamageMultiplier), 1, -1, this.launcher);                    
                    hitList[j].TakeDamage(dinfo);
                    hitThing = hitList[j];
                }
                if(hitList[j] is Fire)
                {
                    Fire hitFire = hitList[j] as Fire;
                    hitFire.fireSize -= .25f;
                    if(hitFire.fireSize <= .1f)
                    {
                        destroyFires.Add(hitFire);
                    }
                }
            }
            for(int i = 0; i < destroyFires.Count; i++)
            {
                destroyFires[i].Destroy(DestroyMode.Vanish);
            }            
        }

        public override void DrawEffects(Vector3 effectVec)
        {
            effectVec.x += Rand.Range(-0.4f, 0.4f);
            effectVec.z += Rand.Range(-0.4f, 0.4f);            
            TM_MoteMaker.ThrowGenericMote(this.moteDef, effectVec, this.Map, Rand.Range(.15f, .45f), Rand.Range(.05f, .1f), .03f, Rand.Range(.2f, .3f), Rand.Range(-200, 200), Rand.Range(1f, 6f), moteAngle + Rand.Range(-20,20), Rand.Range(0, 360));
        }

        public override void Draw()
        {
            bool flag = this.flyingThing != null;
            if (flag)
            {                
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, DrawRotation, this.flyingThing.def.DrawMatSingle, 0);                
            }
            else
            {                
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, DrawRotation, this.def.DrawMatSingle, 0);
            }
            base.Comps_PostDraw();
        }

        public Quaternion DrawRotation
        {
            get
            {                
                return Quaternion.LookRotation(Quaternion.AngleAxis((age * rotationRate) + rotationOffset, Vector3.up) * this.travelVector);
            }
        }
    }
}
