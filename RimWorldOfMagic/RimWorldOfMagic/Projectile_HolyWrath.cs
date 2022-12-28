using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using TorannMagic.ModOptions;
using UnityEngine;
using TorannMagic.Utils;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_HolyWrath : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int verVal = 0;
        private int pwrVal = 0;
        private int timeToSmite = 360;
        private int strikeNum;
        private int age = -1;
        private float arcaneDmg = 1;
        List<int> wrathAge = new List<int>();
        List<IntVec3> smitePos = new List<IntVec3>();
        Pawn caster;

        ColorInt colorInt = new ColorInt(255, 255, 140);
        private Sustainer sustainer;

        private float angle = 0;
        private float radius = 5;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Other/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.strikeNum, "strikeNum", 5, false);
            Scribe_Values.Look<int>(ref this.timeToSmite, "timeToSmite", 360, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1, false);
            Scribe_Collections.Look<int>(ref this.wrathAge, "wrathAge", LookMode.Value);
            Scribe_Collections.Look<IntVec3>(ref this.smitePos, "smitePos", LookMode.Value);
        }

        private int TicksLeft
        {
            get
            {
                return this.timeToSmite - this.age;
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_HolyWrath.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_HolyWrath_pwr");
                MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_HolyWrath.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_HolyWrath_ver");
                verVal = ver.level;
                pwrVal = pwr.level;
                this.arcaneDmg = comp.arcaneDmg;
                if (Settings.Instance.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 1;
                    verVal = 1;
                }
                this.angle = Rand.Range(-12f, 12f);
                this.strikeNum = 5 + pwrVal;
                IntVec3 curCell = base.Position;
                this.CheckSpawnSustainer();
                this.GetSmites(curCell, map);
                this.GetAffectedPawns(curCell, map);
                this.initialized = true;
            }

            if (this.sustainer != null)
            {
                this.sustainer.info.volumeFactor = this.age / this.timeToSmite;
                this.sustainer.Maintain();
                if (this.TicksLeft <= 0)
                {
                    this.sustainer.End();
                    this.sustainer = null;
                }
            }

            for (int j = 0; j < smitePos.Count; j++)
            {
                if (wrathAge[j] == this.timeToSmite/strikeNum)
                {
                    TM_MoteMaker.MakePowerBeamMoteColor(smitePos[j], base.Map, this.radius * 3f, 2f, .5f, .1f, .5f, colorInt.ToColor);
                    this.caster = this.launcher as Pawn;
                    CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                    GenExplosion.DoExplosion(smitePos[j], map, 3f, TMDamageDefOf.DamageDefOf.TM_Overwhelm, this.launcher as Pawn, Mathf.RoundToInt((12 + TMDamageDefOf.DamageDefOf.TM_Overwhelm.defaultDamage + 3*pwrVal) * this.arcaneDmg), 0, TorannMagicDefOf.TM_Lightning, def, this.equipmentDef, null, null, 0f, 1, null, false, null, 0f, 1, 0f, false);
                }
            }
        }

        public void GetSmites(IntVec3 center, Map map)
        {
            CellRect cellRect = CellRect.CenteredOn(center, Mathf.RoundToInt(this.def.projectile.explosionRadius - 1));
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < this.strikeNum; i++)
            {
                this.smitePos.Add(cellRect.RandomCell);
                this.wrathAge.Add(Rand.Range(-(i * this.timeToSmite / (2*strikeNum)), -(i * this.timeToSmite / strikeNum)));
            }
        }

        public void GetAffectedPawns(IntVec3 center, Map map)
        {
            foreach (IntVec3 curCell in GenRadial.RadialCellsAround(center, def.projectile.explosionRadius, true))
            {
                if (!curCell.InBoundsWithNullCheck(map) || !curCell.IsValid) return;
                Pawn victim = curCell.GetFirstPawn(map);
                if (victim == null || victim.Dead) return;

                if (victim.Faction == caster.Faction)
                {
                    if (verVal >= 1)
                    {
                        HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_HediffTimedInvulnerable, 1f);
                        Hediff hd = victim.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffTimedInvulnerable);
                        HediffComp_Disappears hdc = hd.TryGetComp<HediffComp_Disappears>();
                        if (hdc != null)
                        {
                            hdc.ticksToDisappear += 360;
                        }
                    }
                    if (verVal >= 2)
                    {
                        if (!victim.Dead && !TM_Calc.IsUndead(victim))
                        {
                            IEnumerable<Hediff_Injury> injuries = victim.health.hediffSet.hediffs
                                .OfType<Hediff_Injury>()
                                .Where(injury => injury.CanHealNaturally())
                                .DistinctBy(injury => injury.Part)
                                .Take(3);

                            float healAmount = caster.IsColonist ? 5.0f * arcaneDmg : 20.0f;
                            foreach (Hediff_Injury injury in injuries)
                            {
                                injury.Heal(healAmount);
                                TM_MoteMaker.ThrowRegenMote(victim.Position.ToVector3Shifted(), victim.Map, .6f);
                                TM_MoteMaker.ThrowRegenMote(victim.Position.ToVector3Shifted(), victim.Map, .4f);
                            }
                        }
                    }                
                    if (verVal >= 3)
                    {
                        HealthUtility.AdjustSeverity(victim, HediffDef.Named("BestowMightHD"), 1f);
                    }

                }
                if(TM_Calc.IsUndead(victim))
                {
                    TM_Action.DamageUndead(victim, Rand.Range(5f, 12f) * this.arcaneDmg, this.launcher);
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            for (int i = 0; i < smitePos.Count; i++)
            {
                if (wrathAge[i] >= 0 && wrathAge[i] <= this.timeToSmite/this.strikeNum)
                {
                    DrawSmiteBeams(smitePos[i], wrathAge[i]);
                }
            }
        }

        public void DrawSmiteBeams(IntVec3 pos, int wrathAge)
        {
            Vector3 drawPos = pos.ToVector3Shifted(); // this.parent.DrawPos;
            drawPos.z = drawPos.z - 1.5f;
            float num = ((float)base.Map.Size.z - drawPos.z) * 1.4f;
            Vector3 a = Vector3Utility.FromAngleFlat(this.angle - 90f);  //angle of beam
            Vector3 a2 = drawPos + a * num * 0.5f;                      //
            a2.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays); //mote depth
            float num2 = Mathf.Min((float)wrathAge / 10f, 1f);          //
            Vector3 b = a * ((1f - num2) * num);
            float num3 = 0.975f + Mathf.Sin((float)wrathAge * 0.3f) * 0.025f;       //color
            if (this.TicksLeft > (this.timeToSmite * .2f))                          //color
            {
                num3 *= (float)this.age / (this.timeToSmite * .8f);
            }
            Color arg_50_0 = colorInt.ToColor;
            Color color = arg_50_0;
            color.a *= num3;
            Projectile_HolyWrath.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(a2 + a * this.radius * 0.5f + b, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius, 1f, num));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_HolyWrath.BeamMat, 0, null, 0, Projectile_HolyWrath.MatPropertyBlock);
            Vector3 vectorPos = drawPos + b;
            vectorPos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(vectorPos, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius, 1f, this.radius));                 //drawer for beam end
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_HolyWrath.BeamEndMat, 0, null, 0, Projectile_HolyWrath.MatPropertyBlock);
            num = num - (num * ((float)wrathAge/(float)(this.timeToSmite/this.strikeNum)));
            Vector3 a3 = a * num;
            Vector3 vectorOrb = drawPos + a3;
            vectorOrb.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix3 = default(Matrix4x4);
            matrix3.SetTRS(vectorOrb, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius*((5*(float)wrathAge)/((float)this.timeToSmite/this.strikeNum)), 1f, this.radius * ((5 * (float)wrathAge) / ((float)this.timeToSmite/this.strikeNum))));                 //drawer for beam end
            Graphics.DrawMesh(MeshPool.plane10, matrix3, Projectile_HolyWrath.BombardMat, 0, null, 0, Projectile_HolyWrath.MatPropertyBlock);
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
            for (int i = 0; i < this.wrathAge.Count; i++)
            {
                this.wrathAge[i]++;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.timeToSmite;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        private void CheckSpawnSustainer()
        {
            if (this.TicksLeft >= 0)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    this.sustainer = SoundDef.Named("OrbitalBeam").TrySpawnSustainer(SoundInfo.InMap(this.selectedTarget, MaintenanceType.PerTick));
                });
            }
        }
    }
}


