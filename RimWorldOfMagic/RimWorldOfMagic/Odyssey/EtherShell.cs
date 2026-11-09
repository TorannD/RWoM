using System;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    public static class SpaceSpellProgression
    {
        public static bool ResearchComplete => ModsConfig.OdysseyActive
            && Current.Game?.researchManager != null
            && DefDatabase<ResearchProjectDef>.GetNamedSilentFail("OrbitalTech")?.IsFinished == true
            && DefDatabase<ResearchProjectDef>.GetNamedSilentFail("TM_AdvancedScribing")?.IsFinished == true;

        public static bool IsSpaceSpell(TMAbilityDef ability) => ability?.defName == "TM_EtherShell"
            || ability?.defName == "TM_BreachSeal";

        public static bool Visible(MagicPower power) => power != null
            && (!IsSpaceSpell(power.abilityDef as TMAbilityDef) || power.learned || ResearchComplete);

        public static int Duration(Pawn caster, string spell)
        {
            var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(spell);
            var magic = caster?.GetCompAbilityUserMagic();
            int level = ability == null || magic == null || !magic.IsMagicUser ? 0
                : magic.MagicData.GetSkill_Versatility(ability)?.level ?? 0;
            return 60000 + Math.Max(0, Math.Min(3, level)) * 15000;
        }
    }

    public class EtherShellAbility : MagicAbility
    {
        private bool appliedReceipt;

        public EtherShellAbility() { }
        public EtherShellAbility(AbilityUser.CompAbilityUser user) : base(user) { }
        public EtherShellAbility(Pawn user, AbilityUser.AbilityDef def) : base(user, def) { }

        internal void SettleAppliedShell()
        {
            appliedReceipt = true;
            PostAbilityAttempt();
        }

        public override void PostAbilityAttempt()
        {
            if (!appliedReceipt) return;
            appliedReceipt = false;
            base.PostAbilityAttempt();
        }
    }

    public static class EtherShell
    {
        public const int DurationTicks = 60000;
        public const string HediffName = "TM_EtherShellHD";

        public static string TargetRejection(Pawn caster, Pawn target)
        {
            if (!ModsConfig.OdysseyActive) return "TM_EtherShell_Unavailable";
            if (caster == null || caster.Dead || !caster.Spawned || target == null || target.Dead
                || !target.Spawned || target.Map != caster.Map || target.health?.hediffSet == null)
                return "TM_SpellItem_InvalidTarget";
            if (target != caster && (target.Faction == null || caster.Faction == null
                || (target.Faction != caster.Faction && target.Faction.RelationKindWith(caster.Faction) != FactionRelationKind.Ally)))
                return "TM_SpellItem_HostileTarget";
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(HediffName);
            if (def == null) return "TM_EtherShell_Unavailable";
            return target.health.hediffSet.HasHediff(def) ? "TM_EtherShell_AlreadyProtected" : null;
        }

        public static bool TryApply(Pawn caster, Pawn target)
            => TryApply(caster, target, DurationTicks);

        public static bool TryApply(Pawn caster, Pawn target, int duration)
        {
            if (TargetRejection(caster, target) != null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(HediffName);
            Hediff shell = HediffMaker.MakeHediff(def, target);
            HediffComp_Disappears expiry = shell.TryGetComp<HediffComp_Disappears>();
            if (expiry == null) return false;
            expiry.SetDuration(duration);
            try { target.health.AddHediff(shell); }
            catch (Exception exception)
            {
                Compatibility.TM_Log.Warning("EtherShell", "Shell health notification interrupted: " + exception.Message);
            }
            // Native notifications can fail after insertion. A real effect still costs resources.
            bool applied = target.health.hediffSet.hediffs.Contains(shell);
            if (applied) Hediff_EtherShell.ClearStats(target);
            return applied;
        }
    }

    public class Hediff_EtherShell : HediffWithComps
    {
        internal static void ClearStats(Pawn pawn)
        {
            if (pawn == null) return;
            if (ModsConfig.OdysseyActive)
                DefDatabase<StatDef>.GetNamedSilentFail("VacuumResistance")?.Worker.ClearCacheForThing(pawn);
            DefDatabase<StatDef>.GetNamedSilentFail("ComfyTemperatureMin")?.Worker.ClearCacheForThing(pawn);
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            try { base.PostAdd(dinfo); }
            finally { ClearStats(pawn); }
        }

        public override void PostRemoved()
        {
            try { base.PostRemoved(); }
            finally { ClearStats(pawn); }
        }
    }
}

namespace TorannMagic
{
    public class Verb_EtherShell : Verb_SB
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
            => Odyssey.EtherShell.TargetRejection(CasterPawn, target.Thing as Pawn) == null
                && base.CanHitTargetFrom(root, target);

        protected override bool TryCastShot()
        {
            var ability = Ability as Odyssey.EtherShellAbility;
            bool result = ability != null && ability.CanCastPowerCheck(AbilityUser.AbilityContext.Player, out _)
                && CanHitTargetFrom(CasterPawn.Position, currentTarget)
                && Odyssey.EtherShell.TryApply(CasterPawn, currentTarget.Thing as Pawn,
                    Odyssey.SpaceSpellProgression.Duration(CasterPawn, "TM_EtherShell"));
            // Settle immediately: interruption after applying the shell cannot make it free.
            // The job's later finish callback sees no receipt and cannot charge twice.
            if (result) ability.SettleAppliedShell();
            PostCastShot(result, out result);
            return result;
        }
    }
}
