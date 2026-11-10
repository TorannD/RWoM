using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace TorannMagic
{
    public class Projectile_Poison : ClassProgression.Projectile_SpiritImpact
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

        protected override string SkillStem => "TM_Poison";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Poison;

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
            if (Scribe.mode == LoadSaveMode.PostLoadInit && initialized) Initialize(hitPawn);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            caster = SourcePawn;
            if (caster == null || Destroyed)
            {
                if (!Destroyed) { age = duration; Destroy(); }
                return;
            }
            if (!projected)
            {
                var magic = caster.GetCompAbilityUserMagic();
                if (magic?.MagicData == null) { age = duration; Destroy(); return; }
                var skills = Skills(magic.MagicData);
                pwrVal = SpellRank(caster, skills, SkillStem, "_pwr");
                verVal = SpellRank(caster, skills, SkillStem, "_ver");
                arcaneDmg = magic.arcaneDmg;
                if (ModOptions.Settings.Instance.AIHardMode && !caster.IsColonist) pwrVal = verVal = 3;
            }
            if (!initialized)
            {
                hitPawn = hitThing as Pawn;
                if (hitPawn?.needs?.food != null && !hitPawn.Dead)
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
                    return;
                }
            }

            if (age > (lastPoison + poisonRate))
            {
                try
                {
                    if (hitPawn != null && !hitPawn.Dead && hitPawn.Spawned && hitPawn.Map == map && !caster.Dead)
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

        protected override void Tick()
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
