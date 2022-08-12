using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;


namespace TorannMagic
{
	public class Projectile_Blizzard : Projectile_AbilityBase
	{
        private int age = 0;
        private int duration = 720;
        private int lastStrikeTiny = 0;
        private int lastStrikeSmall = 0;
        private int lastStrikeLarge = 0;
        private int snowCount = 0;
        private int[] ticksTillSnow = new int[400];
        private IntVec3[] snowPos = new IntVec3[400];
        private bool initialized = false;
        CellRect cellRect;
        Pawn pawn;
        MagicPowerSkill pwr;
        MagicPowerSkill ver;
        private int verVal = 0;
        private int pwrVal = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 720, false);
            Scribe_Values.Look<int>(ref this.lastStrikeTiny, "lastStrikeTiny", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrikeSmall, "lastStrikeSmall", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrikeLarge, "lastStrikeLarge", 0, false);
        }

        public void Initialize(Map map)
        {
            pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Blizzard.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Blizzard_pwr");
            ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Blizzard.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Blizzard_ver");
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            pwrVal = pwr.level;
            verVal = ver.level;
            if (settingsRef.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 1;
                verVal = 1;
            }
            cellRect = CellRect.CenteredOn(base.Position, (int)(base.def.projectile.explosionRadius + (.75 *(verVal + pwrVal))));
            cellRect.ClipInsideMap(map);
            duration = Mathf.RoundToInt(duration + (90 * verVal) * comp.arcaneDmg);
            initialized = true;
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            IntVec3 impactPos;
            if (!initialized)
            {
                Initialize(map);
            }
            impactPos = cellRect.RandomCell;
            if (this.age > lastStrikeLarge + Rand.Range(200 - (pwrVal * 30), duration/(4 + pwrVal)) && impactPos.Standable(map) && impactPos.InBoundsWithNullCheck(map) && impactPos.DistanceToEdge(map) >= 2)
            {
                this.lastStrikeLarge = this.age;
                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Large, impactPos, map);
                FleckMaker.ThrowSmoke(impactPos.ToVector3(), map, 5f);
                ticksTillSnow[snowCount] = TorannMagicDefOf.TM_Blizzard_Large.skyfaller.ticksToImpactRange.RandomInRange+4;
                snowPos[snowCount] = impactPos;
                snowCount++;
            }
            impactPos = cellRect.RandomCell;
            if (this.age > lastStrikeTiny + Rand.Range(6-(pwrVal), 18-(2*pwrVal)) && impactPos.Standable(map) && impactPos.InBoundsWithNullCheck(map))
            {
                this.lastStrikeTiny = this.age;
                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Tiny, impactPos, map);
                FleckMaker.ThrowSmoke(impactPos.ToVector3(), map, 1f);
                ticksTillSnow[snowCount] = TorannMagicDefOf.TM_Blizzard_Tiny.skyfaller.ticksToImpactRange.RandomInRange +2;
                snowPos[snowCount] = impactPos;
                snowCount++;
            }
            impactPos = cellRect.RandomCell;
            if ( this.age > lastStrikeSmall + Rand.Range(30-(2*pwrVal), 60-(4*pwrVal)) && impactPos.Standable(map) && impactPos.InBoundsWithNullCheck(map))
            {
                this.lastStrikeSmall = this.age;
                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Small, impactPos, map);
                FleckMaker.ThrowSmoke(impactPos.ToVector3(), map, 3f);
                ticksTillSnow[snowCount] = TorannMagicDefOf.TM_Blizzard_Small.skyfaller.ticksToImpactRange.RandomInRange+2;
                snowPos[snowCount] = impactPos;
                snowCount++;
            }

            for(int i = 0; i <= snowCount; i++)
            {
                if (ticksTillSnow[i] == 0)
                {
                    AddSnowRadial(snowPos[i], map, 2f, 2f);
                    FleckMaker.ThrowSmoke(snowPos[i].ToVector3(), map, 4f);                
                    ticksTillSnow[i]--;
                }
                else
                {
                    ticksTillSnow[i]--;
                }
            }

        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
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
