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
    public class Projectile_BloodMoon : Projectile_AbilityBase
    {
        private bool initialized = false;
        private bool validTarget = false;
        private int verVal = 0;
        private int pwrVal = 0;
        private int effVal = 0;
        private float arcaneDmg = 1;
        private int duration = 1200;
        private int age = -1;
        private int bloodFrequency = 8;
        private float attackFrequency = 30;
        List<IntVec3> bloodCircleCells = new List<IntVec3>();
        List<IntVec3> bloodCircleOuterCells = new List<IntVec3>();
        
        Pawn caster;
        List<Pawn> victims = new List<Pawn>();
        List<int> victimHitTick = new List<int>();
        List<float> wolfDmg = new List<float>();

        private int delayTicks = 25;
        private int nextAttack = 0;

        ColorInt colorInt = new ColorInt(45, 0, 4, 250);
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
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1200, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1f, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 6f, false);
            Scribe_Values.Look<float>(ref this.attackFrequency, "attackFrequency", 30f, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Collections.Look<Pawn>(ref this.victims, "victims", LookMode.Reference);
            Scribe_Collections.Look<int>(ref this.victimHitTick, "victimHitTick", LookMode.Value);
            Scribe_Collections.Look<float>(ref this.wolfDmg, "wolfDmg", LookMode.Value);
            Scribe_Collections.Look<IntVec3>(ref this.bloodCircleOuterCells, "bloodCircleOuterCells", LookMode.Value);
            Scribe_Collections.Look<IntVec3>(ref this.bloodCircleCells, "bloodCircleCells", LookMode.Value);
        }

        private int TicksLeft
        {
            get
            {
                return this.duration - this.age;
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            if (!this.initialized)
            {

                this.bloodCircleOuterCells = new List<IntVec3>();
                this.bloodCircleOuterCells.Clear();
                this.victimHitTick = new List<int>();
                this.victimHitTick.Clear();
                this.victims = new List<Pawn>();
                this.victims.Clear();
                this.wolfDmg = new List<float>();
                this.wolfDmg.Clear();

                caster = this.launcher as Pawn;
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                MagicPowerSkill bpwr = comp.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_pwr");
                pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodMoon_pwr").level;
                verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodMoon_ver").level;
                effVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodMoon_eff").level;
                this.arcaneDmg = comp.arcaneDmg;
                this.arcaneDmg *= (1f + (.1f * bpwr.level));
                this.attackFrequency *= (1 - (.05f * effVal));
                this.duration = Mathf.RoundToInt(this.duration + (this.duration * .1f * verVal));

                this.angle = Rand.Range(-2f, 2f);
                this.radius = this.def.projectile.explosionRadius;
                
                IntVec3 curCell = base.Position;

                this.CheckSpawnSustainer();

                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                {
                    List<IntVec3> cellList = GenRadial.RadialCellsAround(base.Position, this.radius, true).ToList();
                    for (int i = 0; i < cellList.Count; i++)
                    {
                        curCell = cellList[i];
                        if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                        {
                            this.bloodCircleCells.Add(curCell);
                        }
                    }
                    cellList.Clear();
                    cellList = GenRadial.RadialCellsAround(base.Position, this.radius+1, true).ToList();
                    List<IntVec3> outerRing = new List<IntVec3>();
                    for (int i = 0; i < cellList.Count; i++)
                    {
                        curCell = cellList[i];
                        if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                        {
                            outerRing.Add(curCell);
                        }
                    }
                    this.bloodCircleOuterCells = outerRing.Except(this.bloodCircleCells).ToList();
                }

                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodCircle, base.Position.ToVector3Shifted(), caster.Map, this.radius + 2, (this.duration/60) *.9f, (this.duration / 60) * .06f, (this.duration / 60) * .08f, Rand.Range(-50, -50), 0, 0, Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodCircle, base.Position.ToVector3Shifted(), caster.Map, this.radius + 2, (this.duration / 60) * .9f, (this.duration / 60) * .06f, (this.duration / 60) * .08f, Rand.Range(50, 50), 0, 0, Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodCircle, base.Position.ToVector3Shifted(), caster.Map, this.radius + 2, (this.duration / 60) * .9f, (this.duration / 60) * .06f, (this.duration / 60) * .08f, Rand.Range(-50,50), 0, 0, Rand.Range(0, 360));
                caster.Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_BloodMoon(caster.Map, this.duration, 2f - (this.pwrVal * .1f)));
                this.initialized = true;
            }            

            if (this.initialized && this.Map != null && this.age > 15)
            {
                if (this.victims.Count > 0)
                {
                    for (int i = 0; i < this.victims.Count; i++)
                    {
                        if (this.victimHitTick[i] < this.age)
                        {
                            TM_Action.DamageEntities(victims[i], null, Mathf.RoundToInt((Rand.Range(5, 8) * this.wolfDmg[i])*this.arcaneDmg), DamageDefOf.Bite, this.launcher);
                            TM_MoteMaker.ThrowBloodSquirt(victims[i].DrawPos, victims[i].Map, Rand.Range(.6f, 1f));
                            this.victims.Remove(this.victims[i]);
                            this.victimHitTick.Remove(this.victimHitTick[i]);
                            this.wolfDmg.Remove(this.wolfDmg[i]);
                        }
                    }
                }

                if (Find.TickManager.TicksGame % this.bloodFrequency == 0)
                {
                    Filth filth = (Filth)ThingMaker.MakeThing(ThingDefOf.Filth_Blood);
                    GenSpawn.Spawn(filth, this.bloodCircleOuterCells.RandomElement(), this.Map);
                }

                if(this.nextAttack < this.age && !this.caster.DestroyedOrNull() && !this.caster.Dead)
                {

                    Pawn victim = TM_Calc.FindNearbyEnemy(base.Position, this.Map, this.caster.Faction, this.radius, 0);
                    if (victim != null)
                    {
                        IntVec3 rndPos = victim.Position;
                        while (rndPos == victim.Position)
                        {
                            rndPos = this.bloodCircleCells.RandomElement();
                        }
                        Vector3 wolf = rndPos.ToVector3Shifted();
                        Vector3 direction = TM_Calc.GetVector(wolf, victim.DrawPos);
                        float angle = direction.ToAngleFlat();
                        float fadeIn = .1f;
                        float fadeOut = .25f;
                        float solidTime = .10f;
                        float drawSize = Rand.Range(.7f, 1.2f)+(this.pwrVal *.1f);
                        float velocity = (victim.DrawPos - wolf).MagnitudeHorizontal();
                        if (angle >= -135 && angle < -45) //north
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodWolfNorth, wolf, this.Map, drawSize, solidTime, fadeIn, fadeOut, 0, 2*velocity, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 0);
                        }
                        else if (angle >= 45 && angle < 135) //south
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodWolfSouth, wolf, this.Map, drawSize, solidTime, fadeIn, fadeOut, 0, 2 * velocity, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 0);
                        }
                        else if (angle >= -45 && angle < 45) //east
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodWolfEast, wolf, this.Map, drawSize, solidTime, fadeIn, fadeOut, 0, 2 * velocity, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 0);
                        }
                        else //west
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodWolfWest, wolf, this.Map, drawSize, solidTime, fadeIn, fadeOut, 0, 2 * velocity, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 0);
                        }
                        int hitDelay = this.age + this.delayTicks;
                        Effecter BloodShieldEffect = TorannMagicDefOf.TM_BloodShieldEffecter.Spawn();
                        BloodShieldEffect.Trigger(new TargetInfo(wolf.ToIntVec3(), this.Map, false), new TargetInfo(wolf.ToIntVec3(), this.Map, false));
                        BloodShieldEffect.Cleanup();
                        this.victims.Add(victim);
                        this.victimHitTick.Add(hitDelay);
                        this.wolfDmg.Add(drawSize);
                        if (Rand.Chance(.1f))
                        {
                            if (Rand.Chance(.65f))
                            {
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(wolf.ToIntVec3(), this.Map, false), MaintenanceType.None);
                                SoundDef.Named("TM_DemonCallHigh").PlayOneShot(info);
                            }
                            else
                            {
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(wolf.ToIntVec3(), this.Map, false), MaintenanceType.None);
                                info.pitchFactor = .8f;
                                info.volumeFactor = .8f;
                                SoundDef.Named("TM_DemonPain").PlayOneShot(info);
                            }
                        }
                    }
                    this.nextAttack = this.age + Mathf.RoundToInt(Rand.Range(.4f * (float)this.attackFrequency, .8f * (float)this.attackFrequency));
                }
            }
        }

        public override void Draw()
        {
            float beamSize = 8f;
            Vector3 drawPos = base.Position.ToVector3Shifted(); // this.parent.DrawPos;
            drawPos.z = drawPos.z - ((.5f * beamSize)*this.radius);
            float num = ((float)base.Map.Size.z - drawPos.z) * 1.4f;
            Vector3 a = Vector3Utility.FromAngleFlat(this.angle - 90f);  //angle of beam
            Vector3 a2 = drawPos + a * num * 0.5f;                      //
            a2.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays); //mote depth
            float num2 = Mathf.Min((float)this.age / 10f, 1f);          //
            Vector3 b = a * ((1f - num2) * num);
            float num3 = 0.975f + (.15f) * 0.025f;       //color
            if (this.age < (this.duration * .1f))                          //color
            {
                num3 *= (float)(this.age) / (this.duration * .1f);
            }
            if(this.age > (.9f * this.duration))
            {
                num3 *= (float)(this.duration - this.age) / (this.duration * .1f);
            }
            Color arg_50_0 = colorInt.ToColor;
            Color color = arg_50_0;
            color.a *= num3;
            Projectile_BloodMoon.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(a2 + a * (this.radius*beamSize) * 0.5f + b, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius*beamSize, 1f, num));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_BloodMoon.BeamMat, 0, null, 0, Projectile_BloodMoon.MatPropertyBlock);
            Vector3 vectorPos = drawPos;
            //vectorPos.z -= (this.radius * (.5f * beamSize));
            vectorPos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(vectorPos, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius * beamSize, 1f, this.radius * beamSize));                 //drawer for beam end
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_BloodMoon.BeamEndMat, 0, null, 0, Projectile_BloodMoon.MatPropertyBlock);
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;            
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < this.duration;
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


