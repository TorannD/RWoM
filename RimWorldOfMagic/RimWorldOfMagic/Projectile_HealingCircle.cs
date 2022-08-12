using Verse;
using RimWorld;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_HealingCircle : Projectile_AbilityBase
    {
        int pwrVal = 0;
        int verVal = 0;
        private float arcaneDmg = 1;
        int age = -1;
        int duration = 1200;
        float radius = 6;
        bool initialized = false;
        int healDelay = 40;
        int waveDelay = 300;
        int lastHeal = 0;
        int lastWave = 0;
        Pawn caster;
        float angle;
        float innerRing;
        float outerRing;
        float ringFrac;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1200, false);
            Scribe_Values.Look<int>(ref this.healDelay, "healDelay", 30, false);
            Scribe_Values.Look<int>(ref this.lastHeal, "lastHeal", 0, false);
            Scribe_Values.Look<int>(ref this.waveDelay, "waveDelay", 240, false);
            Scribe_Values.Look<int>(ref this.lastWave, "lastWave", 0, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 6, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
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
            this.caster = this.launcher as Pawn;

            if(!this.initialized)
            {
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_HealingCircle.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_HealingCircle_pwr");
                MagicPowerSkill ver = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_HealingCircle.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_HealingCircle_ver");
                pwrVal = pwr.level;
                verVal = ver.level;
                if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (!caster.IsColonist && settingsRef.AIHardMode)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.radius = this.def.projectile.explosionRadius;
                this.angle = Rand.Range(-12, 12);
                this.duration = this.duration + (180 * (pwrVal + verVal));
                //TM_MoteMaker.MakePowerBeamMote(base.Position, base.Map, this.radius * 8f, 1.2f, this.duration/60f);
                this.initialized = true;
            }

            if (this.age >= this.lastWave)
            {
                if (this.age >= this.lastHeal + this.healDelay)
                {
                    switch (ringFrac)
                    {
                        case 0:
                            this.innerRing = 0;
                            this.outerRing = this.radius * ((ringFrac + .1f) / 5f);
                            TM_MoteMaker.MakePowerBeamMote(base.Position, base.Map, this.radius * 6f, 1.2f, this.waveDelay / 60f, (this.healDelay * 3f) / 60f, (this.healDelay * 2f) / 60f);
                            break;
                        case 1:
                            this.innerRing = this.outerRing;
                            this.outerRing = this.radius * ((ringFrac) / 5f);                            
                            break;
                        case 2:
                            this.innerRing = this.outerRing;
                            this.outerRing = this.radius * ((ringFrac) / 5f);
                            break;
                        case 3:
                            this.innerRing = this.outerRing;
                            this.outerRing = this.radius * ((ringFrac) / 5f);
                            break;
                        case 4:
                            this.innerRing = this.outerRing;
                            this.outerRing = this.radius * ((ringFrac) / 5f);
                            break;
                        case 5:
                            this.innerRing = this.outerRing;
                            this.outerRing = this.radius;
                            this.lastWave = this.age + this.waveDelay;
                            break;
                    }                    
                    ringFrac++;
                    this.lastHeal = this.age;
                    Search(map);
                }
                if (ringFrac >= 6)
                {
                    ringFrac = 0;
                }
            }       
        }

        public void Search(Map map)
        {
            IntVec3 curCell;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, this.radius, true);
            IEnumerable<IntVec3> innerCircle = GenRadial.RadialCellsAround(base.Position, this.innerRing, true);
            IEnumerable<IntVec3> outerCircle = GenRadial.RadialCellsAround(base.Position, this.outerRing, true);

            for (int i = innerCircle.Count(); i < outerCircle.Count(); i++)
            {                
                Pawn pawn = null;                
                curCell = targets.ToArray<IntVec3>()[i];
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                {
                    pawn = curCell.GetFirstPawn(map);
                }
                if (pawn != null && (pawn.Faction == caster.Faction || pawn.IsPrisoner || pawn.Faction == null || (pawn.Faction != null && !pawn.Faction.HostileTo(caster.Faction))) && !TM_Calc.IsUndead(pawn))
                {
                    Heal(pawn);
                }
                if(pawn != null && TM_Calc.IsUndead(pawn))
                {
                    TM_Action.DamageUndead(pawn, (10.0f + (float)pwrVal * 3f) * this.arcaneDmg, this.launcher);
                }
            }
        }

        public void Heal(Pawn pawn)
        {
            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                int num = 1 + verVal;
                
                using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;
                        bool flag2 = num > 0;                        

                        if (flag2)
                        {
                            int num2 = 1 + verVal;
                            IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                            Func<Hediff_Injury, bool> arg_BB_1;

                            arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                            foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                            {
                                bool flag4 = num2 > 0;
                                if (flag4)
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                    if (flag5)
                                    {
                                        //current.Heal((float)((int)current.Severity + 1));
                                        if (Rand.Chance(.8f))
                                        {
                                            current.Heal((10.0f + (float)pwrVal * 2f) * this.arcaneDmg); // power affects how much to heal
                                            TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, 1.2f);
                                        }
                                        num--;
                                        num2--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }        

        public override void Tick()
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


