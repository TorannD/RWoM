using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Guidance
{
    // Receipts belong to the save, never to global player tutorial preferences.
    public sealed class MagicGuideState : IExposable
    {
        private bool initialized;
        private List<string> seen = new List<string>();
        public bool Initialized => initialized;

        public List<string> TakeNew(IEnumerable<string> eligible)
        {
            var newlyReady = eligible.Where(key => !seen.Contains(key)).Distinct().ToList();
            seen.AddRange(newlyReady);
            if (!initialized)
            {
                initialized = true;
                return new List<string>();
            }
            return newlyReady;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Collections.Look(ref seen, "seen", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && seen == null) seen = new List<string>();
        }
    }

    public sealed class GameComponent_MagicGuidance : GameComponent
    {
        private MagicGuideState receipts = new MagicGuideState();
        private bool startPending;
        private bool researchPending;
        private readonly Game game;

        public GameComponent_MagicGuidance(Game game) { this.game = game; }

        private IEnumerable<string> Ready() => MagicGuides.Unlocked(
            name => DefDatabase<ResearchProjectDef>.GetNamedSilentFail(name)?.IsFinished == true,
            ModsConfig.OdysseyActive, Compatibility.OptionalCompatibility.IsEnabled("ShipOxygenGuidance"));

        public override void StartedNewGame()
        {
            // Native starting research is applied before this callback.
            receipts.TakeNew(Ready());
            startPending = game.Scenario?.AllParts.OfType<Odyssey.ScenPart_MagicalShipArrival>().Any() == true;
        }

        public override void LoadedGame()
        {
            // New component on an old save: remember history silently. Existing
            // saves retain a completion queued just before saving.
            if (!receipts.Initialized) receipts.TakeNew(Ready());
        }

        public void ResearchFinished() { researchPending = true; }

        public override void GameComponentTick()
        {
            if ((!startPending && !researchPending) || Current.ProgramState != ProgramState.Playing) return;
            if (startPending)
            {
                startPending = false;
                Find.LetterStack.ReceiveLetter("TM_GuideTitle".Translate(),
                    "TM_GuideStart".Translate() + "\n\n" + MagicGuides.StartBody(), LetterDefOf.NeutralEvent);
            }
            if (researchPending)
            {
                researchPending = false;
                var ready = receipts.TakeNew(Ready());
                if (ready.Count > 0)
                    Find.LetterStack.ReceiveLetter("TM_GuideTitle".Translate(),
                        "TM_GuideResearch".Translate() + "\n\n" + string.Join("\n\n", ready.Select(key => MagicGuides.Body(key))),
                        LetterDefOf.NeutralEvent);
            }
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref receipts, "TM_magicGuidance");
            Scribe_Values.Look(ref startPending, "TM_magicGuideStartPending");
            // Completion is coalesced until the next tick; save its intent too.
            Scribe_Values.Look(ref researchPending, "TM_magicGuideResearchPending");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && receipts == null) receipts = new MagicGuideState();
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class MagicGuidanceResearchPatch
    {
        public static void Postfix() => Current.Game?.GetComponent<GameComponent_MagicGuidance>()?.ResearchFinished();
    }
}
