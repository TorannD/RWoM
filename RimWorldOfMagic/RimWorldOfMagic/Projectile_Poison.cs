using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace TorannMagic
{
    public class Projectile_Poison : Projectile_AbilityBase
    {
        BodyPartRecord[] vulnerableParts = new BodyPartRecord[10];
        private bool initialized = false;
        private int age = 0;
        private int lastPoison = 0;
        private int poisonRate = 270;
        private int duration = 2700;
        private IntVec3 oldPosition;
        Pawn hitPawn = null;
        Pawn caster = null;

        MagicPowerSkill pwr;
        MagicPowerSkill ver;
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 2700, false);
            Scribe_Values.Look<int>(ref this.lastPoison, "lastPoison", 0, false);
            Scribe_Values.Look<int>(ref this.poisonRate, "poisonRate", 270, false);
            Scribe_Values.Look<IntVec3>(ref this.oldPosition, "oldPosition", default(IntVec3), false);
            Scribe_References.Look<Pawn>(ref this.hitPawn, "hitPawn", false);
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
                        
            caster = this.launcher as Pawn;
            pwr = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Poison.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Poison_pwr");
            ver = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Poison.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Poison_ver");
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            pwrVal = pwr.level;
            verVal = ver.level;
            if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }
            this.arcaneDmg = caster.GetCompAbilityUserMagic().arcaneDmg;
            if (settingsRef.AIHardMode && !caster.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }
            if (!initialized)
            {
                hitPawn = hitThing as Pawn;
                if (hitThing != null && hitPawn.needs.food != null)
                {
                    duration += (verVal * 180);                    
                    Initialize(hitPawn);
                    oldPosition = hitPawn.Position;
                    damageEntities(hitPawn, null, 4 , TMDamageDefOf.DamageDefOf.TM_Poison);
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Poisoned_HD, Rand.Range(1f + verVal, 4f + (2f * verVal)));                    
                    initialized = true;
                    TM_MoteMaker.ThrowPoisonMote(hitPawn.Position.ToVector3(), map, 2.2f);
                    if (hitPawn.IsColonist && !caster.IsColonist)
                    {
                        TM_Action.SpellAffectedPlayerWarning(hitPawn);
                    }
                }
                else
                {
                    Log.Message("No target found for poison or target not susceptable to poison.");
                    this.age = this.duration + 1;
                    this.Destroy(DestroyMode.Vanish);
                }
            }

            if (age > (lastPoison + poisonRate))
            {
                try
                {
                    if (hitPawn != null && !hitPawn.Dead && caster != null && !caster.Dead)
                    {
                        if (hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Poisoned_HD, false))
                        {
                            int dmg = (int)(((hitPawn.Position - oldPosition).LengthHorizontal) * (1 + (.25f * pwrVal)));
                            int rndPart = (int)Rand.RangeInclusive(0, 4);
                            damageEntities(hitPawn, vulnerableParts[rndPart], dmg, TMDamageDefOf.DamageDefOf.TM_Poison);
                            TM_MoteMaker.ThrowPoisonMote(hitPawn.Position.ToVector3(), map, 1f);
                            this.lastPoison = this.age;
                            oldPosition = hitPawn.Position;
                        }
                        else
                        {
                            //no longer poisoned, end
                            this.age = this.duration + 1;
                            this.Destroy(DestroyMode.Vanish);
                        }
                    }
                    else
                    {
                        //pawn is dead, end 
                        this.age = this.duration + 1;
                        this.Destroy(DestroyMode.Vanish);
                    }
                }
                catch
                {
                    this.age = this.duration + 1;
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            
        }

        public void Initialize(Pawn victim)
        {
            BodyPartRecord vitalPart = null;
            if (victim != null && !victim.Dead)
            {
                
                IEnumerable<BodyPartRecord> partSearch = victim.def.race.body.AllParts;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource));
                if (vitalPart != null)
                {
                    this.vulnerableParts[0] = vitalPart;
                }
                vitalPart = null;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationKidney));
                if (vitalPart != null)
                {
                    this.vulnerableParts[1] = vitalPart;
                }
                vitalPart = null;
                vitalPart = partSearch.LastOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationKidney));
                if (vitalPart != null)
                {
                    this.vulnerableParts[2] = vitalPart;
                }
                vitalPart = null;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationLiver));
                if (vitalPart != null)
                {
                    this.vulnerableParts[3] = vitalPart;
                }
                vitalPart = null;
                vitalPart = partSearch.LastOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationLiver));
                if (vitalPart != null)
                {
                    this.vulnerableParts[4] = vitalPart;
                }
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = Mathf.RoundToInt((float)amt * Rand.Range(.5f, 1.2f) * this.arcaneDmg);
            if (this.caster != null && victim != null && !victim.Dead && !victim.Downed && hitPart != null)
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, this.caster, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                dinfo.SetAllowDamagePropagation(false);
                victim.TakeDamage(dinfo);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }
    }
}
