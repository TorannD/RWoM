using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthHealthForm
    {
        internal const string DefinitionName = "TM_SpiritRebirthForm";
        private static HediffDef Definition => DefDatabase<HediffDef>.GetNamedSilentFail(DefinitionName);
        internal static bool HasForm(Pawn pawn)
            => SpiritRebirthFormPolicy.IsEthereal(pawn) || SpiritRebirthRitualTransaction.IsPreparing(pawn);

        private static int Skill(Pawn pawn, string label)
        {
            var data = pawn?.GetComp<CompAbilityUserMagic>()?.ExistingMagicData;
            if (data == null) return 0;
            var skills = data.magicPowerSkill_Custom;
            int level = 0;
            if (skills != null)
                foreach (var skill in skills)
                    if (skill != null && TMDefs.TM_CustomSkillLabels.Canonical(skill.label) == label && skill.level > level)
                        level = skill.level;
            string cantripLabel = label == "TM_SpiritPossession_pwr" ? "TM_Cantrips_pwr" : "TM_Cantrips_ver";
            if (data.magicPowerSkill_Cantrips != null)
                foreach (var skill in data.magicPowerSkill_Cantrips)
                    if (skill != null && skill.label == cantripLabel)
                    {
                        level = System.Math.Max(level, skill.level / 5);
                        break;
                    }
            // Match native GetMagicSkillLevel(canCopy:false), without lazy getters.
            if (ModOptions.Settings.Instance?.AIHardMode == true && !pawn.IsColonist) level = 3;
            return level;
        }

        internal static bool TryCapacity(Pawn pawn, out float capacity)
        {
            capacity = 0f;
            if (!SpiritRebirthBody.RetainsNeeds(pawn)) return false;
            var data = pawn.GetComp<CompAbilityUserMagic>()?.ExistingMagicData;
            capacity = 100f + (data?.MagicUserLevel ?? 0) * 2f + Skill(pawn, "TM_SpiritPossession_ver") * 15f;
            if (pawn.story?.Adulthood?.identifier == "tm_ancient_spirit") capacity += 50f;
            return true;
        }

        internal static bool Owns(Hediff hediff)
            => hediff is Hediff_SpiritRebirthForm && Definition?.hediffClass == typeof(Hediff_SpiritRebirthForm) && hediff.def == Definition;

        // Dirty state requests maintenance; it never authorizes a physical form.
        internal static void TryRefreshPending(Pawn pawn)
        {
            if (!SpiritRebirthState.TryGet(pawn, out var state) || !state.HealthFormDirty
                || !(SpiritRebirthFormPolicy.IsSpawnedEthereal(pawn) || state.HasValidStone(pawn)
                    || state.HasValidHost(pawn) || SpiritRebirthRitualTransaction.IsPendingAccountingOwner(pawn)
                    || MapComponent_SpiritCollapse.IsPendingAccountingOwner(pawn))) return;
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (tick < state.HealthFormRetryAt) return;
            try
            {
                if (pawn.health?.hediffSet == null || Definition?.hediffClass != typeof(Hediff_SpiritRebirthForm)
                    || (HasForm(pawn) && !(TorannMagicDefOf.TM_SpiritPossessorHD?.stages?.Count > 0)))
                {
                    state.DeferHealthFormRefresh(tick);
                    return;
                }
                // A preceding native callback may have removed the hediff before
                // throwing. Invalidate this pawn even when this retry has no delta.
                ReconcileCore(pawn, true);
                state.CompleteHealthFormRefresh();
            }
            catch (System.Exception exception)
            {
                state.DeferHealthFormRefresh(tick);
                if (state.TakeHealthFormWarning())
                    Compatibility.TM_Log.Warning("SpiritHealthForm", "Form refresh deferred for " + pawn.ThingID + ": " + exception);
            }
        }

        internal static bool Reconcile(Pawn pawn) => ReconcileCore(pawn, false);
        private static bool ReconcileCore(Pawn pawn, bool forceCacheRefresh)
        {
            var health = pawn?.health;
            if (health?.hediffSet == null) return false;
            var definition = Definition;
            if (definition == null || definition.hediffClass != typeof(Hediff_SpiritRebirthForm)) return false;
            bool active = HasForm(pawn)
                && TorannMagicDefOf.TM_SpiritPossessorHD?.stages?.Count > 0;
            bool changed = false;
            Hediff effect = null;
            var hediffs = health.hediffSet.hediffs;
            if (active)
            {
                for (int i = 0; i < hediffs.Count; i++)
                    if (Owns(hediffs[i])) { effect = hediffs[i]; break; }
            }
            for (int i = hediffs.Count - 1; i >= 0; i--)
                if (Owns(hediffs[i]) && hediffs[i] != effect) { health.RemoveHediff(hediffs[i]); changed = true; }
            if (active)
            {
                if (effect == null)
                {
                    effect = HediffMaker.MakeHediff(definition, pawn);
                    effect.Severity = .5f + Skill(pawn, "TM_SpiritPossession_pwr");
                    health.AddHediff(effect);
                    changed = true;
                }
                float severity = System.Math.Min(definition.maxSeverity, .5f + Skill(pawn, "TM_SpiritPossession_pwr"));
                if (effect.Severity != severity) { effect.Severity = severity; health.Notify_HediffChanged(effect); changed = true; }
            }
            if (changed || forceCacheRefresh)
            {
                health.hediffSet.DirtyCache();
                // Other stats can depend on the affected capacities/stat factors.
                // Clear only this pawn's entries on a mutation or pending refresh.
                foreach (var stat in DefDatabase<StatDef>.AllDefsListForReading)
                    if (stat.cacheable || stat.immutable) stat.Worker.ClearCacheForThing(pawn);
            }
            return changed;
        }
    }

    public sealed class Hediff_SpiritRebirthForm : HediffWithComps
    {
        public override HediffStage CurStage
        {
            get
            {
                if (!SpiritRebirthHealthForm.Owns(this) || !SpiritRebirthHealthForm.HasForm(pawn)) return null;
                var stages = TorannMagicDefOf.TM_SpiritPossessorHD?.stages;
                if (stages == null) return null;
                for (int i = stages.Count - 1; i >= 0; i--)
                    if (stages[i] != null && Severity >= stages[i].minSeverity) return stages[i];
                return null;
            }
        }

        public override void PostTick()
        {
            if (!SpiritRebirthHealthForm.Owns(this)) { base.PostTick(); return; }
            if (!SpiritRebirthHealthForm.HasForm(pawn)) SpiritRebirthHealthForm.Reconcile(pawn);
            if (CurStage != null) base.PostTick();
        }
        public override void PostTickInterval(int delta)
        {
            if (!SpiritRebirthHealthForm.Owns(this)) { base.PostTickInterval(delta); return; }
            SpiritRebirthHealthForm.Reconcile(pawn);
            if (CurStage != null) base.PostTickInterval(delta);
        }
    }
}
