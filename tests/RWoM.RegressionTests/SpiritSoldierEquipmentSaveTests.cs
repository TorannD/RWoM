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
    // Full Pawn.ExposeData with real equipment ownership, not a component-only roundtrip.
    private static void SoldierNativeEquipmentSave(RuntimeFixture f) => SpiritNativeResurrectionTests.WithPreparedCorpse(f, (pawn, corpse) => SoldierEquipmentSave(f,pawn));
    private static void SoldierHostedEquipmentSave(RuntimeFixture f) => SpiritRebirthRitualTransactionTests.Run(f,c =>
    {
        using var fixture=new Fixture(f,c);
        SoldierEquipmentSave(f,c.Original,c.Creator,c.Map,c.Circle);
    });
    private static void SoldierEquipmentSave(RuntimeFixture f,Pawn pawn,Pawn? carrier=null,params ILoadReferenceable[] external)
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
        using var definitions=SuperSoldierCareerTests.Setup(f);
        pawn.def.category=ThingCategory.Pawn;
        ClassProgressionBootstrap.EnsureComp(pawn.def);
        var state=new ClassProgressionState(Vocation.Warrior);
        state.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);
        Set(ClassIdentity.Component(pawn),"state",state);
        var training=Production("SuperSoldierMartialPath").GetMethod("Training",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[state.Find("rwom.supersoldier")])!;
        Set(training,"School","TM_RifleSpec");
        var might=pawn.GetCompAbilityUserMight();
        if(might==null){might=new CompAbilityUserMight{parent=pawn,customIndex=-1};pawn.AllComps.Add(might);}
        var data=new MightData(might){MightUserLevel=23,MightUserXP=117,MightAbilityPoints=8};Set(might,"mightData",data);
        data.MightPowersSS.First(p=>p.abilityDef.defName=="TM_RifleSpec").learned=true;
        data.GetSkill_Power(TorannMagicDefOf.TM_RifleSpec).level=4;
        var weapon=SoldierReloadItem("SoldierSaveRifle","rifle");
        weapon.def.thingClass=typeof(ThingWithComps);
        weapon.def.useHitPoints=true;
        weapon.def.comps.Add(new CompProperties{compClass=typeof(CompEquippable)});
        weapon.def.comps.Add(new CompProperties{compClass=typeof(CompQuality)});
        weapon.thingIDNumber=73421;weapon.HitPoints=73;
        var quality=new CompQuality{parent=weapon};quality.Initialize(weapon.def.comps[1]);
        typeof(CompQuality).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Single(field=>field.FieldType==typeof(QualityCategory)).SetValue(quality,QualityCategory.Excellent);weapon.AllComps.Add(quality);
        var actor=carrier??pawn;
        actor.mindState=new Verse.AI.Pawn_MindState(actor);
        actor.equipment=new Pawn_EquipmentTracker(actor);actor.equipment.AddEquipment(weapon);
        void Register<T>(T def) where T:Def,new(){if(def!=null && DefDatabase<T>.GetNamedSilentFail(def.defName)==null)DefDatabase<T>.Add(def);}
        foreach(var type in new[]{typeof(BodyDefOf),typeof(BeardDefOf),typeof(TattooDefOf)})RuntimeLog.EnsureHeadlessDefOfInitialized(type);
        pawn.story.hairDef=new HairDef{defName="SoldierSaveHair"};Register(pawn.story.hairDef);
        pawn.story.headType=new HeadTypeDef{defName="SoldierSaveHead"};Register(pawn.story.headType);
        Register(weapon.def);Register(pawn.def);Register(pawn.kindDef);Register(pawn.def.race.body);
        foreach(var skill in pawn.skills.skills)Register(skill.def);
        foreach(var time in pawn.timetable.times)Register(time);
        int hediffId=24000;foreach(var h in pawn.health.hediffSet.hediffs){Register(h.def);h.loadID=hediffId++;}
        var references=new List<ILoadReferenceable>(external);
        foreach(var relation in pawn.relations.DirectRelations){Register(relation.def);references.Add(relation.otherPawn);}
        if(pawn.Faction!=null)references.Add(pawn.Faction);
        if(carrier!=null) {
            Register(TorannMagicDefOf.RawMagicyte);Register(ThingDefOf.Silver);
            var ideology=new Ideo();Set(ideology,"id",24101);Set(pawn.ideo,"ideo",ideology);references.Add(ideology);
            carrier.def.defName="SoldierHostedSaveBody";carrier.def.thingClass=typeof(Pawn);carrier.def.category=ThingCategory.Pawn;
            carrier.kindDef=pawn.kindDef;carrier.ageTracker=new Pawn_AgeTracker(carrier);
            carrier.def.race.lifeStageAges=pawn.def.race.lifeStageAges;
            Set(carrier.ageTracker,"ageBiologicalTicksInt",25L*GenDate.TicksPerYear);Set(carrier.ageTracker,"cachedLifeStageIndex",0);
            carrier.def.comps??=new();ClassProgressionBootstrap.EnsureComp(carrier.def);
            carrier.story.hairDef=pawn.story.hairDef;carrier.story.headType=pawn.story.headType;
            TorannMagicDefOf.TM_SpiritPossessionHD.comps=new(){new HediffCompProperties{compClass=typeof(HediffComp_SpiritPossession)}};
            Register(carrier.def);Register(TorannMagicDefOf.TM_SpiritPossessionHD);Register(PrisonerInteractionModeDefOf.MaintainOnly);
            foreach(var h in carrier.health.hediffSet.hediffs){Register(h.def);h.loadID=hediffId++;}
            var jobDef=new JobDef{defName="TM_SpiritCastSuppressingFire",driverClass=typeof(JobDriver_SpiritSoldierBurst)};Register(jobDef);
            var job=JobMaker.MakeJob(jobDef,pawn,new IntVec3(20,0,20),weapon);
            var driver=new JobDriver_SpiritSoldierBurst{pawn=carrier,job=job,ticksLeftThisToil=17};
            Set(driver,"curToilIndex",0);Set(carrier.jobs,"curJob",job);Set(carrier.jobs,"curDriver",driver);
        }
        var lib=Assembly.Load("0Harmony");var id="rwom.tests.soldier-pawn-save."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!,id)!;
        var callback=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(AbilityUserMod).GetMethod("InitializeComps_PostFix")!)!;
        harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(ThingWithComps).GetMethod("InitializeComps",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),null,callback,null,null]);
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
            string file=Path.Combine(f.WorkingDirectory,"soldier-equipped-pawn.xml");Pawn loaded=null!;
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
                Check.Same(owner,loaded.CurJob.targetA.Pawn,"saved cast targets loaded soul, not old object");
                Check.Same(loaded.equipment.Primary,loaded.CurJob.targetC.Thing,"saved cast binds loaded original weapon");
                Check.Equal(17,loaded.jobs.curDriver.ticksLeftThisToil,"saved cast keeps remaining warmup");
                Check.Same(loaded,loaded.jobs.curDriver.pawn,"saved driver belongs to carrier");
            }
            var saved=(MightData)typeof(CompAbilityUserMight).GetField("mightData",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner.GetCompAbilityUserMight())!;
            Check.Equal(23,saved.MightUserLevel,"equipped pawn level");Check.Equal(117,saved.MightUserXP,"equipped pawn XP");Check.Equal(8,saved.MightAbilityPoints,"unspent points");
            Check.Equal(4,saved.GetSkill_Power(TorannMagicDefOf.TM_RifleSpec).level,"specialization improvement");
            Check.Equal(true,saved.MightPowersSS.First(p=>p.abilityDef.defName=="TM_RifleSpec").learned,"specialization learned");
            var restoredTraining=Production("SuperSoldierMartialPath").GetMethod("Existing",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[ClassIdentity.Component(owner).State.Find("rwom.supersoldier")])!;
            Check.Equal("TM_RifleSpec",(string)restoredTraining.GetType().GetField("School",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(restoredTraining)!,"saved school");
            var equipped=loaded.equipment.Primary;Check.NotNull(equipped,"saved original equipped");
            Check.Same(weapon.def,equipped.def,"original definition");Check.Equal(73421,equipped.thingIDNumber,"original identity");Check.Equal(73,equipped.HitPoints,"weapon wear");
            Check.Equal(QualityCategory.Excellent,equipped.GetComp<CompQuality>().Quality,"weapon quality");
            Check.Same(loaded.equipment.GetDirectlyHeldThings(),equipped.holdingOwner,"loaded equipment owner");
            foreach(var comp in equipped.AllComps)Check.Same(equipped,comp.parent,"loaded comp belongs to actual original");
            Check.Equal(1,loaded.equipment.AllEquipmentListForReading.Count,"no extra synthetic weapon");
            Check.Equal(false,loaded.GetCompAbilityUserMight().equipmentContainer?.Contains(equipped)==true,"original not also in retained container");
        }finally{System.Globalization.CultureInfo.CurrentCulture=culture;harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
    }
}
