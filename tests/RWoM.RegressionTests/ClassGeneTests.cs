using System.Reflection;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ClassGeneTests
{
    private static TraitDef BloodTrait(string name) => name switch {
        "TM_ArcaneConduitGene"=>TorannMagicDefOf.TM_ArcaneConduitTD,
        "TM_ManaWellGene"=>TorannMagicDefOf.TM_ManaWellTD,
        "TM_BoundlessGene"=>TorannMagicDefOf.TM_BoundlessTD,
        "TM_FaeBloodGene"=>TorannMagicDefOf.TM_FaeBloodTD,
        "TM_GiantsBloodGene"=>TorannMagicDefOf.TM_GiantsBloodTD,
        "TM_EnlightenedGene"=>TorannMagicDefOf.TM_EnlightenedTD,
        _=>throw new ArgumentException(name)};
    private static bool Enabled(ref bool __result){__result=true;return false;}
    public static IEnumerable<TestCase> Cases()
    {
        foreach(string name in new[]{"TM_ManaGene","TM_StaminaGene"}) {
            yield return new("ClassGenes.ActiveReactivate."+name,r=>Run(r,p=>{
                var def=DefDatabase<GeneDef>.GetNamed(name);
                var blocker=new GeneDef{defName="TestActiveBlocker",geneClass=typeof(Gene),exclusionTags=def.exclusionTags};
                DefDatabase<GeneDef>.Add(blocker);
                var dominant=p.genes.AddGene(blocker,true);
                var gene=p.genes.AddGene(def,false);
                Check.Equal(false,gene.Active,"native override suppresses active endogene");
                p.genes.RemoveGene(dominant);
                Check.Equal(true,gene.Active,"removing blocker activates gene");
                Check.Same(gene,p.story.traits.GetTrait(def.forcedTraits.Single().def)?.sourceGene,"reactivated potential owned by original gene");
            }));
            foreach(bool natural in new[]{false,true})
                yield return new("ClassGenes.Save."+name+(natural?".Natural":".Genetic"),r=>Run(r,p=>SaveTraits(r,p,name,natural)));
            foreach(string mode in new[]{"Remove","Natural","Suppressed"})
                yield return new("ClassGenes.Lifecycle."+name+"."+mode,r=>Run(r,p=>Lifecycle(p,name,mode)));
            yield return new("ClassGenes.ActivePotential."+name,r=>Run(r,p=>{
                var trait=name=="TM_ManaGene"?TorannMagicDefOf.TM_Gifted:TorannMagicDefOf.PhysicalProdigy;
                var gene=p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(name),true);
                TM_ClassGenes.SyncClassGenes(p);
                Check.NotNull(p.story.traits.GetTrait(trait),"classless active gene grants potential");
                Check.Same(gene,p.story.traits.GetTrait(trait).sourceGene,"potential belongs to active gene");
                Check.Equal(true,p.genes.GenesListForReading.Contains(gene),"active gene retained before class selection");
                Check.Equal(1,p.genes.GenesListForReading.Count,"no redundant latent gene added");
                Check.Equal(true,p.GetComp<TorannMagic.ClassProgression.CompClassProgression>().State==null,"no automatic class enrollment");
                p.genes.RemoveGene(gene);
                Check.Equal(false,p.story.traits.HasTrait(trait),"removal cleans genetic potential");
            }));
            yield return new("ClassGenes.ActiveAssigned."+name,r=>Run(r,p=>{
                var state=new TorannMagic.ClassProgression.ClassProgressionState(TorannMagic.ClassProgression.Vocation.Mage);
                state.TryAddFoundation("rwom.fire",TorannMagic.ClassProgression.DisciplineDomain.Magic);
                Set(p.GetComp<TorannMagic.ClassProgression.CompClassProgression>(),"state",state);
                p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(name),true);
                Check.Equal(false,p.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted)||p.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy),"active gene cannot add a second vocation to existing class");
                Check.Same(state,p.GetComp<TorannMagic.ClassProgression.CompClassProgression>().State,"assigned state preserved");
            }));
        }
        yield return new("ClassGenes.MartialFoundationConsumesHiddenGene",f=>{
            // Beta martial foundation commits a discipline without any class
            // GainTrait event for might classes; the foundation itself must
            // consume the latent hidden warrior gene (live report: the gene
            // survived a chosen class). Mirrors the ClassEnrollmentTests
            // generated-recruit path.
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            // LoadBaseDefinitions stubs carry no degrees; GainTrait resolves
            // DataAtDegree(0) and would throw on an empty degree list.
            foreach (var name in new[] { "Gladiator", "TM_VocationWarrior" })
            {
                var traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(name);
                if (traitDef == null) { traitDef = new TraitDef { defName = name }; DefDatabase<TraitDef>.Add(traitDef); }
                if (traitDef.degreeDatas == null || traitDef.degreeDatas.Count == 0)
                    traitDef.degreeDatas = [new TraitDegreeData { degree = 0, label = name }];
            }
            var previousGame = Current.Game;
            var game = (Game)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game));
            typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(game, new TickManager());
            typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(game, new UniqueIDsManager());
            Current.Game = game;
            var (p, _, might) = GrimoireProgressionTests.PawnWithComps(false, false);
            p.AllComps.Add(new TorannMagic.ClassProgression.CompClassProgression { parent = p });
            p.kindDef = new PawnKindDef { defName = "ClassGeneGladiator", race = p.def };
            p.ageTracker = new Pawn_AgeTracker(p); p.mindState = new Pawn_MindState(p);
            p.def.race.lifeStageAges = [new LifeStageAge { minAge = 0, def = new LifeStageDef { defName = "ClassGeneAdult", developmentalStage = DevelopmentalStage.Adult } }];
            Set(p.ageTracker, "cachedLifeStageIndex", 0);
            p.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "ClassGeneBody" } } };
            Set(p, "factionInt", new Faction { def = new FactionDef { isPlayer = false } });
            Set(p, "mapIndexOrState", (sbyte)-1);
            if (DefDatabase<GeneDef>.GetNamedSilentFail("TM_SleepingWarriorGene") == null)
                DefDatabase<GeneDef>.Add(new GeneDef { defName = "TM_SleepingWarriorGene", geneClass = typeof(Gene) });
            p.genes ??= new Pawn_GeneTracker(p);
            p.genes.AddGene(DefDatabase<GeneDef>.GetNamed("TM_SleepingWarriorGene"), true);
            var gladiator = TorannMagicDefOf.Gladiator;
            Check.NotNull(gladiator, "gladiator trait present");
            var sprint = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_Sprint");
            Check.NotNull(sprint, "gladiator classic ability def present");
            sprint.abilityClass = typeof(AbilityUser.PawnAbility);
            var oldSprintClass = sprint.abilityClass;
            sprint.abilityClass = typeof(AbilityUser.PawnAbility);
            // The wrapper falls back to the classic overlay trait when the
            // foundation transaction refuses — a false must stay visible.
            var wrapper = typeof(TorannMagic.TorannMagicMod).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Single(t => t.Name == "PawnGenerator_Patch")
                .GetMethod("GrantRolledWarrior", BindingFlags.NonPublic | BindingFlags.Static)!;
            try
            {
                wrapper.Invoke(null, new object?[] { p, gladiator, 0 });
                // State is an internal field, not a property.
                var compState = (TorannMagic.ClassProgression.ClassProgressionState)typeof(TorannMagic.ClassProgression.CompClassProgression)
                    .GetField("state", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(p.GetComp<TorannMagic.ClassProgression.CompClassProgression>())!;
                Check.NotNull(compState.Find("rwom.gladiator"), "martial discipline committed");
                Check.Equal(false, p.genes.GenesListForReading.Any(g => g.def.defName == "TM_SleepingWarriorGene"),
                    "the chosen martial class consumes the hidden warrior gene");
                Check.Equal(true, might.MightData.MightPowersG.Single(q => q.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sprint)).learned,
                    "gladiator classic power learned inside the foundation");
            }
            finally { sprint.abilityClass = oldSprintClass; Current.Game = previousGame; }
        });
        yield return new("ClassGenes.NativePawnSave",r=>Run(r,p=>NativePawnSave(r,p)));
        yield return new("ClassGenes.ActiveNativePawnSave",r=>Run(r,p=>NativePawnSave(r,p,true)));
        yield return new("ClassGenes.ActiveClassedPawnSave",r=>Run(r,p=>NativePawnSave(r,p,true,true)));
        foreach(bool beta in new[]{false,true})
        foreach(bool inherited in new[]{false,true})
        {
            yield return new($"ClassGenes.CustomXenotype.Beta{beta}.Inherited{inherited}",r=>Run(r,p=>CustomXenotype(p,beta,inherited)));
            yield return new($"ClassGenes.ActiveCustomXenotype.Beta{beta}.Inherited{inherited}",r=>Run(r,p=>CustomXenotype(p,beta,inherited,true)));
        }
        foreach(var name in new[]{"TM_ArcaneConduitGene","TM_ManaWellGene","TM_BoundlessGene","TM_FaeBloodGene","TM_GiantsBloodGene","TM_EnlightenedGene"})
        {
            foreach(bool natural in new[]{false,true})
                yield return new("ClassGenes.Save."+name+(natural?".Natural":".Genetic"),r=>Run(r,p=>SaveTraits(r,p,name,natural)));
            foreach(string mode in new[]{"Remove","Natural","Suppressed"})
                yield return new("ClassGenes.Lifecycle."+name+"."+mode,r=>Run(r,p=>Lifecycle(p,name,mode)));
            yield return new("ClassGenes.BloodlineOwnership."+name,r=>Run(r,p=>{
                var gene=p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(name),true);
                TM_ClassGenes.SyncClassGenes(p);
                var trait=p.story.traits.GetTrait(BloodTrait(name));
                Check.NotNull(trait,"gene grants bloodline trait");
                Check.Same(gene,trait.sourceGene,"bloodline gift belongs to its gene");
                p.genes.RemoveGene(gene);
                Check.Equal(false,p.story.traits.HasTrait(BloodTrait(name)),"removal cannot leave an orphaned bloodline gift");
            }));
        }
        foreach(bool active in new[]{false,true})
        yield return new("ClassGenes.FoundationConsumesBothGeneticPotentials"+(active?".Active":""),r=>Run(r,p=>{
            p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(active?"TM_ManaGene":"TM_HiddenMageGene"),true);
            p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(active?"TM_StaminaGene":"TM_SleepingWarriorGene"),true);
            var state=new TorannMagic.ClassProgression.ClassProgressionState(TorannMagic.ClassProgression.Vocation.Mage);
            state.TryAddFoundation("rwom.fire",TorannMagic.ClassProgression.DisciplineDomain.Magic);
            Set(p.GetComp<TorannMagic.ClassProgression.CompClassProgression>(),"state",state);
            typeof(TorannMagic.ClassProgression.ClassEnrollment).GetMethod("FinishMageFoundation",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[p]);
            TM_ClassGenes.SyncClassGenes(p);
            Check.Equal(false,p.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted)||p.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy),"foundation consumes both gifts");
            Check.Equal(true,p.genes.GetGene(DefDatabase<GeneDef>.GetNamed("TM_HiddenMageGene"))==null&&p.genes.GetGene(DefDatabase<GeneDef>.GetNamed("TM_SleepingWarriorGene"))==null,"no gene can recreate consumed potential");
            Check.Same(state,p.GetComp<TorannMagic.ClassProgression.CompClassProgression>().State,"foundation state retained");
            bool beta=TorannMagic.ClassProgression.ClassProgressionFeature.Enabled;
            try {
                TorannMagic.ClassProgression.ClassProgressionFeature.Enabled=false;
                TM_ClassGenes.SyncClassGenes(p);
                Check.Equal(false,p.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted)||p.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy),"beta opt-out cannot regenerate spent potentials");
                Check.Same(state,p.GetComp<TorannMagic.ClassProgression.CompClassProgression>().State,"beta opt-out retains chosen class data");
            } finally {TorannMagic.ClassProgression.ClassProgressionFeature.Enabled=beta;}
        }));
        yield return new("ClassGenes.NoviceManaDoesNotConsumePotential",r=>Run(r,p=>{
            Set(p.GetComp<TorannMagic.ClassProgression.CompClassProgression>(),"state",new TorannMagic.ClassProgression.ClassProgressionState(TorannMagic.ClassProgression.Vocation.Mage));
            p.needs=new Pawn_NeedsTracker(p);
            var gift=p.genes.AddGene(DefDatabase<GeneDef>.GetNamed("TM_HiddenMageGene"),true);
            p.needs.AllNeeds.Add(new Need_Mana(p){def=TorannMagicDefOf.TM_Mana});
            Check.Equal(true,TM_Calc.IsMagicUser(p),"fixture exposes novice mana through legacy magic-user query");
            TM_ClassGenes.SyncClassGenes(p);
            Check.Equal(true,gift.Active,"novice mana does not replace dormant gift with active class marker");
            Check.Equal(true,p.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted),"novice retains potential before class choice");
            Check.Equal(true,p.genes.GetGene(DefDatabase<GeneDef>.GetNamed("TM_ManaGene"))==null,"mana need alone is not a chosen class");
        }));
        yield return new("ClassGenes.DoublePotential",r=>Run(r,p=>{
            p.genes.AddGene(DefDatabase<GeneDef>.GetNamed("TM_HiddenMageGene"),true);
            p.genes.AddGene(DefDatabase<GeneDef>.GetNamed("TM_SleepingWarriorGene"),true);
            TM_ClassGenes.SyncClassGenes(p);
            Check.Equal(true,p.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted),"mage potential retained with warrior gift");
            Check.Equal(true,p.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy),"warrior potential retained with mage gift");
        }));
        foreach (string mode in new[]{"IncomingConflict","ExistingConflict","Suppressed","Compatible"})
            yield return new("ClassGenes.Enchantment."+mode,r=>Run(r,p=>Enchantment(p,mode)));
        foreach(string name in new[]{"TM_HiddenMageGene","TM_SleepingWarriorGene"})
        {
            yield return new("ClassGenes.Reactivate."+name,r=>Run(r,p=>{
                var def=DefDatabase<GeneDef>.GetNamed(name);
                var blocker=new GeneDef{defName="TestActiveBlocker",geneClass=typeof(Gene),exclusionTags=def.exclusionTags};
                DefDatabase<GeneDef>.Add(blocker);
                var dominant=p.genes.AddGene(blocker,true);
                var latent=p.genes.AddGene(def,false);
                Check.Equal(false,latent.Active,"native override blocks endogene at insertion");
                Check.Equal(false,p.story.traits.HasTrait(def.forcedTraits.Single().def),"initially overridden gene grants nothing");
                p.genes.RemoveGene(dominant);
                Check.Equal(true,latent.Active,"native removal reactivates endogene");
                Check.Equal(true,p.story.traits.HasTrait(def.forcedTraits.Single().def),"reactivation grants missing trait without load or manual repair");
            }));
            yield return new("ClassGenes.SpecialClass."+name,r=>Run(r,p=>{
                // Existing loaded class, including one omitted from TM_Data.MagicTraits.
                var empath=new TraitDef{defName="TM_Empath",degreeDatas=new(){new TraitDegreeData{degree=0,label="empath"}}};
                var retained=new Trait(empath);p.story.traits.GainTrait(retained);
                p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(name),true);
                Check.Equal(false,p.story.traits.HasTrait(DefDatabase<GeneDef>.GetNamed(name).forcedTraits.Single().def),"special class cannot gain a second potential");
                Check.Equal(true,p.story.traits.allTraits.Contains(retained)&&!retained.Suppressed,"special class remains active");
            }));
            foreach(bool natural in new[]{false,true})
                yield return new("ClassGenes.Save."+name+(natural?".Natural":".Genetic"),r=>Run(r,p=>SaveTraits(r,p,name,natural)));
            foreach(string mode in new[]{"Remove","Natural","Assigned","Suppressed"})
                yield return new("ClassGenes.Lifecycle."+name+"."+mode,r=>Run(r,p=>Lifecycle(p,name,mode)));
            yield return new("ClassGenes.XenogenePotential."+name,r=>Run(r,p=>{
                var gene=p.genes.AddGene(DefDatabase<GeneDef>.GetNamed(name),true);
                Check.NotNull(gene,"native xenogene added");
                TM_ClassGenes.SyncClassGenes(p);
                var trait=name=="TM_HiddenMageGene"?TorannMagicDefOf.TM_Gifted:TorannMagicDefOf.PhysicalProdigy;
                Check.Equal(true,p.story.traits.HasTrait(trait),"active xenogene grants corresponding potential");
                Check.Equal(true,p.genes.HasActiveGene(gene.def),"sync retains unconsumed gene");
                TM_ClassGenes.SyncClassGenes(p);
                Check.Equal(1,p.story.traits.allTraits.Count(t=>t.def==trait),"repeated sync cannot duplicate potential");
            }));
        }
    }
    private static void NativePawnSave(RuntimeFixture r,Pawn pawn,bool active=false,bool assigned=false)
    {
        // Later save fixtures must not resolve their pawns against this test's
        // synthetic race or kind after the native Scribe round trip completes.
        using var things = new SavedDefinitions<ThingDef>();
        using var kinds = new SavedDefinitions<PawnKindDef>();
        using var bodies = new SavedDefinitions<BodyDef>();
        using var hairs = new SavedDefinitions<HairDef>();
        using var heads = new SavedDefinitions<HeadTypeDef>();
        using var skills = new SavedDefinitions<SkillDef>();
        using var assignments = new SavedDefinitions<TimeAssignmentDef>();
        using var hediffs = new SavedDefinitions<HediffDef>();
        using var relations = new SavedDefinitions<PawnRelationDef>();
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(XenotypeDefOf));
        var names=active?new[]{"TM_ManaGene","TM_StaminaGene"}:new[]{"TM_HiddenMageGene","TM_SleepingWarriorGene"};
        foreach(var name in names)pawn.genes.AddGene(DefDatabase<GeneDef>.GetNamed(name),true);
        if(assigned) {
            var state=new TorannMagic.ClassProgression.ClassProgressionState(TorannMagic.ClassProgression.Vocation.Mage);
            state.TryAddFoundation("rwom.fire",TorannMagic.ClassProgression.DisciplineDomain.Magic);
            Set(pawn.GetComp<TorannMagic.ClassProgression.CompClassProgression>(),"state",state);
            var magic=pawn.GetCompAbilityUserMagic();
            magic.MagicData.MagicUserLevel=27;magic.MagicData.MagicUserXP=123;
            magic.MagicData.MagicAbilityPoints=9;
            var bolt=magic.MagicData.MagicPowersIF.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_RayofHope));
            Check.Equal(true,bolt.TMabilityDefs.Count>2,"ranked save fixture uses a real multi-rank spell");
            bolt.level=2;bolt.learned=true;
            magic.MagicData.GetSkill_Power(TorannMagicDefOf.TM_Firebolt).level=2;
            typeof(TorannMagic.ClassProgression.ClassEnrollment).GetMethod("FinishMageFoundation",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[pawn]);
            TM_ClassGenes.SyncClassGenes(pawn);
        }
        pawn.def.category=ThingCategory.Pawn;
        TorannMagic.ClassProgression.ClassProgressionBootstrap.EnsureComp(pawn.def);
        pawn.mindState=new Verse.AI.Pawn_MindState(pawn);
        void Register<T>(T def)where T:Def,new(){if(def!=null&&DefDatabase<T>.GetNamedSilentFail(def.defName)==null)DefDatabase<T>.Add(def);}
        foreach(var type in new[]{typeof(BodyDefOf),typeof(BeardDefOf),typeof(TattooDefOf)})RuntimeLog.EnsureHeadlessDefOfInitialized(type);
        pawn.story.hairDef=new HairDef{defName="GeneSaveHair"};Register(pawn.story.hairDef);
        pawn.story.headType=new HeadTypeDef{defName="GeneSaveHead"};Register(pawn.story.headType);
        Register(pawn.def);Register(pawn.kindDef);Register(pawn.def.race.body);
        foreach(var skill in pawn.skills.skills)Register(skill.def);
        foreach(var time in pawn.timetable.times)Register(time);
        int hediffId=25000;foreach(var h in pawn.health.hediffSet.hediffs){Register(h.def);h.loadID=hediffId++;}
        var references=new List<ILoadReferenceable>();
        foreach(var relation in pawn.relations.DirectRelations){Register(relation.def);references.Add(relation.otherPawn);}
        if(pawn.Faction!=null)references.Add(pawn.Faction);
        var lib=Assembly.Load("0Harmony");var id="rwom.tests.gene-pawn-save."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!,id)!;
        var callback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(AbilityUser.AbilityUserMod).GetMethod("InitializeComps_PostFix")!)!;
        harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(ThingWithComps).GetMethod("InitializeComps",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),null,callback,null,null]);
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
            string file=Path.Combine(r.WorkingDirectory,"gene-native-pawn.xml");Pawn loaded=null!;
            try{Scribe.saver.InitSaving(file,"regression");Scribe_Deep.Look(ref pawn,"pawn");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            try{
                Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref loaded,"pawn");
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach(var reference in references)directory.RegisterLoaded(reference);
                Scribe.loader.FinalizeLoading();
            }finally{Scribe.ForceStop();}
            RuntimeLog.AssertClean();
            if(assigned) {
                var state=loaded.GetComp<TorannMagic.ClassProgression.CompClassProgression>().State;
                Check.NotNull(state?.Find("rwom.fire"),"whole pawn retains selected fire discipline");
                var magic=loaded.GetCompAbilityUserMagic();
                Check.Equal(27,magic.MagicData.MagicUserLevel,"whole pawn preserves earned level");
                Check.Equal(123,magic.MagicData.MagicUserXP,"whole pawn preserves XP");
                Check.Equal(9,magic.MagicData.MagicAbilityPoints,"whole pawn preserves unspent points");
                var bolt=magic.MagicData.MagicPowersIF.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_RayofHope));
                Check.Equal(2,bolt.level,"whole pawn preserves spell rank");
                Check.Equal(true,bolt.learned,"whole pawn preserves learned flag");
                Check.Equal(2,magic.MagicData.GetSkill_Power(TorannMagicDefOf.TM_Firebolt).level,"whole pawn preserves invested upgrade points");
                TM_ClassGenes.SyncClassGenes(loaded);
                Check.Equal(false,loaded.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted)||loaded.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy),"load and sync cannot restore consumed potentials");
                return;
            }
            foreach(var name in names) {
                var def=DefDatabase<GeneDef>.GetNamed(name);var gene=loaded.genes.GetGene(def);
                Check.NotNull(gene,"whole pawn retains gene");Check.Same(loaded,gene.pawn,"whole pawn gene points to loaded pawn");
                Check.Same(gene,loaded.story.traits.GetTrait(def.forcedTraits.Single().def).sourceGene,"whole pawn retains trait source reference");
                loaded.genes.RemoveGene(gene);
                Check.Equal(false,loaded.story.traits.HasTrait(def.forcedTraits.Single().def),"loaded pawn removes genetic gift");
            }
        } finally {System.Globalization.CultureInfo.CurrentCulture=culture;harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
    }
    private static void CustomXenotype(Pawn p,bool beta,bool inherited,bool active=false)
    {
        bool previous=TorannMagic.ClassProgression.ClassProgressionFeature.Enabled;
        var priorHair=p.kindDef.forcedHairColor;
        try {
            TorannMagic.ClassProgression.ClassProgressionFeature.Enabled=beta;
            var skin=new GeneDef{defName="TestSkinGene",geneClass=typeof(Gene),endogeneCategory=EndogeneCategory.Melanin};
            var hair=new GeneDef{defName="TestHairGene",geneClass=typeof(Gene),endogeneCategory=EndogeneCategory.HairColor};
            DefDatabase<GeneDef>.Add(skin);DefDatabase<GeneDef>.Add(hair);
            var names=active?new[]{"TM_ManaGene","TM_StaminaGene","TM_ArcaneConduitGene"}:new[]{"TM_HiddenMageGene","TM_SleepingWarriorGene","TM_ArcaneConduitGene"};
            var gifts=names.Select(n=>DefDatabase<GeneDef>.GetNamed(n)).ToList();
            var custom=new RimWorld.CustomXenotype{name="Both gifts",inheritable=inherited,iconDef=new XenotypeIconDef{defName="TestGeneIcon"},genes=gifts.Concat(new[]{skin,hair}).ToList()};
            p.kindDef.forcedHairColor=UnityEngine.Color.black;
            var request=new PawnGenerationRequest{ForcedCustomXenotype=custom};
            typeof(PawnGenerator).GetMethod("GenerateGenes",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[p,new XenotypeDef{defName="TestBaseline",genes=new()},request]);
            foreach(var def in gifts) {
                var gene=p.genes.GetGene(def);
                Check.NotNull(gene,"custom xenotype gene applied");
                Check.Equal(true,gene.Active,"both potential families active");
                Check.Equal(!inherited,p.genes.Xenogenes.Contains(gene),"custom xenotype inheritance respected");
                var trait=p.story.traits.GetTrait(def.forcedTraits.Single().def);
                Check.NotNull(trait,"native generation grants gift without manual sync");
                Check.Same(gene,trait.sourceGene,"generated trait belongs to generated gene");
            }
            Check.Equal(true,p.GetComp<TorannMagic.ClassProgression.CompClassProgression>().State==null,"genes do not automatically assign a class or enroll beta");
        } finally {TorannMagic.ClassProgression.ClassProgressionFeature.Enabled=previous;p.kindDef.forcedHairColor=priorHair;}
    }
    private static void SaveTraits(RuntimeFixture r,Pawn p,string name,bool natural)
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(XenotypeDefOf));
        var geneDef=DefDatabase<GeneDef>.GetNamed(name);
        var traitDef=geneDef.forcedTraits.Single().def;
        if(natural)p.story.traits.GainTrait(new Trait(traitDef));
        var gene=p.genes.AddGene(geneDef,true);
        var originalGenes=p.genes;var originalTraits=p.story.traits;
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        string file=Path.Combine(r.WorkingDirectory,name+(natural?"-natural":"-genetic")+".xml");
        try {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
            var genes=p.genes;var traits=p.story.traits;
            try {
                Scribe.saver.InitSaving(file,"regression");
                Scribe_Deep.Look(ref genes,"genes",p);Scribe_Deep.Look(ref traits,"traits",p);
                Scribe.saver.FinalizeSaving();
            } finally {Scribe.ForceStop();}
            genes=null!;traits=null!;
            try {
                Scribe.loader.InitLoading(file);
                Scribe_Deep.Look(ref genes,"genes",p);Scribe_Deep.Look(ref traits,"traits",p);
                p.genes=genes;p.story.traits=traits;
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(p);
                Scribe.loader.FinalizeLoading();
            } finally {Scribe.ForceStop();}
            RuntimeLog.AssertClean();
            var restored=genes.GetGene(geneDef);
            Check.NotNull(restored,"gene restored through native tracker Scribe");
            var gift=traits.GetTrait(traitDef);
            Check.NotNull(gift,"trait survives native PostLoad cleanup");
            if(natural)Check.Equal(true,gift.sourceGene==null,"save keeps natural trait ownership");
            else Check.Same(restored,gift.sourceGene,"trait references the loaded gene, not the old instance");
            Check.Same(p,restored.pawn,"gene pawn cross-reference restored");
            genes.RemoveGene(restored);
            Check.Equal(natural,traits.HasTrait(traitDef),"gene removal after load respects trait ownership");
        } finally {p.genes=originalGenes;p.story.traits=originalTraits;System.Globalization.CultureInfo.CurrentCulture=culture;}
    }
    private static void Lifecycle(Pawn p,string name,string mode)
    {
        var def=DefDatabase<GeneDef>.GetNamed(name);
        var trait=def.forcedTraits.Single().def;
        Trait? natural=null;
        if(mode=="Natural") {natural=new Trait(trait);p.story.traits.GainTrait(natural);}
        if(mode=="Assigned") {
            var state=new TorannMagic.ClassProgression.ClassProgressionState(TorannMagic.ClassProgression.Vocation.Mage);
            state.TryAddFoundation("rwom.fire",TorannMagic.ClassProgression.DisciplineDomain.Magic);
            Set(p.GetComp<TorannMagic.ClassProgression.CompClassProgression>(),"state",state);
        }
        var gene=p.genes.AddGene(def,true);
        if(mode=="Assigned") {
            Check.Equal(false,p.story.traits.HasTrait(trait),"gene cannot grant consumed potential to assigned class");
            TM_ClassGenes.SyncClassGenes(p);
            Check.Equal(false,p.genes.GenesListForReading.Contains(gene),"assigned class consumes gene");
            return;
        }
        Check.Equal(true,p.story.traits.HasTrait(trait),"native AddGene alone grants potential");
        if(mode=="Suppressed") {
            var blocker=new GeneDef{defName="TestLatentOverride",geneClass=typeof(Gene)};
            DefDatabase<GeneDef>.Add(blocker);
            gene.OverrideBy(p.genes.AddGene(blocker,true));
            p.story.traits.RecalculateSuppression();
            TM_ClassGenes.SyncClassGenes(p);
            Check.Equal(false,p.story.traits.HasTrait(trait),"overridden gene suppresses its trait");
            Check.Equal(true,p.genes.GenesListForReading.Contains(gene),"suppression retains latent gene");
            return;
        }
        p.genes.RemoveGene(gene);
        Check.Equal(mode=="Natural",p.story.traits.HasTrait(trait),"removal removes only genetic trait");
        if(natural!=null)Check.Equal(true,p.story.traits.allTraits.Contains(natural)&&natural.sourceGene==null,"natural trait ownership preserved");
    }
    private static void Enchantment(Pawn pawn,string mode)
    {
        var old=TorannMagicDefOf.TM_ArcaneConduitTD;
        var incoming=new TraitDef{defName="TestGeneConduit",degreeDatas=new(){new TraitDegreeData{degree=0,label="conduit"}}};
        var existing=new TraitDef{defName="TestGeneExisting",degreeDatas=new(){new TraitDegreeData{degree=0,label="existing"}}};
        if(mode=="IncomingConflict")incoming.conflictingTraits=new(){existing};
        if(mode=="ExistingConflict")existing.conflictingTraits=new(){incoming};
        TorannMagicDefOf.TM_ArcaneConduitTD=incoming;
        var geneDef=DefDatabase<GeneDef>.GetNamed("TM_ArcaneConduitGene");
        var forced=geneDef.forcedTraits;
        geneDef.forcedTraits=new(){new GeneticTraitData{def=incoming,degree=0}};
        try {
            var retained=new Trait(existing);
            pawn.story.traits.GainTrait(retained);
            var gene=pawn.genes.AddGene(DefDatabase<GeneDef>.GetNamed("TM_ArcaneConduitGene"),true);
            if(mode=="Suppressed") {
                var blocker=new GeneDef{defName="TestGeneOverride",geneClass=typeof(Gene)};
                DefDatabase<GeneDef>.Add(blocker);
                gene.OverrideBy(pawn.genes.AddGene(blocker,true));
                pawn.story.traits.RecalculateSuppression();
            }
            TM_ClassGenes.SyncClassGenes(pawn);
            Check.Equal(mode=="Compatible",pawn.story.traits.HasTrait(incoming),"only active compatible gene grants trait");
            Check.Equal(true,pawn.story.traits.allTraits.Contains(retained)&&!retained.Suppressed,"existing trait retained active");
            TM_ClassGenes.SyncClassGenes(pawn);
            Check.Equal(mode=="Compatible"?1:0,pawn.story.traits.allTraits.Count(t=>t.def==incoming&&!t.Suppressed),"repeat sync preserves grant decision without duplicates");
            Check.Equal(true,pawn.genes.GenesListForReading.Contains(gene),"blocked enchantment gene is retained, not deleted");
        } finally {TorannMagicDefOf.TM_ArcaneConduitTD=old;geneDef.forcedTraits=forced;}
    }
    private static void Run(RuntimeFixture r,Action<Pawn> test)=>SpiritNativeResurrectionTests.WithPreparedCorpse(r,(pawn,corpse)=>{
        var priorDefs=DefDatabase<GeneDef>.AllDefsListForReading.ToArray();
        var geneProperties=priorDefs.ToDictionary(d=>d,d=>(d.forcedTraits,d.exclusionTags));
        var fields=typeof(TM_ClassGenes).GetFields(BindingFlags.NonPublic|BindingFlags.Static);
        var prior=fields.ToDictionary(f=>f,f=>f.GetValue(null));
        var giftTraits=new[]{TorannMagicDefOf.TM_Gifted,TorannMagicDefOf.PhysicalProdigy,TorannMagicDefOf.TM_ArcaneConduitTD,TorannMagicDefOf.TM_ManaWellTD,TorannMagicDefOf.TM_BoundlessTD,TorannMagicDefOf.TM_FaeBloodTD,TorannMagicDefOf.TM_GiantsBloodTD,TorannMagicDefOf.TM_EnlightenedTD};
        var degrees=giftTraits.ToDictionary(t=>t,t=>t.degreeDatas);
        var lib=Assembly.Load("0Harmony");var id="rwom.tests.classgenes."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!,id)!;
        var callback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(ClassGeneTests).GetMethod(nameof(Enabled),BindingFlags.NonPublic|BindingFlags.Static)!)!;
        try {
            foreach(var method in new[]{typeof(ModsConfig).GetProperty("BiotechActive")!.GetMethod!,typeof(ModLister).GetProperty("BiotechInstalled")!.GetMethod!})
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[method,callback,null,null,null]);
            var guard=typeof(TorannMagicMod.AdvancedClass_TraitGain_Patch).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static)!;
            var guardCallback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,guard)!;
            var after=typeof(TorannMagicMod.AdvancedClass_TraitGain_Patch).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static)!;
            var afterCallback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,after)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(TraitSet).GetMethod("GainTrait")!,guardCallback,afterCallback,null,null]);
            var removal=typeof(TM_ClassGenes).Assembly.GetType("TorannMagic.TM_ClassGeneRemovalPatch")?.GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static);
            if(removal!=null){
                var removalCallback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,removal)!;
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(Pawn_GeneTracker).GetMethod("RemoveGene")!,null,removalCallback,null,null]);
            }
            foreach(var name in new[]{"TM_ManaGene","TM_StaminaGene","TM_HiddenMageGene","TM_SleepingWarriorGene","TM_ArcaneConduitGene","TM_ManaWellGene","TM_BoundlessGene","TM_FaeBloodGene","TM_GiantsBloodGene","TM_EnlightenedGene"})
                if(DefDatabase<GeneDef>.GetNamedSilentFail(name)==null)DefDatabase<GeneDef>.Add(new GeneDef{defName=name,geneClass=typeof(Gene)});
            foreach(var t in giftTraits)t.degreeDatas=new(){new TraitDegreeData{degree=0,label=t.defName}};
            var xml=System.Xml.Linq.XDocument.Load(Path.Combine(r.Repository,"v1.7/Biotech/Defs/GeneDefs/Genes_TMagic.xml"));
            foreach(var element in xml.Root!.Elements("GeneDef")) {
                string name=element.Element("defName")!.Value;
                DefDatabase<GeneDef>.GetNamed(name).exclusionTags=element.Element("exclusionTags")?.Elements("li").Select(x=>x.Value).ToList();
                DefDatabase<GeneDef>.GetNamed(name).forcedTraits=element.Element("forcedTraits")?.Elements("li").Select(x=>new GeneticTraitData{def=giftTraits.Single(t=>t.defName==x.Element("def")!.Value),degree=int.Parse(x.Element("degree")!.Value)}).ToList();
            }
            typeof(TM_ClassGenes).GetField("geneLookupDone",BindingFlags.NonPublic|BindingFlags.Static)!.SetValue(null,false);
            pawn.genes=new Pawn_GeneTracker(pawn);
            // Remove the corpse fixture's unrelated priest path; preserve native trackers.
            var component=pawn.GetComp<TorannMagic.ClassProgression.CompClassProgression>();
            Set(component,"state",null!);
            Set(pawn.GetCompAbilityUserMagic(),"initializedIsMagicUser",true);Set(pawn.GetCompAbilityUserMagic(),"isMagicUser",false);
            test(pawn);
        } finally {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            foreach(var field in fields)field.SetValue(null,prior[field]);
            foreach(var entry in degrees)entry.Key.degreeDatas=entry.Value;
            foreach(var entry in geneProperties){entry.Key.forcedTraits=entry.Value.forcedTraits;entry.Key.exclusionTags=entry.Value.exclusionTags;}
            DefDatabase<GeneDef>.Clear();foreach(var def in priorDefs)DefDatabase<GeneDef>.Add(def);
        }
    });
}
