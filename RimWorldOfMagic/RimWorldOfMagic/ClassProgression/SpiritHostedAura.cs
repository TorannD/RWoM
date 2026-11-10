using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedAura
    {
        private static bool Managed(TMAbilityDef def) => def != null && def.abilityHediff != null &&
            (def.abilityHediff == TorannMagicDefOf.TM_RayOfHope_AuraHD || def.abilityHediff == TorannMagicDefOf.TM_InnerFire_AuraHD ||
             def.abilityHediff == TorannMagicDefOf.TM_Shadow_AuraHD || def.abilityHediff == TorannMagicDefOf.TM_SoothingBreeze_AuraHD);

        internal static bool HasLocalEquivalent(Pawn host, TMAbilityDef def)
        {
            if (!Managed(def)) return false;
            bool Matches(AbilityUser.PawnAbility power) => power?.Def is TMAbilityDef native && native.abilityHediff == def.abilityHediff;
            return host?.GetCompAbilityUserMagic()?.AbilityData.AllPowers.Any(Matches) == true ||
                host?.GetCompAbilityUserMight()?.AbilityData.AllPowers.Any(Matches) == true;
        }

        internal static bool CanMaintain(Pawn host, Pawn soul, TMAbilityDef def)
        {
            if (!Managed(def) || host?.health?.hediffSet == null || SpiritHostedBolt.Find(host, soul, def) == null) return false;
            // The host's own aura continues to own its effects, even when the base spell IDs differ.
            foreach (var h in host.health.hediffSet.hediffs)
                if (h.def == TorannMagicDefOf.TM_InnerFire_AuraHD || h.def == TorannMagicDefOf.TM_RayOfHope_AuraHD ||
                    h.def == TorannMagicDefOf.TM_Shadow_AuraHD || h.def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD) return false;
            return true;
        }

        internal static bool CanToggle(Pawn host, Pawn soul, TMAbilityDef def)
        {
            if (!CanMaintain(host, soul, def) || soul.health?.hediffSet == null ||
                def.abilityHediff.comps?.Any(c => c.compClass == typeof(HediffComp_Aura)) != true) return false;
            var hediffs = soul.health.hediffSet;
            bool togglingOff = hediffs.HasHediff(TorannMagicDefOf.TM_RayOfHope_AuraHD) || hediffs.HasHediff(TorannMagicDefOf.TM_InnerFire_AuraHD)
                || hediffs.HasHediff(TorannMagicDefOf.TM_Shadow_AuraHD) || hediffs.HasHediff(TorannMagicDefOf.TM_SoothingBreeze_AuraHD);
            return togglingOff || soul.GetCompAbilityUserMagic().maxMP >= def.upkeepEnergyCost;
        }

        internal static void TickAuras(Pawn soul)
        {
            SpiritHostedCommander.VisitAuras(soul,false);
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                EmpathLifecycle.UpdateProjectedEmpathy(soul,false);
                DemonSoulBond.UpdateProjection(soul,false);
            }
            Visit(soul, TorannMagicDefOf.TM_RayOfHope_AuraHD, false);
            Visit(soul, TorannMagicDefOf.TM_InnerFire_AuraHD, false);
            Visit(soul, TorannMagicDefOf.TM_Shadow_AuraHD, false);
            Visit(soul, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, false);
        }

        internal static void EndAuras(Pawn soul)
        {
            SpiritHostedCommander.VisitAuras(soul,true);
            EmpathLifecycle.UpdateProjectedEmpathy(soul, true);
            DemonSoulBond.UpdateProjection(soul,true);
            if (TorannMagicDefOf.TM_SuppressiveAuraHD != null && SpiritRebirthState.TryGet(soul,out var state))
            {
                var body = state.Host;
                if (TorannMagicDefOf.TM_OutOfBodyHD != null &&
                    body?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD)?.TryGetComp<HediffComp_SymbiosisCaster>()?.originalId == soul.thingIDNumber)
                    TM_Action.RemoveSymbiosisCommand(body);
                var aura = body?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_SuppressiveAuraHD);
                if (aura?.TryGetComp<HediffComp_SuppressiveAura>()?.SourceId == soul.thingIDNumber)
                    body.health.RemoveHediff(aura);
            }
            Visit(soul, TorannMagicDefOf.TM_RayOfHope_AuraHD, true);
            Visit(soul, TorannMagicDefOf.TM_InnerFire_AuraHD, true);
            Visit(soul, TorannMagicDefOf.TM_Shadow_AuraHD, true);
            Visit(soul, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, true);
        }

        private static void Visit(Pawn soul, HediffDef def, bool remove)
        {
            var hediff = def == null ? null : soul?.health?.hediffSet?.GetFirstHediffOfDef(def);
            var aura = hediff?.TryGetComp<HediffComp_Aura>();
            if (aura?.Projected != true) return;
            if (remove) aura.EndProjection();
            else aura.TickHosted();
        }
    }
}
