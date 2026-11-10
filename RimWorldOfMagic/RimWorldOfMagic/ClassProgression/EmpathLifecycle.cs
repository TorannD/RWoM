using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class EmpathLifecycle
    {
        private static bool Learned(Pawn owner)
        {
            return ClassIdentity.IsEmpath(owner) && EmpathOwners.Resolve(owner.GetCompAbilityUserMagic()?.ExistingMagicData)
                ?.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Empathy)) == true;
        }

        internal static void UpdateProjectedEmpathy(Pawn soul, bool ending)
        {
            if (TorannMagicDefOf.TM_EmpathHD == null || !SpiritRebirthState.TryGet(soul, out var state)) return;
            var body = state.Host;
            if (body?.health?.hediffSet == null) return;
            var effect = body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmpathHD);
            var empathy = effect?.TryGetComp<HediffComp_Empath>();
            if (effect != null && (empathy == null || empathy.SourceId != soul.thingIDNumber)) return;
            if (effect != null && Learned(body))
            {
                empathy.SourceId = 0;
                return;
            }
            if (ending || state.DepletionPending || !state.HasValidHost(soul) || body.needs?.mood == null || !Learned(soul))
            {
                if (effect != null) body.health.RemoveHediff(effect);
                return;
            }
            if (effect != null || Learned(body)) return;
            var projection = HediffMaker.MakeHediff(TorannMagicDefOf.TM_EmpathHD, body);
            var source = projection.TryGetComp<HediffComp_Empath>();
            if (source == null) return;
            source.SourceId = soul.thingIDNumber;
            body.health.AddHediff(projection);
        }

        public static bool Restore(CompAbilityUserMagic comp)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(comp?.Pawn)) return true;
            if (comp?.Pawn == null || !ClassIdentity.TryGetState(comp.Pawn, out var state)) return false;
            var path = state.Find("rwom.empath");
            if ((state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended
                && ClassIdentity.Profile("rwom.empath")?.domain == DisciplineDomain.Magic
                && TorannMagicDefOf.TM_EmpathHD != null && comp.Pawn.health?.hediffSet != null
                && !comp.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmpathHD)
                && EmpathOwners.Resolve(comp.MagicData)?.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Empathy)) == true)
                HealthUtility.AdjustSeverity(comp.Pawn, TorannMagicDefOf.TM_EmpathHD, 1f);
            return true;
        }
    }
}
