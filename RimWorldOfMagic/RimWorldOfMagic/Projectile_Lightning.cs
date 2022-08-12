using AbilityUser;
using RimWorld;
using System.Linq;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;
using Verse.Sound;

namespace TorannMagic
{
	public class Projectile_Lightning : Projectile_AbilityBase
	{

        List<IntVec3> strikeLocs = new List<IntVec3>();
        List<IntVec3> newStrikeLocs = new List<IntVec3>();
        List<Mesh> strikeMeshes = new List<Mesh>();
        Vector3 direction = default(Vector3);

		private int age = -1;

        private int maxStrikes = 1;
        private int maxForks = 1;
        private int strikeTick = 0;
		private int strikeInt = 0;
        private float hopRadius = 4f;

        public int verVal;
        public int pwrVal;
        public float arcaneDmg = 1;

        private int hopCount = 0;
        private List<Thing> chainedThings = new List<Thing>();
        private int lastStrikeTick = 0;
        private int strikeDelay = 15;

        //unsaved
        private bool initialized = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.hopCount, "hopCount", 0, false);
            //
            Scribe_Values.Look<int>(ref this.strikeTick, "strikeTick", 0, false);
            Scribe_Values.Look<int>(ref this.strikeInt, "strikeInt", 0, false);
            Scribe_Collections.Look<IntVec3>(ref this.strikeLocs, "strikeLocs", LookMode.Value);
            Scribe_Values.Look<Vector3>(ref this.direction, "direction", default(Vector3), false);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
            bool flag = (this.strikeInt >= this.maxStrikes) && Find.TickManager.TicksGame >= (lastStrikeTick + strikeDelay);
			if (flag)
			{
                base.Destroy(mode);
			}
		}

        private void Initialize(Thing t)
        {
            GenClamor.DoClamor(this, 2f, ClamorDefOf.Ability);
            if (!t.DestroyedOrNull() && t is Pawn p)
            {
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                if (comp != null && comp.MagicData != null)
                {
                    //pwrVal = TM_Calc.GetMagicSkillLevel(p, comp.MagicData.MagicPowerSkill_ChainLightning, "TM_ChainLightning", "_pwr", true);
                    //verVal = TM_Calc.GetMagicSkillLevel(p, comp.MagicData.MagicPowerSkill_ChainLightning, "TM_ChainLightning", "_ver", true);
                    pwrVal = TM_Calc.GetSkillPowerLevel(p, TorannMagicDefOf.TM_ChainLightning);
                    verVal = TM_Calc.GetSkillVersatilityLevel(p, TorannMagicDefOf.TM_ChainLightning);
                    this.arcaneDmg = comp.arcaneDmg;
                }                
            }
            newStrikeLocs = new List<IntVec3>();
            newStrikeLocs.Clear();
            strikeLocs = new List<IntVec3>();
            strikeLocs.Clear();
            strikeMeshes = new List<Mesh>();
            strikeMeshes.Clear();
            chainedThings = new List<Thing>();
            chainedThings.Clear();
        }

		protected override void Impact(Thing hitThing)
		{			         
            Map map = base.Map;
            IntVec3 strikePos = default(IntVec3);
            if (!this.launcher.DestroyedOrNull())
            {
                if (!initialized)
                {
                    if (hitThing != null && hitThing.Position != base.Position)
                    {
                        strikePos = hitThing.Position;
                    }
                    else
                    {
                        strikePos = base.Position;
                    }
                    this.direction = TM_Calc.GetVector(this.launcher.Position, strikePos);
                    Initialize(this.launcher);
                    //SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, map, false), MaintenanceType.None);
                    //info.pitchFactor = 1f;
                    //info.volumeFactor = .6f;
                    //TorannMagicDefOf.TM_Lightning.PlayOneShot(info);
                    initialized = true;
                }
                if (Find.TickManager.TicksGame < (lastStrikeTick + strikeDelay))
                {
                    for (int i = 0; i < strikeLocs.Count; i++)
                    {
                        Vector3 dir = TM_Calc.GetVector(strikeLocs[i], newStrikeLocs[i]);
                        float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
                        DrawBolt(strikeMeshes[i], TM_MatPool.standardLightning, strikeLocs[i], angle, GetFadedBrightness);
                    }
                }
                if (strikeLocs != null && strikeInt < maxStrikes)
                {
                    lastStrikeTick = Find.TickManager.TicksGame;
                    if (strikeInt == 0)
                    {
                        Vector3 dir = this.direction;
                        float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
                        IntVec3 origin = this.launcher.Position;
                        strikeLocs.Add(origin);
                        float range = (strikePos - origin).LengthHorizontal;
                        Mesh mesh = RandomBoltMesh(range);
                        strikeMeshes.Add(mesh);
                        lastStrikeTick = Find.TickManager.TicksGame;
                        DrawBolt(mesh, TM_MatPool.standardLightning, origin, angle, GetFadedBrightness);
                        DamageCell(strikePos, this.launcher);
                        GenExplosion.DoExplosion(strikePos, this.Map, 2f, TMDamageDefOf.DamageDefOf.TM_Lightning, this.launcher, Mathf.RoundToInt(Rand.Range(3 + pwrVal, 6 + pwrVal) * arcaneDmg), 1.2f, null);
                        newStrikeLocs.Add(strikePos);
                    }
                    RandomStrikes(base.Position, this.launcher);
                    strikeInt++;
                }
            }
            Destroy();
        }

        public void DrawBolt(Mesh mesh, Material mat, IntVec3 start, float angle, float fadedBrightness)
        {
            if (start != default(IntVec3))
            {
                Graphics.DrawMesh(mesh, start.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteLow), Quaternion.Euler(0f, angle, 0f), FadedMaterialPool.FadedVersionOf(mat, fadedBrightness), 0); 
            }
        }

        public void DamageCell(IntVec3 c, Thing caster)
        {
            if (c != default(IntVec3) && c.IsValid && c.InBoundsWithNullCheck(this.Map))
            {
                FleckMaker.ThrowLightningGlow(c.ToVector3Shifted(), this.Map, 1f);
                List<Thing> thingList = c.GetThingList(this.Map);
                if (thingList != null && thingList.Count > 0)
                {
                    int damage = Mathf.RoundToInt(Mathf.Max(Rand.Range(this.maxStrikes + pwrVal, (5 * maxStrikes) + pwrVal) - (2 * strikeInt) * arcaneDmg, 0));
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing t = thingList[i];
                        if (t is Pawn)
                        {
                            TM_Action.DamageEntities(t, null, damage, TMDamageDefOf.DamageDefOf.TM_Lightning, caster);
                            chainedThings.Add(t);
                        }
                        else if (t is Building)
                        {
                            TM_Action.DamageEntities(t, null, damage * 2, TMDamageDefOf.DamageDefOf.TM_Lightning, caster);
                            chainedThings.Add(t);
                        }
                    }
                }
            }
        }

        public void RandomStrikes(IntVec3 from, Thing caster)
        {
            int rndStrikes = Rand.RangeInclusive(2, 4);
            for(int i = 0; i < rndStrikes; i++)
            {
                Vector3 dir = (Quaternion.AngleAxis(Rand.Range(0, 360), Vector3.up) * this.direction);
                float range = Rand.Range(2f, 6f);
                IntVec3 hitCell = from + (dir * range).ToIntVec3();
                //Log.Message("random strike " + hitCell);
                //DamageCell(hitCell, caster);
                GenExplosion.DoExplosion(hitCell, this.Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, caster, Mathf.RoundToInt(Rand.Range(3 + pwrVal, 6 + pwrVal) * arcaneDmg), 1.2f, null); 
                this.Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshGeneric(this.Map, TM_MatPool.thinLightning, from, hitCell, 2f, AltitudeLayer.MoteLow, strikeDelay, strikeDelay, 2));
            }
        }

        public Mesh RandomBoltMesh(float range)
        {
            return TM_MeshMaker.NewBoltMesh(range, 3);            
        }

        public float GetFadedBrightness
        {
            get
            {
                return 1f + ((float)(lastStrikeTick -  Find.TickManager.TicksGame) / (float)(strikeDelay));
            }
        }
    }
}
