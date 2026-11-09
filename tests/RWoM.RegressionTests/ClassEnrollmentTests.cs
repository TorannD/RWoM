using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ClassEnrollmentTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach(var roster in DebugClassRosterTests.Cases())yield return roster;
        yield return new("DebugClassRoster.GrantAndProtectExisting",f=>NativeEnrollment(f,false,physicalStart:true,gladiator:"TM_ReadGladiatorSprint",debugRoster:true));
        yield return new("MartialCareer.XMLAndLanguages", MartialCareerDefinitionTests.Verify);
        yield return new("ClassEnrollment.GiftedEligibilityAndNoLazyMutation", Eligibility);
        yield return new("ClassEnrollment.NeutralVocationDefinitions", Definitions);
        yield return new("ClassEnrollment.NativeEnrollmentCreatesManaWithoutSpells", f => NativeEnrollment(f, false));
        yield return new("ClassEnrollment.WandererLastUnlearnedFamily", f => NativeEnrollment(f, true, nearlyComplete: true));
        yield return new("ClassEnrollment.GeneratedWarriorFoundation", f => NativeEnrollment(f,false,physicalStart:true,gladiator:"TM_ReadGladiatorSprint",generated:true));
        foreach (var route in new[] { Vocation.Mage, Vocation.Warrior })
            yield return new("ClassEnrollment.Wanderer." + route, f => NativeEnrollment(f, true, route));
        yield return new("ClassEnrollment.PhysicalGiftChoosesMagicWanderer", f => NativeEnrollment(f, true, Vocation.Mage, physicalStart: true));
        yield return new("ClassEnrollment.BestowBeforeClass", f => NativeEnrollment(f, true, Vocation.Mage, dual: true));
        foreach (var choice in new[] { "TM_ReadGladiatorSprint", "TM_ReadGladiatorManeuver" })
            yield return new("GladiatorInitiation." + choice, f => NativeEnrollment(f, false, physicalStart: true, gladiator: choice));
        foreach (var choice in new[] { "TM_ReadBladeFocus", "TM_ReadBladeMemory" })
            yield return new("BladeFoundation." + choice, f => NativeEnrollment(f, false, physicalStart: true, gladiator: choice));
        foreach (var choice in new[] { "TM_ReadRangerTraining", "TM_ReadRangerTrailLore" })
            yield return new("RangerFoundation." + choice, f => NativeEnrollment(f, false, physicalStart: true, gladiator: choice));
        foreach (var entry in AdditionalProfiles)
            foreach (var option in entry.Choices)
                yield return new(entry.Name + "Career." + option, f => NativeEnrollment(f,false,physicalStart:true,gladiator:option));
        yield return new("SniperFoundation.NativeProjectilePatch", SniperShotTests.SpawnPatch);
        foreach (var choice in new[] { "TM_ReadSniperFocus", "TM_ReadSniperPatientAim" })
            yield return new("SniperFoundation." + choice, f => NativeEnrollment(f, false, physicalStart: true, gladiator: choice));
        yield return new("SniperMastery.Definitions", SniperAscensionTests.Definitions);
        foreach (var peaceful in new[] { false, true })
            yield return new("SniperMastery." + (peaceful ? "Research" : "Combat"), f => NativeEnrollment(f, false, physicalStart: true,
                gladiator: peaceful ? "TM_ReadSniperPatientAim" : "TM_ReadSniperFocus", sniperMastery: true));
        yield return new("ClassEnrollment.BooklessWanderer", f => NativeEnrollment(f, true));
    }
    private static readonly (string Name,string Book,string[] Choices)[] AdditionalProfiles = {
        ("SuperSoldier","TM_SuperSoldierFieldManual",["TM_ReadSuperSoldierCloseQuarters","TM_ReadSuperSoldierFieldAid"]),
        ("Commander","BookOfCommander",["TM_ReadCommanderProvisioner","TM_ReadCommanderSignals"]),
        ("Monk","BookOfMonk",["TM_ReadMonkRestorative","TM_ReadMonkCultivation"]),
        ("ShadowWalker","BookOfShadow",["TM_ReadShadowWalkerStrike","TM_ReadShadowWalkerPoison"]),
        ("Faceless","BookOfFaceless",["TM_ReadFacelessInfiltrator","TM_ReadFacelessUnderstudy"]),
        ("DeathKnight","BookOfDeathKnight",["TM_ReadDeathKnightDread","TM_ReadDeathKnightSpite"]),
        ("Psionic","BookOfPsionic",["TM_ReadPsionicProjector","TM_ReadPsionicAccelerator"]),
        ("Apothecary","BookOfApothecary",["TM_ReadApothecaryHerbalist","TM_ReadApothecaryFreshStock"])
    };
    private static Pawn? guardedBy;
    private static float redirectedDamage;
    public static bool CaptureRedirect(Thing __instance, DamageInfo dinfo)
    {
        if (__instance != guardedBy) return true;
        redirectedDamage += dinfo.Amount;
        return false;
    }
    static bool rescueThreat;
    public static bool RescueThreat(ref bool __result) { __result=rescueThreat; return false; }
    public static bool SkipPhysiology() => false;

    static void NativeEnrollment(RuntimeFixture f, bool wanderer, Vocation route = Vocation.WarriorMage, bool dual = false, bool physicalStart = false, string? gladiator = null, bool sniperMastery = false, bool debugRoster = false, bool nearlyComplete = false, bool generated = false)
    {
        LoadBaseDefinitions(f);
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(NeedDefOf));
        var previousGame = Current.Game;
        var oldMana = TorannMagicDefOf.TM_Mana; var oldHediff = TorannMagicDefOf.TM_MagicUserHD;
        var oldGifted = TorannMagicDefOf.TM_Gifted;
        var oldPhysical = TorannMagicDefOf.PhysicalProdigy;
        var oldStamina = TorannMagicDefOf.TM_Stamina; var oldMightHediff = TorannMagicDefOf.TM_MightUserHD;
        var traits = DefDatabase<TraitDef>.AllDefsListForReading.ToArray();
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        var classes = TM_ClassUtility.CustomClasses;
        var harmony = Assembly.Load("0Harmony"); var ht = harmony.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.enrollment-native." + Guid.NewGuid(); var instance = Activator.CreateInstance(ht, id)!;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Set(Current.Game, "maps", new List<Map> { map });
            TM_ClassUtility.CustomClasses = [];
            TorannMagicDefOf.TM_Gifted = new TraitDef { defName = "TM_Gifted", degreeDatas = [new TraitDegreeData { degree = 0 }] };
            var vocation = new TraitDef { defName = "TM_VocationMage", degreeDatas = [new TraitDegreeData { degree = 0 }] };
            if (DefDatabase<TraitDef>.GetNamedSilentFail(vocation.defName) == null) DefDatabase<TraitDef>.Add(vocation);
            else vocation = DefDatabase<TraitDef>.GetNamed(vocation.defName);
            DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "EnrollmentCapacity" });
            var p = PawnWithComps(false, false);
            p.pawn.kindDef = new PawnKindDef { defName = "EnrollmentColonist" };
            p.pawn.ageTracker = new Pawn_AgeTracker(p.pawn);
            p.pawn.mindState = new Pawn_MindState(p.pawn);
            p.pawn.def.race.lifeStageAges = [new LifeStageAge { minAge = 0, def = new LifeStageDef { defName = "EnrollmentAdult", developmentalStage = DevelopmentalStage.Adult } }];
            Set(p.pawn.ageTracker, "cachedLifeStageIndex", 0);
            p.pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "EnrollmentBody" } } };
            Set(p.pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
            Set(p.pawn, "mapIndexOrState", (sbyte)0);
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Gifted));
            var comp = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(comp);
            TorannMagicDefOf.TM_Mana = new NeedDef { defName = "EnrollmentMana", needClass = typeof(Need_Mana), onlyIfCausedByHediff = true };
            TorannMagicDefOf.TM_MagicUserHD = new HediffDef { defName = "EnrollmentMagicUser", hediffClass = typeof(Hediff), initialSeverity = 1,
                stages = [new HediffStage { enablesNeeds = [TorannMagicDefOf.TM_Mana] }] };
            var prefix = Activator.CreateInstance(harmony.GetType("HarmonyLib.HarmonyMethod")!, typeof(ClassEnrollmentTests).GetMethod(nameof(SkipPhysiology))!)!;
            ht.GetMethod("Patch")!.Invoke(instance, [typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange"), prefix, null, null, null]);
            if (physicalStart)
            {
                p.pawn.story.traits.allTraits.Clear();
                TorannMagicDefOf.PhysicalProdigy = new TraitDef { defName = "PhysicalProdigy", degreeDatas = [new TraitDegreeData { degree = 0 }] };
                p.pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy));
                if (DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarriorMage") == null)
                    DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationWarriorMage", degreeDatas = [new TraitDegreeData { degree = 0 }] });
                if (gladiator != null)
                {
                    using var language = new SpellItemEffectTests.LanguageScope(f.Repository);
                    var defs = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
                    var choice = gladiator.EndsWith("Maneuver") ? "TM_GladiatorManeuver" : "TM_Sprint";
                    var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(choice) ?? f.AddAbility(choice);
                    var oldType = ability.abilityClass;
                    var oldSprintType = TorannMagicDefOf.TM_Sprint.abilityClass;
                    var sweep = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorSweep") ?? f.AddAbility("TM_GladiatorSweep");
                    var oldSweepType = sweep.abilityClass;
                    var extra = AdditionalProfiles.FirstOrDefault(x => x.Choices.Contains(gladiator));
                    var suite = extra.Name == null ? null : typeof(ClassEnrollmentTests).Assembly.GetType("RWoM.RegressionTests."+extra.Name+"CareerTests",true)!;
                    const BindingFlags testHooks = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
                    using var additions = (IDisposable?)suite?.GetMethod("Setup",testHooks)?.Invoke(null,[f]);
                    bool blade = gladiator.StartsWith("TM_ReadBlade");
                    var passage = blade ? DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladePassage") ?? f.AddAbility("TM_BladePassage") : null;
                    var oldPassage = passage?.abilityClass;
                    bool ranger = gladiator.StartsWith("TM_ReadRanger");
                    var friend = ranger ? TorannMagicDefOf.TM_AnimalFriend : null;
                    var oldFriend = friend?.abilityClass;
                    try
                    {
                        if (friend != null) friend.abilityClass = typeof(PawnAbility);
                        if (passage != null) passage.abilityClass = typeof(PawnAbility);
                        sweep.abilityClass = ability.abilityClass = TorannMagicDefOf.TM_Sprint.abilityClass = typeof(PawnAbility);
                        if (DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarrior") == null)
                            DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationWarrior", degreeDatas = [new TraitDegreeData {degree=0}] });
                        if (generated)
                        {
                            Set(p.pawn,"mapIndexOrState",(sbyte)-1);
                            Set(p.pawn,"factionInt",new Faction { def = new FactionDef { isPlayer = false } });
                            var grant = typeof(TorannMagicMod.PawnGenerator_Patch).GetMethod("GrantRolledWarrior",testHooks)!;
                            grant.Invoke(null,[p.pawn,TorannMagicDefOf.Gladiator,0]);
                            Check.Equal(InitiationStage.Foundation, comp.State.Find("rwom.gladiator").Stage,"only foundation at generation");
                            Check.Equal(false,p.pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator),"no classic overlay");
                            Check.Equal(true,p.might.MightData.MightPowersG.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sprint)).learned,"real owned ability");
                            return;
                        }
                        DefDatabase<DesignationDef>.Add(new DesignationDef {defName="GladiatorJobTest"});
                        map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map);
                        map.reservationManager = new ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
                        p.pawn.inventory = new Pawn_InventoryTracker(p.pawn);
                        var form = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                        form.defName=extra.Book ?? (ranger ? "BookOfRanger" : blade ? "BookOfBladedancer" : gladiator.StartsWith("TM_ReadSniper") ? "BookOfSniper" : "BookOfGladiator"); form.category=ThingCategory.Item; form.thingClass=typeof(Thing); form.drawerType=DrawerType.RealtimeOnly; form.stackLimit=5; form.destroyable=true;
                        if(debugRoster){DebugClassRosterTests.Grant(f,p.pawn,form,comp);return;}
                        if(extra.Name=="ShadowWalker") ShadowWalkerCareerTests.BookMenu(p.pawn,form);
                        var book = new Thing { def=form, stackCount=2 };
                        Check.Equal(true,p.pawn.inventory.innerContainer.TryAdd(book),"owns book");
                        var complete = typeof(JobDriver_GladiatorInitiation).GetMethod("Complete",BindingFlags.Static|BindingFlags.NonPublic)!;
                        bool Read(string option) => (bool)complete.Invoke(null,[p.pawn,book,option])!;
                        Check.Equal(false,Read("unknown"),"invalid choice refused");
                        Check.Equal(false,Read(form.defName=="BookOfSniper" ? "TM_ReadGladiatorSprint" : "TM_ReadSniperFocus"),"wrong book for choice");
                        Check.Equal(true,Read(gladiator),"initiation succeeds");
                        Check.Equal(false,Read(gladiator),"no duplicate payment");
                        Check.Equal(1,book.stackCount,"one form consumed");
                        Check.Equal(Vocation.Warrior,comp.State.Vocation,"warrior identity");
                        if (suite != null)
                        {
                            suite.GetMethod("Verify",testHooks)!.Invoke(null,[f,p.pawn,p.might,comp,gladiator==extra.Choices[1]]);
                            return;
                        }
                        if (ranger)
                        {
                            bool trailLore = gladiator.EndsWith("TrailLore");
                            RangerFoundationTests.Verify(f,p.pawn,p.might,comp,trailLore);
                            RangerCareerTests.Verify(f,p.pawn,p.might,comp,trailLore);
                            return;
                        }
                        if (blade)
                        {
                            Check.Equal(false,p.pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer),"neutral warrior vocation");
                            Check.Equal(true,p.might.MightData.MightPowersB.Single(power=>power.TMabilityDefs.Contains(TorannMagicDefOf.TM_BladeFocus)).learned,"shared passive owner");
                            BladeFoundationTests.Verify(f,p.pawn,p.might,gladiator.EndsWith("Memory"));
                            BladeCareerTests.Verify(f,p.pawn,p.might,comp,gladiator.EndsWith("Memory"));
                            return;
                        }
                        if (form.defName == "BookOfSniper")
                        {
                            VerifySniper(f,p.pawn,p.might,comp,gladiator.EndsWith("PatientAim"), sniperMastery);
                            return;
                        }
                        Check.Equal(true,p.might.AbilityData.Powers.Any(a => a.Def == ability),"selected technique usable");
                        var scrollDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                        scrollDef.defName = "SkillOf_Sprint"; scrollDef.destroyable = true; scrollDef.thingClass = typeof(ThingWithComps); scrollDef.drawerType = DrawerType.RealtimeOnly;
                        var scroll = new ThingWithComps { def = scrollDef, stackCount = 1 };
                        var reader = new TorannMagic.CompUseEffect_LearnSkill { parent = scroll };
                        Check.Equal(gladiator.EndsWith("Maneuver"), reader.CanBeUsedBy(p.pawn).Accepted, "unknown Sprint can be taught independently");
                        reader.DoEffect(p.pawn);
                        Check.Equal(gladiator.EndsWith("Maneuver"), scroll.Destroyed, "only successful learning consumes scroll");
                        Check.Equal(1, p.might.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Sprint), "one runtime Sprint after reading");
                        var second = new ThingWithComps { def = scrollDef, stackCount = 1 };
                        var secondReader = new TorannMagic.CompUseEffect_LearnSkill { parent = second };
                        Check.Equal(false,secondReader.CanBeUsedBy(p.pawn).Accepted,"already learned from any source");
                        secondReader.DoEffect(p.pawn);
                        Check.Equal(false,second.Destroyed,"duplicate scroll retained");
                        Check.Equal(true,p.might.AbilityData.Powers.Any(a => a.Def == ability),"chosen foundation remains usable");
                        var xml = XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/CustomDefs/TM_GladiatorInitiation.xml")).Root!;
                        var effect = xml.Elements("HediffDef").Single(e => (string?)e.Element("defName")=="TM_GladiatorManeuverHD");
                        var timed = new HediffDef { defName = "TM_GladiatorManeuverHD", hediffClass = typeof(HediffWithComps),
                            comps = [new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange((int)effect.Descendants("disappearsAfterTicks").Single()) }] };
                        var previousHediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
                        try
                        {
                            DefDatabase<HediffDef>.Add(timed);
                            var verb = new Verb_GladiatorManeuver { caster = p.pawn };
                            Set(verb, "currentTarget", new LocalTargetInfo(p.pawn));
                            var cast = typeof(Verb_GladiatorManeuver).GetMethod("TryCastShot", BindingFlags.Instance|BindingFlags.NonPublic)!;
                            Check.Equal(gladiator.EndsWith("Maneuver"), (bool)cast.Invoke(verb,null)!, "only selected variant casts");
                            var active = p.pawn.health.hediffSet.GetFirstHediffOfDef(timed);
                            if (active != null) Check.Equal(480,active.TryGetComp<HediffComp_Disappears>().ticksToDisappear,"eight-second native effect");
                            Check.Equal(gladiator.EndsWith("Maneuver"),active != null,"native health effect present only for maneuver");
                        }
                        finally { DefDatabase<HediffDef>.Clear(); foreach (var d in previousHediffs) DefDatabase<HediffDef>.Add(d); }
                        var ally = PawnWithComps(false,false).pawn;
                        ally.AllComps.Add(new CompClassProgression { parent=ally });
                        Set(ally,"mapIndexOrState",(sbyte)0);
                        Set(ally,"factionInt",p.pawn.Faction);
                        var dest = new IntVec3(20,0,20);
                        Set(p.pawn,"positionInt",dest); Set(ally,"positionInt",dest + IntVec3.North);
                        var flags = BindingFlags.Static | BindingFlags.NonPublic;
                        var finish = typeof(JobDriver_GladiatorDefense).GetMethod("Finish",flags)!;
                        var chooseDefense = typeof(JobDriver_GladiatorDefense).GetMethod("Choose",flags)!;
                        bool Drill(int distance) => (bool)finish.Invoke(null,[p.pawn,ally,dest - new IntVec3(distance,0,0),true])!;
                        bool SelectDefense(GladiatorDefense d) => (bool)chooseDefense.Invoke(null,[p.pawn,d])!;
                        var defense = gladiator.EndsWith("Sprint") ? GladiatorDefense.Cover : GladiatorDefense.Fortitude;
                        Check.Equal(false,SelectDefense(defense),"trial precedes choice");
                        Check.Equal(false,Drill(0),"stationary drill refused");
                        Check.Equal(true,Drill(8),"aid an ally with either foundation");
                        Check.Equal(true,SelectDefense(defense),"independent defensive choice");
                        Check.Equal(false,SelectDefense(defense == GladiatorDefense.Cover ? GladiatorDefense.Fortitude : GladiatorDefense.Cover),"defensive choice permanent");
                        if (defense == GladiatorDefense.Cover)
                        {
                            var source = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                            source.projectile = new ProjectileProperties();
                            var incoming = new DamageDef { defName="GladiatorDefenseTestHit" };
                            var staminaDef = new NeedDef { defName="GuardStamina", needClass=typeof(Need_Stamina) };
                            TorannMagicDefOf.TM_Stamina = staminaDef;
                            var stamina = new Need_Stamina(p.pawn) { def=staminaDef };
                            p.pawn.needs.AllNeeds.Add(stamina);
                            Set(p.pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000L);
                            stamina.CurLevelPercentage = 1f;
                            bool Cover() => (bool)finish.Invoke(null,[p.pawn,ally,dest,false])!;
                            Check.Equal(true,Cover(),"cover starts adjacent");
                            Check.Equal(false,Cover(),"cooldown prevents repeat");
                            Check.Equal(true,Math.Abs(stamina.CurLevelPercentage-.8f)<.0001f,"one fifth stamina spent once");
                            var receive = ClassIdentity.Component(ally);
                            var patch = Activator.CreateInstance(harmony.GetType("HarmonyLib.HarmonyMethod")!,typeof(ClassEnrollmentTests).GetMethod(nameof(CaptureRedirect))!)!;
                            ht.GetMethod("Patch")!.Invoke(instance,[typeof(Thing).GetMethod("TakeDamage"),patch,null,null,null]);
                            guardedBy = p.pawn; redirectedDamage = 0;
                            try
                            {
                                var blast = new DamageInfo(new DamageDef {isExplosive=true},50,0,-1,null,null,source);
                                receive.PostPreApplyDamage(ref blast,out bool absorbed);
                                Check.Equal(50f,blast.Amount,"explosion ignored");
                                var shooter = PawnWithComps(false,false).pawn;
                                shooter.kindDef = new PawnKindDef { hostileToAll=true, race=shooter.def };
                                Set(shooter,"mapIndexOrState",(sbyte)0);
                                Set(shooter,"factionInt",new Faction { def=new FactionDef() });
                                var hit = new DamageInfo(incoming,12,0,-1,shooter,null,source);
                                receive.PostPreApplyDamage(ref hit,out absorbed);
                                Check.Equal(true,absorbed,"small shot fully intercepted");
                                var next = new DamageInfo(incoming,18,0,-1,null,null,source);
                                receive.PostPreApplyDamage(ref next,out absorbed);
                                Check.Equal(false,absorbed,"overflow hits ally");
                                Check.Equal(10f,next.Amount,"only unused budget deducted");
                                Check.Equal(20f,redirectedDamage,"damage delivered to guardian");
                            }
                            finally { guardedBy=null; }
                        }
                        var mastery = typeof(GladiatorAttack).Assembly.GetType("TorannMagic.ClassProgression.GladiatorMastery")!;
                        var attack = defense == GladiatorDefense.Fortitude ? GladiatorAttack.Sweep : GladiatorAttack.Cleave;
                        bool ChooseAttack(GladiatorAttack option) => (bool)mastery.GetMethod("Choose",flags)!.Invoke(null,[p.pawn,option])!;
                        if (defense == GladiatorDefense.Cover)
                            Check.Equal(true,comp.State.Find("rwom.gladiator").CompletedTrials.Contains("rwom.gladiator.holdLine"),"actual hostile interception completes combat route");
                        else
                        {
                            Check.Equal(false,ChooseAttack(attack),"second trial required");
                            Check.Equal(true,Drill(8),"peaceful mastery route");
                        }
                        Check.Equal(true,ChooseAttack(attack),"attack choice granted");
                        Check.Equal(false,ChooseAttack(attack == GladiatorAttack.Sweep ? GladiatorAttack.Cleave : GladiatorAttack.Sweep),"attack cannot be exchanged");
                        Check.Equal(attack == GladiatorAttack.Sweep,p.might.AbilityData.Powers.Any(a => a.Def == sweep),"only manual branch gets a separate action");
                        Check.Equal(attack == GladiatorAttack.Cleave,(bool)mastery.GetMethod("Automatic",flags)!.Invoke(null,[p.pawn,p.might])!,"only classic branch procs");
                        var asc = typeof(GladiatorUltimate).Assembly.GetType("TorannMagic.ClassProgression.GladiatorAscension")!;
                        bool Graduate() => (bool)asc.GetMethod("Graduate",flags)!.Invoke(null,[p.pawn])!;
                        bool Ascend(GladiatorUltimate option) => (bool)asc.GetMethod("Choose",flags)!.Invoke(null,[p.pawn,option])!;
                        var ultimate=attack==GladiatorAttack.Sweep ? GladiatorUltimate.LastCircle : GladiatorUltimate.Whirlwind;
                        p.might.MightData.MightUserLevel=29;
                        Check.Equal(false,Graduate(),"level 29 cannot graduate");
                        p.might.MightData.MightUserLevel=30;
                        Check.Equal(false,Ascend(ultimate),"level alone is insufficient");
                        if (ultimate == GladiatorUltimate.Whirlwind)
                        {
                            var oldRescue = JobDefOf.Rescue;
                            try
                            {
                                JobDefOf.Rescue = new JobDef {defName="Rescue"};
                                map.info=new MapInfo {Size=new IntVec3(40,1,40)};
                                map.cellIndices=new CellIndices(map); map.thingGrid=new ThingGrid(map);
                                var bedDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); bedDef.size=new IntVec2(1,1);
                                var bed=new Building_Bed {def=bedDef};
                                Set(bed,"positionInt",ally.Position); Set(bed,"mapIndexOrState",(sbyte)0);
                                ally.def.category=ThingCategory.Pawn; ally.def.size=new IntVec2(1,1);
                                map.thingGrid.Register(bed); map.thingGrid.Register(ally);
                                ally.jobs=new Pawn_JobTracker(ally); Set(ally.jobs,"curJob",new Job {def=new JobDef {defName="LayDown"}});
                                ally.jobs.posture=PawnPosture.LayingInBed;
                                Set(ally.health,"healthState",PawnHealthState.Down);
                                ally.health.hediffSet.hediffs.Add(new Hediff_Injury {pawn=ally,def=new HediffDef {defName="Injury"}});
                                var threatPatch=Activator.CreateInstance(harmony.GetType("HarmonyLib.HarmonyMethod")!,typeof(ClassEnrollmentTests).GetMethod(nameof(RescueThreat))!)!;
                                ht.GetMethod("Patch")!.Invoke(instance,[typeof(GenHostility).GetMethod("AnyHostileActiveThreatToPlayer"),threatPatch,null,null,null]);
                                var rescueJob=new JobDriver_TakeToBed {pawn=p.pawn,job=new Job {def=JobDefOf.Rescue,targetA=ally,targetB=bed}};
                                var rescuePatch=typeof(GladiatorUltimate).Assembly.GetType("TorannMagic.ClassProgression.GladiatorRescueTrial")!.GetMethod("Postfix",flags)!;
                                void Deliver() => ((IEnumerable<Toil>)rescuePatch.Invoke(null,[Array.Empty<Toil>(),rescueJob])!).Single().initAction();
                                rescueThreat=false; Deliver();
                                Check.Equal(false,Ascend(ultimate),"peacetime bed delivery is not a combat rescue");
                                Check.Equal(true,ally.Downed,"rescue victim downed"); Check.Same(bed,ally.CurrentBed(),"actual bed occupant");
                                rescueThreat=true; Deliver();
                                Check.Equal(true,comp.State.Find("rwom.gladiator").CompletedTrials.Contains("rwom.gladiator.evacuation"),"wounded ally delivered to designated bed during threat");
                            }
                            finally { JobDefOf.Rescue=oldRescue; rescueThreat=false; }
                        }
                        else
                        {
                        var origin = dest - new IntVec3(10,0,0);
                        var evacuation = new JobDriver_GladiatorDefense {pawn=p.pawn, job=new Job {
                            def=new JobDef {defName="TM_GladiatorEvacuationDrill"},targetA=ally,targetB=origin}};
                        Set(p.pawn,"positionInt",origin);
                        Check.Equal(true,evacuation.TryMakePreToilReservations(false),"legal graduation drill");
                        var drillSteps=((IEnumerable<Toil>)typeof(JobDriver_GladiatorDefense).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(evacuation,null)!).ToArray();
                        drillSteps.Last().initAction();
                        Check.Equal(false,Ascend(ultimate),"starting cell without rendezvous grants nothing");
                        Set(p.pawn,"positionInt",dest);
                        Check.Equal(300,drillSteps[1].defaultDuration,"evacuation preparation");
                        drillSteps[2].initAction(); drillSteps.Last().initAction();
                        Check.Equal(false,Ascend(ultimate),"rendezvous alone grants nothing");
                        Set(p.pawn,"positionInt",origin); drillSteps.Last().initAction();
                        Set(p.pawn,"positionInt",dest);
                        Check.Equal(false,Graduate(),"trial already committed by native job");
                        }
                        var oldWhirlwind=TorannMagicDefOf.TM_Whirlwind.abilityClass;
                        try
                        {
                            TorannMagicDefOf.TM_Whirlwind.abilityClass=typeof(PawnAbility);
                            Check.Equal(true,Ascend(ultimate),"one permanent ultimate");
                            Check.Equal(false,Ascend(ultimate==GladiatorUltimate.LastCircle ? GladiatorUltimate.Whirlwind : GladiatorUltimate.LastCircle),"no second ultimate");
                            Check.Equal(ultimate==GladiatorUltimate.Whirlwind,p.might.AbilityData.Powers.Any(a=>a.Def==TorannMagicDefOf.TM_Whirlwind),"classic grant only when selected");
                        }
                        finally { TorannMagicDefOf.TM_Whirlwind.abilityClass=oldWhirlwind; }
                        if (attack == GladiatorAttack.Sweep) GladiatorSweepTests.Attack(p.pawn, f);
                        var saved = comp.State; var file = Path.Combine(f.WorkingDirectory,"gladiator-choice.xml");
                        try { Scribe.saver.InitSaving(file,"regression"); Scribe_Deep.Look(ref saved,"state"); Scribe.saver.FinalizeSaving(); }
                        finally { Scribe.ForceStop(); }
                        saved = null!;
                        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref saved,"state"); Scribe.loader.FinalizeLoading(); }
                        finally { Scribe.ForceStop(); }
                        var savedPath = saved.Find("rwom.gladiator");
                        Check.Equal(ultimate,(GladiatorUltimate)typeof(DisciplineProgress).GetField("Ultimate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(savedPath)!, "ultimate persists");
                        Check.Equal(attack,(GladiatorAttack)typeof(DisciplineProgress).GetField("Attack",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(savedPath)!, "saved attack variant");
                        Check.Equal(defense, (GladiatorDefense)typeof(DisciplineProgress).GetField("Defense",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(savedPath)!, "independent defensive choice persists");
                        if (defense == GladiatorDefense.Cover)
                            Check.Equal(Find.TickManager.TicksGame + 1800,(int)typeof(DisciplineProgress).GetField("CoverReadyAt",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(savedPath)!, "cooldown persists across Scribe");
                        Check.Equal(choice == "TM_GladiatorManeuver",(bool)typeof(DisciplineProgress).GetField("GladiatorManeuver", BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(saved.Find("rwom.gladiator"))!, "saved variant");
                    }
                    finally { if (friend != null) friend.abilityClass=oldFriend; if (passage != null) passage.abilityClass=oldPassage; sweep.abilityClass=oldSweepType; ability.abilityClass=oldType; TorannMagicDefOf.TM_Sprint.abilityClass=oldSprintType; DefDatabase<DesignationDef>.Clear(); foreach (var d in defs) DefDatabase<DesignationDef>.Add(d); }
                    return;
                }
                var select = typeof(ClassEnrollment).GetMethod("ChooseWanderer", BindingFlags.Static | BindingFlags.NonPublic)!;
                Check.Equal(true, (bool)select.Invoke(null, [p.pawn, route])!, "physical gift can freely choose magic Wanderer");
                Check.Equal(route, comp.State.Vocation, "selected vocation committed");
                Check.Equal(false, p.pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy), "physical gift consumed");
                Check.Equal(true, p.pawn.story.traits.HasTrait(vocation), "mage trait installed");
                return;
            }
            if (dual)
            {
                TorannMagicDefOf.PhysicalProdigy = new TraitDef { defName = "PhysicalProdigy", degreeDatas = [new TraitDegreeData { degree = 0 }] };
                TorannMagic.Ideology.JobDriver_BestowMight.BestowMight(p.pawn, p.pawn);
                TorannMagic.Ideology.JobDriver_BestowMight.BestowMight(p.pawn, p.pawn);
                Check.Equal(1, p.pawn.story.traits.allTraits.Count(t => t.def == TorannMagicDefOf.PhysicalProdigy), "only one opposite gift");
            }
            Check.Equal(true, ClassEnrollment.CanEnrollMage(p.pawn), "native controlled gifted pawn eligible");
            Check.Equal(true, ClassEnrollment.TryEnrollMage(p.pawn), "native enrollment succeeds");
            Check.Equal(0, comp.CompGetGizmosExtra().Count(), "enrolled pawn uses existing grimoire button, not a second command");
            Check.Equal(Vocation.Mage, comp.State.Vocation, "saved vocation committed");
            Check.Equal(dual, p.pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted), "both potentials stay available until class choice");
            Check.Equal(!dual, p.pawn.story.traits.HasTrait(vocation), "dual gift defers neutral vocation");
            var mana = Check.NotNull(p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "native recovery creates mana");
            var data = p.magic.MagicData;
            Check.Equal(0, data.MagicUserXP, "new data starts at zero XP");
            Check.Equal(0, data.AllMagicPowersWithSkills.Count(x => x.learned), "enrollment grants no spells");
            Check.Equal(0, p.magic.AbilityData.Powers.Count, "runtime spells remain empty");
            Check.Equal(true, p.magic.TickConditionsMet, "magic tick cache enabled");
            mana.CurLevel = 0;
            Check.Equal(false, ClassEnrollment.TryEnrollMage(p.pawn), "repeat enrollment refused");
            Check.Same(data, p.magic.MagicData, "repeat preserves data owner");
            Check.Same(mana, p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "repeat preserves mana owner");
            Check.Equal(0f, mana.CurLevel, "repeat never refills mana");
            if (wanderer)
            {
                var hybrid = new TraitDef { defName = "TM_VocationWarriorMage", degreeDatas = [new TraitDegreeData { degree = 0 }] };
                if (DefDatabase<TraitDef>.GetNamedSilentFail(hybrid.defName) == null) DefDatabase<TraitDef>.Add(hybrid);
                TorannMagicDefOf.TM_Stamina = new NeedDef { defName = "WandererStamina", needClass = typeof(Need_Stamina), onlyIfCausedByHediff = true };
                TorannMagicDefOf.TM_MightUserHD = new HediffDef { defName = "WandererMightUser", hediffClass = typeof(Hediff), initialSeverity = 1,
                    stages = [new HediffStage { enablesNeeds = [TorannMagicDefOf.TM_Stamina] }] };
                var choose = typeof(ClassEnrollment).GetMethod("ChooseWanderer", BindingFlags.Static | BindingFlags.NonPublic)!;
                bool Choose() => (bool)choose.Invoke(null,[p.pawn,route])!;
                if (route == Vocation.Warrior && DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarrior") == null)
                    DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationWarrior", degreeDatas = [new TraitDegreeData { degree = 0 }] });
                data.MagicAbilityPoints = 9;
                Check.Equal(true, Choose(), "bookless grimoire action succeeds");
                if (route != Vocation.WarriorMage)
                {
                    Check.Equal(route, comp.State.Vocation, "single domain choice");
                    Check.Equal((int)route, (int)comp.State.Find("rwom.wanderer").Domain, "saved route");
                    Check.Equal(true, p.pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("TM_Vocation" + route)), "matching neutral trait");
                    TorannMagic.Ideology.JobDriver_BestowMight.BestowMight(p.pawn, p.pawn);
                    TorannMagic.Ideology.JobDriver_BestowMagic.BestowMagic(p.pawn, p.pawn);
                    Check.Equal(false, p.pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy), "classed recipient cannot regain physical potential");
                    Check.Equal(false, p.pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted), "classed recipient cannot regain magic potential");
                    Check.Equal(false, Choose(), "route is chosen once");
                    return;
                }
                Check.Equal(Vocation.WarriorMage, comp.State.Vocation, "combined vocation");
                Check.Equal(DisciplineDomain.WarriorMage, comp.State.Disciplines.Single().Domain, "one dual discipline");
                Check.Equal(2, data.MagicPowersW.Count(x => x.learned), "two magic records");
                Check.Equal(2, p.might.MightData.MightPowersW.Count(x => x.learned), "two martial records");
                Check.Equal(9, data.MagicAbilityPoints, "existing points retained");
                Check.Equal(0f, mana.CurLevel, "no mana refill");
                p.might.MightData.MightAbilityPoints = 7;
                p.might.AssignAbilities();
                Check.Equal(7, p.might.MightData.MightAbilityPoints, "reinitialization preserves martial points");
                Check.NotNull(p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Stamina), "new side gets native stamina");
                Check.Same(mana, p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "original mana retained");
                Check.Equal(2, p.might.MightData.AllMightPowersWithSkills.Count(x => x.learned), "no unrelated martial grants");
                Check.Equal(false, Choose(), "cannot initiate twice");
                Set(p.pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000L);
                var complete = typeof(JobDriver_WandererSurvey).GetMethod("Complete", BindingFlags.Static | BindingFlags.NonPublic)!;
                var goal = new IntVec3(20, 0, 20);
                Set(p.pawn, "positionInt", goal);
                bool Survey(int distance) => (bool)complete.Invoke(null, [p.pawn, goal - new IntVec3(distance, 0, 0), goal])!;
                var stamina = p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Stamina);
                stamina.CurLevelPercentage = 1;
                Check.Equal(false, Survey(12), "insufficient mana pays neither resource");
                Check.Equal(1f, stamina.CurLevelPercentage, "stamina retained on refusal");
                mana.CurLevelPercentage = 1;
                Check.Equal(false, Survey(11), "short route rejected");
                var order = new Job { targetA = goal, targetB = goal - new IntVec3(12, 0, 0) };
                var driver = new JobDriver_WandererSurvey { pawn = p.pawn, job = order };
                var toils = ((IEnumerable<Toil>)typeof(JobDriver_WandererSurvey)
                    .GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null)!).ToList();
                Check.Equal(180, toils[1].defaultDuration, "work precedes payment");
                Check.Equal(1f, mana.CurLevelPercentage, "interrupted approach or work pays nothing");
                toils.Last().initAction();
                Check.Equal(true, comp.State.Find("rwom.wanderer").CompletedTrials.Contains("rwom.wanderer.survey"), "native job completes trial");
                Check.Equal(false, Survey(12), "no repeat reward");
                Check.Equal(true, Math.Abs(mana.CurLevelPercentage - .9f) < .0001f, "one mana payment");
                Check.Equal(true, Math.Abs(stamina.CurLevelPercentage - .9f) < .0001f, "one stamina payment");
                Check.Equal(9, data.MagicAbilityPoints, "trial does not grant points");
                Check.Equal(7, p.might.MightData.MightAbilityPoints, "trial does not grant points");
                var saved = comp.State;
                string file = Path.Combine(f.WorkingDirectory, "unified-wanderer.xml");
                try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref saved, "progression"); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                ClassProgressionState loaded = null!;
                try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "progression"); Scribe.loader.FinalizeLoading(); }
                finally { Scribe.ForceStop(); }
                Set(comp, "state", loaded);
                Check.Equal(Vocation.WarriorMage, loaded.Vocation, "both roles survive Scribe");
                Check.Equal(DisciplineDomain.WarriorMage, loaded.Find("rwom.wanderer").Domain, "one saved path owns both domains");
                Check.Equal(InitiationStage.Foundation, loaded.Find("rwom.wanderer").Stage, "trial alone cannot advance stage");
                Check.Equal(false, Survey(12), "reload cannot repeat survey");
                Check.Equal(false, Choose(), "loading cannot reopen initiation");
                VerifyLessons(f, p.pawn, loaded, nearlyComplete);

            }
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(instance, [id]); Current.Game = previousGame;
            TorannMagicDefOf.TM_Mana = oldMana; TorannMagicDefOf.TM_MagicUserHD = oldHediff;
            TorannMagicDefOf.TM_Stamina = oldStamina; TorannMagicDefOf.TM_MightUserHD = oldMightHediff;
            TorannMagicDefOf.PhysicalProdigy = oldPhysical;
            TorannMagicDefOf.TM_Gifted = oldGifted;
            TM_ClassUtility.CustomClasses = classes;
            DefDatabase<TraitDef>.Clear(); foreach (var trait in traits) DefDatabase<TraitDef>.Add(trait);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var cap in capacities) DefDatabase<PawnCapacityDef>.Add(cap);
        }
    }

    static void VerifySniper(RuntimeFixture f, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool patient, bool mastery)
    {
        var oldDegrees = TorannMagicDefOf.TM_Sniper.degreeDatas;
        var oldAiming = StatDefOf.AimingDelayFactor;
        try
        {
            var aiming = StatDefOf.AimingDelayFactor = new StatDef { defName = "AimingDelayFactor" };
            var work = new StatDef { defName = "WorkSpeedGlobal" };
            var trait = XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/TraitDefs/Traits.xml")).Root!.Elements()
                .Single(e => (string?)e.Element("defName")=="TM_Sniper");
            TorannMagicDefOf.TM_Sniper.degreeDatas = trait.Element("degreeDatas")!.Elements().Select(e => new TraitDegreeData {
                degree=(int)e.Element("degree")!, statOffsets=e.Element("statOffsets")!.Elements()
                    .Where(x=>x.Name.LocalName==aiming.defName || x.Name.LocalName==work.defName)
                    .Select(x=>new StatModifier {stat=x.Name.LocalName==aiming.defName?aiming:work,value=(float)x}).ToList() }).ToList();
            var state = comp.State;
            var file = Path.Combine(f.WorkingDirectory,"sniper-foundation.xml");
            try { Scribe.saver.InitSaving(file,"regression"); Scribe_Deep.Look(ref state,"state"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            state=null!;
            try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref state,"state"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Set(comp,"state",state);
            Check.NotNull(state.Find("rwom.sniper"),"sniper saved");
            Check.Equal(1,might.MightData.MightPowersS.Count(x=>x.learned),"only foundation learned");
            var training=might.MightData.MightPowerSkill_SniperFocus.Single();
            Check.Equal(patient?4:3,training.levelMax,"no dead ranks");
            pawn.pather=(Pawn_PathFollower)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_PathFollower));
            Set(pawn.pather,"lastMovedTick",20);
            Set(Find.TickManager,"ticksGameInt",199);
            Check.Equal(patient?0f:-.1f,ClassPassiveEffects.StatOffset(pawn,aiming),"179 ticks");
            Set(Find.TickManager,"ticksGameInt",200);
            Check.Equal(patient?-.02f:-.1f,ClassPassiveEffects.StatOffset(pawn,aiming),"180 ticks");
            Check.Equal(patient?0f:-.1f,ClassPassiveEffects.StatOffset(pawn,work),"work tradeoff");
            training.level=training.levelMax;
            var expected=patient?-.1f:TorannMagicDefOf.TM_Sniper.degreeDatas.Last().statOffsets.Single(x=>x.stat==aiming).value;
            Check.Equal(true,Math.Abs(expected-ClassPassiveEffects.StatOffset(pawn,aiming))<.00001f,"maximum rank");
            Set(pawn.pather,"lastMovedTick",200);
            Check.Equal(true,Math.Abs((patient?0f:expected)-ClassPassiveEffects.StatOffset(pawn,aiming))<.00001f,"moving resets only Patient Aim");
            SniperShotTests.Progress(f,pawn,might,comp,patient);
            if (mastery) SniperMasteryTests.Verify(f, pawn, might, comp, patient);
            else SniperFractureImpactTests.Verify(pawn);
        }
        finally { TorannMagicDefOf.TM_Sniper.degreeDatas=oldDegrees; StatDefOf.AimingDelayFactor=oldAiming; }
    }

    static void VerifyLessons(RuntimeFixture f, Pawn pawn, ClassProgressionState state, bool nearlyComplete)
    {
        var files = Directory.GetFiles(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs"), "*.xml", SearchOption.AllDirectories);
        var defs = files.SelectMany(file => XDocument.Load(file).Root!.Elements("TorannMagic.TMAbilityDef"))
            .Where(node => (string?)node.Element("canCopy") == "true" && (string?)node.Element("shouldInitialize") != "false")
            .Select(node => DefDatabase<TMAbilityDef>.GetNamedSilentFail((string)node.Element("defName")!))
            .Where(d => d != null).Distinct().ToList();
        var original = defs.Select(d => (def:d, d.canCopy, d.shouldInitialize, d.abilityClass)).ToList();
        var book = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        book.defName = "WandererTestTome"; book.destroyable = true; book.stackLimit = 20; book.thingClass = typeof(Thing); book.category = ThingCategory.Item;
        var profile = new ClassDisciplineDef { defName = "WandererTestBookSource", profileId = "test.book", adeptBook = book };
        var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        var designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        try
        {
            foreach (var d in defs) { d.canCopy = d.shouldInitialize = true; d.abilityClass = typeof(PawnAbility); }
            DefDatabase<ClassDisciplineDef>.Add(profile);
            var map = pawn.Map;
            map.listerMergeables = new ListerMergeables(map);
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "WandererTestDesignation" });
            map.designationManager = new DesignationManager(map);
            map.reservationManager = new ReservationManager(map);
            map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
            book.drawerType = DrawerType.RealtimeOnly;
            pawn.inventory ??= new Pawn_InventoryTracker(pawn);
            var stack = new Thing { def = book, stackCount = 3 };
            Check.Equal(true, pawn.inventory.innerContainer.TryAdd(stack), "owns qualifying full book");
            Check.Equal(true, ClassInitiation.CompleteBook(pawn, stack), "cross-class book opens adept offer");
            Check.Equal(2, stack.stackCount, "one book paid");
            var path = state.Find("rwom.wanderer");
            Check.Equal(InitiationStage.Adept, path.Stage, "reading advances stage");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object Lessons() => typeof(DisciplineProgress).GetProperty("Lessons",flags)!.GetValue(path)!;
            string[] Offer() => ((IEnumerable<string>)Lessons().GetType().GetProperty("Offer", flags)!.GetValue(Lessons())!).ToArray();
            bool Pick(string key) => (bool)Lessons().GetType().GetMethod("Pick", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,[pawn,key])!;
            var choices = Offer();
            Check.Equal(6, choices.Distinct().Count(), "six distinct authored families");
            Check.Equal(true, choices.Any(id => id.StartsWith("W/")) && choices.Any(id => id.StartsWith("M/")), "both domains represented");
            Check.Equal(false, ClassInitiation.CompleteBook(pawn, stack), "no second payment or reroll");
            Check.Equal(true, Pick(choices[0]), "first selected ability learned");
            Check.Equal(false, Pick(choices[0]), "duplicate selection refused");
            var file = Path.Combine(f.WorkingDirectory, "wanderer-offer.xml");
            try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref state,"state"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            state = null!;
            try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref state,"state"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Set(ClassIdentity.Component(pawn),"state",state);
            path = state.Find("rwom.wanderer");
            Check.Equal(true, Offer().SequenceEqual(choices.Skip(1)), "same remaining offer after reload");
            Check.Equal(true, Pick(choices[1]), "second selected ability learned");
            Check.Equal(false, Pick(choices[2]), "third selection refused");
            Check.Equal(false, ClassInitiation.CompleteBook(pawn, stack), "adept cannot reuse full book");
            var survey = typeof(JobDriver_WandererSurvey).GetMethod("Complete", BindingFlags.NonPublic | BindingFlags.Static)!;
            var goal = pawn.Position;
            Check.Equal(false, (bool)survey.Invoke(null,[pawn, goal - new IntVec3(23,0,0), goal])!, "second expedition requires longer route");
            Check.Equal(true, (bool)survey.Invoke(null,[pawn, goal - new IntVec3(24,0,0), goal])!, "second expedition completed");
            Check.Equal(false, ClassInitiation.CompleteBook(pawn, stack), "wrong book tier cannot advance even after trial");
            var master = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            master.defName = "WandererMasterTome"; master.destroyable = true; master.stackLimit = 20;
            master.thingClass = typeof(Thing); master.category = ThingCategory.Item; master.drawerType = DrawerType.RealtimeOnly;
            profile.masterBook = master;
            var masterStack = new Thing { def = master, stackCount = 2 };
            Check.Equal(true, pawn.inventory.innerContainer.TryAdd(masterStack), "owns unrelated master tome");
            if (nearlyComplete)
            {
                var remaining = ((IEnumerable<string>)typeof(WandererLessons).GetMethod("Candidates", flags | BindingFlags.Static)!
                    .Invoke(null, [pawn,path])!).ToList();
                var keep = remaining.First();
                var otherRoots = remaining.Skip(1).Select(id => id.Substring(2)).ToHashSet();
                foreach (var ability in defs.Where(d => otherRoots.Contains(d.defName) && d.defName != keep.Substring(2))) ability.canCopy = false;
            }
            Check.Equal(true, ClassInitiation.CompleteBook(pawn, masterStack), "master tome opens next choice");
            Check.Equal(InitiationStage.Master, path.Stage, "mastery");
            Check.Equal(nearlyComplete ? 1 : 6, Offer().Length, "only available new choices offered");
            if (nearlyComplete)
            {
                Check.Equal(true, Pick(Offer().Single()), "last remaining ability can be picked");
                Check.Equal(0, (int)typeof(WandererLessons).GetProperty("Remaining", flags)!.GetValue(Lessons())!, "no outstanding unfulfillable pick");
            }
            Check.Equal(false, Offer().Any(id => id == choices[0] || id == choices[1]), "owned choices excluded");
        }
        finally
        {
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
            foreach (var d in original) { d.def.canCopy = d.canCopy; d.def.shouldInitialize = d.shouldInitialize; d.def.abilityClass = d.abilityClass; }
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
        }
    }

    static void Eligibility(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var traits = DefDatabase<TraitDef>.AllDefsListForReading.ToArray();
        var classes = TM_ClassUtility.CustomClasses;
        var previousGame = Current.Game;
        var harmony = Assembly.Load("0Harmony"); var ht = harmony.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.enrollment." + Guid.NewGuid(); var instance = Activator.CreateInstance(ht, id)!;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            if (DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage") == null)
                DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationMage", degreeDatas = [new TraitDegreeData { degree = 0 }] });
            TM_ClassUtility.CustomClasses = [];
            var p = PawnWithComps(false, false);
            p.pawn.mindState = new Pawn_MindState(p.pawn);
            p.pawn.kindDef = new PawnKindDef { defName = "EligibilityColonist" };
            Set(p.pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
            Set(p.pawn, "mapIndexOrState", (sbyte)0);
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Gifted));
            var c = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(c);
            Check.Equal(true, ClassEnrollment.CanEnrollMage(p.pawn), "pure gifted new pawn eligible");
            Check.Equal(true, ClassEnrollment.CanEnrollMage(p.pawn), "repeated preflight is stable");
            Check.Same(null!, typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(p.magic)!, "preflight does not allocate MagicData");
            Check.Same(null!, c.State!, "preflight does not enroll");
            Set(p.magic, "magicData", new MagicData { MagicUserXP = 1234, MagicAbilityPoints = 7 });
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "saved magic progression refused");
            Check.Equal(false, ClassEnrollment.TryEnrollMage(p.pawn), "explicit try cannot migrate saved progression");
            Check.Equal(1234, p.magic.MagicData.MagicUserXP, "old XP untouched");
            Check.Equal(7, p.magic.MagicData.MagicAbilityPoints, "old points untouched");
            Set(p.magic, "magicData", null);
            Set(p.might, "mightData", new MightData());
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "saved martial progression refused");
            Set(p.might, "mightData", null);
            p.magic.spell_Heal = true;
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "orphaned learned spell flag refused");
            Check.Equal(false, ClassEnrollment.TryEnrollMage(p.pawn), "explicit enrollment respects legacy spell flag");
            Check.Equal(true, p.magic.spell_Heal, "refused enrollment preserves learned flag");
            p.magic.spell_Heal = false;
            Set(p.magic, "abilityData", new AbilityData { Powers = [(PawnAbility)RuntimeHelpers.GetUninitializedObject(typeof(PawnAbility))] });
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "runtime power without data refused");
            Set(p.magic, "abilityData", null);
            foreach (var trait in new[] { TorannMagicDefOf.PhysicalProdigy, TorannMagicDefOf.Priest })
            {
                var t = new Trait(trait); p.pawn.story.traits.allTraits.Add(t);
                Check.Equal(trait == TorannMagicDefOf.PhysicalProdigy, ClassEnrollment.CanEnrollMage(p.pawn), "dual potential allowed, legacy class refused");
                p.pawn.story.traits.allTraits.Remove(t);
            }
            var addon = new TraitDef { defName = "KureEnrollmentTest", degreeDatas = [new TraitDegreeData { degree = 0 }] };
            TM_ClassUtility.CustomClasses = [new TM_CustomClass { classTrait = addon, traitDegree = 4, isAdvancedClass = true }];
            var addonTrait = new Trait(addon); p.pawn.story.traits.allTraits.Add(addonTrait);
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "addon advanced trait refused even with mismatched degree");
            p.pawn.story.traits.allTraits.Remove(addonTrait);
            Set(p.pawn, "mapIndexOrState", (sbyte)-1);
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "unspawned pawn refused");
            Set(p.pawn, "mapIndexOrState", (sbyte)0);
            c.BeginNewProgression(Vocation.Mage);
            Check.Equal(false, ClassEnrollment.CanEnrollMage(p.pawn), "already enrolled pawn refused");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(instance, [id]);
            Current.Game = previousGame;
            TM_ClassUtility.CustomClasses = classes;
            DefDatabase<TraitDef>.Clear(); foreach (var trait in traits) DefDatabase<TraitDef>.Add(trait);
        }
    }

    static void Definitions(RuntimeFixture f)
    {
        var root = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/TM_Vocations.xml")).Root!;
        Check.Equal(3, root.Elements("TraitDef").Count(), "all three vocations defined");
        foreach (var def in root.Elements("TraitDef"))
        {
            Check.Equal("0", def.Element("commonality")!.Value, "vocations are never randomly generated");
            Check.Equal(false, def.Descendants().Any(e => new[] { "statOffsets", "statFactors", "skillGains", "disabledWorkTags" }.Contains(e.Name.LocalName)), "vocations have no class passive bonuses");
        }
    }
}
