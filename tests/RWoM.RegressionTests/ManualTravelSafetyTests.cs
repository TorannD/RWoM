using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.Odyssey;
using TorannMagic.SpellItems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    static Action? manualContinuation;
    static int manualDialogs, manualOrders;
    sealed class ManualMagic : MagicAbility
    {
        public override bool CanCastPowerCheck(AbilityContext context, out string reason) { reason = ""; return true; }
        public override Job GetJob(AbilityContext context, LocalTargetInfo target)
            => new Job(new JobDef { defName = "ManualTravelFixture" }, target) { verbToUse = Verb, count = context == AbilityContext.Player ? 1 : 0 };
    }
    sealed class PaidManualMagic : MagicAbility
    {
        public PaidManualMagic(CompAbilityUserMagic comp) : base(comp) { }
        public override bool CanCastPowerCheck(AbilityContext context, out string reason)
        { reason = ""; return CooldownTicksLeft <= 0 && MagicUser.Mana.CurLevel >= MagicUser.ActualManaCost(magicDef); }
    }
    static FlyingObject_Flight? manualFlight;
    static int manualImpactCalls;
    static bool CountManualImpact(ref DamageWorker.DamageResult __result)
    { manualImpactCalls++; __result = new DamageWorker.DamageResult(); return false; }
    public sealed class ManualReceiptSave : IExposable
    {
        public Job job = null!;
        public MagicAbility ability = null!;
        public void ExposeData()
        {
            Scribe_Deep.Look(ref job, "job");
            Scribe_Deep.Look(ref ability, "ability");
        }
    }
    static bool ManualFlightSpawn(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
    { Set(newThing, "mapIndexOrState", (sbyte)0); Set(newThing, "positionInt", loc); manualFlight = newThing as FlyingObject_Flight; __result = newThing; return false; }
    static bool ManualFlightDespawn(Pawn __instance)
    {
        active!.Map.thingGrid.Deregister(__instance); Set(__instance, "mapIndexOrState", (sbyte)-1);
        Set(__instance.jobs, "curJob", null); // Native despawn cancels the cast: payment cannot depend on its final toil.
        return false;
    }
    static Patches ManualBoundaries()
    {
        manualContinuation = null; manualDialogs = manualOrders = 0;
        var patches = new Patches();
        patches.Prefix(typeof(ManualTravelSafety).GetMethod(nameof(ManualTravelSafety.Confirm))!, nameof(CaptureTravelWarning));
        patches.Prefix(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.TryTakeOrderedJob))!, nameof(CaptureTravelOrder));
        return patches;
    }
    static bool CaptureTravelWarning(string warning, Action continuation)
    { manualDialogs++; manualContinuation = continuation; return false; }
    static bool CaptureTravelOrder(Pawn_JobTracker __instance, Job job, ref bool __result)
    { manualOrders++; Set(__instance, "curJob", job); __result = true; return false; }

    static IEnumerable<TestCase> ManualTravelCases()
    {
        yield return new("ManualTravel.ScornWarnsForClampedLandingAndRejectsStaleOrders", f => World(f,w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster,w.Map);
            using var patches = ManualBoundaries();
            env.Vacuum = 1;
            var click = new IntVec3(1,0,12);
            var landing = new IntVec3(5,0,12);
            var submit = typeof(Verb_Scorn).GetMethod("Submit",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
            var job = new Job(new JobDef {defName="ScornBounds"});
            bool valid = true; int orders = 0;
            bool Submit(bool player) => (bool)submit.Invoke(null,[w.Caster,job,(LocalTargetInfo)click,player,(Func<bool>)(()=>valid),(Func<bool>)(()=>{orders++;return true;})])!;
            Check.Equal(false,Submit(false),"dangerous automatic landing rejected");
            Check.Equal(0,manualDialogs,"automatic order is silent");
            Check.Equal(false,Submit(true),"manual order waits for warning");
            Check.Equal(1,manualDialogs,"one manual warning");
            Check.Equal(landing,job.globalTarget.Cell,"warning binds actual landing near map edge");
            valid=false; manualContinuation!();
            Check.Equal(0,orders,"stale order cannot move");
            valid=true; Submit(true); manualContinuation!();
            Check.Equal(1,orders,"valid explicit confirmation submits once");
            Check.Equal(true,ManualTravelSafety.HasConsent(w.Caster,job,w.Map,landing),"exact landing consent");
            Check.Equal(false,ManualTravelSafety.HasConsent(w.Caster,job,w.Map,click),"consent never follows the unclamped cell");
        }));
        foreach (string suffix in new[] { "", "_I", "_II", "_III" })
        {
            string rank = suffix;
            yield return new("ManualTravel.HolographicShockwavePayment" + rank, f => World(f, w =>
            {
                GrimoireProgressionTests.LoadBaseDefinitions(f);
                using var patches = w.MovementBoundaries(false);
                patches.Prefix(typeof(HistoryEventsManager).GetMethod(nameof(HistoryEventsManager.RecordEvent))!, nameof(SkipGraphics));
                var might = new CompAbilityUserMight { parent = w.Caster }; w.Caster.AllComps.Add(might); TM_Calc.InvalidateCompCache(w.Caster);
                w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
                w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
                Set(w.Caster.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
                var stamina = new Need_Stamina(w.Caster); stamina.CurLevel = 1f; w.Caster.needs.AllNeeds.Add(stamina);
                Set(might, "mightData", new MightData());
                var def = f.AddAbility("TM_HolographicShockwave" + rank);
                def.MainVerb.verbClass = typeof(Effect_Flight); def.MainVerb.SecondsToRecharge = 60;
                def.staminaCost = .65f; def.consumeEnergy = true;
                var ability = new MightAbility(might) { Def = def };
                var verb = new Effect_Flight { caster = w.Caster, verbProps = def.MainVerb, Ability = ability }; Set(ability, "verb", verb);
                // The cast job can finish before Flight begins its own receipt.
                ability.PostAbilityAttempt();
                Check.Equal(1f, stamina.CurLevel, "no premature payment on cancelled takeoff");
                Check.Equal(0, might.MightUsed.Count, "no false use event");
                ManualTravelSafety.BeginAttempt(ability); ManualTravelSafety.Settle(ability);
                float paid = stamina.CurLevel;
                Check.Equal(true, paid < 1f, "successful takeoff pays real stamina");
                Check.Equal(1, might.MightUsed.Count, "exactly one native use event");
                ability.PostAbilityAttempt(); ability.PostAbilityAttempt();
                Check.Equal(paid, stamina.CurLevel, "final callbacks cannot repeat payment");
                Check.Equal(1, might.MightUsed.Count, "no duplicated XP/use event");
            }));
        }
        foreach (var test in ManualPortalPaymentCases()) yield return test;
        foreach (var test in SpellItemEffectTests.ManualItemBlinkCases()) yield return test;
        yield return new("ManualTravel.CarrierNativeScribeBindsPassengerEndpointAndImpactOnce", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            using var patches = new Patches();
            patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.ExposeData))!, nameof(FlightPawnSave));
            patches.Prefix(typeof(Find).GetProperty(nameof(Find.FactionManager))!.GetMethod!, nameof(FlightFactions));
            patches.Prefix(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!, nameof(CountManualImpact));
            var cargo = FlightCargo(w);
            var def = Blank<ThingDef>(); def.defName = "ManualFlightSaveCarrier"; def.thingClass = typeof(FlyingObject_Flight); def.category = ThingCategory.Ethereal; def.comps = [];
            using var carrierDef = new DefinitionScope<ThingDef>(def);
            using var raceDef = new DefinitionScope<ThingDef>(w.Caster.def);
            using var cargoDef = new DefinitionScope<ThingDef>(cargo.def);
            using var jobDef = new DefinitionScope<JobDef>(w.Job.def);
            w.Caster.thingIDNumber = 773; w.Job.loadID = 774; w.Caster.jobs.curDriver = new JobDriver_Goto { pawn = w.Caster, job = w.Job };
            var carrier = new FlyingObject_Flight { def = def, thingIDNumber = 775 };
            Set(carrier, "mapIndexOrState", (sbyte)0); Set(carrier, "positionInt", new IntVec3(12,0,10));
            Set(carrier, "origin", new UnityEngine.Vector3(10,0,10)); Set(carrier, "destination", new UnityEngine.Vector3(12,0,10));
            w.Map.thingGrid.Deregister(w.Caster); Set(w.Caster, "mapIndexOrState", (sbyte)-1); Set(carrier, "flyingThing", w.Caster);
            Set(carrier, "acceptedDanger", false);
            carrier.impactDamage = new DamageInfo(new DamageDef { defName = "ManualFlightImpact" }, 1);
            var impact = typeof(FlyingObject_Flight).GetMethod("Impact", Flags | System.Reflection.BindingFlags.DeclaredOnly)!;
            var oldSound = SoundDefOf.Ambient_AltitudeWind; SoundDefOf.Ambient_AltitudeWind = new SoundDef();
            try
            {
                manualImpactCalls = 0; impact.Invoke(carrier, [w.Caster]); impact.Invoke(carrier, [w.Caster]);
                Check.Equal(1, manualImpactCalls, "landing retries apply impact once");
                Check.Equal(false, carrier.Destroyed, "unsafe retry retains carrier");
                Set(carrier, "acceptedDanger", true); Set(carrier, "acceptedDestination", new IntVec3(12,0,10));
                Set(carrier, "acceptedMap", w.Map); Set(carrier, "acceptedPassenger", w.Caster);
                string file = Path.Combine(f.WorkingDirectory, "manual-carrier-in-air.xml");
                try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref carrier, "carrier"); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                FlyingObject_Flight loaded = null!;
                try
                {
                    Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "carrier");
                    var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", Flags)!.GetValue(Scribe.loader.crossRefs)!;
                    directory.RegisterLoaded(w.Map); Scribe.loader.FinalizeLoading();
                }
                finally { Scribe.ForceStop(); }
                var passenger = (Pawn)Get(loaded, "flyingThing")!;
                Check.Same(passenger, Get(loaded, "acceptedPassenger"), "consent rebound to exact loaded passenger");
                Check.Same(w.Map, Get(loaded, "acceptedMap"), "consent retains destination map reference");
                Check.Equal(new IntVec3(12,0,10), (IntVec3)Get(loaded, "acceptedDestination")!, "consent retains exact cell");
                Check.Equal(true, (bool)Get(loaded, "acceptedDanger")!, "explicit continuation survives flight save");
                Check.Equal(true, (bool)Get(loaded, "impactApplied")!, "already applied impact survives save");
                Set(loaded, "mapIndexOrState", (sbyte)0); Set(loaded, "positionInt", new IntVec3(12,0,10));
                // A different map reference invalidates consent even at identical coordinates.
                Set(loaded, "acceptedMap", Blank<Map>());
                using var loadedEnvironment = new TravelEnvironmentFixture(passenger, w.Map); loadedEnvironment.Vacuum = 1;
                loaded.impactDamage = carrier.impactDamage;
                impact.Invoke(loaded, [passenger]);
                Check.Equal(1, manualImpactCalls, "post-load retry never repeats impact");
                Check.Equal(false, loaded.Destroyed, "wrong-map consent cannot force dangerous recovery");
                Check.Equal(false, passenger.Spawned, "loaded passenger retained safely");
            }
            finally { SoundDefOf.Ambient_AltitudeWind = oldSound; }
        }));
        yield return new("ManualTravel.NativeJobConsentAndPaymentScribe", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            using var patches = ManualBoundaries();
            patches.PatchType(typeof(ManualTravelSafety).Assembly.GetType("TorannMagic.Odyssey.TravelConsentSavePatch")!);
            patches.PatchType(typeof(ManualTravelSafety).Assembly.GetType("TorannMagic.Odyssey.TravelPaymentSavePatch")!);
            using var registered = new DefinitionScope<JobDef>(w.Job.def);
            var ability = new MagicAbility { Def = f.AddAbility("ManualReceiptNontravel") };
            ManualTravelSafety.BeginAttempt(ability);
            var cell = new IntVec3(12,0,10);
            ManualTravelSafety.Submit(w.Caster, w.Job, w.Map, cell, true, () => true, () => { }); manualContinuation!();
            var saved = new ManualReceiptSave { job = w.Job, ability = ability };
            string file = Path.Combine(f.WorkingDirectory, "manual-travel-receipts.xml");
            try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref saved, "receipt"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            ManualReceiptSave loaded = null!;
            try
            {
                Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "receipt");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", Flags)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(w.Caster); directory.RegisterLoaded(w.Map); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            Check.Equal(true, ManualTravelSafety.HasConsent(w.Caster, loaded.job, w.Map, cell), "native loaded exact job has consent");
            Check.Equal(false, ManualTravelSafety.HasConsent(w.Caster, loaded.job, w.Map, cell + IntVec3.East), "native loaded consent remains endpoint bound");
            Check.Equal(false, ManualTravelSafety.TakePayment(loaded.ability), "native loaded failed/settled callback cannot charge");
            Check.Equal(false, ManualTravelSafety.TakePayment(loaded.ability), "repeated loaded callback stays suppressed");
        }));
        foreach (bool flight in new[] { false, true })
        {
            bool fly = flight;
            yield return new("ManualTravel.NativePaymentStaleDepletedSuccessAndFinalCallback." + (fly ? "Flight" : "Blink"), f => World(f, w =>
            {
                GrimoireProgressionTests.LoadBaseDefinitions(f);
                using var sense = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null
                    ? new DefinitionScope<HediffDef>(new HediffDef { defName = "TM_SyrriumSenseHD" }) : null;
                using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
                using var patches = w.MovementBoundaries(false);
                patches.Prefix(typeof(HistoryEventsManager).GetMethod(nameof(HistoryEventsManager.RecordEvent))!, nameof(SkipGraphics));
                patches.Prefix(typeof(GameConditionManager).GetProperty("Parent", Flags)!.GetMethod!, nameof(NoParentConditions));
                w.Map.gameConditionManager = new GameConditionManager(w.Map);
                var magic = new CompAbilityUserMagic { parent = w.Caster }; w.Caster.AllComps.Add(magic); TM_Calc.InvalidateCompCache(w.Caster);
                w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
                w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
                Set(w.Caster.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
                var mana = new Need_Mana(w.Caster); mana.CurLevel = .9f; w.Caster.needs.AllNeeds.Add(mana);
                Set(magic, "magicData", new MagicData());
                var def = f.AddAbility("ManualPaymentFixture" + fly); def.defName = fly ? "TM_Flight" : "TM_Blink";
                def.MainVerb.range = 20; def.manaCost = .04f; def.consumeEnergy = true;
                var ability = new PaidManualMagic(magic) { Def = def };
                var verb = (Verb_UseAbility)Activator.CreateInstance(VerbType(fly ? "Effect_Flight" : "Verb_Blink"), true)!;
                verb.caster = w.Caster; verb.verbProps = def.MainVerb; verb.Ability = ability; Set(ability, "verb", verb);
                var cell = new IntVec3(12,0,10); Set(verb, "currentTarget", new LocalTargetInfo(cell)); verb.TargetsAoE.Add(cell);
                w.Job.verbToUse = verb; w.Job.count = 1;
                patches.Prefix(typeof(Effect_Flight).GetMethod(nameof(Effect_Flight.CanHitTargetFrom))!, nameof(FlightShootLine));
                bool FireManual()
                {
                    if (!fly) return Fire(verb);
                    ((Effect_Flight)verb).PostCastShot(true, out bool result); return result;
                }
                Check.Equal(false, FireManual(), "warmup environment changed: actual completion refuses");
                ability.PostAbilityAttempt(); Check.Equal(.9f, mana.CurLevel, "native final callback after refusal costs nothing");
                env.Vacuum = 0; mana.CurLevel = 0;
                Check.Equal(fly ? "TM_Flight" : "TM_Blink", ability.Def?.defName, "definition identity after attempt");
                Check.Equal(true, ManualTravelSafety.IsTravel(ability), "native definition recognized");
                Check.Equal(true, magic.ActualManaCost(def) > 0, "native mana price positive");
                Check.Equal(false, ability.CanCastPowerCheck(AbilityContext.Player, out _), "fixture resource admission sees depleted mana");
                Check.Equal(false, ManualTravelSafety.CanPay(ability), "completion resource gate sees depleted mana");
                Check.Equal(false, FireManual(), "mana depleted during warmup: actual completion refuses");
                ability.PostAbilityAttempt(); Check.Equal(0f, mana.CurLevel, "failed depleted attempt stays free");
                mana.CurLevel = .9f;
                var carrierDef = Blank<ThingDef>(); carrierDef.defName = "FlyingObject_Flight"; carrierDef.thingClass = typeof(FlyingObject_Flight); carrierDef.category = ThingCategory.Ethereal;
                carrierDef.comps = [];
                using var registered = new DefinitionScope<ThingDef>(carrierDef);
                foreach (var spawn in typeof(GenSpawn).GetMethods().Where(m => m.Name == "Spawn" && m.GetParameters()[0].ParameterType == typeof(Thing)))
                    patches.Prefix(spawn, nameof(ManualFlightSpawn));
                patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!, nameof(ManualFlightDespawn));
                patches.Prefix(typeof(FlyingObject_Flight).GetMethod("Initialize", Flags)!, nameof(SkipGraphics));
                w.Caster.rotationTracker = new Pawn_RotationTracker(w.Caster);
                Check.Equal(true, FireManual(), "actual effect succeeds");
                Check.Equal(true, Math.Abs(mana.CurLevel - .86f) < .00001f, "successful movement pays real mana once");
                Check.Equal(1, magic.MagicUsed.Count, "one native magic history entry");
                ability.PostAbilityAttempt(); ability.PostAbilityAttempt();
                Check.Equal(true, Math.Abs(mana.CurLevel - .86f) < .00001f, "later final callbacks cannot repeat payment");
                if (fly) { Check.Equal(false, w.Caster.Spawned, "flight owns despawned passenger"); Check.Same(w.Caster, Get(manualFlight!, "flyingThing"), "exact carrier passenger"); }
            }));
        }
        yield return new("ManualTravel.LearnedBlinkAdmissionBeforeJobAndExplicitOnce", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            using var patches = ManualBoundaries();
            var verb = (Verb_UseAbility)w.Verb("Verb_Blink", f);
            var ability = new ManualMagic { Def = verb.Ability.Def, AbilityUser = new CompAbilityUserMagic { parent = w.Caster } };
            ability.Def.defName = "TM_Blink"; Set(ability, "verb", verb); verb.Ability = ability;
            var original = w.Caster.CurJob; var cell = new IntVec3(12, 0, 10);
            Check.Equal<Job?>(null, ability.UseAbility(AbilityContext.Player, cell), "danger awaits explicit continuation");
            Check.Equal(1, manualDialogs, "one warning"); Check.Equal(0, manualOrders, "no order before consent");
            Check.Same(original, w.Caster.CurJob, "old job untouched while warning open");
            manualContinuation!(); manualContinuation!();
            Check.Equal(1, manualOrders, "Continue submits exact order once");
            Check.Equal(true, ManualTravelSafety.HasConsent(w.Caster, w.Caster.CurJob, w.Map, cell), "actual job owns endpoint consent");
            Check.Equal(false, ManualTravelSafety.HasConsent(w.Caster, original, w.Map, cell), "old job gets no consent");
            Check.Equal(false, ManualTravelSafety.HasConsent(w.Caster, w.Caster.CurJob, w.Map, cell + IntVec3.East), "different endpoint gets no consent");
        }));
        yield return new("ManualTravel.CancelStaleAndNPCNeverSubmit", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            using var patches = ManualBoundaries(); var job = new Job(w.Job.def); var cell = new IntVec3(12, 0, 10);
            bool valid = true; int submitted = 0;
            Check.Equal(false, ManualTravelSafety.Submit(w.Caster, job, w.Map, cell, false, () => true, () => submitted++), "automatic rejected");
            Check.Equal(0, manualDialogs, "automatic never opens UI");
            ManualTravelSafety.Submit(w.Caster, job, w.Map, cell, true, () => valid, () => submitted++);
            Check.Equal(0, submitted, "cancelled/unanswered warning does nothing");
            valid = false; manualContinuation!();
            Check.Equal(0, submitted, "stale continuation cannot submit");
            Check.Equal(false, ManualTravelSafety.HasConsent(w.Caster, job, w.Map, cell), "stale continuation creates no receipt");
            w.Player.def.isPlayer = false;
            ManualTravelSafety.Submit(w.Caster, job, w.Map, cell, true, () => true, () => submitted++);
            Check.Equal(1, manualDialogs, "NPC even with player context receives no UI");
        }));
        foreach (var name in new[] { "Verb_Blink", "Verb_PhaseStrike" })
        {
            string route = name;
            yield return new("ManualTravel.ActualCompletionRejectsStaleThenHonorsConsent." + route, f => World(f, w =>
            {
                using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
                using var patches = ManualBoundaries(); using var movement = w.MovementBoundaries();
                var verb = w.Verb(route, f); var cell = new IntVec3(12, 0, 10); Set(verb, "currentTarget", new LocalTargetInfo(cell));
                Check.Equal(false, Fire(verb), "changed dangerous completion rejects");
                Check.Equal(0, w.Events.Count, "rejected completion has no move or secondary effect");
                ManualTravelSafety.Submit(w.Caster, w.Job, w.Map, cell, true, () => true, () => { });
                manualContinuation!();
                Check.Equal(true, Fire(verb), "same exact job may deliberately land in danger");
                Check.Equal(cell, w.Caster.Position, "accepted destination reached");
            }));
        }
        yield return new("ManualTravel.ItemBlinkCompletionRechecksBeforeEffect", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            using var patches = ManualBoundaries(); using var movement = w.MovementBoundaries();
            var props = new CompProperties_SpellItem { spell = SpellItemKind.Blink, range = 30 };
            var cell = new IntVec3(12, 0, 10);
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, cell, props), "item effect refuses unconfirmed danger");
            Check.Equal(0, w.Events.Count, "rejected effect cannot trigger a paid move");
            ManualTravelSafety.Submit(w.Caster, w.Job, w.Map, cell, true, () => true, () => { }); manualContinuation!();
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, cell, props), "confirmed item effect succeeds");
            Check.Equal(cell, w.Caster.Position, "item reaches endpoint");
        }));
        yield return new("ManualTravel.LegacyPortalRefusalHasNoMoveOrEnergy", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            using var graphics = new Patches();
            foreach (var method in typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial"))
                graphics.Prefix(method, nameof(SkipGraphics));
            var portal = new Building_TMPortal();
            portal.PortalDestinationMap = w.Map; portal.PortalDestinationPosition = new IntVec3(12, 0, 10);
            Set(portal, "arcaneEnergyCur", .8f); var start = w.Caster.Position;
            Check.Equal(false, (bool)typeof(Building_TMPortal).GetMethod("PortalPawn", Flags)!.Invoke(portal, [w.Caster])!, "portal refuses before cosmetics/despawn");
            Check.Equal(start, w.Caster.Position, "pawn retained at entrance");
            Check.Equal(.8f, (float)Get(portal, "arcaneEnergyCur")!, "no portal energy payment on refusal");
        }));
        yield return new("ManualTravel.CarrierRetainsPassengerWhenAllCellsDangerous", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map); env.Vacuum = 1;
            var carrierDef = Blank<ThingDef>(); carrierDef.defName = "ManualFlightCarrier";
            var carrier = new FlyingObject_Flight { def = carrierDef };
            Set(carrier, "mapIndexOrState", (sbyte)0); Set(carrier, "positionInt", new IntVec3(12,0,10));
            w.Map.thingGrid.Deregister(w.Caster); Set(w.Caster, "mapIndexOrState", (sbyte)-1);
            Set(carrier, "flyingThing", w.Caster); Set(carrier, "origin", new UnityEngine.Vector3(10,0,10));
            carrier.Destroy();
            Check.Equal(false, carrier.Destroyed, "cleanup cannot discard held passenger");
            Check.Same(w.Caster, Get(carrier, "flyingThing"), "actual passenger retained");
            Check.Equal(false, w.Caster.Spawned, "no unsafe forced landing");
        }));
    }
}
