using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class SniperArmorBreach : Hediff
    {
        private static readonly ConditionalWeakTable<Pawn, SniperArmorBreach> active = new();
        private List<Opening> openings = new();
        private const int Duration = 360;

        internal static void Apply(Pawn shooter, Pawn victim, int rank)
        {
            if (shooter?.Faction == null || victim?.Spawned != true || victim.Dead || victim.IsPrisoner ||
                !victim.HostileTo(shooter) || rank < 1 || rank > 3) return;

            var def = DefDatabase<HediffDef>.GetNamed("TM_SniperArmorBreach");
            var breach = victim.health.hediffSet.GetFirstHediffOfDef(def) as SniperArmorBreach;
            bool isNew = breach == null;
            if (isNew) breach = (SniperArmorBreach)HediffMaker.MakeHediff(def, victim);
            breach.openings.Add(new Opening(shooter.Faction.loadID, SniperTraining.BreachPercent(rank), Find.TickManager.TicksGame + Duration));
            if (isNew) victim.health.AddHediff(breach);
        }

        // Read only when native injury processing is about to roll armor. The weak index
        // avoids scanning every unrelated pawn's entire medical record on each hit.
        internal static float Penetration(ref DamageInfo damage, Pawn victim)
        {
            float original = damage.ArmorPenetrationInt;
            if (!active.TryGetValue(victim, out var breach) || damage.Instigator?.Faction is not Faction faction ||
                !victim.HostileTo(faction) || !victim.health.hediffSet.hediffs.Contains(breach)) return original;

            int points = 0;
            int now = Find.TickManager.TicksGame;
            foreach (var opening in breach.openings)
                if (opening.FactionId == faction.loadID && opening.Expires > now) points += opening.Points;
            return original + points / 100f;
        }

        private void Index()
        {
            if (pawn == null) return;
            active.Remove(pawn);
            active.Add(pawn, this);
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Index();
        }

        public override void PostRemoved()
        {
            if (pawn != null && active.TryGetValue(pawn, out var current) && current == this) active.Remove(pawn);
            base.PostRemoved();
        }

        public override bool ShouldRemove => openings.Count == 0;
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            int expired = 0;
            int now = Find.TickManager.TicksGame;
            while (expired < openings.Count && openings[expired].Expires <= now) expired++;
            if (expired > 0) openings.RemoveRange(0, expired);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref openings, "sniperOpenings", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                openings ??= new List<Opening>();
                openings.RemoveAll(o => o == null);
                Index();
            }
        }

        private sealed class Opening : IExposable
        {
            public int FactionId, Points, Expires;
            public Opening() { }
            public Opening(int faction, int points, int expires)
            { FactionId = faction; Points = points; Expires = expires; }
            public void ExposeData()
            {
                Scribe_Values.Look(ref FactionId, "factionId");
                Scribe_Values.Look(ref Points, "points");
                Scribe_Values.Look(ref Expires, "expires");
            }
        }
    }

    [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
    internal static class SniperBreachArmorRoll
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var read = AccessTools.PropertyGetter(typeof(DamageInfo), nameof(DamageInfo.ArmorPenetrationInt));
            int at = code.FindIndex(c => c.Calls(read));
            if (at < 0 || code.Skip(at + 1).Any(c => c.Calls(read)))
                throw new InvalidOperationException("Expected one penetration read in the injury armor roll.");
            var loadVictim = new CodeInstruction(OpCodes.Ldarg_2);
            loadVictim.MoveLabelsFrom(code[at]);
            code[at].operand = AccessTools.Method(typeof(SniperArmorBreach), nameof(SniperArmorBreach.Penetration));
            code.Insert(at, loadVictim);
            return code;
        }
    }
}
