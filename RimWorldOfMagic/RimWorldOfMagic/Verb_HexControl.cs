using System.Linq;
using Verse;

namespace TorannMagic
{
    public abstract class Verb_HexControl : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body, Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            if (body?.Spawned != true || currentTarget.Pawn != body || magic == null) return false;
            var targets = magic.HexedPawns.Where(p => p.Spawned).ToArray();
            if (targets.Length == 0) return false;
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner,TorannMagicDefOf.TM_Hex);
            foreach (var target in targets)
                if (target.Spawned && !target.Dead) Apply(target,owner,versatility);
            return true;
        }

        protected abstract void Apply(Pawn target,Pawn owner,int versatility);
    }
}
