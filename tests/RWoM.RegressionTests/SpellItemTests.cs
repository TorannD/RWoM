using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.SpellItems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class SpellItemTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in ExtendedLifecycleCases()) yield return test;
        yield return new("SpellItems.ComponentContractExists", _ =>
        {
            var assembly = typeof(CompAbilityUserMagic).Assembly;
            foreach (string name in new[] { "SpellItemKind", "CompProperties_SpellItem", "CompSpellItem" })
                Check.NotNull(assembly.GetType("TorannMagic.SpellItems." + name), "Spell item implementation must expose " + name);
        });
        foreach (var kind in Enum.GetValues<SpellItemKind>())
        {
            var selected = kind;
            yield return new("SpellItems.InventoryScrollNoMageOrLearning." + kind, _ => InWorld(pawn =>
            {
                var item = Item(selected, stack: 2); Hold(pawn, item);
                using var effects = EffectScope();
                Check.Equal(false, item.parent.Spawned, "held source is not spawned");
                Check.Equal(0, pawn.AllComps.Count, "ordinary pawn starts without mage or might comp");
                Check.Equal<string?>(null, item.UseRejection(pawn), "ordinary inventory holder may cast");
                int token = Begin(item, pawn);
                Check.Equal(true, item.TryCommit(pawn, token, Target), "successful fixed effect commits");
                Check.Equal(1, item.parent.stackCount, "one scroll consumed, not entire stack");
                Check.Equal(1, item.RemainingUses, "remaining scroll retains its one use");
                Check.Equal(1, effectCalls, "one effect dispatched");
                Check.Equal(selected, lastSpell, "requested spell forwarded to effect boundary");
                Check.Equal(0, pawn.AllComps.Count, "casting creates no magic comp or learned ability owner");
                Check.Equal(false, item.IsBusy, "completion clears lock");
                Check.Equal(220, item.ReadyTick, "cooldown starts at successful completion tick");
                Check.Equal(0, item.TryBegin(pawn), "same stack cannot bypass cooldown");
                Tick(220);
                Check.Equal(true, item.TryCommit(pawn, Begin(item, pawn), Target), "last scroll may be used after cooldown");
                Check.Equal(true, item.parent.Destroyed, "last scroll destroyed");
                Check.Equal(false, pawn.inventory.innerContainer.Contains(item.parent), "destroyed scroll removed from real holder");
            }));
        }
        yield return new("SpellItems.ExactHolderAndCompletionOwnership", _ => InWorld(pawn =>
        {
            var other = Pawn(2); var item = Item(stack: 2); Hold(pawn, item);
            using var effects = EffectScope();
            Check.Equal(false, item.IsOwnedBy(other), "another colonist does not own this exact item");
            Check.Equal(0, item.TryBegin(other), "wrong holder cannot reserve item");
            int token = Begin(item, pawn);
            Check.Equal(false, item.Matches(other, token), "token bound to exact pawn");
            Check.Equal(false, item.TryCommit(other, token, Target), "wrong pawn cannot spend owner token");
            item.Release(other, token);
            Check.Equal(true, item.Matches(pawn, token), "wrong pawn cannot release owner lock");
            pawn.inventory.innerContainer.Remove(item.parent); Hold(other, item);
            Check.Equal(false, item.Matches(pawn, token), "ownership lost during warmup invalidates completion");
            Check.Equal(false, item.TryCommit(pawn, token, Target), "former owner cannot commit transferred item");
            Check.Equal(0, effectCalls, "ownership failures never dispatch effect");
            Check.Equal(2, item.parent.stackCount, "ownership failures consume nothing");
            item.Release(pawn, token);
            Check.Equal(true, item.TryCommit(other, Begin(item, other), Target), "new holder can start a fresh use");
        }));
        yield return new("SpellItems.InventoryTransferAwayAndBackCannotReviveToken", _ => InWorld(pawn =>
        {
            var item = Item(stack: 2); Hold(pawn, item); using var effects = EffectScope();
            using var removal = new Patches("inventory-removal");
            var patch = Check.NotNull(typeof(CompSpellItem).Assembly.GetType("TorannMagic.SpellItems.SpellItemInventoryRemovalPatch"), "inventory ownership patch exists");
            removal.Postfix(typeof(Pawn_InventoryTracker).GetMethod("Notify_ItemRemoved")!, patch.GetMethod("Postfix")!);
            int token = Begin(item, pawn);
            pawn.inventory.innerContainer.Remove(item.parent); Hold(pawn, item);
            Check.Equal(false, item.IsBusy, "real inventory removal invalidates source lock");
            Check.Equal(false, item.TryCommit(pawn, token, Target), "returning exact source cannot revive old token");
            Check.Equal(2, item.parent.stackCount, "transfer does not spend scroll");
            Check.Equal(0, effectCalls, "old token cannot dispatch effect");
            Check.Equal(true, item.TryCommit(pawn, Begin(item, pawn), Target), "fresh reservation after return succeeds");
        }));
        yield return new("SpellItems.StaffRequiresPrimaryAndTenCommitsConsumeTen", _ => InWorld(pawn =>
        {
            var item = Item(uses: 10, equipped: true); Hold(pawn, item);
            using var effects = EffectScope();
            Check.Equal(false, item.IsOwnedBy(pawn), "inventory staff is not equipped");
            Check.Equal(0, item.TryBegin(pawn), "staff cannot cast from inventory");
            Check.Equal(0, SpellItemCasting.Sources(pawn).Count(), "inventory staff not offered as cast source");
            pawn.inventory.innerContainer.Remove(item.parent); Equip(pawn, item);
            Check.Same(item.parent, pawn.equipment.Primary, "actual primary equipment owns staff");
            Check.Equal(1, SpellItemCasting.Sources(pawn).Count(), "equipped staff offered once");
            for (int cast = 1; cast <= 10; cast++)
            {
                Tick(100 + (cast - 1) * 120);
                int token = Begin(item, pawn);
                Check.Equal(true, item.TryCommit(pawn, token, Target), "staff cast " + cast);
                Check.Equal(10 - cast, item.RemainingUses, "exactly one charge per commit " + cast);
                Check.Equal(cast == 10, item.parent.Destroyed, "staff survives until tenth successful cast");
                Check.Equal(false, item.TryCommit(pawn, token, Target), "completed token cannot repeat");
            }
            Check.Equal(10, effectCalls, "ten charges produce ten effects, never two casts of five charges");
            Check.Equal(false, pawn.equipment.Contains(item.parent), "empty staff removed from equipment");
        }));
        yield return new("SpellItems.CancellationAndStaleTokensDoNotSpend", _ => InWorld(pawn =>
        {
            var item = Item(stack: 3); Hold(pawn, item); using var effects = EffectScope();
            int canceled = Begin(item, pawn);
            Check.Equal(0, item.TryBegin(pawn), "duplicate begin cannot replace live token");
            item.Release(pawn, canceled + 1);
            Check.Equal(true, item.Matches(pawn, canceled), "wrong token cannot unlock cast");
            item.Release(pawn, canceled);
            Check.Equal(false, item.IsBusy, "cancel unlocks source");
            Check.Equal(false, item.TryCommit(pawn, canceled, Target), "canceled token cannot commit");
            Check.Equal(3, item.parent.stackCount, "cancel never consumes scroll");
            Check.Equal(0, item.ReadyTick, "cancel starts no cooldown");
            int current = Begin(item, pawn);
            Check.NotEqual(canceled, current, "new reservation has distinct identity");
            item.Release(pawn, canceled);
            Check.Equal(true, item.Matches(pawn, current), "late cleanup cannot cancel new cast");
            Check.Equal(true, item.TryCommit(pawn, current, Target), "fresh token succeeds");
            Check.Equal(false, item.TryCommit(pawn, current, Target), "same token cannot charge twice");
            Check.Equal(2, item.parent.stackCount, "one successful token consumes one scroll");
            Check.Equal(1, effectCalls, "stale/canceled tokens never apply effect");
        }));
        foreach (bool rejected in new[] { true, false })
        {
            bool targetRejected = rejected;
            yield return new("SpellItems.Failed" + (rejected ? "Target" : "Effect") + "NeverPays", _ => InWorld(pawn =>
            {
                var item = Item(uses: 10, equipped: true); Equip(pawn, item); using var effects = EffectScope();
                targetRejection = targetRejected ? "invalid target" : null; effectSucceeds = false;
                int token = Begin(item, pawn);
                Check.Equal(false, item.TryCommit(pawn, token, Target), "failure does not commit");
                Check.Equal(10, item.RemainingUses, "failed completion spends no charge");
                Check.Equal(0, item.ReadyTick, "failed completion starts no cooldown");
                Check.Equal(targetRejected ? 0 : 1, effectCalls, "validation precedes effect dispatch");
                item.Release(pawn, token);
                targetRejection = null; effectSucceeds = true;
                Check.Equal(true, item.TryCommit(pawn, Begin(item, pawn), Target), "failure does not poison future use");
                Check.Equal(9, item.RemainingUses, "recovered successful attempt pays once");
            }));
        }
        yield return new("SpellItems.ReentrantCompletionCannotApplyOrPayTwice", _ => InWorld(pawn =>
        {
            var item = Item(uses: 10, equipped: true); Equip(pawn, item); using var effects = EffectScope();
            int token = Begin(item, pawn);
            duringEffect = () =>
            {
                Check.Equal(false, item.TryCommit(pawn, token, Target), "effect callback cannot reenter same commit");
                item.Release(pawn, token);
                Check.Equal(0, item.TryBegin(pawn), "callback cannot release and reopen item while committing");
            };
            Check.Equal(true, item.TryCommit(pawn, token, Target), "outer completion succeeds");
            Check.Equal(1, effectCalls, "exactly one effect despite reentry");
            Check.Equal(9, item.RemainingUses, "exactly one payment despite reentry");
        }));
        yield return new("SpellItems.ManipulationLossBlocksBeginAndCompletion", _ => InWorld(pawn =>
        {
            var item = Item(stack: 2); Hold(pawn, item); using var effects = EffectScope();
            int token = Begin(item, pawn); Capacity(pawn, 0);
            Check.Equal(false, item.Matches(pawn, token), "capacity lost during preparation");
            Check.Equal(false, item.TryCommit(pawn, token, Target), "incapable caster cannot finish");
            item.Release(pawn, token);
            Check.Equal(0, item.TryBegin(pawn), "incapable holder cannot start");
            Check.Equal(0, effectCalls, "no gameplay dispatch");
            Check.Equal(2, item.parent.stackCount, "no resource loss");
        }));
        yield return new("SpellItems.SplitCopiesCooldownAndCancelsSourceLock", _ => InWorld(pawn =>
        {
            var source = Item(stack: 4); Hold(pawn, source); using var effects = EffectScope();
            Check.Equal(true, source.TryCommit(pawn, Begin(source, pawn), Target), "initial cast");
            Tick(220); int token = Begin(source, pawn);
            var piece = Item(); source.PostSplitOff(piece.parent);
            Check.Equal(1, piece.RemainingUses, "split piece copies initialized uses");
            Check.Equal(220, piece.ReadyTick, "split never clears cooldown");
            Check.Equal(false, source.IsBusy, "splitting cancels original lock");
            Check.Equal(false, piece.IsBusy, "split piece does not inherit live lock");
            Check.Equal(false, source.TryCommit(pawn, token, Target), "pre-split token cannot complete");
            Check.Equal(3, source.parent.stackCount, "PostSplitOff itself spends no additional scroll");
            Check.Equal(true, source.AllowStackWith(piece.parent), "identical idle cooldowns may stack");
            Check.Equal(false, source.AllowStackWith(Item().parent), "different cooldown cannot be laundered by stacking");
            int next = Begin(source, pawn);
            Check.Equal(false, source.AllowStackWith(piece.parent), "busy source cannot stack");
            Check.Equal(false, piece.AllowStackWith(source.parent), "busy other cannot stack");
            source.Release(pawn, next);
            Check.Equal(false, Item(uses: 10, equipped: true).AllowStackWith(Item(uses: 10, equipped: true).parent), "staff never stacks");
        }));
        yield return new("SpellItems.UnequipReequipPreservesUsesCooldownAndInvalidatesToken", _ => InWorld(pawn =>
        {
            var item = Item(uses: 10, equipped: true); Equip(pawn, item); using var effects = EffectScope();
            Check.Equal(true, item.TryCommit(pawn, Begin(item, pawn), Target), "first charge");
            pawn.equipment.Remove(item.parent); Hold(pawn, item);
            pawn.inventory.innerContainer.Remove(item.parent); Equip(pawn, item);
            Check.Equal(9, item.RemainingUses, "equip does not refill");
            Check.Equal(220, item.ReadyTick, "equip does not reset cooldown");
            Check.Equal(0, item.TryBegin(pawn), "reequip cannot bypass cooldown");
            Tick(220); int token = Begin(item, pawn); pawn.equipment.Remove(item.parent); Equip(pawn, item);
            Check.Equal(false, item.IsBusy, "real equipment removal releases lock");
            Check.Equal(false, item.TryCommit(pawn, token, Target), "reequipping cannot revive old token");
        }));
        yield return new("SpellItems.RealScribePreservesUsesCooldownAndReservationIdentity", f => InWorld(pawn =>
        {
            var item = Item(uses: 10, equipped: true); Equip(pawn, item); using var effects = EffectScope();
            Check.Equal(true, item.TryCommit(pawn, Begin(item, pawn), Target), "first charge");
            string cooldown = Save(f, item);
            pawn.equipment.Remove(item.parent); item = Item(uses: 10, equipped: true); Equip(pawn, item);
            Load(cooldown, item, pawn);
            Check.Equal(9, item.RemainingUses, "actual Scribe restores initialized charge count");
            Check.Equal(220, item.ReadyTick, "actual Scribe restores cooldown");
            Check.Equal(0, item.TryBegin(pawn), "loading cannot bypass pending cooldown");
            Tick(220); int token = Begin(item, pawn); string active = Save(f, item);
            pawn.equipment.Remove(item.parent); item = Item(uses: 10, equipped: true); Equip(pawn, item);
            Load(active, item, pawn);
            Check.Equal(true, item.IsBusy, "active reservation survives Scribe");
            Check.Same(pawn, item.ActivePawn, "real Scribe reference resolves supplied pawn");
            Check.Equal(token, item.ActiveToken, "active token preserved");
            Check.Equal(true, item.Matches(pawn, token), "loaded owned reservation usable");
            item.Release(pawn, token); int fresh = Begin(item, pawn);
            Check.Equal(token + 1, fresh, "next token counter survives save, never reuses stale identity");
            Check.Equal(true, item.TryCommit(pawn, fresh, Target), "loaded item pays normally");
            Check.Equal(8, item.RemainingUses, "load did not refill or double spend");
        }));
        yield return new("SpellItems.QueuedSubmissionAcquiresTokenOnlyWhenWarmupActuallyStarts", _ => InWorld(pawn =>
        {
            var item = Item(stack: 3); Hold(pawn, item); using var effects = EffectScope();
            if (DefDatabase<JobDef>.GetNamedSilentFail("TM_SpellItemCast") == null)
                DefDatabase<JobDef>.Add(new JobDef { defName = "TM_SpellItemCast", driverClass = typeof(JobDriver_SpellItemCast) });
            using var submission = new Patches("queued-submission");
            submission.Prefix(typeof(Pawn_JobTracker).GetMethod("TryTakeOrderedJob")!, nameof(CaptureQueuedJob));
            using var end = new Patches("queued-engine-end"); end.Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!, nameof(EndJob));
            queuedJob = null;
            Check.Equal(true, SpellItemCasting.TryStart(pawn, item.parent, Target), "ordinary holder may submit delayed order");
            var discarded = Check.NotNull(queuedJob, "real TryStart submits a job");
            Check.Equal<Job?>(null, pawn.CurJob, "accepted queued request has not become current job");
            Check.Equal(0, discarded.count, "queued request carries no reservation token");
            Check.Equal(false, item.IsBusy, "submission cannot lock source while waiting in queue");
            Check.Equal(0, item.ActiveToken, "no active token until real preparation starts");
            Check.Equal(3, item.parent.stackCount, "queueing does not spend source");
            Check.Equal(0, item.ReadyTick, "queueing starts no cooldown");
            queuedJob = null; // Discard without any JobDriver or cleanup, as vanilla queue removal can do.
            Check.Equal<string?>(null, item.UseRejection(pawn), "discarded queued request leaves no poisoned lock");
            Check.Equal(true, SpellItemCasting.TryStart(pawn, item.parent, Target), "fresh submission after discard accepted");
            var request = Check.NotNull(queuedJob, "replacement queued job captured");
            Check.NotEqual(discarded, request, "replacement is a fresh job");
            Check.Equal(0, request.count, "fresh queued request is still unreserved");
            var driver = Driver(pawn, item, request);
            Check.Equal(0, request.count, "driver setup alone cannot reserve source");
            Check.Equal(false, item.IsBusy, "no reservation before wait init");
            driver.ReadyForNextToil();
            Check.NotEqual(0, request.count, "actual wait init writes reservation into saved Job.count");
            Check.Equal(request.count, item.ActiveToken, "physical source owns actual running job token");
            Check.Same(pawn, item.ActivePawn, "actual wait binds caster");
            Check.Equal(true, driver.CanContinue, "newly reserved running job is valid");
            Check.Equal(60, driver.ticksLeftThisToil, "real warmup starts full preparation delay");
            for (int tick = 1; tick < 60; tick++) { Tick(100 + tick); driver.DriverTick(); }
            Check.Equal(0, effectCalls, "queued or warming request applies no early effect");
            Check.Equal(3, item.parent.stackCount, "no payment before completed warmup");
            Tick(160); driver.DriverTick();
            Check.Equal(1, effectCalls, "real queued-to-running job completes exactly once");
            Check.Equal(2, item.parent.stackCount, "one completion consumes one scroll");
            Check.Equal(280, item.ReadyTick, "successful completion starts cooldown");
            Check.Equal(false, item.IsBusy, "completion releases running reservation");
            Check.Equal(JobCondition.Succeeded, lastEnd, "queued-to-running job completes successfully");
            Toils(driver)[1].initAction();
            Check.Equal(1, effectCalls, "repeated finish does not duplicate queued effect");
            Check.Equal(2, item.parent.stackCount, "repeated finish does not duplicate payment");
            queuedJob = null;
        }));
        yield return new("SpellItems.EngineWarmupCompletesOnceAndCancellationDoesNotPay", _ => InWorld(pawn =>
        {
            var item = Item(stack: 3); Hold(pawn, item); using var effects = EffectScope();
            using var end = new Patches("engine-end"); end.Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!, nameof(EndJob));
            var driver = Driver(pawn, item); driver.ReadyForNextToil();
            Check.Equal(60, driver.ticksLeftThisToil, "engine enters sixty-tick warmup");
            for (int tick = 1; tick < 60; tick++) { Tick(100 + tick); driver.DriverTick(); }
            Check.Equal(0, effectCalls, "nothing applied before final warmup tick");
            Check.Equal(3, item.parent.stackCount, "warmup pays nothing");
            Tick(160); driver.DriverTick();
            Check.Equal(1, effectCalls, "engine completes one effect");
            Check.Equal(2, item.parent.stackCount, "engine completion consumes one scroll");
            Check.Equal(280, item.ReadyTick, "completion, not request, starts cooldown");
            Toils(driver)[1].initAction();
            Check.Equal(1, effectCalls, "duplicate finish action idempotent");
            Check.Equal(JobCondition.Succeeded, lastEnd, "engine ends completed job successfully");
            Tick(280); driver = Driver(pawn, item); int token = driver.job.count; driver.ReadyForNextToil();
            for (int tick = 0; tick < 20; tick++) driver.DriverTick();
            ((JobDriver)driver).Cleanup(JobCondition.InterruptForced);
            Check.Equal(false, item.IsBusy, "base engine cleanup runs registered release action");
            Check.Equal(false, item.TryCommit(pawn, token, Target), "canceled job token invalidated");
            Check.Equal(2, item.parent.stackCount, "interrupted warmup does not consume scroll");
            Check.Equal(280, item.ReadyTick, "interruption does not move cooldown");
        }));
        yield return new("SpellItems.RealScribeResumesPhysicalItemWarmupWithoutRestartOrDoublePay", f => InWorld(pawn =>
        {
            var item = Item(uses: 10, equipped: true); Equip(pawn, item); using var effects = EffectScope();
            using var end = new Patches("saved-engine-end"); end.Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!, nameof(EndJob));
            Check.Equal(true, item.TryCommit(pawn, Begin(item, pawn), Target), "spend first charge before saving later warmup");
            Tick(220); var driver = Driver(pawn, item); int token = driver.job.count; driver.ReadyForNextToil();
            for (int tick = 1; tick <= 20; tick++) { Tick(220 + tick); driver.DriverTick(); }
            Check.Equal(40, driver.ticksLeftThisToil, "twenty of sixty preparation ticks elapsed");
            Check.Equal(9, item.RemainingUses, "in-progress cast has not paid");
            string file = Path.Combine(f.WorkingDirectory, "item-warmup.xml");
            var envelope = new WarmupEnvelope(pawn, item) { Job = driver.job, Driver = driver };
            // Scope: actual component state, Job.count and driver timing/identity. The
            // existing pawn and physical item references are supplied by the envelope;
            // this is deliberately not a full pawn, item-definition or map save/load.
            try
            {
                Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref envelope, "cast", pawn, item); Scribe.saver.FinalizeSaving();
            }
            finally { Scribe.ForceStop(); }
            pawn.equipment.Remove(item.parent); item = Item(uses: 10, equipped: true); Equip(pawn, item);
            envelope = null;
            try
            {
                Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref envelope, "cast", pawn, item);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(pawn); directory.RegisterLoaded(item.parent); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            driver = Check.NotNull(envelope, "real Scribe restored warmup envelope").Driver;
            var wait = Toils(driver)[0]; wait.preTickActions?.Clear(); wait.preTickIntervalActions?.Clear();
            Check.Same(item.parent, driver.job.targetA.Thing, "loaded job uses supplied held physical source");
            Check.Same(pawn, driver.job.targetC.Thing, "loaded job retains caster identity");
            Check.Equal(token, driver.job.count, "real Job.count retains reservation token");
            Check.Equal(token, item.ActiveToken, "component reservation matches loaded job token");
            Check.Same(pawn, item.ActivePawn, "component caster resolved through real Scribe cross-reference");
            Check.Equal(9, item.RemainingUses, "load preserves prior charge payment");
            Check.Equal(220, item.ReadyTick, "load preserves prior cooldown deadline");
            Check.Equal(0, driver.CurToilIndex, "same preparation toil restored");
            Check.Equal(40, driver.ticksLeftThisToil, "load does not restart warmup");
            Check.Equal(true, driver.CanContinue, "loaded job and item reservation remain mutually valid");
            for (int tick = 1; tick < 40; tick++) { Tick(240 + tick); driver.DriverTick(); }
            Check.Equal(1, effectCalls, "remaining preparation still required after loading");
            Check.Equal(9, item.RemainingUses, "no precompletion payment after loading");
            Tick(280); driver.DriverTick();
            Check.Equal(2, effectCalls, "loaded engine continuation applies exactly one new effect");
            Check.Equal(8, item.RemainingUses, "loaded engine continuation pays one charge");
            Check.Equal(400, item.ReadyTick, "resumed completion starts cooldown at actual completion");
            Check.Equal(false, item.IsBusy, "resumed completion releases reservation");
            Check.Equal(JobCondition.Succeeded, lastEnd, "resumed job completes successfully");
            Toils(driver)[1].initAction();
            Check.Equal(2, effectCalls, "repeated loaded finish cannot apply twice");
            Check.Equal(8, item.RemainingUses, "repeated loaded finish cannot pay twice");
        }));
        yield return new("SpellItems.ShippedRecipesItemsAndDeveloperEntries", DefinitionContracts);
    }

    static readonly LocalTargetInfo Target = new(new IntVec3(4, 0, 4));
    static int effectCalls;
    static bool effectSucceeds;
    static string? targetRejection;
    static SpellItemKind lastSpell;
    static Action? duringEffect;
    static JobCondition lastEnd;
    static Job? queuedJob;
    static bool CaptureQueuedJob(Job job, ref bool __result)
    {
        queuedJob = job; __result = true; return false;
    }
    static Patches EffectScope()
    {
        effectCalls = 0; effectSucceeds = true; targetRejection = null; duringEffect = null;
        var scope = new Patches("effects");
        // Only target/effect dispatch is replaced. Real ownership, health, tokens, payment,
        // holder removal, cooldown, destruction and Scribe remain under test.
        scope.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.TargetRejection))!, nameof(ValidateTarget));
        scope.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.TryApply))!, nameof(ApplyEffect));
        return scope;
    }
    static bool ValidateTarget(ref string? __result) { __result = targetRejection; return false; }
    static bool ApplyEffect(Pawn pawn, LocalTargetInfo target, CompProperties_SpellItem props, ref bool __result)
    {
        Check.Equal(true, pawn.Spawned, "effect receives actual spawned caster");
        Check.Equal(Target.Cell, target.Cell, "effect receives requested target");
        effectCalls++; lastSpell = props.spell; duringEffect?.Invoke(); __result = effectSucceeds; return false;
    }
    static bool EndJob(Pawn_JobTracker __instance, JobCondition condition)
    {
        lastEnd = condition; Set(__instance, "curJob", null); return false;
    }
    static int Begin(CompSpellItem item, Pawn pawn)
    {
        int token = item.TryBegin(pawn); Check.NotEqual(0, token, "eligible item obtains reservation");
        Check.Equal(true, item.Matches(pawn, token), "reservation matches owner"); return token;
    }
    static void Tick(int tick) => Set(Find.TickManager, "ticksGameInt", tick);
    static CompSpellItem Item(SpellItemKind kind = SpellItemKind.Fireball, int uses = 1, bool equipped = false, int stack = 1)
    {
        var props = new CompProperties_SpellItem { spell = kind, maxUses = uses, requiresEquipped = equipped };
        var def = Blank<ThingDef>(); def.defName = "SpellItemFixture"; def.label = "spell item";
        def.thingClass = typeof(ThingWithComps); def.category = ThingCategory.Item; def.destroyable = true;
        def.stackLimit = equipped ? 1 : 25; def.equipmentType = equipped ? EquipmentType.Primary : EquipmentType.None;
        def.comps = [props];
        var thing = new ThingWithComps { def = def, stackCount = stack, thingIDNumber = 1000 };
        var comp = new CompSpellItem { parent = thing }; comp.Initialize(props);
        var comps = new List<ThingComp> { comp };
        if (equipped)
        {
            var equippable = new CompEquippable { parent = thing };
            equippable.Initialize(new CompProperties { compClass = typeof(CompEquippable) });
            // Empty initialized verbs avoid unrelated melee-definition generation/graphics.
            Set(equippable.verbTracker, "verbs", new List<Verb>()); comps.Add(equippable);
        }
        Set(thing, "comps", comps); comp.PostPostMake(); return comp;
    }
    static void Hold(Pawn pawn, CompSpellItem item) => Check.Equal(true, pawn.inventory.innerContainer.TryAdd(item.parent, false), "real inventory accepts exact item");
    static void Equip(Pawn pawn, CompSpellItem item)
    {
        pawn.equipment.AddEquipment(item.parent); Check.Same(item.parent, pawn.equipment.Primary, "real equipment holds exact item");
    }
    static Pawn Pawn(int id = 1)
    {
        var race = Blank<ThingDef>(); race.defName = "SpellItemHuman";
        race.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = race, kindDef = new PawnKindDef { race = race }, thingIDNumber = id };
        pawn.health = new Pawn_HealthTracker(pawn); pawn.inventory = new Pawn_InventoryTracker(pawn);
        pawn.equipment = new Pawn_EquipmentTracker(pawn); pawn.jobs = new Pawn_JobTracker(pawn);
        pawn.stances = new Pawn_StanceTracker(pawn); pawn.pather = new Pawn_PathFollower(pawn);
        pawn.mindState = Blank<Pawn_MindState>(); pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
        Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
        Capacity(pawn, 1); return pawn;
    }
    static bool capacityInitialized;
    static void Capacity(Pawn pawn, float value)
    {
        if (!capacityInitialized)
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
            capacityInitialized = true;
        }
        var capacity = PawnCapacityDefOf.Manipulation;
        if (capacity == null)
        {
            capacity = new PawnCapacityDef { defName = "Manipulation", minForCapable = .1f };
            DefDatabase<PawnCapacityDef>.Add(capacity); PawnCapacityDefOf.Manipulation = capacity;
        }
        // Precompute only the engine's expensive body-capacity calculation. Production
        // still calls real CapableOf/GetLevel, including the incapacity boundary test.
        var field = typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object cache = Activator.CreateInstance(field.FieldType)!;
        var indexer = field.FieldType.GetProperty("Item", [typeof(PawnCapacityDef)])!;
        object entry = indexer.GetValue(cache, [capacity])!;
        Set(entry, "status", Enum.Parse(entry.GetType().GetField("status")!.FieldType, "Cached")); Set(entry, "value", value);
        field.SetValue(pawn.health.capacities, cache);
    }
    static void InWorld(Action<Pawn> body)
    {
        var old = Current.Game;
        try
        {
            Current.Game = Blank<Game>(); Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = Blank<Map>(); map.info = new MapInfo { Size = new IntVec3(10, 1, 10) }; map.cellIndices = new CellIndices(map);
            // ThingOwner/destruction only query thing designations. Avoid constructing
            // unrelated renderer DefMaps before headless DesignationDefs are loaded.
            map.designationManager = Blank<DesignationManager>();
            Set(map.designationManager, "thingDesignations", new Dictionary<Thing, List<Designation>>());
            map.reservationManager = new ReservationManager(map);
            map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
            Set(Current.Game, "maps", new List<Map> { map }); Tick(100); body(Pawn());
        }
        finally { duringEffect = null; Scribe.ForceStop(); Current.Game = old; }
    }
    static JobDriver_SpellItemCast Driver(Pawn pawn, CompSpellItem item, Job? submittedJob = null)
    {
        var def = DefDatabase<JobDef>.GetNamedSilentFail("TM_SpellItemCast");
        if (def == null) { def = new JobDef { defName = "TM_SpellItemCast", driverClass = typeof(JobDriver_SpellItemCast) }; DefDatabase<JobDef>.Add(def); }
        var job = submittedJob ?? JobMaker.MakeJob(def, item.parent, Target, pawn);
        if (submittedJob == null) job.count = Begin(item, pawn);
        var driver = new JobDriver_SpellItemCast { pawn = pawn, job = job };
        Set(pawn.jobs, "curJob", job); Set(pawn.jobs, "curDriver", driver);
        typeof(JobDriver).GetMethod("SetupToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null);
        // Exclude only progress-bar drawing callbacks, not engine delay/fail/finish logic.
        var wait = Toils(driver)[0]; wait.preTickActions?.Clear(); wait.preTickIntervalActions?.Clear(); return driver;
    }
    static List<Toil> Toils(JobDriver driver) => (List<Toil>)typeof(JobDriver).GetField("toils", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver)!;
    public sealed class WarmupEnvelope(Pawn pawn, CompSpellItem component) : IExposable
    {
        public Job Job = null!;
        public JobDriver_SpellItemCast Driver = null!;
        public void ExposeData()
        {
            // Keep component references under the envelope's real Scribe parent, just
            // as ThingWithComps calls each component's PostExposeData in an item save.
            component.PostExposeData();
            Scribe_Deep.Look(ref Job, "job"); Scribe_Deep.Look(ref Driver, "driver");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Job.targetA = component.parent; Job.targetC = pawn;
                Driver.pawn = pawn; Driver.job = Job;
                Set(pawn.jobs, "curJob", Job); Set(pawn.jobs, "curDriver", Driver);
            }
        }
    }
    public sealed class ComponentEnvelope(CompSpellItem component) : IExposable
    {
        public void ExposeData() => component.PostExposeData();
    }
    static string Save(RuntimeFixture f, CompSpellItem component)
    {
        string file = Path.Combine(f.WorkingDirectory, "component.xml");
        var envelope = new ComponentEnvelope(component);
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref envelope, "item", component); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        return File.ReadAllText(file);
    }
    static void Load(string xml, CompSpellItem component, Pawn pawn)
    {
        // The component state envelope deliberately supplies an existing held item and
        // pawn, rather than claiming a whole map/pawn round trip. Pawn reference lookup
        // and all component loading/cross-reference/PostLoadInit passes remain real.
        string file = Path.Combine(Path.GetDirectoryName(typeof(SpellItemTests).Assembly.Location)!, "component-load.xml");
        File.WriteAllText(file, xml); ComponentEnvelope? envelope = null;
        try
        {
            Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref envelope, "item", component);
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(pawn); Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        Check.NotNull(envelope, "actual Scribe loaded component envelope");
    }
    static void DefinitionContracts(RuntimeFixture f)
    {
        var doc = new XmlDocument(); doc.Load(Path.Combine(f.Repository, "v1.7/Defs/SpellItems/SpellItems.xml"));
        XmlInheritance.Clear();
        try
        {
            foreach (XmlNode node in doc.SelectNodes("Defs/RecipeDef")!) XmlInheritance.TryRegister(node, null);
            XmlInheritance.Resolve();
            foreach (string spell in new[] { "Fireball", "Heal", "Blink" })
            foreach (bool staff in new[] { false, true })
            {
                string itemName = "TM_" + (staff ? "SpellStaff_" : "CastScroll_") + spell;
                string recipeName = "TM_Make" + (staff ? "SpellStaff_" : "CastScroll_") + spell;
                var node = Check.NotNull(doc.SelectSingleNode("Defs/RecipeDef[defName='" + recipeName + "']"), recipeName + " exists");
                var recipe = XElement.Parse(XmlInheritance.GetResolvedNodeFor(node).OuterXml);
                Check.Equal("TM_MagicScribing", recipe.Element("researchPrerequisite")!.Value, recipeName + " research gate");
                Check.Equal("TableMagicPrinter", recipe.Element("recipeUsers")!.Element("li")!.Value, recipeName + " scribing table");
                Check.Equal(1, recipe.Element("products")!.Elements().Count(), recipeName + " one output type");
                Check.Equal("1", recipe.Element("products")!.Element(itemName)!.Value, recipeName + " produces one item");
                var extension = recipe.Element("modExtensions")!.Elements("li").Single(x => (string?)x.Attribute("Class") == "TorannMagic.SpellItems.SpellItemRecipeRequirement");
                Check.Equal("TM_" + spell, extension.Element("requiredSpell")!.Value, recipeName + " learned family gate");
                var ingredients = recipe.Element("ingredients")!.Elements("li").ToDictionary(x => x.Element("filter")!.Element("thingDefs")!.Element("li")!.Value, x => (int)x.Element("count")!);
                Check.Equal(staff ? 4 : 2, ingredients.Count, recipeName + " explicit ingredient types");
                Check.Equal(staff ? 70 : 12, ingredients["RawMagicyte"], recipeName + " magicyte cost");
                if (staff)
                {
                    Check.Equal(1, ingredients["TM_CastScroll_" + spell], recipeName + " consumes exactly one matching scroll");
                    Check.Equal(30, ingredients["WoodLog"], recipeName + " wood cost");
                    Check.Equal(spell == "Fireball" ? 10 : 20, ingredients[spell == "Fireball" ? "Gold" : "Jade"], recipeName + " binding material cost");
                }
                else Check.Equal(8, ingredients["Cloth"], recipeName + " cloth cost");
                var item = Check.NotNull(doc.SelectSingleNode("Defs/ThingDef[defName='" + itemName + "']"), itemName + " exists");
                var compNode = item.SelectSingleNode("comps/li[@Class='TorannMagic.SpellItems.CompProperties_SpellItem']")!;
                var props = DirectXmlToObject.ObjectFromXml<CompProperties_SpellItem>(compNode, false);
                Check.Equal(Enum.Parse<SpellItemKind>(spell), props.spell, itemName + " fixed spell");
                Check.Equal(staff ? 10 : 1, props.maxUses, itemName + " uses");
                Check.Equal(staff, props.requiresEquipped, itemName + " source location");
                Check.Equal(typeof(CompSpellItem), props.compClass, itemName + " component resolved");
            }
            var debug = Check.NotNull(typeof(CompSpellItem).Assembly.GetType("TorannMagic.SpellItems.SpellItemDebug"), "developer spawn menu service exists");
            foreach (string method in new[] { "SpawnCastScroll", "SpawnChargedStaff" })
                Check.Equal(true, debug.GetMethod(method)!.CustomAttributes.Any(a => a.AttributeType.Name == "DebugActionAttribute"), method + " discoverable in developer menu");
        }
        finally { XmlInheritance.Clear(); }
    }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static void Set(object target, string name, object? value)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new InvalidOperationException("Missing fixture field " + name);
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly object instance; readonly Type type; readonly string id;
        public Patches(string suffix) { id = "rwom.regression.spell-items." + suffix; type = assembly.GetType("HarmonyLib.Harmony")!; instance = Activator.CreateInstance(type, id)!; }
        public void Prefix(MethodBase original, string name)
        {
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpellItemTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            type.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5).Invoke(instance, [original, prefix, null, null, null]);
        }
        public void Postfix(MethodBase original, MethodInfo callback)
        {
            var postfix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, callback)!;
            type.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5).Invoke(instance, [original, null, postfix, null, null]);
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(instance, [id]);
    }
}
