using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using Verse;
using AbilityDef = AbilityUser.AbilityDef;

namespace RWoM.RegressionTests;

// Ф5 spike (card 60): the vanilla Ability bridge. A bridge def carries a
// stable spellId, resolves the original TMAbilityDef through the core
// registry, and drives the EXISTING JecsLite cast path via a transient
// companion PawnAbility (rebuilt from knowledge + defs, never scribed).
// The warm cast path (verbs, projectiles, resources) is not rewritten.
internal static class VanillaBridgeTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("VanillaBridge.DefResolvesOriginal", DefResolvesOriginal);
        yield return new("VanillaBridge.CompanionVerbWiring", CompanionVerbWiring);
        yield return new("VanillaBridge.TrackerGainAndLookup", TrackerGainAndLookup);
        yield return new("VanillaBridge.SleepingDefDegrades", SleepingDefDegrades);
        yield return new("VanillaBridge.SyncGrantsFromKnowledge", SyncGrantsFromKnowledge);
        yield return new("VanillaBridge.SyncRespectsAllowList", SyncRespectsAllowList);
        yield return new("VanillaBridge.GeneratedBridgesAreWellFormed", GeneratedBridgesAreWellFormed);
        yield return new("VanillaBridge.BridgesCarryIcons", BridgesCarryIcons);
        yield return new("VanillaBridge.AllFamiliesEnabled", AllFamiliesEnabled);
        yield return new("VanillaBridge.LegacyGizmoHiddenForEnabled", LegacyGizmoHiddenForEnabled);
        yield return new("VanillaBridge.GizmoDisabledReflectsResources", GizmoDisabledReflectsResources);
        yield return new("VanillaBridge.CanCastReflectsResources", CanCastReflectsResources);
        yield return new("VanillaBridge.BridgeDefForLookup", BridgeDefForLookup);
        yield return new("VanillaBridge.OldSaveConversion", OldSaveConversion);
        yield return new("VanillaBridge.TrackerSave", TrackerSave);
        yield return new("VanillaBridge.OwnerAutocastCommandAtEveryRank", OwnerAutocastCommandAtEveryRank);
        yield return new("VanillaBridge.SinglePlayerJob", SinglePlayerJob);
        yield return new("VanillaBridge.AiJobFlowsThroughCompanion", AiJobFlowsThroughCompanion);
        yield return new("VanillaBridge.BloodSacrificeIdleAndQueue", BloodSacrificeIdleAndQueue);
    }

    // Old saves carry no knowledge comp; loading must inject it into every
    // humanlike pawn (idempotently) so the import + bridge grant migrate the
    // save to the new format in place. Non-humanlike pawns are skipped.
    private static void OldSaveConversion(RuntimeFixture _)
    {
        var pawn = BridgePawn(); // no knowledge comp yet
        Check.Equal(true, pawn.TryGetComp<AbilityKnowledgeComp>() == null, "fresh pawn starts without the comp");

        VanillaBridgeSync.InjectKnowledgeComp(pawn);
        Check.NotNull(pawn.TryGetComp<AbilityKnowledgeComp>(), "injection adds the knowledge comp");

        var before = pawn.AllComps.Count;
        VanillaBridgeSync.InjectKnowledgeComp(pawn);
        Check.Equal(before, pawn.AllComps.Count, "injection is idempotent");

        var animal = BridgePawn();
        animal.def.race.intelligence = Intelligence.Animal;
        VanillaBridgeSync.InjectKnowledgeComp(animal);
        Check.Equal(true, animal.TryGetComp<AbilityKnowledgeComp>() == null, "animal stays outside the migration");
        var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        VanillaBridgeSync.ConvertExistingPawns(null);
        VanillaBridgeSync.ConvertExistingPawns(game);
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game,
            new List<Map> { null, (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
        game.World = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        game.World.worldPawns = new WorldPawns();
        var oldWorldPawn = BridgePawn();
        game.World.worldPawns.AllPawnsDead.Add(oldWorldPawn);
        VanillaBridgeSync.ConvertExistingPawns(game);
        VanillaBridgeSync.ConvertExistingPawns(game);
        var added = oldWorldPawn.AllComps.OfType<AbilityKnowledgeComp>().Single();
        Check.Same(oldWorldPawn, added.parent, "existing world pawn gains exactly one initialized comp");
        Check.Same(oldWorldPawn.def.comps[0], added.props, "uses the registered comp properties");
    }

    // Generated bridge defs carry the original's icon so the vanilla gizmo
    // grid renders recognizably.
    private static void BridgesCarryIcons(RuntimeFixture f)
    {
        string heal = Path.Combine(f.Repository, "v1.7", "Defs", "AbilityDefs", "Bridge", "Bridge_Abilities_TMagic_Paladin.xml");
        var doc = System.Xml.Linq.XDocument.Load(heal);
        var bridge = doc.Root.Elements("AbilityDef").First(el => (string)el.Element("defName") == "TM_Bridge_Heal");
        Check.Equal("UI/Heal", (string)bridge.Element("iconPath"), "iconPath copied from the original uiIconPath");
        Check.Equal(true, (bool?)bridge.Element("displayGizmoWhileUndrafted") == true,
            "undrafted visibility matches the classic gizmo behavior");
    }

    // Full rollout: every convention spellId family is enabled — the boundary
    // check still applies, so unrelated rwom.* ids stay untouched.
    private static void AllFamiliesEnabled(RuntimeFixture _)
    {
        Check.Equal(true, VanillaBridgeSync.FamilyEnabled("rwom.tm_heal"), "heal enabled");
        Check.Equal(true, VanillaBridgeSync.FamilyEnabled("rwom.tm_firebolt_ii"), "variant families enabled");
        Check.Equal(true, VanillaBridgeSync.FamilyEnabled("rwom.tm_advanced_heal"), "multi-word families enabled");
        Check.Equal(false, VanillaBridgeSync.FamilyEnabled("rwom.custom.heal"), "foreign namespaces stay excluded");
    }

    // Phase Б: our classic gizmo hides only when THIS pawn's vanilla tracker
    // actually holds the bridge — an old save without the knowledge comp gets
    // no bridges and must keep its classic gizmos (the fallback world).
    private static void LegacyGizmoHiddenForEnabled(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var def = Original("TM_HideGizmoA");
        CoreBridge.RegisterSpell(def);
        Check.Equal(false, VanillaBridgeSync.ShouldHideLegacyGizmo(null, def), "no bridge def — classic gizmo stays");

        var bridge = BridgeDef("TM_Bridge_Hidegizmoa", "rwom.tm_hidegizmoa"); // matches the DisplayName convention
        DefDatabase<RimWorld.AbilityDef>.Clear();
        DefDatabase<RimWorld.AbilityDef>.Add(bridge);
        try
        {
            var pawn = BridgePawn();
            pawn.abilities = new Pawn_AbilityTracker(pawn);

            // Old-save shape: the bridge is NOT granted — the classic gizmo
            // must stay visible even though the bridge def exists.
            Check.Equal(false, VanillaBridgeSync.ShouldHideLegacyGizmo(pawn, def),
                "a pawn without the granted bridge keeps its classic gizmo");

            GiveOwner(pawn, bridge);
            pawn.abilities.GainAbility(bridge);
            Check.Equal(true, VanillaBridgeSync.ShouldHideLegacyGizmo(pawn, def),
                "a pawn holding the granted bridge hides the classic gizmo");

            var other = Original("TM_HideGizmoB");
            CoreBridge.RegisterSpell(other);
            Check.Equal(false, VanillaBridgeSync.ShouldHideLegacyGizmo(pawn, other), "spell without a bridge def keeps its classic gizmo");
        }
        finally
        {
            DefDatabase<RimWorld.AbilityDef>.Clear();
        }
    }

    // The gizmo's grey-out consults the companion's CanCastPowerCheck, which
    // probes ambient state a generated or exotic pawn may not carry — the
    // probe must never throw out of the gizmo (the cast re-checks anyway).
    // The bridge is also invisible to vanilla AI: our ResolveAIAutoCast keeps
    // AI ownership, so the bridge must not double-cast.
    private static void GizmoDisabledReflectsResources(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        CoreBridge.RegisterSpell(Original("TM_Heal"));
        var def = BridgeDef("TM_Bridge_Heal", "rwom.tm_heal");
        var pawn = BridgePawn();
        var magic = new CompAbilityUserMagic { parent = pawn };
        pawn.AllComps.Add(magic);
        var knowledge = new AbilityKnowledgeComp { parent = pawn };
        pawn.AllComps.Add(knowledge);

        var current = GiveOwner(pawn, def);
        var ability = (TMBridgeAbility)AbilityUtility.MakeAbility(def, pawn);
        Check.Same(current, ability.Companion, "companion built for the disabled-state check");
        bool disabled = false;
        try
        {
            disabled = ability.GizmoDisabled(out _);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("GizmoDisabled must tolerate ambient state: " + ex.Message, ex);
        }
        Check.Equal(true, disabled, "unspawned pawn cannot cast");
        RuntimeLog.Clear(); // This fixture has no language pack for the disabled reason.
        Check.Equal(false, ability.AICanTargetNow(pawn), "bridges are excluded from vanilla AI targeting");
    }

    // External AI drivers and the vanilla cast job read CanCast; the vanilla
    // tracker never holds the real cooldown or resource state, so the bridge
    // routes readiness through the companion's admission check. It must be
    // quiet and tolerate the same ambient state GizmoDisabled tolerates.
    private static void CanCastReflectsResources(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        CoreBridge.RegisterSpell(Original("TM_Heal"));
        var def = BridgeDef("TM_Bridge_Heal", "rwom.tm_heal");
        var pawn = BridgePawn();
        var magic = new CompAbilityUserMagic { parent = pawn };
        pawn.AllComps.Add(magic);
        var knowledge = new AbilityKnowledgeComp { parent = pawn };
        pawn.AllComps.Add(knowledge);
        GiveOwner(pawn, def);
        var ability = (TMBridgeAbility)AbilityUtility.MakeAbility(def, pawn);
        Check.NotNull(ability.Companion, "companion built for the CanCast check");
        bool canCast;
        try
        {
            canCast = ability.CanCast;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("CanCast must tolerate ambient state: " + ex.Message, ex);
        }
        Check.Equal(false, canCast, "an unspawned pawn cannot cast through the bridge");
        RuntimeLog.Clear();
    }

    // The read-only lookup optional integrations use: enabled family + a
    // registered bridge def; it grants nothing and touches no state.
    private static void BridgeDefForLookup(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        CoreBridge.RegisterSpell(Original("TM_Heal"));
        var bridge = BridgeDef("TM_Bridge_Heal", "rwom.tm_heal");
        DefDatabase<RimWorld.AbilityDef>.Clear();
        DefDatabase<RimWorld.AbilityDef>.Add(bridge);
        try
        {
            // Earlier fixtures may have cached this spellId in the sync's own
            // def cache, so assert the resolved shape rather than identity.
            var resolved = VanillaBridgeSync.BridgeDefFor(Original("TM_Heal"));
            Check.Equal(true, resolved is TMBridgeAbilityDef { defName: "TM_Bridge_Heal" }, "the enabled bridge def resolves by spell identity");
            Check.Same(null, VanillaBridgeSync.BridgeDefFor(null), "null def stays null");
            Check.Same(null, VanillaBridgeSync.BridgeDefFor(Original("TM_WithoutBridge")), "spell without a bridge def stays null");
        }
        finally
        {
            DefDatabase<RimWorld.AbilityDef>.Clear();
        }
    }

    // Full rollout: foreign namespaces (non "rwom.tm_*") stay excluded even
    // when the knowledge store has a learned entry and a bridge def exists.
    private static void SyncRespectsAllowList(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var bridge = BridgeDef("TM_Bridge_Customheal", "rwom.custom_heal");
        DefDatabase<RimWorld.AbilityDef>.Clear();
        DefDatabase<RimWorld.AbilityDef>.Add(bridge);
        try
        {
            var pawn = BridgePawn();
            pawn.abilities = new Pawn_AbilityTracker(pawn);
            var knowledge = new AbilityKnowledgeComp { parent = pawn };
            pawn.AllComps.Add(knowledge);
            var entry = knowledge.GetOrCreate("rwom.custom_heal", 0);
            entry.learned = true;

            VanillaBridgeSync.Sync(pawn);
            Check.Equal(true, pawn.abilities.GetAbility(bridge) == null,
                "a foreign namespace is not granted (FamilyEnabled gate)");
        }
        finally
        {
            DefDatabase<RimWorld.AbilityDef>.Clear();
        }
    }

    // RED 7: every generated bridge def XML is well formed, carries a spellId
    // and a verb class, and the defName follows the sync convention.
    private static void GeneratedBridgesAreWellFormed(RuntimeFixture f)
    {
        string dir = Path.Combine(f.Repository, "v1.7", "Defs", "AbilityDefs", "Bridge");
        Check.Equal(true, Directory.Exists(dir), "bridge output directory exists");
        var files = Directory.GetFiles(dir, "*.xml");
        Check.Equal(true, files.Length > 10, "generated bridge files present");

        var seen = new HashSet<string>();
        var bridgeIds = new HashSet<string>(StringComparer.Ordinal);
        int defs = 0;
        foreach (string file in files)
        {
            var doc = System.Xml.Linq.XDocument.Load(file);
            foreach (var el in doc.Root.Elements("AbilityDef"))
            {
                Check.Equal("TorannMagic.TMBridgeAbilityDef", (string)el.Attribute("Class"),
                    "bridge class on " + file);
                if (el.Attribute("Abstract") != null) continue; // the shared base uses Name=
                string defName = (string)el.Element("defName");
                Check.Equal(true, !string.IsNullOrEmpty(defName) && seen.Add(defName),
                    "unique non-empty defName " + defName);
                if (el.Attribute("Abstract") != null) continue; // the shared base
                Check.Equal(true, bridgeIds.Add((string)el.Element("spellId")!), "unique bridge spellId on " + defName);
                var verbProps = el.Element("verbProperties");
                Check.NotNull(verbProps, "verbProperties on " + defName);
                // Subclasses of VerbProperties_Ability carry family config — the copy
                // preserves the original's Class as-is.
                Check.Equal(true, !string.IsNullOrEmpty((string)verbProps.Attribute("Class")),
                    "verb properties class on " + defName);
                Check.NotNull(verbProps.Element("verbClass"), "verb class on " + defName);
                Check.Equal(true, (bool?)verbProps.Element("isPrimary") ?? true, "tracker needs a primary verb on " + defName);
                Check.Equal(true, defName.StartsWith("TM_Bridge_"), "defName convention on " + defName);
                defs++;
            }
        }
        var casterIds = new HashSet<string>(StringComparer.Ordinal);
        var registered = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(Path.Combine(f.Repository, "v1.7", "Defs"), "*.xml", SearchOption.AllDirectories))
        {
            foreach (var def in System.Xml.Linq.XDocument.Load(path).Root!.Elements())
            foreach (var ext in def.Element("modExtensions")?.Elements("li") ?? Enumerable.Empty<System.Xml.Linq.XElement>())
            {
                if ((string?)ext.Attribute("Class") != "TorannMagic.Core.SpellIdentityExtension") continue;
                var spellId = ext.Element("spellId")!.Value;
                Check.Equal("huginn.rwom.core", (string?)ext.Attribute("MayRequire"), "optional core on " + spellId);
                Check.Equal(true, registered.Add(spellId), "duplicate registry identity " + spellId);
                Check.Equal(true, def.Name.LocalName.EndsWith("AbilityDef", StringComparison.Ordinal), "non-ability claiming " + spellId);
                if (def.Name.LocalName == "TorannMagic.TMAbilityDef") casterIds.Add(spellId);
            }
        }
        Check.Equal(true, bridgeIds.SetEquals(casterIds), "each RWoM spell has exactly one bridge");
        Check.Equal(true, defs > 400, "generated bridge def count covers the ability tree: " + defs);
    }

    // RED 5: the sync grants the bridge ability for a learned knowledge entry,
    // idempotently, and skips unlearned or sleeping entries.
    private static void SyncGrantsFromKnowledge(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var original = Original("TM_Heal");
        CoreBridge.RegisterSpell(original);
        var bridge = BridgeDef("TM_Bridge_Heal", "rwom.tm_heal"); // matches the defName convention
        DefDatabase<RimWorld.AbilityDef>.Clear();
        DefDatabase<RimWorld.AbilityDef>.Add(bridge);
        try
        {
            var pawn = BridgePawn();
            pawn.abilities = new Pawn_AbilityTracker(pawn); // the Pawn ctor leaves this to spawn
            var knowledge = new AbilityKnowledgeComp { parent = pawn };
            pawn.AllComps.Add(knowledge);

            VanillaBridgeSync.Sync(pawn);
            Check.Equal(true, pawn.abilities.GetAbility(bridge) == null, "unlearned knowledge grants nothing");

            var entry = knowledge.GetOrCreate("rwom.tm_heal", 0);
            entry.learned = true;
            VanillaBridgeSync.Sync(pawn);
            Check.NotNull(pawn.abilities.GetAbility(bridge), "learned knowledge grants the bridge ability");

            VanillaBridgeSync.Sync(pawn);
            Check.Equal(1, pawn.abilities.abilities.Count, "sync is idempotent");
            entry.learned = false;
            VanillaBridgeSync.Sync(pawn);
            Check.Equal(true, pawn.abilities.GetAbility(bridge) == null, "unlearning revokes the bridge");
        }
        finally
        {
            DefDatabase<RimWorld.AbilityDef>.Clear();
        }
    }

    // The owner submits player orders inside UseAbility. A second submission
    // here would touch pawn.jobs (deliberately absent on this fixture).
    private sealed class CountingOwner : AbilityUser.PawnAbility
    {
        public int Calls;
        public override Verse.AI.Job UseAbility(AbilityUser.AbilityContext context, LocalTargetInfo target)
        {
            Check.Equal(global::AbilityUser.AbilityContext.Player, context, "manual cast context");
            Calls++;
            return new Verse.AI.Job();
        }
    }

    private sealed class BridgeAdmissionProbe : TMBridgeAbility
    {
        public bool Blocked;
        public override bool GizmoDisabled(out string reason) { reason = ""; return Blocked; }
    }

    private static void BloodSacrificeIdleAndQueue(RuntimeFixture f)
    {
        var xml = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository, "v1.7", "Defs", "AbilityDefs",
            "Bridge", "Bridge_Abilities_TMagic_BloodMage.xml"));
        var row = xml.Root!.Elements("AbilityDef").Single(e => (string?)e.Element("spellId") == "rwom.tm_bloodgift");
        var props = row.Element("verbProperties")!;
        var def = BridgeDef(row.Element("defName")!.Value, "rwom.tm_bloodgift");
        def.verbProperties.verbClass = typeof(TMBridgeAbility).Assembly.GetType(props.Element("verbClass")!.Value, true)!;
        def.verbProperties.isPrimary = (bool?)props.Element("isPrimary") ?? def.verbProperties.isPrimary;
        EnsureCoreLoaded(f);
        CoreBridge.RegisterSpell(Original("TM_BloodGift"));
        var pawn = BridgePawn();
        pawn.jobs = new Verse.AI.Pawn_JobTracker(pawn);
        // A permissive companion isolates the queue-reservation logic from the
        // admission check: an unspawned fixture pawn legitimately fails
        // CanCastPowerCheck, which CanCast now reflects.
        var user = pawn.TryGetComp<CompAbilityUserMagic>();
        user.AbilityData.Powers.Add(new GizmoOwner { Def = def.Original, AbilityUser = user });
        user.AbilityData.AllPowers = null;
        VanillaBridgeSync.InjectKnowledgeComp(pawn);
        pawn.TryGetComp<AbilityKnowledgeComp>().GetOrCreate("rwom.tm_bloodgift", 0).learned = true;
        var job = new Verse.AI.Job(new JobDef { defName = "OrdinaryWork" });
        pawn.jobs.jobQueue.EnqueueLast(job);
        var ability = (TMBridgeAbility)AbilityUtility.MakeAbility(def, pawn);
        Check.Equal(true, (bool)ability.CanCast, "the permissive companion keeps CanCast open");
        Check.Equal(false, ability.Casting, "idle Blood Sacrifice must be drawable");
        Check.Equal(true, ability.CanQueueCast, "ordinary work without a verb must not reserve Blood Sacrifice");
        job.verbToUse = ability.verb;
        Check.Equal(false, ability.CanQueueCast, "an actual queued bridge verb still blocks a duplicate");
    }

    private sealed class GizmoDef : TMAbilityDef
    {
        public override string GetDescription() => "Test spell";
    }

    private sealed class GizmoOwner : AbilityUser.PawnAbility
    {
        public override bool CanCastPowerCheck(global::AbilityUser.AbilityContext context, out string reason)
        { reason = ""; return true; }
    }

    private static void OwnerAutocastCommandAtEveryRank(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var parent = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository, "v1.7", "Defs", "AbilityDefs",
            "Bridge", "Abilities_Bridge_Heal.xml")).Root!.Elements().Single(e => (string?)e.Attribute("Name") == "TMBridgeAbilityBase");
        Check.Equal(false, (bool?)parent.Element("disableGizmoWhileUndrafted") ?? true, "no blanket draft gate");
        Check.Equal(false, (bool?)parent.Element("casterMustBeCapableOfViolence") ?? true, "owner decides whether spell is violent");
        var pawn = BridgePawn();
        var user = pawn.TryGetComp<CompAbilityUserMagic>();
        foreach (var suffix in new[] { "", "_II" })
        {
            var original = (GizmoDef)RuntimeHelpers.GetUninitializedObject(typeof(GizmoDef));
            original.defName = "TM_AutocastButtonProbe" + suffix;
            original.label = "test spell";
            original.MainVerb = new AbilityUser.VerbProperties_Ability { verbClass = typeof(AbilityUser.Verb_UseAbility) };
            CoreBridge.RegisterSpell(original);
            var bridge = BridgeDef("TM_Bridge_Autocastbuttonprobe" + suffix, SpellIds.Of(original));
            GiveOwner(pawn, bridge);
            var owner = new GizmoOwner { Def = original, AbilityUser = user };
            user.AbilityData.Powers.Clear();
            user.AbilityData.Powers.Add(owner);
            user.AbilityData.AllPowers = null;
            var ability = new BridgeAdmissionProbe { pawn = pawn, def = bridge };
            var cmd = (AbilityUser.Command_PawnAbility)ability.GetGizmos().Single();
            Check.Same(owner, cmd.pawnAbility, "autocast toggle receives live owner at rank " + suffix);
            Check.Same(owner.Verb, cmd.verb, "manual targeting uses same verb");
        }
    }

    private static void SinglePlayerJob(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var original = Original("TM_OnePlayerJob");
        CoreBridge.RegisterSpell(original);
        var bridge = BridgeDef("TM_Bridge_Oneplayerjob", SpellIds.Of(original));
        var pawn = BridgePawn();
        GiveOwner(pawn, bridge);
        var user = pawn.TryGetComp<CompAbilityUserMagic>();
        var owner = new CountingOwner { AbilityUser = user, Def = original };
        user.AbilityData.Powers[0] = owner;
        user.AbilityData.AllPowers = null;
        var ability = new BridgeAdmissionProbe { pawn = pawn, def = bridge };
        ability.QueueCastingJob(pawn, LocalTargetInfo.Invalid);
        Check.Equal(1, owner.Calls, "queue submits exactly one player order");
        ability.Blocked = true;
        ability.QueueCastingJob(pawn, LocalTargetInfo.Invalid);
        Check.Equal(false, ability.Activate(pawn, LocalTargetInfo.Invalid), "blocked admission rejects direct activation too");
        Check.Equal(1, owner.Calls, "no extra order on rejection");
    }

    // The live crash: Ability AI hands out ruled bridge spells through the
    // vanilla cast machinery, and the bridge's own tracker verb has no
    // PawnAbility owner, so vanilla warmup firing it crashed inside
    // Verb_UseAbility. GetJob must return the companion's classic job —
    // the owner verb, the TM drivers, payment and cooldown all stay on
    // their normal path — tagged with the bridge ability so external
    // drivers recognize the running cast.
    private static void AiJobFlowsThroughCompanion(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var original = Original("TM_OnePlayerJob");
        CoreBridge.RegisterSpell(original);
        var bridge = BridgeDef("TM_Bridge_Oneplayerjob", SpellIds.Of(original));
        var pawn = BridgePawn();
        GiveOwner(pawn, bridge);
        var user = pawn.TryGetComp<CompAbilityUserMagic>();
        var owner = new GizmoOwner { Def = original, AbilityUser = user };
        user.AbilityData.Powers[0] = owner;
        user.AbilityData.AllPowers = null;
        var ability = (TMBridgeAbility)AbilityUtility.MakeAbility(bridge, pawn);
        // A cell target: a synthetic pawn target would NRE in PawnAbility.GetJob's
        // Downed check (the fixture pawn has no health state) — in game targets
        // are spawned things or cells.
        var job = ability.GetJob(new LocalTargetInfo(new IntVec3(3, 0, 3)), LocalTargetInfo.Invalid);
        Check.NotNull(job, "companion job returned for the external AI path");
        Check.Same(owner.Verb, job.verbToUse, "the classic owner verb casts, not the tracker verb");
        Check.Equal(1, job.count, "null-faction fixture keeps the historical player context");
        Check.Same(ability, job.ability, "job is tagged with the bridge ability for running-cast detection");
        Check.Same(AbilityUser.AbilityDefOf.CastAbilityVerb, job.def, "the classic verb job runs the cast");

        var sleeping = (TMBridgeAbility)AbilityUtility.MakeAbility(bridge, BridgePawn());
        Check.Same(null, sleeping.GetJob(new LocalTargetInfo(new IntVec3(3, 0, 3)), LocalTargetInfo.Invalid), "sleeping spell refuses without a job");
    }

    private static void TrackerSave(RuntimeFixture f)
    {
        var tracker = new Pawn_AbilityTracker(BridgePawn());
        var bridge = BridgeDef("TM_Bridge_SaveProbe", "rwom.tm_saveprobe");
        tracker.GainAbility(bridge);
        var nativeDef = BridgeDef("Native_SaveProbe", "unused");
        nativeDef.abilityClass = typeof(Ability);
        tracker.GainAbility(nativeDef);
        var type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        const string id = "rwom.test.bridge.tracker-save";
        var harmony = Activator.CreateInstance(type, id)!;
        var path = Path.Combine(f.WorkingDirectory, "tracker.xml");
        try
        {
            var patch = typeof(VanillaBridgeSync).Assembly.GetType("TorannMagic.VanillaBridgeTrackerSavePatch")!;
            var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            Scribe.saver.InitSaving(path, "tracker");
            tracker.ExposeData();
            Scribe.saver.FinalizeSaving();
        }
        finally { Scribe.ForceStop(); type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
        var xml = System.Xml.Linq.XDocument.Load(path);
        var defs = xml.Descendants("def").Select(x => x.Value).ToList();
        Check.Equal(true, defs.Contains(nativeDef.defName), "native tracker ability is saved");
        Check.Equal(false, defs.Contains(bridge.defName), "transient bridge is omitted");
        Check.Equal(2, tracker.abilities.Count, "save does not mutate live abilities");
    }

    private static void EnsureCoreLoaded(RuntimeFixture f)
    {
        var coreDll = CoreSdkTests.AssemblyPath(f.Repository);
        // Each fixture constructs its own synthetic Def instances, including TM_Heal.
        if (File.Exists(coreDll))
            Assembly.LoadFrom(coreDll).GetType("TorannMagic.Core.AbilityCore", true)!.GetMethod("Invalidate")!.Invoke(null, null);
    }

    private static TMAbilityDef Original(string name)
    {
        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = name;
        def.label = name;
        def.MainVerb = new AbilityUser.VerbProperties_Ability { verbClass = typeof(AbilityUser.Verb_UseAbility) };
        return def;
    }

    private static TMBridgeAbilityDef BridgeDef(string name, string spellId)
    {
        var def = (TMBridgeAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMBridgeAbilityDef));
        def.defName = name;
        def.label = name;
        def.abilityClass = typeof(TMBridgeAbility);
        def.spellId = spellId;
        // GetUninitializedObject skips field initializers; the vanilla Ability
        // ctor reads def.comps and the tracker reads def.verbProperties. A real
        // XML bridge def carries a VerbProperties_Ability copy of the original.
        def.comps = new List<AbilityCompProperties>();
        def.verbProperties = new AbilityUser.VerbProperties_Ability { verbClass = typeof(AbilityUser.Verb_UseAbility) };
        return def;
    }

    private static AbilityUser.PawnAbility GiveOwner(Pawn pawn, TMBridgeAbilityDef bridge)
    {
        var user = pawn.TryGetComp<CompAbilityUserMagic>();
        var owner = new AbilityUser.PawnAbility { Def = bridge.Original, AbilityUser = user };
        user.AbilityData.Powers.Add(owner);
        user.AbilityData.AllPowers = null;
        VanillaBridgeSync.InjectKnowledgeComp(pawn);
        pawn.TryGetComp<AbilityKnowledgeComp>().GetOrCreate(bridge.spellId, 0).learned = true;
        return owner;
    }

    private static Pawn BridgePawn()
    {
        if (Current.Game == null)
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
                .SetValue(Current.Game, new TickManager());
            typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
                .SetValue(Current.Game, new UniqueIDsManager());
        }
        var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawnDef.defName = "Regression_BridgeHuman";
        pawnDef.comps = new List<CompProperties> { new CompProperties { compClass = typeof(AbilityKnowledgeComp) } };
        pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = pawnDef };
        // The companion PawnAbility binds to the pawn's CompAbilityUser
        // (CompAbilityUserMagic is a CompAbilityUser subclass). comps lives on
        // ThingWithComps, not Pawn.
        for (Type type = pawn.GetType(); type != null; type = type.BaseType)
        {
            var comps = type.GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (comps == null) continue;
            comps.SetValue(pawn, new List<ThingComp> { new CompAbilityUserMagic { parent = pawn } });
            break;
        }
        return pawn;
    }

    // RED 1: the bridge def resolves its original through the stable spellId
    // via the core registry.
    private static void DefResolvesOriginal(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var original = Original("TM_ResolutionProbe");
        CoreBridge.RegisterSpell(original);
        var def = BridgeDef("TM_Bridge_Resolutionprobe", "rwom.tm_resolutionprobe");
        Check.Same(original, def.Original, "bridge def resolves the original TMAbilityDef");

        var defMissing = BridgeDef("TM_Bridge_Lost", "rwom.tm_removed");
        Check.Equal(true, defMissing.Original == null, "missing original resolves null (sleeping), never throws");
    }

    // RED 2: the companion PawnAbility wires the real verb: Ability backref,
    // caster, and the original's MainVerb props.
    private static void CompanionVerbWiring(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        CoreBridge.RegisterSpell(Original("TM_Heal"));
        var def = BridgeDef("TM_Bridge_Heal2", "rwom.tm_heal");
        var pawn = BridgePawn();
        var realOwner = GiveOwner(pawn, def);
        var ability = (TMBridgeAbility)AbilityUtility.MakeAbility(def, pawn);
        Check.NotNull(ability, "MakeAbility constructs the bridge ability");
        var companion = ability.Companion;
        Check.Same(realOwner, companion, "targeting and cooldown use the live owner");
        Check.Same(def.Original, companion.Def, "companion wraps the original def");
        var verb = companion.Verb;
        Check.NotNull(verb, "companion verb initialized");
        Check.Same(companion, verb.Ability, "verb points back at the companion");
        Check.Same(pawn, verb.caster, "verb caster is the pawn");
        Check.Same(def.Original.MainVerb, verb.verbProps, "verb uses the original MainVerb props");
    }

    // RED 3: GainAbility into the vanilla tracker; the bridge ability is
    // findable through the vanilla lookup.
    private static void TrackerGainAndLookup(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        CoreBridge.RegisterSpell(Original("TM_Heal"));
        var def = BridgeDef("TM_Bridge_Heal3", "rwom.tm_heal");
        var pawn = BridgePawn();
        var tracker = new Pawn_AbilityTracker(pawn);
        tracker.GainAbility(def);
        var gained = tracker.GetAbility(def);
        Check.NotNull(gained, "vanilla tracker holds the bridge ability");
        Check.Equal(true, gained is TMBridgeAbility, "gained ability is the bridge subclass");
        tracker.RemoveAbility(def);
        Check.Equal(true, tracker.GetAbility(def) == null, "removal works");
    }

    // RED 4: a bridge whose original is gone reports disabled through the
    // vanilla gate without a companion and without throwing.
    private static void SleepingDefDegrades(RuntimeFixture f)
    {
        EnsureCoreLoaded(f);
        var def = BridgeDef("TM_Bridge_Lost2", "rwom.tm_removed");
        var pawn = BridgePawn();
        var ability = (TMBridgeAbility)AbilityUtility.MakeAbility(def, pawn);
        Check.Equal(true, ability.Companion == null, "no companion without the original def");
        bool disabled;
        try
        {
            disabled = ability.GizmoDisabled(out _);
        }
        finally
        {
            // Headless runs have no language pack, so the vanilla disabled-reason
            // Translate() logs "No active language!" — an environment limitation.
            RuntimeLog.Clear();
        }
        Check.Equal(true, disabled, "sleeping bridge is gizmo-disabled");
    }
}
