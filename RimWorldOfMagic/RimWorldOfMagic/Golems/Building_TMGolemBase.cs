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
using Verse.Sound;

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
        public float tempGoal = 21f;
        public bool canRegulateTemp = false;

        private LocalTargetInfo threatTarget = null;
        private TargetingParameters targetingParameters = new TargetingParameters();

        public Pawn tmpGolem = null;

        ThingOwner innerContainer = null;

        private List<GolemWorkstationEffect> activeEffects = new List<GolemWorkstationEffect>();

        CompProperties_Glower glowerProps = new CompProperties_Glower();
        public CompGlower glower = null;
        bool glowingInt = false;

        public void InitializeGlower(ColorInt glowColor, float glowRadius)
        {
            this.glower = new CompGlower();          
            glowerProps.glowColor = glowColor;
            glowerProps.glowRadius = glowRadius;
            glower.parent = this;
            glower.Initialize(glowerProps);
        }

        public void ToggleGlowing()
        {
            if (this.Map != null && glower != null)
            {
                if (!glowingInt)
                {
                    GlowOff();
                }
                else
                {
                    GlowOn();               
                }
            }
        }

        public void GlowOff()
        {
            this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things);
            this.Map.glowGrid.DeRegisterGlower(glower);
        }

        public void GlowOn()
        {
            this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things);
            this.Map.glowGrid.RegisterGlower(glower);
        }
        

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false);
            Scribe_Values.Look<bool>(ref this.holdFire, "holdFire", false);
            Scribe_Values.Look<bool>(ref this.activating, "activating");
            Scribe_Values.Look<int>(ref this.activationAge, "activationAge", 0);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look<bool>(ref this.glowingInt, "glowingInt", false);
            Scribe_Values.Look<float>(ref this.tempGoal, "tempGoal", 21f);
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
                                    if (gu.currentLevel > 0 && gu.enabled && gu.golemUpgradeDef.workstationEffects != null && gu.golemUpgradeDef.workstationEffects.Count > 0)
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
            if (this.glower != null)
            {
                GlowOff();
            }
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

        private int drawIteration = 0;
		public override void Draw()
		{
			base.Draw();
            if (Upgrades != null)
            {
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.currentLevel > 0)
                    {
                        if (gu.golemUpgradeDef.animationPath != null && gu.golemUpgradeDef.animationPath.Count > 0)
                        {
                            if (gu.animationMats == null)
                            {
                                gu.PopulateAnimationMaterial();
                            }
                            Vector3 vector = this.DrawPos;
                            vector.y = this.DrawPos.y;
                            if (this.Rotation == Rot4.North)
                            {
                                vector.y += gu.golemUpgradeDef.drawOffsetNorth.y;
                                vector.x += gu.golemUpgradeDef.drawOffsetNorth.x;
                                vector.z += gu.golemUpgradeDef.drawOffsetNorth.z;
                            }
                            else if (this.Rotation == Rot4.East)
                            {
                                vector.y += gu.golemUpgradeDef.drawOffsetEast.y;
                                vector.x += gu.golemUpgradeDef.drawOffsetEast.x;
                                vector.z += gu.golemUpgradeDef.drawOffsetEast.z;
                            }
                            else if (this.Rotation == Rot4.West)
                            {
                                vector.y += gu.golemUpgradeDef.drawOffsetWest.y;
                                vector.x += gu.golemUpgradeDef.drawOffsetWest.x;
                                vector.z += gu.golemUpgradeDef.drawOffsetWest.z;
                            }
                            else
                            {
                                vector.y += gu.golemUpgradeDef.drawOffsetSouth.y;
                                vector.x += gu.golemUpgradeDef.drawOffsetSouth.x;
                                vector.z += gu.golemUpgradeDef.drawOffsetSouth.z;
                            }

                            if (Find.TickManager.TicksGame % gu.golemUpgradeDef.changeAnimationTicks == 0)
                            {
                                if (gu.golemUpgradeDef.randomAnimation)
                                {
                                    drawIteration = Rand.RangeInclusive(0, gu.animationMats.Count - 1);
                                }
                                else
                                {
                                    drawIteration++;
                                    if (drawIteration >= gu.animationMats.Count)
                                    {
                                        drawIteration = 0;
                                    }
                                }
                            }

                            Material mat = gu.animationMats[drawIteration];

                            Vector3 s = new Vector3(gu.golemUpgradeDef.drawSize, this.DrawPos.y, gu.golemUpgradeDef.drawSize);
                            Matrix4x4 matrix = default(Matrix4x4);
                            matrix.SetTRS(vector, Quaternion.identity, s);
                            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
                        }
                        else
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
                command_Action.defaultDesc = "TM_ActivateGolemDesc".Translate(GolemDef.minimumEnergyPctToActivate.ToString("P1"));
                if(Energy.StoredEnergyPct < GolemDef.minimumEnergyPctToActivate)
                {
                    command_Action.defaultDescPostfix = "\n"+"TM_ActivateGolemDisabled".Translate();
                }                
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/MoveOut", true);
                command_Action.action = delegate
                {
                    if(Energy.StoredEnergyPct >= GolemDef.minimumEnergyPctToActivate)
                    {
                        activating = !activating;
                    }
                    else
                    {
                        Vector3 pos = this.DrawPos;
                        pos.z += .2f;
                        MoteMaker.ThrowText(pos, this.Map, "TM_GolemMinimumToActivate".Translate(Energy.StoredEnergyPct.ToString("P"), GolemDef.minimumEnergyPctToActivate.ToString("P1")), -1);                        
                    }
                };
                command_Action.disabled = (Energy.StoredEnergyPct < GolemDef.minimumEnergyPctToActivate);
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

                if (glower != null)
                {
                    Command_Toggle command_Glow = new Command_Toggle();
                    command_Glow.defaultLabel = "TM_GolemLight".Translate();
                    command_Glow.defaultDesc = "TM_GolemLightDesc".Translate();
                    command_Glow.icon = ContentFinder<Texture2D>.Get("UI/lightbulb");
                    command_Glow.toggleAction = delegate
                    {
                        glowingInt = !glowingInt;
                        ToggleGlowing();
                    };
                    command_Glow.isActive = (() => glowingInt);
                    yield return (Gizmo)command_Glow;
                }
                if(canRegulateTemp)
                {
                    float offset = RoundedToCurrentTempModeOffset(-10f);
                    Command_Action command_Temperature = new Command_Action();
                    command_Temperature.action = delegate
                    {
                        InterfaceChangeTargetTemperature(offset);
                    };
                    command_Temperature.defaultLabel = offset.ToStringTemperatureOffset("F0");
                    command_Temperature.defaultDesc = "CommandLowerTempDesc".Translate();
                    command_Temperature.hotKey = KeyBindingDefOf.Misc5;
                    command_Temperature.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
                    yield return (Gizmo)command_Temperature;
                    float offset2 = RoundedToCurrentTempModeOffset(-1f);
                    Command_Action command_Temperature2 = new Command_Action();
                    command_Temperature2.action = delegate
                    {
                        InterfaceChangeTargetTemperature(offset2);
                    };
                    command_Temperature2.defaultLabel = offset2.ToStringTemperatureOffset("F0");
                    command_Temperature2.defaultDesc = "CommandLowerTempDesc".Translate();
                    command_Temperature2.hotKey = KeyBindingDefOf.Misc4;
                    command_Temperature2.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
                    yield return (Gizmo)command_Temperature2;
                    Command_Action command_Temperature3 = new Command_Action();
                    command_Temperature3.action = delegate
                    {
                        tempGoal = 21f;
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        ThrowCurrentTemperatureText();
                    };
                    command_Temperature3.defaultLabel = "CommandResetTemp".Translate();
                    command_Temperature3.defaultDesc = "CommandResetTempDesc".Translate();
                    command_Temperature3.hotKey = KeyBindingDefOf.Misc1;
                    command_Temperature3.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset");
                    yield return (Gizmo)command_Temperature3;
                    float offset3 = RoundedToCurrentTempModeOffset(1f);
                    Command_Action command_Temperature4 = new Command_Action();
                    command_Temperature4.action = delegate
                    {
                        InterfaceChangeTargetTemperature(offset3);
                    };
                    command_Temperature4.defaultLabel = "+" + offset3.ToStringTemperatureOffset("F0");
                    command_Temperature4.defaultDesc = "CommandRaiseTempDesc".Translate();
                    command_Temperature4.hotKey = KeyBindingDefOf.Misc2;
                    command_Temperature4.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
                    yield return (Gizmo)command_Temperature4;
                    float offset4 = RoundedToCurrentTempModeOffset(10f);
                    Command_Action command_Temperature5 = new Command_Action();
                    command_Temperature5.action = delegate
                    {
                        InterfaceChangeTargetTemperature(offset4);
                    };
                    command_Temperature5.defaultLabel = "+" + offset4.ToStringTemperatureOffset("F0");
                    command_Temperature5.defaultDesc = "CommandRaiseTempDesc".Translate();
                    command_Temperature5.hotKey = KeyBindingDefOf.Misc3;
                    command_Temperature5.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
                    yield return (Gizmo)command_Temperature5;
                }
            }            
        }

        private float RoundedToCurrentTempModeOffset(float celsiusTemp)
        {
            return GenTemperature.ConvertTemperatureOffset((float)Mathf.RoundToInt(GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode)), Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
        }

        private void InterfaceChangeTargetTemperature(float offset)
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            tempGoal += offset;
            tempGoal = Mathf.Clamp(tempGoal, -273.15f, 1000f);
            ThrowCurrentTemperatureText();
        }

        private void ThrowCurrentTemperatureText()
        {
            MoteMaker.ThrowText(this.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), this.Map, tempGoal.ToStringTemperature("F0"), Color.white);
        }

        public override string GetInspectString()
        {
            string baseStr = base.GetInspectString();
            if (canRegulateTemp)
            {
                string tempString = "\n" + "TargetTemperature".Translate() + ": " + "\n" + tempGoal.ToStringTemperature("F0");
                baseStr += tempString;
            }
            return baseStr;
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
