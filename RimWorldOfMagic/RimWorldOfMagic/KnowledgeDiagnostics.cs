using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic
{
    // Anti-spam reporting for "sleeping knowledge": knowledge entries whose def
    // no longer resolves (spell removed or renamed). One summary line per pawn
    // per in-game day; never throws, never per-entry spam.
    public static class KnowledgeDiagnostics
    {
        private const int ReportIntervalTicks = 60000;

        public static void ReportSleeping(Pawn pawn, AbilityKnowledgeComp knowledge)
        {
            if (pawn == null || knowledge == null) return;
            int now = Current.Game?.tickManager?.TicksGame ?? 0;
            if (now < knowledge.NextDiagnosticTick) return;
            knowledge.NextDiagnosticTick = now + ReportIntervalTicks;
            int sleeping = 0;
            foreach (SpellKnowledge spell in knowledge.Spells)
            {
                if (spell == null || !spell.learned) continue;
                if (!CoreBridge.TryResolve(spell.spellId, out _)) sleeping++;
            }
            if (sleeping > 0)
            {
                // LabelShortCap walks race/kind data a generated or exotic pawn
                // may not have — this is a per-tick path and must never throw.
                string label = pawn.kindDef == null ? "Pawn" + pawn.thingIDNumber : pawn.LabelShortCap;
                Log.Message("[RWoM] " + label + ": " + sleeping
                    + " known spell(s) currently have no matching def (sleeping knowledge).");
            }
        }
    }
}
