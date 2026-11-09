using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class DeathKnightCast
    {
        internal const string Spite = "TM_SpiritCastSpite", Fear = "TM_SpiritCastWaveOfFear", Blade = "TM_SpiritCastGraveBlade";
        internal static bool Handles(string job) => job == Spite || job == Fear || job == Blade;
        internal static TMAbilityDef Definition(string job) => job == Spite ? TorannMagicDefOf.TM_Spite :
            job == Fear ? TorannMagicDefOf.TM_WaveOfFear : job == Blade ? TorannMagicDefOf.TM_GraveBlade : null;
        internal static bool Matches(string job, TMAbilityDef def) => def != null && (def == Definition(job) ||
            job == Spite && (def == TorannMagicDefOf.TM_Spite_I || def == TorannMagicDefOf.TM_Spite_II || def == TorannMagicDefOf.TM_Spite_III) ||
            job == Blade && (def == TorannMagicDefOf.TM_GraveBlade_I || def == TorannMagicDefOf.TM_GraveBlade_II || def == TorannMagicDefOf.TM_GraveBlade_III));

        private static Verb_SpiritTargetable For(Pawn body, MightAbility power)
        {
            if (power?.mightDef.MainVerb?.verbClass is not Type type || !typeof(Verb_SpiritTargetable).IsAssignableFrom(type)) return null;
            var verb = (Verb_SpiritTargetable)Activator.CreateInstance(type);
            verb.caster = body; verb.Ability = power; verb.verbProps = power.mightDef.MainVerb;
            return verb;
        }

        internal static bool Funded(Pawn body, MightAbility power, string job)
        {
            if (power.mightDef.MainVerb.isViolent && body.WorkTagIsDisabled(WorkTags.Violent)) return false;
            return job != Spite || TM_Calc.GetHateHediff(power.Pawn)?.Severity >= 20f;
        }

        internal static bool Valid(Pawn body, MightAbility power, string job, LocalTargetInfo target)
        {
            if (power == null || body?.Spawned != true || !target.IsValid ||
                target.HasThing && (!target.Thing.Spawned || target.Thing.Map != body.Map)) return false;
            if (job == Fear && target.Thing != body) return false;
            return For(body, power)?.CanHitTargetFrom(body.Position, target) == true;
        }

        internal static bool Execute(Pawn body, Pawn soul, string job, LocalTargetInfo target)
        {
            var power = SpiritHostedMartial.Find(body, soul, job);
            if (!SpiritHostedMartial.Ready(body, power, job) || !Valid(body, power, job, target)) return false;
            if (!For(body, power).CastFromHost(target)) return false;
            power.PostAbilityAttempt();
            return true;
        }

        internal static void Choose(Pawn body, Pawn soul, MightAbility power, string name)
        {
            var job = DefDatabase<JobDef>.GetNamed(name);
            bool Allowed(LocalTargetInfo target) => SpiritHostedMartial.Ready(body, SpiritHostedMartial.Find(body, soul, name), name) && Valid(body, power, name, target);
            void Order(LocalTargetInfo target)
            {
                if (body.CurJobDef != job && Allowed(target)) body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job, soul, target), JobTag.Misc);
            }
            if (name == Fear) { Order(body); return; }
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams, Order,
                highlightAction: target => GenDraw.DrawRadiusRing(body.Position, power.mightDef.MainVerb.range),
                targetValidator: Allowed, caster: body);
        }
    }
}
