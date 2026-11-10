# Developing an add-on with RWoM Core

1. Read API.md and inspect the installed TorannMagic.Core.dll for the target release. Check packageId huginn.rwom.core. Match the actual public signatures before generating XML or compiling C#.
2. Keep references to the game and Core non-copy-local. For optional integrations isolate code or XML that references optional types so it is not loaded when the dependency is missing. A loadAfter entry alone does not make a direct reference optional.
3. Choose unique permanent spell IDs. Keep the spelling stable through Def renames. Domain and tier do not form a composite lookup key. Do not infer a pawn's class from a resource need.
4. Resolve on the game thread after Def loading. Handle unknown IDs, null pawns, absent needs and removed add-ons. Use returned maximums for percentages. Do not spend resources while rendering gizmos, searching for targets or deciding AI eligibility.
5. Compile a consumer without a reference to TorannMagic.dll for features advertised as Core-only. Exercise Core alone, Core with the main mod, and the main mod without Core. Check an existing save on a copy, disable and restore optional content, and verify spell IDs and balances survive.
6. Report which combinations actually ran. Distinguish compilation, automated runtime checks and manual in-game checks. Do not advertise planned class enrollment, chi or blood APIs as available.
7. Read knowledge through PawnSpellKnowledge.Read. Only Available and MissingDefinition supply valid snapshots. Preserve dormant IDs. Never translate PendingImport into rank zero or trigger migration from UI.
8. Read all discipline rows (Kind is a flag set); Classic has no equivalent initiation tier. For cast previews pass the actual Verb and mode. Treat UnsupportedAbility as outside RWoM, and never issue a cast solely from a cached Allowed result.

9. For external cast ownership, use CastOwnership with a stable provider ID and a side-effect-free, game-thread-only probe. Keep vendor type checks in the optional adapter. Do not cache pawn ownership or infer combat eligibility from a true result. Never register/unregister inside a probe.
