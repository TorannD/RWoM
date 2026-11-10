using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public abstract class Projectile_SpiritStormField : Projectile_SpiritBolt
    {
        protected int age;
        protected int duration;
        protected int lastStrikeTiny;
        protected int lastStrikeSmall;
        protected int lastStrikeLarge;
        protected List<int> effectAtAge = new List<int>();
        protected List<IntVec3> effectCells = new List<IntVec3>();
        protected bool initialized;
        private bool statsInitialized;
        protected CellRect cellRect;

        protected Projectile_SpiritStormField(int baseDuration)
        {
            BaseDuration = duration = baseDuration;
        }

        protected int BaseDuration { get; }
        protected abstract int ScaledDuration();
        protected abstract void ApplyCellEffect(IntVec3 cell, Map map);
        protected abstract string SavePrefix { get; }

        protected void SpawnStrike(ThingDef strikeDef, IntVec3 cell, Map map)
        {
            var strike = SkyfallerMaker.SpawnSkyfaller(strikeDef, cell, map);
            if (projected && strike is SpiritStormSkyfaller storm) storm.source = SourcePawn;
        }

        protected void ScheduleCell(IntVec3 cell, int delay)
        {
            effectCells.Add(cell);
            effectAtAge.Add(age + delay);
        }

        protected void ApplyDueEffects(Map map)
        {
            for (int i = 0; i < effectCells.Count;)
            {
                if (age < effectAtAge[i]) { i++; continue; }
                var cell = effectCells[i];
                effectCells.RemoveAt(i);
                effectAtAge.RemoveAt(i);
                if (cell.InBounds(map)) ApplyCellEffect(cell, map);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref effectAtAge, SavePrefix + "Ages", LookMode.Value);
            Scribe_Collections.Look(ref effectCells, SavePrefix + "Cells", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                effectAtAge ??= new List<int>();
                effectCells ??= new List<IntVec3>();
            }
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", BaseDuration, false);
            Scribe_Values.Look<int>(ref this.lastStrikeTiny, "lastStrikeTiny", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrikeSmall, "lastStrikeSmall", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrikeLarge, "lastStrikeLarge", 0, false);
            bool legacyActive = duration != BaseDuration || lastStrikeLarge > 0 || lastStrikeTiny > 0 || lastStrikeSmall > 0;
            Scribe_Values.Look(ref statsInitialized, "TM_stormFieldStats", legacyActive);
        }

        public void Initialize(Map map)
        {
            if (!statsInitialized)
            {
                if (!projected)
                {
                    var pawn = launcher as Pawn;
                    var comp = pawn?.GetCompAbilityUserMagic();
                    if (comp?.MagicData != null)
                    {
                        var skills = Skills(comp.MagicData);
                        pwrVal = skills.FirstOrDefault(x => x.label == SkillStem + "_pwr")?.level ?? 0;
                        verVal = skills.FirstOrDefault(x => x.label == SkillStem + "_ver")?.level ?? 0;
                        arcaneDmg = comp.arcaneDmg;
                        if (ModOptions.Settings.Instance.AIHardMode && !pawn.IsColonist) pwrVal = verVal = 1;
                    }
                }
                duration = ScaledDuration();
                statsInitialized = true;
            }
            cellRect = CellRect.CenteredOn(Position, (int)(def.projectile.explosionRadius + .5f * (verVal + pwrVal)));
            cellRect.ClipInsideMap(map);
            initialized = true;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (age >= duration)
            {
                base.Destroy(mode);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            this.age++;
        }

    }
}
