using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_Refraction : ClassProgression.Projectile_SpiritBolt
    {
        private bool initialized = false;
        private bool wallActive = false;
        private float wallLength = 0;
        private int age = -1;
        private int duration = 300;
        Vector3 wallStart = default(Vector3);
        Vector3 wallDir = default(Vector3);
        Vector3 wallEnd = default(Vector3);
        List<IntVec3> wallPositions = new List<IntVec3>();
        LocalTargetInfo secondTarget = null;
        Pawn caster;
        private bool promptOpened;
        private bool promptCancelled;
        private HashSet<int> mirrored = new HashSet<int>();
        private HashSet<int> crossing = new HashSet<int>();
        public static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Refraction;
        protected override string SkillStem => "TM_Refraction";
        private float wallEnergy = 1500f;

        //unsaved variables
        private int wallLengthMax = 20;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.wallActive, "wallActive", false, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 300, false);
            Scribe_Values.Look<float>(ref this.wallLength, "wallLength", 0, false);
            Scribe_Values.Look<float>(ref this.wallEnergy, "wallEnergy", 1500f, false);
            Scribe_Values.Look<Vector3>(ref this.wallStart, "wallStart", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.wallDir, "wallDir", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.wallEnd, "wallEnd", default(Vector3), false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_TargetInfo.Look(ref secondTarget, "TM_refractionEndpoint");
            Scribe_Values.Look(ref promptCancelled, "TM_refractionCancelled");
            Scribe_Collections.Look<IntVec3>(ref this.wallPositions, "wallPositions", LookMode.Value);
            Scribe_Collections.Look(ref mirrored, "TM_refractionMirrored", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && mirrored == null) mirrored = new HashSet<int>();
        }

        public void BeginTargetingWithVerb(TMAbilityDef verbToAdd, TargetingParameters targetParams, Action<LocalTargetInfo> action, Pawn caster = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
        {
            Find.Targeter.BeginTargeting(targetParams, action, caster: caster, actionWhenFinished: actionWhenFinished, mouseAttachment: mouseAttachment);
        }

        private void GetSecondTarget()
        {
            if (promptOpened || promptCancelled || Find.CurrentMap != Map || Find.Targeter.IsTargeting) return;
            var body = launcher as Pawn;
            if (body?.Spawned != true || body.Map != Map ||
                projected && (!ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body, out var soul) || soul != caster))
            { promptCancelled = true; return; }
            promptOpened = true;
            bool Available() => Spawned && Find.CurrentMap == Map && body.Spawned && !body.Dead && body.Map == Map &&
                (!projected || ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body,out var held) && held == caster);
            bool Valid(LocalTargetInfo info) => Available() && info.IsValid && info.Cell != Position && info.Cell.InBounds(Map);
            Find.Targeter.BeginTargeting(TorannMagicDefOf.TM_CompVerb.MainVerb.targetParams,
                info => { if (Valid(info)) { secondTarget = new LocalTargetInfo(info.Cell); promptCancelled = false; } },
                highlightAction: null, targetValidator: Valid, actionWhenFinished: () => { if (!secondTarget.IsValid) promptCancelled = true; },
                onUpdateAction: _ => { if (!Available()) Find.Targeter.StopTargeting(); });
        }

        private void GetWallCells()
        {
            wallPositions = new List<IntVec3>();
            wallPositions.Clear();
            wallLength = Mathf.RoundToInt(Mathf.Clamp(wallLength, 1, wallLengthMax));
            IntVec3 cell = default(IntVec3);
            for(int i = 0; i < wallLength; i++)
            {
                cell = (wallStart + (wallDir * i)).ToIntVec3();
                if (cell.InBounds(Map) && !wallPositions.Contains(cell))
                {
                    wallPositions.Add(cell);
                }
            }
        }

        private void Initialize()
        {
            if (caster != null)
            {
                if (!projected)
                {
                    pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_Refraction);
                    verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_Refraction);
                }
                if (caster.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD))
                {
                    HediffComp_LightCapacitance hd = caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD).TryGetComp<HediffComp_LightCapacitance>();
                    this.wallEnergy *= hd.LightPotency;
                    hd.LightEnergy -= 20f;
                }
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            if (!Spawned || map == null) return;
            if (!this.initialized)
            {
                caster = SourcePawn;
                Initialize();
                this.initialized = true;
            }

            if (caster != null && !caster.DestroyedOrNull() && !caster.Dead)
            {
                if (!wallActive && !secondTarget.IsValid) GetSecondTarget();
                if (promptCancelled) { age = duration + 1; Destroy(); return; }
                if (!this.wallActive && this.secondTarget.IsValid)
                {
                    this.age = 0;
                    this.duration = 1500 + (verVal * 150);
                    this.wallActive = true;
                    this.wallStart = base.Position.ToVector3Shifted();
                    this.wallEnd = this.secondTarget.Cell.ToVector3Shifted();
                    this.wallDir = TM_Calc.GetVector(wallStart, wallEnd);
                    this.wallLength = (wallEnd - wallStart).magnitude;
                    this.secondTarget = null;
                    GetWallCells();
                }

                if (!wallActive)
                {
                    if (Find.TickManager.TicksGame % 6 == 0)
                    {
                        FleckMaker.ThrowDustPuff(base.Position, map, Rand.Range(.6f, .9f));
                    }
                }
                else
                {
                    if (this.Map != null)
                    {
                        if (Find.TickManager.TicksGame % 2 == 0)
                        {
                            RefractProjectiles();
                        }
                    }
                }
            }
            else
            {
                this.age = this.duration;
            }
        }

        public void RefractProjectiles()
        {
            if (wallEnergy <= 0f) { age = duration + 1; Destroy(); return; }
            float eMissVar = .1f + (.015f * pwrVal);
            float fMissVar = .05f - (.01f * pwrVal);
            crossing.Clear();
            for (int k = 0; k < this.wallPositions.Count; k++)
            {
                if (!wallPositions[k].InBounds(Map)) continue;
                List<Thing> cellList = this.wallPositions[k].GetThingList(this.Map);
                for (int i = cellList.Count - 1; i >= 0; i--)
                {
                    Thing t = cellList[i];
                    if (t is Projectile proj && t.def.defName != "Projectile_Refraction")
                    {
                        IntVec3 projOrigin = Traverse.Create(root: proj).Field(name: "origin").GetValue<Vector3>().ToIntVec3();

                        if (proj.Launcher != null && (proj.Launcher.Faction != null || proj.def.defName == "Projectile_LightLaser") && !proj.def.projectile.flyOverhead && !wallPositions.Contains(projOrigin))
                        {
                            if (proj.Launcher.Faction != this.caster.Faction)
                            {
                                Vector3 displayEffect = this.wallPositions[k].ToVector3Shifted();
                                displayEffect.x += Rand.Range(-.2f, .2f);
                                displayEffect.z += Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_LightBarrier, displayEffect, this.Map, .4f, .2f, .05f, .2f, 0, 0, 0, 0);
                                IntVec3 targetVec = Traverse.Create(root: proj).Field(name: "destination").GetValue<Vector3>().ToIntVec3();
                                float targetRange = (this.Position - targetVec).LengthHorizontal;
                                LocalTargetInfo initialTarget = proj.intendedTarget;
                                targetVec.x += Mathf.RoundToInt(Rand.Range(-eMissVar, eMissVar) * targetRange);
                                targetVec.z += Mathf.RoundToInt(Rand.Range(-eMissVar, eMissVar) * targetRange);
                                TM_CopyAndLaunchProjectile.CopyAndLaunchThingFromPosition(proj.def, proj.Launcher, wallPositions[k], this.Map, targetVec.ClampInsideMap(Map), initialTarget, ProjectileHitFlags.All, null);
                                proj.Destroy(DestroyMode.Vanish);
                                this.wallEnergy -= proj.def.projectile.GetDamageAmount(1f, null);
                            }
                            else
                            {
                                if (!crossing.Add(proj.thingIDNumber) || mirrored.Contains(proj.thingIDNumber)) continue;
                                Vector3 displayEffect = this.wallPositions[k].ToVector3Shifted();
                                displayEffect.x += Rand.Range(-.2f, .2f);
                                displayEffect.z += Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_LightBarrier, displayEffect, this.Map, .4f, .2f, .05f, .2f, 0, 0, 0, 0);
                                IntVec3 targetCell = proj.intendedTarget.Cell;
                                float targetRange = (this.Position - targetCell).LengthHorizontal;
                                LocalTargetInfo initialTarget = proj.intendedTarget;
                                targetCell.x += Mathf.RoundToInt(Rand.Range(-fMissVar, fMissVar) * targetRange);
                                targetCell.z += Mathf.RoundToInt(Rand.Range(-fMissVar, fMissVar) * targetRange);
                                TM_CopyAndLaunchProjectile.CopyAndLaunchThingFromPosition(proj.def, proj.Launcher, wallPositions[k], this.Map, targetCell.ClampInsideMap(Map), initialTarget, ProjectileHitFlags.All);
                                this.wallEnergy -= proj.def.projectile.GetDamageAmount(1f, null);
                            }
                            if (wallEnergy <= 0f) { age = duration + 1; Destroy(); return; }
                        }
                    }
                }
            }
            var previous = mirrored;
            mirrored = crossing;
            crossing = previous;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (wallActive)
            {
                float altitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);          //graphic depth
                float wallWidth = Mathf.Clamp(1f * (wallEnergy/1000f), .3f, 2f);
                Vector3 pos = wallStart + (wallDir * wallLength * .5f);
                pos.y = altitude;
                float angle = Vector3Utility.AngleFlat(wallDir);
                Matrix4x4 matrix = new Matrix4x4();
                matrix.SetTRS(pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(wallWidth, altitude, wallLength));   //drawer for wall shell
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_MatPool.LightBarrier, 0);
                if (Rand.Chance(.5f))
                {
                    angle += 180;
                }
                Matrix4x4 matrix2 = new Matrix4x4();
                Vector3 pulsePos = pos;
                pulsePos.x += Rand.Range(-.025f, .025f);
                pulsePos.z += Rand.Range(0.025f, .025f);
                matrix2.SetTRS(pulsePos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(wallWidth * 1.3f, altitude, wallLength * .95f));   //drawer for internal pulse
                Graphics.DrawMesh(MeshPool.plane10, matrix2, TM_MatPool.chiLightning, 0, null, 0, Projectile_Refraction.MatPropertyBlock);         
            }
        }

        protected override void Tick()
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


