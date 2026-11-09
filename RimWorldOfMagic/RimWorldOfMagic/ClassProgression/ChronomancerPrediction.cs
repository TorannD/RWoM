using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Native queue remains the sole owner and serializer of each incident.
    public static class ChronomancerPrediction
    {
        sealed class Tag { public string Token; }
        static readonly ConditionalWeakTable<QueuedIncident, Tag> tags = new ConditionalWeakTable<QueuedIncident, Tag>();
        static readonly System.Reflection.FieldInfo queueField = AccessTools.Field(typeof(IncidentQueue), "queuedIncidents");
        static List<QueuedIncident> Entries(IncidentQueue queue) => queue == null ? null : queueField.GetValue(queue) as List<QueuedIncident>;

        internal static Pawn CastingBody(Pawn owner)
        {
            if (SpiritRebirthState.TryGet(owner,out var state))
                return !state.DepletionPending && state.HasValidHost(owner) && state.Host.Spawned ? state.Host : null;
            return owner?.Spawned == true ? owner : null;
        }

        internal static Pawn Predictor(Pawn body)
        {
            var effect = body?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD);
            return effect?.TryGetComp<HediffComp_Prediction>() is { removeNow: false } prediction ? prediction.Owner : null;
        }

        public static void ExposeComp(CompAbilityUserMagic comp)
        {
            Scribe_Values.Look(ref comp.predictionToken, "rwomPredictionToken", null);
        }

        public static void ExposeQueued(QueuedIncident entry)
        {
            var tag = tags.GetValue(entry, _ => new Tag());
            Scribe_Values.Look(ref tag.Token, "rwomPredictionToken", null);
        }

        static bool Tagged(QueuedIncident entry, out Tag tag)
        {
            return tags.TryGetValue(entry, out tag) && !string.IsNullOrEmpty(tag.Token);
        }

        public static QueuedIncident ResolveOwned(CompAbilityUserMagic comp, IncidentQueue queue)
        {
            if (comp == null || string.IsNullOrEmpty(comp.predictionToken)) return null;
            var entries = Entries(queue);
            if (entries == null) return null;
            QueuedIncident found = null;
            foreach (var entry in entries)
            {
                if (entry != null && Tagged(entry, out var tag) && string.Equals(tag.Token, comp.predictionToken, StringComparison.Ordinal))
                {
                    if (found != null) return null; // Ambiguous ownership must never delete an event.
                    found = entry;
                }
            }
            return found;
        }

        public static bool Matches(CompAbilityUserMagic comp, IncidentParms parms, IncidentQueue queue)
        {
            return parms != null && ReferenceEquals(ResolveOwned(comp, queue)?.FiringIncident?.parms, parms);
        }

        public static bool IsReturningPrediction(IncidentQueue queue, IncidentParms parms)
        {
            if (parms == null) return false;
            // Native TryFire runs before removing its entry; retain the tag through retries.
            return Entries(queue)?.Any(q => q != null && Tagged(q, out _) && ReferenceEquals(q.FiringIncident?.parms, parms)) == true;
        }

        static bool Insert(IncidentQueue queue, QueuedIncident entry)
        {
            var entries = Entries(queue);
            if (entries == null || entry?.FiringIncident?.def == null || entry.FiringIncident.parms == null || entries.Contains(entry)) return false;
            tags.GetValue(entry, _ => new Tag()).Token = Guid.NewGuid().ToString("N");
            try
            {
                if (queue.Add(entry) && entries.Contains(entry)) return true;
            }
            catch
            {
                entries.Remove(entry);
                tags.Remove(entry);
                throw;
            }
            entries.Remove(entry);
            tags.Remove(entry);
            return false;
        }

        static void Claim(CompAbilityUserMagic comp, QueuedIncident entry)
        {
            comp.predictionToken = tags.GetValue(entry, _ => new Tag()).Token;
            comp.predictionIncidentDef = entry.FiringIncident.def;
            comp.predictionTick = entry.FireTick;
            comp.predictionHash = entry.FiringIncident.parms.GetHashCode();
        }

        public static bool TryEnqueue(CompAbilityUserMagic comp, IncidentQueue queue, QueuedIncident entry)
        {
            if (comp == null || !string.IsNullOrEmpty(comp.predictionToken) || !Insert(queue, entry)) return false;
            Claim(comp, entry);
            return true;
        }

        public static void Clear(CompAbilityUserMagic comp)
        {
            if (comp == null) return;
            comp.predictionToken = null;
            comp.predictionIncidentDef = null;
            comp.predictionTick = 0;
            comp.predictionHash = 0;
        }

        public static bool CancelOwned(CompAbilityUserMagic comp, IncidentQueue queue)
        {
            var owned = ResolveOwned(comp, queue);
            if (owned == null || !Entries(queue).Remove(owned)) return false;
            Clear(comp);
            return true;
        }

        public static void Expire(CompAbilityUserMagic comp, IncidentQueue queue)
        {
            var owned = ResolveOwned(comp, queue);
            // Once native execution has begun, its retry policy owns the event.
            // Expiring a prediction marker must not cancel a failed native retry.
            if (owned != null && !owned.TriedToFire) Entries(queue).Remove(owned);
            Clear(comp); // Unknown legacy ownership clears only local metadata.
        }

        public static bool ReplaceOwned(CompAbilityUserMagic comp, IncidentQueue queue, QueuedIncident expected, QueuedIncident replacement)
        {
            if (expected == null || !ReferenceEquals(expected, ResolveOwned(comp, queue)) || !Insert(queue, replacement)) return false;
            if (!Entries(queue).Remove(expected))
            {
                Entries(queue).Remove(replacement);
                tags.Remove(replacement);
                return false;
            }
            Claim(comp, replacement);
            return true;
        }
    }

    [HarmonyPatch(typeof(QueuedIncident), nameof(QueuedIncident.ExposeData))]
    public static class ChronomancerPredictionScribePatch
    {
        public static void Postfix(QueuedIncident __instance) => ChronomancerPrediction.ExposeQueued(__instance);
    }
}

