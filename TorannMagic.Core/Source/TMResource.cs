using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic.Core
{
    // Unified resource API for querying and consuming pawn energies (mana,
    // stamina, chi) without referencing RWoM-internal Need subclasses.
    // Resource types are looked up by NeedDef defName — vanilla-supported.
    public static class TMResource
    {
        public static readonly string Mana = "TM_Mana";
        public static readonly string Stamina = "TM_Stamina";
        public static readonly string Chi = "TM_ChiHD"; // hediff-based, not a need

        public static Need FindNeed(Pawn pawn, string resourceDefName)
        {
            if (pawn?.needs == null || resourceDefName == null) return null;
            var def = DefDatabase<NeedDef>.GetNamedSilentFail(resourceDefName);
            return def == null ? null : pawn.needs.TryGetNeed(def);
        }

        public static bool TryGetLevel(Pawn pawn, string resourceDefName, out float current, out float max)
        {
            var need = FindNeed(pawn, resourceDefName);
            if (need != null) { current = need.CurLevel; max = need.MaxLevel; return true; }
            current = 0f; max = 0f; return false;
        }

        public static bool TryConsume(Pawn pawn, string resourceDefName, float amount)
        {
            var need = FindNeed(pawn, resourceDefName);
            if (!float.IsFinite(amount) || amount < 0f || need == null || need.CurLevel < amount) return false;
            need.CurLevel -= amount;
            return true;
        }

        public static void TryAdd(Pawn pawn, string resourceDefName, float amount)
        {
            var need = FindNeed(pawn, resourceDefName);
            if (need != null && float.IsFinite(amount) && amount >= 0f) need.CurLevel += amount;
        }

        public static bool IsMagicUser(Pawn pawn)
        {
            if (pawn?.needs == null) return false;
            var def = DefDatabase<NeedDef>.GetNamedSilentFail(Mana);
            return def != null && pawn.needs.TryGetNeed(def) != null;
        }

        public static bool IsMightUser(Pawn pawn)
        {
            if (pawn?.needs == null) return false;
            var def = DefDatabase<NeedDef>.GetNamedSilentFail(Stamina);
            return def != null && pawn.needs.TryGetNeed(def) != null;
        }
    }
}
