using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    public enum FleshSkill { Devour, Restore, Empower, Harden, Feast, Mobility, Strength, Industry, Capacity }

    public sealed class FleshProgress : IExposable
    {
        public int Level = 1, SkillPoints, SchemaVersion = 1, LastTick;
        public float Experience, Thirst = 20f;
        public int EmpowerUntil, HardenUntil, FeastUntil;
        public bool FeastRage;
        private Dictionary<FleshSkill, int> ranks = new Dictionary<FleshSkill, int>();
        private Dictionary<FleshSkill, int> readyTicks = new Dictionary<FleshSkill, int>();

        public static int MaxRank(FleshSkill skill)
        {
            switch (skill)
            {
                case FleshSkill.Devour: case FleshSkill.Restore: case FleshSkill.Empower: case FleshSkill.Harden: return 3;
                case FleshSkill.Feast: return 1;
                case FleshSkill.Mobility: case FleshSkill.Strength: case FleshSkill.Industry: return 10;
                case FleshSkill.Capacity: return 8;
                default: return 0;
            }
        }

        public int GetRank(FleshSkill skill) => ranks != null && ranks.TryGetValue(skill, out int rank) ? rank : skill == FleshSkill.Devour ? 1 : 0;
        public float MaxThirst => 20f + 10f * GetRank(FleshSkill.Capacity);
        public float RequiredExperience => 100f + 25f * (Level - 1);
        public bool CanUnlockFeast
        {
            get
            {
                foreach (FleshSkill skill in Enum.GetValues(typeof(FleshSkill)))
                    if (skill != FleshSkill.Feast && GetRank(skill) < MaxRank(skill)) return false;
                return true;
            }
        }

        public bool TryUpgrade(FleshSkill skill)
        {
            if (SkillPoints <= 0 || MaxRank(skill) == 0 || GetRank(skill) >= MaxRank(skill) || (skill == FleshSkill.Feast && !CanUnlockFeast)) return false;
            if (ranks == null) ranks = new Dictionary<FleshSkill, int>();
            ranks[skill] = GetRank(skill) + 1;
            SkillPoints--;
            return true;
        }

        public void AddExperience(float amount)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            Experience += amount;
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                SkillPoints++;
            }
        }

        public void AddThirst(float amount)
        {
            if (float.IsNaN(amount)) return;
            Thirst = Math.Max(0f, Math.Min(MaxThirst, Thirst + amount));
        }

        public int GetReadyTick(FleshSkill skill) => readyTicks != null && readyTicks.TryGetValue(skill, out int tick) ? tick : 0;
        public void SetReadyTick(FleshSkill skill, int tick)
        {
            if (readyTicks == null) readyTicks = new Dictionary<FleshSkill, int>();
            readyTicks[skill] = tick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "schemaVersion", 1);
            Scribe_Values.Look(ref Level, "level", 1);
            Scribe_Values.Look(ref Experience, "experience");
            Scribe_Values.Look(ref SkillPoints, "skillPoints");
            Scribe_Values.Look(ref Thirst, "thirst", 20f);
            Scribe_Values.Look(ref LastTick, "lastTick");
            Scribe_Values.Look(ref EmpowerUntil, "empowerUntil");
            Scribe_Values.Look(ref HardenUntil, "hardenUntil");
            Scribe_Values.Look(ref FeastUntil, "feastUntil");
            Scribe_Values.Look(ref FeastRage, "feastRage");
            Scribe_Collections.Look(ref ranks, "ranks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref readyTicks, "readyTicks", LookMode.Value, LookMode.Value);
        }
    }
}
