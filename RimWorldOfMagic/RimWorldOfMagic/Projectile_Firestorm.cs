using RimWorld;
using Verse;
using System.Collections.Generic;


namespace TorannMagic
{
	public class Projectile_Firestorm : ClassProgression.Projectile_SpiritStormField
	{
        protected override string SkillStem => "TM_Firestorm";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Firestorm;
        public Projectile_Firestorm() : base(420) { }
        protected override int ScaledDuration() => (int)((BaseDuration + 60 * verVal) * arcaneDmg);
        protected override string SavePrefix => "TM_firestormShrapnel";
        protected override void ApplyCellEffect(IntVec3 cell, Map map)
        {
            GenExplosion.DoExplosion(cell, map, .4f, TMDamageDefOf.DamageDefOf.TM_Firestorm_Small, SpellInstigator,
                Rand.Range(5, def.projectile.GetDamageAmount(1, null)), 0, SoundDefOf.BulletImpact_Ground, def, equipmentDef,
                null, null, 0f, 1, null, null, 0, false, null, 0f, 1, 0.2f, false);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            IntVec3 impactPos;
            if (!initialized)
            {
                Initialize(map);
            }

            impactPos = cellRect.RandomCell;
            if (this.age > lastStrikeLarge + Rand.Range((200/(1+pwrVal))+20, (duration/(1+pwrVal))+40) && impactPos.InBoundsWithNullCheck(map) && impactPos.Standable(map))
            {
                this.lastStrikeLarge = this.age;
                SpawnStrike(TorannMagicDefOf.TM_Firestorm_Large, impactPos, map);
                CellRect cellRectSec = CellRect.CenteredOn(impactPos, (int)(TorannMagicDefOf.TM_Firestorm_Large.skyfaller.explosionRadius + 2));
                for (int j = 0; j < (int)Rand.Range(1 + verVal, 5 + verVal); j++)
                {
                    ScheduleCell(cellRectSec.RandomCell, TorannMagicDefOf.TM_Firestorm_Large.skyfaller.ticksToImpactRange.RandomInRange + 8);
                }                
            }
            impactPos = cellRect.RandomCell;
            if (this.age > lastStrikeTiny + Rand.Range(7-pwrVal, 20-pwrVal) && impactPos.InBoundsWithNullCheck(map) && impactPos.Standable(map))
            {
                this.lastStrikeTiny = this.age;
                SpawnStrike(TorannMagicDefOf.TM_Firestorm_Tiny, impactPos, map);
            }
            impactPos = cellRect.RandomCell;
            if ( this.age > lastStrikeSmall + Rand.Range(18-(2*pwrVal), 42-(2*pwrVal)) && impactPos.InBoundsWithNullCheck(map) && impactPos.Standable(map))
            {
                this.lastStrikeSmall = this.age;
                SpawnStrike(TorannMagicDefOf.TM_Firestorm_Small, impactPos, map);
            }

            ApplyDueEffects(map);
        }
    }
}
