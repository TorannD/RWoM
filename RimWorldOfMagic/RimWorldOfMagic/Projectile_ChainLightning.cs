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
	public class Projectile_ChainLightning : Projectile_AbilityBase
	{

        List<IntVec3> strikeLocs = new List<IntVec3>();
        List<IntVec3> newStrikeLocs = new List<IntVec3>();
        List<Mesh> strikeMeshes = new List<Mesh>();
        Vector3 direction = default(Vector3);

		private int age = -1;

        private int maxStrikes = 4;
        private int maxForks = 2;
        private int strikeTick = 0;
		private int strikeInt = 0;
        private float hopRadius = 4f;

        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

        private int hopCount = 0;
        private List<Thing> chainedThings = new List<Thing>();
        private int lastStrikeTick = 0;
        private int strikeDelay = 6;

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
            bool flag = (this.strikeInt >= this.maxStrikes) && Find.TickManager.TicksGame < (lastStrikeTick + 4);
			if (flag)
			{
                base.Destroy(mode);
			}
		}

        private void Initialize(Pawn p)
        {
            if(!p.DestroyedOrNull())
            {
                GenClamor.DoClamor(this, 2f, ClamorDefOf.Ability);
                CompAbilityUserMagic comp = p.TryGetComp<CompAbilityUserMagic>();
                if (comp != null && comp.MagicData != null)
                {
                    //pwrVal = TM_Calc.GetMagicSkillLevel(p, comp.MagicData.MagicPowerSkill_ChainLightning, "TM_ChainLightning", "_pwr", true);
                    //verVal = TM_Calc.GetMagicSkillLevel(p, comp.MagicData.MagicPowerSkill_ChainLightning, "TM_ChainLightning", "_ver", true);
                    pwrVal = TM_Calc.GetSkillPowerLevel(p, TorannMagicDefOf.TM_ChainLightning, true);
                    verVal = TM_Calc.GetSkillVersatilityLevel(p, TorannMagicDefOf.TM_ChainLightning, true);
                    this.arcaneDmg = comp.arcaneDmg;
                }
                maxStrikes += verVal;
                newStrikeLocs = new List<IntVec3>();
                newStrikeLocs.Clear();
                strikeLocs = new List<IntVec3>();
                strikeLocs.Clear();
                strikeMeshes = new List<Mesh>();
                strikeMeshes.Clear();
                chainedThings = new List<Thing>();
                chainedThings.Clear();
            }
        }

		protected override void Impact(Thing hitThing)
		{			         
            Map map = base.Map;
            Pawn caster = this.launcher as Pawn;
            IntVec3 strikePos = default(IntVec3);
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
                this.direction = TM_Calc.GetVector(caster.Position, strikePos);
                Initialize(caster);
                SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, map, false), MaintenanceType.None);
                info.pitchFactor = 1.1f;
                info.volumeFactor = .8f;
                TorannMagicDefOf.TM_Lightning.PlayOneShot(info);
                initialized = true;
            }
            if(Find.TickManager.TicksGame < (lastStrikeTick + strikeDelay))
            {
                for (int i =0; i < strikeLocs.Count; i++)
                {
                    Vector3 dir = TM_Calc.GetVector(strikeLocs[i], newStrikeLocs[i]);
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
                    DrawBolt(strikeMeshes[i], TM_MatPool.standardLightning, strikeLocs[i], angle, GetFadedBrightness);
                }
            }
            else if (!caster.DestroyedOrNull() && !caster.Dead && !caster.Downed && strikeLocs != null && strikeInt < maxStrikes)
            {
                lastStrikeTick = Find.TickManager.TicksGame;
                if(strikeInt == 0)
                {
                    Vector3 dir = this.direction;
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
                    IntVec3 origin = this.Caster.Position;
                    strikeLocs.Add(origin);
                    float range = (strikePos - origin).LengthHorizontal;
                    Mesh mesh = RandomBoltMesh(range);
                    strikeMeshes.Add(mesh);
                    lastStrikeTick = Find.TickManager.TicksGame;
                    DrawBolt(mesh, TM_MatPool.standardLightning, origin, angle, GetFadedBrightness);
                    DamageCell(strikePos, caster);
                    newStrikeLocs.Add(strikePos);
                }
                else
                {
                    List<IntVec3> validStrikes = new List<IntVec3>();
                    validStrikes.Clear();
                    strikeLocs.Clear();
                    strikeLocs.AddRange(newStrikeLocs);
                    newStrikeLocs.Clear();
                    strikeMeshes.Clear();
                    lastStrikeTick = Find.TickManager.TicksGame;
                    foreach (IntVec3 pos in strikeLocs)
                    {
                        for (int i = 0; i < this.maxForks; i++)
                        {
                            IntVec3 searchCell = (hopRadius * direction).ToIntVec3() + pos;
                            Thing target = GetTargetAroundCell(searchCell, hopRadius + verVal);
                            if (target != null)
                            {
                                validStrikes.Add(pos);
                                newStrikeLocs.Add(target.Position);
                                Vector3 dir = TM_Calc.GetVector(pos, target.Position);
                                float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
                                float range = (pos - target.Position).LengthHorizontal;
                                Mesh mesh = RandomBoltMesh(range);
                                strikeMeshes.Add(mesh);
                                DrawBolt(mesh, TM_MatPool.standardLightning, pos, angle, GetFadedBrightness);
                                DamageCell(target.Position, caster);
                            }
                        }
                        RandomStrikes(pos, caster);
                    }
                    strikeLocs = validStrikes;                    
                }
                strikeInt++;
            }
            else
            {
                strikeInt = maxStrikes;
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

        public void DamageCell(IntVec3 c, Pawn caster)
        {
            if (c != default(IntVec3) && c.IsValid && c.InBounds(this.Map))
            {
                FleckMaker.ThrowLightningGlow(c.ToVector3Shifted(), this.Map, 1f);
                List<Thing> thingList = c.GetThingList(this.Map);
                if (thingList != null && thingList.Count > 0)
                {
                    int damage = Mathf.RoundToInt(Mathf.Max(Rand.Range(this.maxStrikes + (2*pwrVal), (5 * (maxStrikes + pwrVal)) - (2 * strikeInt) * arcaneDmg), 0));
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing t = thingList[i];
                        if (t is Pawn)
                        {
                            TM_Action.DamageEntities(t, null, damage, TMDamageDefOf.DamageDefOf.TM_Lightning, caster);
                            chainedThings.Add(t);
                            break;
                        }
                        else if (t is Building && t.Faction != null)
                        {
                            TM_Action.DamageEntities(t, null, damage * 2, TMDamageDefOf.DamageDefOf.TM_Lightning, caster);
                            chainedThings.Add(t);
                            break;
                        }
                    }
                }
            }
        }

        public void RandomStrikes(IntVec3 from, Pawn caster)
        {
            int rndStrikes = Rand.RangeInclusive(1, 3);
            for(int i = 0; i < rndStrikes; i++)
            {
                Vector3 dir = (Quaternion.AngleAxis(Rand.Range(-90, 90), Vector3.up) * this.direction);
                float range = Rand.Range(1f, 6f);
                IntVec3 hitCell = from + (dir * range).ToIntVec3();
                //Log.Message("random strike " + hitCell);
                //DamageCell(hitCell, caster);
                GenExplosion.DoExplosion(hitCell, this.Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, caster, Mathf.RoundToInt(Rand.Range(4 + pwrVal, 8 + pwrVal) * arcaneDmg), 1.2f); 
                this.Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshGeneric(this.Map, TM_MatPool.thinLightning, from, hitCell, 2f, AltitudeLayer.MoteLow, strikeDelay, strikeDelay, 2));
            }
        }

        public Thing GetTargetAroundCell(IntVec3 cell, float radius)
        {
            FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), this.Map, 1f);
            List<Thing> ts = (from thing in this.Map.listerThings.AllThings
                                               where ((thing is Building || thing is Pawn) && (thing.Position - cell).LengthHorizontal <= radius && !chainedThings.Contains(thing))
                                               select thing).ToList();            
            if(ts != null && ts.Count > 0)
            {
                return ts.RandomElement();
            }
            return null;            
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
