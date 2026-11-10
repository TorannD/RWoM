using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;


namespace TorannMagic
{
	public class Projectile_Blizzard : ClassProgression.Projectile_SpiritStormField
	{
        protected override string SkillStem => "TM_Blizzard";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Blizzard;
        public Projectile_Blizzard() : base(720) { }
        protected override int ScaledDuration() => Mathf.RoundToInt(BaseDuration + 90 * verVal * arcaneDmg);
        protected override string SavePrefix => "TM_blizzardSnow";
        protected override void ApplyCellEffect(IntVec3 cell, Map map)
        {
            AddSnowRadial(cell, map, 2f, 2f);
            FleckMaker.ThrowSmoke(cell.ToVector3(), map, 4f);
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
            if (this.age > lastStrikeLarge + Rand.Range(200 - (pwrVal * 30), duration/(4 + pwrVal)) && impactPos.InBoundsWithNullCheck(map) && impactPos.DistanceToEdge(map) >= 2 && impactPos.Standable(map))
            {
                this.lastStrikeLarge = this.age;
                SpawnStrike(TorannMagicDefOf.TM_Blizzard_Large, impactPos, map);
                FleckMaker.ThrowSmoke(impactPos.ToVector3(), map, 5f);
                ScheduleCell(impactPos, TorannMagicDefOf.TM_Blizzard_Large.skyfaller.ticksToImpactRange.RandomInRange+4);
            }
            impactPos = cellRect.RandomCell;
            if (this.age > lastStrikeTiny + Rand.Range(6-(pwrVal), 18-(2*pwrVal)) && impactPos.InBoundsWithNullCheck(map) && impactPos.Standable(map))
            {
                this.lastStrikeTiny = this.age;
                SpawnStrike(TorannMagicDefOf.TM_Blizzard_Tiny, impactPos, map);
                FleckMaker.ThrowSmoke(impactPos.ToVector3(), map, 1f);
                ScheduleCell(impactPos, TorannMagicDefOf.TM_Blizzard_Tiny.skyfaller.ticksToImpactRange.RandomInRange +2);
            }
            impactPos = cellRect.RandomCell;
            if ( this.age > lastStrikeSmall + Rand.Range(30-(2*pwrVal), 60-(4*pwrVal)) && impactPos.InBoundsWithNullCheck(map) && impactPos.Standable(map))
            {
                this.lastStrikeSmall = this.age;
                SpawnStrike(TorannMagicDefOf.TM_Blizzard_Small, impactPos, map);
                FleckMaker.ThrowSmoke(impactPos.ToVector3(), map, 3f);
                ScheduleCell(impactPos, TorannMagicDefOf.TM_Blizzard_Small.skyfaller.ticksToImpactRange.RandomInRange+2);
            }

            ApplyDueEffects(map);
        }

        public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
        {
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (intVec.InBoundsWithNullCheck(map))
                {
                    float lengthHorizontal = (center - intVec).LengthHorizontal;
                    float num2 = 1f - lengthHorizontal / radius;
                    map.snowGrid.AddDepth(intVec, num2 * depth);

                }
            }
        }

    }
}
