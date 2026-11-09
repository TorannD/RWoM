using System;
using AbilityUser;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    // Vanilla tracker entry for an RWoM ability. The active JecsLite owner
    // owns the verb, targeting, costs and cooldown throughout its lifetime.
    public class TMBridgeAbility : Ability
    {
        public TMBridgeAbility()
        {
        }

        public TMBridgeAbility(Pawn pawn, RimWorld.AbilityDef def) : base(pawn, def)
        {
        }

        // The JecsLite cast engine for the resolved original. Null = sleeping
        // knowledge (def gone); every consumer gates on this, never throwing.
        public PawnAbility Companion
        {
            get
            {
                var bridge = def as TMBridgeAbilityDef;
                var original = bridge?.Original;
                if (original == null || pawn?.TryGetComp<AbilityKnowledgeComp>()?.Find(bridge.spellId)?.learned != true)
                    return null;
                foreach (ThingComp comp in pawn.AllComps)
                {
                    if (comp is CompAbilityUserTMBase user)
                    {
                        var powers = user.AbilityData?.AllPowers;
                        if (powers == null) continue;
                        foreach (var power in powers)
                            if (power?.Def == original) return power;
                    }
                }
                return null;
            }
        }

        public override IEnumerable<Command> GetGizmos()
        {
            var owner = Companion;
            if (owner == null || !VanillaBridgeGizmoVisibilityPatch.VisibleThroughTracker(owner)) yield break;
            // RWoM adds its autocast control to Command_PawnAbility. Use the
            // same owner command before and after the tracker synchronizes a rank.
            var command = owner.GetGizmo();
            if (!command.Disabled && GizmoDisabled(out string reason)) command.Disable(reason);
            yield return command;
        }

        public override bool GizmoDisabled(out string reason)
        {
            // The vanilla gizmo greys out on the REAL resource/cooldown state
            // through the companion's own admission check. Both that check and
            // the vanilla base logic probe ambient state (faction, needs,
            // story, world) a generated or exotic pawn may not carry — the
            // grey-out is cosmetic and must never break the gizmo; the cast
            // itself re-checks admission.
            try
            {
                PawnAbility owner = Companion;
                if (owner == null)
                {
                    reason = "TM_SleepingSpellsDesc".Translate();
                    return true;
                }
                if (owner.Verb == null || !pawn.Spawned)
                {
                    reason = "TM_SleepingSpellsDesc".Translate();
                    return true;
                }
                if (!owner.CanCastPowerCheck(AbilityContext.Player, out string why))
                {
                    reason = why;
                    return true;
                }
                return base.GizmoDisabled(out reason);
            }
            catch (Exception)
            {
                reason = "TM_SleepingSpellsDesc".Translate();
                return true;
            }
        }

        // Phased out of vanilla AI targeting on purpose: our ResolveAIAutoCast
        // keeps AI ownership, so the bridge must not double-cast. Ability AI
        // drives bridges through the verb and comps instead: it never consults
        // Ability.AICanTargetNow, and vanilla's own fight AI already skips
        // bridge defs (aiCanUse unset).
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return false;
        }

        // The bridge must not run the vanilla verb/warmup machinery — the cast
        // path owns ticking through the companion's PawnAbility. Vanilla
        // WarmupTick derefs job/verb state the bridge never sets (crashed on
        // world pawns).
        public override void AbilityTick()
        {
        }

        // External AI drivers (and the vanilla cast job's own FailOn) read
        // CanCast. The vanilla tracker never holds the real cooldown or
        // resource state, so route readiness through the companion's own
        // admission check. Quiet: returns the reason, never posts messages.
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;
                try
                {
                    PawnAbility owner = Companion;
                    // Readiness is the companion's admission alone: AI drivers
                    // and queueing only ask about pawns whose spells are live.
                    // (GizmoDisabled keeps its stricter Spawned gate.)
                    if (owner == null)
                        return "TM_SleepingSpellsDesc".Translate();
                    if (!owner.CanCastPowerCheck(CastContext, out string why)) return why;
                    return true;
                }
                catch (Exception)
                {
                    return "TM_SleepingSpellsDesc".Translate();
                }
            }
        }

        // Player-faction pawns cast through the player path (payment, xp,
        // selection restores); factioned non-player pawns (NPC casters) cast
        // through the AI path, matching how the classic system distinguishes
        // contexts. A null faction (wild animals, synthetic fixtures) keeps
        // the historical player path.
        private AbilityContext CastContext =>
            pawn?.Faction == null || pawn.Faction == Faction.OfPlayer
                ? AbilityContext.Player : AbilityContext.AI;

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return !GizmoDisabled(out _) && Companion.UseAbility(CastContext, target) != null;
        }

        // External AI drivers and the vanilla cast machinery ask the bridge for
        // its cast job. The bridge's own tracker verb has no PawnAbility owner,
        // so letting vanilla warmup fire it crashes inside Verb_UseAbility
        // (observed live: TryCastNextBurstShot NullReferenceException when
        // Ability AI handed out a ruled bridge spell). Return the companion's
        // classic job instead — the owner verb, the TM job drivers, payment
        // and cooldown all stay on their normal path. Marking the job with
        // this bridge ability lets external drivers recognize the running
        // cast and not re-order it. Null companion (sleeping spell) refuses
        // the cast without a job.
        public override Job GetJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            PawnAbility owner = Companion;
            Job job = owner?.GetJob(CastContext, target);
            if (job != null) job.ability = this;
            return job;
        }

        public override void QueueCastingJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            Activate(target, destination);
        }

    }
}
