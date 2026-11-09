using RimWorld;
using Verse;
namespace TorannMagic.ClassProgression
{
    // All three new books use explicit progression, never legacy random class grants.
    public sealed class CompUseEffect_EmpathBook : CompUseEffect
    {
        private bool Eligible(Pawn pawn)
            => WandererLessons.Accepts(pawn, parent) || (parent?.def?.defName == "Torn_BookOfEmpathy" ? ClassInitiation.CanUseFoundation(pawn, parent, "rwom.empath", out _)
                : parent?.def?.defName == "BookOfEmpathy" ? ClassInitiation.CanUseAdept(pawn, parent, out _)
                : parent?.def?.defName == "TM_MasterTomeEmpath" && ClassInitiation.CanUseMaster(pawn, parent, out _));
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
            => Eligible(pawn) ? (AcceptanceReport)true : (AcceptanceReport)"TM_ProgressionBookUnavailable".Translate();
        public override void DoEffect(Pawn pawn)
        {
            if (!Eligible(pawn) || !ClassInitiation.CompleteBook(pawn, parent))
                Messages.Message("TM_ProgressionBookUnavailable".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
        }
    }
}
