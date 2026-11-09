using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static partial class SpiritMapCollapseTests
{
    private static bool ClinicalDiagnostic(string __0) { System.Console.WriteLine(__0);return false; }
    private static bool ClinicalDrawPosition(ref UnityEngine.Vector3 __result) { __result=UnityEngine.Vector3.zero;return false; }
    private static BodyPartRecord ApothecaryPatient(Pawn patient)
    {
            patient.kindDef=new PawnKindDef{race=patient.def,ignoresPainShock=true};patient.ageTracker??=new Pawn_AgeTracker(patient);
            patient.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult,healthScaleFactor=1}}];
            Set(patient.ageTracker,"cachedLifeStageIndex",0);
            var core=new BodyPartRecord{def=new BodyPartDef{defName="ClinicalCarrierCore",hitPoints=100,
                tags=[BodyPartTagDefOf.ConsciousnessSource,BodyPartTagDefOf.ManipulationLimbCore,BodyPartTagDefOf.MovingLimbCore]},coverage=1};
            patient.def.race.body=new BodyDef{defName="ClinicalCarrierBody",corePart=core};patient.def.race.body.ResolveReferences();
            patient.def.race.baseHealthScale=1;
            Set(patient.def.race,"bloodDef",(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)));
            patient.def.race.bleedRateFactor=1;
        return core;
    }
    private static IEnumerable<TestCase> HostedApothecaryCases()
    {
        yield return new("ApothecaryProjection.FullPawnSave.Native",ApothecaryNativePawnSave);
        yield return new("ApothecaryProjection.FullPawnSave.Hosted",ApothecaryHostedPawnSave);
        foreach(var test in ApothecaryCaravanCases()) yield return test;
        foreach(string mode in new[]{"Hosted","Depleted","NativeCarrier"})
        yield return new("ApothecaryProjection.PruningStock."+mode,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var setup=ApothecaryCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);state.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);Set(ClassIdentity.Component(c.Original),"state",state);
            foreach(var skill in c.Original.GetCompAbilityUserMight().MightData.MightPowerSkill_Herbalist)skill.level=3;
            var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};stock.Severity=50;c.Original.health.hediffSet.hediffs.Add(stock);
            var oldTick=Find.TickManager.TicksGame;
            try {
                Set(Find.TickManager,"ticksGameInt",150);
                Set(c.Creator.jobs,"curJob",new Verse.AI.Job{def=new JobDef{defName="PruneGauranlenTree"}});
                if(mode=="Depleted")Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                if(mode=="NativeCarrier")c.Creator.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Apothecary));
                stock.PostTick();
                float expected=50-.015f*.7f+(mode=="Hosted"?.4f*1.3f:0);
                Check.Equal(true,Math.Abs(stock.Severity-expected)<.0001f,"decay stays on soul; only its active carrier earns pruning herbs");
                Check.Equal(130f,stock.MaxSeverity,"source herbalist ranks set capacity");
                Check.Equal(false,c.Creator.health.hediffSet.HasHediff(stock.def),"pruning does not create second stock");
            } finally {Set(Find.TickManager,"ticksGameInt",oldTick);}
        }));
        foreach(var spell in new[]{"HerbalClarity","HerbalBliss","PoisonFlask","FireSuppressionFlask","FireSuppressionFlask_I","FireSuppressionFlask_II","FireSuppressionFlask_III"})
        yield return new("ApothecaryProjection.ProjectileOrder."+spell,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var setup=ApothecaryCareerTests.Setup(r);
            using var abilities=new SavedDefinitions<TMAbilityDef>();
            var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_"+spell)??r.AddAbility("TM_"+spell);
            var source=c.Original.GetCompAbilityUserMight();var power=new MightAbility(source){Def=def};
            source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            string order="TM_SpiritCast"+spell.Split('_')[0];
            var find=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.SpiritHostedMartial")!.GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
            Check.Same(power,find.Invoke(null,[c.Creator,c.Original,order]),"projectile order resolves exact owned rank");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(null,find.Invoke(null,[c.Creator,c.Original,order]),"depleted spirit cannot offer projectile order");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            var native=c.Creator.GetCompAbilityUserMight();native.AbilityData.Powers.Add(new MightAbility(native){Def=def});native.AbilityData.AllPowers=null;
            Check.Equal(null,find.Invoke(null,[c.Creator,c.Original,order]),"native carrier spell takes precedence");
        }));
        foreach(var spell in new[]{"HerbalClarity","HerbalBliss","PoisonFlask","FireSuppressionFlask","FireSuppressionFlask_I","FireSuppressionFlask_II","FireSuppressionFlask_III"})
        yield return new("ApothecaryProjection.NativeLaunch."+spell,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var setup=ApothecaryCareerTests.Setup(r);using var things=new SavedDefinitions<ThingDef>();
            using var language=new SpellItemEffectTests.LanguageScope(r.Repository);
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(ClamorDefOf));
            LanguageDatabase.activeLanguage.keyedReplacements["AU_CastSuccess"]=new LoadedLanguage.KeyedReplacement{key="AU_CastSuccess",value="Success"};
            var settings=TorannMagic.ModOptions.Settings.Instance;
            using var abilityDefs=new SavedDefinitions<TMAbilityDef>();
            var abilityDef=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_"+spell)??r.AddAbility("TM_"+spell);
            using var events=new SavedDefinitions<HistoryEventDef>();
            using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
            var priorEvent=TorannMagicDefOf.TM_UsedManeuver;
            var oldHediff=abilityDef.requiredHediff;float oldCost=abilityDef.hediffCost,oldEfficiency=abilityDef.efficiencyReductionPercent;bool oldConsume=abilityDef.consumeEnergy;
            var oldVerb=abilityDef.MainVerb;
            var oldDamageStat=StatDefOf.RangedWeapon_DamageMultiplier;
            var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
            string id="rwom.apothecary.launch."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                StatDefOf.RangedWeapon_DamageMultiplier=new StatDef{defName="RangedWeapon_DamageMultiplier",defaultBaseValue=1,workerClass=typeof(StatWorker)};
                var processor=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[typeof(MightAbility).Assembly.GetType("TorannMagic.SniperRoundSpawn")!])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
                using var nativeOwner=new Patch(typeof(Verb).GetProperty(nameof(Verb.DirectOwner))!.GetMethod!,nameof(NativeAbilityOwner));
                using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                c.Map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));c.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
                var presentation=typeof(MaterialPool).GetMethods().Where(m=>m.Name==nameof(MaterialPool.MatFrom)).Select(m=>new Patch(m,nameof(CancelNative))).ToArray();
                var mods=LoadedModManager.RunningModsListForReading.ToArray();
                try{LoadedModManager.RunningModsListForReading.Clear();RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle);}
                finally{LoadedModManager.RunningModsListForReading.AddRange(mods);foreach(var p in presentation)p.Dispose();}
                var source=c.Original.GetCompAbilityUserMight();Set(source,"initializedIsMightUser",true);Set(source,"isMightUser",true);
                if(source.Stamina==null)c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                c.Original.GetCompAbilityUserMagic().MagicData.magicPowerSkill_Cantrips=[new MagicPowerSkill("TM_Cantrips_pwr",""),new MagicPowerSkill("TM_Cantrips_ver","")];
                foreach(var skill in source.MightData.MightPowerSkill_PoisonFlask)skill.level=3;
                var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(AbilityUser.ProjectileDef_Ability));def.defName="ApothecaryNative"+spell;
                def.thingClass=spell=="PoisonFlask"?typeof(Projectile_PoisonFlask):spell.StartsWith("FireSuppressionFlask")?typeof(Projectile_FireSuppressionFlask):typeof(AbilityUser.Projectile_Ability);
                def.category=ThingCategory.Projectile;def.stackLimit=1;def.size=new IntVec2(1,1);def.comps=[];def.destroyable=true;def.drawerType=DrawerType.RealtimeOnly;
                def.projectile=new ProjectileProperties{damageDef=new DamageDef{defaultDamage=0},speed=60,explosionRadius=4};DefDatabase<ThingDef>.Add(def);
                abilityDef.MainVerb=new AbilityUser.VerbProperties_Ability{range=30,defaultProjectile=def,abilityDef=abilityDef,
                    targetParams=new TargetingParameters{canTargetSelf=true,canTargetPawns=true,canTargetLocations=true}};
                var effect=new HediffDef{defName="ApothecaryImpact"+spell,hediffClass=typeof(HediffWithComps),maxSeverity=10};
                float severity=spell=="HerbalClarity"?3:1;
                if(spell.StartsWith("Herbal")) {
                    ApothecaryPatient(c.Creator);
                    abilityDef.MainVerb.hediffsToApply=[new AbilityUser.ApplyHediffs{hediffDef=effect,applyChance=1,severity=severity}];
                }
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="ProjectileHostedUse"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                var path=new ClassProgressionState(Vocation.Warrior);path.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);Set(ClassIdentity.Component(c.Original),"state",path);
                var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};stock.Severity=80;c.Original.health.hediffSet.hediffs.Add(stock);
                abilityDef.requiredHediff=stock.def;abilityDef.hediffCost=20;abilityDef.consumeEnergy=true;abilityDef.efficiencyReductionPercent=0;abilityDef.MainVerb.SecondsToRecharge=4;
                var power=new MightAbility(source){Def=abilityDef};
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var execute=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.ApothecaryCast")!.GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static)!;
                bool Fire(LocalTargetInfo target)=>(bool)execute.Invoke(null,[c.Creator,c.Original,"TM_SpiritCast"+spell.Split('_')[0],target])!;
                Check.Equal(false,Fire(LocalTargetInfo.Invalid),"invalid projectile order is rejected");
                Check.Equal(80f,stock.Severity,"invalid order cannot spend herbs");
                stock.Severity=19;Check.Equal(false,Fire(c.Creator),"insufficient herbs cannot launch");
                Check.Equal(19f,stock.Severity,"rejected launch does not refund unpaid herbs");
                stock.Severity=80;
                var hosted=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.SpiritHostedMartial")!;
                string order="TM_SpiritCast"+spell.Split('_')[0];
                Check.Same(power,hosted.GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[c.Creator,c.Original,order]),"owned projectile command resolves source power");
                Check.Equal(true,(bool)hosted.GetMethod("Ready",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[c.Creator,power,order])!,"funded projectile is ready");
                Check.Equal(true,(bool)execute.DeclaringType!.GetMethod("Valid",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[c.Creator,power,new LocalTargetInfo(c.Creator)])!,"physical projectile target is valid");
                Check.Equal(true,Fire(c.Creator),"funded hosted command launches through native JecsLite path");
                Check.Equal(60f,stock.Severity,"native payment consumes source herbs exactly once");
                Check.Equal(true,power.CooldownTicksLeft>0,"source power owns launch cooldown");
                Check.Equal(false,Fire(c.Creator),"cooldown rejects duplicate launch");
                Check.Equal(60f,stock.Severity,"duplicate launch cannot pay twice");
                var shot=(Projectile)c.Map.thingGrid.ThingsListAt(c.Creator.Position).Single(t=>t.def==def);
                Check.Same(c.Creator,shot.Launcher,"projectile keeps physical launcher");
                Check.Equal(true,shot.Spawned && !shot.Destroyed,"projectile actually exists");
                if(shot is Projectile_FireSuppressionFlask fireFlask) ApothecaryFireImpact(r,spell,c.Map,fireFlask);
                if(spell.StartsWith("Herbal")) {
                    using var debugPuff=new Patch(typeof(Pawn_DrawTracker).GetMethod(nameof(Pawn_DrawTracker.Notify_DebugAffected))!,nameof(CancelNative));
                    using var textMote=new Patch(typeof(MoteMaker).GetMethod(nameof(MoteMaker.ThrowText),[typeof(UnityEngine.Vector3),typeof(Map),typeof(string),typeof(float)])!,nameof(CancelNative));
                    typeof(AbilityUser.Projectile_AbilityBase).GetMethod("Impact",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(shot,[c.Creator,false]);
                    var applied=c.Creator.health.hediffSet.GetFirstHediffOfDef(effect);
                    Check.Equal(true,applied!=null,"native projectile impact applies the carried remedy");
                    Check.Equal(severity,applied!.Severity,"impact retains configured remedy strength");
                    Check.Equal(false,c.Original.health.hediffSet.HasHediff(effect),"remedy affects physical patient, not held soul");
                    Check.Equal(true,shot.Destroyed,"remedy projectile finishes after impact");
                }
                if(shot is Projectile_PoisonFlask poison) ApothecaryPoisonImpact(r,c.Map,poison,c.Creator);
                if(shot is Projectile_PoisonFlask)Check.Equal(540,(int)typeof(Projectile_PoisonFlask).GetField("duration",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"real launch captures exact soul training");
            } finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);abilityDef.MainVerb=oldVerb;abilityDef.requiredHediff=oldHediff;abilityDef.hediffCost=oldCost;abilityDef.consumeEnergy=oldConsume;abilityDef.efficiencyReductionPercent=oldEfficiency;TorannMagicDefOf.TM_UsedManeuver=priorEvent;StatDefOf.RangedWeapon_DamageMultiplier=oldDamageStat;TorannMagic.ModOptions.Settings.Instance=settings;}
        }));
        foreach(bool elixir in new[]{false,true})
        yield return new("ApothecaryProjection.ClinicalEffect."+(elixir?"Elixir":"Balm"),r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var definitions=ApothecaryCareerTests.Setup(r);
            using var diagnostics=new Patch(typeof(TorannMagic.Compatibility.TM_Log).GetMethod("Error",new[]{typeof(string)})!,nameof(ClinicalDiagnostic));
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ClinicalDrawPosition));
            using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
            var core=ApothecaryPatient(c.Creator);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var priorEvent=TorannMagicDefOf.TM_UsedManeuver;
            using var events=new SavedDefinitions<HistoryEventDef>();
            var old=elixir?TorannMagicDefOf.TM_HerbalElixirHD:TorannMagicDefOf.TM_SoothingBalmHD;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="ClinicalHostedUse"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                var source=c.Original.GetCompAbilityUserMight();Set(source,"initializedIsMightUser",true);Set(source,"isMightUser",true);
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                c.Original.GetCompAbilityUserMagic().MagicData.magicPowerSkill_Cantrips=[new MagicPowerSkill("TM_Cantrips_pwr",""),new MagicPowerSkill("TM_Cantrips_ver","")];
                var abilityDef=elixir?TorannMagicDefOf.TM_Elixir:TorannMagicDefOf.TM_SoothingBalm;
                var skills=elixir?source.MightData.MightPowerSkill_Elixir:source.MightData.MightPowerSkill_SoothingBalm;
                foreach(var skill in skills)skill.level=3;
                var effect=new HediffDef{defName="ClinicalHostedEffect",hediffClass=typeof(HediffWithComps),maxSeverity=10,initialSeverity=0};
                if(elixir) {effect.comps=[new HediffCompProperties{compClass=typeof(MightAbility).Assembly.GetType("TorannMagic.HediffComp_HerbalElixir")!}];TorannMagicDefOf.TM_HerbalElixirHD=effect;}
                else TorannMagicDefOf.TM_SoothingBalmHD=effect;
                var power=new MightAbility(source){Def=abilityDef};
                AbilityUser.Verb_UseAbility verb=elixir?new Verb_Elixir():new Verb_SoothingBalm();
                verb.caster=c.Creator;verb.Ability=power;verb.verbProps=new AbilityUser.VerbProperties_Ability();
                Check.Equal(true,verb is Verb_SpiritTargetable,"direct clinical effect exposes hosted completion independently of projectile return");
                var cast=typeof(Verb_SpiritTargetable).GetMethod("CastFromHost",BindingFlags.Instance|BindingFlags.NonPublic)!;
                Check.Equal(false,(bool)cast.Invoke(verb,[LocalTargetInfo.Invalid])!,"invalid patient rejected without applying effect");
                Check.Same(c.Creator,verb.CasterPawn,"physical caster retained");
                Check.Equal(true,c.Creator.Spawned,"clinical patient spawned");
                Check.Equal(false,c.Creator.Dead,"clinical patient alive");
                Check.Equal(true,(bool)cast.Invoke(verb,[new LocalTargetInfo(c.Creator)])!,"hosted clinical effect completes");
                var applied=c.Creator.health.hediffSet.GetFirstHediffOfDef(effect);
                Check.Equal(true,applied!=null,"effect applied to physical patient");
                Check.Equal(true,Math.Abs(applied!.Severity-(elixir?1f:.21f))<.0001f,"effect uses source training");
                Check.Equal(false,c.Original.health.hediffSet.HasHediff(effect),"soul is not the physical patient");
                var wounds=new List<Hediff_Injury>();
                for(int i=0;i<2;i++) {
                    var wound=new Hediff_Injury{def=new HediffDef{defName="ClinicalSmallWound"+i,injuryProps=new InjuryProps(),tendable=true,maxSeverity=100},pawn=c.Creator,Part=core,loadID=9950+i};
                    Set(wound,"severityInt",elixir?.001f:1f);c.Creator.health.hediffSet.hediffs.Add(wound);wounds.Add(wound);
                    if(!elixir)Check.Equal(true,wound.TendableNow(),$"balm fixture has treatable wound: visible={wound.Visible},flesh={c.Creator.RaceProps.IsFlesh},bleed={c.Creator.health.CanBleed}");
                }
                if(elixir) {
                    var comp=((HediffWithComps)applied).comps.Single();
                    Check.Equal(3f,(float)comp.GetType().GetField("pwrVal")!.GetValue(comp)!,"elixir stores source healing power");
                    Check.Equal(3f,(float)comp.GetType().GetField("verVal")!.GetValue(comp)!,"elixir stores source versatility");
                    comp.GetType().GetMethod("TickAction")!.Invoke(comp,null);
                } else {
                    using var visuals=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
                    Check.Equal(true,(bool)cast.Invoke(verb,[new LocalTargetInfo(c.Creator)])!,"balm handles complete healing of multiple wounds");
                }
                Check.Equal(true,wounds.All(w=>w.Severity<=0),"both wounds healed; native health tick removes zero-severity injuries later: "+string.Join(",",wounds.Select(w=>w.Severity)));
                var pathState=new ClassProgressionState(Vocation.Warrior);pathState.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
                Set(ClassIdentity.Component(c.Original),"state",pathState);
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};
                stock.Severity=80;c.Original.health.hediffSet.hediffs.Add(stock);
                var order=elixir?"TM_SpiritCastElixir":"TM_SpiritCastBalm";
                var api=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.SpiritHostedMartial")!;
                var find=api.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,order]),"clinical command resolves exact soul power");
                var originalVerb=abilityDef.MainVerb;var originalHediff=abilityDef.requiredHediff;float originalCost=abilityDef.hediffCost;
                bool originalConsume=abilityDef.consumeEnergy;float originalEfficiency=abilityDef.efficiencyReductionPercent;
                try {
                    abilityDef.MainVerb=new AbilityUser.VerbProperties_Ability{range=1.9f,SecondsToRecharge=4};
                    abilityDef.requiredHediff=stock.def;abilityDef.hediffCost=20;abilityDef.consumeEnergy=true;abilityDef.efficiencyReductionPercent=0;
                    var ready=api.GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!;
                    var execute=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.ApothecaryCast")!.GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic)!;
                    bool Fire(LocalTargetInfo target)=>(bool)execute.Invoke(null,[c.Creator,c.Original,order,target])!;
                    stock.Severity=19;Check.Equal(false,(bool)ready.Invoke(null,[c.Creator,power,order])!,"low herbs prevent hosted treatment");
                    stock.Severity=80;Check.Equal(false,Fire(LocalTargetInfo.Invalid),"invalid target cannot pay");
                    Check.Equal(80f,stock.Severity,"invalid treatment preserves stock");
                    Check.Equal(true,Fire(c.Creator),"funded hosted treatment executes");
                    Check.Equal(60f,stock.Severity,"actual ability pays source herbs once");
                    Check.Equal(true,power.CooldownTicksLeft>0,"cooldown remains on source ability");
                    Check.Equal(false,Fire(c.Creator),"cooldown rejects immediate duplicate treatment");
                    Check.Equal(60f,stock.Severity,"duplicate cannot pay twice");
                    power.CooldownTicksLeft=0;Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    Check.Equal(false,Fire(c.Creator),"depleted possession cannot spend remaining herbs");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
                    if(elixir) {
                        using var jobs=new SavedDefinitions<JobDef>();
                        using var endJob=new Patch(typeof(Verse.AI.JobDriver).GetMethod(nameof(Verse.AI.JobDriver.EndJobWith))!,nameof(CancelNative));
                        var jobDef=new JobDef{defName=order,driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
                        var job=JobMaker.MakeJob(jobDef,c.Original,c.Creator);
                        var driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=job};
                        Set(c.Creator.jobs,"curJob",job);Set(c.Creator.jobs,"curDriver",driver);
                        Verse.AI.Toil[] Toils(Verse.AI.JobDriver d)=>((IEnumerable<Verse.AI.Toil>)typeof(JobDriver_SpiritHostedMartial)
                            .GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(d,null)!).ToArray();
                        Toils(driver)[0].initAction();Check.Equal(60f,stock.Severity,"warmup cannot spend herbs");
                        var loaded=RoundTripSummon(r,"apothecary-clinical-warmup",new MonkJobSave{Body=c.Creator,Order=job,Driver=driver},c.Creator,c.Original);
                        Set(c.Creator.jobs,"curJob",loaded.Order);Set(c.Creator.jobs,"curDriver",loaded.Driver);
                        Toils(loaded.Driver)[1].initAction();Check.Equal(40f,stock.Severity,"resumed job pays once");
                        loaded=RoundTripSummon(r,"apothecary-clinical-attempted",loaded,c.Creator,c.Original);
                        Set(c.Creator.jobs,"curJob",loaded.Order);Set(c.Creator.jobs,"curDriver",loaded.Driver);
                        power.CooldownTicksLeft=0;
                        Toils(loaded.Driver)[1].initAction();Check.Equal(40f,stock.Severity,"saved attempted job cannot replay payment even after cooldown expires");
                    }
                } finally {abilityDef.MainVerb=originalVerb;abilityDef.requiredHediff=originalHediff;abilityDef.hediffCost=originalCost;abilityDef.consumeEnergy=originalConsume;abilityDef.efficiencyReductionPercent=originalEfficiency;}
            } finally {TorannMagicDefOf.TM_UsedManeuver=priorEvent;TorannMagic.ModOptions.Settings.Instance=settings;if(elixir)TorannMagicDefOf.TM_HerbalElixirHD=old;else TorannMagicDefOf.TM_SoothingBalmHD=old;}
        }));
        yield return new("ApothecaryProjection.PoisonFogLifetime",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var definitions=new SavedDefinitions<ThingDef>();
            var xml=System.Xml.Linq.XDocument.Load(Path.Combine(r.Repository,"v1.7/Defs/DamageDefs/TM_Damages.xml"));
            Check.Equal(typeof(DamageWorker_PoisonFlask).FullName,xml.Root!.Elements("DamageDef")
                .Single(x=>(string?)x.Element("defName")=="TM_Poison").Element("workerClass")?.Value,"shipped damage definition selects instance fog worker");
            var previous=TorannMagicDefOf.Fog_Poison;
            try {
                var fog=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                fog.defName="ApothecaryLifetimeGas";fog.thingClass=typeof(Gas);fog.category=ThingCategory.Gas;
                fog.comps=[];fog.size=new IntVec2(1,1);fog.stackLimit=1;fog.destroyable=true;fog.drawerType=DrawerType.RealtimeOnly;
                fog.gas=new GasProperties{expireSeconds=new FloatRange(5,5)};DefDatabase<ThingDef>.Add(fog);
                TorannMagicDefOf.Fog_Poison=fog;
                var shot=new Projectile_PoisonFlask();Set(shot,"duration",540);
                var spawn=typeof(Projectile_PoisonFlask).GetMethod("SpawnFog",BindingFlags.Instance|BindingFlags.NonPublic);
                Check.Equal(true,spawn!=null,"flask must set lifetime on each gas instance, not shared definition");
                var cell=c.Creator.Position+new IntVec3(2,0,0);
                var damage=new DamageDef{defName="ApothecaryFogDamage",workerClass=typeof(DamageWorker_PoisonFlask)};
                var explosion=new Explosion{instigator=shot,damType=damage,damAmount=0,doVisualEffects=false};
                Set(explosion,"mapIndexOrState",(sbyte)c.Map.Index);
                void Affect(bool visuals)=>damage.Worker.ExplosionAffectCell(explosion,cell,[],[],visuals);
                Affect(false);Check.Equal(null,cell.GetGas(c.Map),"damage-only neighbor does not spawn fog");
                explosion.postExplosionSpawnThingDef=fog;Affect(true);
                Check.Equal(null,cell.GetGas(c.Map),"legacy native post-spawn instruction cannot duplicate gas");
                explosion.postExplosionSpawnThingDef=null;explosion.instigator=c.Creator;Affect(true);
                Check.Equal(null,cell.GetGas(c.Map),"other poison sources cannot create flask gas");
                explosion.instigator=shot;Affect(true);var trained=(Gas)cell.GetGas(c.Map);
                Check.Equal(Find.TickManager.TicksGame+540,trained.destroyTick,"trained fog uses captured lifetime");
                var loaded=RoundTripSummon(r,"apothecary-fog",trained,c.Creator);
                Check.Equal(trained.destroyTick,loaded.destroyTick,"native gas save preserves exact expiration");
                Check.Equal(5f,fog.gas.expireSeconds.min,"shared gas minimum unchanged");
                Check.Equal(5f,fog.gas.expireSeconds.max,"shared gas maximum unchanged");
                var ordinary=(Gas)GenSpawn.Spawn(fog,cell,c.Map);
                Check.Equal(Find.TickManager.TicksGame+300,ordinary.destroyTick,"ordinary trap gas retains its own lifetime");
                Set(shot,"duration",360);spawn!.Invoke(shot,[c.Map,cell]);
                Check.Equal(Find.TickManager.TicksGame+360,((Gas)cell.GetGas(c.Map)).destroyTick,"next untrained flask does not inherit earlier bonus");
            } finally {TorannMagicDefOf.Fog_Poison=previous;}
        }));
        yield return new("ApothecaryProjection.PoisonCapturedTraining",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var definitions=ApothecaryCareerTests.Setup(r);using var things=new SavedDefinitions<ThingDef>();
            var previousSettings=TorannMagic.ModOptions.Settings.Instance;
            try {
            TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
            var source=c.Original.GetCompAbilityUserMight();Set(source,"initializedIsMightUser",true);Set(source,"isMightUser",true);
            c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
            c.Original.GetCompAbilityUserMagic().MagicData.magicPowerSkill_Cantrips=[new MagicPowerSkill("TM_Cantrips_pwr",""),new MagicPowerSkill("TM_Cantrips_ver","")];
            var skills=source.MightData.MightPowerSkill_PoisonFlask;
            foreach(var skill in skills)skill.level=3;
            var power=new MightAbility(source){Def=TorannMagicDefOf.TM_PoisonFlask};
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="ApothecaryCapturedPoison";def.thingClass=typeof(Projectile_PoisonFlask);def.category=ThingCategory.Projectile;
            def.comps=[];def.size=new IntVec2(1,1);def.stackLimit=1;def.destroyable=true;def.drawerType=DrawerType.RealtimeOnly;
            def.projectile=new ProjectileProperties{speed=20,explosionRadius=4};DefDatabase<ThingDef>.Add(def);
            var spawn=typeof(MightAbility).Assembly.GetType("TorannMagic.SniperRoundSpawn")!.GetMethod("Spawn",BindingFlags.Static|BindingFlags.NonPublic)!;
            var verb=new Verb_SB{caster=c.Creator,Ability=power};
            var shot=(Projectile_PoisonFlask)spawn.Invoke(null,[def,c.Creator.Position,c.Map,WipeMode.Vanish,verb])!;
            SpellItemEffectTests.SetProjectileField(shot,"launcher",c.Creator);
            object Value(object x,string name)=>typeof(Projectile_PoisonFlask).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(x)!;
            Check.Equal(540,(int)Value(shot,"duration"),"launch captures soul's power before possession can end");
            Check.Equal(6.4f,(float)Value(shot,"radius"),"launch captures soul's versatility");
            foreach(var skill in skills)skill.level=0;
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            var loaded=RoundTripSummon(r,"apothecary-trained-airborne",shot,c.Creator);
            typeof(Projectile_PoisonFlask).GetMethod("CaptureTraining",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(loaded,[c.Creator]);
            Check.Equal(540,(int)Value(loaded,"duration"),"airborne save retains captured lifetime despite changed owner");
            Check.Equal(6.4f,(float)Value(loaded,"radius"),"airborne save retains captured radius");
            Check.Equal(false,(bool)Value(loaded,"initialized"),"capture does not prematurely impact");
            } finally {TorannMagic.ModOptions.Settings.Instance=previousSettings;}
        }));
        foreach(string scenario in new[]{"HostedGoldenHour","GoldenHourAdmission","DepletedGoldenHour"})
        yield return new("ApothecaryProjection."+scenario,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var definitions=ApothecaryCareerTests.Setup(r);
            // Use a pain-immune carrier for the payment/pulse contract. Native
            // health updates still run; fixture stat defaults must not down it.
            c.Creator.kindDef=new PawnKindDef{race=c.Creator.def,ignoresPainShock=true};c.Creator.ageTracker??=new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult,healthScaleFactor=1}}];
            Set(c.Creator.ageTracker,"cachedLifeStageIndex",0);
            var core=new BodyPartRecord{def=new BodyPartDef{defName="ApothecaryCarrierCore",hitPoints=100,
                tags=[BodyPartTagDefOf.ConsciousnessSource,BodyPartTagDefOf.ManipulationLimbCore,BodyPartTagDefOf.MovingLimbCore]},coverage=1};
            c.Creator.def.race.body=new BodyDef{defName="ApothecaryCarrierBody",corePart=core};c.Creator.def.race.body.ResolveReferences();
            c.Creator.def.race.baseHealthScale=1;
            var state=new ClassProgressionState(Vocation.Warrior);state.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
            Set(ClassIdentity.Component(c.Original),"state",state);
            var path=state.Find("rwom.apothecary")!;Set(path,"stage",InitiationStage.Ascended);
            var data=Activator.CreateInstance(typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.ApothecaryProgress")!,true)!;
            Set(data,"GoldenHour",true);Set(path,"MartialData",data);
            var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};
            c.Original.health.hediffSet.hediffs.Add(stock);
            var source=c.Original.GetCompAbilityUserMight();
            var kit=new ApothecaryKitAbility(source){Def=DefDatabase<TMAbilityDef>.GetNamed("TM_ApothecaryGoldenHour")};
            source.AbilityData.Powers.Add(kit);source.AbilityData.AllPowers=null;
            if(scenario=="GoldenHourAdmission")
            {
                var api=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.SpiritHostedMartial")!;
                var find=api.GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Same(kit,find.Invoke(null,[c.Creator,c.Original,"TM_ApothecaryGoldenHourCast"]),"hosted kit resolves exact source");
                var native=c.Creator.GetCompAbilityUserMight();var local=new ApothecaryKitAbility(native){Def=kit.Def};
                native.AbilityData.Powers.Add(local);native.AbilityData.AllPowers=null;
                Check.Equal(null,find.Invoke(null,[c.Creator,c.Original,"TM_ApothecaryGoldenHourCast"]),"native carrier ability keeps priority");
                native.AbilityData.Powers.Remove(local);native.AbilityData.AllPowers=null;
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(null,find.Invoke(null,[c.Creator,c.Original,"TM_ApothecaryGoldenHourCast"]),"depleted source cannot start kit");
                return;
            }
            ApothecaryClinicalTests.Kit(r,c.Creator,source,stock,c.Original,scenario=="DepletedGoldenHour" ?
                ()=>Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true) : null);
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(stock.def),"channel does not create carrier herbs");
        }));
        yield return new("ApothecaryProjection.AntitoxinAdmission",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var definitions=ApothecaryCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);state.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
            Set(ClassIdentity.Component(c.Original),"state",state);
            Set(state.Find("rwom.apothecary")!,"stage",InitiationStage.Adept);
            var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};
            c.Original.health.hediffSet.hediffs.Add(stock);stock.Severity=39;
            var source=c.Original.GetCompAbilityUserMight();
            var power=new ApothecaryAntitoxinAbility(source){Def=DefDatabase<TMAbilityDef>.GetNamed("TM_ApothecaryAntitoxin")};
            source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            var api=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.SpiritHostedMartial")!;
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static;
            object? Find()=>api.GetMethod("Find",flags)!.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastAntitoxin"]);
            bool Ready()=>(bool)api.GetMethod("Ready",flags)!.Invoke(null,[c.Creator,power,"TM_SpiritCastAntitoxin"])!;
            Check.Same(power,Find(),"hosted antitoxin resolves exact soul ability");
            Check.Equal(true,Ready(),"herb-funded treatment does not require stamina");
            power.CooldownTicksLeft=1;Check.Equal(false,Ready(),"cooldown blocks command");power.CooldownTicksLeft=0;
            stock.Severity=19;Check.Equal(false,Ready(),"short herbs block command");stock.Severity=39;
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(null,Find(),"depletion removes source");Check.Equal(false,Ready(),"stale power cannot spend after depletion");
        }));
        yield return new("ApothecaryProjection.HostedDetox",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var definitions=ApothecaryCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);state.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
            Set(ClassIdentity.Component(c.Original),"state",state);
            Set(state.Find("rwom.apothecary")!,"stage",InitiationStage.Adept);
            var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};
            c.Original.health.hediffSet.hediffs.Add(stock);
            var source=c.Original.GetCompAbilityUserMight();
            var power=new ApothecaryAntitoxinAbility(source){Def=DefDatabase<TMAbilityDef>.GetNamed("TM_ApothecaryAntitoxin")};
            ApothecaryClinicalTests.Detox(c.Creator,stock,c.Original,power);
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(stock.def),"antitoxin cannot create duplicate carrier herbs");
        }));
        foreach(string mode in new[]{"HostedHarvest","DepletedHarvest","NativeHarvest","LegacyHarvest","DepletedLegacyHarvest","NativeLegacyHarvest"})
        yield return new("ApothecaryProjection."+mode,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=ApothecaryCareerTests.Setup(r);
            bool legacy=mode.Contains("Legacy");
            var state=new ClassProgressionState(Vocation.Warrior);
            if(legacy)c.Original.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Apothecary));
            else state.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
            Set(ClassIdentity.Component(c.Original),"state",state);
            _=c.Original.GetCompAbilityUserMight().MightData;
            var stock=new Hediff_ApothecaryHerbs {def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};stock.Severity=0;
            c.Original.health.hediffSet.hediffs.Add(stock);
            if(mode.StartsWith("Depleted"))Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            if(mode.StartsWith("Native"))
            {
                var nativeState=new ClassProgressionState(Vocation.Warrior);
                if(legacy)c.Creator.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Apothecary));
                else nativeState.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);
                Set(ClassIdentity.Component(c.Creator),"state",nativeState);
                var nativeStock=new Hediff_ApothecaryHerbs{def=stock.def,pawn=c.Creator};nativeStock.Severity=0;
                c.Creator.health.hediffSet.hediffs.Add(nativeStock);
                ApothecaryClinicalTests.Harvest(c.Creator,nativeStock,legacy:legacy);
                Check.Equal(0f,stock.Severity,"native carrier harvest cannot fill the soul's stock as well");
                return;
            }
            ApothecaryClinicalTests.Harvest(c.Creator,stock,c.Original,!mode.StartsWith("Depleted"),legacy);
            Check.Same(c.Original,stock.pawn,"harvest keeps exact original resource owner");
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(stock.def),"carrier does not receive a duplicate stock");
        }));
        foreach(string field in new[]{"initialized","duration","lastStrike","LegacyCloud","LegacyAirborne","Resume"})
            yield return new("ApothecaryProjection.PoisonSave."+field,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
                using var fixture=new Fixture(r,c);
                using var definitions=new SavedDefinitions<ThingDef>();
                var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName="ApothecarySavePoison";def.thingClass=typeof(Projectile_PoisonFlask);def.category=ThingCategory.Projectile;
                def.comps=new();def.size=new IntVec2(1,1);def.stackLimit=1;def.destroyable=true;def.drawerType=DrawerType.RealtimeOnly;
                def.projectile=new ProjectileProperties{speed=20,explosionRadius=4};
                DefDatabase<ThingDef>.Add(def);
                var shot=(Projectile_PoisonFlask)GenSpawn.Spawn(def,c.Creator.Position,c.Map);
                SpellItemEffectTests.SetProjectileField(shot,"launcher",c.Creator);
                SpellItemEffectTests.SetProjectileField(shot,"landed",true);
                Set(shot,"initialized",true);Set(shot,"duration",540);Set(shot,"age",121);
                Set(shot,"lastStrike",120);Set(shot,"radius",6.4f);
                if(field=="LegacyAirborne") { Set(shot,"age",-1);Set(shot,"initialized",false); }
                // Complete native projectile Scribe, with the physical launcher
                // resolved externally. This does not substitute impact or ownership.
                var loaded=RoundTripSummon(r,"apothecary-poison-"+field,shot,c.Creator);
                object? Value(object target,string name)=>typeof(Projectile_PoisonFlask).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(target);
                if(field=="Resume")
                {
                    var cell=shot.Position;shot.DeSpawn();GenSpawn.Spawn(loaded,cell,c.Map);
                    // The base projectile impact is outside this cloud clock contract;
                    // the actual derived impact, initialization and pulse guards run.
                    using var baseImpact=new Patch(typeof(AbilityUser.Projectile_AbilityBase).GetMethod("Impact",BindingFlags.NonPublic|BindingFlags.Instance)!,nameof(CancelNative));
                    typeof(Projectile_PoisonFlask).GetMethod("Impact",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(loaded,[null,false]);
                    Check.Equal(122,(int)Value(loaded,"age")!,"loaded cloud advances instead of restarting");
                    Check.Equal(120,(int)Value(loaded,"lastStrike")!,"no premature poison pulse on first resumed tick");
                    Check.Equal(540,(int)Value(loaded,"duration")!,"resumed impact does not recompute captured lifetime");
                    Check.Equal(6.4f,(float)Value(loaded,"radius")!,"resumed impact does not recompute radius from current caster");
                    return;
                }
                if(field.StartsWith("Legacy"))
                {
                    var file=Path.Combine(r.WorkingDirectory,"apothecary-poison-"+field+".xml");
                    var xml=System.Xml.Linq.XDocument.Load(file);
                    foreach(var node in xml.Descendants().Where(n=>new[]{"initialized","duration","lastStrike"}.Contains(n.Name.LocalName)).ToArray())node.Remove();
                    xml.Save(file);
                    loaded=null!;
                    try {
                        Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref loaded,"summon");
                        var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                        directory.RegisterLoaded(c.Creator);Scribe.loader.FinalizeLoading();
                    } finally { Scribe.ForceStop(); }
                    Check.Equal(field=="LegacyCloud",(bool)Value(loaded,"initialized")!,"old active cloud cannot replay first impact; airborne flask still initializes");
                    Check.Equal(field=="LegacyCloud"?120:0,(int)Value(loaded,"lastStrike")!,"old pulse clock resumes from elapsed cloud age");
                    Check.Equal(360,(int)Value(loaded,"duration")!,"missing historical duration uses documented base lifetime");
                    return;
                }
                Check.Equal(Value(shot,"age"),Value(loaded,"age"),"cloud elapsed time retained");
                Check.Equal(Value(shot,"radius"),Value(loaded,"radius"),"cloud radius retained");
                Check.Equal(Value(shot,field),Value(loaded,field),"active poison cloud retains "+field+" without replay/truncation");
            }));
    }
}
