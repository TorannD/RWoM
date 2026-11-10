using System.Reflection;
using System.Collections;
using TorannMagic;
using TorannMagic.AutoCast;
using AbilityUser;
using Verse;
using Verse.AI;
using RimWorld;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    static IEnumerable<TestCase> TravelCases()
    {
        yield return new("TravelAutocast.NativePathQueryRetainsPawnModeAndReleasesPath", f => World(f, w =>
        {
            PrepareTravel(w);
            using var patches = new Patches();
            w.Map.pathFinder = Blank<PathFinder>();
            patches.Prefix(typeof(Faction).GetProperty(nameof(Faction.OfPlayer))!.GetMethod!, nameof(TravelPlayerFaction));
            patches.Prefix(typeof(PathFinder).GetMethods().Single(m => m.Name == "FindPathNow" && m.GetParameters().Length >= 3 && m.GetParameters()[2].ParameterType == typeof(TraverseParms)), nameof(TravelNativePath));
            nativeTravelPath = new PawnPath(); nativeTravelPath.AddNode(new IntVec3(8, 0, 4)); Set(nativeTravelPath, "totalCostInt", 321f);
            Check.Equal(321f, TM_TravelAutocast.RemainingCost(w.Caster, w.Caster.Position, w.Caster.pather.Destination, PathEndMode.Touch), "native found cost returned");
            Check.Equal(0, nativeTravelPath.NodesReversed.Count, "native found path released to pool");
            nativeTravelPath = PawnPath.NotFound;
            Check.Equal(float.PositiveInfinity, TM_TravelAutocast.RemainingCost(w.Caster, w.Caster.Position, w.Caster.pather.Destination, PathEndMode.Touch), "native failed path cannot look like cheap progress");
        }));
        yield return new("TravelAutocast.AlternativeLandingUsesRemainingPathCost", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = TravelBoundaries(w);
            var goal = w.Caster.pather.Destination.Cell;
            w.Building(goal);
            var alternative = goal + new IntVec3(0, 0, 1);
            travelCost = c => c == w.Caster.Position ? 900 : c == alternative ? 20 : 850;
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan), "blocked direct goal has a useful neighboring landing");
            Check.Equal(alternative, plan.Landing, "chooses lowest remaining path cost, not the blocked goal");
            Check.Equal(PathEndMode.Touch, plan.Mode, "retains original work endpoint semantics");
            Check.Equal(true, travelQueries <= 17, "bounded pathfinder work per attempt");
        }));
        yield return new("TravelAutocast.NoSavingUnreachableOrTinySavingCannotCast", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = TravelBoundaries(w);
            foreach (float cost in new[] { float.PositiveInfinity, 1000f, 895f })
            {
                travelCost = c => c == w.Caster.Position ? 900 : cost;
                Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _), "unreachable, equal or negligible benefit rejected");
            }
        }));
        yield return new("TravelAutocast.StandingManualDraftedAndNPCRejected", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = TravelBoundaries(w);
            Set(w.Caster.pather, "moving", false);
            Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _), "stale destination cannot teleport stationary pawn");
            Set(w.Caster.pather, "moving", true); w.Job.playerForced = true;
            Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _), "manual work is not interrupted");
            w.Job.playerForced = false; Set(w.Caster.drafter, "draftedInt", true);
            Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _), "combat movement not treated as peaceful work");
            Set(w.Caster.drafter, "draftedInt", false); w.Player.def.isPlayer = false;
            Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _), "NPC travel cannot bypass combat gate");
            Check.Equal(0, travelQueries, "ineligible pawns do not pathfind");
        }));
        yield return new("TravelAutocast.RevalidatesJobGoalAndLanding", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = TravelBoundaries(w);
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan), "plan created");
            Check.Equal(true, TM_TravelAutocast.StillValid(w.Caster, plan), "unchanged plan valid");
            Set(w.Caster.jobs, "curJob", new Job(w.Job.def));
            Check.Equal(false, TM_TravelAutocast.StillValid(w.Caster, plan), "new job invalidates plan");
            Set(w.Caster.jobs, "curJob", w.Job); Set(w.Caster.pather, "destination", new LocalTargetInfo(new IntVec3(16, 0, 14)));
            Check.Equal(false, TM_TravelAutocast.StillValid(w.Caster, plan), "new path goal invalidates plan");
            Set(w.Caster.pather, "destination", plan.Destination); w.Building(plan.Landing);
            Check.Equal(false, TM_TravelAutocast.StillValid(w.Caster, plan), "new obstacle invalidates plan");
            Check.Equal(false, TM_TravelAutocast.LandingAllowed(w.Caster, w.Caster.Position, 16), "same cell rejected");
        }));
        foreach (bool might in new[] { false, true })
        {
            bool fighter = might;
            yield return new("TravelAutocast.WorkAndCargoSinglePayment." + (might ? "Phase" : "Blink"), f => World(f, w =>
            {
                GrimoireProgressionTests.LoadBaseDefinitions(f);
                using var syrrium = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null
                    ? new DefinitionScope<HediffDef>(new HediffDef { defName = "TM_SyrriumSenseHD" }) : null;
                w.Map.gameConditionManager = new GameConditionManager(w.Map);
                PrepareTravel(w); using var patches = TravelBoundaries(w);
                patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.Notify_Teleported))!, nameof(TravelNotify));
                patches.Prefix(typeof(HistoryEventsManager).GetMethod(nameof(HistoryEventsManager.RecordEvent))!, nameof(SkipGraphics));
                patches.Prefix(typeof(GameConditionManager).GetProperty("Parent", Flags)!.GetMethod!, nameof(NoParentConditions));
                var magic = new CompAbilityUserMagic { parent = w.Caster }; var melee = new CompAbilityUserMight { parent = w.Caster };
                w.Caster.AllComps.Add(magic); w.Caster.AllComps.Add(melee); TM_Calc.InvalidateCompCache(w.Caster);
                w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
                w.Caster.ageTracker = new Pawn_AgeTracker(w.Caster); Set(w.Caster.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
                Set(w.Caster.ageTracker, "cachedLifeStageIndex", 0);
                w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
                var mana = new Need_Mana(w.Caster); mana.CurLevel = .9f;
                var stamina = new Need_Stamina(w.Caster); stamina.CurLevel = .9f;
                w.Caster.needs.AllNeeds.Add(mana); w.Caster.needs.AllNeeds.Add(stamina);
                Set(magic, "magicData", new MagicData()); Set(melee, "mightData", new MightData());
                w.Caster.carryTracker = new Pawn_CarryTracker(w.Caster);
                var cargoDef = Blank<ThingDef>(); cargoDef.defName = "TravelCargo"; cargoDef.stackLimit = 75;
                var cargo = new Thing { def = cargoDef, stackCount = 12 };
                Check.Equal(true, w.Caster.carryTracker.innerContainer.TryAdd(cargo), "real cargo owner populated");
                var queued = new Job(new JobDef { defName = "QueuedTravelWork" }); w.Caster.jobs.jobQueue.EnqueueLast(queued);
                w.Selected.Clear(); var selected = new Thing(); w.Selected.Add(selected);
                var weaponDef = Blank<ThingDef>(); weaponDef.defName = "TravelSword"; weaponDef.category = ThingCategory.Item;
                weaponDef.equipmentType = EquipmentType.Primary;
                var weapon = new ThingWithComps { def = weaponDef };
                Set(weapon, "comps", new List<ThingComp> { new CompEquippable { parent = weapon, props = new CompProperties(typeof(CompEquippable)) } });
                Check.Equal(true, ((ThingOwner<ThingWithComps>)Get(w.Caster.equipment, "equipment")!).TryAdd(weapon, false), "real melee weapon equipped");
                var def = f.AddAbility("TM_TravelPayment" + fighter); def.MainVerb.range = 16; def.MainVerb.warmupTime = 0;
                def.manaCost = fighter ? 0 : .05f; def.staminaCost = fighter ? .05f : 0; def.consumeEnergy = true;
                PawnAbility ability = fighter ? new MightAbility(melee) { Def = def } : new MagicAbility(magic) { Def = def };
                float cost = fighter ? melee.ActualStaminaCost(def) : magic.ActualManaCost(def);
                var goal = w.Caster.pather.Destination;
                var before = w.Caster.Position;
                var magicPower = new MagicPower { autocast = false }; var mightPower = new MightPower { autocast = false };
                if (fighter) Phase.Evaluate(melee, def, ability, mightPower, 1, out _);
                else Blink.Evaluate(magic, def, ability, magicPower, 1, out _);
                Check.Equal(before, w.Caster.Position, "disabled toggle prevents travel");
                if (fighter) stamina.CurLevel = 0; else mana.CurLevel = 0;
                Check.Equal(false, TM_TravelAutocast.TryExecute(w.Caster, def, ability, 1, fighter), "insufficient resource cannot move");
                Check.Equal(before, w.Caster.Position, "failed payment admission retains position");
                if (fighter) stamina.CurLevel = .9f; else mana.CurLevel = .9f;
                travelThrowVisuals = !fighter;
                TorannMagic.Odyssey.ManualTravelSafety.BeginAttempt(ability);
                ability.PostAbilityAttempt(); // A rejected earlier manual attempt leaves no payable receipt.
                Check.Equal(true, TM_TravelAutocast.TryExecute(w.Caster, def, ability, 1, fighter), "travel completes");
                Check.Equal(1, travelVisualCalls, "post-move visual callback attempted once, failure cannot fail paid cast");
                Check.Same(w.Job, w.Caster.CurJob, "same work job retained");
                Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "same carried stack and owner retained");
                Check.Equal(12, cargo.stackCount, "cargo quantity retained");
                Check.Same(queued, w.Caster.jobs.jobQueue.Peek().job, "queued work retained");
                Check.Same(selected, Find.Selector.SingleSelectedThing, "unrelated selection untouched");
                Check.Equal(goal, resumedGoal, "path restarted toward original goal");
                Check.Equal(PathEndMode.Touch, resumedMode, "work endpoint mode retained");
                float remaining = fighter ? stamina.CurLevel : mana.CurLevel;
                Check.Equal(true, Math.Abs(remaining - (.9f - cost)) < .00001f, "real ability charged exactly one cost");
                Check.Equal(true, ability.CooldownTicksLeft > 0, "real cooldown started");
                ability.PostAbilityAttempt(); ability.PostAbilityAttempt();
                Check.Equal(remaining, fighter ? stamina.CurLevel : mana.CurLevel, "old manual callbacks cannot charge successful automatic travel twice");
                Check.Equal(false, TM_TravelAutocast.TryExecute(w.Caster, def, ability, 1, fighter), "repeat cannot pay or move again");
                Check.Equal(remaining, fighter ? stamina.CurLevel : mana.CurLevel, "retry does not charge");
                if (travelThrowVisuals)
                {
                    var diagnostics = (List<string>)typeof(RuntimeLog).GetField("Problems", Flags)!.GetValue(null)!;
                    Check.Equal(1, diagnostics.Count, "only injected visual failure is logged");
                    Check.Equal(true, diagnostics[0].StartsWith("[RWoM.Teleport] Travel completed; visual effect failed:")
                        && diagnostics[0].Contains("injected travel graphics failure"), "specific injected failure remains observable");
                    RuntimeLog.Clear();
                }
            }));
        }
        foreach (string name in new[] { "Blink", "Phase" })
        {
            string route = name;
            yield return new("TravelAutocast.SharedSafeRoute." + route, _ =>
            {
                var type = typeof(MagicAbility).Assembly.GetType("TorannMagic.AutoCast." + route)!;
                var calls = type.GetMethods(Flags).Where(m => m.DeclaringType == type)
                    .SelectMany(Calls).ToArray();
                Check.Equal(false, calls.Any(c => c.DeclaringType?.Name == "Selector" || c.DeclaringType?.Name.Contains("Camera") == true), "travel must not select pawns or move the camera");
                Check.Equal(false, calls.Any(c => c.Name == "DeSpawn" || c.Name == "ClearQueuedJobs"), "travel retains ownership and queued work");
                Check.Equal(true, calls.Any(c => c.DeclaringType?.Name == "TM_TravelAutocast" && c.Name == "TryExecute"), "travel must use the shared checked execution path");
            });
        }
    }

    static Func<IntVec3, float> travelCost = null!;
    static int travelQueries;
    static bool travelThrowVisuals;
    static int travelVisualCalls;
    static LocalTargetInfo resumedGoal;
    static PathEndMode resumedMode;
    static PawnPath nativeTravelPath = null!;
    static bool TravelPlayerFaction(ref Faction __result) { __result = active!.Player; return false; }
    static bool TravelNativePath(TraverseParms traverseParms, PathEndMode peMode, ref PawnPath __result)
    {
        Check.Same(active!.Caster, traverseParms.pawn, "native query uses pawn-specific traversal, not PassDoors");
        Check.Equal(TraverseMode.ByPawn, traverseParms.mode, "closed door permissions belong to pawn");
        Check.Equal(PathEndMode.Touch, peMode, "native query uses original endpoint semantics");
        __result = nativeTravelPath; return false;
    }
    static void PrepareTravel(WorldState w)
    {
        Set(w.Caster.drafter, "draftedInt", false); w.Job.locomotionUrgency = LocomotionUrgency.Jog;
        w.Job.def.driverClass = typeof(JobDriver_Goto);
        w.Caster.pather = new Pawn_PathFollower(w.Caster);
        Set(w.Caster.pather, "moving", true); Set(w.Caster.pather, "destination", new LocalTargetInfo(new IntVec3(14, 0, 4)));
        Set(w.Caster.pather, "peMode", PathEndMode.Touch);
        var path = new PawnPath(); path.AddNode(new IntVec3(14, 0, 4)); path.AddNode(w.Caster.Position); Set(path, "curNodeIndex", 1);
        w.Caster.pather.curPath = path;
        travelQueries = 0; travelCost = c => (c - new IntVec3(14, 0, 4)).LengthHorizontal * 60;
        travelThrowVisuals = false; travelVisualCalls = 0;
        resumedGoal = LocalTargetInfo.Invalid;
    }
    static Patches TravelBoundaries(WorldState w)
    {
        var patches = w.MovementBoundaries(stubSettlement: false);
        patches.Prefix(typeof(TM_TravelAutocast).GetMethod(nameof(TM_TravelAutocast.RemainingCost))!, nameof(TravelCost));
        patches.Prefix(typeof(Pawn_PathFollower).GetMethod(nameof(Pawn_PathFollower.StartPath))!, nameof(ResumeTravel));
        patches.Prefix(typeof(TM_TravelAutocast).GetMethod(nameof(TM_TravelAutocast.DrawEffects))!, nameof(TravelVisuals));
        return patches;
    }
    static bool TravelCost(Pawn pawn, IntVec3 start, LocalTargetInfo destination, PathEndMode mode, ref float __result)
    {
        Check.Same(active!.Caster, pawn, "cost query uses actual pawn");
        Check.Equal(pawn.pather.Destination, destination, "cost query retains full goal");
        Check.Equal(PathEndMode.Touch, mode, "cost query retains endpoint mode");
        travelQueries++; __result = travelCost(start); return false;
    }
    static bool TravelNotify(Pawn __instance, bool endCurrentJob)
    {
        Check.Equal(false, endCurrentJob, "native teleport must not terminate work");
        __instance.pather.Notify_Teleported_Int(); return false;
    }
    static bool NoParentConditions(ref GameConditionManager __result) { __result = null!; return false; }
    static bool TravelVisuals()
    {
        travelVisualCalls++;
        if (travelThrowVisuals) throw new InvalidOperationException("injected travel graphics failure");
        return false;
    }
    static bool ResumeTravel(LocalTargetInfo dest, PathEndMode peMode)
    {
        resumedGoal = dest; resumedMode = peMode; return false;
    }
}
