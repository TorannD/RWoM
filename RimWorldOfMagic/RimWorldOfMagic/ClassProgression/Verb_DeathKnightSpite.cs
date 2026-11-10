using Verse;

namespace TorannMagic.ClassProgression
{
    // Recheck hate after warmup: another defensive effect may have spent it.
    public sealed class Verb_DeathKnightSpite : Verb_SB
    {
        protected override bool TryCastShot()
        {
            var owner = Ability?.Pawn ?? CasterPawn;
            if (owner != CasterPawn &&
                (!SpiritRebirthState.TryGetPossessingSpirit(CasterPawn, out var soul) || soul != owner ||
                 !SpiritRebirthState.TryGet(owner, out var state) || state.DepletionPending)) return false;
            if (DeathKnightMartialPath.Path(owner) == null) return base.TryCastShot();
            var hate = TM_Calc.GetHateHediff(owner);
            if (hate == null || hate.Severity < 20f || !base.TryCastShot()) return false;
            hate.Severity = UnityEngine.Mathf.Max(.001f, hate.Severity - 20f);
            return true;
        }
    }
}
