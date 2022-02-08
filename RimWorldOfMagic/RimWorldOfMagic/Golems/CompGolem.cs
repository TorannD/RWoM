using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;
using AbilityUser;
using TorannMagic.TMDefs;
using HarmonyLib;

namespace TorannMagic.Golems
{
    public class CompGolem : ThingComp, IThingHolder
	{
        //saved
        public int age = 0;
        private int nextActionTick = 0;
        public int actionTickAverage80 = 300;
        public IntVec3 dormantPosition = new IntVec3();
        public Map dormantMap;
        public Building_TMGolemBase dormantThing;
        public Rot4 dormantRotation;        

        public Thing threatTarget = null;
        public float threatRange = 40f;
        public Pawn pawnMaster = null;
        public bool followsMaster = true;
        public bool followsMasterDrafted = false;
        public bool remainDormantWhenUpgrading = true;
        public bool useAbilitiesWhenDormant = true;
        public bool checkThreatPath = false;

        public float minEnergyPctForAbilities = .2f;
        public float energyPctShouldRest = .1f;
        public float energyPctShouldAwaken = 1f;
        private Name golemName = NameTriple.FromString("Blank");
        public Name GolemName
        {
            get
            {
                if(golemName == null)
                {
                    golemName = NameTriple.FromString("Blank");
                }
                if(golemName.ToString() == "")
                {
                    golemName = NameTriple.FromString("Blank");
                }
                return golemName;
            }
            set
            {
                golemName = value;
            }
        }

        public List<TM_GolemAbility> abilityList = new List<TM_GolemAbility>();

        ThingOwner innerContainer;

        private TM_Golem golem;  

        public Thing ActiveThreat
        {
            get
            {
                if(threatTarget != null)
                {
                    if (threatTarget is Pawn)
                    {
                        Pawn p = threatTarget as Pawn;
                        if(p.DestroyedOrNull() || p.Dead || p.Downed || p.Map == null)
                        {
                            threatTarget = null;
                        }
                    }
                }
                return threatTarget;
            }
        }

        public bool TargetIsValid(Thing source, LocalTargetInfo target)
        {
            if (target == null)
            {
                return false;
            }
            if (target.HasThing)
            {
                Thing targetThing = target.Thing;
                if (targetThing.DestroyedOrNull())
                {
                    return false;
                }
                if (!targetThing.Spawned)
                {
                    return false;
                }
                if (targetThing is Pawn)
                {
                    Pawn p = targetThing as Pawn;
                    if (p.Dead || p.Downed)
                    {
                        return false;
                    }
                    if (checkThreatPath && p.CanReach(source, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.PassDoors))
                    {
                        return false;
                    }
                }
                if (!GenHostility.HostileTo(source, targetThing))
                {
                    return false;
                }
            }
            if (target.Cell.DistanceToEdge(source.Map) < 8)
            {
                return false;
            }
            if (checkThreatPath && !target.Cell.InAllowedArea(Pawn))
            {
                return false;
            }

            return true;
        }

        //unsaved variables
        private bool initialized = false;
        public bool shouldDespawn = false;
        public bool despawnNow = false;
        private int abilityTick = 0;
        private int abilityBurstTick = 0;
        private int abilityMaxTicks = 0;
        private LocalTargetInfo abilityTarget = null;
        private TM_GolemAbility activeAbility = null;
        private AbilityTargetType abilityTargetType = AbilityTargetType.None;
        private Effecter effecter;

        public float EnergyCostModifier
        {
            get
            {
                float mod = 1f;
                foreach(TM_GolemUpgrade gu in Upgrades)
                {
                    if(gu.golemUpgradeDef.abilityModifiers != null)
                    {
                        mod += (gu.golemUpgradeDef.abilityModifiers.energyCostModifier * gu.currentLevel);
                    }
                }
                return mod;
            }
        }
        public float DurationModifier
        {
            get
            {
                float mod = 1f;
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.golemUpgradeDef.abilityModifiers != null)
                    {
                        mod += (gu.golemUpgradeDef.abilityModifiers.durationModifier * gu.currentLevel);
                    }
                }
                return mod;
            }
        }
        public float DamageModifier
        {
            get
            {
                float mod = 1f;
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.golemUpgradeDef.abilityModifiers != null)
                    {
                        mod += (gu.golemUpgradeDef.abilityModifiers.damageModifier * gu.currentLevel);
                    }
                }
                return mod;
            }
        }
        public float CooldownModifier
        {
            get
            {
                float mod = 1f;
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.golemUpgradeDef.abilityModifiers != null)
                    {
                        mod += (gu.golemUpgradeDef.abilityModifiers.cooldownModifier * gu.currentLevel);
                    }
                }
                return mod;
            }
        }
        public float ProcessingModifier
        {
            get
            {
                float mod = 1f;
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.golemUpgradeDef.abilityModifiers != null)
                    {
                        mod += (gu.golemUpgradeDef.abilityModifiers.processingModifier * gu.currentLevel);
                    }
                }
                return mod;
            }
        }
        public float HealingModifier
        {
            get
            {
                float mod = 1f;
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.golemUpgradeDef.abilityModifiers != null)
                    {
                        mod += (gu.golemUpgradeDef.abilityModifiers.healingModifier * gu.currentLevel);
                    }
                }
                return mod;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.shouldDespawn, "shouldDespawn", false);
            Scribe_Values.Look<int>(ref this.age, "age", 0);
            Scribe_Deep.Look<TM_Golem>(ref this.golem, "golem");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look<int>(ref this.actionTickAverage80, "actionTickAverage80", 300);
            Scribe_Values.Look<Rot4>(ref this.dormantRotation, "dormantRotation", Rot4.South, false);
            Scribe_Values.Look<IntVec3>(ref this.dormantPosition, "dormantPosition", default(IntVec3));
            Scribe_References.Look<Map>(ref this.dormantMap, "dormantMap");
            Scribe_References.Look<Pawn>(ref this.pawnMaster, "pawnMaster");
            Scribe_Values.Look<bool>(ref this.followsMaster, "followsMaster", false);
            Scribe_Values.Look<bool>(ref this.followsMasterDrafted, "followsMasterDrafted", false);
            Scribe_Values.Look<float>(ref this.threatRange, "threatRange", 40f);
            Scribe_References.Look<Thing>(ref this.threatTarget, "threatTarget");
            Scribe_Values.Look<float>(ref this.energyPctShouldRest, "energyPctShouldRest", .1f);
            Scribe_Values.Look<float>(ref this.minEnergyPctForAbilities, "minEnergyForAbilities", .2f);
            Scribe_Values.Look<float>(ref this.energyPctShouldAwaken, "energyPctShouldAwaken", 1f);
            Scribe_Deep.Look<Name>(ref this.golemName, "golemName");
        }

        public bool AbilityActive => abilityTick <= abilityMaxTicks;

        public LocalTargetInfo AbilityTarget => abilityTarget;
        public TM_GolemAbility ActiveAbility => activeAbility;
        public Need_GolemEnergy Energy => Pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
        public bool HasEnergyForAbilities => Energy.CurEnergyPercent > minEnergyPctForAbilities;

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
                    golem = new TM_Golem(Pawn);
                }
                return golem;                
            }
            set => golem = value;
        }

        public List<TM_GolemUpgrade> Upgrades => Golem.upgrades;

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

        public TMPawnGolem PawnGolem => Pawn as TMPawnGolem;

        public Building_TMGolemBase InnerWorkstation
        {
            get
            {
                if(dormantThing == null)
                {
                    foreach(Thing t in innerContainer)
                    {
                        if(t is Building_TMGolemBase)
                        {
                            dormantThing = t as Building_TMGolemBase;
                        }
                    }
                }
                return dormantThing;
            }
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
            foreach(HediffDef hd in Golem.hediffs)
            {
                HealthUtility.AdjustSeverity(Pawn, hd, hd.initialSeverity);
            }
            if (Pawn.playerSettings != null)
            {
                Pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                if (InnerWorkstation.ThreatTarget != null && (InnerWorkstation.ThreatTarget.Position - Pawn.Position).LengthHorizontal <= threatRange)
                {
                    threatTarget = InnerWorkstation.ThreatTarget;
                }
                else
                {
                    threatTarget = null;
                }
            }
            actionTickAverage80 = 3 * Golem.processorEvaluationTicks;
            ClearHediffs();
            ApplyNeeds();
            ApplyUpgrades();
            ApplyDamages();            
            DeSpawnGolemWorkstation();             
            PawnGolem.PostGolemActivate();
        }

        protected virtual void ClearHediffs()
        {
            foreach(TM_GolemUpgrade gu in Upgrades)
            {
                if(gu.golemUpgradeDef.hediff != null)
                {
                    Hediff hd = Pawn.health.hediffSet.GetFirstHediffOfDef(gu.golemUpgradeDef.hediff);
                    if (hd != null)
                    {
                        Pawn.health.RemoveHediff(hd);
                    }
                }
            }
        }

        protected virtual void ApplyNeeds()
        {
            Need_GolemEnergy need_ge = Pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
            if (need_ge != null)
            {
                CompGolemEnergyHandler cgeh = InnerWorkstation.Energy;
                if (cgeh != null)
                {
                    need_ge.CurLevel = cgeh.StoredEnergy;
                    need_ge.maxEnergy = cgeh.StoredEnergyMax;
                    need_ge.energyEfficiency = cgeh.ConversionEfficiency;
                }
            }
        }

        protected virtual void ApplyUpgrades()
        {
            ApplyUpgradeParts();
            ApplyUpgradeHediffs();
            UpdateGolemStatus(true);
        }

        protected virtual void ApplyUpgradeParts()
        {
            List<BodyPartDef> potentialPartsToRemove = new List<BodyPartDef>();
            potentialPartsToRemove.Clear();
            List<BodyPartRecord> partsToRemove = Pawn.health.hediffSet.GetNotMissingParts().ToList();
            foreach(TM_GolemUpgrade gu in Upgrades)
            {
                if(gu.golemUpgradeDef.bodypart != null && gu.golemUpgradeDef.partRequiresUpgrade && !potentialPartsToRemove.Contains(gu.golemUpgradeDef.bodypart))
                {
                    potentialPartsToRemove.Add(gu.golemUpgradeDef.bodypart);                                       
                }
            }

            foreach (TM_GolemUpgrade gu in Upgrades)
            {
                if (gu.golemUpgradeDef.bodypart != null && gu.golemUpgradeDef.partRequiresUpgrade && potentialPartsToRemove.Contains(gu.golemUpgradeDef.bodypart) && gu.currentLevel > 0)
                {
                    potentialPartsToRemove.Remove(gu.golemUpgradeDef.bodypart);
                }
            }

            foreach (BodyPartRecord bpr in partsToRemove)
            {
                if (potentialPartsToRemove.Contains(bpr.def) && !Pawn.health.hediffSet.PartIsMissing(bpr))
                {
                    TM_Action.RemoveBodypart(Pawn, bpr);
                }
            }
            foreach (Hediff h in Pawn.health.hediffSet.hediffs)
            {
                if (h.Bleeding && h is Hediff_MissingPart)
                {
                    Traverse.Create(root: h).Field(name: "isFreshInt").SetValue(false);
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
                        if(gu.currentLevel > 0 && gu.golemUpgradeDef.bodypart == record.def && gu.golemUpgradeDef.hediff != null)
                        {
                            Hediff hd = HediffMaker.MakeHediff(gu.golemUpgradeDef.hediff, Pawn, record);
                            hd.Severity = gu.golemUpgradeDef.hediff.initialSeverity + (gu.golemUpgradeDef.hediffSeverityPerLevel * gu.currentLevel);
                            Pawn.health.AddHediff(hd, record);
                        }
                    }
                }
            }
        }

        protected virtual void UpdateGolemStatus(bool forceClear = false)
        {
            if(abilityList == null || forceClear)
            {
                abilityList = new List<TM_GolemAbility>();
                abilityList.Clear();
            }

            (Pawn as TMPawnGolem).ValidRangedVerbs(forceClear);
            
            foreach(TM_GolemUpgrade gu in Upgrades)
            {
                if(gu.currentLevel > 0)
                {
                    if (gu.golemUpgradeDef.bodypart != null && Pawn.health.hediffSet.PartIsMissing(Pawn.def.race.body.AllParts.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def == gu.golemUpgradeDef.bodypart)))
                    {
                        if (gu.golemUpgradeDef.ability != null)
                        {
                            TM_GolemAbility abilityToRemove = null;
                            foreach (TM_GolemAbility ability in abilityList)
                            {
                                if (ability.golemAbilityDef.defName == gu.golemUpgradeDef.ability.defName)
                                {
                                    abilityToRemove = ability;
                                }
                            }
                            if (abilityToRemove != null)
                            {
                                abilityList.Remove(abilityToRemove);
                            }
                        }
                        gu.currentLevel = 0;
                    } 
                    else
                    {
                        if (gu.golemUpgradeDef.ability != null)
                        {
                            TM_GolemAbility abilityToRemove = null;
                            bool abilityInList = false;
                            foreach (TM_GolemAbility ability in abilityList)
                            {
                                if (ability.golemAbilityDef.defName == gu.golemUpgradeDef.ability.defName)
                                {
                                    abilityToRemove = ability;
                                    abilityInList = true;
                                }
                            }
                            if (!abilityInList && gu.enabled)
                            {
                                abilityList.Add(new TM_GolemAbility(gu.golemUpgradeDef.ability, gu.currentLevel));
                            }
                            else if(abilityInList && !gu.enabled)
                            {
                                abilityList.Remove(abilityToRemove);
                            }
                        }
                    }
                }
            }
            if (abilityList.Count > 1)
            {
                //sort by priority, lowest priority listed first
                var tmpAbilities = abilityList.OrderBy(t => t.golemAbilityDef.priority).ToList();
                abilityList = tmpAbilities;
            }
        }

        protected virtual void ApplyDamages()
        {
            
        }

        protected virtual void DeSpawnGolemWorkstation()
        {
            if (!dormantThing.DestroyedOrNull() && dormantThing.Spawned)
            {
                if (dormantThing.ThreatTarget != null && (dormantThing.ThreatTarget.Position - Pawn.Position).LengthHorizontal <= threatRange)
                {
                    Job job = new Job(JobDefOf.AttackMelee, dormantThing.ThreatTarget);
                    Pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
                innerContainer.TryAddOrTransfer(dormantThing.SplitOff(1), false);
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

                if (this.Pawn.Spawned && Pawn.Map != null)
                {
                    if (!this.Pawn.Downed)
                    {                       
                        if(AbilityActive && this.activeAbility != null)
                        {
                            UseAbility();
                        }
                        else if (Find.TickManager.TicksGame >= nextActionTick)
                        {
                            nextActionTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(.8f, 1.2f) * actionTickAverage80 * ProcessingModifier);
                            UpdateGolemStatus();
                            TryUseAbilities();
                        }

                        if(Find.TickManager.TicksGame % 605 == 0)
                        {
                            UpdateGolemancerStatus();
                        }

                        if ((this.ShouldDespawnNow && !Pawn.IsBurning()) || Energy.CurLevel <= .1f)
                        {
                            DeSpawnGolem();
                            this.shouldDespawn = false;
                        }
                    }
                }                
            }
            age++;            
        }        

        public void UpdateGolemancerStatus()
        {
            if(!pawnMaster.DestroyedOrNull() && !pawnMaster.Dead)
            {
                CompAbilityUserMagic masterComp = pawnMaster.TryGetComp<CompAbilityUserMagic>();
                if(TM_Calc.IsMagicUser(pawnMaster) && masterComp != null && masterComp.MagicData != null && masterComp.MagicData.AllMagicPowersWithSkills.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Golemancy).learned)
                {
                    float pSev = .5f + masterComp.MagicData.GetSkill_Power(TorannMagicDefOf.TM_Golemancy).level;
                    float eSev = .5f + masterComp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Golemancy).level;
                    float vSev = .5f + masterComp.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_Golemancy).level;
                    Hediff hd = pawnMaster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_GolemancyVersatilityHD);
                    if(hd != null)
                    {
                        if (hd.Severity != vSev)
                        {
                            hd.Severity = vSev;
                        }
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(pawnMaster, TorannMagicDefOf.TM_GolemancyVersatilityHD, vSev);
                    }
                    hd = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_GolemancyPowerHD);
                    if(hd != null)
                    {
                        if (hd.Severity != pSev)
                        {
                            hd.Severity = pSev;
                        }
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_GolemancyPowerHD, pSev);
                    }
                    hd = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_GolemancyEfficiencyHD);
                    if (hd != null)
                    {
                        if (hd.Severity != eSev)
                        {
                            hd.Severity = eSev;
                        }
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_GolemancyEfficiencyHD, eSev);
                    }

                }
            }
            else
            {
                pawnMaster = null;
            }
        }

        public void UseAbility()
        {
            bool flag4 = this.effecter == null;
            if (flag4)
            {
                EffecterDef progressBar = EffecterDefOf.ProgressBar;
                this.effecter = progressBar.Spawn();
            }
            else
            {
                LocalTargetInfo localTargetInfo = Pawn;
                bool spawned2 = Pawn.Spawned;
                if (spawned2)
                {
                    this.effecter.EffectTick(Pawn, TargetInfo.Invalid);
                }
                MoteProgressBar mote = ((SubEffecter_ProgressBar)this.effecter.children[0]).mote;
                bool flag5 = mote != null;
                if (flag5)
                {
                    float value = (float)(this.abilityTick) / (float)this.abilityMaxTicks;
                    mote.progress = Mathf.Clamp01(value);
                    mote.offsetZ = -0.5f;                    
                }
            }
            if(this.activeAbility.golemAbilityDef.tickMote != null && Find.TickManager.TicksGame % this.activeAbility.golemAbilityDef.tickMoteFrequency == 0)
            {
                float angle = Rand.Range(0f, 360f);
                if(this.activeAbility.golemAbilityDef.tickMoteVelocityTowardsTarget != 0)
                {
                    angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(Pawn.DrawPos, abilityTarget.CenterVector3)).ToAngleFlat();
                }
                TM_MoteMaker.ThrowGenericMote(this.activeAbility.golemAbilityDef.tickMote, Pawn.DrawPos, Pawn.Map, this.activeAbility.golemAbilityDef.tickMoteSize,
                    this.activeAbility.golemAbilityDef.tickMote.mote.solidTime,
                    this.activeAbility.golemAbilityDef.tickMote.mote.fadeInTime,
                    this.activeAbility.golemAbilityDef.tickMote.mote.fadeOutTime,
                    Rand.Range(-50, 50),
                    this.activeAbility.golemAbilityDef.tickMoteVelocityTowardsTarget,
                    angle,
                    Rand.Range(0, 360));
            }
            if(this.abilityTick >= activeAbility.golemAbilityDef.warmupTicks)
            {
                if(abilityBurstTick <= 0)
                {
                    abilityBurstTick = Mathf.RoundToInt((float)activeAbility.golemAbilityDef.ticksBetweenBurstShots/DamageModifier);
                    if (activeAbility.golemAbilityDef.effects != null)
                    {
                        foreach (CompProperties_GolemAbilityEffect effectDef in activeAbility.golemAbilityDef.effects)
                        {
                            if (effectDef.CanApplyOn(abilityTarget, Pawn, activeAbility.golemAbilityDef))
                            {                               
                                effectDef.Apply(abilityTarget, Pawn, activeAbility.golemAbilityDef, activeAbility.currentLevel, DamageModifier);
                            }
                            else
                            {
                                this.abilityTick = this.abilityMaxTicks;
                            }                            
                        }
                    }
                }
                abilityBurstTick--;
            }
            this.abilityTick++;
            if(this.abilityTick > this.abilityMaxTicks)
            {
                EndActiveAbility();
            }
        }

        public void EndActiveAbility()
        {            
            this.abilityMaxTicks = 0;
            this.abilityTick = 0;
            this.activeAbility = null;
            this.abilityBurstTick = 0;            
            this.effecter?.Cleanup();
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
            IntVec3 despawnPos = Pawn.Position;
            if((dormantPosition - despawnPos).LengthHorizontal <= 1.4f)
            {
                despawnPos = dormantPosition;
            }
            if (InnerWorkstation != null)
            {
                GenPlace.TryPlaceThing(InnerWorkstation.SplitOff(1), despawnPos, Pawn.Map, ThingPlaceMode.Direct, null, null, dormantRotation);
                spawnedThing = InnerWorkstation;
            }
            else
            {
                
                AbilityUser.SpawnThings spawnables = new SpawnThings();
                spawnables.def = TM_GolemUtility.GetGolemDefFromThing(Pawn).golemWorkstationDef;
                spawnables.spawnCount = 1;
                bool flag = spawnables.def != null;
                if (flag)
                {
                    Faction faction = Pawn.Faction;
                    ThingDef def = spawnables.def;
                    ThingDef stuff = null;
                    bool madeFromStuff = def.MadeFromStuff;
                    if (madeFromStuff && TM_GolemUtility.GetGolemDefFromThing(Pawn).golemStuff != null)
                    {
                        stuff = TM_GolemUtility.GetGolemDefFromThing(Pawn).golemStuff;
                    }
                    spawnedThing = ThingMaker.MakeThing(def, stuff) as Building_TMGolemBase;
                    spawnedThing.SetFaction(Pawn.Faction);
                    GenSpawn.Spawn(spawnedThing, Pawn.Position, this.Pawn.Map, dormantRotation, WipeMode.Vanish, false);                    
                }
            }
            if (Find.Selector.SingleSelectedThing == this.parent)
            {
                Find.Selector.ClearSelection();
            }
            spawnedThing.Energy.SetEnergy(Energy.CurLevel);
            spawnedThing.tmpGolem = Pawn;
            spawnedThing.pauseFor = 300;
            spawnedThing.ToggleGlowing();
            this.initialized = false;
            this.parent.DeSpawn(DestroyMode.Vanish);
            PawnGolem.PostGolemDeActivate();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);
        }

        public void TryUseAbilities()
        {
            if (HasEnergyForAbilities && Pawn.CurJobDef != TorannMagicDefOf.JobDriver_GolemAbilityJob)
            {
                if (abilityList != null && abilityList.Count > 0)
                {
                    foreach (TM_GolemAbility ga in abilityList)
                    {                        
                        bool success = false;
                        if (!ga.AbilityReady)
                        {
                            continue;
                        }
                        if (Pawn.drafter != null && ga.golemAbilityDef.autocasting != null)
                        {
                            if (Pawn.Drafted && !ga.golemAbilityDef.autocasting.drafted)
                            {
                                continue;
                            }
                            if (!Pawn.Drafted && !ga.golemAbilityDef.autocasting.undrafted)
                            {
                                continue;
                            }
                        }
                        GolemAbilityWorker.ResolveAbilityUse(Pawn as TMPawnGolem, this, ga, out success);
                        if (success) break;
                    }
                }
            }
        }

        public void StartAbility(TM_GolemAbility ability, LocalTargetInfo target)
        {
            this.activeAbility = ability;
            this.abilityTarget = target;
            this.abilityMaxTicks = ability.golemAbilityDef.warmupTicks + (Mathf.RoundToInt(ability.golemAbilityDef.burstCount * DurationModifier * ability.golemAbilityDef.ticksBetweenBurstShots));
            ability.lastUsedTick = Find.TickManager.TicksGame + Mathf.RoundToInt((float)this.abilityMaxTicks * CooldownModifier);
            if(ability.golemAbilityDef.requiredNeed != null)
            {
                DecreaseNeed(ability.golemAbilityDef.requiredNeed, ability.golemAbilityDef.needCost);
            }
            if(ability.golemAbilityDef.requiredHediff != null)
            {
                DecreaseHediff(ability.golemAbilityDef.requiredHediff, ability.golemAbilityDef.hediffCost);
            }
            this.abilityTick = 0;
            this.abilityBurstTick = 0;
        }

        public void DecreaseNeed(NeedDef need, float amount)
        {
            Need_GolemEnergy n = Pawn.needs.TryGetNeed(need) as Need_GolemEnergy;
            if(n != null)
            {
                n.CurLevel -= (n.ActualNeedCost(amount) * EnergyCostModifier);
            }
        }

        public void DecreaseHediff(HediffDef hediff, float amount)
        {
            Hediff h = Pawn.health.hediffSet.GetFirstHediffOfDef(hediff, false);
            if (h != null)
            {
                h.Severity -= (amount * EnergyCostModifier);
            }
        }
    }

    public enum AbilityTargetType
    {
        None,
        Multi,
        Single,
        Cell,
        Area
    }

}