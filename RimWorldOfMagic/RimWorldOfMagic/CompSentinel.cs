using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using AbilityUser;

namespace TorannMagic
{
    public class CompSentinel : ThingComp
	{
        private bool initialized = false;
        List<Pawn> threatList = new List<Pawn>();
        public LocalTargetInfo target = null;
        private int age = -1;
        private bool shouldDespawn = false;
        public IntVec3 sentinelLoc;
        public Rot4 rotation;
        public Pawn sustainerPawn = null;

        private int killNow = 0;

        private int threatRange = 40;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<Rot4>(ref this.rotation, "rotation", Rot4.South, false);
            Scribe_Values.Look<IntVec3>(ref this.sentinelLoc, "sentinelLoc", default(IntVec3), false);
            Scribe_References.Look<Pawn>(ref this.sustainerPawn, "sustainerPawn", false);
        }

        private Pawn Pawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                bool flag = pawn == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
        }

        private bool ShouldDespawn
        {
            get
            {
                return target == null && this.shouldDespawn;
            }
        }

        public override void CompTick()
        {
            if (this.age > 0)
            {
                if (!this.initialized)
                {
                    this.initialized = true;
                }

                if (this.Pawn.Spawned)
                {
                    if (!this.Pawn.Downed)
                    {
                        if (Find.TickManager.TicksGame % 300 == 0)
                        {
                            if (this.sustainerPawn != null)
                            {
                                DetermineThreats();
                                DetermineSustainerPawn();

                                if (this.ShouldDespawn && this.Pawn.Position != this.sentinelLoc)
                                {
                                    if (sentinelLoc.Walkable(this.Pawn.Map))
                                    {
                                        //this.Pawn.jobs.ClearQueuedJobs();
                                        //this.Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                                        Job job = new Job(JobDefOf.Goto, this.sentinelLoc);
                                        this.Pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                                    }
                                    else
                                    {
                                        Messages.Message("TM_SentinelCannotReturn".Translate(
                                        this.sustainerPawn.LabelShort
                                        ), MessageTypeDefOf.RejectInput, false);
                                        this.Pawn.Destroy(DestroyMode.Vanish);
                                    }

                                }

                                if(this.target.Thing is Pawn prisonerPawn)
                                {
                                    if(prisonerPawn.IsPrisoner)
                                    {
                                        Job job = new Job(JobDefOf.AttackMelee, prisonerPawn);
                                        this.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }
                                }
                            }
                            else
                            {
                                Log.Message("Sentinel has despawned due to lack of mana to sustain it.");
                                this.Pawn.Destroy(DestroyMode.Vanish);
                            }
                        }
                    }
                    else
                    { 
                        if (this.killNow > 100)
                        {
                            DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 100, 0, (float)-1, this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                            this.Pawn.TakeDamage(dinfo);
                        }
                        this.killNow++;
                    }

                    if(this.ShouldDespawn && this.Pawn.Position == this.sentinelLoc)
                    {
                        SingleSpawnLoop();
                        this.Pawn.Destroy(DestroyMode.Vanish);
                    }
                    
                }                
            }
            age++;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);
            if(dinfo.Instigator != null)
            {
                Thing instigatorThing = dinfo.Instigator;
                if(instigatorThing is Building)
                {
                    if (instigatorThing.Faction != null && instigatorThing.Faction != this.Pawn.Faction)
                    {
                        
                    }
                }
            }
        }

        private void DetermineSustainerPawn()
        {
            if(this.sustainerPawn.DestroyedOrNull() || this.sustainerPawn.Dead)
            {
                this.Pawn.Kill(null, null);
            }
        }

        private void DetermineThreats()
        {
            this.target = null;
            try
            {                
                List<Pawn> allPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count(); i++)
                {
                    if (!allPawns[i].DestroyedOrNull() && allPawns[i] != this.Pawn)
                    {
                        if (!allPawns[i].Dead && !allPawns[i].Downed && !allPawns[i].IsPrisonerInPrisonCell())
                        {
                            if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.threatRange)
                            {
                                if (allPawns[i].Faction != null && allPawns[i].Faction != this.Pawn.Faction)
                                {
                                    if (FactionUtility.HostileTo(this.Pawn.Faction, allPawns[i].Faction))
                                    {
                                        if(ModCheck.Validate.PrisonLabor.IsInitialized())
                                        {
                                            if(!allPawns[i].IsPrisoner)
                                            {
                                                this.target = allPawns[i];
                                                break;
                                            }                                            
                                        }
                                        else
                                        {
                                            this.target = allPawns[i];
                                            break;
                                        }                                      
                                    }
                                }
                            }
                        }
                    }
                }

                if (this.target != null && this.Pawn.meleeVerbs.TryGetMeleeVerb(this.target.Thing) != null)
                {
                    Thing currentTargetThing = this.Pawn.CurJob.targetA.Thing;
                    if (currentTargetThing == null)
                    {
                        this.Pawn.TryStartAttack(this.target);
                    }
                }
                else
                {
                    this.shouldDespawn = true;
                }
            }
            catch(NullReferenceException ex)
            {
                //Log.Message("Error processing threats" + ex);
            }
        }

        public void DamageEntities(Thing e, float d, DamageDef type, Thing instigator)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, Rand.Range(0,amt), (float)-1, instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }

        public bool TargetIsValid(Thing target)
        {
            if(target.DestroyedOrNull())
            {
                return false;
            }
            if(!target.Spawned)
            {
                return false;
            }
            if(target is Pawn targetPawn)
            {
                return !targetPawn.Downed;
            }
            if(target.Position.DistanceToEdge(this.Pawn.Map) < 8)
            {
                return false;
            }
            if(target.Faction != null)
            {
                return target.Faction != this.Pawn.Faction;
            }
            return true;
        }

        public void SingleSpawnLoop()
        {
            Thing spawnedThing = null;
            AbilityUser.SpawnThings spawnables = new SpawnThings();
            spawnables.def = ThingDef.Named("TM_Sentinel");
            spawnables.spawnCount = 1;
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = this.sustainerPawn.Faction;
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff)
                {
                    stuff = ThingDef.Named("BlocksGranite");
                }
                spawnedThing = ThingMaker.MakeThing(def, stuff);
                GenSpawn.Spawn(spawnedThing, this.sentinelLoc, this.Pawn.Map, this.rotation, WipeMode.Vanish, false);
                float totalHealth = 0;
                float healthDeficit = 0;
                using (IEnumerator<BodyPartRecord> enumerator = this.Pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;
                        totalHealth += rec.def.hitPoints;
                        IEnumerable<Hediff_Injury> arg_BB_0 = this.Pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;

                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {                            
                            healthDeficit += current.Severity;
                        }
                    }
                }
                CompAbilityUserMagic comp = this.sustainerPawn.GetCompAbilityUserMagic();
                comp.summonedSentinels.Remove(this.Pawn);
                comp.summonedSentinels.Add(spawnedThing);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 10*healthDeficit, 0, (float)-1, this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                spawnedThing.TakeDamage(dinfo);

            }
        }
    }
}