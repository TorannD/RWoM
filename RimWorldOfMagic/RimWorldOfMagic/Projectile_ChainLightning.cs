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
	public class Projectile_ChainLightning : ClassProgression.Projectile_SpiritBolt
	{

        private List<IntVec3> strikeLocs = new List<IntVec3>();
        private List<IntVec3> newStrikeLocs = new List<IntVec3>();
        private readonly List<Mesh> strikeMeshes = new List<Mesh>();
        private Vector3 direction;
        private int strikeInt, lastStrikeTick;
        private const int strikeDelay = 6;
        private const float hopRadius = 4f;
        private const int maxForks = 2;
        private int maxStrikes => 4 + verVal;
        private HashSet<int> struckIds = new HashSet<int>();
        private bool initialized;
        protected override string SkillStem => "TM_ChainLightning";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_ChainLightning;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref strikeInt,"strikeInt");
            Scribe_Collections.Look(ref strikeLocs,"strikeLocs",LookMode.Value);
            Scribe_Collections.Look(ref newStrikeLocs,"TM_chainEndpoints",LookMode.Value);
            Scribe_Collections.Look(ref struckIds,"TM_chainTargets",LookMode.Value);
            Scribe_Values.Look(ref direction,"direction");
            Scribe_Values.Look(ref lastStrikeTick,"TM_chainLastTick");
            Scribe_Values.Look(ref initialized,"TM_chainActive");
            if (!projected)
            {
                Scribe_Values.Look(ref pwrVal,"TM_chainPower");
                Scribe_Values.Look(ref verVal,"TM_chainVersatility");
                Scribe_Values.Look(ref arcaneDmg,"TM_chainScale",1f);
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                strikeLocs ??= new List<IntVec3>();
                newStrikeLocs ??= new List<IntVec3>();
                struckIds ??= new HashSet<int>();
            }
        }

        private void ClearMeshes()
        {
            foreach (var mesh in strikeMeshes) if (mesh != null) UnityEngine.Object.Destroy(mesh);
            strikeMeshes.Clear();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ClearMeshes();
            base.DeSpawn(mode);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var body = launcher as Pawn;
            if (blockedByShield || body?.Spawned != true || body.Map != Map || body.Dead || body.Downed ||
                SpellInstigator is not Pawn caster || strikeInt > 0)
            {
                Destroy();
                return;
            }
            var target = hitThing?.Spawned == true && hitThing.Map == Map ? hitThing.Position : Position;
            if (!projected)
            {
                pwrVal = TM_Calc.GetSkillPowerLevel(body,TorannMagicDefOf.TM_ChainLightning,true);
                verVal = TM_Calc.GetSkillVersatilityLevel(body,TorannMagicDefOf.TM_ChainLightning,true);
                arcaneDmg = body.GetCompAbilityUserMagic()?.arcaneDmg ?? 1f;
            }
            GenClamor.DoClamor(this,2f,ClamorDefOf.Ability);
            var sound = SoundInfo.InMap(new TargetInfo(Position,Map));
            sound.pitchFactor=1.1f;
            sound.volumeFactor=.8f;
            TorannMagicDefOf.TM_Lightning.PlayOneShot(sound);
            initialized = true;
            direction = TM_Calc.GetVector(body.Position,target);
            lastStrikeTick = Find.TickManager.TicksGame;
            strikeInt = 1;
            strikeLocs.Add(body.Position);
            newStrikeLocs.Add(target);
            DamageCell(target,caster,hitThing);
            DrawStrikes();
        }

        protected override void TickInterval(int delta)
        {
            if (!initialized)
            {
                // Older saves kept only the start cells. A partly resolved old bolt
                // has no recoverable next targets; never replay its initial damage.
                if (strikeInt > 0) Destroy();
                else base.TickInterval(delta);
                return;
            }
            if (launcher is not Pawn body || !body.Spawned || body.Map != Map || body.Dead || body.Downed ||
                SpellInstigator is not Pawn caster) { Destroy(); return; }
            if (Find.TickManager.TicksGame - lastStrikeTick < strikeDelay) { DrawStrikes(); return; }
            if (strikeInt >= maxStrikes || newStrikeLocs.Count == 0) { Destroy(); return; }
            lastStrikeTick = Find.TickManager.TicksGame;
            var sources = newStrikeLocs;
            newStrikeLocs = new List<IntVec3>();
            strikeLocs.Clear();
            ClearMeshes();
            strikeInt++;
            foreach (var from in sources)
            {
                for (int i = 0; i < maxForks; ++i)
                {
                    var target = GetTargetAroundCell((hopRadius * direction).ToIntVec3()+from,hopRadius+verVal);
                    if (target == null) continue;
                    var cell = target.Position;
                    strikeLocs.Add(from);
                    newStrikeLocs.Add(cell);
                    DamageCell(cell,caster,target);
                }
                RandomStrikes(from,caster);
            }
            DrawStrikes();
        }

        private void DrawStrikes()
        {
            int count = Math.Min(strikeLocs.Count,newStrikeLocs.Count);
            for (int i=0; i<count; ++i)
            {
                var from=strikeLocs[i];
                var to=newStrikeLocs[i];
                if (strikeMeshes.Count <= i) strikeMeshes.Add(from==to ? null : RandomBoltMesh(from.DistanceTo(to)));
                if (strikeMeshes[i]==null) continue;
                var dir=TM_Calc.GetVector(from,to);
                DrawBolt(strikeMeshes[i],TM_MatPool.standardLightning,from,(Quaternion.AngleAxis(90,Vector3.up)*dir).ToAngleFlat(),GetFadedBrightness);
            }
        }

        public void DrawBolt(Mesh mesh, Material mat, IntVec3 start, float angle, float fadedBrightness)
        {
            if (start != default(IntVec3))
            {
                Graphics.DrawMesh(mesh, start.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteLow), Quaternion.Euler(0f, angle, 0f), FadedMaterialPool.FadedVersionOf(mat, fadedBrightness), 0); 
            }
        }

        public void DamageCell(IntVec3 cell, Pawn caster) => DamageCell(cell,caster,null);

        private void DamageCell(IntVec3 cell, Pawn caster, Thing selected)
        {
            if (!cell.InBoundsWithNullCheck(Map)) return;
            FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(),Map,1f);
            var victim = selected?.Spawned == true && selected.Map == Map && selected.Position == cell && CanStrike(selected)
                ? selected : cell.GetThingList(Map).FirstOrDefault(CanStrike);
            if (victim == null) return;
            int damage = Mathf.RoundToInt(Mathf.Max(Rand.Range(maxStrikes + 2*pwrVal,
                5*(maxStrikes+pwrVal) - 2*(strikeInt-1)*arcaneDmg), 0));
            struckIds.Add(victim.thingIDNumber);
            TM_Action.DamageEntities(victim,null,victim is Building ? 2*damage : damage,TMDamageDefOf.DamageDefOf.TM_Lightning,caster);
        }

        private static bool CanStrike(Thing t) => t is Pawn || t is Building && t.Faction != null;

        public void RandomStrikes(IntVec3 from, Pawn caster)
        {
            int rndStrikes = Rand.RangeInclusive(1, 3);
            for(int i = 0; i < rndStrikes; i++)
            {
                Vector3 dir = (Quaternion.AngleAxis(Rand.Range(-90, 90), Vector3.up) * this.direction);
                float range = Rand.Range(1f, 6f);
                IntVec3 hitCell = from + (dir * range).ToIntVec3();
                if (!hitCell.InBounds(Map)) continue;
                //Log.Message("random strike " + hitCell);
                //DamageCell(hitCell, caster);
                GenExplosion.DoExplosion(hitCell, this.Map, 1f, TMDamageDefOf.DamageDefOf.TM_Lightning, caster, Mathf.RoundToInt(Rand.Range(4 + pwrVal, 8 + pwrVal) * arcaneDmg), 1.2f); 
                this.Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshGeneric(this.Map, TM_MatPool.thinLightning, from, hitCell, 2f, AltitudeLayer.MoteLow, strikeDelay, strikeDelay, 2));
            }
        }

        public Thing GetTargetAroundCell(IntVec3 cell, float radius)
        {
            if (cell.InBounds(Map)) FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), Map, 1f);
            var candidates = GenRadial.RadialDistinctThingsAround(cell,Map,radius,true)
                .Where(t => CanStrike(t) && t.Position.InHorDistOf(cell,radius) && !struckIds.Contains(t.thingIDNumber));
            return candidates.TryRandomElement(out var target) ? target : null;
        }

        public Mesh RandomBoltMesh(float range)
        {
            // Rebuilding visual meshes after load must not reroll the following targets.
            Rand.PushState();
            try { return TM_MeshMaker.NewBoltMesh(range, 3); }
            finally { Rand.PopState(); }
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
