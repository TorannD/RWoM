using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using TorannMagic.TMDefs;
using AbilityUser;

namespace TorannMagic.Golems
{
    public class Building_TMGolemBase : Building_WorkTable, IThingHolder, IAttackTarget, IAttackTargetSearcher
    {
        int activationAge = 0;
		private bool activating = false;
        private bool initialized = false;
        private int nextEvaluationTick = 0;
        public float lastDrawRotation = 0f;
        public bool holdFire = false;

        private LocalTargetInfo threatTarget = null;
        private TargetingParameters targetingParameters = new TargetingParameters();

        public Pawn tmpGolem = null;

        ThingOwner innerContainer = null;

        private List<GolemWorkstationEffect> activeEffects = new List<GolemWorkstationEffect>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false);
            Scribe_Values.Look<bool>(ref this.holdFire, "holdFire", false);
            Scribe_Values.Look<bool>(ref this.activating, "activating");
            Scribe_Values.Look<int>(ref this.activationAge, "activationAge", 0);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        Thing IAttackTarget.Thing
        {
            get
            {
                return this;
            }
        }

        Thing IAttackTargetSearcher.Thing
        {
            get
            {
                return this;
            }
        }

        public float TargetPriorityFactor => 1f;
        public LocalTargetInfo TargetCurrentlyAimingAt => ThreatTarget;
        public LocalTargetInfo LastAttackedTarget => ThreatTarget;
        private int lastAttackTargetTick;
        public int LastAttackTargetTick => lastAttackTargetTick;
        public Verb CurrentEffectiveVerb => null;

        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            CompFlickable comp = GetComp<CompFlickable>();
            if (comp != null && !comp.SwitchIsOn)
            {
                return true;
            }
            if(!CanActivate())
            {
                return true;
            }
            if(!GolemComp.useAbilitiesWhenDormant && GolemComp.threatRange <= 0)
            {
                return true;
            }
            return false;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public TMPawnGolem GolemPawn
        {
            get
            {
                if(innerContainer != null)
                {
                    return GetDirectlyHeldThings().FirstOrDefault() as TMPawnGolem;
                }
                return null;
            }
        }

        public CompGolem GolemComp
        {
            get
            {
                if(GolemPawn != null)
                {
                    return GolemPawn.TryGetComp<CompGolem>();
                }
                return null;
            }
        }

        public TM_Golem GolemDef
        {
            get
            {
                if (GolemComp != null)
                {
                    return GolemComp.Golem;
                }
                return null;
            }
        }

        public List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                if (GolemComp != null)
                {
                    return GolemComp.Upgrades;
                }
                return null;
            }
        }

        public CompGolemEnergyHandler Energy => this.TryGetComp<CompGolemEnergyHandler>();
        public Thing ThreatTarget => threatTarget.HasThing ? threatTarget.Thing : null;

        public float ActualEnergyCost(float cost)
        {
            return (cost * GolemComp.EnergyCostModifier) / Energy.ConversionEfficiency;
        }

        public bool IsUpgrade(RecipeDef rec)
        {
            foreach (TM_GolemUpgrade gu in Upgrades)
            {
                if (gu.golemUpgradeDef.recipe == rec)
                {
                    return gu.golemUpgradeDef.maxLevel != 0;
                }
            }
            return true;
        }

        public bool CanUpgrade(RecipeDef rec)
        {
            foreach (TM_GolemUpgrade gu in Upgrades)
            {
                if (gu.golemUpgradeDef.recipe == rec)
                {
                    return gu.currentLevel < gu.golemUpgradeDef.maxLevel;
                }
            }
            return true;
        }

        public bool CanActivate()
        {
            List<Thing> tmpList = this.Position.GetThingList(this.Map);
            if(tmpList != null && tmpList.Count > 0)
            {
                int num = tmpList.Count;
                for(int i =0; i < num; i++)
                {
                    if(tmpList[i] is TorannMagic.Golems.UnfinishedNoProductThing)
                    {
                        return false;
                    }
                }
            }
            if(GolemComp.remainDormantWhenUpgrading && this.BillStack != null && this.BillStack.Bills != null && this.BillStack.Bills.Count > 0)
            {
                foreach(Bill b in this.BillStack)
                {
                    if (b.ShouldDoNow())
                    {
                        return false;
                    }
                    
                }
            }
            return true;
        }

        public void IncreaseUpgrade_Recipe(RecipeDef rec)
        {
            foreach(TM_GolemUpgrade gu in Upgrades)
            {
                if(gu.golemUpgradeDef.recipe == rec)
                {
                    if (gu.golemUpgradeDef.bodypart != null)
                    {
                        if (gu.golemUpgradeDef.partRequiresUpgrade)
                        {
                            foreach (TM_GolemUpgrade guz in Upgrades)
                            {
                                if (guz.golemUpgradeDef.bodypart != null)
                                {
                                    if (guz.golemUpgradeDef.defName != gu.golemUpgradeDef.defName && GolemPawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord x) => x.def == guz.golemUpgradeDef.OccupiedPart) == GolemPawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord x) => x.def == gu.golemUpgradeDef.OccupiedPart))
                                    {
                                        if (guz.currentLevel > 0 && !GolemPawn.health.hediffSet.PartIsMissing(GolemPawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord bpr) => bpr.def == guz.golemUpgradeDef.bodypart)))
                                        {
                                            guz.currentLevel = 0;
                                            TM_Action.RemoveBodypart(GolemPawn, GolemPawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord x) => x.def == guz.golemUpgradeDef.bodypart));
                                            Messages.Message("TM_GolemPartReplaced".Translate(gu.golemUpgradeDef.label, guz.golemUpgradeDef.label, gu.golemUpgradeDef.OccupiedPart.label), MessageTypeDefOf.NeutralEvent);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if(gu.golemUpgradeDef.OccupiedPart != null)
                        {
                            foreach(TM_GolemUpgrade guz in Upgrades)
                            {
                                if(guz.currentLevel > 0 && guz.golemUpgradeDef.OccupiedPart == gu.golemUpgradeDef.OccupiedPart && guz.golemUpgradeDef != gu.golemUpgradeDef)
                                {
                                    Messages.Message("TM_GolemPartReplaced".Translate(gu.golemUpgradeDef.label, guz.golemUpgradeDef.label, gu.golemUpgradeDef.OccupiedPart.label), MessageTypeDefOf.NeutralEvent);
                                    guz.currentLevel = 0;
                                }
                            }
                        }
                    }
                    
                    UpgradePart(gu);
                }
            }
        }

        public void UpgradePart(TM_GolemUpgrade gu)
        {
            gu.currentLevel++;
            if (gu.currentLevel == 1 && gu.golemUpgradeDef.partRequiresUpgrade)
            {
                foreach (Hediff hd in GolemPawn.health.hediffSet.hediffs)
                {
                    if (hd.Part != null && hd.Part.def == gu.golemUpgradeDef.bodypart)
                    {
                        GolemPawn.health.RemoveHediff(hd);
                        break;
                    }
                }
            }
        }

        public void ApplyProductEffects(RecipeDef rec, List<Thing> ingredients)
        {
            foreach (TM_GolemUpgrade gu in Upgrades)
            {
                if (gu.golemUpgradeDef.recipe == rec)
                {
                    if(gu.golemUpgradeDef.workstationEffects != null && gu.golemUpgradeDef.workstationEffects.Count > 0)
                    {
                        foreach(GolemWorkstationEffect effect in gu.golemUpgradeDef.workstationEffects)
                        {
                            if(effect.CanDoEffect(this))
                            {
                                effect.StartEffect(this, gu);
                            }
                            else
                            {
                                Messages.Message("TM_NoGolemPartToRestore".Translate(rec.label), MessageTypeDefOf.RejectInput);
                                foreach (Thing ing in ingredients)
                                {
                                    Thing t = ThingMaker.MakeThing(ing.def, null);
                                    t.stackCount = ing.stackCount;
                                    GenPlace.TryPlaceThing(t, this.InteractionCell, this.Map, ThingPlaceMode.Near);
                                }
                            }
                        }
                    }
                }
            }
        }

        public Building_TMGolemBase()
        {
            innerContainer = new ThingOwner<Thing>(this);            
        }

        public virtual void Initialize()
        {
            if(GolemPawn == null)
            {
                innerContainer.Clear();
                TMPawnSummoned initGolem = SpawnGolem();               
                innerContainer.TryAddOrTransfer(initGolem.SplitOff(1), false);             
            }
        }

        public int pauseFor = 0;
		public override void Tick()
		{
			base.Tick();
            if(!initialized)
            {
                Initialize();
                initialized = true;
            }
            if(tmpGolem != null)
            {
                innerContainer.TryAddOrTransfer(tmpGolem.SplitOff(1), false);
                tmpGolem = null;
            }
            if (innerContainer.Any)
            {
                if (Find.TickManager.TicksGame % 305 == 0)
                {
                    foreach (TM_GolemUpgrade gu in Upgrades)
                    {
                        if (gu.currentLevel > 0)
                        {
                            if (gu.golemUpgradeDef.workstationCapacity == WorkstationCapacity.EnergyMax)
                            {
                                Energy.Upgrade_StoredEnergy(gu.currentLevel);
                            }
                            if (gu.golemUpgradeDef.workstationCapacity == WorkstationCapacity.Efficiency)
                            {
                                Energy.Upgrade_ConversionEfficiency(gu.currentLevel);
                            }
                            if (gu.golemUpgradeDef.workstationCapacity == WorkstationCapacity.EnergyRegeneration)
                            {
                                Energy.Upgrade_RegenerationFactor(gu.currentLevel);
                            }
                        }
                    }
                }
                pauseFor--;
                if (this.GetComp<CompFlickable>().SwitchIsOn && pauseFor <=0)
                {                    
                    if (activating)
                    {
                        if (Find.TickManager.TicksGame % Mathf.RoundToInt(GolemDef.activationTicks * .05f) == 0)
                        {
                            Vector3 rndPos = this.DrawPos;
                            rndPos.x += Rand.Range(-.6f, .6f);
                            rndPos.z += Rand.Range(-.6f, .6f);
                            FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.6f, 1.1f));
                            if (!CanActivate())
                            {
                                activationAge = 0;
                                activating = false;
                            }
                        }
                        if (activationAge >= (.9f * GolemDef.activationTicks) && Find.TickManager.TicksGame % 6 == 0)
                        {
                            Find.CameraDriver.shaker.DoShake(.05f);
                        }
                        activationAge++;
                        if (Rand.Chance(GolemComp.ProcessingModifier))
                        {
                            activationAge++;
                        }
                        if (activationAge >= GolemDef.activationTicks)
                        {
                            activationAge = 0;
                            activating = false;
                            SpawnGolem();
                        }                        
                    }
                    else
                    {
                        if (Find.TickManager.TicksGame > this.nextEvaluationTick)
                        {
                            nextEvaluationTick = Mathf.RoundToInt(Find.TickManager.TicksGame + (Rand.Range(.8f, 1.2f) * GolemDef.processorEvaluationTicks * GolemComp.ProcessingModifier));
                            if (!TargetIsValid(this, threatTarget))
                            {
                                threatTarget = null;
                            }
                            if (threatTarget == null)
                            {
                                DetermineThreats();
                            }
                            TMPawnGolem p = GetDirectlyHeldThings().FirstOrDefault() as TMPawnGolem;
                            if (p != null)
                            {
                                TM_Action.DoAction_HealPawn(null, p, 1, 2 * GolemComp.HealingModifier);
                            }
                            if (GolemComp.useAbilitiesWhenDormant && CanActivate())
                            {
                                foreach (TM_GolemUpgrade gu in Upgrades)
                                {
                                    if (gu.currentLevel > 0 && gu.golemUpgradeDef.workstationEffects != null && gu.golemUpgradeDef.workstationEffects.Count > 0)
                                    {
                                        foreach (GolemWorkstationEffect gwe in gu.golemUpgradeDef.workstationEffects)
                                        {
                                            gwe.target = threatTarget;
                                            gwe.parent = this;
                                            gwe.parentUpgrade = gu;
                                            lastAttackTargetTick = Find.TickManager.TicksGame;
                                            if (gwe.CanDoEffect(this))
                                            {
                                                gwe.StartEffect(this, gu, gu.currentLevel);
                                                activeEffects.Add(gwe);
                                            }
                                        }
                                    }
                                }
                            }
                            if (this.Energy.StoredEnergyPct >= GolemComp.energyPctShouldAwaken && GolemComp.energyPctShouldAwaken != 0f && CanActivate())
                            {
                                this.activating = true;
                            }
                        }
                        if (ThreatTarget != null && CanActivate() && GolemComp.threatRange > 0 && (ThreatTarget.Position - this.Position).LengthHorizontal <= GolemComp.threatRange)
                        {
                            activating = true;
                        }
                        List<GolemWorkstationEffect> tmpList = new List<GolemWorkstationEffect>();
                        tmpList.Clear();
                        foreach (GolemWorkstationEffect effect in activeEffects)
                        {
                            if (effect.EffectActive)
                            {
                                if (effect.requiresTarget && !TargetIsValid(this, threatTarget))
                                {
                                    tmpList.Add(effect);
                                    continue;
                                }
                                effect.ContinueEffect(this);
                            }
                            else
                            {
                                tmpList.Add(effect);
                            }
                        }
                        if (tmpList.Count > 0)
                        {
                            foreach (GolemWorkstationEffect gwe in tmpList)
                            {
                                activeEffects.Remove(gwe);
                            }
                        }
                    }
                }
            }
		}

        public virtual TMPawnSummoned SpawnGolem()
        {
            TMPawnSummoned spawnedThing = null;
            LifeStageDef lsDef = null;
            TM_Golem tmpGolem = null;
            if (Upgrades != null)
            {
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.currentLevel > 0 && gu.golemUpgradeDef.lifeStages != null && gu.golemUpgradeDef.lifeStages.Count == (gu.golemUpgradeDef.maxLevel + 1))
                    {
                        if (GolemPawn.RaceProps.lifeStageAges.FirstOrDefault().def != gu.golemUpgradeDef.lifeStages[gu.currentLevel])
                        {
                            tmpGolem = new TM_Golem(GolemDef, this);
                            lsDef = gu.golemUpgradeDef.lifeStages[gu.currentLevel];
                        }
                    }
                }
            }
            if (innerContainer != null && innerContainer.Any && lsDef == null)
            {
                Pawn p = innerContainer.FirstOrDefault() as Pawn;
                GenPlace.TryPlaceThing(p.SplitOff(1), this.Position, this.Map, ThingPlaceMode.Near, null, null, this.Rotation);
                spawnedThing = p as TMPawnSummoned;
            }
            else
            {
                AbilityUser.SpawnThings spawnables = new SpawnThings();
                spawnables.def = TM_GolemUtility.GetGolemDefFromThing(this).golemDef;
                spawnables.kindDef = TM_GolemUtility.GetGolemDefFromThing(this).golemKindDef;
                if (lsDef != null)
                {
                    spawnables.def.race.lifeStageAges.FirstOrDefault().def = lsDef;
                }
                spawnables.spawnCount = 1;
                
                bool flag = spawnables.def != null;
                if (flag)
                {
                    spawnedThing = TM_Action.SingleSpawnLoop(null, spawnables, this.Position, this.Map, 0, false, false, this.Faction) as TMPawnSummoned;
                    spawnedThing.validSummoning = true;
                    spawnedThing.ageTracker.AgeBiologicalTicks = 0;
                    Projectile_RaiseUndead.RemoveHediffsAddictionsAndPermanentInjuries(spawnedThing);
                }
            }
            if(spawnedThing != null)
            {
                CompGolem cg = spawnedThing.TryGetComp<CompGolem>();
                if(cg != null)
                {
                    cg.dormantThing = this;
                    cg.dormantPosition = this.Position;
                    cg.dormantRotation = this.Rotation;
                    cg.dormantMap = this.Map;
                    if (tmpGolem != null)
                    {
                        cg.Golem = tmpGolem;
                    }
                    cg.age = 0;
                    innerContainer.ClearAndDestroyContents();
                }
            }
            if(Find.Selector.SingleSelectedThing == this)
            {
                Find.Selector.ClearSelection();
            }
            return spawnedThing;
        }

		public override void Draw()
		{
			base.Draw();
            if (Upgrades != null)
            {
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.currentLevel > 0)
                    {
                        foreach (GolemWorkstationEffect gwe in gu.golemUpgradeDef.workstationEffects)
                        {
                            if (gwe.alwaysDraw)
                            {
                                Material mat = gu.golemUpgradeDef.drawThing.DrawMatSingle;
                                float rotation = lastDrawRotation;
                                if (this.threatTarget != null && this.threatTarget.Thing != null)
                                {
                                    rotation = TM_Calc.GetVector(this.DrawPos, this.threatTarget.Thing.DrawPos).ToAngleFlat();
                                }
                                Vector3 vector = this.DrawPos;
                                vector.y = Altitudes.AltitudeFor(AltitudeLayer.BuildingOnTop);
                                vector += gwe.drawOffset;

                                Vector3 s = new Vector3(gu.golemUpgradeDef.drawSize, this.DrawPos.y, gu.golemUpgradeDef.drawSize);
                                Matrix4x4 matrix = default(Matrix4x4);
                                Quaternion q = Quaternion.AngleAxis(rotation, Vector3.up);
                                matrix.SetTRS(vector, q, s);
                                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
                            }
                        }
                    }
                }
            }
        }

		public override IEnumerable<Gizmo> GetGizmos()
		{
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (this.GetComp<CompFlickable>().SwitchIsOn)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "TM_ActivateGolem".Translate();
                command_Action.defaultDesc = "TM_ActivateGolemDesc".Translate();
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/MoveOut", true);
                command_Action.action = delegate
                {
                    activating = !activating;
                };
                yield return command_Action;

                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.defaultLabel = "CommandHoldFire".Translate();
                command_Toggle.defaultDesc = "CommandHoldFireDesc".Translate();
                command_Toggle.icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire");
                command_Toggle.hotKey = KeyBindingDefOf.Misc6;
                command_Toggle.toggleAction = delegate
                {
                    holdFire = !holdFire;
                };
                command_Toggle.isActive = (() => holdFire);
                yield return (Gizmo)command_Toggle;

                TM_Command_Target command_Target = new TM_Command_Target();
                command_Target.defaultLabel = "CommandSetForceAttackTarget".Translate();
                command_Target.defaultDesc = "CommandSetForceAttackTargetDesc".Translate();
                command_Target.targetingParams = this.targetingParameters;
                command_Target.hotKey = KeyBindingDefOf.Misc4;
                command_Target.icon = TexCommand.Attack;
                command_Target.action = delegate (LocalTargetInfo target)
                {
                    if (TargetIsValid(this, target))
                    {
                        threatTarget = target;
                    }
                };
                yield return command_Target;
            }            
        }

        private void DetermineThreats()
        {
            this.threatTarget = null;
            try
            {
                List<Pawn> allPawns = this.Map.mapPawns.AllPawnsSpawned.InRandomOrder().ToList();
                for (int i = 0; i < allPawns.Count(); i++)
                {
                    if (TargetIsValid(this, allPawns[i]))
                    {
                        this.threatTarget = allPawns[i];
                        break;                                              
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                //Log.Message("Error processing threats" + ex);
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
                    if ((targetThing as Pawn).Dead || (targetThing as Pawn).Downed)
                    {
                        return false;
                    }
                }
                if (!GenHostility.HostileTo(source, targetThing))
                {
                    return false;
                }
            }
            if (target.Cell.DistanceToEdge(this.Map) < 8)
            {
                return false;
            }
            
            return true;
        }
    }
}
