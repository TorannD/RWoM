using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using Verse.AI;
using RimWorld;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthRuntimeTests
{
    public sealed class SpiritPassive : PassiveEffectWorker
    {
        public static int Calls;
        public override void Tick(CompAbilityUser user) { Calls++; }
    }
    public sealed class FormerPassive : PassiveEffectWorker
    {
        public static int Calls;
        public override void Tick(CompAbilityUser user) { Calls++; }
    }
    public static IEnumerable<TestCase> Cases()
    {
        foreach (bool partial in new[] { false, true })
        yield return new("SpiritRuntime.PendingReceiptAccounting." + (partial ? "NativeRevivalException" : "Paid"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            if (partial) typeof(SpiritRebirthRitualTransactionTests).GetMethod("AliveException", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c]);
            else Check.Equal("ConvertedHeld", c.Execute(), "actual same-Pawn paid conversion");
            using var setup = new SpiritRebirthReleaseTests.ReleaseFixture(r, c);
            using var ui = new UiPauseScope();
            using var physicalProbe = new PhysicalTickProbe(c.Original, c.Creator);
            using var pendingPatches = new FleshStatTests.ProductionPatches();
            SpiritRebirthActionTests.Patch(pendingPatches, "SpiritRebirthPendingTickPatch");
            SpiritRebirthActionTests.Patch(pendingPatches, "SpiritRebirthPendingIntervalPatch");
            var pawn = c.Original; var magic = pawn.GetComp<CompAbilityUserMagic>();
            pawn.def.tickerType = TickerType.Normal;
            c.Map.gameConditionManager = new GameConditionManager(c.Map);
            var receipt = ClassIdentity.Component(pawn).GetType().GetProperty("SpiritRitual", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ClassIdentity.Component(pawn))!;
            var oldSettings = TorannMagic.ModOptions.Settings.Instance;
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            try
            {
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                foreach (var name in new[] { "TM_SyrriumSenseHD", "TM_PomanaSenseHD" })
                    if (DefDatabase<HediffDef>.GetNamedSilentFail(name) == null) DefDatabase<HediffDef>.Add(new HediffDef { defName = name });
                Set(ClassIdentity.Component(pawn), "state", new ClassProgressionState(Vocation.Warrior));
                var data = (MagicData)typeof(CompAbilityUserMagic).GetProperty("ExistingMagicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(magic)!;
                Check.NotNull(data, "native original already owns saved MagicData");
                var mana = new Need_Mana(pawn) { def = TorannMagicDefOf.TM_Mana, CurLevel = .5f };
                Set(mana, "lastParacyteCheck", int.MaxValue);
                pawn.needs.AllNeeds.Clear(); pawn.needs.AllNeeds.Add(mana);
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
                Check.Same(mana, pawn.needs.TryGetNeed<Need_Mana>(), "pending exact original retains existing Mana instance");
                Check.Equal(true, pawn.needs.TryGetNeed<Need_Spirit>() == null, "unprepared pending original does not initialize missing Spirit");
                pawn.needs.AllNeeds.Remove(mana); pawn.needs.AddOrRemoveNeedsAsAppropriate();
                Check.Equal(true, pawn.needs.TryGetNeed<Need_Mana>() == null, "unprepared pending original does not initialize missing Mana");
                pawn.needs.AllNeeds.Add(mana);
                var spirit = new Need_Spirit(pawn) { def = TorannMagicDefOf.TM_SpiritND, CurLevel = 0f };
                pawn.needs.AllNeeds.Add(spirit); pawn.needs.AddOrRemoveNeedsAsAppropriate();
                Check.Same(spirit, pawn.needs.TryGetNeed<Need_Spirit>(), "pending original retains existing zero Spirit instance");
                Check.Equal(0f, spirit.CurLevel, "pending accounting does not refill zero Spirit");
                magic.summonedMinions = Enumerable.Range(0, 8).Select(_ => (Thing)PawnWithComps(false, false).pawn).ToList();
                var summons = magic.summonedMinions.ToArray(); magic.mpRegenRate = 1f; magic.maxMP = 1f;
                var former = r.AddAbility("PendingRuntimeFormer"); former.PassiveProps = new PassiveEffectProperties { worker = typeof(FormerPassive) };
                var canonical = r.Ability("TM_SpiritDrain"); canonical.PassiveProps = new PassiveEffectProperties { worker = typeof(SpiritPassive) };
                var power = new MagicAbility(magic) { Def = former, CooldownTicksLeft = 11 };
                magic.AbilityData.Powers.Add(power); magic.AbilityData.Powers.Add(new MagicAbility(magic) { Def = canonical, CooldownTicksLeft = 9 });
                magic.AbilityData.AllPowers = null; FormerPassive.Calls = SpiritPassive.Calls = 0;
                spirit.CurLevel = 5f;
                Check.Equal(false, SpiritRebirthCasting.Allows(pawn, canonical), "pending positive energy cannot admit canonical cast");
                Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "receipt moves actual pending original to native creator inventory");
                Check.Same(c.Creator.inventory.innerContainer, pawn.holdingOwner, "known inventory owns actual original");
                int tick = Enumerable.Range(1, 150).First(t => { Set(Find.TickManager, "ticksGameInt", t); return pawn.IsHashIntervalTick(150); });
                c.Creator.inventory.innerContainer.DoTick();
                Check.Equal(true, mana.CurLevel < .5f, "native inventory Pawn tick charges existing minion upkeep");
                float once = mana.CurLevel; c.Creator.inventory.innerContainer.DoTick();
                Check.Equal(once, mana.CurLevel, "same held observation cannot double-charge upkeep");
                Check.Equal(10, power.CooldownTicksLeft, "pending original cooldown progresses once");
                Check.Equal(0, FormerPassive.Calls, "pending original never runs class passive worker");
                Check.Equal(0, SpiritPassive.Calls, "pending original never runs canonical Spirit passive worker");
                Check.Equal(5f, spirit.CurLevel, "pending original has no ethereal energy drain");
                Check.Equal(0, PhysicalTickProbe.PendingJobs, "pending inventory original does not enter native job interval");
                Check.Equal(0, PhysicalTickProbe.PendingMind, "pending inventory original does not enter native mental interval");
                // Native map ownership can result from an interrupted release or ordinary inventory unloading.
                GenSpawn.Spawn(pawn, new IntVec3(15, 0, 15), c.Map);
                Check.Same(c.Map.spawnedThings, pawn.holdingOwner, "actual pending original moves through native spawn to map custody");
                Set(Find.TickManager, "ticksGameInt", tick + 150); pawn.DoTick();
                Check.Equal(true, mana.CurLevel < once, "actual pending map original continues retained upkeep");
                Check.Equal(0, PhysicalTickProbe.PendingJobs, "pending map original does not enter native job interval");
                Check.Equal(0, PhysicalTickProbe.PendingMind, "pending map original does not enter native mental interval");
                Check.Equal(0, SpiritPassive.Calls + FormerPassive.Calls, "pending map native tick cannot execute passive workers");
                Set(Find.TickManager, "ticksGameInt", tick + 151);
                c.Creator.jobs.JobTrackerTickInterval(1);
                Check.Equal(1, PhysicalTickProbe.CreatorJobs, "ordinary creator native job interval remains enabled");
                Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(pawn).SpiritRebirth.Form, "accounting does not activate Spirit");
                Check.Equal(true, summons.Zip(magic.summonedMinions).All(pair => ReferenceEquals(pair.First, pair.Second)), "existing actual summoned references retained");
                Check.Same(data, typeof(CompAbilityUserMagic).GetProperty("ExistingMagicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(magic), "accounting preserves original class data owner");
                RuntimeLog.AssertClean();
            }
            finally
            {
                TorannMagic.ModOptions.Settings.Instance = oldSettings;
                DefDatabase<HediffDef>.Clear(); foreach (var def in hediffs) DefDatabase<HediffDef>.Add(def);
            }
        }));
        yield return new("SpiritRuntime.NativeOriginalClockAndNeedScribe", r => SoulStoneHolderTests.Save(r, true));
        yield return new("SpiritRuntime.SharedEconomyDifferentialAndReadOnlyOwners", r => SpiritRebirthEnergyTests.Run(r, (c, energy) =>
        {
            LoadBaseDefinitions(r);
            var pawn = c.Creator; var magic = pawn.GetComp<CompAbilityUserMagic>();
            Set(ClassIdentity.Component(pawn), "state", new ClassProgressionState(Vocation.Warrior));
            pawn.apparel = new Pawn_ApparelTracker(pawn); pawn.equipment = new Pawn_EquipmentTracker(pawn);
            pawn.ageTracker = new Pawn_AgeTracker(pawn);
            pawn.kindDef = new PawnKindDef { defName = "SpiritRuntimeHuman", race = pawn.def };
            pawn.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }];
            Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
            c.Map.gameConditionManager = new GameConditionManager(c.Map);
            var oldSun = TorannMagicDefOf.TM_Sunlight; var oldEnchant = TorannMagicDefOf.TM_EnchantWeapon;
            var enchantDefinition = r.Ability("TM_EnchantWeapon");
            float oldEnchantCost = enchantDefinition.manaCost, oldEnchantEfficiency = enchantDefinition.efficiencyReductionPercent;
            var oldPsychic = StatDefOf.PsychicSensitivity;
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            try
            {
                StatDefOf.PsychicSensitivity = new StatDef { defName = "SpiritRuntimePsychic", defaultBaseValue = 1f, workerClass = typeof(StatWorker) };
                if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                    DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
                TorannMagicDefOf.TM_Sunlight = r.AddAbility("SpiritRuntimeSun");
                TorannMagicDefOf.TM_Sunlight.upkeepEnergyCost = .13f; TorannMagicDefOf.TM_Sunlight.upkeepRegenCost = .17f;
                TorannMagicDefOf.TM_EnchantWeapon = enchantDefinition;
                TorannMagicDefOf.TM_EnchantWeapon.manaCost = .21f; TorannMagicDefOf.TM_EnchantWeapon.efficiencyReductionPercent = .1f;
                var effect = new HediffDef { defName = "SpiritRuntimeRetainedEffect" };
                var ability = r.AddAbility("SpiritRuntimeAliasAbility"); ability.abilityHediff = effect;
                ability.upkeepEnergyCost = .12f; ability.upkeepRegenCost = .25f; ability.upkeepEfficiencyPercent = .1f;
                r.AddDefinition(new TM_CustomPowerDef { defName = "SpiritRuntimeAlias", customPower = new TM_CustomPower {
                    forMage = true, abilityDefs = [ability], skills = [new TM_CustomSkill { label = "SpiritRuntimeShared_eff", levelMax = 3 }] } });
                var data = new MagicData(magic);
                data.magicPowerCustom = [new MagicPower([ability])];
                data.magicPowerSkill_Custom = [new MagicPowerSkill { label = "SpiritRuntimeShared_eff", level = 2 }];
                data.magicPowerSkill_EnchantWeapon = [new MagicPowerSkill { label = "TM_EnchantWeapon_eff", level = 2 }];
                Set(magic, "magicData", data);
                magic.summonedLights.Add(c.Circle); magic.weaponEnchants.Add(c.Original);
                pawn.health.hediffSet.hediffs.Add(new Hediff { def = effect, pawn = pawn });
                // Native ordinary calculation supplies the reference values on the same saved owner.
                magic.mpCost = 1f; magic.ResolveEnchantments();
                float max = magic.maxMP, regen = magic.mpRegenRate;
                // Preserve only the originally supplied lists. Lazy ordinary scaffolding is not part of the marked owner.
                var fresh = new MagicData(magic) { magicPowerCustom = data.magicPowerCustom,
                    magicPowerSkill_Custom = data.magicPowerSkill_Custom, magicPowerSkill_EnchantWeapon = data.magicPowerSkill_EnchantWeapon };
                Set(magic, "magicData", fresh);
                var fields = typeof(MagicData).GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(field => field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)).ToArray();
                var before = fields.Select(field => field.GetValue(fresh)).ToArray();
                var resolve = typeof(CompAbilityUserMagic).GetMethod("ResolveOwnedMagicEconomy", BindingFlags.Instance | BindingFlags.NonPublic)!;
                for (int i = 0; i < 2; i++)
                {
                    magic.mpCost = 1f; resolve.Invoke(magic, null);
                    Check.Equal(max, magic.maxMP, "retained structure, weapon and aliased effect match native capacity upkeep");
                    Check.Equal(regen, magic.mpRegenRate, "retained aliased effect and structure match native regeneration upkeep");
                    for (int n = 0; n < fields.Length; n++) Check.Equal(true, ReferenceEquals(before[n], fields[n].GetValue(fresh)), "marked economy cannot initialize or replace " + fields[n].Name);
                }
                Check.Same(c.Circle, magic.summonedLights.Single(), "actual sustained structure reference remains");
                Check.Same(c.Original, magic.weaponEnchants.Single(), "actual enchantment target remains");
                RuntimeLog.AssertClean();
            }
            finally
            {
                TorannMagicDefOf.TM_Sunlight = oldSun; TorannMagicDefOf.TM_EnchantWeapon = oldEnchant;
                enchantDefinition.manaCost = oldEnchantCost; enchantDefinition.efficiencyReductionPercent = oldEnchantEfficiency;
                StatDefOf.PsychicSensitivity = oldPsychic;
                DefDatabase<HediffDef>.Clear(); foreach (var def in hediffs) DefDatabase<HediffDef>.Add(def);
            }
        }));
        yield return new("SpiritRuntime.NativeManaDifferentialAndCadence", r => SpiritRebirthEnergyTests.Run(r, (c, energy) =>
        {
            var pawn = c.Creator; var magic = pawn.GetComp<CompAbilityUserMagic>(); var data = magic.MagicData;
            pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
            pawn.apparel ??= new Pawn_ApparelTracker(pawn);
            c.Map.gameConditionManager = new GameConditionManager(c.Map);
            Set(ClassIdentity.Component(pawn), "state", new ClassProgressionState(Vocation.Warrior));
            var mana = new Need_Mana(pawn) { def = new NeedDef { defName = "SpiritRuntimeMana", needClass = typeof(Need_Mana) } };
            pawn.needs.AllNeeds.Add(mana);
            Set(mana, "lastParacyteCheck", int.MaxValue);
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            try
            {
                foreach (var name in new[] { "TM_SyrriumSenseHD", "TM_PomanaSenseHD" })
                    if (DefDatabase<HediffDef>.GetNamedSilentFail(name) == null) DefDatabase<HediffDef>.Add(new HediffDef { defName = name });
                magic.summonedMinions = Enumerable.Range(0, 8).Select(_ => (Thing)PawnWithComps(false, false).pawn).ToList();
                var owners = magic.summonedMinions.ToArray();
                magic.mpRegenRate = 1f; magic.maxMP = 1f;
                var marker = ClassIdentity.Component(pawn).SpiritRebirth;
                Set(ClassIdentity.Component(pawn), "spiritRebirth", null!);
                Set(ClassIdentity.Component(pawn), "state", new ClassProgressionState(Vocation.Mage));
                mana.CurLevel = .5f; mana.GainNeed(1f); float ordinary = mana.CurLevel;
                Set(ClassIdentity.Component(pawn), "spiritRebirth", marker);
                Set(ClassIdentity.Component(pawn), "state", new ClassProgressionState(Vocation.Warrior));
                data.magicPowerSkill_global_regen = null; data.magicPowerSkill_SummonMinion = null;
                mana.CurLevel = .5f; mana.GainNeed(1f);
                Check.Equal(ordinary, mana.CurLevel, "same saved owner ordinary minion upkeep arithmetic retained");
                Check.Equal(true, data.magicPowerSkill_global_regen == null && data.magicPowerSkill_SummonMinion == null,
                    "marked economy reads zero defaults without initializing former skill lists");
                Check.Equal(true, owners.Zip(magic.summonedMinions).All(pair => ReferenceEquals(pair.First, pair.Second)), "existing minion references preserved");
                mana.CurLevel = mana.MaxLevel; mana.GainNeed(1f);
                Check.Equal(true, mana.CurLevel < mana.MaxLevel, "negative upkeep is charged even at full Mana");
                Set(Find.TickManager, "ticksGameInt", 10); mana.NeedInterval(); float once = mana.CurLevel;
                mana.NeedInterval(); Set(Find.TickManager, "ticksGameInt", 30); mana.NeedInterval();
                Check.Equal(once, mana.CurLevel, "distinct native/hash phases cannot double charge an interval");
                Set(Find.TickManager, "ticksGameInt", 160); mana.NeedInterval();
                Check.Equal(true, mana.CurLevel < once, "later observed native interval is not starved");
                RuntimeLog.AssertClean();
            }
            finally { DefDatabase<HediffDef>.Clear(); foreach (var def in hediffs) DefDatabase<HediffDef>.Add(def); }
        }));
        yield return new("SpiritRuntime.NativeDispatchWarriorTimersAndDedup", r => SpiritRebirthEnergyTests.Run(r, (c, need) =>
        {
            using var uiPause = new UiPauseScope();
            Find.World.UI = (RimWorld.WorldInterface)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.WorldInterface));
            Find.World.UI.tilePicker = new RimWorld.TilePicker();
            Find.World.components = [];
            var magic = c.Creator.GetComp<CompAbilityUserMagic>(); var might = c.Creator.GetComp<CompAbilityUserMight>();
            Set(ClassIdentity.Component(c.Creator), "state", new ClassProgressionState(Vocation.Warrior));
            SpiritPassive.Calls = FormerPassive.Calls = 0;
            var spiritDef = r.AddAbility("SpiritRuntimeCanonical"); spiritDef.PassiveProps = new PassiveEffectProperties { worker = typeof(SpiritPassive) };
            spiritDef.MainVerb.verbClass = typeof(Verb_UseAbility);
            var formerDef = r.AddAbility("SpiritRuntimeFormer"); formerDef.PassiveProps = new PassiveEffectProperties { worker = typeof(FormerPassive) };
            DefDatabase<TM_CustomClassDef>.Clear(); DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [new TM_CustomClass {
                classTrait = TorannMagicDefOf.TM_Possessor, isMage = true, classMageAbilities = [spiritDef] }] });
            var spirit = new MagicAbility(magic) { Def = spiritDef, CooldownTicksLeft = 11 };
            var former = new MagicAbility(magic) { Def = formerDef, CooldownTicksLeft = 13 };
            var fighter = new MightAbility(might) { Def = formerDef, CooldownTicksLeft = 17 };
            magic.AbilityData.Powers.AddRange([spirit, former]); might.AbilityData.Powers.Add(fighter);
            magic.AbilityData.AllPowers = null; might.AbilityData.AllPowers = null;
            Set(Find.TickManager, "ticksGameInt", 1);
            magic.CompTick();
            Check.Equal(1, SpiritPassive.Calls, "canonical Spirit runs actual native PawnAbility.Tick for former Warrior");
            might.CompTick();
            Check.Equal(0, FormerPassive.Calls, "former Magic and Might native passive callbacks suppressed");
            Check.Equal(10, spirit.CooldownTicksLeft, "Spirit cooldown native decrement");
            Check.Equal(12, former.CooldownTicksLeft, "former Magic retains timer progress");
            Check.Equal(16, fighter.CooldownTicksLeft, "former Might retains timer progress");
            magic.CompTick(); might.CompTick();
            Check.Equal(1, SpiritPassive.Calls, "same observed tick cannot execute twice");
            Check.Equal(12, former.CooldownTicksLeft, "same observed tick cannot decrement twice");
            Set(Find.TickManager, "ticksGameInt", 2);
            Set(Find.TickManager, "curTimeSpeed", TimeSpeed.Paused);
            magic.CompTick(); might.CompTick();
            Check.Equal(12, former.CooldownTicksLeft, "native paused speed prevents timer progress");
            Check.Equal(false, magic.IsMagicUser, "saved Warrior vocation is not enrolled into Mage");
            Set(Find.TickManager, "curTimeSpeed", TimeSpeed.Normal);
            need.GainNeed(-100f);
            var stone = c.Map.spawnedThings.OfType<Thing_SoulStone>().Single();
            stone.def.tickerType = TickerType.Normal;
            Check.Equal(false, ((IThingHolderTickable)stone).ShouldTickContents, "native holder must not tick original body");
            stone.DoTick();
            Check.Equal(11, former.CooldownTicksLeft, "native map stone tick advances original timer");
            Check.Equal(1, SpiritPassive.Calls, "held canonical abilities do not execute passive worker");
            stone.DeSpawn();
            var inventory = new Pawn_InventoryTracker(c.Original);
            Check.Equal(true, inventory.innerContainer.TryAdd(stone, false), "actual carrier inventory accepts same occupied stone");
            inventory.innerContainer.DoTick();
            Check.Equal(11, former.CooldownTicksLeft, "map to inventory transfer does not double tick");
            Set(Find.TickManager, "ticksGameInt", 3);
            inventory.innerContainer.DoTick();
            Check.Equal(10, former.CooldownTicksLeft, "native inventory holder recursion advances narrow original clock");
            Check.Same(stone, c.Creator.holdingOwner.Owner, "narrow runtime preserves original native custody");
            var host = PawnWithComps(true, true).pawn;
            var hostMagic = host.GetComp<CompAbilityUserMagic>().MagicData;
            var oldPossession = TorannMagicDefOf.TM_SpiritPossessionHD;
            try
            {
                TorannMagicDefOf.TM_SpiritPossessionHD = new HediffDef { defName = "SpiritRuntimePossession" };
                var hediff = new HediffWithComps { def = TorannMagicDefOf.TM_SpiritPossessionHD, pawn = host };
                var possession = new HediffComp_SpiritPossession { parent = hediff };
                hediff.comps = [possession]; host.health.hediffSet.hediffs.Add(hediff);
                Set(possession, "innerContainer", new ThingOwner<Thing>(possession));
                Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryImplant", c.Creator, possession), "actual original enters actual empty host holder");
                float severity = 0; Set(Find.TickManager, "ticksGameInt", 4); possession.CompPostTick(ref severity);
                Check.Equal(9, former.CooldownTicksLeft, "native possession component drives original narrow clock");
                possession.CompPostTick(ref severity);
                Check.Equal(9, former.CooldownTicksLeft, "duplicate host observation does not tick original twice");
                Check.Same(hostMagic, host.GetComp<CompAbilityUserMagic>().MagicData, "host data is unchanged");
                Check.Equal(1, SpiritPassive.Calls, "host does not activate Spirit original passive worker");
                Check.Equal(0f, severity, "new path does not adjust host severity");
            }
            finally { TorannMagicDefOf.TM_SpiritPossessionHD = oldPossession; }
            RuntimeLog.AssertClean();
        }));
    }
    // Only unrelated UI/gravship freeze is unavailable headlessly; native Paused and all ability ticks remain real.
    static bool NoUiPause(ref bool __result) { __result = false; return false; }
    sealed class UiPauseScope : IDisposable
    {
        readonly object harmony; readonly string id = "spirit-runtime-ui-" + Guid.NewGuid();
        public UiPauseScope()
        {
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var prefix = typeof(SpiritRebirthRuntimeTests).GetMethod(nameof(NoUiPause), BindingFlags.Static | BindingFlags.NonPublic)!;
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, prefix)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(TickManager).GetProperty(nameof(TickManager.ForcePaused))!.GetMethod, method, null, null, null]);
            var cameraPrefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthRuntimeTests).GetMethod(nameof(CameraRate), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(GenTicks).GetMethod(nameof(GenTicks.GetCameraUpdateRate))!, cameraPrefix, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static bool CameraRate(ref int __result) { __result = 1; return false; }
    static void ObserveJobs(Pawn ___pawn)
    {
        if (ReferenceEquals(___pawn, PhysicalTickProbe.Pending)) PhysicalTickProbe.PendingJobs++;
        if (ReferenceEquals(___pawn, PhysicalTickProbe.Creator)) PhysicalTickProbe.CreatorJobs++;
    }
    static void ObserveMind(Pawn ___pawn)
    {
        if (ReferenceEquals(___pawn, PhysicalTickProbe.Pending)) PhysicalTickProbe.PendingMind++;
    }
    sealed class PhysicalTickProbe : IDisposable
    {
        internal static Pawn? Pending, Creator;
        internal static int PendingJobs, PendingMind, CreatorJobs;
        readonly object harmony; readonly string id = "spirit-pending-observer-" + Guid.NewGuid();
        public PhysicalTickProbe(Pawn pending, Pawn creator)
        {
            Pending = pending; Creator = creator; PendingJobs = PendingMind = CreatorJobs = 0;
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            foreach (var (target, prefix) in new[] {
                (typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.JobTrackerTick))!, nameof(ObserveJobs)),
                (typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.JobTrackerTickInterval))!, nameof(ObserveJobs)),
                (typeof(Pawn_MindState).GetMethod(nameof(Pawn_MindState.MindStateTickInterval))!, nameof(ObserveMind)) })
            {
                var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthRuntimeTests).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)!)!;
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, method, null, null, null]);
            }
        }
        public void Dispose() { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]); Pending = Creator = null; }
    }
}
