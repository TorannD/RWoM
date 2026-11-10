# RWoM 1.7 regression checks

Run from PowerShell 7 with the .NET 10 SDK and a local RimWorld 1.7 installation:

```powershell
pwsh -NoProfile -File tests/run-regression-tests.ps1
```

The runner builds the 1.7 mod and harness into a new temporary directory. It does not install the DLL, start RimWorld, load a colony, or build 1.6. Production packages must already be in the local NuGet cache. Existing project compile-time references under `../refs/out` and Harmony must also be available.

Default runtime locations match this checkout's development installation. Override them when needed:

```powershell
pwsh -NoProfile -File tests/run-regression-tests.ps1 `
  -GameDir 'D:/Games/RimWorld' `
  -JecsDir 'D:/SteamLibrary/steamapps/workshop/content/294100/3312959701/1.7/Assemblies' `
  -HarmonyDir 'D:/SteamLibrary/steamapps/workshop/content/294100/2009463077/1.7/Assemblies'
```

To run a focused group or compare an existing production DLL:

```powershell
pwsh -NoProfile -File tests/run-regression-tests.ps1 -Filter 'MagicData.'
pwsh -NoProfile -File tests/run-regression-tests.ps1 `
  -ModAssembly 'C:/Temp/baseline/TorannMagic.dll' `
  -ArtifactsDir 'C:/Temp/rwom-baseline-check'
```

Use a fresh artifacts directory for each comparison. Build logs, test output and temporary Scribe fixtures are saved there. A nonzero exit code means a build/test failed; runtime warnings and errors captured during tests also fail the test. The production mod still has pre-existing compiler warnings, reported separately from test failures.

## Current verification status

The prepared 1.7 update records **227 passed / 2 failed / 229 total**, including 14 ability-projectile and 17 cast-safety checks, with a production build of 0 errors and 395 existing warnings. The two failures remain visible: `Grimoire.Metadata` and `Grimoire.InstalledIsekaiExactNameGate`. The intentionally retained name `A RimWorld of Magic — 1.7 (Unofficial)` differs from the exact name expected by the tested Isekai XML integration. Both failures also occur against the unchanged pre-fix DLL. These are known unresolved compatibility failures, not a fully green suite. No assertion is skipped or weakened to hide them.

## Coverage and limits

The harness loads real compiled RWoM code and installed RimWorld/JecsLite assemblies. Its controlled fixtures use actual game types and definitions derived from the shipped XML. Asset-dependent constructors are bypassed where no graphics engine exists; game effects that depend on a full map are not simulated as though they had run.

Skill round trips use real Verse Scribe saving, loading, cross-reference resolution and post-load initialization on `MagicData`. These are not complete pawn/map save-load tests. Skill levels already erased from an older save cannot be recovered by the fix; use an earlier save or manual compensation.

The suite deliberately avoids matching exact C# source text. Behavioral checks and metadata-contract checks protect the implemented save, targeting, registration and UI policies; human tooltip prose is verified separately for meaning and XML integrity.

For effects requiring a running game, use [the 1.7 smoke-test checklist](smoke-tests/validation-1.7.md). Command-line success does not claim a live 50×50 battle, camera/selection check, GUI rendering pass or performance benchmark.

## Successful-cast logging checks

CombatLog tests validate class-scanner metadata for Pawn.Kill and the three execution hooks, inspect the installed engine/JecsLite warmup IL, invoke both actual warmup prefixes with real PawnAbility verbs, and exercise the transient activation consumer. They verify matching caster/verb, original target capture after retargeting, one entry per burst, rearming, no captured activation for rejected requests/resumed bursts, cancellation replacement, and self/cell/friendly-support filtering. Constructor fixtures supply an in-memory Game clock and ID manager; no map, save, or settings file is loaded.

On 1.7, a cast entry means the engine reached Pawn.Notify_UsedVerb after Available() and TryCastShot() both succeeded. It does not prove that a projectile subsequently hit. Legacy verbs such as Hex/Enrage that apply an effect but return false remain unreported. Cell-only/AoE casts produce no invented primary-pawn cast entry; existing actual spell-damage attribution remains separate. Activation state is not serialized: a mid-burst load without WarmupComplete emits no new cast entry, avoiding guessed or duplicate events. Live game cancellation/scheduling, complete battle-log rendering/save-load, third-party Harmony interaction, and effect execution still require smoke tests.

## Grimoire and compatibility metadata checks

Grimoire tests call the compiled Magic/Might visibility getters with real pawn components, colonist/selection gates and controlled registered-tab lists. They cover Faceless both without a race Magic tab and after another same-race mage registered one. Ability-bar mode suppresses inspect entries. These checks do not render a window or exercise GUI tab deselection.

Before engine metadata static initialization, the harness redirects `GenFilePaths` mod/content/config paths into its isolated output directory and writes only a small controlled metadata/config fixture there. The installed Isekai `RoMTreePatches.xml` is read without modification. Its actual `PatchOperationFindMod` gate executes through the game engine with its original exact-name criteria and a marker match branch; former-name, inactive and canonical-active cases are checked. This verifies the native gate, not the full passive-tree patch or third-party Harmony execution. Touched translation keys are checked for uniqueness, nonempty values and matching placeholders; human prose is reviewed for meaning separately.

Native Isekai compatibility claims cover only maximum mana/stamina, regeneration, cooldowns and resource costs. The native gate test does not verify those bonuses in a running game. Live UI, map and complete save/load smoke tests have not been run.

## Need, class and localized skill initialization checks

Initialization tests invoke the real `Pawn_NeedsTracker.AddNeed` lifecycle, including the base `Need` constructor's early `SetInitialLevel` call. They check Faceless mana visibility without changing the shared `NeedDef`. Class assignment tests use shipped ability identities and the real JecsLite ability registration path with powers already leveled, covering the transition used by random debug class rolls.

The localized Empath checks load the shipped custom-power XML through Verse inheritance, object loading and cross-reference resolution, then apply the actual Russian `DefInjectionPackage` before invoking `HediffComp_Empath.Initialize`. They cover the language-injection stage that the earlier hand-built skill fixtures omitted. A separate check verifies that the engine rejects future translations of the `[NoTranslate]` skill identifier while valid description translations still apply.

The historical fixture records the 48 removed Russian identifier translations. Real Scribe tests migrate those exact aliases independently of the active language, preserve unknown skills and invested metadata, and keep the highest invested object when a canonical duplicate exists. The three aliases sharing built-in mage skill trees are saved through those trees; fighter aliases receive the same canonical identifiers on load. These checks do not load a complete colony or run a live battle.

## Magic shield off-map ticking

MagicShield tests run the real component on an unspawned pawn through its full lifetime, verifying normal expiry and uninterrupted ticking after a severity loss without accessing map effects. The baseline reaches graphics-dependent code and fails in the headless renderer before the live report's `FleckMaker.Static` null-map dereference. These checks cover the component's off-map behavior, not a complete WorldPawns tick, rendered shield effects, or Multiplayer synchronization.

## Fortitude damage and undead resource needs

Damage tests call the compiled `Pawn_PreApplyDamage` prefix with explosive damage and real pawn, MightData and need objects. The undead cases first execute RWoM's actual need policy to verify that stamina is forbidden, then verify that Fortitude still reduces or fully absorbs damage without creating that need. Living controls verify the same mitigation and real stamina restoration. The headless setup accepts only the exact expected `PawnKindDefOf` initialization warning; runtime warnings still fail tests. These checks reproduce a valid failure path matching the reported prefix stack; they do not identify the original damaged pawn or execute a complete explosion, raid, resurrection or Multiplayer pipeline.
Self-cast regression cases execute the production job driver's combat toil and cleanup with the installed engine's `TryStartCastOn`. They check initial and stale AI verb targets, colonist casting, valid cell targets, invalid target/unspawned caster rejection without charging, and positive warmup followed by one shot through the engine's real warmup stance. A controlled verb records the shot target; a fixed aiming stat avoids unrelated stat infrastructure. The harness does not simulate Advanced Heal's healing/graphics or the AI scheduler after job rejection.

## Shared cast safety and automatic line of sight

`CastSafety` cases exercise the real engine start method and JecsLite's direct pre-cast/blocked-projectile methods. A controlled 20-by-20 map contains actual Thing, Edifice and Cover grids and a full wall; the native shoot-line algorithm includes leaning. A deliberately permissive custom verb demonstrates that a missing custom self/LoS check cannot bypass the shared guard. Its shot records execution without applying spell effects or rendering anything. Message interception records feedback without displaying UI.

The self rule honors `canTargetSelf` while retaining explicit `TargetSelf` category semantics used by legacy self abilities; it does not infer targeting from `isViolent`. The Player/AI origin uses the matching executing cast job's count marker, not faction or `playerForced`. Automatic selection receives the actual verb, filters nearby pawn candidates before random selection, and revalidates the target at start. Spell LoS requirements use the engine shoot line, while spells configured to ignore LoS remain usable. Generic automatic projectile launches recheck sight when `stopBurstWithoutLos` requires it and reject quietly; manual launch feedback is retained.

Job-driver cases prepare a valid toil, insert a wall before init, then exercise real init/cleanup. They verify no shot or `PostAbilityAttempt` after rejection and one attempt after success. Separate fixture-scoped hooks observe that rejected starts never reach the private psionic/Hate deduction methods; they do not execute the health/resource mutation itself. Preflight preserves those deductions' timing for accepted zero-warmup casts. The accepted-start marker is serialized by the driver, but these cases do not establish a complete save/load round trip, live cancellation behavior, a rendered mass battle or Multiplayer synchronization. Use the [live checklist](smoke-tests/validation-1.7.md).

## Kure overhead ability projectiles

`AbilityProjectile` cases use the installed JecsLite `PawnAbility.GetJob` for Player and AI contexts, followed by its real generic `Projectile_AbilityBase.ImpactSomething` and `Impact_Override` methods on a controlled map with a seeded launch state. The effect-recipient boundary records the pawn without applying health/mental-state changes; impact teardown is isolated from graphics. This is not a complete verb launch, job scheduler or Kure XML-loading test.

On RimWorld 1.7, overhead projectiles expose `HitFlags.None` during flight. The tested JecsLite impact method consequently rejects even the selected pawn, then its generic fallback applies effects to the caster. The unchanged DLL reproduces seven failures in the initial eleven-case comparison; the compatibility patches resolve them while preserving thick-roof blocking, ordinary ground hits and real self buffs. Additional cases retain deliberately disabled hits, reject mismatched used/intended targets, preserve full-cover rejection and deliberate derived-projectile caster effects, and leave cell-only and non-RWoM ability behavior unchanged. The patches are compiled only with `RW17`; no 1.6 assembly is built or replaced.

The [Kure live checklist](smoke-tests/kure-targeting-1.7.md) covers manual casts, autocast, ally buffs, misses and roofs. Headless success does not establish full launch/flight behavior, rendered projectiles, every addon spell, complete save/load, or Multiplayer synchronization.

## Flesh Development

FleshTargeting cases run production UI activation and the installed engine's Targeter/TargetingParameters on controlled spawned pawn/corpse fixtures. They verify selection of corpses and neutral living targets, invalid-target rejection, Thirst sorting priority and all four other self-active skills with cooldown/repeat checks. The meat stat uses a fixed worker to isolate selection; these do not execute mouse input, pathfinding, or the complete consumption job. ThingDefOf is initialized once during harness setup, with only its expected headless initialization warning accepted.

FleshPresentation cases cover grimoire placement settings independently of ability/resource visibility, learned active skills including cooldown-blocked entries, point labels, active/passive labels, thirst capacity updates and mutation-free eligibility. An actual inspect-hook fixture checks insertion, duplicate avoidance, shared-race isolation and selected-pawn tab visibility after switching settings. It also verifies the patched declaring method against the installed engine. Window rendering, tab click/deselection and command targeting still require live checks.

FleshProgress tests exercise the production per-pawn repository through actual Verse Scribe loading, reference resolution and post-load initialization. An envelope substitutes for a full Pawn save: tests distinguish absent old-save data from existing progression, preserve ranks/XP/Thirst/deadlines, cover humanoid and animal undead markers, and verify that ordinary pawns are excluded. A metadata check verifies that the class scanner can discover the Pawn.ExposeData hook. These are not full-colony save/load tests.

Gameplay checks exercise freshness/reward arithmetic, timer catch-up and one-shot expiry bookkeeping, rank values, passive stat arithmetic, actual damage-result aggregation and rage ownership cleanup. UI contracts cover language keys/placeholders, runtime artwork and synchronized command signatures. Runtime fixtures bypass asset-dependent definition constructors; the harness assembly is registered for Scribe to resolve its envelope type normally.

The [in-game checklist](smoke-tests/flesh-development.md) covers actual corpse consumption, healing, damage, rendered UI, old-colony loading and two-client Multiplayer. Headless checks do not establish those live outcomes.


The restoration case also calls production activation and real RemoveHediff with injuries, a permanent scar, missing natural parts and implants. Temporary fixture-scoped Harmony patches suppress DirtyCache and CheckForStateChange; capacities, rendered body refresh and world health transitions remain live-test responsibilities.

FleshHealthEffects checks load shipped health definitions through Verse XML inheritance and reconcile actual descriptive Hediffs, including learned ranks, normal/debug level changes, existing progression without indicators and duplicate prevention. They suppress only fixture health caches/state transitions; the display entries grant no stat stages. Effect checks run real self-action success/rejection and gameplay ticks through the production emitter boundary, verify compiled consumption/bite hooks, off-map safety and throttling. Shipped Fleck definitions inherit from the installed game's actual FleckBase/FleckBase_Thrown XML; texture paths, graphic class and shader references are checked. A controlled renderer boundary consumes random values to verify emission restores gameplay RNG and never modifies shared definitions. These checks do not render particles, exercise a live consumption job or certify camera behavior, mass-battle performance or Multiplayer synchronization.

FleshCasting checks real UI dispatch to an ordered-job boundary, then run the actual JobDriver delay/transition engine for 120/300 ticks. They verify no early cost, completion timing/once, cleanup cancellation, invalid caster rejection, vanilla progress-bar callbacks and bounded preparation particles. A real Scribe envelope preserves the Job's skill field and the driver's current toil/remaining time; pawn target binding is supplied by the fixture, so it is not a full colony/target-reference round trip. Renderer callbacks, health cache transitions and subsequent AI scheduling are isolated. The optional MP registration's metadata and local MP ShouldSync guard are checked/read separately; a two-client live test is still required.

FleshStat checks exercise the registered stat hooks, explanations and cache invalidation, including dependent crafting stats. The Health entries describe progression and active effects without applying a second set of stat bonuses. FleshBite checks exercise conversion to vanilla Bite damage and combat-log rules, critical bites, toxicity, Feast body-part consumption and eligible wound infection. Controlled fixtures isolate hit orchestration, damage-worker and anatomy/death side effects; they do not prove full battle-log rendering or manual Devour behavior in a live battle.

UndeadRelease checks request collapse on the first normal/interval callback after confirmed owner death, preserve living/unresolved links and skeleton exclusions, and verify re-entry protection. Guest reset runs the real engine method with the production prefix. Recording Pawn overrides isolate Kill and SetFaction for other cases; injected failures before/after faction assignment verify one release attempt and preservation of the real exception stack. They do not establish full corpse creation, all third-party faction callbacks or actual world scheduling. The original reported NullReferenceException's exact source remains unproven because the old handler discarded its stack.
# Focused ordering checks

`run-regression-tests.ps1 -Filter 'ClassState,LegacyDataRefresh,Teleport'` selects
the union of comma-separated name substrings. A single substring keeps its existing
meaning. Add `-Reverse` to run the selected cases in reverse registration order in
one process. Always pass an `-ArtifactsDir` outside the repository.

## Odyssey magical thrusters

Use `-Filter 'Odyssey.MagicalThruster,Odyssey.Thruster'` for the flight-fuel and definition checks. The runner builds the optional `TorannMagic.Integrations/Source/TorannMagic.VGE` adapter into the external artifacts directory when the installed VGE assembly is available. `-VgeDir` selects that read-only dependency directory; the core assembly has no mandatory VGE reference.

The launch fixture executes the installed `Building_GravEngine.ConsumeFuel` with production Harmony hooks and real `CompRefuelable` payments. It supplies native floor/room caches and a cached route cost, so this verifies payment and rollback, not world-map generation or pathfinding. VGE cases use its real provider enumeration, payment and refund APIs. Definition cases resolve native XML inheritance and apply the shipping patches. Sprite checks verify directional files and alpha-capable PNGs; visual alignment, exhaust animations, the world target UI and an actual departure/landing still require an in-game check.

### Integration extraction candidates

The runner builds matching Core sources and `TorannMagic.Integrations` into the selected external ArtifactsDir, alongside conditional `TorannMagic.VGE`. `-CoreAssembly` selects an explicit matching Core binary; `-WithoutCore` keeps Core unloaded in the harness while retaining build references. Core-required VPE cases then report capability skips. Use `Core.CastOwnership,VpeAutocast,PridestalkerCompatibility,DebugClassRoster,IntegrationPackage` for the migration contracts and `GrimoireKure` for class data coverage. The shipped Core example accepts `-p:CoreAssemblyPath=...` for candidate validation. These commands do not update repository/install DLLs.
