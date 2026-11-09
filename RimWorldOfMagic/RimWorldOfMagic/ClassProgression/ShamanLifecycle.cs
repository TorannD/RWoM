using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ShamanLifecycle
    {
        internal static bool OwnsGuardian(CompAbilityUserMagic comp)
        {
            var guardian = comp?.bondedSpirit;
            if (guardian == null || guardian.Destroyed || guardian.Dead || guardian.Faction != comp.Pawn.Faction) return false;
            var controller = guardian.TryGetComp<CompAnimalController>();
            var spawner = (guardian as TMPawnSummoned)?.Spawner;
            if (controller == null || (controller.summonerPawn != null && controller.summonerPawn != comp.Pawn) ||
                (spawner != null && spawner != comp.Pawn)) return false;
            return spawner == comp.Pawn || controller.summonerPawn == comp.Pawn ||
                (comp.guardianSpiritType != null && guardian.def == comp.guardianSpiritType);
        }

        internal static bool ReleaseGuardian(CompAbilityUserMagic comp)
        {
            if (!OwnsGuardian(comp)) return false;
            var guardian = comp.bondedSpirit;
            var cargo = guardian.inventory?.innerContainer;
            if (!guardian.Spawned && (guardian.carryTracker?.CarriedThing != null || cargo?.Count > 0)) return false;
            if (!SpiritSummonedHost.CanDismiss(guardian)) return false;
            if (guardian.carryTracker?.CarriedThing != null &&
                !guardian.carryTracker.TryDropCarriedThing(guardian.Position,ThingPlaceMode.Near,out _)) return false;
            while (cargo?.Count > 0)
                if (!cargo.TryDrop(cargo[0],guardian.Position,guardian.Map,ThingPlaceMode.Near,out _)) return false;
            if (guardian is TMPawnSummoned summoned) summoned.PreDestroy();
            guardian.Destroy();
            if (!guardian.Destroyed) return false;
            if (comp.bondedSpirit == guardian) comp.bondedSpirit = null;
            comp.SyncGuardianControl();
            return true;
        }

        private static float CrowStrength(CompAbilityUserMagic owner)
        {
            if (owner?.bondedSpirit?.def != TorannMagicDefOf.TM_SpiritCrowR || !OwnsGuardian(owner)) return 0f;
            return .5f + SpiritRebirthRuntime.SavedRank(owner.ExistingMagicData,"TM_GuardianSpirit_pwr");
        }

        internal static float CrowInsight(Pawn body)
        {
            if (body?.health?.hediffSet == null || SpiritRebirthEnergy.Marked(body)) return 0f;
            float strength = CrowStrength(body.GetCompAbilityUserMagic());
            if (SpiritRebirthState.TryGetPossessingSpirit(body,out var soul))
                strength = System.Math.Max(strength,CrowStrength(soul.GetCompAbilityUserMagic()));
            return strength;
        }

        internal static void ApplyCrowInsight(CompAbilityUserMagic owner)
        {
            var body = owner.Pawn;
            if (SpiritRebirthState.TryGet(body,out var state))
            {
                if (!state.HasValidHost(body) || state.DepletionPending) return;
                body = state.Host;
            }
            float strength = CrowInsight(body);
            if (strength <= 0f) return;
            var mark = body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_CrowInsightHD)
                ?? body.health.AddHediff(TorannMagicDefOf.TM_CrowInsightHD);
            if (mark != null && mark.Severity != strength) mark.Severity = strength;
        }

        internal static void GrantControls(CompAbilityUserMagic comp)
        {
            TM_ClassState.AddRuntimeAbility(comp,TorannMagicDefOf.TM_Hex_Pain);
            TM_ClassState.AddRuntimeAbility(comp,TorannMagicDefOf.TM_Hex_MentalAssault);
            TM_ClassState.AddRuntimeAbility(comp,TorannMagicDefOf.TM_Hex_CriticalFail);
        }

        internal static bool Control(AbilityUser.AbilityDef def) => def != null &&
            (def == TorannMagicDefOf.TM_Hex_Pain || def == TorannMagicDefOf.TM_Hex_MentalAssault || def == TorannMagicDefOf.TM_Hex_CriticalFail);

        public static bool Restore(CompAbilityUserMagic comp)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(comp?.Pawn) &&
                (!SpiritRebirthState.TryGet(comp.Pawn,out var soul) || !soul.HasValidHost(comp.Pawn) || soul.DepletionPending)) return true;
            if (comp?.Pawn == null || !ClassIdentity.TryGetState(comp.Pawn, out _)) return false;
            var controls = new[] { TorannMagicDefOf.TM_Hex_Pain, TorannMagicDefOf.TM_Hex_MentalAssault, TorannMagicDefOf.TM_Hex_CriticalFail };
            var owner = comp.MagicData.MagicPowersShaman.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Hex));
            // Native copied/item Hex can create these controls without permanent
            // ownership. Preserve that runtime proof while its actual targets remain.
            bool hasRuntimeSource = comp.AbilityData.Powers.Any(p => p != null
                && (controls.Contains(p.Def) || owner?.TMabilityDefs.Contains(p.Def) == true));
            bool active = comp.HexedPawns.Count > 0 && (owner?.learned == true || hasRuntimeSource);
            foreach (var control in controls)
            {
                if (control == null) continue;
                if (active) TM_ClassState.AddRuntimeAbility(comp, control);
                else comp.RemovePawnAbility(control);
            }
            return true;
        }
    }

    public class HediffComp_CrowInsight : HediffComp
    {
        public override void CompPostTick(ref float adjustment)
        {
            if (!Pawn.IsHashIntervalTick(60)) return;
            float strength = ShamanLifecycle.CrowInsight(Pawn);
            if (strength == 0f) Pawn.health.RemoveHediff(parent);
            else if (parent.Severity != strength) parent.Severity = strength;
        }
    }
}
