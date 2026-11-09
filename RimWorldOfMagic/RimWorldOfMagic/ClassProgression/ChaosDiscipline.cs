using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class ChaosDisciplineState : IExposable
    {
        private List<string> borrowed = new List<string>();
        private List<string> practiced = new List<string>();
        private string anchor;
        private int generation, lastPracticeGeneration = -1, diverseSets;
        public IReadOnlyList<string> Borrowed => borrowed.AsReadOnly();
        public string Anchor => anchor;
        public int Practiced => practiced.Count;
        public int DiverseSets => diverseSets;
        internal bool Contains(string id) => id != null && borrowed.Contains(id);
        internal bool SelectAnchor(string id, DisciplineProgress path)
        {
            if (!ReferenceEquals(path?.Chaos, this) || path.ClassId != "rwom.chaos" || path.Stage != InitiationStage.Ascended || (id != null && !Contains(id))) return false;
            anchor = id;
            return true;
        }
        internal void Dealt(IEnumerable<string> ids)
        {
            borrowed = ids.Distinct().ToList();
            anchor = null;
            generation++;
        }
        internal void Used(DisciplineProgress path, string id)
        {
            if (!Contains(id)) return;
            if (path.Stage == InitiationStage.Foundation && !practiced.Contains(id))
            {
                practiced.Add(id);
                if (practiced.Count >= 2) path.RecordCompletedTrial("rwom.chaos.foundation");
            }
            if (path.Stage == InitiationStage.Adept && lastPracticeGeneration != generation)
            {
                lastPracticeGeneration = generation;
                if (++diverseSets >= 2) path.RecordCompletedTrial("rwom.chaos.mastery");
            }
        }
        public void ExposeData()
        {
            Scribe_Collections.Look(ref borrowed,"borrowed",LookMode.Value);
            Scribe_Collections.Look(ref practiced,"practiced",LookMode.Value);
            Scribe_Values.Look(ref anchor,"anchor");
            Scribe_Values.Look(ref generation,"generation");
            Scribe_Values.Look(ref lastPracticeGeneration,"lastPracticeGeneration",-1);
            Scribe_Values.Look(ref diverseSets,"diverseSets");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                borrowed ??= new List<string>(); practiced ??= new List<string>();
                if (!Contains(anchor)) anchor = null;
            }
        }
    }

    internal static class ChaosDiscipline
    {
        internal static DisciplineProgress Path(Pawn pawn) => ClassIdentity.TryGetState(pawn, out var s)
            ? s.Find("rwom.chaos") : null;
        internal static string Id(MagicPower power) => power?.TMabilityDefs?.FirstOrDefault() is TMAbilityDef def
            ? SpellIds.Of(def) : null;

        internal static List<MagicPower> Active(CompAbilityUserMagic comp, DisciplineProgress path)
        {
            var result = new List<MagicPower>();
            if (path.Chaos == null || path.Chaos.Borrowed.Count == 0) return result;
            var byId = comp.MagicData.AllMagicPowersForChaosMage.Where(p => p?.learned == true && Id(p) != null)
                .ToLookup(Id);
            foreach (var id in path.Chaos.Borrowed)
                if (byId[id].FirstOrDefault() is MagicPower power) result.Add(power);
            return result;
        }

        internal static bool Deal(CompAbilityUserMagic comp, DisciplineProgress path)
        {
            var ledger = path.EnsureChaos();
            var state = ClassIdentity.Component(comp.Pawn).State;
            var permanent = new HashSet<TMAbilityDef>(ClassSpellAccess.FlagAbilities(comp));
            var pool = comp.MagicData.AllMagicPowersForChaosMage.Where(p => Id(p) != null
                && !p.TMabilityDefs.OfType<TMAbilityDef>().Any(a => permanent.Contains(a) || state.NoviceSpells.Contains(a.defName))
                && state.Mastery(Id(p)) == null && (!p.learned || ledger.Contains(Id(p))))
                .GroupBy(Id).Select(g => g.First()).ToList();
            if (pool.Count == 0) return false;
            int versatility = comp.MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault(s => s.label == "TM_ChaosTradition_ver")?.level ?? 0;
            int count = 2 + System.Math.Min(2, (int)path.Stage - 1) + UnityEngine.Mathf.Clamp(versatility, 0, 3);
            var next = new List<MagicPower>();
            var kept = ledger.Anchor == null ? null : pool.FirstOrDefault(p => p.learned && Id(p) == ledger.Anchor);
            if (kept != null) { next.Add(kept); pool.Remove(kept); }
            while (next.Count < count && pool.Count > 0)
            {
                int index = Rand.Range(0, pool.Count);
                next.Add(pool[index]); pool.RemoveAt(index);
            }
            TM_Action.ClearSustainedMagicHediffs(comp);
            TM_ChaosProgression.ReleasePowers(comp);
            ledger.Dealt(next.Select(Id));
            foreach (var power in next)
            {
                power.learned = true;
                TM_ChaosProgression.RestorePower(comp, power);
                GrimoireKnowledge.SyncEntry(comp.Pawn, power.abilityDef, power.level, true);
            }
            comp.InitializeSpell();
            return true;
        }

        internal static void RestoreView(CompAbilityUserMagic comp, DisciplineProgress path)
        {
            var active = Active(comp,path);
            if (comp.chaosPowers != null && comp.chaosPowers.Select(p => p.Ability).SequenceEqual(active.Select(p => p.abilityDef))) return;
            comp.chaosPowers = active.Select(p => new TM_ChaosPowers((TMAbilityDef)p.abilityDef,
                TM_ClassUtility.GetAssociatedMagicPowerSkill(comp,p))).ToList();
        }

        internal static void RecordUse(MagicAbility ability)
        {
            var pawn = ability.Pawn;
            if (pawn?.Spawned != true || Path(pawn) is not DisciplineProgress path || path.Chaos == null) return;
            var magic = ability.MagicUser;
            if (!magic.AbilityData.Powers.Any(a => ReferenceEquals(a,ability))) return;
            var power = Active(magic,path).FirstOrDefault(p => p.TMabilityDefs.Contains(ability.Def));
            if (power != null) path.Chaos.Used(path,Id(power));
        }
    }
}
