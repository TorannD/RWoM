using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class GolemancerTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GolemancerTrial.RunePaymentAndActualResult", Rune);
        yield return new("GolemancerTrial.RuneBypassOwnershipAndDrift", RuneBoundaries);
        yield return new("GolemancerTrial.BrandSourcePairAndRefresh", Brand);
        yield return new("GolemancerTrial.NativeFiveBeneficialBrands", NativeBrands);
        yield return new("GolemancerTrial.TransferKeepsExactPairsAndRejectsStaleDispel", Transfer);
        yield return new("GolemancerTrial.NativeRecipePaymentFailureAndBypass", NativeRecipe);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly PawnCapacityDef[] capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        readonly SkillDef[] skills = DefDatabase<SkillDef>.AllDefsListForReading.ToArray();
        readonly StatDef oldEfficiency = TorannMagicDefOf.TM_RuneCarvingEfficiency;
        readonly EffecterDef oldEffect = TorannMagicDefOf.TM_RuneCarving_EffecterED;
        readonly SkillDef oldArt, oldCraft;
        readonly float oldCost = TorannMagicDefOf.TM_RuneCarving?.manaCost ?? 0;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly RecipeDef[] recipes = DefDatabase<RecipeDef>.AllDefsListForReading.ToArray();
        readonly TorannMagic.TM_CustomPowerDef[] custom = DefDatabase<TorannMagic.TM_CustomPowerDef>.AllDefsListForReading.ToArray();
        readonly List<(FieldInfo field, object value)> fields = new();
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.golemancer");
        public readonly Recipe_RuneCarving Worker = new();
        public Bill Bill;
        public BodyPartRecord Part => Target.RaceProps.body.corePart;
        public Fixture(RuntimeFixture f)
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(SkillDefOf));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(TaleDefOf));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(LetterDefOf));
            oldArt = SkillDefOf.Artistic; oldCraft = SkillDefOf.Crafting;
            LoadBaseDefinitions(f);
            if (DefDatabase<PawnCapacityDef>.DefCount == 0) DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "GolemancerTrialCapacity" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "letterStack", new LetterStack());
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)); map.events = new MapEvents(map); Set(Current.Game, "maps", new List<Map> { map });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.guest = new Pawn_GuestTracker(pawn); pawn.kindDef = new PawnKindDef { defName = "GolemancerTrialPawn" }; pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "GolemancerTrialBody", hitPoints = 100 } } };
                pawn.def.race.body.corePart.body = pawn.def.race.body; pawn.def.race.body.AllParts.Add(pawn.def.race.body.corePart);
                Set(pawn.def.race, "fleshType", new FleshTypeDef { defName = "GolemancerTrialFlesh", isOrganic = true });
                Set(pawn, "positionInt", new IntVec3(3, 0, 3)); Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "factionInt", faction);
            }
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.golemancer", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "GolemancerTrialProfile", profileId = "rwom.golemancer", domain = DisciplineDomain.Magic });
            foreach (var name in Names.Concat(new[] { "RuneCarvedPart", "ArcaneTatooPart" }))
            {
                var field = typeof(TorannMagicDefOf).GetField("TM_" + name + "HD")!; fields.Add((field, field.GetValue(null)!));
                field.SetValue(null, new HediffDef { defName = "TM_" + name + "HD", maxSeverity = 1000, hediffClass = typeof(HediffWithComps), comps = name.EndsWith("Brand") ? new List<HediffCompProperties> { new() { compClass = typeof(HediffComp_BrandingBase) } } : new() });
            }
            DefDatabase<TorannMagic.TM_CustomPowerDef>.Clear();
            foreach (var name in Names)
                DefDatabase<TorannMagic.TM_CustomPowerDef>.Add(new TorannMagic.TM_CustomPowerDef { defName = "TM_" + name, customPower = new TorannMagic.TMDefs.TM_CustomPower { forMage = true, abilityDefs = new List<AbilityUser.AbilityDef> { DefDatabase<TMAbilityDef>.GetNamed("TM_" + name) } } });
            TM_Data.ResetDefinitionCaches();
            var recipe = new RecipeDef { defName = "TM_RuneCarveBodyPart", surgerySuccessChanceFactor = .7f, workerClass = typeof(Recipe_RuneCarving), defaultIngredientFilter = new ThingFilter(), fixedIngredientFilter = new ThingFilter() };
            DefDatabase<RecipeDef>.Clear(); DefDatabase<RecipeDef>.Add(recipe); Worker.recipe = recipe; Bill = new Bill_Medical(recipe, null);
            Owner(false).learned = true;
            DefDatabase<SkillDef>.Clear();
            SkillDefOf.Artistic = new SkillDef { defName = "GolemancerTrialArt" }; SkillDefOf.Crafting = new SkillDef { defName = "GolemancerTrialCraft" };
            DefDatabase<SkillDef>.Add(SkillDefOf.Artistic); DefDatabase<SkillDef>.Add(SkillDefOf.Crafting); Caster.skills = new Pawn_SkillTracker(Caster);
            var mana = (Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)); Set(mana, "pawn", Caster); mana.def = new NeedDef { defName = "TM_Mana" }; Caster.needs.AllNeeds.Add(mana); Magic.maxMP = 1; mana.CurLevel = 1;
            TorannMagicDefOf.TM_RuneCarving.manaCost = .2f;
            TorannMagicDefOf.TM_RuneCarvingEfficiency = new StatDef { defName = "GolemancerTrialEfficiency", defaultBaseValue = 1, minValue = 0, maxValue = 999, workerClass = typeof(FixedStat) };
            TorannMagicDefOf.TM_RuneCarving_EffecterED = new EffecterDef();
        }
        public MagicPower Owner(bool brand) => Magic.MagicData.MagicPowersGolemancer.Single(p => p.TMabilityDefs.Contains(brand ? TorannMagicDefOf.TM_Branding : TorannMagicDefOf.TM_RuneCarving));
        public void Adept() { Path.RecordCompletedTrial(GolemancerTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.golemancer", InitiationStage.Foundation, GolemancerTrials.FoundationTrialId); Owner(true).learned = true; }
        public PawnAbility Ability(string name = "AwarenessBrand", bool permanent = true)
        {
            var def = DefDatabase<TMAbilityDef>.GetNamed("TM_" + name); GolemancerBrands.Owners(Magic.MagicData)!.Single(p => p.abilityDef == def).learned = true;
            var a = new PawnAbility(Magic); Set(a, "powerDef", def); if (permanent) Magic.AbilityData.Powers.Add(a); return a;
        }
        public GolemancerTrials.RuneSnapshot Capture() => GolemancerTrials.CaptureRune(Worker, Caster, Target, Part, Bill);
        public void Dispose() { Current.Game = previous; Restore(capacities); Restore(skills); SkillDefOf.Artistic = oldArt; SkillDefOf.Crafting = oldCraft; TorannMagicDefOf.TM_RuneCarvingEfficiency = oldEfficiency; TorannMagicDefOf.TM_RuneCarving_EffecterED = oldEffect; TorannMagicDefOf.TM_RuneCarving.manaCost = oldCost; foreach (var f in fields) f.field.SetValue(null, f.value); Restore(profiles); Restore(recipes); Restore(custom); TM_Data.ResetDefinitionCaches(); }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var d in defs) DefDatabase<T>.Add(d); }
    }
    static readonly string[] Names = { "AwarenessBrand", "EmotionBrand", "FitnessBrand", "ProtectionBrand", "VitalityBrand", "SiphonBrand" };
    static void Rune(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        var unpaid = w.Capture(); w.Worker.RunePart(w.Target, w.Caster, w.Part);
        Check.Equal(false, GolemancerTrials.CompleteRune(unpaid), "native effect without observed native payment rejected");
        Check.Equal(true, w.Capture() == null, "existing rune cannot count again"); w.Target.health.hediffSet.hediffs.Clear();
        var s = w.Capture(); GolemancerTrials.RecordRunePayment(s, 1f, .8f, .2f);
        Check.Equal(false, GolemancerTrials.CompleteRune(s), "payment without actual result rejected");
        s = w.Capture(); GolemancerTrials.RecordRunePayment(s, 1f, .8f, .2f); w.Worker.RunePart(w.Target, w.Caster, w.Part);
        Check.Equal(true, GolemancerTrials.CompleteRune(s), "native new tattoo plus exact payment witness qualifies");
        Check.Equal(false, GolemancerTrials.CompleteRune(s), "attempt is one shot"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual remains required");
    }
    static void RuneBoundaries(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        w.Bill.recipe.surgerySuccessChanceFactor = 1; Check.Equal(true, w.Capture() == null, "guaranteed unpaid bypass rejected"); w.Bill.recipe.surgerySuccessChanceFactor = .7f;
        w.Owner(false).learned = false; Check.Equal(true, w.Capture() == null, "unlearned native owner rejected"); w.Owner(false).learned = true;
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; Check.Equal(true, w.Capture() == null, "captives rejected"); } w.Target.guest.guestStatusInt = GuestStatus.Guest;
        foreach (var payment in new[] { (1f, 1f, .2f), (1f, .9f, .2f), (1f, .5f, .2f), (1f, .8f, 0f), (float.NaN, .8f, .2f) })
        { var s = w.Capture(); GolemancerTrials.RecordRunePayment(s, payment.Item1, payment.Item2, payment.Item3); w.Worker.RunePart(w.Target, w.Caster, w.Part); Check.Equal(false, GolemancerTrials.CompleteRune(s), "no partial penalty or invalid payment credit"); w.Target.health.hediffSet.hediffs.Clear(); }
        var drift = w.Capture(); GolemancerTrials.RecordRunePayment(drift, 1, .8f, .2f); w.Worker.RunePart(w.Target, w.Caster, w.Part); w.Adept(); Check.Equal(false, GolemancerTrials.CompleteRune(drift), "stage drift rejected");
    }
    static void Brand(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); w.Adept(); var ability = w.Ability();
        Check.Equal(true, GolemancerTrials.CaptureBrand(w.Ability(permanent: false), w.Target) == null, "copied or item runtime rejected");
        Check.Equal(true, GolemancerTrials.CaptureBrand(w.Ability("SiphonBrand"), w.Target) == null, "siphon excluded");
        Check.Equal(true, GolemancerTrials.CaptureBrand(ability, w.Caster) == null, "self excluded");
        Check.Equal(false, GolemancerTrials.CompleteBrand(GolemancerTrials.CaptureBrand(ability, w.Target)), "no effect rejected");
        var s = GolemancerTrials.CaptureBrand(ability, w.Target); TM_Action.UpdateBrand(w.Target, w.Caster, w.Magic, TorannMagicDefOf.TM_AwarenessBrandHD);
        Check.Equal(false, GolemancerTrials.CompleteBrand(s), "native pair without BranderPawn rejected");
        var hd = w.Target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AwarenessBrandHD); hd.TryGetComp<HediffComp_BrandingBase>().BranderPawn = w.Caster;
        Check.Equal(true, GolemancerTrials.CaptureBrand(ability, w.Target) == null, "refresh and transfer excluded");
        w.Target.health.hediffSet.hediffs.Clear(); w.Magic.BrandPawns.Clear(); w.Magic.BrandDefs.Clear();
        s = GolemancerTrials.CaptureBrand(ability, w.Target); TM_Action.UpdateBrand(w.Target, w.Caster, w.Magic, TorannMagicDefOf.TM_AwarenessBrandHD);
        w.Target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AwarenessBrandHD).TryGetComp<HediffComp_BrandingBase>().BranderPawn = w.Caster;
        Check.Equal(true, GolemancerTrials.CompleteBrand(s), "new actual source and paired entry qualifies"); Check.Equal(false, GolemancerTrials.CompleteBrand(s), "repeat rejected");
    }
    static void Transfer(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        var awareness = TorannMagicDefOf.TM_AwarenessBrandHD;
        var fitness = TorannMagicDefOf.TM_FitnessBrandHD;
        var other = PawnWithComps(true,false);
        TM_Action.UpdateBrand(w.Target,w.Caster,w.Magic,awareness);
        var marker = w.Target.health.hediffSet.GetFirstHediffOfDef(awareness);
        marker.TryGetComp<HediffComp_BrandingBase>().BranderPawn = w.Caster;
        // Both a recurring target and a recurring definition must move by index.
        w.Magic.BrandPawns.InsertRange(0,[w.Target,other.pawn]);
        w.Magic.BrandDefs.InsertRange(0,[fitness,awareness]);
        w.Magic.BrandPawns.Add(w.Target); w.Magic.BrandDefs.Add(awareness);
        w.Magic.BrandPawns.Add(other.pawn); // incomplete tail from an old save
        TM_Action.UpdateBrand(w.Target,other.pawn,other.magic,awareness);
        marker.TryGetComp<HediffComp_BrandingBase>().BranderPawn = other.pawn;
        Check.Equal(true,w.Magic.BrandPawns.SequenceEqual([w.Target,other.pawn]) &&
            w.Magic.BrandDefs.SequenceEqual([fitness,awareness]),"other target and other definition retain their pairs");
        var remove = typeof(Verb_DispelBranding).GetMethod("RemoveOwnedBrand",BindingFlags.Static | BindingFlags.NonPublic)!;
        Check.Equal(false,(bool)remove.Invoke(null,[w.Caster,w.Target,awareness])!,"stale prior caster cannot dispel the transferred mark");
        Check.Same(marker,w.Target.health.hediffSet.GetFirstHediffOfDef(awareness),"foreign mark survives");
        TM_Action.UpdateBrand(w.Target,other.pawn,other.magic,awareness);
        Check.Equal(1,other.magic.BrandPawns.Count,"same-owner refresh leaves one upkeep entry");
    }

    static void NativeBrands(RuntimeFixture f)
    {
        foreach (string name in Names.Take(5))
        {
            using var w = new Fixture(f); using var hooks = new Hooks(); w.Adept(); var a = w.Ability(name);
            var type = typeof(Verb_BrandAwareness).Assembly.GetType("TorannMagic.Verb_Brand" + name.Replace("Brand", ""))!;
            var verb = (Verb_UseAbility)Activator.CreateInstance(type)!; verb.Ability = a; Set(verb, "caster", w.Caster); Set(verb, "currentTarget", new LocalTargetInfo(w.Target));
            type.GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(verb, null);
            Check.Equal(true, w.Path.CompletedTrials.Contains(GolemancerTrials.MasteryTrialId), "native " + name + " creates effect source and pair");
        }
    }
    public class FixedStat : StatWorker
    { public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true) => 1f; }
    static bool Position(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    static bool SpawnEffect(ref Effecter __result) { __result = (Effecter)RuntimeHelpers.GetUninitializedObject(typeof(Effecter)); __result.def = TorannMagicDefOf.TM_RuneCarving_EffecterED; return false; }
    static bool Enabled(ref bool __result) { __result = false; return false; }
    static int SeedFor(bool success)
    {
        for (int seed = 0; seed < 1000; seed++) { Rand.PushState(seed); try { if (Rand.Chance(.7f) == success) return seed; } finally { Rand.PopState(); } }
        throw new Exception("No deterministic seed found");
    }
    static void NativeRecipe(RuntimeFixture f)
    {
        using var language = new SpellItemEffectTests.LanguageScope(f.Repository);
        foreach (string mode in new[] { "success", "failure", "bypass", "insufficient", "unlearned", "direct", "previous" })
        {
            using var w = new Fixture(f); using var hooks = new Hooks(recipe: true);
            if (mode == "bypass") w.Bill.recipe.surgerySuccessChanceFactor = 1;
            if (mode == "insufficient") w.Magic.Mana.CurLevel = .1f;
            if (mode == "unlearned") w.Owner(false).learned = false;
            if (mode == "previous") w.Worker.RunePart(w.Target, w.Caster, w.Part);
            Check.Equal(1f, w.Caster.GetStatValue(TorannMagicDefOf.TM_RuneCarvingEfficiency), "native fixed stat input");
            // Native draw-tracker construction consumes RNG; initialize presentation before selecting the recipe seed.
            var warmPresentation = w.Caster.DrawPos;
            float before = w.Magic.Mana.CurLevel;
            Rand.PushState(SeedFor(mode != "failure"));
            try
            {
                if (mode == "direct") { Check.Equal(false, w.Worker.CheckRuneCarvingFail(w.Caster, w.Target, new List<Thing>(), w.Part, w.Bill), "native direct check succeeds"); w.Worker.RunePart(w.Target, w.Caster, w.Part); }
                else w.Worker.ApplyOnPawn(w.Target, w.Part, w.Caster, new List<Thing>(), w.Bill);
            }
            finally { Rand.PopState(); }
            float expected = mode == "bypass" ? before : (mode == "insufficient" || mode == "unlearned") ? before / 2 : before - .2f;
            Check.Equal(expected, w.Magic.Mana.CurLevel, "native mana outcome " + mode);
            Check.Equal(mode != "failure" && mode != "insufficient" && mode != "unlearned", w.Target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneTatooPartHD), "actual native result " + mode + " core=" + w.Part.IsCorePart + " effects=" + string.Join(",", w.Target.health.hediffSet.hediffs.Select(h => h.def.defName)));
            Check.Equal(mode == "success", w.Path.CompletedTrials.Contains(GolemancerTrials.FoundationTrialId), "only new paid native recipe success qualifies: " + mode);
        }
    }
    static bool Skip() => false;
    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!; readonly string id = "rwom.golemancertrial." + Guid.NewGuid(); readonly object harmony;
        public Hooks(bool recipe = false)
        {
            harmony = Activator.CreateInstance(type, id)!;
            if (recipe)
            {
                Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(Position));
                Prefix(typeof(SkillRecord).GetProperty("TotallyDisabled")!.GetMethod!, nameof(Enabled));
                Prefix(typeof(EffecterDef).GetMethod("Spawn", Type.EmptyTypes)!, nameof(SpawnEffect));
                foreach (var m in typeof(Effecter).GetMethods().Where(m => m.Name == "Trigger" || m.Name == "Cleanup")) Prefix(m);
                foreach (var m in typeof(MoteMaker).GetMethods().Where(m => m.Name == "ThrowText")) Prefix(m);
                foreach (var m in typeof(FleckMaker).GetMethods().Where(m => m.Name == "ThrowLightningGlow")) Prefix(m);
                foreach (var m in typeof(LetterStack).GetMethods().Where(m => m.Name == "ReceiveLetter")) Prefix(m);
                foreach (var m in typeof(TaleRecorder).GetMethods().Where(m => m.Name == "RecordTale")) Prefix(m);
            }
            Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!);
            Prefix(typeof(Verb_UseAbility).GetMethod("PostCastShot")!);
            foreach (var t in new[] { typeof(Verb_Brand), typeof(Verb_BrandProtection) })
                Prefix(t.GetMethod("DrawBrand", BindingFlags.Instance | BindingFlags.NonPublic)!);
        }
        void Prefix(MethodInfo m, string name = nameof(Skip)) { var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(GolemancerTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!; type.GetMethod("Patch")!.Invoke(harmony, [m, hook, null, null, null]); }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}




