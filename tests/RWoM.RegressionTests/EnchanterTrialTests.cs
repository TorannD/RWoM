using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class EnchanterTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EnchanterTrial.RawPaymentPlacementAndDifferentOutput", Raw);
        yield return new("EnchanterTrial.RawNestedScopesOwnershipAndDrift", Scopes);
        yield return new("EnchanterTrial.RawNoopPartialPaymentRejected", NoopPayment);
        yield return new("EnchanterTrial.EnchantmentProvenanceAndRecipients", Enchantment);
        yield return new("EnchanterTrial.NativeEnchantmentMutation", NativeEnchantment);
        yield return new("EnchanterTrial.EnchantmentOwnershipAndEquipmentDrift", EnchantmentDrift);
        yield return new("EnchanterTrial.StaleDispelRetainsForeignEffectAndExactWeapon", r => {
            using var w = new Fixture(r);
            var ward = w.Ward();
            var component = ward.TryGetComp<HediffComp_EnchantedWeapon>();
            component.enchanterPawn = w.Target;
            w.Magic.weaponEnchants.AddRange([w.Target, null!, w.Target]);
            var dispel = typeof(Verb_DispelEnchantWeapon).GetMethod("DispelOwned",BindingFlags.NonPublic|BindingFlags.Static)!;
            Check.Equal(true,(bool)dispel.Invoke(null,[w.Magic])!, "stale entries drained without relying on callbacks");
            Check.Equal(true,w.Target.health.hediffSet.hediffs.Contains(ward) && w.Magic.weaponEnchants.Count==0,"other caster's enchantment untouched");
            Check.Equal(false,(bool)dispel.Invoke(null,[w.Magic])!, "repeat has nothing to remove");
            var hit = typeof(HediffComp_EnchantedWeapon).GetMethod("ActiveOnHit",BindingFlags.NonPublic|BindingFlags.Static)!;
            Check.Same(ward,hit.Invoke(null,[w.Target,ward.def,w.Weapon.def]),"equipped weapon receives the effect");
            Check.Equal(true,hit.Invoke(null,[w.Target,ward.def,w.Output])==null,"unrelated damage cannot trigger the enchantment");
            w.Target.equipment.GetDirectlyHeldThings().Remove(w.Weapon);
            Check.Equal(true,hit.Invoke(null,[w.Target,ward.def,w.Weapon.def])==null,"dropped weapon immediately loses the on-hit effect");
        });
    }
    sealed class Fixture : IDisposable
    {
        readonly Type? priorDispelType;
        readonly Game previous = Current.Game;
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] saved = { TorannMagicDefOf.TM_WeaponEnchantment_FireHD, TorannMagicDefOf.TM_WeaponEnchantment_IceHD, TorannMagicDefOf.TM_WeaponEnchantment_LitHD, TorannMagicDefOf.TM_WeaponEnchantment_DarkHD };
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public Thing Source;
        public ThingWithComps Weapon;
        public ThingDef Output;
        public HediffDef[] Enchants;
        public DisciplineProgress Path => Progression.State.Find("rwom.enchanter");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            priorDispelType = TorannMagicDefOf.TM_DispelEnchantWeapon.abilityClass;
            TorannMagicDefOf.TM_DispelEnchantWeapon.abilityClass = typeof(PawnAbility);
            if (DefDatabase<DesignationDef>.DefCount == 0) DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "EnchanterTrialDesignation" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) { map.events = new MapEvents(map); map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map); map.info = new MapInfo { Size = new IntVec3(10, 1, 10) }; map.haulDestinationManager = new HaulDestinationManager(map); map.reservationManager = new Verse.AI.ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager(); }
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World)); world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction);
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.guest = new Pawn_GuestTracker(pawn); pawn.kindDef = new PawnKindDef { defName = "EnchanterTrialPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.enchanter", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "EnchanterTrialProfile", profileId = "rwom.enchanter", domain = DisciplineDomain.Magic });
            Source = new Thing { def = Resource("TrialSource"), stackCount = 100, Position = new IntVec3(3, 0, 3) }; Set(Source, "mapIndexOrState", (sbyte)0);
            Output = Resource("TrialOutput");
            var weaponDef = Resource("TrialMelee"); weaponDef.equipmentType = EquipmentType.Primary; weaponDef.tools = new List<Tool> { new Tool() };
            Weapon = new ThingWithComps { def = weaponDef };
            var equippable = new CompEquippable { parent = Weapon };
            equippable.Initialize(new CompProperties { compClass = typeof(CompEquippable) });
            Set(equippable.verbTracker, "verbs", new List<Verb>());
            Set(Weapon, "comps", new List<ThingComp> { equippable });
            Target.mindState = new Verse.AI.Pawn_MindState(Target);
            Target.jobs = new Verse.AI.Pawn_JobTracker(Target); Target.stances = new Pawn_StanceTracker(Target);
            Target.equipment = new Pawn_EquipmentTracker(Target); Target.equipment.AddEquipment(Weapon);
            Enchants = new[] { "Fire", "Ice", "Lit", "Dark" }.Select(n => new HediffDef { defName = "TM_WeaponEnchantment_" + n + "HD", maxSeverity = 1000, hediffClass = typeof(HediffWithComps) }).ToArray();
            TorannMagicDefOf.TM_WeaponEnchantment_FireHD = Enchants[0]; TorannMagicDefOf.TM_WeaponEnchantment_IceHD = Enchants[1]; TorannMagicDefOf.TM_WeaponEnchantment_LitHD = Enchants[2]; TorannMagicDefOf.TM_WeaponEnchantment_DarkHD = Enchants[3];
        }
        static ThingDef Resource(string name)
        { var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); d.defName = name; d.thingClass = typeof(Thing); d.category = ThingCategory.Item; d.drawerType = DrawerType.RealtimeOnly; d.stackLimit = 1000; d.destroyable = true; d.comps = new List<CompProperties>(); return d; }
        public MagicPower Owner(bool enchant) => Magic.MagicData.MagicPowersE.Single(p => p.TMabilityDefs.Contains(enchant ? TorannMagicDefOf.TM_EnchantWeapon : TorannMagicDefOf.TM_Transmutate));
        public PawnAbility Ability(bool enchant = false, bool owned = true)
        { Owner(enchant).learned = true; var a = new PawnAbility(Magic); Set(a, "powerDef", enchant ? TorannMagicDefOf.TM_EnchantWeapon : TorannMagicDefOf.TM_Transmutate); if (owned) Magic.AbilityData.Powers.Add(a); return a; }
        public void Adept() { Path.RecordCompletedTrial(EnchanterTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.enchanter", InitiationStage.Foundation, EnchanterTrials.FoundationTrialId); }
        public EnchanterTrials.RawConversion Capture(PawnAbility ability)
        { var prior = EnchanterTrials.BeginTransmutation(ability, Source); try { return EnchanterTrials.CaptureRawConversion(Caster, Source); } finally { EnchanterTrials.EndTransmutation(prior); } }
        public void Pay(EnchanterTrials.RawConversion trial)
        { var payment = Source.SplitOff(2); payment.Destroy(DestroyMode.Vanish); EnchanterTrials.RawResourceConsumed(trial, 2, payment.Destroyed); }
        public HediffWithComps Ward(HediffDef? def = null)
        {
            var comp = new HediffComp_EnchantedWeapon { enchanterPawn = Caster, enchantedWeapon = Weapon };
            var hd = new HediffWithComps { pawn = Target, def = def ?? Enchants[0], comps = new() { comp } }; comp.parent = hd;
            Set(hd, "severityInt", 1f); Target.health.hediffSet.hediffs.Add(hd); return hd;
        }
        public void Dispose()
        {
            TorannMagicDefOf.TM_DispelEnchantWeapon.abilityClass = priorDispelType;
            EnchanterTrials.EndTransmutation(null!); Current.Game = previous;
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
            TorannMagicDefOf.TM_WeaponEnchantment_FireHD = saved[0]; TorannMagicDefOf.TM_WeaponEnchantment_IceHD = saved[1]; TorannMagicDefOf.TM_WeaponEnchantment_LitHD = saved[2]; TorannMagicDefOf.TM_WeaponEnchantment_DarkHD = saved[3];
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
        }
    }
    static void Raw(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability();
        var trial = w.Capture(ability); EnchanterTrials.RawResourceConsumed(trial, 2, true);
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 7, true), "claimed consumption without actual stack loss rejected");
        trial = w.Capture(ability); w.Pay(trial);
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 7, false), "payment with failed placement rejected");
        trial = w.Capture(ability); w.Pay(trial);
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Source.def, 7, true), "same resource rejected");
        trial = w.Capture(ability); w.Pay(trial);
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 0, true), "zero output rejected");
        trial = w.Capture(ability); w.Pay(trial);
        Check.Equal(true, EnchanterTrials.CompleteRawConversion(trial, w.Output, 7, true), "native split/destroy payment and successful positive different output qualify");
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 7, true), "single use");
        Check.Equal(92, w.Source.stackCount, "native partial stack consumption preserved");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual still required");
    }
    public sealed class NoopDestroyThing : Thing
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { }
    }
    static void NoopPayment(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability();
        w.Source.def.thingClass = typeof(NoopDestroyThing);
        var trial = w.Capture(ability);
        var payment = w.Source.SplitOff(2); payment.Destroy(DestroyMode.Vanish);
        Check.Equal(98, w.Source.stackCount, "native split reduces original source");
        Check.Equal(false, payment.Destroyed, "no-op override leaves payment alive");
        EnchanterTrials.RawResourceConsumed(trial, 2, payment.Destroyed);
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 7, true), "partial split without actual payment destruction rejected");
    }
    static void Scopes(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability(); var copy = w.Ability(owned: false);
        Check.Equal(true, w.Capture(copy) == null, "copy excluded");
        w.Magic.AbilityData.TemporaryWeaponPowers.Add(copy); Check.Equal(true, w.Capture(copy) == null, "item excluded");
        var prior = EnchanterTrials.BeginTransmutation(ability, w.Source);
        var nested = EnchanterTrials.BeginTransmutation(copy, w.Source);
        Check.Equal(true, EnchanterTrials.CaptureRawConversion(w.Caster, w.Source) == null, "nested invalid cast cannot borrow outer source");
        EnchanterTrials.EndTransmutation(nested);
        var trial = EnchanterTrials.CaptureRawConversion(w.Caster, w.Source);
        Check.Equal(true, trial != null, "outer scope restored");
        Check.Equal(true, EnchanterTrials.CaptureRawConversion(w.Caster, w.Source) == null, "source claimed once");
        EnchanterTrials.EndTransmutation(prior); w.Pay(trial!); w.Magic.AbilityData.Powers.Remove(ability);
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 2, true), "runtime owner drift rejected");
        w.Magic.AbilityData.Powers.Add(ability); trial = w.Capture(ability); w.Pay(trial); w.Owner(false).learned = false;
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 2, true), "native owner drift rejected");
        w.Owner(false).learned = true; trial = w.Capture(ability); w.Pay(trial); w.Adept();
        Check.Equal(false, EnchanterTrials.CompleteRawConversion(trial, w.Output, 2, true), "stage drift rejected");
    }
    static void Enchantment(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability(true);
        Check.Equal(true, EnchanterTrials.CaptureEnchantment(w.Ability(true, false), w.Target) == null, "copied source rejected");
        Check.Equal(true, EnchanterTrials.CaptureEnchantment(ability, w.Caster) == null, "self rejected");
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; Check.Equal(true, EnchanterTrials.CaptureEnchantment(ability, w.Target) == null, "captive excluded"); }
        w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "factionInt", new Faction()); Check.Equal(true, EnchanterTrials.CaptureEnchantment(ability, w.Target) == null, "foreign faction excluded"); Set(w.Target, "factionInt", w.Caster.Faction);
        w.Magic.weaponEnchants.Add(w.Target); var hd = w.Ward();
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(EnchanterTrials.CaptureEnchantment(ability, w.Target)), "unchanged enchant rejected");
        var trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); hd = w.Ward(); hd.TryGetComp<HediffComp_EnchantedWeapon>().enchanterPawn = w.Target;
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "foreign source rejected");
        trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); hd = w.Ward(); hd.TryGetComp<HediffComp_EnchantedWeapon>().enchantedWeapon = w.Source;
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "different weapon rejected");
        trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); w.Ward(); w.Magic.weaponEnchants.Clear();
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "missing caster registry rejected");
        w.Magic.weaponEnchants.Add(w.Target); trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); w.Ward();
        Check.Equal(true, EnchanterTrials.CompleteEnchantment(trial), "fresh owned equipped melee enchant qualifies");
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "single use");
    }
    static void EnchantmentDrift(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability(true);
        w.Target.equipment.GetDirectlyHeldThings().Remove(w.Weapon);
        Check.Equal(true, EnchanterTrials.CaptureEnchantment(ability, w.Target) == null, "unarmed ally excluded");
        w.Target.equipment.GetDirectlyHeldThings().TryAdd(w.Weapon);
        Set(w.Target, "mapIndexOrState", (sbyte)1); Check.Equal(true, EnchanterTrials.CaptureEnchantment(ability, w.Target) == null, "cross map excluded"); Set(w.Target, "mapIndexOrState", (sbyte)0);
        w.Magic.weaponEnchants.Add(w.Target);
        var trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); w.Ward(); w.Magic.AbilityData.Powers.Remove(ability);
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "runtime ownership drift excluded"); w.Magic.AbilityData.Powers.Add(ability);
        trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); w.Ward(); w.Owner(true).learned = false;
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "learned owner loss excluded"); w.Owner(true).learned = true;
        trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); w.Ward(); w.Target.equipment.GetDirectlyHeldThings().Remove(w.Weapon);
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "equipment drift excluded"); w.Target.equipment.GetDirectlyHeldThings().TryAdd(w.Weapon);
        trial = EnchanterTrials.CaptureEnchantment(ability, w.Target); w.Ward(); Set(w.Path, "stage", InitiationStage.Master);
        Check.Equal(false, EnchanterTrials.CompleteEnchantment(trial), "stage drift excluded");
    }
    sealed class VisualHooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.enchantertrial." + Guid.NewGuid(); readonly object harmony;
        public VisualHooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            void Prefix(MethodInfo target, string name)
            {
                var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(EnchanterTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
                type.GetMethod("Patch")!.Invoke(harmony, [target, hook, null, null, null]);
            }
            Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(FixedPosition));
            foreach (var m in typeof(TM_MoteMaker).GetMethods().Where(m => m.Name == "ThrowGenericMote")) Prefix(m, nameof(Skip));
            foreach (var m in typeof(FleckMaker).GetMethods().Where(m => m.Name == "ThrowFireGlow" || m.Name == "ThrowLightningGlow" || m.Name == "ThrowSmoke")) Prefix(m, nameof(Skip));
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static bool Skip() => false;
    static bool FixedPosition(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    static void NativeEnchantment(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new VisualHooks(); w.Adept(); var ability = w.Ability(true);
        var trial = EnchanterTrials.CaptureEnchantment(ability, w.Target);
        // Seed zero-severity native hediff containers after capture; native ApplyEnchantment chooses, adjusts and links the real effect.
        foreach (var def in w.Enchants) { var hd = w.Ward(def); Set(hd, "severityInt", 0f); var comp = hd.TryGetComp<HediffComp_EnchantedWeapon>(); comp.enchanterPawn = null; comp.enchantedWeapon = null; }
        var verb = new Verb_EnchantWeapon { Ability = ability }; Set(verb, "caster", w.Caster); Set(verb, "pwrVal", 2);
        verb.ApplyEnchantment(w.Target);
        Check.Equal(1, w.Magic.weaponEnchants.Count, "native caster registry updated exactly once");
        Check.Equal(2.5f, w.Target.health.hediffSet.hediffs.Sum(h => h.Severity), "native magnitude preserved");
        Check.Equal(true, EnchanterTrials.CompleteEnchantment(trial), "native apply source weapon registry and severity qualify");
    }
}
