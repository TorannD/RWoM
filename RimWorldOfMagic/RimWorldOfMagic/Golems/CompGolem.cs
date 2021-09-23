using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using AbilityUser;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class CompGolem : ThingComp, IThingHolder
	{
        private bool initialized = false;
        public bool shouldDespawn = false;
        public bool despawnNow = false;
        public int age = 0;
        public IntVec3 dormantPosition = new IntVec3();
        public Building_TMGolemBase dormantThing;
        public int dormantHealth;
        public Rot4 dormantRotation;

        public Pawn threatTarget = null;
        public float threatRange = 40f;

        public List<TM_GolemAbility> abilityList = new List<TM_GolemAbility>();

        ThingOwner innerContainer;

        private TM_Golem golem;
        private List<TM_GolemUpgrade> upgrades;        

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.shouldDespawn, "shouldDespawn", false);
            Scribe_Values.Look<int>(ref this.age, "age", 0);
            Scribe_Collections.Look<TM_GolemUpgrade>(ref this.upgrades, "upgrades", LookMode.Deep);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look<int>(ref this.dormantHealth, "dormantHealth", 0);
            Scribe_Values.Look<Rot4>(ref this.dormantRotation, "dormantRotation", Rot4.South, false);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public TM_Golem Golem
        {
            get
            {
                if (golem == null)
                {
                    golem = TM_GolemUtility.GetGolemFromThingDef(parent.def);
                }
                return golem;
            }
        }

        public List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                if (upgrades == null)
                {
                    upgrades = new List<TM_GolemUpgrade>();
                    upgrades.Clear();
                    upgrades.AddRange(Golem.upgrades);
                }
                return upgrades;
            }
        }

        public CompProperties_Golem Props
        {
            get
            {
                return (CompProperties_Golem)this.props;
            }
        }

        public CompGolem()
        {
            innerContainer = new ThingOwner<Thing>(this);
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

        public void CopyUpgrades(List<TM_GolemUpgrade> copiedUpgrades)
        {
            upgrades = new List<TM_GolemUpgrade>();
            upgrades.Clear();
            upgrades.AddRange(copiedUpgrades);
        }

        private bool ShouldDespawnNow
        {
            get
            {
                if(despawnNow)
                {
                    return true;
                }
                if(this.shouldDespawn)
                {
                    if(Pawn.Position == dormantPosition)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        protected virtual void Initialize()
        {
            this.shouldDespawn = false;
            this.despawnNow = false;
            ApplyNeeds();
            ApplyUpgrades();
            ApplyDamages();
            DeSpawnGolemWorkstation();
        }

        protected virtual void ApplyNeeds()
        {
            Need_GolemEnergy need_ge = Pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
            if (need_ge != null)
            {
                CompGolemEnergyHandler cgeh = dormantThing.TryGetComp<CompGolemEnergyHandler>();
                if (cgeh != null)
                {
                    need_ge.CurLevel = cgeh.StoredEnergy;
                }
            }
        }

        protected virtual void ApplyUpgrades()
        {
            ApplyUpgradeParts();
            ApplyUpgradeHediffs();
            ApplyAbilities();
        }

        protected virtual void ApplyUpgradeParts()
        {
            
            foreach(TM_GolemUpgrade gu in  Upgrades)
            {
                if(gu.bodypart != null && gu.currentLevel <= 0 && gu.partRequiresUpgrade)
                {
                    foreach(Hediff hd in Pawn.health.hediffSet.hediffs)
                    {
                        if(hd.Part.def == gu.bodypart)
                        {
                            Pawn.health.hediffSet.hediffs.Remove(hd);
                            break;
                        }
                    }
                    
                }
            }
        }

        protected virtual void ApplyUpgradeHediffs()
        {
            List<BodyPartRecord> bodyParts = Pawn.health.hediffSet.GetNotMissingParts().ToList();
            if(bodyParts != null)
            {
                foreach (BodyPartRecord record in bodyParts)
                {
                    foreach(TM_GolemUpgrade gu in Upgrades)
                    {
                        if(gu.currentLevel > 0 && gu.bodypart == record.def)
                        {
                            HealthUtility.AdjustSeverity(Pawn, gu.hediff, gu.currentLevel - .5f);
                        }
                    }
                }
            }
        }

        protected virtual void ApplyAbilities()
        {
            if(abilityList == null)
            {
                abilityList = new List<TM_GolemAbility>();
            }
            abilityList.Clear();
            foreach(TM_GolemUpgrade gu in upgrades)
            {
                if(gu.ability != null && gu.currentLevel > 0)
                {
                    abilityList.Add(gu.ability);
                }
            }
        }

        protected virtual void ApplyDamages()
        {
            
        }

        protected virtual void DeSpawnGolemWorkstation()
        {
            if (!dormantThing.DestroyedOrNull() && dormantThing.Spawned)
            {
                this.dormantHealth = this.dormantThing.HitPoints;
                this.dormantThing.DeSpawn(DestroyMode.Vanish);
            }
        }

        public override void CompTick()
        {
            if (this.age > 0)
            {
                if (!this.initialized)
                {
                    Initialize();
                    this.initialized = true;
                }

                if (this.Pawn.Spawned)
                {
                    if (!this.Pawn.Downed)
                    {
                        //if (Find.TickManager.TicksGame % 300 == 0)
                        //{
                        //    DetermineThreats();
                        //    if(threatTarget != null)
                        //    {
                        //        UseAbilities();
                        //    }
                        //}
                    }

                    if(this.ShouldDespawnNow)
                    {
                        Job job = new Job(TorannMagicDefOf.JobDriver_GolemDespawn, dormantPosition);
                        Pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        this.shouldDespawn = false;
                    }                    
                }                
            }
            age++;
        }

        public void DeSpawnGolem()
        {
            for(int i =0; i < 4; i++)
            {
                Vector3 rndPos = dormantPosition.ToVector3Shifted();
                rndPos.x += Rand.Range(-1f, 1f);
                rndPos.z += Rand.Range(-1f, 1f);
                FleckMaker.ThrowSmoke(rndPos, Pawn.Map, Rand.Range(.6f, 1.2f));                
            }
            Find.CameraDriver.shaker.DoShake(.25f);
            Building_TMGolemBase spawnedThing = null;
            AbilityUser.SpawnThings spawnables = new SpawnThings();
            spawnables.def = Golem.golemWorkstationDef;
            spawnables.spawnCount = 1;
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = Pawn.Faction;
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff && Golem.golemStuff != null)
                {
                    stuff = Golem.golemStuff;
                }
                spawnedThing = ThingMaker.MakeThing(def, stuff) as Building_TMGolemBase;
                GenSpawn.Spawn(spawnedThing, Pawn.Position, this.Pawn.Map, dormantRotation, WipeMode.Vanish, false);
                spawnedThing.HitPoints = dormantHealth;
                spawnedThing.tmpGolem = Pawn;
            }
            this.initialized = false;
            this.parent.DeSpawn(DestroyMode.Vanish);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);

        }

        public void UseAbilities()
        {
            foreach(TM_GolemUpgrade gu in upgrades)
            {
                bool success = false;
                if(gu.ability != null && gu.currentLevel > 0)
                {
                   //
                }
                if (success) break;
            }
        }

        

        private void DetermineThreats()
        {
            this.threatTarget = null;
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
                                                this.threatTarget = allPawns[i];
                                                break;
                                            }                                            
                                        }
                                        else
                                        {
                                            this.threatTarget = allPawns[i];
                                            break;
                                        }                                      
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                //Log.Message("Error processing threats" + ex);
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
            if(target is Pawn)
            {
                return !(target as Pawn).Downed;
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
    }
}