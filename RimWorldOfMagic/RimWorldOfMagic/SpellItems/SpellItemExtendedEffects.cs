using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.SpellItems
{
    public static partial class SpellItemEffects
    {
        private static readonly HashSet<string> Diseases = new HashSet<string>
        { "WoundInfection", "Flu", "Animal_Flu", "Animal_Plague", "Plague", "Malaria", "SleepingSickness", "GutWorms", "MuscleParasites", "FoodPoisoning", "Scaria" };
        private static readonly HashSet<string> Chronic = new HashSet<string>
        { "Asthma", "BadBack", "Frail", "Cataract", "HearingLoss", "ArteryBlockage" };

        public static Hediff TreatmentTarget(Pawn patient, bool purify)
        {
            return patient?.health?.hediffSet?.hediffs.FirstOrDefault(h => purify
                ? (h is Hediff_Injury injury && injury.IsPermanent()) || Chronic.Contains(h.def.defName)
                    || (h is Hediff_Addiction && h.def.defName != "LuciferiumAddiction")
                : Diseases.Contains(h.def.defName));
        }

        private static string ExtendedRejection(Pawn caster, LocalTargetInfo target, SpellItemKind kind)
        {
            if (kind == SpellItemKind.Portal)
                return SpellItemPortal.ValidCell(caster.Map, target.Cell) ? null : "TM_SpellItem_InvalidTarget";
            if (kind == SpellItemKind.Resurrection)
            {
                Corpse corpse = target.Thing as Corpse;
                Pawn dead = corpse?.InnerPawn;
                return dead != null && dead.Dead && !dead.Discarded && dead.RaceProps.IsFlesh && !TM_Calc.IsUndead(dead)
                    && corpse.TryGetComp<CompRottable>() != null
                    && !dead.HostileTo(caster) && corpse.GetRotStage() == RotStage.Fresh
                    ? null : "TM_SpellItem_InvalidTarget";
            }
            Pawn patient = target.Thing as Pawn;
            if (patient == null || patient.Dead || patient.RaceProps?.IsFlesh != true || TM_Calc.IsUndead(patient))
                return "TM_SpellItem_InvalidTarget";
            if (patient.HostileTo(caster)) return "TM_SpellItem_HostileTarget";
            switch (kind)
            {
                case SpellItemKind.Regeneration:
                    return HealingTargets(patient).Length > 0 && !patient.health.hediffSet.hediffs.Any(h => h is Hediff_SpellItemRegeneration)
                        ? null : "TM_SpellItem_NoTreatment";
                case SpellItemKind.CureDisease:
                case SpellItemKind.Purify:
                    return TreatmentTarget(patient, kind == SpellItemKind.Purify) != null ? null : "TM_SpellItem_NoTreatment";
                case SpellItemKind.Soothe:
                    return patient.needs?.mood?.thoughts?.memories != null
                        && !patient.needs.mood.thoughts.memories.Memories.Any(m => m.def.defName == "TM_ItemSoothed")
                        ? null : "TM_SpellItem_NoTreatment";
                default: return "TM_SpellItem_InvalidTarget";
            }
        }

        private static bool ApplyExtended(Pawn caster, LocalTargetInfo target, SpellItemKind kind)
        {
            if (kind == SpellItemKind.Portal)
                return SpellItemPortal.TryCreate(caster, target.Cell, caster.CurJob?.globalTarget ?? GlobalTargetInfo.Invalid);
            if (kind == SpellItemKind.Resurrection)
            {
                Pawn dead = ((Corpse)target.Thing).InnerPawn;
                try { ResurrectionUtility.TryResurrectWithSideEffects(dead); }
                catch (Exception e) { Compatibility.TM_Log.Warning("SpellItems", "Item resurrection interrupted: " + e); }
                // Native notifications may throw after revival. Charge for the actual result.
                return !dead.Dead;
            }
            Pawn patient = (Pawn)target.Thing;
            if (kind == SpellItemKind.CureDisease || kind == SpellItemKind.Purify)
            {
                Hediff condition = TreatmentTarget(patient, kind == SpellItemKind.Purify);
                if (condition == null) return false;
                try { patient.health.RemoveHediff(condition); }
                catch (Exception e) { Compatibility.TM_Log.Warning("SpellItems", "Item treatment interrupted: " + e); }
                if (patient.health.hediffSet.hediffs.Contains(condition)) return false;
            }
            else if (kind == SpellItemKind.Regeneration)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("TM_ItemRegeneration");
                if (def == null) return false;
                try { patient.health.AddHediff(def); }
                catch (Exception e) { Compatibility.TM_Log.Warning("SpellItems", "Item regeneration interrupted: " + e); }
                if (!patient.health.hediffSet.HasHediff(def)) return false;
            }
            else if (kind == SpellItemKind.Soothe)
            {
                ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail("TM_ItemSoothed");
                if (def == null) return false;
                var memories = patient.needs.mood.thoughts.memories;
                try { memories.TryGainMemory(def); }
                catch (Exception e) { Compatibility.TM_Log.Warning("SpellItems", "Item soothe interrupted: " + e); }
                if (!memories.Memories.Any(m => m.def == def)) return false;
            }
            else return false;
            Cosmetic(() => EmitHeal(patient));
            return true;
        }
    }

    // Ten pulses, five health total per pulse. Natural injuries only; never regrows parts.
    public class Hediff_SpellItemRegeneration : Hediff
    {
        private int elapsed, pulses;
        public override bool ShouldRemove => elapsed >= 1200 || pawn.Dead;
        public override void TickInterval(int delta)
        {
            elapsed += delta;
            while (pulses < Math.Min(10, elapsed / 120))
            {
                pulses++; // Record before health notifications; never retry a completed pulse.
                var injury = SpellItemEffects.HealingTargets(pawn).FirstOrDefault();
                if (injury != null) injury.Heal(5f);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref elapsed, "itemRegenElapsed");
            Scribe_Values.Look(ref pulses, "itemRegenPulses");
        }
    }
}
