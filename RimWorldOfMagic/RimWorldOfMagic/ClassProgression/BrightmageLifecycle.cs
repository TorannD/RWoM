using Verse;
using RimWorld;

namespace TorannMagic.ClassProgression
{
    public static class BrightmageLifecycle
    {
        internal static FlyingObject_SpiritOfLight GuardFor(Pawn body)
        {
            var local = body.GetCompAbilityUserMagic()?.SoL;
            if (local?.Spawned == true && local.Map == body.Map) return local;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body,out var soul)) return null;
            var spirit = soul.GetCompAbilityUserMagic()?.SoL;
            return spirit?.Spawned == true && spirit.Map == body.Map && spirit.OwnerPawn == soul ? spirit : null;
        }
        internal static void TickHostedLight(Pawn soul, int now)
        {
            if (!SpiritRebirthState.TryGet(soul, out var state) || !state.HasValidHost(soul)
                || state.DepletionPending || !state.Host.Spawned) return;
            var def = TorannMagicDefOf.TM_LightCapacitanceHD;
            if (def == null || soul.health?.hediffSet == null) return;
            var resource = soul.health.hediffSet.GetFirstHediffOfDef(def);
            if (resource == null && ClassIdentity.IsBrightmage(soul) &&
                (!ClassIdentity.TryGetState(soul, out _) || ClassIdentity.Profile("rwom.brightmage")?.domain == DisciplineDomain.Magic))
                resource = soul.health.AddHediff(def);
            resource?.TryGetComp<HediffComp_LightCapacitance>()?.TickCharge(now);
            var comp = soul.GetCompAbilityUserMagic();
            if (comp?.SoL != null || comp?.spell_EqualizeLight == true) SynchronizeControl(comp);
        }

        // True means new-model maintenance handled this pawn; classic paths remain native.
        public static bool Restore(CompAbilityUserMagic comp)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(comp?.Pawn)) return true;
            if (comp?.Pawn == null || !ClassIdentity.TryGetState(comp.Pawn, out var state)) return false;
            var path = state.Find("rwom.brightmage");
            if ((state.Vocation & Vocation.Mage) != 0 && path?.Domain == DisciplineDomain.Magic
                && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended
                && ClassIdentity.Profile("rwom.brightmage")?.domain == DisciplineDomain.Magic
                && TorannMagicDefOf.TM_LightCapacitanceHD != null
                && comp.Pawn.health?.hediffSet != null
                && !comp.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD))
            {
                HealthUtility.AdjustSeverity(comp.Pawn, TorannMagicDefOf.TM_LightCapacitanceHD, 1f);
            }

            SynchronizeControl(comp);
            return true;
        }

        internal static void SynchronizeControl(CompAbilityUserMagic comp)
        {
            // A saved entity owns this control even after class changes or missing profiles.
            // A despawned entity may be in transit; only destroyed entities are discarded.
            if (comp.SoL != null && comp.SoL.Destroyed) comp.SoL = null;
            if (comp.SoL != null)
            {
                TM_ClassState.AddRuntimeAbility(comp, TorannMagicDefOf.TM_SoL_Equalize);
                comp.spell_EqualizeLight = true;
            }
            else
            {
                comp.RemovePawnAbility(TorannMagicDefOf.TM_SoL_Equalize);
                comp.spell_EqualizeLight = false;
            }
        }
    }
}
