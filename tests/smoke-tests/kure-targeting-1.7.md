# Kure spell targeting on RimWorld 1.7

This is a pending live-test checklist, not a record of completed gameplay tests. Use a disposable colony with Harmony, JecsLite, RWoM and Kure 1.7, plus the DLC used in the report. Restart the game after changing assemblies. Test only 1.7.

## Enemy effects

1. Give one colonist Wind Mage and another Harbinger. Place a hostile pawn within range on open ground. Disable autocast first.
2. Manually cast Gust and Infuriate on the enemy. Check the actual effect recipient, projectile destination and caster Health/Mental State. A correctly logged cast alone does not prove a correct hit.
3. Repeat with Stifle and the remaining learned enemy-targeted spells, including upgraded ranks. Record which spell/rank fails rather than treating all Kure abilities as equivalent.
4. Repeat with autocast enabled, then with hostile Wind Mages and Harbingers casting at colonists. Check effect recipients independently of projectile graphics.

## Allies, misses and roofs

1. Cast Windstep with the caster and an ally in its area. Confirm both receive the intended speed effect. A working self buff alone does not establish correct ally targeting.
2. Move or despawn the target during flight. A missed external-target effect must not be applied to the caster.
3. Check thick-roof blocking, ordinary cover, downed targets and different maps. Do not expect the compatibility patch to bypass those native restrictions.
4. Check a genuine self-target buff and an ordinary ground projectile from a base RWoM class.

## Integration

1. Save/reload while ability projectiles are in flight; verify their original target/effect behavior after loading.
2. Repeat the minimal setup in the target modpack. If Multiplayer is used, verify both clients remain synchronized during manual casts and autocast.
3. Record RimWorld, JecsLite, RWoM and Kure versions and the full log, including Harmony patch failures. The compatibility patch targets the generic JecsLite terminal-impact path; custom derived projectile behavior still needs individual live checks.
