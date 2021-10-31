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
        public List<DrawMesh> drawQueue = new List<DrawMesh>();

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

        

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public TMPawnGolem()
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
            base.Tick();
            if(Downed && !Dead)
            {
                Kill(null, null);
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
                    if(!(Drafted || CurJobDef == JobDefOf.AttackMelee) && !gu.golemUpgradeDef.drawUndrafted)
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
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var gizmoList = base.GetGizmos().ToList();

            Command_Action command_Despawn = new Command_Action();
            command_Despawn.defaultLabel = "TM_DeActivateGolem".Translate();
            command_Despawn.defaultDesc = "TM_DeActivateGolemDesc".Translate();
            command_Despawn.icon = ContentFinder<Texture2D>.Get("UI/golem_icon", true);
            command_Despawn.action = delegate
            {
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
                if(cell.IsValid && cell.InBounds(this.Map) && !cell.Fogged(this.Map) && cell.Standable(this.Map) && ReachabilityUtility.CanReach(this, infoTarget, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
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

            return gizmoList;
        }
    }
}
