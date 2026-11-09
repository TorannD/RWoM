using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.SpellItems;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in ShadowStrikeCases()) yield return test;
        foreach (var test in TravelEnvironmentCases()) yield return test;
        foreach (var test in ExtendedCases()) yield return test;
        foreach (var test in EtherShellCases()) yield return test;
        foreach (bool hit in new[] { false, true })
            yield return new("CombatStress.SeerLightning." + (hit ? "HitDestroyedTarget" : "Miss"), f => World(f, w =>
            {
                var type = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Weapon.SeerRing_Lightning")!;
                var laser = (AbilityUser.Projectile_AbilityBase)Activator.CreateInstance(type)!;
                laser.def = ProjectileDef(); laser.def.projectile.damageDef = new DamageDef { defName = "TestLightning" };
                // A departed launcher is a legal projectile state; no stat/UI fixture is needed.
                SetProjectileField(laser, "origin", w.Caster.Position.ToVector3Shifted());
                var destination = new IntVec3(16, 0, 8);
                SetProjectileField(laser, "destination", destination.ToVector3Shifted());
                Set(laser, "positionInt", destination); Set(laser, "mapIndexOrState", (sbyte)0);
                var target = new Thing { def = Blank<ThingDef>() };
                var hitCell = new IntVec3(12, 0, 7); Set(target, "positionInt", hitCell); Set(target, "mapIndexOrState", (sbyte)0);
                using var patches = new Patches();
                Set(Current.Game, "battleLog", new BattleLog());
                Find.TickManager.gameStartAbsTick = 1;
                patches.Prefix(typeof(FleckMaker).GetMethod(nameof(FleckMaker.Static), [typeof(Vector3), typeof(Map), typeof(FleckDef), typeof(float)])!, nameof(SkipSeerVisual));
                patches.Prefix(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowMicroSparks))!, nameof(SkipSeerVisual));
                patches.Prefix(typeof(Verse.Sound.SoundStarter).GetMethod(nameof(Verse.Sound.SoundStarter.PlayOneShot))!, nameof(SkipSeerVisual));
                if (type.GetMethod("EmitImpactVisuals") is MethodInfo visuals)
                    patches.Prefix(visuals, nameof(CaptureSeerVisuals));
                patches.Prefix(type.GetMethod("StaticExplosion", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(CaptureSeerScatter));
                patches.Prefix(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!, nameof(CaptureSeerDamage));
                seerCells.Clear(); seerDamage = 0;
                laser.Impact_Override(hit ? target : null!);
                var center = hit ? hitCell : destination;
                Check.Equal(hit ? 1 : 0, seerDamage, "direct damage only on a real target, exactly once");
                Check.Equal(center, seerVisualCell, "visual center captured before target destruction or from miss destination");
                Check.Equal(true, seerCells.Count >= 1 && seerCells.Count <= 7, "scatter effects still occur on hit and miss");
                Check.Equal(true, seerCells.All(c => CellRect.CenteredOn(center, 2).Contains(c) && c.InBounds(w.Map)), "scatter stays around captured impact cell");
            }));
        yield return new("SpellItemEffects.TargetPreviewUsesRangeSightAndProjectileRadius", f => World(f, w =>
        {
            using var defs = new DefinitionScope<ThingDef>(ProjectileDef());
            using var patches = PreviewBoundaries();
            foreach (bool equipped in new[] { false, true })
            {
                previewRings.Clear(); previewLines.Clear();
                var props = Props(SpellItemKind.Fireball); props.range = 24; props.requiresEquipped = equipped;
                SpellItemGizmos.DrawPreview(w.Caster, props, w.Patient);
                Check.Equal(2, previewRings.Count, "scroll/staff draws range and blast radius");
                Check.Equal(w.Caster.Position, previewRings[0].center, "range centered on caster");
                Check.Equal(24f, previewRings[0].radius, "range from item properties");
                Check.Equal(w.Patient.Position, previewRings[1].center, "blast centered on target");
                Check.Equal(1.9f, previewRings[1].radius, "same fixed radius as real item projectile impact");
                Check.Equal(SimpleColor.Green, previewLines.Single(), "legal target has green line");
                Check.Equal(true, previewRings[0].predicate(w.Patient.Position), "visible target included");
                w.Fog(w.Patient.Position, true);
                Check.Equal(false, previewRings[0].predicate(w.Patient.Position), "fog excluded");
                w.Fog(w.Patient.Position, false);
            }
            w.Wall(); previewRings.Clear(); previewLines.Clear();
            SpellItemGizmos.DrawPreview(w.Caster, Props(SpellItemKind.Fireball), w.Patient);
            Check.Equal(false, previewRings[0].predicate(w.Patient.Position), "real wall clips LoS contour");
            Check.Equal(SimpleColor.Red, previewLines.Single(), "blocked shot has red line");
        }));
        yield return new("SpellItemEffects.TargetPreviewBlinkIgnoresWallsHealHasNoBlast", f => World(f, w =>
        {
            using var patches = PreviewBoundaries(); w.Wall();
            previewRings.Clear(); previewLines.Clear();
            var target = new IntVec3(14, 0, 6);
            SpellItemGizmos.DrawPreview(w.Caster, Props(SpellItemKind.Blink), target);
            Check.Equal(1, previewRings.Count, "blink has no blast ring");
            Check.Equal(true, previewRings[0].predicate(target), "blink contour crosses real wall");
            Check.Equal(SimpleColor.Green, previewLines.Single(), "legal blink beyond wall stays green");
            previewRings.Clear(); previewLines.Clear();
            var heal = Props(SpellItemKind.Heal); heal.range = 12;
            SpellItemGizmos.DrawPreview(w.Caster, heal, w.Patient);
            Check.Equal(1, previewRings.Count, "heal has no invented area of effect");
            Check.Equal(12f, previewRings.Single().radius, "heal range from properties");
            Check.Equal(SimpleColor.Red, previewLines.Single(), "heal blocked by wall");
            previewRings.Clear(); previewLines.Clear();
            SpellItemGizmos.DrawPreview(w.Caster, heal, LocalTargetInfo.Invalid);
            Check.Equal(1, previewRings.Count, "range visible even without target");
            Check.Equal(0, previewLines.Count, "no line to invalid cell");
        }));
        yield return new("SpellItemEffects.HealingTargetsSnapshotsFirstThreeDistinctNaturalParts", f => World(f, w =>
        {
            var pawn = w.Patient;
            var scar = Injury(pawn, Part("Scar"), 10, permanent: true);
            Injury(pawn, null, 10);
            Injury(pawn, Part("Empty"), 0);
            pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = "Disease" } });
            var firstPart = Part("First");
            var first = Injury(pawn, firstPart, 2);
            Injury(pawn, firstPart, 20);
            var second = Injury(pawn, Part("Second"), 12);
            var third = Injury(pawn, Part("Third"), 8);
            Injury(pawn, Part("Fourth"), 30);

            Check.Equal(false, scar.CanHealNaturally(), "fixture scar uses real natural-healing eligibility");
            var snapshot = SpellItemEffects.HealingTargets(pawn);
            Check.Equal(3, snapshot.Length, "at most three distinct body parts, not three wounds on one part");
            Check.Same(first, snapshot[0], "first eligible wound on the first part wins even when smaller");
            Check.Same(second, snapshot[1], "stable hediff order selects the second part");
            Check.Same(third, snapshot[2], "fourth distinct part is outside this cast");
            pawn.health.hediffSet.hediffs.Remove(first);
            pawn.health.hediffSet.hediffs.Insert(0, new Hediff_Injury { pawn = pawn, def = first.def, Part = Part("Later") });
            Check.Equal(3, snapshot.Length, "snapshot does not enumerate the changing live list");
            Check.Same(first, snapshot[0], "materialized selection survives removal from the live list");
            Check.Equal(2f, first.Severity, "selection alone does not heal");
        }));
        yield return new("SpellItemEffects.NonMageSelfHealUsesFixedEightWithoutSpill", f => World(f, w =>
        {
            using var patches = w.HealingBoundaries();
            var pawn = w.Caster;
            var part = Part("First");
            var small = Injury(pawn, part, 2);
            var samePart = Injury(pawn, part, 20);
            var second = Injury(pawn, Part("Second"), 12);
            var third = Injury(pawn, Part("Third"), 8);
            var fourth = Injury(pawn, Part("Fourth"), 30);
            var scar = Injury(pawn, Part("Scar"), 10, permanent: true);
            var disease = new Hediff { pawn = pawn, def = new HediffDef { defName = "Disease", maxSeverity = 100 } };
            Set(disease, "severityInt", 17f); pawn.health.hediffSet.hediffs.Add(disease);

            Check.Equal(0, pawn.AllComps.Count, "ordinary self-caster has no ability-user or magic component");
            Check.Equal(true, SpellItemEffects.TryApply(pawn, pawn, Props(SpellItemKind.Heal)), "real self-heal succeeds without a mage");
            Check.Equal(0f, small.Severity, "a two-point wound receives eight, clamped by the real injury");
            Check.Equal(true, small.ShouldRemove, "fully healed wound is ready for native health cleanup");
            Check.Equal(20f, samePart.Severity, "unused healing never spills into a second wound on the same part");
            Check.Equal(4f, second.Severity, "second distinct part receives exactly eight");
            Check.Equal(0f, third.Severity, "third distinct part receives exactly eight");
            Check.Equal(30f, fourth.Severity, "fourth body part remains unchanged");
            Check.Equal(10f, scar.Severity, "permanent injury remains unchanged");
            Check.Equal(17f, disease.Severity, "non-injury condition remains unchanged");
            Check.Same(pawn, w.HealRecipients.Single(), "successful heal emits for its actual patient");
        }));
        yield return new("SpellItemEffects.HealRevalidatesWhenNoNaturalWoundsRemain", f => World(f, w =>
        {
            using var patches = w.HealingBoundaries();
            var injury = Injury(w.Patient, Part("Arm"), 5);
            var props = Props(SpellItemKind.Heal);
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "first completion really heals");
            Check.Equal(0f, injury.Severity, "real health state was changed");
            Check.Equal("TM_SpellItem_NoInjuries", SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "zero-severity wounds cannot qualify again");
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "repeat completion fails validation");
            Check.Equal(1, w.HealRecipients.Count, "rejected repeat emits nothing");
        }));
        yield return new("SpellItemEffects.RangeFogAndMapChangesRejectBeforeHealing", f => World(f, w =>
        {
            using var patches = w.HealingBoundaries();
            var injury = Injury(w.Patient, Part("Arm"), 20);
            var props = Props(SpellItemKind.Heal); props.range = 10;
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "range includes the exact ten-cell boundary");
            props.range = 9.99f;
            Reject("TM_SpellItem_OutOfRange");
            props.range = 10;
            w.Fog(w.Patient.Position, true); Reject("TM_SpellItem_InvalidTarget");
            w.Fog(w.Patient.Position, false);
            Set(w.Patient, "mapIndexOrState", (sbyte)1); Reject("TM_SpellItem_InvalidTarget");
            Set(w.Patient, "mapIndexOrState", (sbyte)-1); Reject("TM_SpellItem_InvalidTarget");
            Set(w.Patient, "mapIndexOrState", (sbyte)0);
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "restoring the actual map and fog state restores eligibility");
            Check.Equal("TM_SpellItem_InvalidTarget", SpellItemEffects.TargetRejection(w.Caster, new IntVec3(20, 0, 4), Props(SpellItemKind.Fireball)), "out-of-map cells are invalid");
            Check.Equal(20f, injury.Severity, "every rejected completion leaves the real injury untouched");
            Check.Equal(0, w.HealRecipients.Count, "rejected geometry never emits healing");

            void Reject(string reason)
            {
                Check.Equal(reason, SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "specific target validation reason");
                Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "completion repeats target validation before effects");
            }
        }));
        yield return new("SpellItemEffects.HealRejectsHostileUndeadAndMechPatients", f => World(f, w =>
        {
            using var patches = w.HealingBoundaries();
            var injury = Injury(w.Patient, Part("Arm"), 20);
            var props = Props(SpellItemKind.Heal);
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "wounded friendly flesh pawn is valid");
            w.Patient.kindDef.hostileToAll = true;
            Check.Equal(true, w.Patient.HostileTo(w.Caster), "hostility is decided by the real engine");
            Reject("TM_SpellItem_HostileTarget");
            w.Patient.kindDef.hostileToAll = false;
            var undead = new Hediff { pawn = w.Patient, def = new HediffDef { defName = "ROM_VampSpellItemFixture" } };
            w.Patient.health.hediffSet.hediffs.Add(undead);
            Check.Equal(true, TM_Calc.IsUndead(w.Patient), "real undead recognition sees the vampire marker");
            Reject("TM_SpellItem_InvalidTarget");
            Check.Equal(0, SpellItemEffects.HealingTargets(w.Patient).Length, "undead do not expose healing candidates");
            w.Patient.health.hediffSet.hediffs.Remove(undead);
            HeadlessDefOf(typeof(FleshTypeDefOf));
            var oldMech = FleshTypeDefOf.Mechanoid;
            try
            {
                FleshTypeDefOf.Mechanoid = new FleshTypeDef { defName = "Mechanoid", isOrganic = false };
                Set(w.Patient.RaceProps, "fleshType", FleshTypeDefOf.Mechanoid);
                Check.Equal(true, w.Patient.RaceProps.IsMechanoid, "fixture is a real mechanoid race");
                Reject("TM_SpellItem_InvalidTarget");
                Check.Equal(0, SpellItemEffects.HealingTargets(w.Patient).Length, "mechanoids expose no natural healing candidates");
            }
            finally { FleshTypeDefOf.Mechanoid = oldMech; }
            Check.Equal(20f, injury.Severity, "ineligible patients retain their injuries");
            Check.Equal(0, w.HealRecipients.Count, "ineligible patients cause no effect emission");

            void Reject(string reason)
            {
                Check.Equal(reason, SpellItemEffects.TargetRejection(w.Caster, w.Patient, props), "patient policy rejects the actual pawn");
                Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "patient rejection also guards completion");
            }
        }));
        yield return new("SpellItemEffects.BlinkSkipsRealLineOfSightButOtherSpellsDoNot", f => World(f, w =>
        {
            using var patches = w.HealingBoundaries();
            var injury = Injury(w.Patient, Part("Arm"), 20);
            w.Wall();
            var destination = new IntVec3(14, 0, 6);
            Check.Equal(false, GenSight.LineOfSight(w.Caster.Position, destination, w.Map, skipFirstCell: true), "real edifice wall blocks the fixture");
            Check.Equal("TM_SpellItem_NoLineOfSight", SpellItemEffects.TargetRejection(w.Caster, destination, Props(SpellItemKind.Fireball)), "fireball cannot cross the wall");
            Check.Equal("TM_SpellItem_NoLineOfSight", SpellItemEffects.TargetRejection(w.Caster, w.Patient, Props(SpellItemKind.Heal)), "healing cannot cross the wall");
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, Props(SpellItemKind.Heal)), "wall gained during warmup prevents healing");
            Check.Equal(20f, injury.Severity, "blocked heal never changes severity");
            Check.Equal<string?>(null, SpellItemEffects.TargetRejection(w.Caster, destination, Props(SpellItemKind.Blink)), "revealed standable Blink destination ignores line of sight");
        }));
        yield return new("SpellItemEffects.BlinkRejectsSameOccupiedAndImpassableCells", f => World(f, w =>
        {
            var props = Props(SpellItemKind.Blink);
            var origin = w.Caster.Position;
            Reject(origin, "TM_SpellItem_SameCell");
            Reject(w.Patient.Position, "TM_SpellItem_Occupied");
            var blocked = new IntVec3(8, 0, 8); w.Building(blocked);
            Check.Equal(false, blocked.Standable(w.Map), "real thing grid marks the destination impassable");
            Reject(blocked, "TM_SpellItem_InvalidTarget");
            Check.Equal(origin, w.Caster.Position, "invalid Blink completions never assign position");

            void Reject(IntVec3 cell, string reason)
            {
                Check.Equal(reason, SpellItemEffects.TargetRejection(w.Caster, cell, props), "Blink destination rejection");
                Check.Equal(false, SpellItemEffects.TryApply(w.Caster, cell, props), "invalid Blink cannot reach engine relocation");
            }
        }));
        yield return new("SpellItemEffects.BlinkCompletesWithDestinationNotificationAndNoCameraCalls", f => World(f, w =>
        {
            using var patches = new Patches();
            // Limit: capture engine relocation/notification boundaries, not their heavy
            // grid, path-follower or renderer internals. The effect dispatcher stays real.
            patches.Prefix(typeof(Thing).GetProperty(nameof(Thing.Position))!.SetMethod!, nameof(AssignPosition));
            patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.Notify_Teleported))!, nameof(NotifyTeleport));
            patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.EmitBlink))!, nameof(EmitBlink));
            // Component cases may already have JIT-compiled a Harmony copy of TryApply
            // before Notify_Teleported was patched. Recompile its real original after
            // installing engine seams so an old inlined notification cannot bypass them.
            patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.TryApply))!, nameof(RunActualEffect));
            w.Wall(); var origin = w.Caster.Position; var destination = new IntVec3(14, 0, 6);
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, destination, Props(SpellItemKind.Blink)), "real Blink dispatcher completes across a wall for a nonmage");
            Check.Equal(destination, w.AssignedDestination, "actual Position setter call receives the requested destination");
            Check.Equal(destination, w.Caster.Position, "headless relocation boundary stores that destination");
            Check.Equal(false, w.TeleportEndsJob, "completion notification must not cancel its own cast job");
            Check.Equal(origin, w.BlinkOrigin, "visual boundary receives the pre-teleport origin");
            Check.Equal("Position,Notify,Blink", string.Join(",", w.Events), "move, notification and rendering occur once in order");
            var calls = SpellItemCalls(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.TryApply))!).ToArray();
            Check.Equal(false, calls.Any(m => m.DeclaringType?.Name.Contains("Selector", StringComparison.Ordinal) == true
                || m.DeclaringType?.Name.Contains("Camera", StringComparison.Ordinal) == true
                || m.DeclaringType == typeof(Find) && (m.Name.Contains("Selector", StringComparison.Ordinal) || m.Name.Contains("Camera", StringComparison.Ordinal))),
                "compiled item-effect path contains no selector or camera calls (not a GUI simulation)");
        }));
        yield return new("SpellItemEffects.VisualFailureAfterHealAndBlinkStillReportsApplied", f => World(f, w =>
        {
            using var patches = w.HealingBoundaries();
            patches.Prefix(typeof(Thing).GetProperty(nameof(Thing.Position))!.SetMethod!, nameof(AssignPosition));
            patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.Notify_Teleported))!, nameof(NotifyTeleport));
            patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.EmitBlink))!, nameof(EmitBlink));
            patches.Prefix(typeof(Log).GetMethod(nameof(Log.Warning), [typeof(string)])!, nameof(ExpectedWarning));
            patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.TryApply))!, nameof(RunActualEffect));
            var injury = Injury(w.Caster, Part("Arm"), 20);
            w.VisualFailure = "injected heal renderer failure";
            w.WarningToCapture = "[RWoM.SpellItems] Spell item visual failed: injected heal renderer failure";
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Caster, Props(SpellItemKind.Heal)), "renderer failure cannot make an already-applied heal free");
            Check.Equal(12f, injury.Severity, "real injury was healed exactly eight before the renderer threw");
            Check.Same(w.Caster, w.HealRecipients.Single(), "fault was injected only after reaching the actual heal renderer boundary");
            Check.Equal(w.WarningToCapture, w.CapturedWarnings.Single(), "heal rendering failure emits the exact expected diagnostic");

            var origin = w.Caster.Position; var destination = new IntVec3(8, 0, 6);
            w.VisualFailure = "injected blink renderer failure";
            w.WarningToCapture = "[RWoM.SpellItems] Spell item visual failed: injected blink renderer failure";
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, destination, Props(SpellItemKind.Blink)), "renderer failure cannot make an already-applied Blink free");
            Check.Equal(destination, w.Caster.Position, "position remains at the completed Blink destination");
            Check.Equal(origin, w.BlinkOrigin, "Blink visual fault happens after the captured origin is forwarded");
            Check.Equal(false, w.TeleportEndsJob, "native completion notification precedes the visual fault");
            Check.Equal("Position,Notify,Blink", string.Join(",", w.Events), "Blink position and notification both happen before rendering throws");
            Check.Equal(2, w.CapturedWarnings.Count, "one precise diagnostic per cosmetic failure");
            Check.Equal(w.WarningToCapture, w.CapturedWarnings[1], "Blink rendering failure emits the exact expected diagnostic");
            // Only exact injected-failure diagnostics are intercepted. Any other
            // warning reaches RuntimeLog and fails the normal harness clean-log gate.
        }));
        yield return new("SpellItemEffects.NonMageFireballReallyLaunchesAndRevalidates", f => World(f, w =>
        {
            var definition = ProjectileDef(); using var defs = new DefinitionScope<ThingDef>(definition);
            using var patches = w.LaunchBoundaries();
            var props = Props(SpellItemKind.Fireball);
            Check.Equal(0, w.Caster.AllComps.Count, "launcher has no magic component");
            Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "actual effect creates and launches the item projectile");
            var shot = w.Projectiles.Single();
            Check.Equal(typeof(Projectile_SpellItemFireball), shot.GetType(), "spawn uses the item-only projectile class");
            Check.Same(definition, shot.def, "dedicated projectile definition is forwarded");
            Check.Same(w.Caster, shot.Launcher, "real Projectile.Launch initializes the launcher");
            Check.Same(w.Patient, shot.usedTarget.Thing, "real launch retains the selected target");
            Check.Same(w.Patient, shot.intendedTarget.Thing, "intended target is not redirected to the caster");
            Check.Equal(ProjectileHitFlags.All, shot.HitFlags, "ordinary native interception policy is retained");
            Check.Equal(w.Caster.Position, shot.Position, "spawned projectile starts at the caster's actual cell");
            w.Fog(w.Patient.Position, true);
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "fog gained before completion prevents a second launch");
            Check.Equal(1, w.Projectiles.Count, "failed validation does not spawn another projectile");
        }));
        yield return new("SpellItemEffects.FireballCannotSucceedWithoutAProjectileDefinition", f => World(f, w =>
        {
            using var patches = w.LaunchBoundaries();
            var definition = ProjectileDef(); definition.projectile = null;
            using var defs = new DefinitionScope<ThingDef>(definition);
            var props = Props(SpellItemKind.Fireball);
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "missing projectile properties cannot report successful application");
            definition.projectile = new ProjectileProperties(); definition.thingClass = typeof(Thing);
            Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "non-projectile class cannot report a launch");
            Check.Equal(0, w.Projectiles.Count, "bad definitions are rejected before spawning");
        }));
        yield return new("SpellItemEffects.ItemFireballEmitsFixedExplosionWithoutMageScaling", f => World(f, w =>
        {
            HeadlessDefOf(typeof(DamageDefOf));
            var oldFlame = DamageDefOf.Flame; var oldSound = TorannMagicDefOf.TM_SoftExplosion;
            try
            {
                DamageDefOf.Flame = new DamageDef { defName = "Flame" };
                TorannMagicDefOf.TM_SoftExplosion = new SoundDef { defName = "TM_SoftExplosion" };
                using var patches = new Patches();
                patches.Prefix(typeof(GenExplosion).GetMethods().Single(m => m.Name == nameof(GenExplosion.DoExplosion)), nameof(Explosion));
                var center = new IntVec3(14, 0, 6); var projectile = ProjectileDef();
                Projectile_SpellItemFireball.EmitFireballImpact(center, w.Map, w.Caster, projectile);
                var explosion = w.Explosions.Single();
                Check.Equal(center, explosion.Center, "explosion is centered on captured impact location");
                Check.Same(w.Map, explosion.Map, "impact map is forwarded");
                Check.Same(w.Caster, explosion.Instigator, "ordinary pawn remains the instigator");
                Check.Same(projectile, explosion.Projectile, "item projectile provenance is preserved");
                Check.Same(DamageDefOf.Flame, explosion.Damage, "fixed flame damage type");
                Check.Same(TorannMagicDefOf.TM_SoftExplosion, explosion.Sound, "item explosion sound");
                Check.Equal(18, explosion.Amount, "fixed eighteen damage, no mage skill scaling");
                Check.Equal(1.9f, explosion.Radius, "fixed 1.9-cell explosion radius");
                Check.Equal(0f, explosion.ArmorPenetration, "no hidden armor penetration scaling");
                Check.Equal(.1f, explosion.FireChance, "bounded native fire-start chance");
                Check.Equal(false, explosion.Falloff, "fixed damage does not fall off");
                Projectile_SpellItemFireball.EmitFireballImpact(center, null!, w.Caster, projectile);
                Projectile_SpellItemFireball.EmitFireballImpact(new IntVec3(20, 0, 6), w.Map, w.Caster, projectile);
                Check.Equal(1, w.Explosions.Count, "missing map and out-of-bounds impacts emit no explosion");
            }
            finally { DamageDefOf.Flame = oldFlame; TorannMagicDefOf.TM_SoftExplosion = oldSound; }
        }));
        yield return new("SpellItemEffects.GizmosAppendInventoryAndEquippedCommandsWithDisabledReasons", f => World(f, w =>
        {
            using var language = new LanguageScope(f.Repository);
            // Exercise the earlier UI/stat cases' headless-root state even in this
            // focused test; native labels must use the local game's scenario instead
            // of trying to find a scenario editor in an uninitialized window stack.
            using var ui = new HeadlessUiScope();
            HeadlessDefOf(typeof(PawnCapacityDefOf));
            var old = PawnCapacityDefOf.Manipulation;
            var oldHitPoints = StatDefOf.MaxHitPoints;
            using var definition = old == null ? new DefinitionScope<PawnCapacityDef>(new PawnCapacityDef { defName = "Manipulation", minForCapable = .1f }) : null;
            try
            {
                // Native item labels unconditionally query maximum HP even when no HP
                // suffix is displayed. Keep the real label path with a fixed-base stat.
                StatDefOf.MaxHitPoints = FleshStatTests.Stat("SpellItemLabelHitPoints", 100);
                PawnCapacityDefOf.Manipulation = old ?? definition!.Definition;
                Capacity(w.Caster, PawnCapacityDefOf.Manipulation);
                w.Caster.inventory = new Pawn_InventoryTracker(w.Caster);
                w.Caster.equipment = new Pawn_EquipmentTracker(w.Caster);
                var scroll = Item("inventory heal scroll", SpellItemKind.Heal, equipped: false, stack: 2);
                var staff = Item("equipped blink staff", SpellItemKind.Blink, equipped: true);
                var carriedStaff = Item("unequipped staff", SpellItemKind.Fireball, equipped: true);
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(scroll.parent, false), "real inventory holds scroll");
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(carriedStaff.parent, false), "real inventory also holds an unequipped staff");
                Check.Equal(true, w.Caster.equipment.GetDirectlyHeldThings().TryAdd(staff.parent, false), "real equipment owner holds staff");
                Check.Same(staff.parent, w.Caster.equipment.Primary, "staff is actually primary equipment");
                var original = new Command_Action { defaultLabel = "existing command" };
                var gizmos = SpellItemGizmos.Append(w.Caster, [original]).ToArray();
                Check.Equal(3, gizmos.Length, "only inventory scroll and equipped staff append commands");
                Check.Same(original, gizmos[0], "existing gizmo is preserved first");
                var commands = gizmos.Skip(1).Cast<Command_Action>().ToArray();
                Check.Equal("inventory heal scroll ×2", commands[0].defaultLabel, "scroll label carries the real stack count");
                Check.Equal("equipped blink staff 10/10", commands[1].defaultLabel, "staff label carries remaining and maximum uses");
                Check.Equal(false, commands[0].Disabled || commands[1].Disabled, "available nonmage sources are enabled");
                Check.Same(scroll.parent.def.uiIcon, commands[0].icon, "def icon is forwarded without GUI rendering");
                Check.NotNull(commands[0].action, "inventory command opens a targeting action");
                Check.NotNull(commands[1].action, "equipment command opens a targeting action");
                Set(scroll, "readyTick", 101); Set(staff, "remainingUses", 0);
                commands = SpellItemGizmos.Append(w.Caster, []).Cast<Command_Action>().ToArray();
                Check.Equal(true, commands.All(c => c.Disabled), "cooldown and empty sources remain visible but unavailable");
                Check.Equal("The item is cooling down.", commands[0].disabledReason, "cooldown explanation comes from real component availability");
                Check.Equal("No uses remaining.", commands[1].disabledReason, "empty staff explanation comes from real component availability");
                Check.Equal(2, scroll.parent.stackCount, "enumerating gizmos never consumes inventory");
            }
            finally { PawnCapacityDefOf.Manipulation = old; StatDefOf.MaxHitPoints = oldHitPoints; }
        }));
    }

    static WorldState? active;
    static void World(RuntimeFixture fixture, Action<WorldState> body)
    {
        var oldGame = Current.Game; var oldActive = active;
        Rand.PushState(1943);
        try
        {
            Current.Game = Blank<Game>(); Set(Current.Game, "tickManager", new TickManager());
            Current.Game.Scenario = new Scenario();
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager()); Set(Find.TickManager, "ticksGameInt", 100);
            using var designation = DefDatabase<DesignationDef>.DefCount == 0
                ? new DefinitionScope<DesignationDef>(new DesignationDef { defName = "SpellItemFixtureDesignation" }) : null;
            using var world = new WorldState(Current.Game); active = world;
            using var environment = new TravelEnvironmentFixture(world.Caster, world.Map) { Space = false };
            body(world);
        }
        finally { active = oldActive; Current.Game = oldGame; Rand.PopState(); }
    }
    sealed class WorldState : IDisposable
    {
        public readonly Map Map;
        public readonly Pawn Caster, Patient;
        readonly PathGrid pathGrid;
        public readonly List<Pawn> Pawns = [];
        public readonly List<Pawn> HealRecipients = [];
        public readonly List<Projectile> Projectiles = [];
        public readonly List<ExplosionCall> Explosions = [];
        public readonly List<string> Events = [];
        public readonly List<string> CapturedWarnings = [];
        public string? VisualFailure, WarningToCapture;
        public IntVec3 AssignedDestination, BlinkOrigin;
        public bool? TeleportEndsJob;
        readonly Faction faction = new() { def = new FactionDef { isPlayer = true } };
        public WorldState(Game game)
        {
            Map = Blank<Map>(); Map.info = new MapInfo { Size = new IntVec3(20, 1, 20) };
            var otherMap = Blank<Map>(); otherMap.info = new MapInfo { Size = new IntVec3(20, 1, 20) };
            otherMap.cellIndices = new CellIndices(otherMap); otherMap.designationManager = new DesignationManager(otherMap);
            Set(game, "maps", new List<Map> { Map, otherMap });
            Map.cellIndices = new CellIndices(Map); Map.thingGrid = new ThingGrid(Map); Map.edificeGrid = new EdificeGrid(Map);
            Map.designationManager = new DesignationManager(Map); Map.fogGrid = new FogGrid(Map);
            // Real native path storage, initially zero-cost; no terrain recalculation or
            // engine-wide path-definition registration is needed to test Standable.
            pathGrid = new PathGrid(Map, new PathGridDef { defName = "SpellItemFixturePath" });
            Map.pathing = Blank<Pathing>(); var context = new PathingContext(Map, pathGrid);
            Set(Map.pathing, "normal", context); Set(Map.pathing, "fenceBlocked", context);
            Caster = Pawn("SpellItemEffectCaster", new IntVec3(4, 0, 4));
            Patient = Pawn("SpellItemEffectPatient", new IntVec3(14, 0, 4));
        }
        Pawn Pawn(string name, IntVec3 position)
        {
            var race = Blank<ThingDef>(); race.defName = name; race.category = ThingCategory.Pawn;
            race.size = new IntVec2(1, 1); race.passability = Traversability.Standable;
            race.race = new RaceProperties { intelligence = Intelligence.Humanlike };
            Set(race.race, "fleshType", new FleshTypeDef { defName = "SpellItemOrganic", isOrganic = true });
            var pawn = new Pawn { def = race, kindDef = new PawnKindDef { defName = name, race = race }, thingIDNumber = Pawns.Count + 1 };
            pawn.health = new Pawn_HealthTracker(pawn); pawn.guest = new Pawn_GuestTracker(pawn);
            pawn.mindState = Blank<Pawn_MindState>(); pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
            Set(pawn, "comps", new List<ThingComp>()); Set(pawn, "factionInt", faction);
            Set(pawn, "positionInt", position); Set(pawn, "mapIndexOrState", (sbyte)0);
            Map.thingGrid.Register(pawn); Pawns.Add(pawn); return pawn;
        }
        public void Fog(IntVec3 cell, bool value)
        {
            // Set the real fog bit without Refog's unrelated renderer/event notifications.
            object bits = Get(Map.fogGrid, "fogGrid")!;
            bits.GetType().GetMethod("Set")!.Invoke(bits, [Map.cellIndices.CellToIndex(cell), value]);
            Set(Map.fogGrid, "fogGrid", bits);
            Check.Equal(value, cell.Fogged(Map), "real fog grid reflects fixture state");
        }
        public void Wall() { for (int z = 0; z < 20; z++) Building(new IntVec3(10, 0, z)); }
        public void Building(IntVec3 cell)
        {
            var def = Blank<ThingDef>(); def.defName = "SpellItemWall"; def.category = ThingCategory.Building;
            def.size = new IntVec2(1, 1); def.fillPercent = 1; def.blockLight = true; def.passability = Traversability.Impassable;
            var building = new Building { def = def }; Set(building, "positionInt", cell); Set(building, "mapIndexOrState", (sbyte)0);
            Map.edificeGrid.Register(building); Map.thingGrid.Register(building);
        }
        public Patches HealingBoundaries()
        {
            var patches = new Patches();
            try
            {
                patches.Prefix(typeof(HediffSet).GetMethod(nameof(HediffSet.DirtyCache))!, nameof(HealthSideEffects));
                patches.Prefix(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.CheckForStateChange))!, nameof(HealthSideEffects));
                patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.EmitHeal))!, nameof(EmitHeal));
                return patches;
            }
            catch { patches.Dispose(); throw; }
        }
        public Patches LaunchBoundaries()
        {
            var patches = new Patches();
            try
            {
                patches.Prefix(typeof(GenSpawn).GetMethod(nameof(GenSpawn.Spawn), [typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode)])!, nameof(SpawnProjectile));
                patches.Prefix(typeof(Pawn).GetProperty(nameof(Verse.Pawn.DrawPos))!.GetMethod!, nameof(DrawPosition));
                return patches;
            }
            catch { patches.Dispose(); throw; }
        }
        public void Dispose() { pathGrid.Dispose(); Map.fogGrid.Dispose(); }
    }

    static CompProperties_SpellItem Props(SpellItemKind kind) => new() { spell = kind, range = 20 };
    static BodyPartRecord Part(string name) => new() { def = new BodyPartDef { defName = name } };
    static Hediff_Injury Injury(Pawn pawn, BodyPartRecord? part, float severity, bool permanent = false)
    {
        var injury = new Hediff_Injury { pawn = pawn, Part = part, def = new HediffDef { defName = "SpellItemWound", minSeverity = 0, maxSeverity = 100, injuryProps = new InjuryProps() } };
        Set(injury, "severityInt", severity);
        if (permanent)
        {
            var comp = new HediffComp_GetsPermanent { parent = injury, props = new HediffCompProperties_GetsPermanent() };
            // Set saved state directly: the setter randomly chooses scar pain category.
            Set(comp, "isPermanentInt", true); injury.comps = [comp];
        }
        pawn.health.hediffSet.hediffs.Add(injury); return injury;
    }
    static ThingDef ProjectileDef()
    {
        var def = Blank<ThingDef>(); def.defName = "TM_ItemFireballProjectile";
        def.thingClass = typeof(Projectile_SpellItemFireball); def.category = ThingCategory.Projectile;
        def.projectile = new ProjectileProperties { speed = 30, stoppingPower = 1 }; return def;
    }
    static CompSpellItem Item(string label, SpellItemKind spell, bool equipped, int stack = 1)
    {
        var props = new CompProperties_SpellItem { spell = spell, requiresEquipped = equipped, maxUses = equipped ? 10 : 1 };
        var def = Blank<ThingDef>(); def.defName = "EffectFixture_" + spell + equipped; def.label = label; def.description = "Item spell";
        def.category = ThingCategory.Item; def.thingClass = typeof(ThingWithComps); def.stackLimit = equipped ? 1 : 25;
        def.equipmentType = equipped ? EquipmentType.Primary : EquipmentType.None; def.uiIcon = Blank<Texture2D>(); def.comps = [props];
        var item = new ThingWithComps { def = def, stackCount = stack };
        var comp = new CompSpellItem { parent = item }; comp.Initialize(props);
        var comps = new List<ThingComp> { comp };
        if (equipped)
        {
            var equippable = new CompEquippable { parent = item };
            equippable.Initialize(new CompProperties { compClass = typeof(CompEquippable) });
            Set(equippable.verbTracker, "verbs", new List<Verb>()); comps.Add(equippable);
        }
        Set(item, "comps", comps); comp.PostPostMake(); return comp;
    }
    static void Capacity(Pawn pawn, PawnCapacityDef capacity)
    {
        // Preserve actual CapableOf/GetLevel; seed only the expensive body's cached level.
        var field = typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cache = Activator.CreateInstance(field.FieldType)!;
        var entry = field.FieldType.GetProperty("Item", [typeof(PawnCapacityDef)])!.GetValue(cache, [capacity])!;
        Set(entry, "status", Enum.Parse(entry.GetType().GetField("status")!.FieldType, "Cached")); Set(entry, "value", 1f);
        field.SetValue(pawn.health.capacities, cache);
    }
    static bool RunActualEffect() => true;
    static bool HealthSideEffects(Pawn ___pawn) => active?.Pawns.Contains(___pawn) != true;
    static bool EmitHeal(Pawn pawn)
    {
        active!.HealRecipients.Add(pawn);
        if (active.VisualFailure != null) throw new InvalidOperationException(active.VisualFailure);
        return false;
    }
    static bool ExpectedWarning(string text)
    {
        if (active?.WarningToCapture == null || text != active.WarningToCapture) return true;
        active.CapturedWarnings.Add(text); return false;
    }
    static bool AssignPosition(Thing __instance, IntVec3 value)
    {
        if (__instance != active?.Caster) return true;
        active.AssignedDestination = value; active.Events.Add("Position"); Set(__instance, "positionInt", value); return false;
    }
    static bool NotifyTeleport(Pawn __instance, bool endCurrentJob)
    {
        if (__instance != active?.Caster) return true;
        active.TeleportEndsJob = endCurrentJob; active.Events.Add("Notify"); return false;
    }
    static bool EmitBlink(Pawn pawn, IntVec3 origin)
    {
        Check.Same(active!.Caster, pawn, "Blink renderer receives the caster"); active.BlinkOrigin = origin; active.Events.Add("Blink");
        if (active.VisualFailure != null) throw new InvalidOperationException(active.VisualFailure);
        return false;
    }
    static bool DrawPosition(Pawn __instance, ref Vector3 __result)
    {
        if (__instance != active?.Caster) return true;
        __result = __instance.Position.ToVector3Shifted(); return false;
    }
    static bool SpawnProjectile(ThingDef def, IntVec3 loc, Map map, ref Thing __result)
    {
        if (map != active?.Map || (def.defName != "TM_ItemFireballProjectile" && def.defName != "TM_ItemLightningProjectile")) return true;
        Check.Equal(active.Caster.Position, loc, "spawn starts at caster's actual cell");
        var projectile = (Projectile)Activator.CreateInstance(def.thingClass)!; projectile.def = def;
        Set(projectile, "comps", new List<ThingComp>()); Set(projectile, "positionInt", loc); Set(projectile, "mapIndexOrState", (sbyte)0);
        active.Projectiles.Add(projectile); __result = projectile; return false;
    }
    sealed record ExplosionCall(IntVec3 Center, Map Map, float Radius, DamageDef Damage, Thing Instigator,
        int Amount, float ArmorPenetration, SoundDef Sound, ThingDef Projectile, float FireChance, bool Falloff);
    static bool Explosion(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator,
        int damAmount, float armorPenetration, SoundDef explosionSound, ThingDef projectile, float chanceToStartFire, bool damageFalloff)
    {
        active!.Explosions.Add(new(center, map, radius, damType, instigator, damAmount, armorPenetration, explosionSound, projectile, chanceToStartFire, damageFalloff)); return false;
    }
    static IEnumerable<MethodBase> SpellItemCalls(MethodBase root)
    {
        var processor = Assembly.Load("0Harmony").GetType("HarmonyLib.PatchProcessor")!;
        var read = processor.GetMethods().Single(m => m.Name == "GetOriginalInstructions" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        var seen = new HashSet<MethodBase>(); var pending = new Stack<MethodBase>(); pending.Push(root);
        while (pending.Count != 0)
        {
            var caller = pending.Pop(); if (!seen.Add(caller)) continue;
            foreach (var instruction in (IEnumerable)read.Invoke(null, [caller, null])!)
            {
                if (instruction.GetType().GetField("operand")!.GetValue(instruction) is not MethodBase called) continue;
                yield return called;
                if (called.DeclaringType?.Namespace == "TorannMagic.SpellItems" && called.GetMethodBody() != null) pending.Push(called);
            }
        }
    }
    sealed class DefinitionScope<T> : IDisposable where T : Def
    {
        public T Definition { get; }
        readonly List<T> list = DefDatabase<T>.AllDefsListForReading;
        readonly Dictionary<string, T> names = (Dictionary<string, T>)typeof(DefDatabase<T>).GetField("defsByName", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        readonly T[] oldList;
        readonly KeyValuePair<string, T>[] oldNames;
        public DefinitionScope(T def)
        {
            Definition = def; oldList = list.ToArray(); oldNames = names.ToArray();
            if (names.Remove(def.defName, out var old)) list.Remove(old);
            DefDatabase<T>.Add(def);
        }
        public void Dispose()
        {
            list.Clear(); list.AddRange(oldList); names.Clear(); foreach (var pair in oldNames) names.Add(pair.Key, pair.Value);
        }
    }
    sealed class HeadlessUiScope : IDisposable
    {
        readonly FieldInfo field = typeof(Current).GetField("rootInt", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? old;
        public HeadlessUiScope()
        {
            old = field.GetValue(null);
            var root = Blank<Root_Play>(); root.uiRoot = Blank<UIRoot_Play>();
            field.SetValue(null, root);
        }
        public void Dispose() => field.SetValue(null, old);
    }
    internal sealed class LanguageScope : IDisposable
    {
        readonly LoadedLanguage old = LanguageDatabase.activeLanguage, oldDefault = LanguageDatabase.defaultLanguage;
        public LanguageScope(string repository)
        {
            var language = Blank<LoadedLanguage>(); language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = XDocument.Load(Path.Combine(repository, "Languages/English/Keyed/TM_SpellItems.xml"))
                .Root!.Elements().ToDictionary(e => e.Name.LocalName, e => new LoadedLanguage.KeyedReplacement { key = e.Name.LocalName, value = e.Value });
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        }
        public void Dispose() { LanguageDatabase.activeLanguage = old; LanguageDatabase.defaultLanguage = oldDefault; }
    }
    static readonly List<(IntVec3 center, float radius, Func<IntVec3, bool> predicate)> previewRings = [];
    static readonly List<IntVec3> seerCells = [];
    internal static void SetProjectileField(Projectile projectile, string name, object value)
    {
        // Reflection SetValue triggers Projectile's renderer-only static material
        // initializer. Set these instance fields exactly as Launch does instead.
        var field = typeof(Projectile).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var setter = new System.Reflection.Emit.DynamicMethod("InitializeProjectile_" + name, null,
            [typeof(Projectile), typeof(object)], typeof(Projectile), true);
        var il = setter.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
        il.Emit(field.FieldType.IsValueType ? System.Reflection.Emit.OpCodes.Unbox_Any : System.Reflection.Emit.OpCodes.Castclass, field.FieldType);
        il.Emit(System.Reflection.Emit.OpCodes.Stfld, field); il.Emit(System.Reflection.Emit.OpCodes.Ret);
        ((Action<Projectile, object>)setter.CreateDelegate(typeof(Action<Projectile, object>)))(projectile, value);
    }
    static IntVec3 seerVisualCell;
    static bool SkipSeerVisual() => false;
    static int seerDamage;
    static bool CaptureSeerVisuals(IntVec3 cell) { seerVisualCell = cell; return false; }
    static bool CaptureSeerScatter(IntVec3 pos, Map map, float radius)
    { Check.Equal(.4f, radius, "scatter radius preserved"); seerCells.Add(pos); return false; }
    static bool CaptureSeerDamage(Thing __instance, ref DamageWorker.DamageResult __result)
    {
        seerDamage++; Set(__instance, "mapIndexOrState", (sbyte)-2); Set(__instance, "positionInt", IntVec3.Invalid);
        __result = new DamageWorker.DamageResult(); return false;
    }
    static readonly List<SimpleColor> previewLines = [];
    static Patches PreviewBoundaries()
    {
        var patches = new Patches();
        patches.Prefix(typeof(SpellItemGizmos).GetMethod(nameof(SpellItemGizmos.DrawRing))!, nameof(CapturePreviewRing));
        patches.Prefix(typeof(SpellItemGizmos).GetMethod(nameof(SpellItemGizmos.DrawLine))!, nameof(CapturePreviewLine));
        patches.Prefix(typeof(TorannMagic.Odyssey.BreachSeal).GetMethod("EmitPreviewCells")!, nameof(CaptureSealPreviewCells));
        patches.Prefix(typeof(TorannMagic.Odyssey.BreachSeal).GetMethod("EmitPreviewReason")!, nameof(CaptureSealPreviewReason));
        return patches;
    }
    static bool CapturePreviewRing(IntVec3 center, float radius, Func<IntVec3, bool> predicate)
    { previewRings.Add((center, radius, predicate)); return false; }
    static bool CapturePreviewLine(SimpleColor color) { previewLines.Add(color); return false; }

    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly object harmony;
        readonly string id = "rwom.regression.spell-item-effects." + Guid.NewGuid();
        public Patches() => harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        public void Prefix(MethodBase original, string method)
        {
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpellItemEffectTests).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [original, prefix, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void HeadlessDefOf(Type type)
    {
        // Earlier harness initialization may already have touched these DefOfs. Accept
        // only a warning emitted by this constructor invocation, never clear test logs.
        var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        int before = problems.Count; RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        if (problems.Count != before) RuntimeLog.AcceptHeadlessDefOfInitialization(type.Name);
    }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static FieldInfo Field(object target, string name)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is { } field) return field;
        throw new InvalidOperationException("Missing effect fixture field: " + name);
    }
    static object? Get(object target, string name) => Field(target, name).GetValue(target);
    static void Set(object target, string name, object? value) => Field(target, name).SetValue(target, value);
}
