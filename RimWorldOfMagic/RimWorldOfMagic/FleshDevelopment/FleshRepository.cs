using System.Runtime.CompilerServices;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    public static class FleshRepository
    {
        // Pawn identity, rather than load IDs, keeps separate games isolated without retaining pawns.
        private static readonly ConditionalWeakTable<Pawn, FleshProgress> states = new ConditionalWeakTable<Pawn, FleshProgress>();

        public static bool IsRaisedUndead(Pawn pawn)
        {
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            foreach (var hediff in hediffs)
                if (hediff?.def?.defName == "TM_UndeadHD" || hediff?.def?.defName == "TM_UndeadAnimalHD") return true;
            return false;
        }

        public static FleshProgress TryGet(Pawn pawn) => pawn != null && states.TryGetValue(pawn, out FleshProgress state) ? state : null;

        public static FleshProgress Ensure(Pawn pawn)
        {
            var existing = TryGet(pawn);
            if (existing != null || !IsRaisedUndead(pawn)) return existing;
            var state = new FleshProgress { LastTick = Current.Game == null ? 0 : Find.TickManager?.TicksGame ?? 0 };
            states.Add(pawn, state);
            return state;
        }

        public static Pawn Owner(Pawn pawn)
        {
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return null;
            foreach (var hediff in hediffs)
                if (hediff?.def?.defName == "TM_UndeadHD" || hediff?.def?.defName == "TM_UndeadAnimalHD")
                    return hediff.TryGetComp<HediffComp_Undead>()?.linkedPawn;
            return null;
        }

        public static void Expose(Pawn pawn)
        {
            if (pawn == null) return;
            FleshProgress state = TryGet(pawn);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Loading the same object again must not retain a previous save's cached state.
                states.Remove(pawn);
                state = null;
            }
            Scribe_Deep.Look(ref state, "TM_FleshDevelopment");
            if (Scribe.mode == LoadSaveMode.LoadingVars && state != null) states.Add(pawn, state);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && state == null) Ensure(pawn);
        }
    }
}
