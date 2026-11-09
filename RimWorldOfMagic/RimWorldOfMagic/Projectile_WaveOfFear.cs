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
        bool shotBound;
        Pawn skillOwner;

        internal void BindShot(Pawn owner)
        {
            if (shotBound) return;
            shotBound = true;
            skillOwner = owner;
            arcaneDmg = owner?.GetCompAbilityUserMight()?.mightPwr ?? 1f;
            if (owner == null) return;
            pwrVal = TM_Calc.GetSkillPowerLevel(owner, TorannMagicDefOf.TM_WaveOfFear);
            verVal = TM_Calc.GetSkillVersatilityLevel(owner, TorannMagicDefOf.TM_WaveOfFear);
            effVal = TM_Calc.GetSkillEfficiencyLevel(owner, TorannMagicDefOf.TM_WaveOfFear);
        }

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
            Scribe_Values.Look(ref shotBound, "TM_fieldBound");
            Scribe_References.Look(ref skillOwner, "TM_fieldOwner");
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 40, false);
            Scribe_Values.Look<int>(ref this.waveRange, "waveRange", 1, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 4, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look(ref effVal, "effVal");
            Scribe_Values.Look(ref arcaneDmg, "arcaneDmg", 1f);
            Scribe_Collections.Look(ref affectedPawns, "affectedPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) affectedPawns ??= new List<Pawn>();
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

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            this.caster = this.launcher as Pawn;
            if (caster == null || !caster.Spawned || map != null && caster.Map != map) return;

            if(!this.initialized)
            {
                CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                //pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_pwr").level;
                //verVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_ver").level;
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_WaveOfFear, "TM_WaveOfFear", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_WaveOfFear, "TM_WaveOfFear", "_pwr", true);
                //effVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_eff").level;
                //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                //    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    pwrVal = mpwr.level;
                //    verVal = mver.level;
                //}
                if (!shotBound) BindShot(caster);
                Pawn resourceOwner = skillOwner ?? caster;
                
                //if (!caster.IsColonist && ModOptions.Settings.Instance.AIHardMode)
                //{
                //    pwrVal = 3;
                //    verVal = 3;
                //}                
                for (int h = 0; h < resourceOwner.health.hediffSet.hediffs.Count; h++)
                {
                    if (resourceOwner.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                    {
                        hediff = resourceOwner.health.hediffSet.hediffs[h];
                    }
                }
                if (hediff != null)
                {
                    this.radius = 4 + (.8f * verVal) + (.07f * hediff.Severity);
                    HealthUtility.AdjustSeverity(resourceOwner, hediff.def, -(25f * (1 - .1f * this.effVal)));
                }
                else
                {
                    this.radius = 4 + (.8f * verVal);
                }
                this.duration = Mathf.RoundToInt(this.radius * 10);
                this.affectedPawns = new List<Pawn>();
                this.affectedPawns.Clear();
                if (this.Map != null)
                {
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, this.Map, false), MaintenanceType.None);
                    TorannMagicDefOf.TM_GaspingAir.PlayOneShot(info);
                    Effecter FearWave = TorannMagicDefOf.TM_FearWave.Spawn();
                    FearWave.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(base.Position, this.Map, false));
                    FearWave.Cleanup();
                    SearchAndFear();
                }
                this.initialized = true;
            }  

        }

        public void SearchAndFear()
        {
            // The persistent wave belongs to its original map, not to a carrier that left it.
            if (caster == null || !caster.Spawned || caster.DeadOrDowned || Map == null || caster.Map != Map) return;
            List<Pawn> mapPawns = this.caster.Map.mapPawns.AllPawnsSpawned.ToList();
            if (mapPawns != null && mapPawns.Count > 0)
            {
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn victim = mapPawns[i];
                    if (!victim.DestroyedOrNull() && !victim.Dead && victim.Map != null && victim.health != null && victim.health.hediffSet != null && !victim.Downed && victim.mindState != null && !victim.InMentalState && !this.affectedPawns.Contains(victim))
                    {
                        if (victim.Faction != null && victim.Faction != caster.Faction && (victim.Position - caster.Position).LengthHorizontal < this.waveRange)
                        {
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(skillOwner ?? caster, victim, true)))
                            {
                                LocalTargetInfo t = new LocalTargetInfo(victim.Position + (6 * this.arcaneDmg * TM_Calc.GetVector(caster.DrawPos, victim.DrawPos)).ToIntVec3());
                                if (victim.jobs != null)
                                {
                                    Job job = new Job(JobDefOf.FleeAndCower, t);
                                    victim.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }
                                HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_WaveOfFearHD"), .5f + pwrVal);
                                this.affectedPawns.Add(victim);
                            }
                            else
                            {
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                            }
                        }
                        else if(victim.Faction == null && (victim.Position - caster.Position).LengthHorizontal < this.waveRange)
                        {
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(skillOwner ?? caster, victim, true)))
                            {
                                LocalTargetInfo t = new LocalTargetInfo(victim.Position + (6 * this.arcaneDmg * TM_Calc.GetVector(caster.DrawPos, victim.DrawPos)).ToIntVec3());
                                if (victim.jobs != null)
                                {
                                    Job job = new Job(JobDefOf.FleeAndCower, t);
                                    victim.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }
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

        protected override void Tick()
        {
            base.Tick();
            this.age++;
            // Projectile impacts run on TickInterval in 1.7. The persistent field needs
            // its own cadence rather than relying on another impact at a matching tick.
            if (initialized && !Destroyed && age <= duration && Find.TickManager.TicksGame % waveDelay == 0 &&
                caster?.Spawned == true && !caster.DeadOrDowned && caster.Map == Map)
            {
                SearchAndFear();
                waveRange++;
            }
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


