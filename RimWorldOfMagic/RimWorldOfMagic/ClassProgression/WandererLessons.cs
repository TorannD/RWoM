using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class WandererLessons : IExposable
    {
        private List<string> offer = new List<string>();
        private List<string> learned = new List<string>();
        private int remaining;
        internal IReadOnlyList<string> Offer => offer;
        internal IReadOnlyList<string> Learned => learned;
        internal int Remaining => remaining;
        public void ExposeData()
        {
            Scribe_Collections.Look(ref offer, "offer", LookMode.Value);
            Scribe_Collections.Look(ref learned, "learned", LookMode.Value);
            Scribe_Values.Look(ref remaining, "remaining");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                offer ??= new List<string>();
                learned ??= new List<string>();
            }
        }

        internal static DisciplineProgress Path(Pawn pawn) => ClassIdentity.TryGetState(pawn, out var state)
            ? state.Find("rwom.wanderer") : null;

        // Only actively castable base families designed to work across classes. Do not deal passive
        // identity records, higher ranks, or summons which require a different class's private lifecycle.
        private static IEnumerable<string> Candidates(Pawn pawn, DisciplineProgress path)
        {
            var known = new HashSet<string>(path.Lessons?.Learned ?? System.Array.Empty<string>());
            var pool = new HashSet<string>();
            if (((int)path.Domain & (int)Vocation.Mage) != 0)
            {
                var comp = pawn.GetCompAbilityUserMagic();
                foreach (var p in comp.MagicData.AllMagicPowersWithSkills)
                {
                    if (!(p?.TMabilityDefs?.FirstOrDefault() is TMAbilityDef d)) continue;
                    var id = "M/" + d.defName;
                    if (p.learned) known.Add(id);
                    else if (d.shouldInitialize && d.canCopy) pool.Add(id);
                }
                foreach (var a in comp.AbilityData.Powers)
                    if (a?.Def != null) known.Add("M/" + a.Def.defName);
            }
            if (((int)path.Domain & (int)Vocation.Warrior) != 0)
            {
                var comp = pawn.GetCompAbilityUserMight();
                foreach (var p in comp.MightData.AllMightPowersWithSkills)
                {
                    if (!(p?.TMabilityDefs?.FirstOrDefault() is TMAbilityDef d)) continue;
                    var id = "W/" + d.defName;
                    if (p.learned) known.Add(id);
                    else if (d.shouldInitialize && d.canCopy) pool.Add(id);
                }
                foreach (var a in comp.AbilityData.Powers)
                    if (a?.Def != null) known.Add("W/" + a.Def.defName);
            }
            return pool.Where(id => !known.Contains(id)).OrderBy(id => id, System.StringComparer.Ordinal);
        }

        internal static bool Accepts(Pawn pawn, Thing book)
        {
            var path = Path(pawn);
            if (path == null || !pawn.Spawned || pawn.Dead || pawn.Downed || book?.def == null || book.Destroyed
                || book.stackCount < 1 || !book.def.destroyable || path.Lessons?.Remaining > 0) return false;
            var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading;
            if (path.Stage == InitiationStage.Foundation)
                return path.CompletedTrials.Contains("rwom.wanderer.survey") &&
                    (profiles.Any(d => d.adeptBook == book.def) || (TM_ClassUtility.CustomClasses?.Any(c => c?.fullScript == book.def) ?? false) || book.def == TorannMagicDefOf.BookOfShadow
                        || book.def.HasComp(typeof(CompUseEffect_LearnMight)));
            return path.Stage == InitiationStage.Adept && path.CompletedTrials.Contains("rwom.wanderer.expedition")
                && profiles.Any(d => d.masterBook == book.def);
        }

        internal static bool Read(Pawn pawn, Thing book)
        {
            if (!Accepts(pawn, book)) return false;
            if (book.Spawned ? book.Map != pawn.Map || !pawn.Position.InHorDistOf(book.Position, 1.5f)
                : book.ParentHolder == null || (book.ParentHolder != pawn.inventory && book.ParentHolder != pawn.carryTracker)) return false;
            var path = Path(pawn);
            var pool = Candidates(pawn, path).ToList();
            var next = new List<string>(6);
            if (path.Domain == DisciplineDomain.WarriorMage)
            {
                foreach (var prefix in new[] { "M/", "W/" })
                {
                    var domain = pool.Where(id => id.StartsWith(prefix)).ToList();
                    if (domain.Count == 0) continue;
                    var skill = domain[Rand.Range(0, domain.Count)];
                    next.Add(skill);
                    pool.Remove(skill);
                }
            }
            while (next.Count < 6 && pool.Count > 0)
            {
                int i = Rand.Range(0, pool.Count);
                next.Add(pool[i]);
                pool.RemoveAt(i);
            }
            var holder = book.holdingOwner;
            var payment = book.SplitOff(1);
            try { payment.Destroy(DestroyMode.Vanish); }
            catch { if (!payment.Destroyed) { ClassInitiation.RestoreUnpaidBook(pawn, payment, holder); return false; } }
            if (!payment.Destroyed) { ClassInitiation.RestoreUnpaidBook(pawn, payment, holder); return false; }
            var trial = path.Stage == InitiationStage.Foundation ? "rwom.wanderer.survey" : "rwom.wanderer.expedition";
            var state = ClassIdentity.Component(pawn).State;
            if (!state.TryAdvanceAfterTrial(path.ClassId, path.Stage, trial)) throw new System.InvalidOperationException("Wanderer stage changed during payment.");
            var lessons = path.EnsureLessons();
            lessons.offer = next;
            lessons.remaining = System.Math.Min(2, next.Count);
            return true;
        }

        internal static IEnumerable<MightPower> MartialPicks(CompAbilityUserMight comp, DisciplineProgress path)
        {
            if (path?.Lessons == null || comp?.ExistingMightData == null) yield break;
            foreach (var id in path.Lessons.Learned)
            {
                if (!id.StartsWith("W/")) continue;
                var def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(id.Substring(2));
                var owner = def == null ? null : comp.ExistingMightData.AllMightPowersWithSkills.FirstOrDefault(p => p?.TMabilityDefs?.FirstOrDefault() == def);
                if (owner?.learned == true) yield return owner;
            }
        }

        internal void ImportMartialPicks(IEnumerable<MightPower> owned)
        {
            foreach (var power in owned)
            {
                var root = power?.TMabilityDefs?.FirstOrDefault();
                if (power?.learned != true || root?.defName == null) continue;
                var id = "W/" + root.defName;
                if (!learned.Contains(id)) learned.Add(id);
            }
        }

        internal static bool Pick(Pawn pawn, string id)
        {
            var lessons = Path(pawn)?.Lessons;
            if (lessons == null || lessons.remaining <= 0 || !lessons.offer.Contains(id) || lessons.learned.Contains(id)) return false;
            var def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(id.Substring(2));
            if (def == null) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            var might = pawn.GetCompAbilityUserMight();
            if (id.StartsWith("M/"))
            {
                var p = magic?.MagicData?.AllMagicPowersWithSkills.FirstOrDefault(x => x?.TMabilityDefs?.FirstOrDefault() == def);
                if (p == null) return false;
                p.learned = true;
            }
            else
            {
                var p = might?.MightData?.AllMightPowersWithSkills.FirstOrDefault(x => x?.TMabilityDefs?.FirstOrDefault() == def);
                if (p == null) return false;
                p.learned = true;
            }
            lessons.learned.Add(id);
            lessons.offer.Remove(id);
            if (--lessons.remaining == 0) lessons.offer.Clear();
            TM_ClassState.AddRuntimeAbility(id.StartsWith("M/") ? (AbilityUser.CompAbilityUser)magic : might, def);
            GrimoireKnowledge.SyncEntry(pawn, def, 0, true);
            return true;
        }
    }
}
