using System.Runtime.CompilerServices;
using System.Reflection;
using RimWorld;
using System.Xml.Linq;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static class SharedClassScrollTests
{
    public sealed class ObservedScroll : ThingWithComps
    {
        public static Action<ObservedScroll>? anyBeforeDestroy;
        public Action? beforeDestroy;
        public Action? afterDestroy;
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            anyBeforeDestroy?.Invoke(this);
            beforeDestroy?.Invoke();
            base.Destroy(mode);
            afterDestroy?.Invoke();
        }
    }

    static bool SkipMessage() => false;
    sealed class MessageScope : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.shared-scroll-test." + Guid.NewGuid();
        public MessageScope()
        {
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SharedClassScrollTests).GetMethod(nameof(SkipMessage), BindingFlags.NonPublic | BindingFlags.Static)!)!;
            var original = typeof(Messages).GetMethods().Single(m => m.Name == "Message" && m.GetParameters() is var p && p.Length == 3 && p[1].ParameterType == typeof(MessageTypeDef) && p[2].ParameterType == typeof(bool));
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [original, hook, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }

    static ThingDef Scroll(string name)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = name;
        return def;
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SharedClassScroll.PaidAccessDoesNotUnlockClassButtons", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var savedProfiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var oldItems = new Dictionary<TMAbilityDef, ThingDef>();
            try
            {
                DefDatabase<ClassDisciplineDef>.Clear();
                foreach (string enrolled in new[] { "rwom.fire", "rwom.priest", "rwom.ice" })
                {
                    var p = GrimoireProgressionTests.PawnWithComps(true, false);
                    var c = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(c);
                    c.BeginNewProgression(Vocation.Mage); c.State.TryAddFoundation(enrolled, DisciplineDomain.Magic);
                    var data = p.magic.MagicData;
                    var rows = new[] {
                        ("rwom.ice", "SpellOf_Rain", TorannMagicDefOf.TM_Rainmaker, data.MagicPowersHoF),
                        ("rwom.arcane", "SpellOf_Blink", TorannMagicDefOf.TM_Blink, data.MagicPowersA),
                        ("rwom.arcane", "SpellOf_Teleport", TorannMagicDefOf.TM_Teleport, data.MagicPowersA),
                        ("rwom.paladin", "SpellOf_Heal", TorannMagicDefOf.TM_Heal, data.MagicPowersP),
                        ("rwom.summoner", "SpellOf_SummonMinion", TorannMagicDefOf.TM_SummonMinion, data.MagicPowersS) };
                    foreach (var (profileId, itemName, ability, owners) in rows)
                    {
                        if (!oldItems.ContainsKey(ability)) oldItems.Add(ability, ability.learnItem);
                        var item = Scroll(itemName); ability.learnItem = item;
                        DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "Shared_" + enrolled + itemName,
                            profileId = profileId, domain = DisciplineDomain.Magic,
                            spells = [new DisciplineSpell { ability = ability, stage = InitiationStage.Adept }] });
                        var owner = owners.Single(power => power.TMabilityDefs.Contains(ability));
                        owner.learned = false; owner.autocast = true; owner.ticksUntilNextCast = 823;
                        int count = owners.Count;
                        Check.Equal(false, ClassSpellAccess.CanLearn(p.pawn, owner), "class button remains gated: " + enrolled + "/" + itemName);
                        Check.Equal(true, ClassSpellAccess.CanLearnFromScroll(p.pawn, owner, item), "paid shared item has explicit stage permission");
                        Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(p.pawn, owner, Scroll("SpellOf_Firestorm")), "mismatched item cannot grant shared access");
                        Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(p.pawn, owner, Scroll(itemName)), "same name without exact learnItem identity is insufficient");
                        var fake = new MagicPower { TMabilityDefs = owner.TMabilityDefs.ToList(), learned = true };
                        Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(p.pawn, fake, item), "detached copied owner is never the paid-scroll recipient");
                        Check.Equal(false, owner.learned, "permission does not grant spell");
                        Check.Equal(true, owner.autocast, "permission preserves autocast investment");
                        Check.Equal(823, owner.ticksUntilNextCast, "permission preserves cooldown investment");
                        Check.Equal(count, owners.Count, "no new owner is created");
                        Check.Same(owner, owners.Single(power => power.TMabilityDefs.Contains(ability)), "native owner identity retained");
                        owner.learned = true;
                        Check.Equal(true, ClassSpellAccess.CanLearn(p.pawn, owner), "existing learned owner remains grandfathered");
                        Check.Equal(true, ClassSpellAccess.CanLearnFromScroll(p.pawn, owner, item), "permission itself does not erase existing grant");
                    }
                    DefDatabase<ClassDisciplineDef>.Clear();
                }
            }
            finally
            {
                foreach (var pair in oldItems) pair.Key.learnItem = pair.Value;
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in savedProfiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            }
        });
        yield return new("SharedClassScroll.RankedFamilyAndUltimateBoundary", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var p = GrimoireProgressionTests.PawnWithComps(true, false);
            var c = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(c);
            c.BeginNewProgression(Vocation.Mage); c.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
            var blink = p.magic.MagicData.MagicPowersA.Single(power => power.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink));
            var saved = blink.TMabilityDefs.OfType<TMAbilityDef>().ToDictionary(a => a, a => a.learnItem);
            var firestorm = p.magic.MagicData.MagicPowersIF.Single(power => power.TMabilityDefs.Contains(TorannMagicDefOf.TM_Firestorm));
            var oldFireItem = TorannMagicDefOf.TM_Firestorm.learnItem;
            var oldProfiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            try
            {
                var item = Scroll("SpellOf_Blink");
                foreach (var ability in blink.TMabilityDefs.OfType<TMAbilityDef>()) ability.learnItem = null!;
                TorannMagicDefOf.TM_Blink.learnItem = item;
                blink.level = blink.TMabilityDefs.Count - 1; blink.learned = false;
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "SharedRankedArcane", profileId = "rwom.arcane", domain = DisciplineDomain.Magic,
                    spells = [new DisciplineSpell { ability = TorannMagicDefOf.TM_Blink, stage = InitiationStage.Adept }] });
                Check.Equal(true, ClassSpellAccess.CanLearnFromScroll(p.pawn, blink, item), "base learnItem identifies an invested ranked family");
                Check.Equal(blink.TMabilityDefs.Count - 1, blink.level, "access never resets saved rank");
                Check.Equal(false, ClassSpellAccess.CanLearn(p.pawn, blink), "rank does not unlock foreign class button");
                TorannMagicDefOf.TM_Firestorm.learnItem = Scroll("SpellOf_Firestorm"); firestorm.learned = false;
                Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(p.pawn, firestorm, TorannMagicDefOf.TM_Firestorm.learnItem), "Firestorm has no shared-scroll ultimate exception");
                Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(p.pawn, blink, Scroll("SpellOf_AlterStorm")), "AlterStorm is not Rainmaker or Blink");
            }
            finally
            {
                foreach (var pair in saved) pair.Key.learnItem = pair.Value;
                TorannMagicDefOf.TM_Firestorm.learnItem = oldFireItem;
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in oldProfiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            }
        });
        yield return new("SharedClassScroll.NativeEffectPaysAndUsesExistingOwner", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var oldGame = Current.Game;
            var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var ability = TorannMagicDefOf.TM_Rainmaker;
            var oldItem = ability.learnItem; var oldClass = ability.abilityClass; bool oldInitialize = ability.shouldInitialize;
            try
            {
                Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                GrimoireProgressionTests.Set(Current.Game, "maps", new List<Map>());
                GrimoireProgressionTests.Set(Current.Game, "tickManager", new TickManager());
                GrimoireProgressionTests.Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
                var p = GrimoireProgressionTests.PawnWithComps(true, false);
                var c = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(c);
                c.BeginNewProgression(Vocation.Mage); c.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
                p.pawn.needs.AllNeeds.Add((Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)));
                var item = Scroll("SpellOf_Rain"); item.thingClass = typeof(ThingWithComps); item.stackLimit = 25; item.destroyable = true; item.comps = []; item.category = ThingCategory.Item;
                ability.learnItem = item; ability.abilityClass = typeof(AbilityUser.PawnAbility); ability.shouldInitialize = true;
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "SharedPaymentIce", profileId = "rwom.ice", domain = DisciplineDomain.Magic,
                    spells = [new DisciplineSpell { ability = ability, stage = InitiationStage.Adept }] });
                var owner = p.magic.MagicData.MagicPowersHoF.Single(power => power.TMabilityDefs.Contains(ability));
                owner.learned = false; owner.ticksUntilNextCast = 923; owner.autocast = true;
                int ownerCount = p.magic.MagicData.MagicPowersHoF.Count;
                var scroll = new ThingWithComps { def = item, stackCount = 2 };
                new CompUseEffect_LearnSpell { parent = scroll }.DoEffect(p.pawn);
                Check.Equal(true, owner.learned, "actual item effect grants existing owner despite foreign profile gate");
                Check.Equal(1, scroll.stackCount, "actual native stack payment consumes exactly one scroll");
                new CompUseEffect_LearnSpell { parent = scroll }.DoEffect(p.pawn);
                Check.Equal(1, scroll.stackCount, "repeated completion with learned owner pays nothing");
                Check.Equal(true, p.magic.spell_Rain, "native saved shared ownership flag retained");
                Check.Equal(ownerCount, p.magic.MagicData.MagicPowersHoF.Count, "payment does not create a second owner");
                Check.Equal(923, owner.ticksUntilNextCast, "native grant does not reset existing cooldown investment");
                Check.Equal(true, owner.autocast, "native grant does not reset autocast investment");
                Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == ability), "one runtime Rainmaker after restore");
                // A real native split must regain a real holder if destruction fails.
                owner.learned = false; p.magic.spell_Rain = false;
                p.pawn.inventory = new Pawn_InventoryTracker(p.pawn);
                item.thingClass = typeof(ObservedScroll);
                var splitSource = new ObservedScroll { def = item, stackCount = 2 };
                p.pawn.inventory.innerContainer.TryAdd(splitSource, false);
                ObservedScroll? splitPayment = null;
                bool splitFailed = false;
                try
                {
                    ObservedScroll.anyBeforeDestroy = payment => {
                        splitPayment = payment;
                        throw new InvalidOperationException("split-payment-before-destroy");
                    };
                    try { new CompUseEffect_LearnSpell { parent = splitSource }.DoEffect(p.pawn); }
                    catch (InvalidOperationException ex) when (ex.Message == "split-payment-before-destroy") { splitFailed = true; }
                }
                finally { ObservedScroll.anyBeforeDestroy = null; item.thingClass = typeof(ThingWithComps); }
                Check.Equal(true, splitFailed, "real split-unit destruction failure was observed");
                Check.Equal(false, ReferenceEquals(splitSource, splitPayment), "payment was a real split unit, not whole source");
                Check.Equal(1, splitSource.stackCount, "original source retains its unspent unit");
                Check.Equal(2, p.pawn.inventory.innerContainer.Sum(t => t.stackCount), "both physical scroll units remain saved in inventory");
                Check.Same(p.pawn.inventory.innerContainer, splitPayment!.holdingOwner, "exact split identity regained original holder");
                Check.Equal(false, owner.learned, "failed split payment cannot grant owner");
                Check.Equal(false, p.magic.spell_Rain, "failed split payment cannot grant saved flag");
                // Native destruction callbacks observe no free grant, and reentry cannot pay twice.
                owner.learned = false; p.magic.spell_Rain = false;
                var observed = new ObservedScroll { def = item, stackCount = 1 };
                var effect = new CompUseEffect_LearnSpell { parent = observed };
                observed.beforeDestroy = () => throw new InvalidOperationException("payment-before-destroy");
                bool rejected = false;
                try { effect.DoEffect(p.pawn); }
                catch (InvalidOperationException ex) when (ex.Message == "payment-before-destroy") { rejected = true; }
                Check.Equal(true, rejected, "actual failed payment callback was observed");
                Check.Equal(false, owner.learned, "failed destruction cannot grant owner");
                Check.Equal(false, p.magic.spell_Rain, "failed destruction cannot grant flag");
                Check.Equal(false, observed.Destroyed, "failed payment leaves item unconsumed");
                int callbacks = 0;
                observed.beforeDestroy = () => {
                    callbacks++;
                    Check.Equal(false, owner.learned, "learned flag cannot precede real payment");
                    Check.Equal(false, p.magic.spell_Rain, "saved flag cannot precede real payment");
                    effect.DoEffect(p.pawn);
                    Check.Equal(false, owner.learned, "nested completion cannot grant before outer payment");
                };
                observed.afterDestroy = () => throw new InvalidOperationException("payment-after-destroy");
                effect.DoEffect(p.pawn);
                Check.Equal(1, callbacks, "same native owner rejects reentrant payment");
                Check.Equal(true, observed.Destroyed, "callback after destruction committed real payment");
                Check.Equal(true, owner.learned, "post-destruction exception cannot erase purchased grant");
                Check.Equal(true, p.magic.spell_Rain, "saved shared flag follows confirmed payment");
            }
            finally
            {
                ability.learnItem = oldItem; ability.abilityClass = oldClass; ability.shouldInitialize = oldInitialize;
                Current.Game = oldGame;
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            }
        });
        yield return new("SharedClassScroll.NativeRankedPaymentAndTraitRestriction", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(MessageTypeDefOf));
            using var language = new FleshStatTests.LanguageScope(f.Repository);
            LanguageDatabase.activeLanguage.keyedReplacements["CannotLearnSpell"] = new LoadedLanguage.KeyedReplacement { key = "CannotLearnSpell", value = "Cannot learn spell" };
            var oldGame = Current.Game;
            var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var p = GrimoireProgressionTests.PawnWithComps(true, false);
            var owner = p.magic.MagicData.MagicPowersA.Single(power => power.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink));
            var saved = owner.TMabilityDefs.OfType<TMAbilityDef>().ToDictionary(a => a, a => (a.learnItem, a.abilityClass, a.shouldInitialize));
            try
            {
                Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                GrimoireProgressionTests.Set(Current.Game, "maps", new List<Map>());
                GrimoireProgressionTests.Set(Current.Game, "tickManager", new TickManager());
                GrimoireProgressionTests.Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
                var c = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(c);
                c.BeginNewProgression(Vocation.Mage); c.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
                p.pawn.needs.AllNeeds.Add((Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)));
                var item = Scroll("SpellOf_Blink"); item.thingClass = typeof(ThingWithComps); item.stackLimit = 25;
                item.destroyable = true; item.comps = []; item.category = ThingCategory.Item;
                foreach (var a in saved.Keys) { a.learnItem = null!; a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
                TorannMagicDefOf.TM_Blink.learnItem = item;
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "SharedPaymentArcane", profileId = "rwom.arcane", domain = DisciplineDomain.Magic,
                    spells = [new DisciplineSpell { ability = TorannMagicDefOf.TM_Blink, stage = InitiationStage.Adept }] });
                owner.learned = false; owner.level = owner.TMabilityDefs.Count - 1; owner.ticksUntilNextCast = 456; owner.autocast = true;
                var scroll = new ThingWithComps { def = item, stackCount = 2 };
                var learner = new CompUseEffect_LearnSpell { parent = scroll };
                // The real item restriction remains outside the new enrolled completion path.
                item.modExtensions = [new DefModExtension_LearnAbilityRequiredTraits { traits = [TorannMagicDefOf.HeartOfFrost] }];
                using (var messages = new MessageScope()) learner.DoEffect(p.pawn);
                Check.Equal(false, owner.learned, "missing native required trait rejects paid exception");
                Check.Equal(2, scroll.stackCount, "restriction rejection pays nothing");
                item.modExtensions.Clear();
                learner.DoEffect(p.pawn);
                Check.Equal(true, owner.learned, "ranked native owner learned without lookup by base active rank");
                Check.Equal(1, scroll.stackCount, "one stack item consumed on completion");
                Check.Equal(owner.TMabilityDefs.Count - 1, owner.level, "saved rank retained");
                Check.Equal(456, owner.ticksUntilNextCast, "saved cooldown retained");
                Check.Equal(true, owner.autocast, "saved autocast retained");
                Check.Equal(1, p.magic.AbilityData.Powers.Count(a => owner.TMabilityDefs.Contains(a.Def)), "one runtime family member");
                Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == owner.abilityDef), "saved current rank is the runtime ability");
                Check.Equal(0, p.magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Blink), "base rank not added alongside saved rank");
                p.magic.spell_Blink = false; // A legacy flag mismatch cannot pay again or reset an invested owner.
                learner.DoEffect(p.pawn);
                Check.Equal(1, scroll.stackCount, "learned owner with missing flag still cannot spend another scroll");
                Check.Equal(false, p.magic.spell_Blink, "already learned completion is a true no-op");
                Check.Equal(1, p.magic.AbilityData.Powers.Count(a => owner.TMabilityDefs.Contains(a.Def)), "repeat adds no runtime duplicate");
                owner.learned = false; p.magic.spell_Blink = true;
                learner.DoEffect(p.pawn);
                Check.Equal(true, owner.learned, "legacy flag-only ownership repairs native owner");
                Check.Equal(1, scroll.stackCount, "flag-only owner repair does not charge again");
                Check.Equal(1, p.magic.AbilityData.Powers.Count(a => owner.TMabilityDefs.Contains(a.Def)), "repair retains one saved rank");
                var classic = GrimoireProgressionTests.PawnWithComps(true, false);
                var boundary = typeof(CompUseEffect_LearnSpell).GetMethod("TryCompleteEnrolledSharedScroll", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Check.Equal(false, (bool)boundary.Invoke(learner, new object[] { classic.pawn, classic.magic })!, "classic owner never routes through enrolled completion");
            }
            finally
            {
                foreach (var pair in saved) { pair.Key.learnItem = pair.Value.learnItem; pair.Key.abilityClass = pair.Value.abilityClass; pair.Key.shouldInitialize = pair.Value.shouldInitialize; }
                Current.Game = oldGame;
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            }
        });
        yield return new("SharedClassScroll.ArcanistPaidMovementAndFlagOwners", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var oldGame = Current.Game;
            var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var defs = new[] { TorannMagicDefOf.TM_Blink, TorannMagicDefOf.TM_Teleport };
            var saved = defs.ToDictionary(a => a, a => (a.learnItem, a.abilityClass, a.shouldInitialize));
            try
            {
                Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                GrimoireProgressionTests.Set(Current.Game, "maps", new List<Map>());
                GrimoireProgressionTests.Set(Current.Game, "tickManager", new TickManager());
                GrimoireProgressionTests.Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
                DefDatabase<ClassDisciplineDef>.Clear();
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "SharedArcanist", profileId = "rwom.arcanist",
                    domain = DisciplineDomain.Magic, magicPowerList = "magicPowerA", spells = [
                        new DisciplineSpell { ability = TorannMagicDefOf.TM_Blink, stage = InitiationStage.Foundation },
                        new DisciplineSpell { ability = TorannMagicDefOf.TM_Teleport, stage = InitiationStage.Master }] });
                foreach (bool enrolled in new[] { false, true })
                foreach (var ability in defs)
                {
                    var p = GrimoireProgressionTests.PawnWithComps(true, false);
                    var c = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(c);
                    c.BeginNewProgression(Vocation.Mage);
                    if (enrolled) c.State.TryAddFoundation("rwom.arcanist", DisciplineDomain.Magic);
                    p.pawn.needs.AllNeeds.Add((Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)));
                    bool blink = ability == TorannMagicDefOf.TM_Blink;
                    var item = Scroll(blink ? "SpellOf_Blink" : "SpellOf_Teleport");
                    item.thingClass = typeof(ThingWithComps); item.stackLimit = 25; item.destroyable = true; item.comps = []; item.category = ThingCategory.Item;
                    ability.learnItem = item; ability.abilityClass = typeof(AbilityUser.PawnAbility); ability.shouldInitialize = true;
                    var owners = p.magic.MagicData.MagicPowersA;
                    var owner = owners.Single(power => power.TMabilityDefs.Contains(ability));
                    owner.learned = false; owner.ticksUntilNextCast = 719; owner.autocast = true;
                    Check.Equal(enrolled && blink, ClassSpellAccess.CanLearn(p.pawn, owner), "native class gate before paid scroll");
                    var scroll = new ThingWithComps { def = item, stackCount = 2 };
                    var effect = new CompUseEffect_LearnSpell { parent = scroll };
                    Check.Equal(false, owner.learned, "no free movement owner before completed reading");
                    effect.DoEffect(p.pawn);
                    Check.Equal(1, scroll.stackCount, "actual Blink/Teleport reading pays one scroll");
                    Check.Equal(true, owner.learned, "paid movement owner committed");
                    Check.Equal(true, blink ? p.magic.spell_Blink : p.magic.spell_Teleport, "native movement flag committed");
                    Check.Equal(719, owner.ticksUntilNextCast, "movement payment preserves cooldown");
                    Check.Equal(true, owner.autocast, "movement payment preserves autocast");
                    Check.Same(owner, owners.Single(power => power.TMabilityDefs.Contains(ability)), "native A owner retained");
                    if (!enrolled) c.State.TryAddFoundation("rwom.arcanist", DisciplineDomain.Magic);
                    ClassSpellAccess.RestoreLearned(p.magic);
                    Check.Equal(true, ClassSpellAccess.CanLearn(p.pawn, owner), "purchased movement survives foundation stage gate");
                    effect.DoEffect(p.pawn);
                    Check.Equal(1, scroll.stackCount, "repeated movement completion cannot pay again");
                    if (blink) p.magic.spell_Blink = false; else p.magic.spell_Teleport = false;
                    effect.DoEffect(p.pawn);
                    Check.Equal(1, scroll.stackCount, "learned owner with missing flag cannot pay again");
                    owner.learned = false;
                    if (blink) p.magic.spell_Blink = true; else p.magic.spell_Teleport = true;
                    effect.DoEffect(p.pawn);
                    Check.Equal(true, owner.learned, "old movement flag repairs exact owner");
                    Check.Equal(1, scroll.stackCount, "old movement flag repair is not charged again");
                    Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == ability), "movement restoration does not duplicate runtime");
                    Check.Equal(InitiationStage.Foundation, c.State.Find("rwom.arcanist").Stage, "paid Teleport does not advance class");
                }
            }
            finally
            {
                foreach (var pair in saved) { pair.Key.learnItem = pair.Value.learnItem; pair.Key.abilityClass = pair.Value.abilityClass; pair.Key.shouldInitialize = pair.Value.shouldInitialize; }
                Current.Game = oldGame;
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            }
        });
        yield return new("SharedClassScroll.NativeXmlSeparatesRainAndAlterStorm", f =>
        {
            var ice = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_IceMage.xml"));
            var rain = ice.Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_Rainmaker");
            Check.Equal("SpellOf_Rain", rain.Element("learnItem")!.Value, "Rainmaker uses actual shared rain scroll");
            var standalone = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Standalone.xml"));
            var storm = standalone.Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_AlterStorm");
            Check.Equal("SpellOf_AlterStorm", storm.Element("learnItem")!.Value, "AlterStorm is a separate ability and item");
            var items = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/ThingDefs/LearnAbility_Item.xml"));
            var restricted = items.Root!.Elements().Single(e => (string?)e.Element("defName") == "SpellOf_AlterStorm");
            Check.Equal(true, restricted.Descendants("traits").Elements().Any(e => e.Value == "HeartOfFrost"), "separate Ice trait restriction remains authored");
        });
    }
}





