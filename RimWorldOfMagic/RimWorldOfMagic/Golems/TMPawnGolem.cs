using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class TMPawnGolem : TMPawnSummoned
    {
        public Material frameMatInt = null;
        public List<DrawMesh> drawQueue = new List<DrawMesh>();
        public List<GolemDrawClass> drawTempMesh = new List<GolemDrawClass>();

        public List<GolemVerbTracker> verbList = new List<GolemVerbTracker>();

        public bool rangedToggle = false;
        public Dictionary<Verb, Command_VerbTarget> verbCommands = null;
        public Command_VerbTarget GetCommandVerbs(Verb verb)
        {
            if (verbCommands == null)
            {
                verbCommands = new Dictionary<Verb, Command_VerbTarget>();
                verbCommands.Clear();
            }
            if (!verbCommands.ContainsKey(verb))
            {
                verbCommands.Add(verb, CreateVerbTargetCommand(this, verb));
            }
            return verbCommands[verb];
        }

        private Command_VerbTarget CreateVerbTargetCommand(Thing ownerThing, Verb verb)
        {
            Command_VerbTarget command_VerbTarget = new Command_VerbTarget();
            ThingStyleDef styleDef = ownerThing.StyleDef;
            command_VerbTarget.defaultDesc = verb.verbProps.label;
            command_VerbTarget.icon = (verb.verbProps.commandIcon != null ? ContentFinder<Texture2D>.Get(verb.verbProps.commandIcon) : ((styleDef != null && styleDef.UIIcon != null) ? styleDef.UIIcon : ownerThing.def.uiIcon));
            command_VerbTarget.iconAngle = ownerThing.def.uiIconAngle;
            command_VerbTarget.iconOffset = ownerThing.def.uiIconOffset;
            command_VerbTarget.tutorTag = "VerbTarget";
            command_VerbTarget.verb = verb;
            if (this.Faction != Faction.OfPlayer)
            {
                command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
            }
            else
            {
                if (this.WorkTagIsDisabled(WorkTags.Violent))
                {
                    command_VerbTarget.Disable("IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
                else if (!this.Drafted)
                {
                    command_VerbTarget.Disable("IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
            }
            return command_VerbTarget;
        }

        private List<Verb> validRangedVerbs = new List<Verb>();
        public List<Verb> ValidRangedVerbs(bool forceUpdate = false)
        {
            if(forceUpdate)
            {
                validRangedVerbs = new List<Verb>();
                validRangedVerbs.Clear();
                foreach(TM_GolemUpgrade gu in Golem.Upgrades)
                {
                    if(gu.golemUpgradeDef.verbProjectile != null && gu.currentLevel > 0 && gu.enabled)
                    {
                        BodyPartRecord bpr = this.RaceProps.body.AllParts.FirstOrDefault(x => x.def == gu.golemUpgradeDef.bodypart);
                        if(bpr != null && !health.hediffSet.PartIsMissing(bpr))
                        {
                            foreach(Verb v in this.VerbTracker.AllVerbs.Where(x => x.verbProps.hasStandardCommand && x.verbProps.range >= 2 && x.verbProps.defaultProjectile != null))
                            {
                                if(this.RaceProps.body.AllParts.Where(x => x.groups.Contains(v.verbProps.linkedBodyPartsGroup)).Contains(bpr) && v.verbProps.defaultProjectile == gu.golemUpgradeDef.verbProjectile && !validRangedVerbs.Contains(v))
                                {
                                    validRangedVerbs.Add(v);
                                }                                
                            }
                        }
                    }
                }
            }
            return validRangedVerbs;
        }

        public Verb GetBestVerb
        {
            get
            {
                List<Verb> validVerbs = ValidRangedVerbs().OrderBy(t => t.verbProps.commonality).ToList();
                foreach(Verb v in validVerbs)
                {
                    if((Golem.HasEnergyForAbilities || v.verbProps.consumeFuelPerShot == 0) && Golem.Energy.CurLevel > v.verbProps.consumeFuelPerShot)
                    {
                        if ((v.LastShotTick + (v.verbProps.defaultCooldownTime * 60f)) < Find.TickManager.TicksGame)
                        {
                            return v;
                        }
                    }
                }
                return null;
            }
        }

        public CompGolem Golem
        {
            get
            {
                return this.TryGetComp<CompGolem>();
            }
        }

        public virtual Vector3 EyeVector
        {
            get
            {
                Vector3 pos = this.DrawPos;
                pos.y += 1f;
                if (this.Rotation == Rot4.North)
                {
                    pos.z += .95f;
                    pos.y += -2f;
                    return pos;
                }
                else if (this.Rotation == Rot4.West)
                {
                    pos.z += .95f;
                    pos.x += -.77f;
                    return pos;
                }
                else if (this.Rotation == Rot4.East)
                {
                    pos.z += .95f;
                    pos.x += .77f;
                    return pos;
                }
                else
                {
                    pos.z += .88f;
                    return pos;
                }
            }
        }

        public bool showDormantPosition = false;
        public bool drawTickFlag = false;
        public virtual bool ShouldDrawTick => this.CurJobDef == TorannMagicDefOf.JobDriver_GolemAttackStatic && drawTickFlag;

        public Verb activeVerb;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref activeVerb, "activeVerb");
            Scribe_Values.Look(ref rangedToggle, "rangedToggle");
            Scribe_Values.Look(ref showDormantPosition, "showDormantPosition", false);
        }

        public TMPawnGolem()
        {
            
        }

        public virtual void PostGolemActivate()
        {

        }

        public virtual void PostGolemDeActivate()
        {

        }

        public override void Tick()
        {
            if (Spawned && this.drafter == null)
            {
                this.drafter = new Pawn_DraftController(this);
            }
            if (this.abilities == null)
            {
                this.abilities = new Pawn_AbilityTracker(this);
            }
            if (this.guest == null)
            {
                this.guest = new Pawn_GuestTracker(this);
            }
            if(this.story == null)
            {
                this.story = new Pawn_StoryTracker(this);
                this.story.childhood = new Backstory();
                this.story.childhood.baseDesc = "Crafted";
                this.story.adulthood = new Backstory();
                this.story.adulthood.baseDesc = "Crafted";
                this.story.title = "Golem";
            }
            if (this.workSettings == null)
            {
                this.workSettings = new Pawn_WorkSettings(this);
            }
            base.Tick();
            if(Downed && !Dead)
            {
                Kill(null, null);
            }
            else if(Find.TickManager.TicksGame % 67 == 0 && this.CurJobDef == JobDefOf.Wait_Combat && ValidRangedVerbs().Count > 0)
            {
                JobGiver_DraftedGolemRangedAttack jg = new JobGiver_DraftedGolemRangedAttack();
                Job rangedAttack = jg.TryGetJob(this);                
                if(rangedAttack != null)
                {
                    this.jobs.TryTakeOrderedJob(rangedAttack, JobTag.Misc);
                }
            }
            else if(Find.TickManager.TicksGame % 321 == 0)
            {
                TM_Action.HealAllSickness(this);
            }
            if(ShouldDrawTick)
            {
                DrawGolemTick();
            }
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            Need rage = this.needs.TryGetNeed(TorannMagicDefOf.TM_GolemRage);           
            if(rage != null)
            {
                rage.CurLevel += dinfo.Amount;
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }

        public virtual void DrawGolemTick()
        {

        }

        List<GolemDrawClass> removeGDC = new List<GolemDrawClass>();
        public override void Draw()
        {
            base.Draw();
            List<DrawMesh> tmpMesh = new List<DrawMesh>();
            foreach(DrawMesh mesh in drawQueue)
            {
                mesh.Draw();
                if(mesh.age > mesh.Duration)
                {
                    tmpMesh.Add(mesh);
                }
            }
            foreach(DrawMesh m in tmpMesh)
            {
                drawQueue.Remove(m);
            }
            foreach (TM_GolemUpgrade gu in Golem.Upgrades)
            {
                if (gu.currentLevel > 0)
                {
                    if(!(Drafted || CurJobDef == JobDefOf.AttackMelee || CurJobDef == TorannMagicDefOf.JobDriver_GolemAttackStatic) && !gu.golemUpgradeDef.drawUndrafted)
                    {
                        continue;
                    }                    
                    if(gu.golemUpgradeDef.drawOnlyWhenActive && Golem.ActiveAbility != null && gu.golemUpgradeDef.ability != Golem.ActiveAbility.golemAbilityDef)
                    {
                        continue;
                    }
                    if(gu.golemUpgradeDef.drawThing == null && gu.golemUpgradeDef.graphicsPath == null)
                    {
                        continue;
                    }
                    Material mat = null;
                    Vector3 vecOffset = new Vector3(0f, 0f, 0f);
                    string graphicLevel = "";
                    if(gu.golemUpgradeDef.levelIncrementsGraphics)
                    {
                        graphicLevel = gu.currentLevel.ToString();
                    }
                    
                    if (this.Rotation == Rot4.North)
                    {
                        if (!gu.golemUpgradeDef.drawNorth)
                        {
                            continue;
                        }
                        if (gu.golemUpgradeDef.graphicsPath != null)
                        {
                            mat = MaterialPool.MatFrom("Golems/" + gu.golemUpgradeDef.graphicsPath + "_north" + graphicLevel, ShaderDatabase.Transparent, Color.white);
                        }
                        vecOffset = gu.golemUpgradeDef.drawOffsetNorth;
                    }
                    else if (this.Rotation == Rot4.West)
                    {
                        if (!gu.golemUpgradeDef.drawWest)
                        {
                            continue;
                        }
                        if (gu.golemUpgradeDef.graphicsPath != null)
                        {
                            mat = MaterialPool.MatFrom("Golems/" + gu.golemUpgradeDef.graphicsPath + "_west" + graphicLevel, ShaderDatabase.Transparent, Color.white);
                        }
                        vecOffset = gu.golemUpgradeDef.drawOffsetWest;
                    }
                    else if (this.Rotation == Rot4.East)
                    {
                        if (!gu.golemUpgradeDef.drawEast)
                        {
                            continue;
                        }
                        if (gu.golemUpgradeDef.graphicsPath != null)
                        {
                            mat = MaterialPool.MatFrom("Golems/" + gu.golemUpgradeDef.graphicsPath + "_east" + graphicLevel, ShaderDatabase.Transparent, Color.white);
                        }
                        vecOffset = gu.golemUpgradeDef.drawOffsetEast;
                    }
                    else
                    {
                        if (!gu.golemUpgradeDef.drawSouth)
                        {
                            continue;
                        }
                        if (gu.golemUpgradeDef.graphicsPath != null)
                        {
                            mat = MaterialPool.MatFrom("Golems/" + gu.golemUpgradeDef.graphicsPath + "_south" + graphicLevel, ShaderDatabase.Transparent, Color.white);
                        }
                        vecOffset = gu.golemUpgradeDef.drawOffsetSouth;
                    }
                    float rotation = 0f;
                    if(gu.golemUpgradeDef.drawThing != null)
                    {
                        mat = gu.golemUpgradeDef.drawThing.DrawMatSingle;
                    }
                    if(gu.golemUpgradeDef.shouldRotate && Golem.AbilityTarget != null)
                    {
                        rotation = TM_Calc.GetVector(DrawPos, Golem.AbilityTarget.CenterVector3).ToAngleFlat();
                    }
                    if(mat == null)
                    {
                        continue;
                    }

                    Vector3 vector = this.DrawPos;
                    vector.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                    vector += vecOffset;

                    Vector3 s = new Vector3(gu.golemUpgradeDef.drawSize, this.DrawPos.y, gu.golemUpgradeDef.drawSize);
                    Matrix4x4 matrix = default(Matrix4x4);
                    Quaternion q = Quaternion.AngleAxis(rotation, Vector3.up);

                    matrix.SetTRS(vector, q, s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
                }
            }            
            removeGDC.Clear();
            foreach(GolemDrawClass gdc in drawTempMesh)
            {
                float fadedBrightness = GetFadedBrightness(gdc.startTick, gdc.duration);
                DrawBolt(gdc.mesh, gdc.mat, gdc.startPos, gdc.angle, fadedBrightness);
                if(Find.TickManager.TicksGame >= (gdc.startTick + gdc.duration))
                {
                    removeGDC.Add(gdc);
                }
            }
            foreach(GolemDrawClass gdc_rem in removeGDC)
            {
                drawTempMesh.Remove(gdc_rem);
            }

            if(ModOptions.Settings.Instance.showDormantFrames || showDormantPosition)
            {
                if(frameMatInt != null)
                {
                    
                    Vector3 vector = this.Golem.dormantPosition.ToVector3Shifted();
                    vector.y = Altitudes.AltitudeFor(AltitudeLayer.Blueprint);                    
                    Vector3 s = new Vector3(Golem.dormantThing.def.graphicData.drawSize.x, this.DrawPos.y, Golem.dormantThing.def.graphicData.drawSize.y);
                    Matrix4x4 matrix = default(Matrix4x4);
                    Quaternion q = Quaternion.identity;
                    matrix.SetTRS(vector, q, s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, frameMatInt, 0);  
                }
                else if(Golem.Golem.GetGolemFrameMat(this) != null)
                {
                    frameMatInt = Golem.Golem.GetGolemFrameMat(this);
                }
            }

            GolemPostDraw();
        }

        public void DrawBolt(Mesh mesh, Material mat, IntVec3 start, float angle, float fadedBrightness)
        {
            if (start != default(IntVec3))
            {
                Graphics.DrawMesh(mesh, start.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteLow), Quaternion.Euler(0f, angle, 0f), FadedMaterialPool.FadedVersionOf(mat, fadedBrightness), 0);
            }
        }

        public float GetFadedBrightness(int startTick, int endTick)
        {
            return 1f + ((float)(startTick - Find.TickManager.TicksGame) / (float)(endTick));
            
        }

        public virtual void GolemPostDraw()
        {
            if (this.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD, false))
            {
                float num = Mathf.Lerp(1.2f, 1.55f, 1f);
                Vector3 vector = this.Drawer.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(1.8f, 1f, 1.8f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                if (this.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.burningFuryMat, 0);
                }
            }
        }

        public override void DrawGUIOverlay()
        {
            Drawer.ui.DrawPawnGUIOverlay();
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (pather.curPath != null)
            {
                pather.curPath.DrawPath(this);
            }
            jobs.DrawLinesBetweenTargets();            
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var gizmoList = base.GetGizmos().ToList();

            if (drafter != null)
            {
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.hotKey = KeyBindingDefOf.Command_ColonistDraft;
                command_Toggle.isActive = (() => Drafted);
                command_Toggle.toggleAction = delegate
                {
                    this.drafter.Drafted = !Drafted;
                    if (verbCommands != null)
                    {
                        verbCommands.Clear();
                    }
                };
                command_Toggle.defaultDesc = "CommandToggleDraftDesc".Translate();
                command_Toggle.icon = TexCommand.Draft;
                command_Toggle.turnOnSound = SoundDefOf.DraftOn;
                command_Toggle.turnOffSound = SoundDefOf.DraftOff;
                command_Toggle.defaultLabel = (Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate();
                if (this.Downed)
                {
                    command_Toggle.Disable("IsIncapped".Translate(this.LabelShort, this));
                }
                if (!Drafted)
                {
                    command_Toggle.tutorTag = "Draft";
                }
                else
                {
                    command_Toggle.tutorTag = "Undraft";
                }
                gizmoList.Add(command_Toggle);
            }

            Command_Action command_Despawn = new Command_Action();
            command_Despawn.defaultLabel = "TM_DeActivateGolem".Translate();
            command_Despawn.defaultDesc = "TM_DeActivateGolemDesc".Translate();
            command_Despawn.icon = ContentFinder<Texture2D>.Get("UI/golem_icon", true);
            command_Despawn.action = delegate
            {
                this.drafter.Drafted = false;
                if (Golem.shouldDespawn)
                {
                    Golem.despawnNow = true;
                }
                else
                {
                    if (Golem.dormantPosition.Walkable(this.Map) && Golem.dormantPosition.Standable(this.Map))
                    {
                        this.jobs.ClearQueuedJobs(true);
                        this.jobs.StopAll();                        
                        Golem.shouldDespawn = true;
                    }
                    else
                    {
                        Messages.Message("TM_GolemCannotReturn".Translate(
                        ), MessageTypeDefOf.RejectInput, false);
                        Golem.despawnNow = true;
                    }
                }
            };
            gizmoList.Add(command_Despawn);

            TargetingParameters newParameters = new TargetingParameters();
            newParameters.canTargetLocations = true;

            Command_LocalTargetInfo command_DormantPos = new Command_LocalTargetInfo();
            command_DormantPos.defaultLabel = "TM_AssignGolemRestPosition".Translate();
            command_DormantPos.defaultDesc = "TM_AssignGolemRestPositionDesc".Translate();
            command_DormantPos.icon = ContentFinder<Texture2D>.Get("UI/golem_icon_new", true);
            command_DormantPos.targetingParams = newParameters;
            command_DormantPos.action = delegate (LocalTargetInfo infoTarget)
            {
                IntVec3 cell = infoTarget.Cell;
                FleckMaker.ThrowAirPuffUp(infoTarget.CenterVector3, this.Map);
                FleckMaker.ThrowHeatGlow(infoTarget.Cell, this.Map, 1f);
                if (cell.IsValid && cell.InBoundsWithNullCheck(this.Map) && !cell.Fogged(this.Map) && cell.Standable(this.Map) && ReachabilityUtility.CanReach(this, infoTarget, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
                {
                    Golem.dormantPosition = cell;
                    Golem.dormantMap = this.Map;
                }
                else
                {
                    Messages.Message("TM_DormantPositionUnreachable".Translate(), MessageTypeDefOf.RejectInput);
                }
            };
            gizmoList.Add(command_DormantPos);

            if (this.ValidRangedVerbs() != null && this.ValidRangedVerbs().Count > 0)
            {
                foreach (Verb v in this.ValidRangedVerbs())
                {
                    gizmoList.Add(GetCommandVerbs(v));
                    //gizmoList.Add((Gizmo)CreateVerbTargetCommand(this, v));
                }
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.defaultLabel = "CommandHoldFire".Translate();
                command_Toggle.defaultDesc = "CommandHoldFireDesc".Translate();
                command_Toggle.icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire");
                command_Toggle.hotKey = KeyBindingDefOf.Misc6;
                command_Toggle.toggleAction = delegate
                {
                    rangedToggle = !rangedToggle;
                    if (rangedToggle)
                    {
                        this.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                };
                command_Toggle.isActive = (() => rangedToggle);
                gizmoList.Add((Gizmo)command_Toggle);
            }

            return gizmoList;
        }
    }
}
