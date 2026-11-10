using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class GolemancerLifecycle
    {
        public static MagicPower GetLearnedNativeOwner(CompAbilityUserMagic comp, TMAbilityDef ability)
        {
            if (comp?.Pawn == null || ability == null) return null;
            var owner = ClassIdentity.TryGetState(comp.Pawn, out _)
                ? comp.MagicData.MagicPowersGolemancer.FirstOrDefault(p => p.TMabilityDefs.Contains(ability))
                : comp.MagicData.ReturnMatchingMagicPower(ability);
            return owner?.learned == true ? owner : null;
        }

        internal static Pawn MasterBody(Pawn master)
        {
            if (master.DestroyedOrNull() || master.Dead) return null;
            if (!SpiritRebirthState.TryGet(master, out var state)) return master;
            return !state.DepletionPending && state.HasValidHost(master) ? state.Host : null;
        }

        internal static bool KnowsGolemancy(Pawn owner)
        {
            var magic = owner?.GetCompAbilityUserMagic();
            return magic?.ExistingMagicData != null && GetLearnedNativeOwner(magic, TorannMagicDefOf.TM_Golemancy) != null;
        }

        internal static IEnumerable<Pawn> Masters(IEnumerable<Pawn> candidates, Faction faction)
        {
            if (faction == null) yield break;
            foreach (var body in candidates)
            {
                if (body.Faction != faction || body.Destroyed || body.Dead) continue;
                if (ReferenceEquals(MasterBody(body), body) && KnowsGolemancy(body)) yield return body;
                if (SpiritRebirthState.TryGetPossessingSpirit(body, out var soul) && soul.Faction == faction &&
                    ReferenceEquals(MasterBody(soul), body) && KnowsGolemancy(soul)) yield return soul;
            }
        }

        internal static CompAbilityUserMagic RuneOwner(Pawn surgeon)
        {
            var local = surgeon?.GetCompAbilityUserMagic();
            bool Learned(CompAbilityUserMagic magic) => magic?.ExistingMagicData != null &&
                GetLearnedNativeOwner(magic, TorannMagicDefOf.TM_RuneCarving) != null;
            if (Learned(local)) return local;
            if (SpiritRebirthState.TryGetPossessingSpirit(surgeon, out var soul) &&
                SpiritRebirthState.TryGet(soul, out var state) && !state.DepletionPending)
            {
                var projected = soul.GetCompAbilityUserMagic();
                if (Learned(projected)) return projected;
            }
            return null;
        }

        internal static float RuneCost(CompAbilityUserMagic magic)
        {
            var spell = TorannMagicDefOf.TM_RuneCarving;
            var skill = magic.MagicData.MagicPowerSkill_RuneCarving.First(x => x.label == "TM_RuneCarving_eff");
            return spell.manaCost - skill.level * spell.efficiencyReductionPercent * spell.manaCost;
        }

        internal static int AlignBrandPairs(CompAbilityUserMagic comp)
        {
            var pawns = comp.BrandPawns;
            var defs = comp.BrandDefs;
            int paired = System.Math.Min(pawns.Count, defs.Count);
            if (pawns.Count > paired) pawns.RemoveRange(paired, pawns.Count - paired);
            if (defs.Count > paired) defs.RemoveRange(paired, defs.Count - paired);
            return paired;
        }

        internal static void RemoveBrandPair(CompAbilityUserMagic comp, Pawn pawn, HediffDef def)
        {
            if (comp == null) return;
            for (int i = AlignBrandPairs(comp) - 1; i >= 0; i--)
            {
                if (comp.BrandPawns[i] != pawn || comp.BrandDefs[i] != def) continue;
                comp.BrandPawns.RemoveAt(i);
                comp.BrandDefs.RemoveAt(i);
            }
        }

        internal static void SyncBrandControl(CompAbilityUserMagic comp)
        {
            var ability = TorannMagicDefOf.TM_DispelBranding;
            if (comp == null || ability == null) return;
            if (comp.BrandPawns.Count > 0) TM_ClassState.AddRuntimeAbility(comp, ability);
            else if (comp.AbilityData.Powers.Any(p => p?.Def == ability)) comp.RemovePawnAbility(ability);
        }

        internal static void SyncWallControl(CompAbilityUserMagic comp)
        {
            if (comp == null) return;
            if (comp.livingWall.DestroyedOrNull()) comp.livingWall = null;
            SetControl(comp, TorannMagicDefOf.TM_DispelLivingWall, comp.livingWall != null && comp.livingWall.CasterPawn == comp.Pawn);
        }

        public static bool Restore(CompAbilityUserMagic comp)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(comp?.Pawn)) return true;
            if (comp?.Pawn == null || !ClassIdentity.TryGetState(comp.Pawn, out _)) return false;
            var pawns = comp.BrandPawns;
            var defs = comp.BrandDefs;
            // Keep native pairs together. Never infer a missing association or alter
            // another caster's hediff when a saved pair is stale after transfer.
            int paired = AlignBrandPairs(comp);
            for (int i = paired - 1; i >= 0; i--)
            {
                var pawn = pawns[i];
                var source = defs[i] == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(defs[i])?.TryGetComp<HediffComp_BrandingBase>();
                if (pawn == null || pawn.Destroyed || pawn.Dead || defs[i] == null || source?.BranderPawn != comp.Pawn)
                { pawns.RemoveAt(i); defs.RemoveAt(i); }
            }
            SetControl(comp, TorannMagicDefOf.TM_DispelBranding, pawns.Count > 0);
            SyncWallControl(comp);
            return true;
        }

        static void SetControl(CompAbilityUserMagic comp, TMAbilityDef ability, bool active)
        {
            if (ability == null) return;
            if (active) TM_ClassState.AddRuntimeAbility(comp, ability);
            else comp.RemovePawnAbility(ability);
        }
    }
}
