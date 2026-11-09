# Public API for add-on authors

Build against RimWorld 1.7, .NET 10, the game's Assembly-CSharp.dll and the installed Core assembly. Use the same game assemblies as your players. Set Private=false for game and Core references. Ship your own assembly only. Code that directly uses these types requires Core at runtime.

~~~xml
<!-- Inside modDependencies in your About.xml -->
<li><packageId>huginn.rwom.core</packageId><displayName>RWoM Core (Ability Framework)</displayName></li>
~~~

~~~xml
<!-- Inside ItemGroup; set RWoMCoreDir to the installed Core mod directory -->
<Reference Include="TorannMagic.Core" HintPath="$(RWoMCoreDir)/Assemblies/TorannMagic.Core.dll" Private="false" />
~~~

## Spell lookup

Call on the game thread after Def loading. AbilityCore.TryResolve(string, out Def) reports false for a missing ID. Resolve returns null in that case. Compare IDs exactly, including case. Registry exposes a read-only view. Acquire it again after a database rebuild. Register(id, def) adds a runtime alias. Prefer XML for a published identity. Invalidate() forgets the current index; the next lookup rebuilds XML identities and rebinds registered aliases to the currently loaded type and Def name. Call it only when managing a Def reload, never per tick.

Declared IDs are indexed before aliases during a rebuild. In a conflict the first accepted binding is kept; the warning identifies the ID. Registering the very same Def twice is harmless. The extension's domain and tier fields are metadata. The current resolver returns one Def per ID. There is no family-and-tier lookup. The main mod also supplies convention IDs derived from Def names. An explicit stable XML ID is preferable for a published add-on.

## Need-based resources

TMResource.FindNeed returns the native Need or null. TryGetLevel returns current and maximum in native need units. A UI showing 100 points may use a different scale; use the returned maximum. TryConsume checks the balance and deducts on success. TryAdd calls the native clamping setter and returns void. Missing needs and negative or non-finite amounts cause no balance change. Zero is a valid no-op for a present need. IsMagicUser and IsMightUser test presence of mana and stamina respectively; neither proves class membership. Chi is currently a hediff. The Chi identifier cannot be used to read or spend hediff severity through these methods.

~~~csharp
using TorannMagic.Core;
using Verse;

public static class GlassWard
{
    public static bool CanOffer(Pawn pawn)
    {
        return AbilityCore.TryResolve("myaddon.glassward", out _)
            && TMResource.TryGetLevel(pawn, TMResource.Mana, out float mana, out float max)
            && max > 0f && mana >= max * 0.10f;
    }
}
~~~

Read-only eligibility checks must not consume resources. For an add-on-owned action, charge at its single authoritative commit point. If RWoM's cast already charges the pawn, leave payment to RWoM.

## Boundaries

A registered Def identifies spell content. Use PawnSpellKnowledge.Read for learned spells and upgrades. Casting permission is a separate gameplay decision. Core does not currently expose a class enrollment, casting, save repair, or knowledge mutation API. Keep durable IDs in your save data and resolve them when needed. Preserve unresolved IDs across save/load so removing and restoring an optional add-on does not erase knowledge.

## Build the included example

Examples/AddonExample.csproj is a small consumer you can build outside the game directory. Copy the Examples folder to a working directory. Set both paths to your installation and run in PowerShell:

~~~powershell
dotnet build ./AddonExample.csproj -c Release `
  "-p:GameDir=C:/Program Files (x86)/Steam/steamapps/common/RimWorld" `
  "-p:RWoMCoreDir=C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/RWoM Core"
~~~

The output is an example client library. To exercise CanOffer in your own mod, call it from the UI that offers your ability, after the Def database has loaded. Supply your own gameplay ability with the example ID. Compiling the sample does not register a spell.

## Choosing required or optional Core support

If the add-on requires Core to work, use the dependency snippet above. If the add-on still works independently, keep Core-specific C# types out of the assembly that loads unconditionally. In an optional XML integration place `MayRequire="huginn.rwom.core"` on the extension's li element. Test a cold game start both with and without Core. This avoids a missing-type XML error during startup.

## Compatibility when publishing an update

* Def renamed, same gameplay spell: keep its explicit ID unchanged, including its case. Test with a save made before the rename.
* Two spells intentionally share a visual or translation: assign an ID to each gameplay Def. Labels and icons are presentation and do not identify a spell.
* Resource cost shown in the UI: compute it in the same native units as payment. Recheck at commit because another effect may have spent mana since drawing the command.
* New rank, cast, discipline or resource API: confirm the member exists in the minimum Core version your add-on supports. This release does not expose a general capability negotiation method. The public surface is in the TorannMagic.Core namespace.

## Maintaining binary compatibility

The compatibility boundary is the published TorannMagic.Core assembly, its public type names and method signatures, and the field names used in XML. Add-on authors should preserve their own spell IDs across releases. Core maintainers should extend the API additively, keep existing entry points available, and exercise a previously compiled client before shipping. If an incompatible change becomes necessary, document the migration and keep a compatibility entry point where possible. The main mod owns pawn progression and Scribe data; a Core update should not require rewriting a player's save to use the lookup API.

## Reading a pawn's spell knowledge

Call `PawnSpellKnowledge.Read(pawn, stableId, out var snapshot)` on the game thread after startup. Supply the exact ID stored for the spell. The returned value is an immutable copy. Query again after training or load. Reading never grants a spell, starts migration, or spends resources.

| KnowledgeStatus | Meaning and handling |
|---|---|
| InvalidRequest | Null pawn or blank ID. Correct the input. |
| ProviderUnavailable | Core alone, older host, or host startup still pending. Disable the knowledge-dependent feature. |
| PendingImport | The pawn awaits its first import or post-load reconciliation. Wait for a normal pawn tick before showing ranks. |
| NoRecord | No knowledge component or no entry for this ID in the ready store. |
| MissingDefinition | A saved entry exists but its Def is absent. The snapshot is valid. Show it as dormant and preserve its ID. |
| Available | The record and Def exist. Check Learned independently. |

Only Available and MissingDefinition supply meaningful snapshots. Fields are SpellId, Learned, Tier, Power, Efficiency, Versatility and Autocast. Autocast records a preference; it does not authorize a cast. The query works with class progression beta off. Never write the snapshot back to Scribe.

~~~csharp
var status = PawnSpellKnowledge.Read(pawn, "myaddon.glassward", out var spell);
if (status == KnowledgeStatus.Available && spell.Learned)
    return $"Tier {spell.Tier}, power {spell.Power}";
if (status == KnowledgeStatus.MissingDefinition)
    return "Remembered spell (content unavailable)";
if (status == KnowledgeStatus.PendingImport)
    return "Waiting for pawn data";
return "No displayable spell";
~~~

A direct reference to this API requires a Core release containing PawnSpellKnowledge. Update Core and the host together. New Core with an older host returns ProviderUnavailable.

## Disciplines, hybrid characters and classic add-ons

Call `PawnDisciplines.TryRead(pawn, out var rows)` after startup on the game thread. False indicates an absent provider or null pawn. True with zero rows means the query ran and found no active disciplines. Rows are immutable values, detached from subsequent progression. Query on demand when refreshing a panel.

Each row exposes Id, Kind and Stage. Native progression uses saved profile IDs (for example rwom.fire and rwom.ice) and Foundation, Adept, Master or Ascended. A hybrid can have two different stages. A classic character matching a discipline definition has its profile ID and stage Classic. For registered custom classes, including Kure, identify the exact trait and degree: legacy.TM_Pridestalker.4. Magic and Might are flags; one row can have both. An unknown degree, a suppressed trait or a potential trait does not grant a registered custom discipline. With progression disabled, inactive saved foundations are not reported. Reading never enrolls a character.

## Asking whether a cast may start

Use `AbilityCasting.Check(verb, target, CastMode.Manual)` or `CastMode.Automatic` with the actual RWoM casting verb. The external client needs only Verse types and Core. UnsupportedAbility means a foreign or unsupported verb: leave its decision to its owner. ProviderUnavailable indicates missing host integration. Only Allowed is positive. Other results identify invalid input, off-map caster, occupied cast, forbidden violence, caster restrictions, invalid target, automatic policy rejection, or an ability that is not ready.

The query does not create a job, pay costs, start a cast, or change the current queue. It consults the owning ability's eligibility and targeting methods; custom implementations must keep those checks safe for previews. Automatic checks include RWoM combat policy and external cast ownership. Check again at the point of execution; a positive preview is neither a reservation nor a grant of permission to bypass the normal cast entry point. Use the same game-thread and version rules as for knowledge queries.

## Gameplay examples from the progression beta

* An elemental hybrid can return both `rwom.fire` and `rwom.ice`. Inspect each row independently. One can be Ascended while the other remains Master.
* Chaos Tradition grants temporary spells. “Learned” describes the current record; it does not promise permanent ownership, mastery eligibility, or that the spell will survive the next draw. Read a fresh snapshot after a reroll. Never restore a missing temporary ability from a cached snapshot.
* The host handles ritual payments, specialization changes, and the soul/body split during possession. An add-on displaying these states should not edit resource balances or reapply physical effects. Core does not currently expose a spell specialization or chaos-anchor writer.

## External cast ownership (Core-only)

`CastOwnership.Register(string id, Func<Pawn, bool> provider)` installs a read-only ownership probe. Use a stable, unique ID such as `myaddon.cast-owner`. It returns false for an invalid ID/callback or an already registered ID; it never replaces another provider. `Unregister(id, provider)` removes only the matching callback. Keep the original delegate if you need to unregister. Register once during startup, after assemblies are available.

`CastOwnership.IsCasting(pawn)` returns true when any provider currently owns that pawn's casting; null or no provider returns false. Queries are live, not cached per pawn. Providers must inspect the actual active job/warmup, not a stale last-cast flag. A true result is occupancy, not combat evidence, hostility or permission to cast. RWoM's automatic entry points consult it; it does not start/cancel jobs or charge resources. The API works with Core alone.

All operations are game-thread-only. Probes must be fast, non-mutating and must not register/unregister during a query. Do not retain pawn/map references in global callbacks. If a provider throws, Core logs once and reports busy for that query so automatic casting cannot interrupt an unknown owner. Fix or unregister a broken probe; do not treat failure as permission to overwrite a cast.

The VEF/VPE-specific probe is supplied by RWoM Integrations, not by Core or Content. Integrations requires the matching Core release containing this API. Main RWoM remains loadable with Core absent or older; absence then means no registered external ownership guard, not guaranteed compatibility with optional mods.
