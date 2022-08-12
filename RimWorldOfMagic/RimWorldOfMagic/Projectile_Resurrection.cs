using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_Resurrection : Projectile_AbilityBase
    {
        private bool initialized = false;
        private bool validTarget = false;
        private int verVal = 0;
        private int pwrVal = 0;
        private int timeToRaise = 1200;
        private int age = -1;
        IntVec3 deadPawnPosition = default(IntVec3);
        Thing corpseThing = null;
        Pawn deadPawn;
        Pawn caster;

        ColorInt colorInt = new ColorInt(255, 255, 140);
        private Sustainer sustainer;

        private float angle = 0;
        private float radius = 5;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.validTarget, "validTarget", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.timeToRaise, "timeToRaise", 1800, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.deadPawnPosition, "deadPawnPosition", default(IntVec3), false);
            Scribe_References.Look<Pawn>(ref this.deadPawn, "deadPawn", false);
            Scribe_References.Look<Thing>(ref this.corpseThing, "corpseThing", false);
        }

        private int TicksLeft
        {
            get
            {
                return this.timeToRaise - this.age;
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            if (!this.initialized)
            {
                if (this.launcher is Pawn caster)
                {
                    CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                    MagicPowerSkill ver = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Resurrection.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Resurrection_ver");
                    MagicPowerSkill pwr = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Resurrection.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Resurrection_eff");
                    verVal = ver.level;
                    pwrVal = pwr.level;
                }
                this.angle = Rand.Range(-12f, 12f);               
                
                IntVec3 curCell = base.Position;

                this.CheckSpawnSustainer();

                if (curCell.InBoundsWithNullCheck(map))
                {
                    Corpse corpse = null;
                    List<Thing> thingList;
                    thingList = curCell.GetThingList(map);
                    int z = 0;
                    while (z < thingList.Count)
                    {
                        corpseThing = thingList[z];
                        if (corpseThing != null)
                        {
                            bool validator = corpseThing is Corpse;
                            if (validator)
                            {
                                corpse = corpseThing as Corpse;
                                CompRottable compRot = corpse.GetComp<CompRottable>(); 
                                
                                deadPawn = corpse.InnerPawn;
                                deadPawnPosition = corpse.Position;
                                if (deadPawn.RaceProps.IsFlesh && !TM_Calc.IsUndead(deadPawn) && compRot != null)
                                {
                                    if (!corpse.IsNotFresh())
                                    {
                                        z = thingList.Count;
                                        this.validTarget = true;
                                        corpse.SetForbidden(true);
                                    }
                                    else
                                    {
                                        Messages.Message("TM_ResurrectionTargetExpired".Translate(), MessageTypeDefOf.RejectInput);                                        
                                    }
                                }
                                if(TM_Calc.IsUndead(deadPawn))
                                {
                                    z = thingList.Count;
                                    this.validTarget = true;
                                }
                            }
                        }
                        z++;
                    }
                }
                this.initialized = true;
            }

            if(corpseThing.Position != this.deadPawnPosition || corpseThing.Map == null)
            {
                Log.Message("Corpse was moved or destroyed during resurrection process.");
                this.age = this.timeToRaise;
            }

            if (this.validTarget)
            {
                if (this.sustainer != null)
                {
                    this.sustainer.info.volumeFactor = this.age / this.timeToRaise;
                    this.sustainer.Maintain();
                    if (this.TicksLeft <= 0)
                    {
                        this.sustainer.End();
                        this.sustainer = null;
                    }
                }
                
                if (this.age+1 == this.timeToRaise)
                {
                    TM_MoteMaker.MakePowerBeamMoteColor(base.Position, base.Map, this.radius * 3f, 2f, 2f, .1f, 1.5f, colorInt.ToColor);
                    if (this.deadPawn == null)
                    {
                        if (corpseThing != null)
                        {
                            Corpse corpse = corpseThing as Corpse;
                            if (corpse != null)
                            {
                                this.deadPawn = corpse.InnerPawn;
                            }
                        }
                    }
                    if (deadPawn != null)
                    {
                        if (TM_Calc.IsUndead(deadPawn))
                        {
                            if(deadPawn.RaceProps.Humanlike)
                            {
                                GenExplosion.DoExplosion(base.Position, this.Map, Rand.Range(10, 16), TMDamageDefOf.DamageDefOf.TM_Holy, this.launcher, Mathf.RoundToInt(Rand.Range(20, 32)), 6, TMDamageDefOf.DamageDefOf.TM_Holy.soundExplosion);
                            }
                            else
                            {
                                GenExplosion.DoExplosion(base.Position, this.Map, Rand.Range(10, 16), TMDamageDefOf.DamageDefOf.TM_Holy, this.launcher, Mathf.RoundToInt(Rand.Range(16, 24)), 3, TMDamageDefOf.DamageDefOf.TM_Holy.soundExplosion);
                            }
                        }
                        else
                        {
                            if (!deadPawn.kindDef.RaceProps.Animal && deadPawn.kindDef.RaceProps.Humanlike)
                            {
                                ResurrectionUtility.ResurrectWithSideEffects(deadPawn);
                                SoundDef.Named("Thunder_OffMap").PlayOneShot(null);
                                SoundDef.Named("Thunder_OffMap").PlayOneShot(null);
                                using (IEnumerator<Hediff> enumerator = deadPawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        Hediff rec = enumerator.Current;
                                        if (rec.def.defName == "ResurrectionPsychosis")
                                        {
                                            if (Rand.Chance(verVal * .33f))
                                            {
                                                deadPawn.health.RemoveHediff(rec);
                                            }
                                        }
                                    }
                                }
                                HealthUtility.AdjustSeverity(deadPawn, HediffDef.Named("TM_ResurrectionHD"), 1f);
                                ReduceSkillsOfPawn(deadPawn, (.35f - .035f * pwrVal));
                                ApplyHealthDefects(deadPawn, .6f - (.06f * verVal), .3f - .03f * verVal);
                            }
                            if (deadPawn.kindDef.RaceProps.Animal)
                            {
                                ResurrectionUtility.Resurrect(deadPawn);
                                HealthUtility.AdjustSeverity(deadPawn, HediffDef.Named("TM_ResurrectionHD"), 1f);
                            }
                        }
                    }                    
                }
            }
            else
            {
                Messages.Message("TM_InvalidResurrection".Translate(
                    caster.LabelShort
                ), MessageTypeDefOf.RejectInput);
                this.age = this.timeToRaise;
            }
        }

        public override void Draw()
        {
            base.Draw();
            Vector3 drawPos = base.Position.ToVector3Shifted(); // this.parent.DrawPos;
            drawPos.z = drawPos.z - 1.5f;
            float num = ((float)base.Map.Size.z - drawPos.z) * 1.41421354f;
            Vector3 a = Vector3Utility.FromAngleFlat(this.angle - 90f);
            Vector3 a2 = drawPos + a * num * 0.5f;
            a2.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            float num2 = Mathf.Min((float)this.age / 10f, 1f);
            Vector3 b = a * ((1f - num2) * num);
            float num3 = 0.975f + Mathf.Sin((float)this.age * 0.3f) * 0.025f;
            if (this.TicksLeft > (this.timeToRaise * .2f))
            {
                num3 *= (float)this.age / (this.timeToRaise * .8f);
            }
            Color arg_50_0 = colorInt.ToColor;
            Color color = arg_50_0;
            color.a *= num3;
            Projectile_Resurrection.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(a2 + a * this.radius * 0.5f + b, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius, 1f, num));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_Resurrection.BeamMat, 0, null, 0, Projectile_Resurrection.MatPropertyBlock);
            Vector3 pos = drawPos + b;
            pos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(pos, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius, 1f, this.radius));
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_Resurrection.BeamEndMat, 0, null, 0, Projectile_Resurrection.MatPropertyBlock);
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < this.timeToRaise;
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

        public static void ReduceSkillsOfPawn(Pawn p, float percent)
        {
            if (p.skills != null)
            {
                p.skills.Learn(SkillDefOf.Shooting, -(p.skills.GetSkill(SkillDefOf.Shooting).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Animals, -(p.skills.GetSkill(SkillDefOf.Animals).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Artistic, -(p.skills.GetSkill(SkillDefOf.Artistic).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Cooking, -(p.skills.GetSkill(SkillDefOf.Cooking).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Crafting, -(p.skills.GetSkill(SkillDefOf.Crafting).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Plants, -(p.skills.GetSkill(SkillDefOf.Plants).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Intellectual, -(p.skills.GetSkill(SkillDefOf.Intellectual).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Medicine, -(p.skills.GetSkill(SkillDefOf.Medicine).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Melee, -(p.skills.GetSkill(SkillDefOf.Melee).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Mining, -(p.skills.GetSkill(SkillDefOf.Mining).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Social, -(p.skills.GetSkill(SkillDefOf.Social).XpTotalEarned * percent), true);
                p.skills.Learn(SkillDefOf.Construction, -(p.skills.GetSkill(SkillDefOf.Construction).XpTotalEarned * percent), true);
            }
        }

        public static void ApplyHealthDefects(Pawn p, float chanceMinor, float chanceMajor)
        {
            List<BodyPartRecord> parts = p.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined).InRandomOrder().ToList();
            for (int k = 0; k < 2; k++)
            {
                if (Rand.Chance(chanceMinor))
                {
                    List<HediffDef> minorHealthDefects = new List<HediffDef>();
                    minorHealthDefects.Add(HediffDefOf.BadBack);
                    minorHealthDefects.Add(HediffDef.Named("HearingLoss"));
                    minorHealthDefects.Add(HediffDefOf.Carcinoma);
                    minorHealthDefects.Add(HediffDef.Named("ChemicalDamageModerate"));

                    HediffDef hdDef = minorHealthDefects.RandomElement();

                    if (hdDef == HediffDefOf.BadBack)
                    {
                        for (int i = 0; i < parts.Count; i++)
                        {
                            if (parts[i].def.defName == "Spine")
                            {
                                p.health.AddHediff(hdDef, parts[i]);
                                break;
                            }
                        }
                    }
                    else if (hdDef == HediffDef.Named("HearingLoss"))
                    {
                        for (int i = 0; i < parts.Count; i++)
                        {
                            if (parts[i].def.tags.Contains(BodyPartTagDefOf.HearingSource))
                            {
                                p.health.AddHediff(hdDef, parts[i]);
                                break;
                            }
                        }
                    }
                    else
                    {
                        p.health.AddHediff(hdDef, parts.RandomElement());
                        Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(hdDef);
                        hd.Severity = Rand.Range(.2f, .6f);
                    }
                }
            }

            if (Rand.Chance(chanceMajor))
            {
                List<HediffDef> majorHealthDefects = new List<HediffDef>();
                majorHealthDefects.Add(HediffDefOf.Frail);
                majorHealthDefects.Add(HediffDefOf.Dementia);
                majorHealthDefects.Add(HediffDefOf.Carcinoma);
                majorHealthDefects.Add(HediffDef.Named("HeartArteryBlockage"));
                majorHealthDefects.Add(HediffDef.Named("ChemicalDamageSevere"));

                HediffDef hdDef = majorHealthDefects.RandomElement();
                if (hdDef == HediffDefOf.Dementia)
                {
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (parts[i].def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource))
                        {
                            p.health.AddHediff(hdDef, parts[i]);
                            break;
                        }
                    }
                }
                else if (hdDef == HediffDefOf.Frail)
                {
                    HealthUtility.AdjustSeverity(p, hdDef, 1f);
                }
                else if(hdDef == HediffDef.Named("HeartArteryBlockage"))
                {
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (parts[i].def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource))
                        {
                            p.health.AddHediff(hdDef, parts[i]);
                            break;
                        }
                    }
                }
                else
                {
                    p.health.AddHediff(hdDef, parts.RandomElement());
                    Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(hdDef);
                    hd.Severity = Rand.Range(.25f, .8f);
                }
            }
        }
    }
}


