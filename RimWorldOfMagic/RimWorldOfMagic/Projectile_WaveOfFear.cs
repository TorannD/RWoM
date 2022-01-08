using Verse;
using RimWorld;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;
using Verse.Sound;
using Verse.AI;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_WaveOfFear : Projectile_AbilityBase
    {
        int pwrVal = 0;
        int verVal = 0;
        int effVal = 0;
        private float arcaneDmg = 1;
        Hediff hediff = null;
        int age = -1;
        int duration = 1800;
        float radius = 4;
        bool initialized = false;
        int waveDelay = 10;
        int waveRange = 1;
        Pawn caster;
        List<Pawn> affectedPawns;

        //if (!victim.IsWildMan() && victim.RaceProps.Humanlike && victim.mindState != null && !victim.InMentalState)
        //                        {
        //                            try
        //                            {
        //                                victim.mindState.mentalStateHandler.TryStartMentalState(TorannMagicDefOf.TM_PanicFlee);
        //                            }
        //                            catch (NullReferenceException ex)
        //                            {

        //                            }
        //                        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 40, false);
            Scribe_Values.Look<int>(ref this.waveRange, "waveRange", 1, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 4, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Values.Look<int>(ref this.waveDelay, "waveDelay", 10, false);
        }

        private int TicksLeft
        {
            get
            {
                return this.duration - this.age;
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            this.caster = this.launcher as Pawn;

            if(!this.initialized)
            {
                CompAbilityUserMight comp = caster.GetComp<CompAbilityUserMight>();
                //pwrVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_pwr").level;
                //verVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_ver").level;
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_WaveOfFear, "TM_WaveOfFear", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_WaveOfFear, "TM_WaveOfFear", "_pwr", true);
                //effVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_eff").level;
                //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mpwr = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                //    MightPowerSkill mver = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    pwrVal = mpwr.level;
                //    verVal = mver.level;
                //}
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_WaveOfFear);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_WaveOfFear);
                effVal = TM_Calc.GetSkillEfficiencyLevel(caster, TorannMagicDefOf.TM_WaveOfFear);
                this.arcaneDmg = comp.mightPwr;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                //if (!caster.IsColonist && settingsRef.AIHardMode)
                //{
                //    pwrVal = 3;
                //    verVal = 3;
                //}
                for (int h = 0; h < caster.health.hediffSet.hediffs.Count; h++)
                {
                    if (caster.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                    {
                        hediff = caster.health.hediffSet.hediffs[h];
                    }
                }
                if (hediff != null)
                {
                    this.radius = 4 + (.8f * verVal) + (.07f * hediff.Severity);
                    HealthUtility.AdjustSeverity(caster, hediff.def, -(25f * (1 - .1f * this.effVal)));
                }
                else
                {
                    this.radius = 4 + (.8f * verVal);
                }
                this.duration = Mathf.RoundToInt(this.radius * 10);
                this.affectedPawns = new List<Pawn>();
                this.affectedPawns.Clear();

                SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, this.Map, false), MaintenanceType.None);
                TorannMagicDefOf.TM_GaspingAir.PlayOneShot(info);
                Effecter FearWave = TorannMagicDefOf.TM_FearWave.Spawn();
                FearWave.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(base.Position, this.Map, false));
                FearWave.Cleanup();
                SearchAndFear();
                this.initialized = true;
            }  

            if(Find.TickManager.TicksGame % this.waveDelay == 0)
            {
                SearchAndFear();
                this.waveRange++;
            }
        }

        public void SearchAndFear()
        {
            List<Pawn> mapPawns = this.caster.Map.mapPawns.AllPawnsSpawned;
            if (mapPawns != null && mapPawns.Count > 0)
            {
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn victim = mapPawns[i];
                    if (!victim.DestroyedOrNull() && !victim.Dead && victim.Map != null && !victim.Downed && victim.mindState != null && !victim.InMentalState && !this.affectedPawns.Contains(victim))
                    {
                        if (victim.Faction != null && victim.Faction != caster.Faction && (victim.Position - caster.Position).LengthHorizontal < this.waveRange)
                        {
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, victim, true)))
                            {
                                LocalTargetInfo t = new LocalTargetInfo(victim.Position + (6 * this.arcaneDmg * TM_Calc.GetVector(caster.DrawPos, victim.DrawPos)).ToIntVec3());
                                Job job = new Job(JobDefOf.FleeAndCower, t);
                                victim.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_WaveOfFearHD"), .5f + pwrVal);
                                this.affectedPawns.Add(victim);
                            }
                            else
                            {
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                            }
                        }
                    }
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }
    }
}


