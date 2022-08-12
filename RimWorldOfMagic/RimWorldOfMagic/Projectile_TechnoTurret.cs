using RimWorld;
using AbilityUser;
using Verse;
using System.Linq;
using Verse.AI;
using UnityEngine;
using HarmonyLib;


namespace TorannMagic
{
    public class Projectile_TechnoTurret : Projectile_AbilityBase
    {

        private int verVal;
        private int pwrVal;
        private int effVal;
        private float arcaneDmg = 1f;

        Thing turret = null;

        protected override void Impact(Thing hitThing)
        {
            Map map = this.launcher.Map;
            Pawn pawn = this.launcher as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoTurret_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoTurret_ver");
            effVal = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoTurret_eff").level;
            verVal = ver.level;
            pwrVal = pwr.level;
            arcaneDmg = comp.arcaneDmg;
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }

            //if ((pawn.Position.IsValid && pawn.Position.Standable(map)))
            //{
                AbilityUser.SpawnThings tempPod = new SpawnThings();
                IntVec3 shiftPos = pawn.Position;

                tempPod.def = ThingDef.Named("TM_TechnoTurret_Base");
                Thing turretGun = new Thing();

                turretGun.def = ThingDef.Named("Gun_Mark-IV");
                if (this.verVal >= 5)
                {
                    tempPod.def = ThingDef.Named("TM_TechnoTurret_Base_RL");
                    turretGun.def = ThingDef.Named("Gun_Mark-IV_RL");
                }
                if(this.verVal >= 10)
                {
                    tempPod.def = ThingDef.Named("TM_TechnoTurret_Base_RL_MTR");
                    turretGun.def = ThingDef.Named("Gun_Mark-IV_RL_MTR");
                }

                turretGun.def.SetStatBaseValue(StatDefOf.AccuracyTouch, 0.8f + (.01f * pwrVal));
                turretGun.def.SetStatBaseValue(StatDefOf.AccuracyShort, 0.75f + (.01f * pwrVal));
                turretGun.def.SetStatBaseValue(StatDefOf.AccuracyMedium, 0.65f + (.01f * pwrVal));
                turretGun.def.SetStatBaseValue(StatDefOf.AccuracyLong, 0.35f + (.01f * pwrVal));
                turretGun.def.Verbs.FirstOrDefault().burstShotCount = Mathf.RoundToInt((6 + (.4f * pwrVal)) * this.arcaneDmg);
                turretGun.def.Verbs.FirstOrDefault().warmupTime = 1.5f - (.03f * pwrVal);
                turretGun.def.Verbs.FirstOrDefault().range = (Mathf.RoundToInt((35 + verVal)*this.arcaneDmg));

                tempPod.def.building.turretGunDef = turretGun.def;

                tempPod.def.SetStatBaseValue(StatDefOf.MaxHitPoints, 200 + (10 * effVal));
                tempPod.spawnCount = 1;
                try
                {
                    this.turret = TM_Action.SingleSpawnLoop(pawn, tempPod, base.Position, map, 3600 + (300 * effVal), true, false, pawn.Faction, true);
                    this.turret.def.building.turretBurstCooldownTime = 4.5f - (.1f * pwrVal);

                    Building_TechnoTurret b_tt = this.turret as Building_TechnoTurret;
                    b_tt.manPawn = pawn;
                    b_tt.iCell = this.launcher.Position;

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
                catch
                {
                    comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_SummonPylon);
                    Log.Message("TM_Exception".Translate(
                            pawn.LabelShort,
                            "techno turret"
                        ));
                }

            //}
            //else
            //{
            //    Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
            //    comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_TechnoTurret));
            //}


            //if ((turret != null && turret.Spawned && turret.Position.IsValid))
            //{
            //    //turret.def.interactionCellOffset = (this.launcher.Position - base.Position);          
                
            //    Job job = new Job(JobDefOf.ManTurret, turret);
            //    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                
            //    //this.Ability.PostAbilityAttempt();
            //}
            //else
            //{
            //    Log.Message("turret was null");
            //}
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            Destroy();
            //base.Impact(hitThing);
        }
    }
}
