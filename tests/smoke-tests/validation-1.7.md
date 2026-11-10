# RWoM 1.7 in-game regression checklist

This is a checklist, not a record of completed live tests. Command-line fixtures do not establish map behavior, rendering, full-colony save compatibility or Multiplayer synchronization. Use a disposable colony or a copy of a save. Only 1.7 is covered.

## Skills and persistence

1. Load an existing save with Empath and spirit skills. Open the Grimoire and let simulation run. Check for duplicate-key exceptions and skill lookup errors.
2. Check Empathy, Suppressive Aura, Distort Spirit, Spirit Drain, Spirit Possession, Spirit Storm and Harvest Passion against their XML level limits and costs. Relevant Empathy skills have a maximum of 5 rather than the usual 3.
3. Spend points, save, return to the menu and reload. Verify levels and remaining points. Reinitializing a class must not duplicate skill objects or reset invested levels.
4. Check legacy Russian skill identifiers independently of the current UI language. Levels already missing from an earlier damaged save cannot be reconstructed; an earlier save or manual compensation is required.

## Death, dismissal and summoned creatures

1. Kill and dismiss undead, summoned pets and Poppi. Check for stalled jobs and repeated exceptions.
2. Check DeathField, mage/fighter kill events and hollow-golem reactions.
3. Repeat with the pawn-management mods used by the target modpack. If faction release throws, the log must retain the full stack and whether the faction was already cleared. RWoM must allow the death prefix to continue; this does not guarantee that every other death callback succeeds. The original warning did not establish which mod or engine method caused the NRE.

## Autocast and targeting

1. Meteor and Orbital Strike must remain manual-only; verify that manual casts still work.
2. Enable Blink autocast, select another pawn and assign the caster distant work. Check useful movement, valid destinations, walls and unchanged selection/camera. Repeat with camera snapping and autocast queues enabled/disabled. Check manual Blink separately.
3. For Cure Disease, an injured pawn without a supported disease must not cause useless casts. Check an eligible disease and sufficient spell rank.
4. Herbal Clarity and Herbal Bliss must expose persistent autocast toggles and work on valid non-combat targets.
5. Check magic, might and hybrid classes with upgraded and standalone abilities. No duplicate attempt per evaluation pass or cooldown bypass should occur. Include drafted Chaos Mage healing.
6. Check self-cast healing with stale or cell-only targets, canceled warmups and despawned casters. Reject invalid casts without charging resources.
7. With both a colonist and a hostile NPC caster, attempt an external-target spell marked `canTargetSelf=false` on the caster. It must not fire or spend resources. Check an allowed self heal/buff and explicit `TargetSelf` abilities separately.
8. Compare manual and automatic casts at a target behind a wall, then behind ordinary leanable cover. Automatic selection must use the spell's actual targeting/LoS rules; spells configured to ignore LoS must retain that behavior. When choosing nearby targets, include both a visible and a blocked pawn.
9. Close a door or move the target during warmup/flight. An automatic LoS rejection must finish without repeated player-facing rejection messages; manual rejected casts must retain their feedback. Spells that explicitly allow a burst to continue after losing sight retain that continuation policy.
10. Repeat rejected starts with Psionic and Death Knight abilities. Verify psionic energy/Hate as well as mana/stamina and cooldowns; accepted casts must retain their usual cost. Save/reload during an accepted warmup and check cost/cooldown behavior once, without claiming the headless marker check covers a full colony load.
11. Repeat a 50-versus-50 debug battle and, separately, the target Multiplayer setup. Check logs, rejection-message spam, spell recipients, performance and synchronization. These live checks are pending.

## Grimoire and descriptions

1. Check Faceless Grimoire access with and without another same-race mage. Switching between pawn-tab and ability-bar placement must avoid missing or duplicate entries.
2. Review translated descriptions for mana regeneration, mining magicyte, animal autocast targets and the local paracyte penalty. Do not promise the removed awakening progression.
3. Check off-map Magic Shield expiry and damage to undead without ordinary needs. No particle or needs access should assume a spawned pawn.

## Combat log

1. Cancel an ability before completion and reject an invalid target. Neither should produce a successful-cast entry.
2. Complete a valid instant pawn-targeted cast: one appropriate entry. For projectiles and bursts, a cast entry records successful launch, not a later hit; actual damage remains separate.
3. Legacy Hex/Enrage verbs may apply effects but return false, so cast logging is not guaranteed for them. Cell/AoE casts without a reliable primary pawn must not invent one.
4. Save/reload during a burst. Resuming without a new warmup must not invent or duplicate a cast entry.

## Isekai

1. Load the target Isekai version with dependencies. RWoM retains package ID `Torann.ARimworldOfMagic`.
2. Known unresolved issue: the tested Isekai XML gate expects the exact name `A RimWorld of Magic`; this fork intentionally keeps `A RimWorld of Magic — 1.7 (Unofficial)`. Its RoM bonus patch will not activate through that gate. The two associated automated failures remain visible. Do not claim integration is working until recognition is fixed and retested.
3. Once recognized, compare maximum mana/stamina, regeneration, cooldowns and resource costs. Do not claim native might-damage or awakening-XP integration. Do not restore the removed TMPIsekai layer or duplicate multipliers.
4. Verify independent operation without Isekai.
