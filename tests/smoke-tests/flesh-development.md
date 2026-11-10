# Flesh Development: in-game checklist for 1.7

This checklist requires a running game. It is not a claim that all items have been tested. Headless tests do not establish rendered effects, full-colony loading, pathfinding, battle performance or two-client Multiplayer behavior. Use a disposable colony or a copy of a save.

## Progression and existing saves

1. Load an old save containing raised human and animal undead. Undead without progression data start at level 1, 0 XP, 0 points, Devour rank 1 and Thirst 20/20. Ordinary living pawns must not receive the grimoire. Starting Thirst is full, so the maximum-hunger speed penalty applies until feeding.
2. Gain XP, spend points, activate an enhancement, save and reload. Preserve ranks, level, residual XP, Thirst and ability deadlines without duplicate points or resets.
3. Consume fresh, older and damaged corpses of the same species. Compare rewards based on freshness and remaining meat. Check equipment preservation, one reward per corpse and no reward for an interrupted consumption job.
4. Bite a living organic target with Devour: low damage, reduced Thirst and no XP. Invalid/disappeared targets must not grant a reward.

## Skills, costs and penalties

5. Restore Flesh should heal injuries/scars and missing natural parts while preserving implants and undead status. Consume all XP toward the next level, retain level/points/ranks and reject use at zero XP.
6. Empower Flesh and Harden Skin last 300 seconds at normal speed. Check bonuses and one-time expiry damage, including save/reload and off-map expiry. Pausing must stop simulation timers.
7. Verify passive limits of 10/10/10/8 and maximum Thirst 100. Crafting must receive the general work multiplier once. Feast stays locked until all other skills are fully developed.
8. Feast starts at 50% Thirst, adds hunger and uses bites. At maximum Thirst it triggers Berserk, which can outlast the normal 60-second effect until Thirst reaches 20% or the master stops it. Reaching zero ends an ordinary Feast.
9. The master can stop one servant or all owned servants, including Feast-owned rage. Preserve Thirst and cooldown, recover only the state owned by Feast and do not alter unrelated mental states.
10. Check natural hunger growth and the 80%/maximum speed penalties. A living owner off-map or in a caravan remains valid.

## Interface, stats and effects

11. Check the red grimoire, learned ability commands, Thirst placement, active/passive labels, missing-point labels, scrolling and targeting. Test both pawn-tab and ability-bar placement. In God mode, the level-up control grants one level/point and refreshes the interface.
12. Health entries must show level, learned passive ranks and bonuses without duplicate stat effects. Active enhancements show rank and remaining time. Compare movement, work/crafting, melee damage and armor before/after upgrades; stat explanations and dependent cached values must refresh.
13. On a visible map, check successful Devour/bite particles, Restore pulses, Empower circles, Harden effects and Feast effects. Active auras should stop on expiry or owner cancellation; Feast-owned rage keeps its aura. Rejected/interrupted actions must not produce success effects.
14. Repeat with many undead, different camera positions and off-map pawns. No visual code may assume a usable map or fleck renderer. Check actual frame rate separately from headless correctness.
15. Empower/Harden prepare for 2 seconds; Feast prepares for 5 seconds, using the normal progress indicator. Apply effects, Thirst and cooldown only at completion. Movement orders, downing, death, despawning or a mental break cancel preparation without cost. Save/reload midway and preserve remaining time. Restore stays instant; Devour uses its own preparation job.

## Bites and owner death

16. During Feast, melee attacks must use vanilla Bite damage and bite combat-log rules. Check the separate 20% body-part consumption and 30% eligible wound-infection rolls. Successful organic bites add small toxic buildup reduced by toxic resistance. Misses and fully absorbed attacks must not apply these effects. Outside Feast, check the 10% critical bite chance and +50% damage over enough attacks to assess probability.
17. Kill a master with several human/animal minions. Each should collapse on its next normal/interval update. Include off-map minions; preserve other owners' servants and independent skeletons. A temporarily unresolved load reference must not count as confirmed death. Keep any new full faction-release warning stack for diagnosis.
18. On two Multiplayer clients, check upgrades, casting, feeding and owner commands, including differing camera positions. Headless metadata checks do not establish synchronized live battles.
