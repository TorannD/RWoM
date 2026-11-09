using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    // The command holds no cloned ability. A queued job saves the original pawn
    // as a reference and resolves its current runtime owner again on execution.
    internal static class SpiritBloodSacrifice
    {
        internal static MagicAbility Find(Pawn body, Pawn soul)
        {
            if (soul == null || !SpiritRebirthState.TryGetPossessingSpirit(body, out var held)
                || held != soul || TorannMagicDefOf.TM_BloodGift == null) return null;
            var magic = soul.GetCompAbilityUserMagic();
            if (magic?.ExistingMagicData == null) return null;
            var local = body.GetCompAbilityUserMagic();
            if (local?.ExistingMagicData != null && local.AbilityData.AllPowers.Any(a => a?.Def == TorannMagicDefOf.TM_BloodGift)) return null;
            return magic.AbilityData.AllPowers.OfType<MagicAbility>().FirstOrDefault(a =>
                a.Def == TorannMagicDefOf.TM_BloodGift && ReferenceEquals(a.AbilityUser, magic));
        }

        internal static bool Ready(Pawn body, MagicAbility owner)
        {
            if (owner == null || !body.Spawned || body.Dead || body.Downed || body.InMentalState
                || body.Faction != Faction.OfPlayer || body.IsPrisoner || owner.CooldownTicksLeft > 0
                || body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MuteHD)
                || !ReferenceEquals(SpiritRebirthCasting.SkillOwner(body, owner), owner.Pawn)) return false;
            var magic = owner.MagicUser;
            var mana = magic?.Mana;
            if (mana == null) return false;
            float cost = magic.ActualManaCost(owner.magicDef);
            return !float.IsNaN(cost) && !float.IsInfinity(cost) && cost >= 0f && mana.CurLevel >= cost;
        }

        internal static IEnumerable<Gizmo> Commands(IEnumerable<Gizmo> existing, Pawn body)
        {
            foreach (var gizmo in existing) yield return gizmo;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body, out var soul)) yield break;
            var owner = Find(body, soul);
            if (owner == null) yield break;
            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("TM_SpiritBloodSacrifice");
            if (jobDef == null) yield break;
            var command = new Command_Action
            {
                defaultLabel = owner.Def.LabelCap,
                defaultDesc = "TM_SpiritBloodSacrificeDesc".Translate(soul.LabelShortCap, body.LabelShortCap),
                icon = owner.Def.uiIcon,
                action = () =>
                {
                    if (body.CurJobDef != jobDef && Ready(body, Find(body, soul)))
                        body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef, soul), JobTag.Misc);
                }
            };
            if (!Ready(body, owner) || body.CurJobDef == jobDef) command.Disable("TM_SpiritRebirthCastUnavailable".Translate());
            yield return command;
        }
    }

    public sealed class JobDriver_SpiritBloodSacrifice : JobDriver
    {
        private bool attempted;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref attempted, "attempted");
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var perform = ToilMaker.MakeToil("SpiritBloodSacrifice");
            perform.initAction = () =>
            {
                var owner = SpiritBloodSacrifice.Find(pawn, job.targetA.Thing as Pawn);
                if (attempted || pawn.CurJob != job || pawn.jobs.curDriver != this || !SpiritBloodSacrifice.Ready(pawn, owner))
                { EndJobWith(JobCondition.Incompletable); return; }
                attempted = true;
                // Charge once at the first physical effect, while the original
                // and the host are still associated. A fruitless attempt is free.
                if (!Verb_BloodGift.Apply(pawn, owner, () => owner.PayForAttempt(pawn)))
                    EndJobWith(JobCondition.Incompletable);
            };
            perform.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return perform;
        }
    }
}
