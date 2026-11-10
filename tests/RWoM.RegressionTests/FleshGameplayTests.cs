using System.Reflection;
using TorannMagic.FleshDevelopment;
using RimWorld;
using Verse;
using Verse.AI;
using System.Runtime.CompilerServices;

namespace RWoM.RegressionTests;

internal static class FleshGameplayTests
{
    private static object Call(string name, params object[] args)
    {
        var type = typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.FleshActions");
        Check.Equal(true, type != null, "Gameplay service exists");
        return type!.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, args)!;
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshGameplay.GodModeLevelUpPreservesProgress", _ => {
            var method = typeof(FleshActions).GetMethod("DebugLevelUp");
            Check.NotNull(method, "God-mode level command exists");
            var pawn = ActionPawn(); var data = FleshRepository.Ensure(pawn);
            data.Level = 5; data.Experience = 55; data.SkillPoints = 2; data.Thirst = 13;
            data.SetReadyTick(FleshSkill.Devour, 12345);
            bool oldGod = DebugSettings.godMode;
            try {
                DebugSettings.godMode = false; method!.Invoke(null, [pawn]);
                Check.Equal(5, data.Level, "outside God mode no level change");
                DebugSettings.godMode = true; method.Invoke(null, [pawn]);
                Check.Equal(6, data.Level, "one level added");
                Check.Equal(3, data.SkillPoints, "one skill point added");
                Check.Equal(55f, data.Experience, "existing experience retained");
                Check.Equal(13f, data.Thirst, "thirst retained");
                Check.Equal(12345, data.GetReadyTick(FleshSkill.Devour), "cooldown retained");
                Check.Equal(1, data.GetRank(FleshSkill.Devour), "learned ranks unchanged");
                pawn.health.hediffSet.hediffs.Clear(); method.Invoke(null, [pawn]);
                Check.Equal(6, data.Level, "ordinary pawn cannot receive undead level");
            } finally { DebugSettings.godMode = oldGod; }
        });
        yield return new("FleshGameplay.FreshnessContinuousAndNoDoubleMissingPenalty", _ =>
        {
            Check.Equal(1f, (float)Call("Freshness", 0f, 100f, 300f), "Fresh corpse");
            Check.Equal(.75f, (float)Call("Freshness", 50f, 100f, 300f), "Aging before rot");
            Check.Equal(.5f, (float)Call("Freshness", 100f, 100f, 300f), "Rot boundary");
            Check.Equal(.25f, (float)Call("Freshness", 200f, 100f, 300f), "Continuous rotting");
            Check.Equal(0f, (float)Call("Freshness", 400f, 100f, 300f), "Desiccated corpse");
            Check.Equal(35f, (float)Call("MeatReward", 70f, .5f), "MeatAmount is already reduced for missing parts");
        });
        yield return new("FleshGameplay.TimerCatchupAndExpireOnlyOnce", _ =>
        {
            var progress = new FleshProgress { LastTick = 100, Thirst = 0, EmpowerUntil = 200, HardenUntil = 200, FeastUntil = 160 };
            Check.Equal(3, (int)Call("Advance", progress, 300), "Both penalties expire");
            Check.Equal(0, progress.EmpowerUntil, "Empower deadline consumed");
            Check.Equal(0, progress.HardenUntil, "Harden deadline consumed");
            Check.Equal(0, progress.FeastUntil, "Feast timer expires");
            Check.Equal(true, Math.Abs(progress.Thirst - (1f + 200f / 6000f)) < .0001f, "Only active Feast interval increases hunger");
            Check.Equal(0, (int)Call("Advance", progress, 300), "Same tick cannot repeat damage");
        });
        yield return new("FleshGameplay.RankCooldownsAndDamage", _ =>
        {
            Check.Equal(60000, (int)Call("CooldownTicks", FleshSkill.Restore, 1), "First restore cooldown");
            Check.Equal(48000, (int)Call("CooldownTicks", FleshSkill.Restore, 3), "Third restore cooldown");
            Check.Equal(60000, (int)Call("CooldownTicks", FleshSkill.Feast, 1), "Feast day cooldown");
            Check.Equal(4f, (float)Call("DevourDamage", 1), "First devour damage");
            Check.Equal(8f, (float)Call("DevourDamage", 3), "Third devour damage");
        });
        yield return new("FleshGameplay.PassivesLinearNoDuplicateCraftBonus", _ =>
        {
            var p = new FleshProgress { SkillPoints = 100, Thirst = 0, EmpowerUntil = 100, HardenUntil = 100 };
            foreach (FleshSkill skill in new[] { FleshSkill.Mobility, FleshSkill.Strength, FleshSkill.Industry, FleshSkill.Empower, FleshSkill.Harden })
                while (p.TryUpgrade(skill)) { }
            Check.Equal(true, Math.Abs(FleshStatPatches.Adjust(p, "MoveSpeed", 1, 0) - 1.5f) < .0001f, "Mobility plus rank three empower");
            Check.Equal(true, Math.Abs(FleshStatPatches.Adjust(p, "WorkSpeedGlobal", 1, 0) - 1.5f) < .0001f, "Global work bonus");
            Check.Equal(1f, FleshStatPatches.Adjust(p, "GeneralLaborSpeed", 1, 0), "Crafting gets no second multiplier");
            Check.Equal(1.3f, FleshStatPatches.Adjust(p, "MeleeDamageFactor", 1, 0), "Ten strength ranks");
            Check.Equal(.5f, FleshStatPatches.Adjust(p, "ArmorRating_Sharp", 0, 0), "Harden rank three");
            p.Thirst = p.MaxThirst;
            Check.Equal(true, Math.Abs(FleshStatPatches.Adjust(p, "MoveSpeed", 1, 100) - .96f) < .0001f, "Expiration and starvation applied");
        });
        yield return new("FleshGameplay.MeleePacketsAggregateActualDamageAndFinalizeSafely", _ =>
        {
            var attacker = new Pawn(); var victim = new Pawn();
            var verb = new Verb_MeleeAttackDamage { caster = attacker };
            FleshCombatPatches.Prefix(verb, new LocalTargetInfo(victim), out var state);
            FleshCombatPatches.Record(victim, new DamageInfo(null, 10, instigator: attacker), new DamageWorker.DamageResult { totalDamageDealt = 0 });
            Check.Equal(0f, state.Damage, "Blocked packet gives no successful hit");
            FleshCombatPatches.Record(victim, new DamageInfo(null, 10, instigator: attacker), new DamageWorker.DamageResult { totalDamageDealt = 4 });
            FleshCombatPatches.Record(victim, new DamageInfo(null, 5, instigator: attacker), new DamageWorker.DamageResult { totalDamageDealt = 2 });
            FleshCombatPatches.Record(new Pawn(), new DamageInfo(null, 5, instigator: attacker), new DamageWorker.DamageResult { totalDamageDealt = 8 });
            Check.Equal(6f, state.Damage, "Only actual target packets counted");
            var failure = new InvalidOperationException("test");
            Check.Same(failure, FleshCombatPatches.Finalizer(failure, state), "Exception retained");
            FleshCombatPatches.Record(victim, new DamageInfo(null, 5, instigator: attacker), new DamageWorker.DamageResult { totalDamageDealt = 8 });
            Check.Equal(6f, state.Damage, "Exception clears combat scope");
        });
        yield return new("FleshGameplay.RageClearingAndReplacementReleaseOnlyOwnFlag", _ =>
        {
            var race = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            race.race = new RaceProperties();
            var pawn = new Pawn { def = race }; pawn.health = new Pawn_HealthTracker(pawn);
            pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = "TM_UndeadHD" } });
            var p = FleshRepository.Ensure(pawn); p.FeastRage = true; p.FeastUntil = 1000;
            FleshRageClearPatch.Prefix(pawn);
            Check.Equal(false, p.FeastRage, "Foreign recovery clears ownership");
            Check.Equal(1000, p.FeastUntil, "Recovery cannot erase timed ability");
            p.FeastRage = true;
            var handler = new MentalStateHandler(pawn);
            FleshRageReplacementPatch.Postfix(handler, pawn, new MentalState_Berserk());
            Check.Equal(false, p.FeastRage, "Replacement, including silent transition, releases ownership");
        });
        yield return new("FleshGameplay.BiteCanSelectVitalOrgansButNeverImplants", _ =>
        {
            var torso = new BodyPartRecord();
            var head = new BodyPartRecord { parent = torso };
            var brain = new BodyPartRecord { parent = head, depth = BodyPartDepth.Inside };
            var eye = new BodyPartRecord { parent = head };
            var race = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            race.race = new RaceProperties { body = new BodyDef { corePart = torso } };
            var pawn = new Pawn { def = race }; pawn.health = new Pawn_HealthTracker(pawn);
            Check.Equal(true, FleshCombatPatches.EdiblePart(pawn, head), "Natural head may be selected");
            Check.Equal(true, FleshCombatPatches.EdiblePart(pawn, brain), "Vital internal organ may be selected");
            pawn.health.hediffSet.hediffs.Add(new Hediff_AddedPart { pawn = pawn, Part = eye, def = new HediffDef { defName = "BionicEye", addedPartProps = new() } });
            Check.Equal(false, FleshCombatPatches.EdiblePart(pawn, eye), "Artificial part excluded");
            Check.Equal(false, FleshCombatPatches.EdiblePart(pawn, head), "Parent cannot consume an implanted descendant");
            Check.Equal(true, FleshCombatPatches.EdiblePart(pawn, brain), "Organic organ remains eligible with unrelated implant");
        });
        yield return new("FleshGameplay.RestoreChargesOnlyCurrentExperienceAndRejectsZero", _ =>
        {
            var pawn = ActionPawn(); var p = FleshRepository.Ensure(pawn);
            p.Level = 7; p.Experience = 55; p.SkillPoints = 5; p.TryUpgrade(FleshSkill.Restore);
            Check.Equal(true, FleshActions.TryActivate(pawn, FleshSkill.Restore), "Restoration executes");
            Check.Equal(0f, p.Experience, "Only current-level XP spent");
            Check.Equal(7, p.Level, "Level preserved"); Check.Equal(4, p.SkillPoints, "Unused points preserved");
            Check.Equal(1, p.GetRank(FleshSkill.Restore), "Learned rank preserved");
            Check.Equal(FleshActions.Now + 60000, p.GetReadyTick(FleshSkill.Restore), "Daily cooldown begins");
            p.SetReadyTick(FleshSkill.Restore, 0);
            Check.Equal(false, FleshActions.TryActivate(pawn, FleshSkill.Restore), "Zero XP cannot restore");
            Check.Equal(true, FleshRepository.IsRaisedUndead(pawn), "Undead marker retained");
        });
        yield return new("FleshGameplay.FeastStartsHalfAndOwnerStopPreservesCooldown", _ =>
        {
            var pawn = ActionPawn(); var owner = ActionPawn(); var stranger = ActionPawn();
            var type = typeof(FleshProgress).Assembly.GetType("TorannMagic.HediffComp_Undead")!;
            var comp = (HediffComp)Activator.CreateInstance(type, true)!;
            type.GetField("linkedPawn")!.SetValue(comp, owner);
            var marker = new HediffWithComps { pawn = pawn, def = new HediffDef { defName = "TM_UndeadHD" }, comps = [comp] };
            comp.parent = marker; pawn.health.hediffSet.hediffs.Clear(); pawn.health.hediffSet.hediffs.Add(marker);
            var p = FleshRepository.Ensure(pawn); p.SkillPoints = 100;
            foreach (FleshSkill skill in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Feast)) while (p.TryUpgrade(skill)) { }
            p.TryUpgrade(FleshSkill.Feast); p.Thirst = 13;
            Check.Equal(true, FleshActions.TryActivate(pawn, FleshSkill.Feast), "Unlocked Feast starts");
            Check.Equal(50f, p.Thirst, "Starts at exactly half maximum");
            int deadline = p.FeastUntil; int cooldown = p.GetReadyTick(FleshSkill.Feast);
            FleshActions.StopFeast(stranger, pawn); Check.Equal(deadline, p.FeastUntil, "Foreign owner cannot stop");
            FleshActions.StopFeast(owner, pawn); Check.Equal(0, p.FeastUntil, "Real owner freely stops");
            Check.Equal(50f, p.Thirst, "Stopping preserves thirst"); Check.Equal(cooldown, p.GetReadyTick(FleshSkill.Feast), "Stopping preserves cooldown");
        });
        yield return new("FleshGameplay.BiteFoodUsesRelativeRemainingMeatAndActualDamage", _ =>
        {
            Check.Equal(10f, (float)Call("BiteMeat", 50f, .1f, .5f, 10f, 10f), "Part share normalized against remaining body; no second missing penalty");
            Check.Equal(5f, (float)Call("BiteMeat", 50f, .1f, .5f, 5f, 10f), "Partial damage consumes proportional flesh");
            Check.Equal(0f, (float)Call("BiteMeat", 50f, .1f, .5f, 0f, 10f), "Blocked bite yields no food");
            Check.Equal(0f, (float)Call("BiteMeat", 50f, 0f, .5f, 10f, 10f), "Zero-meat part yields no food");
            Check.Equal(0f, (float)Call("BiteMeat", 0f, .1f, .5f, 10f, 10f), "Meatless target yields no food");
        });
        yield return new("FleshGameplay.PostFeastNaturalHungerCannotTriggerDelayedRage", _ =>
        {
            var p = new FleshProgress { Thirst = 10, LastTick = 100, FeastUntil = 160 };
            Check.Equal(false, FleshActions.FeastReachedMaximum(p, 120000), "Only overlap counts toward Feast threshold");
            FleshActions.Advance(p, 120000);
            Check.Equal(20f, p.Thirst, "Later natural hunger may still reach maximum");
            p.Thirst = 19; p.LastTick = 100; p.FeastUntil = 160;
            Check.Equal(true, FleshActions.FeastReachedMaximum(p, 120000), "Crossing maximum during active overlap is retained");
        });
        yield return new("FleshGameplay.OwnRageRecoveryHeldAboveTwentyPercentOnly", _ =>
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(MentalStateDefOf));
            MentalStateDefOf.Berserk = new MentalStateDef { defName = "Berserk" };
            var pawn = ActionPawn(); var p = FleshRepository.Ensure(pawn);
            var rage = new MentalState_Berserk { pawn = pawn, def = MentalStateDefOf.Berserk };
            typeof(MentalStateHandler).GetField("curStateInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn.mindState.mentalStateHandler, rage);
            p.FeastRage = true; p.Thirst = 4.01f;
            Check.Equal(false, FleshRageRecoveryPatch.Prefix(rage), "Time/downed recovery held above 20%");
            p.Thirst = 4f;
            Check.Equal(true, FleshRageRecoveryPatch.Prefix(rage), "Recovery permitted exactly at 20%");
            p.Thirst = 20; p.FeastRage = false;
            Check.Equal(true, FleshRageRecoveryPatch.Prefix(rage), "Owner stop clears flag before recovery");
            p.FeastRage = true;
            Check.Equal(true, FleshRageRecoveryPatch.Prefix(new MentalState_Berserk { pawn = pawn, def = MentalStateDefOf.Berserk }), "Unrelated state never blocked");
        });
        yield return new("FleshGameplay.RestoreRemovesInjuriesAndNaturalMissingPartsPreservesImplants", _ =>
        {
            var pawn = ActionPawn(); var p = FleshRepository.Ensure(pawn); p.SkillPoints = 1; p.TryUpgrade(FleshSkill.Restore); p.Experience = 35;
            var torso = new BodyPartRecord(); var arm = new BodyPartRecord { parent = torso }; var hand = new BodyPartRecord { parent = arm };
            var leg = new BodyPartRecord { parent = torso }; var foot = new BodyPartRecord { parent = leg };
            pawn.def.race.body = new BodyDef { corePart = torso };
            var injury = new Hediff_Injury { pawn = pawn, Part = torso, def = new HediffDef { defName = "Injury" } };
            var scar = new Hediff_Injury { pawn = pawn, Part = torso, def = new HediffDef { defName = "Scar" } };
            var permanent = new HediffComp_GetsPermanent { parent = scar, props = new HediffCompProperties_GetsPermanent() };
            scar.comps = [permanent];
            var lostNaturalArm = new Hediff_MissingPart { pawn = pawn, Part = arm, def = new HediffDef { defName = "MissingNaturalArm" } };
            var implant = new Hediff_AddedPart { pawn = pawn, Part = hand, def = new HediffDef { defName = "ImplantedHand", addedPartProps = new() } };
            var prosthetic = new Hediff_AddedPart { pawn = pawn, Part = leg, def = new HediffDef { defName = "ProstheticLeg", addedPartProps = new() } };
            var prostheticMissingFoot = new Hediff_MissingPart { pawn = pawn, Part = foot, def = new HediffDef { defName = "ReplacedNaturalFoot" } };
            var disease = new Hediff { pawn = pawn, def = new HediffDef { defName = "UnrelatedCondition" } };
            pawn.health.hediffSet.hediffs.AddRange([injury, scar, lostNaturalArm, implant, prosthetic, prostheticMissingFoot, disease]);
            // Exercise real RemoveHediff and production selection. Only isolate game-render caches
            // and the unrelated health/death AI scheduler, which require a running graphics/game world.
            var assembly = Assembly.Load("0Harmony"); var harmonyType = assembly.GetType("HarmonyLib.Harmony")!;
            var harmony = Activator.CreateInstance(harmonyType, "rwom.regression.flesh-heal")!;
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(FleshGameplayTests).GetMethod(nameof(HeadlessHealthSideEffects), BindingFlags.NonPublic | BindingFlags.Static)!)!;
            var patch = harmonyType.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5);
            healingPawn = pawn;
            try
            {
                patch.Invoke(harmony, [typeof(HediffSet).GetMethod("DirtyCache"), prefix, null, null, null]);
                patch.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange"), prefix, null, null, null]);
                permanent.IsPermanent = true;
                Check.Equal(true, FleshActions.TryActivate(pawn, FleshSkill.Restore), "Healing action executes");
                var left = pawn.health.hediffSet.hediffs;
                Check.Equal(false, left.Contains(injury), "Injury removed"); Check.Equal(false, left.Contains(scar), "Permanent scar removed");
                Check.Equal(false, left.Contains(lostNaturalArm), "Natural parent part restored");
                Check.Equal(true, left.Contains(implant), "Implant beneath restored parent preserved");
                Check.Equal(true, left.Contains(prosthetic), "Prosthetic preserved");
                Check.Equal(true, left.Contains(prostheticMissingFoot), "Absent natural part under prosthetic remains absent");
                Check.Equal(true, left.Contains(disease), "Unrelated conditions untouched");
                Check.Equal(true, FleshRepository.IsRaisedUndead(pawn), "Undead marker preserved");
                Check.Equal(0f, p.Experience, "Experience cost charged after healing");
            }
            finally { healingPawn = null; harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, ["rwom.regression.flesh-heal"]); }
        });
    }

    private static Pawn ActionPawn()
    {
        var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); race.race = new RaceProperties();
        var pawn = new Pawn { def = race }; pawn.health = new Pawn_HealthTracker(pawn);
        pawn.mindState = (Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));
        pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
        typeof(Thing).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn, new Faction { def = new FactionDef { isPlayer = true } });
        pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = "TM_UndeadHD" } });
        return pawn;
    }
    private static Pawn? healingPawn;
    private static bool HeadlessHealthSideEffects(Pawn ___pawn) => ___pawn != healingPawn;
}
