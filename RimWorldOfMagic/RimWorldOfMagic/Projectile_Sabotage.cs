using Verse;
using Verse.AI;
using Verse.Sound;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;
using HarmonyLib;


namespace TorannMagic
{
    public struct SabotageThing
    {
        public Thing thing;
        public int duration;
        public float effectRadius;
        public int effectFrequency;

        public SabotageThing(Thing sThing, int sDuration, float sEffectRadius, int sEffectFrequency)
        {
            thing = sThing;
            duration = sDuration;
            effectRadius = sEffectRadius;
            effectFrequency = sEffectFrequency;
        }
    }

    class Projectile_Sabotage : ClassProgression.Projectile_SpiritBolt
    {
        int age = -1;
        int duration = 1800;
        bool initialized;
        protected override string SkillStem => "TM_Sabotage";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Sabotage;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        protected override void Tick()
        {
            // Native impact is complete. A loaded projectile may no longer enter Impact again.
            if (initialized)
            {
                base.Destroy();
                return;
            }
            base.Tick();
            this.age++;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            if (Destroyed || map == null) return;
            if (initialized) { age = duration; return; }
            initialized = true;
            var caster = SourcePawn;
            if (caster == null) { age = duration; return; }
            if (!projected)
            {
                var magic = caster.GetCompAbilityUserMagic();
                if (magic == null) { age = duration; return; }
                var skills = Skills(magic.MagicData);
                pwrVal = SpellRank(caster, skills, SkillStem, "_pwr");
                verVal = SpellRank(caster, skills, SkillStem, "_ver");
                arcaneDmg = magic.arcaneDmg;
                if (ModOptions.Settings.Instance.AIHardMode && !caster.IsColonist) pwrVal = verVal = 3;
            }
            SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, this.Map, false), MaintenanceType.None);
            info.pitchFactor = .5f;
            info.volumeFactor = .8f;
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(info);
            Effecter SabotageEffect = TorannMagicDefOf.TM_SabotageExplosion.Spawn();
            SabotageEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
            SabotageEffect.Cleanup();
            var targetCells = GenRadial.RadialCellsAround(Position, def.projectile.explosionRadius, true)
                .Where(cell => cell.InBounds(map)).ToList();

            var hitBuildings = new HashSet<Building>();
            foreach (var cell in targetCells)
            {
                if (!Rand.Chance((.5f + .1f * verVal) * arcaneDmg)) continue;
                float roll = Rand.Value;
                var pawn = cell.GetFirstPawn(map);
                if (pawn != null && TM_Calc.IsRobotPawn(pawn))
                {
                    TM_Action.DoAction_SabotagePawn(pawn, caster, roll, pwrVal, arcaneDmg, SpellInstigator);
                    age = duration;
                    continue;
                }
                var building = cell.GetFirstBuilding(map);
                if (building != null && hitBuildings.Add(building)) SabotageBuilding(building, map, roll);
            }
        }

        private void SabotageBuilding(Building building, Map map, float roll)
        {
            var instigator = projected ? SpellInstigator : null;
            var power = building.GetComp<CompPowerTrader>();
            if (building.GetComp<CompPower>() is CompPower comp && comp.Props.PowerConsumption != 0 && power != null && power.powerOutputInt != 0)
            {
                GenExplosion.DoExplosion(building.Position, map, 2 + pwrVal + Mathf.RoundToInt(power.powerOutputInt / 400), DamageDefOf.Stun, instigator);
                GenExplosion.DoExplosion(building.Position, map, 1 + pwrVal + Mathf.RoundToInt(power.powerOutputInt / 600), TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, instigator);
            }
            if (building is Building_Battery battery && battery.def.thingClass == typeof(Building_Battery))
            {
                var storage = battery.GetComp<CompPowerBattery>();
                if (roll <= .5f)
                {
                    Traverse.Create(battery).Field("ticksToExplode").SetValue(Rand.Range(40, 130) - 5 * pwrVal);
                    storage.SetStoredEnergyPct(.81f);
                }
                else
                {
                    GenExplosion.DoExplosion(battery.Position, map, 2 + pwrVal + Mathf.RoundToInt(storage.StoredEnergy / 200), DamageDefOf.EMP, instigator);
                    storage.DrawPower(storage.StoredEnergy);
                }
            }
            if (building is Building_TurretGun turret && turret.gun != null)
            {
                if (roll <= .5f) turret.SetFaction(Faction.OfAncientsHostile, null);
                else GenExplosion.DoExplosion(turret.Position, map, 2 + pwrVal, TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, instigator);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
        }
    }    
}


