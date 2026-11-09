using System;
using Verse;

namespace TorannMagic.ClassProgression
{
    public enum SpiritRebirthForm { Ethereal, Possessed, Dormant, Stone }

    // Identity and progression remain on the original pawn; native holders own custody.
    public sealed class SpiritRebirthState : IExposable
    {
        public const int CurrentVersion = 2;
        public const int RecoveryDurationTicks = 420000;
        private int version;
        private SpiritRebirthForm form = SpiritRebirthForm.Dormant;
        private Pawn original;
        private Pawn host;
        private Thing_SoulStone stone;
        private bool recoveryRequired;
        private int recoveryTicks;
        private int lastRecoveryTick = -1;
        private bool depletionPending;
        private Thing_SoulStone depletionStone;
        private int depletionRetryAt;
        private bool depletionWarningIssued;
        private bool healthFormDirty;
        private int healthFormRetryAt;
        private bool healthFormWarningIssued;
        private Map pendingDeathMap;
        private int pendingDeathId;
        internal bool HasPendingMapDeath(Pawn pawn)
            => IsStoredOwner(pawn) && pendingDeathMap != null && pendingDeathId > 0
                && (form == SpiritRebirthForm.Possessed || form == SpiritRebirthForm.Dormant);
        internal bool MatchesPendingMapDeath(Pawn pawn, Map map, int id)
            => IsStoredOwner(pawn) && id > 0 && pendingDeathId == id && ReferenceEquals(pendingDeathMap, map)
                && (form == SpiritRebirthForm.Possessed || form == SpiritRebirthForm.Dormant);
        internal bool BeginPendingMapDeath(Pawn pawn, Pawn deadHost, SpiritPendingHostCollapse receipt)
        {
            if (pendingDeathMap != null || pendingDeathId != 0 || receipt == null || !receipt.Registered
                || !ReferenceEquals(receipt.Original, pawn) || receipt.SourceHostId != deadHost?.thingIDNumber
                || !ReferenceEquals(receipt.Keeper.map, deadHost.Map) || (!receipt.Forced && !deadHost.Dead)
                || !CanEnterStone(pawn, deadHost, deadHost.Dead)) return false;
            // Authorization alone never clears the still-owning source graph.
            pendingDeathMap = receipt.Keeper.map; pendingDeathId = receipt.Id;
            return true;
        }
        internal bool SecurePendingMapDeath(Pawn pawn, SpiritPendingHostCollapse receipt)
        {
            if (receipt == null || !ReferenceEquals(receipt.Original, pawn) || !receipt.OwnsOriginal
                || !MatchesPendingMapDeath(pawn, receipt.Keeper.map, receipt.Id)) return false;
            if (form != SpiritRebirthForm.Dormant) { SpiritHostedAura.EndAuras(pawn); form = SpiritRebirthForm.Dormant; host = null; MarkHealthFormDirty(); }
            return true;
        }
        internal bool TryCompletePendingMapDeath(Pawn pawn, SpiritPendingHostCollapse receipt)
        {
            if (receipt == null || !receipt.Registered || !ReferenceEquals(receipt.Original, pawn) || !receipt.OwnsOriginal) return false;
            if (HasValidStone(pawn) && ReferenceEquals(stone, receipt.Stone)) return true;
            if (!SecurePendingMapDeath(pawn, receipt) || !StoneHolds(receipt.Stone, pawn)) return false;
            CommitStone(receipt.Stone);
            return true;
        }
        internal bool HealthFormDirty => healthFormDirty;
        internal int HealthFormRetryAt => healthFormRetryAt;
        internal void MarkHealthFormDirty() { healthFormDirty = true; healthFormRetryAt = 0; }
        internal void CompleteHealthFormRefresh() { healthFormDirty = false; healthFormRetryAt = 0; healthFormWarningIssued = false; }
        internal void DeferHealthFormRefresh(int tick) { healthFormRetryAt = tick + 250; }
        internal bool TakeHealthFormWarning()
        {
            if (healthFormWarningIssued) return false;
            healthFormWarningIssued = true;
            return true;
        }
        public bool DepletionPending => depletionPending;
        internal Thing_SoulStone DepletionStone => depletionStone;
        internal int DepletionRetryAt => depletionRetryAt;
        internal void DeferDepletionRetry(int tick) { depletionRetryAt = tick + 250; }
        internal bool TakeDepletionWarning()
        {
            if (depletionWarningIssued) return false;
            depletionWarningIssued = true;
            return true;
        }
        internal bool BeginDepletion(Pawn pawn)
        {
            if (!IsCurrent(pawn) || !SpiritRebirthFormPolicy.IsEthereal(pawn)) return false;
            depletionPending = true;
            return true;
        }
        internal void RememberDepletionStone(Thing_SoulStone candidate) { depletionStone = candidate; }

        public int Version => version;
        public SpiritRebirthForm Form => form;
        public Pawn Host => host;
        public Thing_SoulStone Stone => stone;
        public bool RecoveryRequired => recoveryRequired;
        public int RecoveryTicks => recoveryTicks;

        public SpiritRebirthState() { }
        internal SpiritRebirthState(Pawn original)
        {
            this.original = original;
            version = CurrentVersion;
            form = SpiritRebirthForm.Ethereal;
            MarkHealthFormDirty();
        }
        internal static SpiritRebirthState CreateRitualPending(Pawn original)
            => new SpiritRebirthState(original) { form = SpiritRebirthForm.Dormant };

        internal bool TryCommitRitualRelease(Pawn pawn, SpiritRebirthRitualTransaction receipt)
        {
            if (!IsCurrent(pawn) || form != SpiritRebirthForm.Dormant || host != null || stone != null
                || receipt == null || !receipt.CanCommitRelease(pawn)) return false;
            form = SpiritRebirthForm.Ethereal;
            MarkHealthFormDirty();
            return true;
        }

        // A disabled feature still owns the saved pawn. Classic possession must never
        // tick, release or destroy that pawn as though it were a classic spirit.
        internal static bool IsPaused(Pawn original) => !ClassProgressionFeature.Enabled &&
            ClassIdentity.StoredComponent(original)?.SpiritRebirth != null;

        public static bool TryGet(Pawn pawn, out SpiritRebirthState state)
        {
            if (ClassProgressionFeature.Enabled) return TryGetStored(pawn,out state);
            state = null;
            return false;
        }

        internal static bool TryGetStored(Pawn pawn, out SpiritRebirthState state)
        {
            state = ClassIdentity.StoredComponent(pawn)?.SpiritRebirth;
            if (state != null && state.version == CurrentVersion && ReferenceEquals(state.original, pawn)
                && pawn != null && !pawn.Destroyed && Enum.IsDefined(typeof(SpiritRebirthForm), state.form)) return true;
            state = null;
            return false;
        }

        private static HediffComp_SpiritPossession Possession(Pawn candidate)
            => candidate?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD)
                ?.TryGetComp<HediffComp_SpiritPossession>();

        private static bool Holds(Pawn candidate, Pawn spirit, bool allowDead = false)
        {
            var possession = Possession(candidate);
            var holder = possession?.GetDirectlyHeldThings();
            return candidate != null && candidate != spirit && (allowDead || !candidate.Dead) && !candidate.Destroyed
                && spirit != null && !spirit.Dead && !spirit.Destroyed && !spirit.Spawned
                && ReferenceEquals(possession?.parent?.pawn, candidate)
                && ReferenceEquals(possession?.SpiritPawn, spirit) && holder?.Contains(spirit) == true
                && ReferenceEquals(holder.Owner, possession)
                && ReferenceEquals(spirit.holdingOwner, holder);
        }

        private bool IsCurrent(Pawn pawn) => TryGet(pawn, out var current) && ReferenceEquals(current, this);
        private bool IsStoredOwner(Pawn pawn) => TryGetStored(pawn,out var current) && ReferenceEquals(current,this);
        public bool HasValidHost(Pawn pawn) => IsCurrent(pawn) && form == SpiritRebirthForm.Possessed && Holds(host, pawn);
        public bool HasValidStone(Pawn pawn) => IsStoredOwner(pawn) && form == SpiritRebirthForm.Stone && StoneHolds(stone, pawn);
        private static bool StoneHolds(Thing_SoulStone candidate, Pawn pawn)
        {
            var holder = candidate?.GetDirectlyHeldThings();
            return candidate != null && !candidate.Destroyed && pawn != null && !pawn.Dead && !pawn.Destroyed && !pawn.Spawned
                && ReferenceEquals(candidate.ContainedSpirit, pawn) && holder?.Contains(pawn) == true
                && ReferenceEquals(holder.Owner, candidate) && ReferenceEquals(pawn.holdingOwner, holder);
        }
        public bool CanVoluntarilyLeaveHost(Pawn pawn)
            => HasValidHost(pawn) && (!recoveryRequired || recoveryTicks >= RecoveryDurationTicks);

        public static bool TryGetPossessingSpirit(Pawn candidate, out Pawn spirit)
        {
            spirit = Possession(candidate)?.SpiritPawn;
            if (TryGet(spirit, out var state) && ReferenceEquals(state.host, candidate) && state.HasValidHost(spirit)) return true;
            spirit = null;
            return false;
        }

        // Future host integration must call once each game tick. Unobserved gaps
        // (including off-map/offline intervals) never count as time in a living host.
        internal bool TickRecovery(Pawn pawn, Pawn expectedHost, int tick)
        {
            if (!HasValidHost(pawn) || !ReferenceEquals(host, expectedHost))
            {
                lastRecoveryTick = -1;
                return false;
            }
            if (!recoveryRequired || tick < 0 || tick <= lastRecoveryTick) return false;
            bool advance = lastRecoveryTick >= 0 && tick - lastRecoveryTick == 1 && recoveryTicks < RecoveryDurationTicks;
            lastRecoveryTick = tick;
            if (advance) recoveryTicks++;
            return advance;
        }

        // Forced collapse only: false means energy depletion, true means host death.
        // Callers must validate the depletion event; this is never voluntary exit.
        internal bool CanEnterStone(Pawn pawn, Pawn expectedHost, bool hostDied)
        {
            if (!IsStoredOwner(pawn) || pawn.Dead) return false;
            if (form == SpiritRebirthForm.Ethereal)
                return !hostDied && expectedHost == null && host == null && SpiritRebirthFormPolicy.HasMapCustody(pawn);
            return form == SpiritRebirthForm.Possessed && ReferenceEquals(host, expectedHost) && host != null
                && host.Dead == hostDied && Holds(host, pawn, hostDied);
        }

        // Commit follows the holder's validated transfer. The original can only
        // be in one native container; saved references alone confer no custody.
        internal bool TryEnterStone(Pawn pawn, Thing_SoulStone destination, Pawn expectedHost, bool hostDied)
        {
            if (!IsStoredOwner(pawn) || !StoneHolds(destination, pawn)) return false;
            if (form == SpiritRebirthForm.Ethereal)
            {
                if (hostDied || expectedHost != null || host != null) return false;
            }
            else if (form != SpiritRebirthForm.Possessed || host == null || !ReferenceEquals(host, expectedHost)
                || host.Destroyed || host.Dead != hostDied
                || !ReferenceEquals(Possession(host)?.parent?.pawn, host)
                || Possession(host)?.GetDirectlyHeldThings()?.Contains(pawn) != false) return false;
            CommitStone(destination);
            return true;
        }

        private void CommitStone(Thing_SoulStone destination)
        {
            SpiritHostedAura.EndAuras(original);
            host = null;
            stone = destination;
            form = SpiritRebirthForm.Stone;
            MarkHealthFormDirty();
            depletionPending = false;
            depletionStone = null;
            depletionRetryAt = 0;
            depletionWarningIssued = false;
            recoveryRequired = true;
            recoveryTicks = 0;
            lastRecoveryTick = -1;
            pendingDeathMap = null;
            pendingDeathId = 0;
        }

        internal bool TryEnterHost(Pawn pawn, Pawn candidate)
        {
            if (!IsCurrent(pawn) || host != null || SpiritSummonedHost.IsSummoned(candidate) || !Holds(candidate, pawn)) return false;
            if (form == SpiritRebirthForm.Stone)
            {
                if (stone == null || stone.Destroyed || stone.GetDirectlyHeldThings().Count != 0) return false;
                recoveryRequired = true;
            }
            else if (form != SpiritRebirthForm.Ethereal || stone != null) return false;
            stone = null;
            host = candidate;
            if (ClassIdentity.TryGetState(pawn, out var progression) && progression.Find("rwom.summoner") is DisciplineProgress summoner)
                summoner.SummonerTrials.LastActive = false;
            form = SpiritRebirthForm.Possessed;
            MarkHealthFormDirty();
            lastRecoveryTick = -1;
            return true;
        }

        internal bool TryLeaveHost(Pawn pawn, Pawn expectedHost)
        {
            if (!IsCurrent(pawn) || form != SpiritRebirthForm.Possessed || host == null || !ReferenceEquals(host, expectedHost)
                || host.Dead || host.Destroyed || (recoveryRequired && recoveryTicks < RecoveryDurationTicks)
                || pawn.Dead || !SpiritRebirthFormPolicy.HasMapCustody(pawn)
                || Possession(expectedHost)?.GetDirectlyHeldThings()?.Contains(pawn) == true) return false;
            SpiritHostedAura.EndAuras(pawn);
            host = null;
            form = SpiritRebirthForm.Ethereal;
            MarkHealthFormDirty();
            lastRecoveryTick = -1;
            return true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref version, "version", 0);
            Scribe_Values.Look(ref form, "form", SpiritRebirthForm.Dormant);
            Scribe_References.Look(ref original, "originalPawn");
            Scribe_References.Look(ref host, "hostPawn");
            Scribe_References.Look(ref stone, "soulStone");
            Scribe_References.Look(ref pendingDeathMap, "pendingDeathMap");
            Scribe_Values.Look(ref pendingDeathId, "pendingDeathId");
            Scribe_Values.Look(ref recoveryRequired, "recoveryRequired", false);
            Scribe_Values.Look(ref recoveryTicks, "recoveryTicks", 0);
            Scribe_Values.Look(ref lastRecoveryTick, "lastRecoveryTick", -1);
            Scribe_Values.Look(ref healthFormDirty, "healthFormDirty", false);
            Scribe_Values.Look(ref healthFormRetryAt, "healthFormRetryAt", 0);
            Scribe_Values.Look(ref healthFormWarningIssued, "healthFormWarningIssued", false);
            Scribe_Values.Look(ref depletionPending, "depletionPending", false);
            Scribe_References.Look(ref depletionStone, "depletionStone");
            Scribe_Values.Look(ref depletionRetryAt, "depletionRetryAt", 0);
            Scribe_Values.Look(ref depletionWarningIssued, "depletionWarningIssued", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && version == 1)
            {
                // Foundation saves had no Stone form or recovery debt.
                if (form == SpiritRebirthForm.Stone) form = SpiritRebirthForm.Dormant;
                stone = null;
                recoveryRequired = false;
                recoveryTicks = 0;
                lastRecoveryTick = -1;
                version = CurrentVersion;
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit && version == CurrentVersion)
            {
                MarkHealthFormDirty(); // Rebuild native caches lazily, after ownership resolves.
                // Repair malformed counters conservatively, without awarding time.
                // A valid ongoing recovery keeps both progress and tick baseline.
                if (recoveryTicks < 0 || recoveryTicks > RecoveryDurationTicks)
                {
                    recoveryRequired = true;
                    recoveryTicks = 0;
                    lastRecoveryTick = -1;
                }
                if (lastRecoveryTick < -1) lastRecoveryTick = -1;
                if (recoveryTicks > 0) recoveryRequired = true;
                if (form == SpiritRebirthForm.Stone)
                {
                    recoveryRequired = true;
                    recoveryTicks = 0;
                    lastRecoveryTick = -1;
                }
            }
        }
    }
}
