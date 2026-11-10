using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Conservative, explicit physical cleanup. Unknown effects require an adapter;
    // never silently discard another mod's identity, links or progression.
    internal sealed class SpiritRebirthHealthPlan
    {
        private readonly Pawn pawn;
        private readonly Hediff[] original;
        private readonly Hediff[] physical;
        private bool attempted;

        private static readonly HashSet<string> PhysicalDefs = new HashSet<string> {
            "BloodLoss", "Anesthetic", "FoodPoisoning", "ToxicBuildup", "DrugOverdose",
            "Malnutrition", "Heatstroke", "Hypothermia", "BadBack", "Frail", "Cataract",
            "Asthma", "Carcinoma", "Cirrhosis", "Alzheimers", "Dementia"
        };
        private static readonly HashSet<string> PhysicalComps = new HashSet<string> {
            "Verse.HediffComp_TendDuration", "Verse.HediffComp_GetsPermanent",
            "Verse.HediffComp_Infecter", "Verse.HediffComp_Immunizable",
            "Verse.HediffComp_SeverityPerDay", "Verse.HediffComp_GrowthMode",
            "Verse.HediffComp_SkillDecay"
        };
        private static readonly HashSet<string> PhysicalImplants = new HashSet<string> {
            "ArchotechEye", "ArchotechArm", "ArchotechLeg", "BionicEye", "BionicArm",
            "BionicLeg", "BionicSpine", "BionicHeart", "BionicStomach", "BionicEar",
            "BionicTongue", "BionicJaw", "PegLeg", "WoodenHand", "WoodenFoot", "Denture",
            "SimpleProstheticLeg", "SimpleProstheticArm", "SimpleProstheticHeart",
            "CochlearImplant", "PowerClaw", "Joywire", "Painstopper"
        };

        private SpiritRebirthHealthPlan(Pawn pawn, Hediff[] original, Hediff[] physical)
        { this.pawn = pawn; this.original = original; this.physical = physical; }

        private static bool CanPrepareBody(Pawn pawn) => pawn?.Dead == true && !pawn.Discarded &&
            (!pawn.Destroyed || pawn.ParentHolder is Corpse corpse && !corpse.Destroyed && corpse.InnerPawn == pawn);

        internal static bool TryPrepare(Pawn pawn, out SpiritRebirthHealthPlan plan, out Hediff unsupported)
        {
            plan = null;
            unsupported = null;
            if (!CanPrepareBody(pawn) || pawn.health?.hediffSet?.hediffs == null) return false;
            var snapshot = pawn.health.hediffSet.hediffs.ToArray();
            var removals = new List<Hediff>();
            foreach (var effect in snapshot)
            {
                if (effect == null || effect.pawn != pawn || effect.def == null) { unsupported = effect; return false; }
                if (IsClassResourceMarker(effect)) continue;
                if (effect.GetType() == typeof(Hediff_ClassDiscipline)
                    && ReferenceEquals(effect.def, DefDatabase<HediffDef>.GetNamedSilentFail("TM_ClassDiscipline"))
                    && effect.def.hediffClass == typeof(Hediff_ClassDiscipline)
                    && (effect.def.stages == null || effect.def.stages.Count == 0)
                    && !effect.def.forceRemoveOnResurrection
                    && effect.def.lethalSeverity <= 0f) continue;
                if (!IsKnownPhysical(effect)) { unsupported = effect; return false; }
                removals.Add(effect);
            }
            plan = new SpiritRebirthHealthPlan(pawn, snapshot, removals.ToArray());
            return true;
        }

        private static readonly Dictionary<string, Type[]> bardSongs = new Dictionary<string, Type[]> {
            { "TM_EntertainingHD", new[] { typeof(HediffComp_SeverityPerDay) } },
            { "TM_InspirationalHD", new[] { typeof(HediffComp_Inspirational), typeof(HediffComp_SeverityPerDay) } },
            { "TM_SingBattleHymnHD", new[] { typeof(HediffComp_SeverityPerDay), typeof(HediffComp_BattleHymn) } }
        };

        private static bool IsClassResourceMarker(Hediff effect)
        {
            var def = effect.def;
            // Preserve the existing class stock and its trained capacity on the
            // original pawn. Do not manufacture a fresh/refilled stock on a host.
            if (def.defName == "TM_ApothecaryHerbsHD")
                return effect.GetType() == typeof(Hediff_ApothecaryHerbs)
                    && def.hediffClass == typeof(Hediff_ApothecaryHerbs)
                    && ReferenceEquals(DefDatabase<HediffDef>.GetNamedSilentFail(def.defName), def)
                    && effect.Part == null && !def.forceRemoveOnResurrection && def.lethalSeverity <= 0f
                    && (def.comps == null || def.comps.Count == 0)
                    && (((HediffWithComps)effect).comps == null || ((HediffWithComps)effect).comps.Count == 0);
            if (effect.GetType() != typeof(HediffWithComps) || def.hediffClass != typeof(HediffWithComps) ||
                def.forceRemoveOnResurrection || def.lethalSeverity > 0f || def.comps == null ||
                !ReferenceEquals(DefDatabase<HediffDef>.GetNamedSilentFail(def.defName), def)) return false;
            if (bardSongs.TryGetValue(def.defName, out var expectedSong))
            {
                var live = ((HediffWithComps)effect).comps;
                return effect.Part == null && live?.Count == expectedSong.Length
                    && def.comps.Select(c => c?.compClass).SequenceEqual(expectedSong)
                    && live.All(c => c?.parent == effect) && live.Select(c => c.GetType()).SequenceEqual(expectedSong);
            }
            if (def.comps.Count != 1) return false;
            var component = def.comps[0]?.compClass;
            if (def.defName == "TM_MagicUserHD" || def.defName == "TM_MightUserHD")
                return component == typeof(HediffComp_SeverityPerDay);
            if (def.defName == "TM_LightCapacitanceHD" || def.defName == "TM_TechnoBitHD")
            {
                var expected = def.defName == "TM_LightCapacitanceHD" ? typeof(HediffComp_LightCapacitance) :
                    typeof(HediffComp_TechnoBit);
                var live = ((HediffWithComps)effect).comps;
                return effect.Part == null && component == expected && live?.Count == 1
                    && live[0]?.GetType() == expected && live[0].parent == effect;
            }
            return def.defName.StartsWith("TM_HediffEnchantment_", StringComparison.Ordinal)
                && component == typeof(Enchantment.HediffComp_Enchantment);
        }

        private static readonly Dictionary<string, Type> physicalClassEffects = new Dictionary<string, Type> {
            { "TM_BloodHD", typeof(HediffComp_Blood) }, { "TM_CrowInsightHD", typeof(HediffComp_CrowInsight) },
            { "TM_SoulBondMentalHD", typeof(HediffComp_SoulBondHost) }, { "TM_SoulBondPhysicalHD", typeof(HediffComp_SoulBondHost) },
            { "TM_WDSoulBondMentalHD", typeof(HediffComp_SoulBond) }, { "TM_SDSoulBondPhysicalHD", typeof(HediffComp_SoulBond) },
            { "TM_EmpathHD", typeof(HediffComp_Empath) }, { "TM_SuppressiveAuraHD", typeof(HediffComp_SuppressiveAura) },
            { "TM_EmotionSuppressionHD", typeof(HediffComp_SeverityPerDay) }, { "TM_HarvestPassionHD", typeof(HediffComp_HarvestPassion) },
            { "TM_OutOfBodyHD", typeof(HediffComp_SymbiosisCaster) }, { "TM_SymbiosisHD", typeof(HediffComp_SymbiosisHost) }
        };

        private static bool IsKnownPhysical(Hediff effect)
        {
            if (physicalClassEffects.TryGetValue(effect.def.defName, out var expected))
            {
                var def = effect.def;
                return ReferenceEquals(DefDatabase<HediffDef>.GetNamedSilentFail(def.defName), def) &&
                    def.hediffClass == typeof(HediffWithComps) && effect.GetType() == def.hediffClass &&
                    effect.Part == null && def.comps?.Count == 1 && def.comps[0]?.compClass == expected &&
                    ((HediffWithComps)effect).comps is { Count: 1 } live && live[0]?.GetType() == expected &&
                    live[0].parent == effect;
            }
            if (effect.def == TorannMagicDefOf.TM_LichHD && SpiritNecromancyRenunciation.Required(effect.pawn))
                return effect.GetType() == typeof(HediffWithComps) && ((HediffWithComps)effect).comps?.All(c =>
                    c is HediffComp_Lich || c is HediffComp_SeverityPerDay) == true;
            var type = effect.GetType();
            bool structural = type == typeof(Hediff_Injury) || type == typeof(Hediff_MissingPart)
                || ((type == typeof(Hediff_AddedPart) || type == typeof(Hediff_Implant))
                    && PhysicalImplants.Contains(effect.def.defName)
                    && ReferenceEquals(DefDatabase<HediffDef>.GetNamedSilentFail(effect.def.defName), effect.def));
            if (!structural && !((type == typeof(Hediff) || type == typeof(HediffWithComps))
                && PhysicalDefs.Contains(effect.def.defName)
                && ReferenceEquals(DefDatabase<HediffDef>.GetNamedSilentFail(effect.def.defName), effect.def))) return false;
            var comps = (effect as HediffWithComps)?.comps;
            return comps == null || comps.All(c => c != null
                && c.GetType().Assembly == typeof(Hediff).Assembly && PhysicalComps.Contains(c.GetType().FullName));
        }

        // Called once at the irreversible conversion boundary, never during menu
        // validation or ritual work. Removal callbacks are native and not rolled back.
        internal bool Apply()
        {
            if (attempted || !CanPrepareBody(pawn) || pawn.health?.hediffSet?.hediffs == null
                || !pawn.health.hediffSet.hediffs.SequenceEqual(original)
                || physical.Any(h => h.pawn != pawn || !IsKnownPhysical(h))) return false;
            attempted = true;
            foreach (var effect in physical)
            {
                if (effect.def.defName == "TM_OutOfBodyHD") effect.TryGetComp<HediffComp_SymbiosisCaster>().RemoveHostHediff();
                if (effect.def.defName == "TM_SymbiosisHD") effect.TryGetComp<HediffComp_SymbiosisHost>().RemoveSymbioteHediff();
                if (effect.TryGetComp<HediffComp_SoulBond>() is { originalId: 0 })
                    DemonSoulBond.EndOnDeath(pawn,pawn.GetCompAbilityUserMagic()?.soulBondPawn,effect.def.defName == "TM_WDSoulBondMentalHD",false);
                if (effect.TryGetComp<HediffComp_SoulBondHost>() is { } receiver)
                    DemonSoulBond.EndOnDeath(receiver.BonderPawn,pawn,effect.def.defName == "TM_SoulBondMentalHD",true);
                pawn.health.RemoveHediff(effect);
            }
            return original.Except(physical).All(h => pawn.health.hediffSet.hediffs.Contains(h))
                && physical.All(h => !pawn.health.hediffSet.hediffs.Contains(h));
        }
    }
}
