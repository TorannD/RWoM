using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Projectile_TechnoTurret : ClassProgression.Projectile_SpiritBolt
    {
        protected override string SkillStem => "TM_TechnoTurret";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_TechnoTurret;
        private int efficiency;
        private float refundableMana;
        private bool resolved;

        internal override void PrepareProjection(MagicAbility owner)
        {
            base.PrepareProjection(owner);
            efficiency = Efficiency(owner.MagicUser);
        }

        private int Efficiency(CompAbilityUserMagic magic) => Skills(magic.MagicData)
            .FirstOrDefault(skill => skill.label == "TM_TechnoTurret_eff")?.level ?? 0;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
            ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher,origin,usedTarget,intendedTarget,hitFlags,preventFriendlyFire,equipment,targetCoverDef);
            var magic = SourcePawn?.GetCompAbilityUserMagic();
            if (localAbilityDef == TorannMagicDefOf.TM_TechnoTurret && magic?.Mana != null)
            {
                refundableMana = magic.ActualManaCost(TorannMagicDefOf.TM_TechnoTurret);
                if (!SourcePawn.IsColonist && ModOptions.Settings.Instance.AIAggressiveCasting) refundableMana *= .5f;
            }
        }

        internal static bool CanDeploy(Map map, IntVec3 cell)
        {
            if (map == null || map.Disposed || Scribe.mode != LoadSaveMode.Inactive || !cell.IsValid ||
                map.thingGrid == null || !cell.InBounds(map)) return false;
            // The targeter can repaint during map teardown after the native grids have been disposed.
            var normal = map.pathing?.Normal?.pathGrid;
            var fences = map.pathing?.FenceBlocked?.pathGrid;
            if (normal == null || fences == null || !normal.Grid_Unsafe.IsCreated || !fences.Grid_Unsafe.IsCreated ||
                !cell.Standable(map)) return false;
            var def = ThingDef.Named("TM_TechnoTurret_Base");
            return !cell.GetThingList(map).Any(t => t is Pawn || t is Building || GenSpawn.SpawningWipes(def,t.def));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref efficiency,"TM_turretEfficiency");
            Scribe_Values.Look(ref refundableMana,"TM_turretRefund");
            Scribe_Values.Look(ref resolved,"TM_turretResolved");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (resolved || Destroyed) return;
            resolved = true;
            var map = Map;
            var owner = SourcePawn;
            var magic = owner?.GetCompAbilityUserMagic();
            try
            {
                if (magic == null || !CanDeploy(map,Position))
                {
                    if (magic?.Mana != null) magic.Mana.CurLevel += refundableMana;
                    return;
                }
                if (!projected)
                {
                    var skills = Skills(magic.MagicData);
                    pwrVal = SpellRank(owner,skills,SkillStem,"_pwr");
                    verVal = SpellRank(owner,skills,SkillStem,"_ver");
                    efficiency = Efficiency(magic);
                    arcaneDmg = magic.arcaneDmg;
                }
                var def = ThingDef.Named(verVal >= 10 ? "TM_TechnoTurret_Base_RL_MTR" : verVal >= 5 ? "TM_TechnoTurret_Base_RL" : "TM_TechnoTurret_Base");
                var turret = (Building_TechnoTurret)ThingMaker.MakeThing(def,ThingDefOf.Steel);
                turret.SetFaction(owner.Faction);
                var body = launcher as Pawn;
                turret.Deploy(owner, body?.Spawned == true && body.Map == map ? body.Position : Position,
                    pwrVal,verVal,efficiency,arcaneDmg);
                GenSpawn.Spawn(turret,Position,map,Rot4.North);
                GenClamor.DoClamor(this,2.1f,ClamorDefOf.Impact);
                ThrowSparks();
            }
            finally { Destroy(); }
        }

        private void ThrowSparks()
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 rndPos = this.DrawPos;
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndPos, this.Map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.8f, 1.2f));
                rndPos = this.DrawPos;
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.ElectricalSpark, rndPos, this.Map, Rand.Range(.4f, .7f), .2f, .05f, .1f, 0, 0, 0, Rand.Range(0, 360));
            }
        }
    }
}
