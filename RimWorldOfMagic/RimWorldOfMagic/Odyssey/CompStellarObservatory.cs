using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
namespace TorannMagic.Odyssey
{
    public class CompProperties_StellarObservatory : CompProperties
    {
        public int ritualTicks = 3600;
        public CompProperties_StellarObservatory() { compClass = typeof(CompStellarObservatory); }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef)) yield return error;
            if (ritualTicks <= 0) yield return "Stellar observatory ritual duration must be positive.";
        }
    }
    public class CompStellarObservatory : ThingComp
    {
        private StellarDivinationSearch search;
        private Quest pendingQuest;
        private bool completed, completing;
        private int nextRetryTick, activationTick = -1, lastDiscoveryTick = -1;
        public CompProperties_StellarObservatory Props => (CompProperties_StellarObservatory)props;
        public bool CanOperate => ModsConfig.OdysseyActive && parent != null && parent.Spawned && !parent.Destroyed
            && parent.Faction != null && parent.Faction.IsPlayer && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown() && Props.ritualTicks > 0;
        public bool IsSearching => search != null && !completed;
        public int RemainingTicks => search?.RemainingTicks ?? 0;
        public int ActivationTick => activationTick;
        public int LastDiscoveryTick => lastDiscoveryTick;
        private WorldComponent_StellarDivination Controller => Find.World?.GetComponent<WorldComponent_StellarDivination>();
        public bool CanPerformRitual(Pawn pawn, bool requirePosition = true)
        {
            if (!CanOperate || IsSearching || pawn == null || !pawn.Spawned || pawn.Map != parent.Map || pawn.Faction != parent.Faction
                || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState || pawn.needs == null
                || (requirePosition && pawn.Position != parent.InteractionCell)) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            return magic != null && magic.SetIsMagicUser() && magic.IsMagicUser;
        }
        public bool CanActivate(Pawn pawn, bool requirePosition = true) => CanPerformRitual(pawn, requirePosition)
            && Controller?.CanStart(Find.TickManager.TicksGame) == true && StellarDivinationPayment.CanPay(parent)
            && StellarDivinationQuestUtility.HasEligibleQuest(parent.Map);
        public bool CompleteActivation(Pawn pawn)
        {
            if (!CanActivate(pawn)) return false;
            var controller = Controller;
            int now = Find.TickManager.TicksGame;
            if (!controller.TryStart(now, () => StellarDivinationPayment.TryPay(parent, () => CanPerformRitual(pawn)
                && StellarDivinationQuestUtility.HasEligibleQuest(parent.Map)))) return false;
            search = new StellarDivinationSearch();
            search.Begin(controller.SearchSerial, Rand.RangeInclusive(480000, 600000));
            completed = false; pendingQuest = null; nextRetryTick = 0; activationTick = now;
            return true;
        }
        public override void CompTick() => TickSearch(Find.TickManager.TicksGame, 1);
        public override void CompTickInterval(int delta) => TickSearch(Find.TickManager.TicksGame, delta);
        private void TickSearch(int now, int delta)
        {
            if (!IsSearching || completing || !CanOperate) return;
            search.Advance(now, delta, true);
            if (RemainingTicks > 0 || now < nextRetryTick) return;
            completing = true; nextRetryTick = now > int.MaxValue - 2500 ? int.MaxValue : now + 2500;
            try
            {
                if (pendingQuest == null) pendingQuest = StellarDivinationQuestUtility.Generate(parent.Map);
                if (pendingQuest == null || !CanOperate) return;
                var quest = pendingQuest;
                try { if (!Find.QuestManager.Contains(quest)) Find.QuestManager.Add(quest); }
                catch (Exception ex)
                {
                    Compatibility.TM_Log.Warning("StellarDivination", "Expedition registration callback failed: " + ex.Message);
                }
                finally
                {
                    // Native Add owns the quest before invoking mod callbacks. A callback failure
                    // must never retry registration or create a replacement expedition.
                    if (Find.QuestManager.Contains(quest))
                    {
                        completed = true; pendingQuest = null; lastDiscoveryTick = now;
                    }
                }
                if (completed) StellarDivinationQuestUtility.SendLetter(quest);
            }
            catch (Exception ex) { Compatibility.TM_Log.Warning("StellarDivination", "Expedition callback failed: " + ex.Message); }
            finally { completing = false; }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref search, "TM_stellarSearch");
            Scribe_Deep.Look(ref pendingQuest, "TM_stellarPendingQuest");
            Scribe_Values.Look(ref completed, "TM_stellarCompleted", false);
            Scribe_Values.Look(ref nextRetryTick, "TM_stellarNextRetry", 0);
            Scribe_Values.Look(ref activationTick, "TM_stellarActivation", -1);
            Scribe_Values.Look(ref lastDiscoveryTick, "TM_stellarDiscovery", -1);
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            if (!CanPerformRitual(pawn, false)) yield break;
            if (!CanActivate(pawn, false) || !pawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Some))
            {
                yield return new FloatMenuOption("TM_StellarRitual".Translate() + ": " + "TM_StellarUnavailable".Translate(), null); yield break;
            }
            yield return new FloatMenuOption("TM_StellarRitual".Translate(), () =>
            {
                if (CanActivate(pawn, false)) pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_ActivateStellarObservatory"), parent), JobTag.Misc);
            });
        }
        public override string CompInspectStringExtra()
        {
            if (IsSearching) return RemainingTicks == 0 ? "TM_StellarWaiting".Translate().ToString()
                : "TM_StellarSearching".Translate(RemainingTicks.ToStringTicksToPeriod()).ToString();
            long cooldown = Controller?.CooldownRemaining(Find.TickManager.TicksGame) ?? 0;
            return cooldown > 0 ? "TM_StellarCooldown".Translate(((int)Math.Min(int.MaxValue, cooldown)).ToStringTicksToPeriod()).ToString()
                : "TM_StellarIdle".Translate().ToString();
        }
    }
}
