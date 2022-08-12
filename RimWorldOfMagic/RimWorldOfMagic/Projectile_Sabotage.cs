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

    class Projectile_Sabotage : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 1800;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 120;
        int lastStrike = 0;
        int targetThingCount = 0;
        bool initialized = false;

        List<IntVec3> targetCells;
        List<SabotageThing> targetThings;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            Pawn caster = this.launcher as Pawn;
            if (!this.initialized)
            {
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();

                pwrVal = comp.MagicData.MagicPowerSkill_Sabotage.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Sabotage_pwr").level;
                verVal = comp.MagicData.MagicPowerSkill_Sabotage.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Sabotage_ver").level;
                if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                if (settingsRef.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, this.Map, false), MaintenanceType.None);
                info.pitchFactor = .5f;
                info.volumeFactor = .8f;
                SoundDefOf.PsychicPulseGlobal.PlayOneShot(info);
                Effecter SabotageEffect = TorannMagicDefOf.TM_SabotageExplosion.Spawn();
                SabotageEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
                SabotageEffect.Cleanup();
                targetCells = new List<IntVec3>();
                targetCells.Clear();
                targetCells = GenRadial.RadialCellsAround(base.Position, this.def.projectile.explosionRadius, true).ToList();
                this.targetThings = new List<SabotageThing>();
                this.targetThings.Clear();
                this.initialized = true;

                Pawn targetPawn = null;
                Building targetBuilding = null;

                for (int i = 0; i < this.targetCells.Count; i++)
                {
                    if (Rand.Chance((.5f + (.1f * verVal)) * this.arcaneDmg))
                    {
                        float rnd = Rand.Range(0, 1f);
                        targetPawn = this.targetCells[i].GetFirstPawn(this.Map);
                        if (targetPawn != null)
                        {
                            if (TM_Calc.IsRobotPawn(targetPawn))
                            {
                                TM_Action.DoAction_SabotagePawn(targetPawn, caster, rnd, pwrVal, this.arcaneDmg, this.launcher);
                                this.age = this.duration;
                            }
                            else
                            {
                                targetPawn = null;
                                //Log.Message("pawn not a robot, mechanoid, or android");
                            }
                        }

                        targetBuilding = this.targetCells[i].GetFirstBuilding(this.Map);
                        if (targetPawn == null && targetBuilding != null)
                        {
                            CompPower compP = targetBuilding.GetComp<CompPower>();
                            CompPowerTrader cpt = targetBuilding.GetComp<CompPowerTrader>();
                            if (compP != null && compP.Props.basePowerConsumption != 0 && cpt != null && cpt.powerOutputInt != 0)
                            {
                                if (true)
                                {
                                    //stun/electrical explosion
                                    GenExplosion.DoExplosion(targetBuilding.Position, base.Map, 2 + pwrVal + Mathf.RoundToInt(cpt.powerOutputInt / 400), DamageDefOf.Stun, null);
                                    GenExplosion.DoExplosion(targetBuilding.Position, base.Map, 1 + pwrVal + Mathf.RoundToInt(cpt.powerOutputInt / 600), TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, null);

                                }
                                else if (rnd <= .66f)
                                {
                                    //electrical burn short circuit
                                }
                                else
                                {

                                }

                            }

                            Building_Battery targetBattery = targetBuilding as Building_Battery;
                            if (targetBattery != null && targetBattery.def.thingClass.ToString() == "RimWorld.Building_Battery")
                            {
                                CompPowerBattery compB = targetBattery.GetComp<CompPowerBattery>();
                                if (rnd <= .5f)
                                {
                                    Traverse.Create(root: targetBattery).Field(name: "ticksToExplode").SetValue(Rand.Range(40, 130) - (5*pwrVal));
                                    compB.SetStoredEnergyPct(.81f);
                                }
                                else
                                {
                                    GenExplosion.DoExplosion(targetBattery.Position, base.Map, 2 + pwrVal + Mathf.RoundToInt(compB.StoredEnergy / 200), DamageDefOf.EMP, null);
                                    compB.DrawPower(compB.StoredEnergy);
                                }

                            }

                            Building_TurretGun targetTurret = targetBuilding as Building_TurretGun;
                            if (targetTurret != null && targetTurret.gun != null)
                            {
                                if (rnd <= .5f)
                                {
                                    targetTurret.SetFaction(Faction.OfAncientsHostile, null);
                                }
                                else
                                {
                                    GenExplosion.DoExplosion(targetTurret.Position, base.Map, 2 + pwrVal, TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, null); //20 default damage
                                }
                            }
                        }
                        else
                        {
                            //Log.Message("no thing to sabotage");
                        }
                        targetPawn = null;
                        targetBuilding = null;
                    }
                }
            }
            else if(this.targetThings.Count > 0)
            {
                this.age = this.duration;
            }
            else
            {
                this.age = this.duration;
            }

            
        }            

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "shockDelay", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrike, "lastStrike", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
        }
    }    
}


