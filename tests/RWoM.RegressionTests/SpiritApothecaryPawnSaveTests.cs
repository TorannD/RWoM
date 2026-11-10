using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    // Full Pawn.ExposeData retains trackers and ability components, including the held soul.
    private static void ApothecaryNativePawnSave(RuntimeFixture f) => SpiritNativeResurrectionTests.WithPreparedCorpse(f, (pawn, corpse) => ApothecaryPawnSave(f,pawn));
    private static void ApothecaryHostedPawnSave(RuntimeFixture f) => SpiritRebirthRitualTransactionTests.Run(f,c =>
    {
        using var fixture=new Fixture(f,c);
        ApothecaryPawnSave(f,c.Original,c.Creator,c.Map,c.Circle);
    });
    private static void ApothecaryPawnSave(RuntimeFixture f,Pawn pawn,Pawn? carrier=null,params ILoadReferenceable[] external)
    {
        using var things=new SavedDefinitions<ThingDef>();
        using var kinds=new SavedDefinitions<PawnKindDef>();
        using var bodies=new SavedDefinitions<BodyDef>();
        using var hairs=new SavedDefinitions<HairDef>();
        using var heads=new SavedDefinitions<HeadTypeDef>();
        using var skills=new SavedDefinitions<SkillDef>();
        using var times=new SavedDefinitions<TimeAssignmentDef>();
        using var hediffs=new SavedDefinitions<HediffDef>();
        using var relations=new SavedDefinitions<PawnRelationDef>();
        using var jobs=new SavedDefinitions<JobDef>();
        using var definitions=ApothecaryCareerTests.Setup(f);
        pawn.def.category=ThingCategory.Pawn;
        ClassProgressionBootstrap.EnsureComp(pawn.def);
        var state=new ClassProgressionState(Vocation.Warrior);
        state.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
        Set(ClassIdentity.Component(pawn),"state",state);
        var might=pawn.GetCompAbilityUserMight();
        if(might==null){might=new CompAbilityUserMight{parent=pawn,customIndex=-1};pawn.AllComps.Add(might);}
        var data=new MightData(might){MightUserLevel=23,MightUserXP=117,MightAbilityPoints=8};Set(might,"mightData",data);
        var learned=data.MightPowersApothecary.First(p=>p.abilityDef==TorannMagicDefOf.TM_SoothingBalm);
        // Balm has one definition (rank 0); its paid improvements live in skill records.
        learned.learned=true;learned.level=0;learned.autocast=true;learned.ticksUntilNextCast=1234;
        foreach(var skill in data.MightPowerSkill_SoothingBalm)skill.level=2;
        foreach(var skill in data.MightPowerSkill_Herbalist)skill.level=3;
        var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=pawn};
        stock.Severity=37.25f;Set(stock,"herbPwr",3);Set(stock,"herbVer",3);Set(stock,"herbEff",3);
        pawn.health.hediffSet.hediffs.Add(stock);
        var actor=carrier??pawn;
        void Register<T>(T def) where T:Def,new(){if(def!=null && DefDatabase<T>.GetNamedSilentFail(def.defName)==null)DefDatabase<T>.Add(def);}
        foreach(var type in new[]{typeof(BodyDefOf),typeof(BeardDefOf),typeof(TattooDefOf)})RuntimeLog.EnsureHeadlessDefOfInitialized(type);
        pawn.story.hairDef=new HairDef{defName="ApothecarySaveHair"};Register(pawn.story.hairDef);
        pawn.story.headType=new HeadTypeDef{defName="ApothecarySaveHead"};Register(pawn.story.headType);
        Register(pawn.def);Register(pawn.kindDef);Register(pawn.def.race.body);
        foreach(var skill in pawn.skills.skills)Register(skill.def);
        foreach(var time in pawn.timetable.times)Register(time);
        int hediffId=24000;foreach(var h in pawn.health.hediffSet.hediffs){Register(h.def);h.loadID=hediffId++;}
        var references=new List<ILoadReferenceable>(external);
        foreach(var relation in pawn.relations.DirectRelations){Register(relation.def);references.Add(relation.otherPawn);}
        if(pawn.Faction!=null)references.Add(pawn.Faction);
        if(carrier!=null) {
            Register(TorannMagicDefOf.RawMagicyte);Register(ThingDefOf.Silver);
            var ideology=new Ideo();Set(ideology,"id",24101);Set(pawn.ideo,"ideo",ideology);references.Add(ideology);
            carrier.def.defName="ApothecaryHostedSaveBody";carrier.def.thingClass=typeof(Pawn);carrier.def.category=ThingCategory.Pawn;
            carrier.kindDef=pawn.kindDef;carrier.ageTracker=new Pawn_AgeTracker(carrier);
            carrier.def.race.lifeStageAges=pawn.def.race.lifeStageAges;
            Set(carrier.ageTracker,"ageBiologicalTicksInt",25L*GenDate.TicksPerYear);Set(carrier.ageTracker,"cachedLifeStageIndex",0);
            carrier.def.comps??=new();ClassProgressionBootstrap.EnsureComp(carrier.def);
            carrier.story.hairDef=pawn.story.hairDef;carrier.story.headType=pawn.story.headType;
            TorannMagicDefOf.TM_SpiritPossessionHD.comps=new(){new HediffCompProperties{compClass=typeof(HediffComp_SpiritPossession)}};
            Register(carrier.def);Register(TorannMagicDefOf.TM_SpiritPossessionHD);Register(PrisonerInteractionModeDefOf.MaintainOnly);
            foreach(var h in carrier.health.hediffSet.hediffs){Register(h.def);h.loadID=hediffId++;}
            Set(carrier.jobs,"curJob",null!);Set(carrier.jobs,"curDriver",null!);
        }
        var lib=Assembly.Load("0Harmony");var id="rwom.tests.apothecary-pawn-save."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!,id)!;
        var callback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(AbilityUserMod).GetMethod("InitializeComps_PostFix")!)!;
        harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(ThingWithComps).GetMethod("InitializeComps",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),null,callback,null,null]);
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
            string file=Path.Combine(f.WorkingDirectory,"apothecary-full-pawn.xml");Pawn loaded=null!;
            try{Scribe.saver.InitSaving(file,"regression");Scribe_Deep.Look(ref actor,"pawn");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            try{
                Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref loaded,"pawn");
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach(var reference in references)directory.RegisterLoaded(reference);
                Scribe.loader.FinalizeLoading();
            }finally{Scribe.ForceStop();}
            RuntimeLog.AssertClean();
            Pawn owner=loaded;
            if(carrier!=null) {
                var possession=loaded.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD).TryGetComp<HediffComp_SpiritPossession>();
                owner=(Pawn)possession.GetDirectlyHeldThings().Single();
                Check.Same(loaded,owner.ParentHolder.ParentHolder,"loaded soul remains held by the actual carrier");
                Check.Equal(true,SpiritRebirthState.TryGetPossessingSpirit(loaded,out var authorized),"loaded possession authorizes original soul");
                Check.Same(owner,authorized!,"loaded authority resolves exact saved owner");

            }
            var saved=(MightData)typeof(CompAbilityUserMight).GetField("mightData",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner.GetCompAbilityUserMight())!;
            Check.Equal(23,saved.MightUserLevel,"equipped pawn level");Check.Equal(117,saved.MightUserXP,"equipped pawn XP");Check.Equal(8,saved.MightAbilityPoints,"unspent points");
            var restoredPower=saved.MightPowersApothecary.First(p=>p.abilityDef==TorannMagicDefOf.TM_SoothingBalm);
            Check.Equal(true,restoredPower.learned && restoredPower.autocast,"learned spell and autocast preserved");
            Check.Equal(0,restoredPower.level,"single-definition spell rank preserved");
            Check.Equal(1234,restoredPower.ticksUntilNextCast,"next autocast timer preserved");
            Check.Equal(true,saved.MightPowerSkill_SoothingBalm.All(s=>s.level==2),"paid Balm improvements preserved");
            Check.Equal(true,saved.MightPowerSkill_Herbalist.All(s=>s.level==3),"herb improvements preserved");
            Check.NotNull(ClassIdentity.Component(owner).State.Find("rwom.apothecary"),"class path preserved");
            var restoredStock=owner.health.hediffSet.hediffs.OfType<Hediff_ApothecaryHerbs>().Single();
            Check.Same(owner,restoredStock.pawn,"stock belongs to restored source");
            Check.Equal(37.25f,restoredStock.Severity,"full pawn retains stock");
            Check.Equal(130f,restoredStock.MaxSeverity,"full pawn retains stock capacity");
            if(carrier!=null)Check.Equal(false,loaded.health.hediffSet.hediffs.OfType<Hediff_ApothecaryHerbs>().Any(),"carrier has no duplicated stock");

        }finally{System.Globalization.CultureInfo.CurrentCulture=culture;harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
    }
}
