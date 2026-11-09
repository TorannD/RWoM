# Integration ownership and verification

This is the current ownership index for the three-package split. The chronological implementation record remains in `plan.md`. A moved source file is not by itself proof of runtime or save compatibility.

Paths below are relative to the repository. Adapter source roots are `TorannMagic.Integrations/Source/`; Content source is `RimWorldOfMagic/RimWorldOfMagic/`. Verification artifacts are outside the repository under `../artifacts/integrations-extraction/`.

| Integration | Implementation owner | Content boundary / retained state | Evidence and limitations |
|---|---|---|---|
| MedPod, Hellsing, Kure potion | Integrations `v1.7/Patches` | Existing definition identities; no new saved classes | `i2-final`: native patch application and conditional load-folder enumeration. Original patch gates retained. |
| VGE 1/2 | `Source/TorannMagic.VGE`, conditional `v1.7/VGECompat` | Content owns Odyssey fuel, oxygen supply credit and shield policy; adapter owns VGE interfaces. `TorannMagic.VGE` assembly and component identities retained. | `i3-report.md`, old/new native VGE save/payment logs; saved ship references and exact debit preserved. Not a claim of graphical flight acceptance. |
| Kure class corrections | `TM_PridestalkerCompatibility.cs` in main adapter | Generic custom classes, grimoire and saved progression stay Content | `i4-report.md`, `i457-final-native`: installed Kure definitions and focused regression. Does not take ownership of Kure's own fork. |
| VEF/VPE | `VefCastOwnership.cs` | Core owns neutral `CastOwnership`; Content queries through `TM_ExternalCastOwnership`/CoreBridge | `i4-report.md`, `i457-final-native`; absence/presence, duplicate-action safeguards. No vendor assembly in Core. |
| Alien Races | `AlienRaceTraitRestrictions.cs` | Public `ModCheck.AlienHumanoidRaces` facade; Content decides initiation/trait grants | `i5-har-report.md`, `i457-final-native`; actual optional race types. |
| Pawnmorpher | `PawnmorpherRaceCompPatch.cs` | Existing magic/might/knowledge/progression comp instances stay attached to their pawn | `i5-pawnmorpher-report.md`, native pruning/Scribe tests. Installed upstream is 1.6; this safeguard is not a full port of Pawnmorpher to 1.7. |
| Giddy-Up | `GiddyUpAdapter.cs`, `EquipmentPatches.cs` | Content equipment callback and old `ModCheck.GiddyUp` facade; travel owns when dismount is necessary | `i5-equipment-report.md`, `i457-final-native`; actual 1.7 fork API. |
| Simple Sidearms | `SidearmsAdapter.cs`, `EquipmentPatches.cs` | Content weapon-memory callback and old `ModCheck.SS` facade | Same equipment packet; current and legacy component names recognized. Vendor state remains vendor-owned. |
| Legacy optional policies | `LegacyModPolicies.cs`, `LegacyBirthAdapter.cs` | `OptionalCompatibility` defaults to false/no-op; `ModCheck.Validate` forwards calls | `i5-legacy-report.md`; Prison Labor, No Job Authors and Children have actual callers. Other old detector names do not imply full supported integrations. |
| BBBodySupport | `LegacyBodyTextures.cs` | Original cloak lookup remains Content | `i1-residual-first` / `i1-residual-reverse`; preserves old texture warming. Original discarded `AddToArray` result is not turned into a new feature. |
| VGE oxygen help gate | `LegacyModPolicies` capability `ShipOxygenGuidance` | General guidance UI and receipts stay Content | Residual packet above; provider absence/presence. |
| Multiplayer | Conditional `Source/TorannMagic.Multiplayer` | `TMPCompat` and `FleshMultiplayer` public facades; deterministic gameplay remains Content | `i6-cross-map-checkpoint.md`; native registry/serialization and no-provider tests. Live two-peer/two-map Blink payment, cooldown and arrival checks passed; final delivery gate and installed hash audit passed. Do not close the general Multiplayer bug merely because extraction is complete. |
| Isekai | Conditional `v1.7/IsekaiCompat` | Generic `TMIsekaiUI` rendering helper stays Content | Native package tests; existing external exact-name gate remains unresolved and documented. No second leveling-bonus implementation. |
| Logging Framework | `LoggingBackend.cs`, `LoggingBackendPatch.cs` | Public `TM_Log`, reentrancy guard and `Verse.Log` fallback stay Content | `i457-final-native`: provider readiness, failure, absence, reentrant and concurrent behavior. |

## Deliberately retained or inactive

- Official DLC behavior belongs to Content, not the optional Integrations package.
- Mandatory JecsLite ability ownership, general cast safety, recovery, rank handling and deterministic effects remain with Content.
- Core contains neutral public contracts; it must not reference optional vendor assemblies or Content gameplay types.
- Old detector facades for SRTS, SOS2, Dual Wield, Vampires and outdated mod names are compatibility surface, not newly implemented support.
- `RimQuest_Questsx.xml` has commented-out operations. Do not reactivate it as part of extraction. Vanilla Events Expanded work is a separate future task.
- Generic custom-class data and `TMIsekaiUI` are not vendor adapters despite their call sites/names.

## Evidence boundaries

Original caller searches are retained in `adapter-callers.txt` and `legacy-check-callers.txt`; the updated source search is `final-source-boundaries.txt`. The installed manifest audit is `../artifacts/package-audit-20261105/final-recheck/audit.json`. These support ownership and packaging review, not exhaustive runtime acceptance of all mod combinations.

The final installed matched snapshot passed2641 regression cases with24 explicitly quarantined baseline cases, staged reverse48/48 and installed audit5473/5473. The exact final Multiplayer DLL also passed the live two-peer/two-map check. I1–I8 are complete; consult `plan.md` and `../artifacts/integrations-extraction/final-report.md` for delivery evidence.


