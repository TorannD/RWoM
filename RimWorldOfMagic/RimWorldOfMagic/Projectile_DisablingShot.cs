using RimWorld;
using Verse;
using System;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using Verse.AI;
using Verse.AI.Group;


namespace TorannMagic
{
    public class Projectile_DisablingShot : Projectile_SniperRound
    {

        BodyPartRecord vitalPart = null;
        private int verVal;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var body = launcher as Pawn;
            base.Impact(hitThing,blockedByShield);
            if (blockedByShield || !(hitThing is Pawn victim) || victim.Dead || (!shotBound && body==null)) return;
            var comp = body?.GetCompAbilityUserMight();
            if (!shotBound && comp?.MightData == null) return;
            verVal = shotBound ? versatility : TM_Calc.GetSkillVersatilityLevel(body,TorannMagicDefOf.TM_DisablingShot);
            int globalStrength = shotBound ? strength : comp.MightData.MightPowerSkill_global_strength.FirstOrDefault(x=>x.label=="TM_global_strength_pwr")?.level ?? 0;
            if (Rand.Chance(shotBound ? accuracy : body.GetStatValue(StatDefOf.ShootingAccuracyPawn)))
            {
                int dmg = (this.def.projectile.GetDamageAmount(1, null));
                if (victim.RaceProps.IsFlesh)
                {
                    System.Random rnd = new System.Random();
                    if (verVal > 0 && victim.needs?.food != null)
                    {
                        int randomTranqSev = GenMath.RoundRandom(rnd.Next((int)(verVal * .5f * globalStrength), (int)((verVal + .5f * globalStrength) * 3)));
                        LegShot(victim, randomTranqSev, TMDamageDefOf.DamageDefOf.TM_Tranquilizer);
                    }
                    else
                    {
                        LegShot(victim, dmg, TMDamageDefOf.DamageDefOf.TM_DisablingShot);
                    }
                }
                else
                {
                    damageEntities(victim, null, dmg, this.def.projectile.damageDef);
                }
            }
        }

        public void LegShot(Pawn victim, int dmg, DamageDef dmgType)
        {            
            if (!victim.Dead )
            {
                IEnumerable<BodyPartRecord> partSearch = victim.def.race.body.AllParts;
                if( Rand.Chance(.5f)) { vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore)); }
                else { vitalPart = partSearch.LastOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore)); }                
                
                if (vitalPart != null)
                {
                    this.damageEntities(victim, vitalPart, dmg, dmgType);
                }
                else
                {
                    vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbSegment));
                    if (vitalPart != null)
                    {
                        this.damageEntities(victim, vitalPart, dmg, dmgType);
                    }
                    else
                    {
                        this.damageEntities(victim, vitalPart, dmg, dmgType);
                    }
                }
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            if (victim != null && hitPart != null)
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, EffectOwner, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            else
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, EffectOwner, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            victim.TakeDamage(dinfo);

            ApplyFriendlyFireGoodwill(victim,-40);

        }
    }
}
