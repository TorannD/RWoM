using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class GolemancerBrands
    {
        private static readonly string[] Names = { "TM_AwarenessBrand", "TM_EmotionBrand", "TM_FitnessBrand", "TM_ProtectionBrand", "TM_SiphonBrand", "TM_VitalityBrand" };
        public static bool IsBrand(MagicPower power) => power?.TMabilityDefs?.Any(a => a != null && Names.Contains(a.defName)) == true;
        public static bool IsBrandScroll(ThingDef scroll) => scroll != null && new[] { "SpellOf_BrandAwareness", "SpellOf_BrandEmotion", "SpellOf_BrandFitness", "SpellOf_BrandProtection", "SpellOf_BrandSiphon", "SpellOf_BrandVitality" }.Contains(scroll.defName);

        // These stock skill-less XML powers belong to CustomStandalone, not the
        // six main native families and not an arbitrary overlapping addon owner.
        public static List<MagicPower> Owners(MagicData data)
        {
            if (data == null) return null;
            foreach (string name in Names)
            {
                var def = DefDatabase<TM_CustomPowerDef>.GetNamedSilentFail(name);
                var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
                var spec = def?.customPower;
                if (ability == null || spec?.forMage != true || spec.abilityDefs?.Count != 1
                    || spec.abilityDefs[0] != ability || spec.skills?.Any(s => s != null) == true) return null;
            }
            var initialize = data.MagicPowersCustom;
            var owners = new List<MagicPower>();
            foreach (string name in Names)
            {
                var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
                var matches = data.MagicPowersCustomStandalone.Where(p => p?.TMabilityDefs?.Count == 1 && p.TMabilityDefs[0] == ability).ToList();
                if (matches.Count != 1) return null;
                owners.Add(matches[0]);
            }
            return owners;
        }

        public static MagicPower ScrollOwner(MagicData data, ThingDef scroll)
            => IsBrandScroll(scroll) ? Owners(data)?.SingleOrDefault(p => (p.TMabilityDefs[0] as TMAbilityDef)?.learnItem == scroll) : null;

        public static bool CanLearn(Pawn pawn, MagicPower power)
        {
            var comp = pawn?.GetCompAbilityUserMagic();
            if (comp == null || !ClassIdentity.TryGetState(pawn, out var state)
                || !ClassIdentity.IsGolemancer(pawn) || ClassIdentity.Profile("rwom.golemancer")?.domain != DisciplineDomain.Magic) return false;
            var path = state.Find("rwom.golemancer");
            return path.Stage >= InitiationStage.Adept && path.Stage <= InitiationStage.Ascended
                && comp.MagicData.MagicPowersGolemancer.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Branding))
                && Owners(comp.MagicData)?.Any(p => ReferenceEquals(p, power)) == true;
        }

        public static bool CanPrepare(CompAbilityUserMagic comp, DisciplineProgress path)
            => comp != null && path != null && path.ClassId == "rwom.golemancer" && path.Domain == DisciplineDomain.Magic
                && (path.GolemancerBrandsGranted || comp.MagicData.MagicPowersGolemancer.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Branding)) || Owners(comp.MagicData) != null);

        internal static List<MagicPower> Prepare(CompAbilityUserMagic comp, DisciplineProgress path)
        {
            if (!CanPrepare(comp, path)) return null;
            // An inherited learned Branding is not a new acquisition. Never use
            // missing old latch data as permission to reroll those existing saves.
            if (path.GolemancerBrandsGranted || comp.MagicData.MagicPowersGolemancer.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Branding))) return new List<MagicPower>();
            var available = Owners(comp.MagicData).Where(p => !p.learned).ToList();
            var chosen = new List<MagicPower>();
            while (available.Count > 0 && chosen.Count < 2)
            {
                var selected = available.RandomElement();
                chosen.Add(selected); available.Remove(selected);
            }
            return chosen;
        }

        internal static void Commit(DisciplineProgress path, List<MagicPower> chosen)
        {
            if (chosen == null || path.GolemancerBrandsGranted) return;
            path.MarkGolemancerBrandsGranted();
            foreach (var owner in chosen) owner.learned = true;
        }
    }
}
