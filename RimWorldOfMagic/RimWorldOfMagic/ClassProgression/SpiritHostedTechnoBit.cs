using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedTechnoBit
    {
        private static bool Learned(CompAbilityUserMagic magic) => magic?.LearnedTechnomancerAbility(TorannMagicDefOf.TM_TechnoBit) == true;

        internal static bool Available(Pawn body, CompAbilityUserMagic owner)
        {
            if (!Learned(owner) || body?.Spawned!=true || body.Dead || body.Downed || body.InMentalState ||
                !SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) || soul!=owner.Pawn ||
                soul.GetComp<CompClassProgression>().SpiritRebirth.DepletionPending) return false;
            var local=body.GetCompAbilityUserMagic();
            return !Learned(local) && !body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoBitHD);
        }

        internal static void Tick(CompAbilityUserMagic owner)
        {
            if (!SpiritRebirthState.TryGet(owner.Pawn,out var state) || !Available(state.Host,owner)) return;
            var body=state.Host;
            var set=owner.Pawn.health.hediffSet;
            var bit=set.GetFirstHediffOfDef(TorannMagicDefOf.TM_TechnoBitHD);
            if (bit==null)
            {
                bit=HediffMaker.MakeHediff(TorannMagicDefOf.TM_TechnoBitHD,owner.Pawn);
                owner.Pawn.health.AddHediff(bit);
            }
            var local=body.GetCompAbilityUserMagic();
            if (local!=null) local.drawHostedBit=true;
            bit.TryGetComp<HediffComp_TechnoBit>()?.TickFor(body);
        }
    }
}
