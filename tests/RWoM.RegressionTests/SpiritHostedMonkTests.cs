using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private static Thing? monkStatBody;
    private static int dragonHits;
    private static DamageInfo dragonPassivePacket;
    private static bool CaptureDragonSplash(DamageInfo dinfo) {dragonHits++;dragonPassivePacket=dinfo;return false;}
    private static bool CaptureDragon(ref DamageWorker.DamageResult __result) {dragonHits++;__result=new DamageWorker.DamageResult();return false;}
    private static bool RejectDragonLanding(ref bool __result) {__result=false;return false;}
    private static bool DragonSafeTemperature(ref FloatRange __result) {__result=new FloatRange(-100,100);return false;}
    private static bool MonkBodyStat(Thing __0,ref float __result) {monkStatBody=__0;__result=10f;return false;}
    private sealed class MonkJobSave : IExposable
    {
        public Pawn Body=null!;
        public Job Order=null!;
        public JobDriver Driver=null!;
        public void ExposeData()
        {
            Scribe_References.Look(ref Body,"body");
            Scribe_Deep.Look(ref Order,"order");
            if (Driver!=null) {Driver.pawn=Body;Driver.job=Order;}
            Scribe_Deep.Look(ref Driver,"driver");
            if (Driver!=null) {Driver.pawn=Body;Driver.job=Order;}
        }
    }
    private sealed class ChiTickSave : IExposable
    {
        public HediffComp_Chi Effect=new();
        public void ExposeData() => Effect.CompExposeData();
    }
    private static IEnumerable<TestCase> HostedMonkCases()
    {
        yield return new("MonkProjection.DragonPassive", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            c.Creator.equipment=new Pawn_EquipmentTracker(c.Creator);
            using var splash=new Patch(typeof(TM_Action).GetMethod(nameof(TM_Action.DoAction_ApplySplashDamage))!,nameof(CaptureDragonSplash));
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            source.MightData.MightPowerSkill_DragonStrike.Single(s=>s.label=="TM_DragonStrike_pwr").level=3;
            source.MightData.MightPowerSkill_DragonStrike.Single(s=>s.label=="TM_DragonStrike_ver").level=3;
            source.MightData.MightPowerSkill_global_strength.Single(s=>s.label=="TM_global_strength_pwr").level=2;
            c.Original.health.hediffSet.hediffs.Add(new Hediff{pawn=c.Original,def=TorannMagicDefOf.TM_MindOverBodyHD,Severity=3.5f});
            foreach(var name in new[]{"TM_Mecha-Golem_LightningCoreHD","TM_PsionicHD"})
                if(DefDatabase<HediffDef>.GetNamedSilentFail(name)==null) DefDatabase<HediffDef>.Add(new HediffDef{defName=name});
            var target=PawnWithComps(false,false).pawn;
            Set(target.def.race,"fleshType",new FleshTypeDef{defName="Normal",isOrganic=true});
            target.kindDef=new PawnKindDef{defName="MonkPassiveTarget",race=target.def};Set(target,"mapIndexOrState",(sbyte)0);Set(target,"positionInt",new IntVec3(20,0,20));
            var field=typeof(MightAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf",true)!.GetField("TM_DragonStrike")!;
            var old=field.GetValue(null);
            try {
                var dragon=new DamageDef{defName="MonkPassiveDragon"};field.SetValue(null,dragon);
                var physical=new DamageDef{defName="MonkPassivePhysical",harmsHealth=true};
                dragonHits=0;Rand.PushState(8541);
                try {for(int i=0;i<100 && dragonHits==0;i++){var hit=new DamageInfo(physical,10,instigator:c.Creator);TorannMagicMod.PostApplyDamage_Patch.Postfix(target.health,ref hit,target);}}
                finally {Rand.PopState();}
                Check.Equal(1,dragonHits,"hosted passive emits a dragon splash");
                Check.Same(c.Creator,dragonPassivePacket.Instigator!,"physical carrier causes splash");
                Check.Same(dragon,dragonPassivePacket.Def,"native dragon damage category retained");
                Check.Equal(true,dragonPassivePacket.Amount>=8 && dragonPassivePacket.Amount<=14,"source power and global strength bound damage");
                for(int i=0;i<40;i++){var hit=new DamageInfo(dragon,10,instigator:c.Creator);TorannMagicMod.PostApplyDamage_Patch.Postfix(target.health,ref hit,target);}
                Check.Equal(1,dragonHits,"dragon damage cannot recursively trigger dragon passive");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                for(int i=0;i<40;i++){var hit=new DamageInfo(physical,10,instigator:c.Creator);TorannMagicMod.PostApplyDamage_Patch.Postfix(target.health,ref hit,target);}
                Check.Equal(1,dragonHits,"depleted source cannot add splash");
            } catch(Exception e) {
                throw new Exception(string.Join("; ",new System.Diagnostics.StackTrace(e).GetFrames().Select(f=>f.GetMethod()?.Name+" IL="+f.GetILOffset().ToString("X"))),e);
            } finally {field.SetValue(null,old);}
        }));
        yield return new("MonkProjection.TigerPassive", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            source.maxSP=1;c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;
            c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
            source.MightData.MightPowerSkill_TigerStrike.Single(s=>s.label=="TM_TigerStrike_pwr").level=3;
            source.MightData.MightPowerSkill_TigerStrike.Single(s=>s.label=="TM_TigerStrike_eff").level=2;
            source.MightData.MightPowerSkill_global_seff.Single(s=>s.label=="TM_global_seff_pwr").level=1;
            c.Original.health.hediffSet.hediffs.Add(new Hediff{pawn=c.Original,def=TorannMagicDefOf.TM_MindOverBodyHD,Severity=3.5f});
            c.Creator.stances=new Pawn_StanceTracker(c.Creator);
            var verb=new Verb_MeleeAttackDamage{caster=c.Creator,verbProps=new VerbProperties()};
            float before=source.Stamina.CurLevel;int xp=source.MightUserXP;
            TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,c.Creator.Position+IntVec3.East,1);
            Check.Equal(before,source.Stamina.CurLevel,"unfinished burst cannot trigger passive");
            Rand.PushState(72813);
            try {for(int i=0;i<100 && source.Stamina.CurLevel==before;i++) TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,c.Creator.Position+IntVec3.East,0);}
            finally {Rand.PopState();}
            float cost=.06f*(1-.2f*(1-.03f));
            Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(before-cost))<.0001f,"native proc consumes source stamina once");
            Check.Equal(xp+10,source.MightUserXP,"source receives passive experience");
            Check.Equal(true,c.Creator.stances.curStance is Stance_Cooldown,"physical body receives shortened cooldown");
            before=source.Stamina.CurLevel;Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            for(int i=0;i<20;i++) TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,c.Creator.Position+IntVec3.East,0);
            Check.Equal(before,source.Stamina.CurLevel,"depletion disables passive spending");
        }));
        yield return new("MonkProjection.BodyTrainingDefense", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                var source=c.Original.GetCompAbilityUserMight();
                source.MightData.MightPowerSkill_MindOverBody.Single(s=>s.label=="TM_MindOverBody_ver").level=3;
                source.totalApparelWeight=100;
                var local=c.Creator.GetCompAbilityUserMight();_=local.MightData;local.totalApparelWeight=4;
                c.Original.health.hediffSet.hediffs.Add(new Hediff{pawn=c.Original,def=TorannMagicDefOf.TM_MindOverBodyHD,Severity=3.5f});
                var attacker=new Pawn{def=c.Creator.def,kindDef=c.Creator.kindDef};
                attacker.story=new Pawn_StoryTracker(attacker){traits=new TraitSet(attacker)};
                attacker.health=new Pawn_HealthTracker(attacker);
                Set(attacker,"comps",new List<ThingComp>());Set(attacker,"mapIndexOrState",(sbyte)0);
                var damage=new DamageDef{defName="MonkProjectedDefense",isExplosive=true};
                float Hit() {var info=new DamageInfo(damage,20,instigator:attacker);TorannMagicMod.Pawn_PreApplyDamage.Prefix(c.Creator,ref info,out _);return info.Amount;}
                Check.Equal(9f,Hit(),"source versatility with carrier armor yields eleven reduction");
                var native=new Hediff{pawn=c.Creator,def=TorannMagicDefOf.TM_MindOverBodyHD,Severity=.5f};
                c.Creator.health.hediffSet.hediffs.Add(native);
                Check.Equal(15f,Hit(),"local passive overrides soul without doubling reduction");
                c.Creator.health.hediffSet.hediffs.Remove(native);
                var fistField=typeof(MightAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf",true)!.GetField("TM_ChiFist")!;
                var oldFist=fistField.GetValue(null);
                try {
                    var fistDef=new DamageDef{defName="MonkProjectedFist"};fistField.SetValue(null,fistDef);
                    var weapon=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));weapon.defName="Human";
                    var fist=new DamageInfo(DamageDefOf.Blunt,20,instigator:c.Creator,weapon:weapon);
                    TorannMagicMod.Pawn_PreApplyDamage.Prefix(attacker,ref fist,out _);
                    Check.Same(fistDef,fist.Def,"source passive converts physical fist damage");
                    Check.Equal(true,fist.Amount>=24 && fist.Amount<=27,"source training severity increases fist damage");
                    Check.Same(c.Creator,fist.Instigator!,"physical body remains damage instigator");
                } finally {fistField.SetValue(null,oldFist);}
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(20f,Hit(),"depletion disables projected defense");
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;}
        }));
        yield return new("MonkProjection.RescueReliefCredit", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var path=state.Find("rwom.monk")!;Set(path,"stage",InitiationStage.Master);
            var type=Production("MonkMartialPath");
            var data=type.GetMethod("Writable",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[path])!;
            void Call(string method,params object[] args)=>type.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
            int Count(string field)=>(int)data.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(data)!;
            Call("Rescue",c.Creator,c.Original);Call("Rescue",c.Creator,c.Original);
            Call("Relief",c.Creator);Call("Relief",c.Creator);
            Check.Equal(1,Count("Rescues"),"rescue credited to soul once per patient per day");
            Check.Equal(1,Count("ReliefDays"),"relief credited to soul once per day");
            Set(Find.TickManager,"ticksGameInt",Find.TickManager.TicksGame+60000);
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Call("Rescue",c.Creator,c.Original);Call("Relief",c.Creator);
            Check.Equal(1,Count("Rescues"),"depleted soul gains no rescue credit");
            Check.Equal(1,Count("ReliefDays"),"depleted soul gains no relief credit");
        }));
        yield return new("MonkProjection.ChiBurstPenetration", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            var source=c.Original.GetCompAbilityUserMight();source.mightPwr=1.6f;
            c.Original.GetCompAbilityUserMagic().arcaneDmg=1;
            c.Creator.GetCompAbilityUserMight().mightPwr=1.1f;c.Creator.GetCompAbilityUserMagic().arcaneDmg=1;
            void Consciousness(Pawn pawn,float value) {
                var field=typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var cache=Activator.CreateInstance(field.FieldType)!;
                var entry=field.FieldType.GetProperty("Item",[typeof(PawnCapacityDef)])!.GetValue(cache,[PawnCapacityDefOf.Consciousness])!;
                Set(entry,"status",Enum.Parse(entry.GetType().GetField("status")!.FieldType,"Cached"));Set(entry,"value",value);
                field.SetValue(pawn.health.capacities,cache);
            }
            Consciousness(c.Original,.4f);Consciousness(c.Creator,.75f);
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.Verb_ChiBurst",true)!;
            var verb=(AbilityUser.Verb_UseAbility)Activator.CreateInstance(type)!;
            verb.caster=c.Creator;verb.Ability=new MightAbility(source){Def=TorannMagicDefOf.TM_ChiBurst};
            var method=type.GetMethod("Penetration",BindingFlags.Instance|BindingFlags.NonPublic);
            Check.NotNull(method,"burst needs source/body penetration split");
            Check.Equal(true,Math.Abs((float)method!.Invoke(verb,null)!-.35f)<.0001f,"source power plus carrier consciousness");
            verb.caster=c.Original;
            Check.Equal(TM_Calc.GetSpellPenetration(c.Original),(float)method.Invoke(verb,null)!,"native penetration unchanged");
        }));
        yield return new("MonkProjection.ChiBurstJobSave", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var oldEffect=TorannMagicDefOf.TM_ChiBurstED;var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var def=TorannMagicDefOf.TM_ChiBurst;var oldVerb=def.MainVerb;var oldCost=def.chiCost;var oldConsume=def.consumeEnergy;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                TorannMagicDefOf.TM_ChiBurstED=new EffecterDef{children=[]};
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="MonkBurstActivation"};
                DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));
                history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                using var finish=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=80};c.Original.health.hediffSet.hediffs.Add(chi);
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1f});
                def.chiCost=.5f;def.consumeEnergy=true;
                def.MainVerb=new AbilityUser.VerbProperties_Ability{abilityDef=def,SecondsToRecharge=120,warmupTime=2,range=40,
                    targetParams=new TargetingParameters{canTargetSelf=true},TargetAoEProperties=new AbilityUser.TargetAoEProperties{range=0}};
                var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var jobDef=new JobDef{defName="TM_SpiritChiBurst",driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
                JobDriver driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=JobMaker.MakeJob(jobDef,c.Original,c.Creator)};
                void Attach(){Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);}
                Toil[] Toils()=>((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                Attach();Toils()[0].initAction();
                Check.Equal(80f,chi.Severity,"burst warmup is free");
                var saved=RoundTripSummon(r,"chi-burst-before",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original);
                driver=saved.Driver;Attach();Toils()[1].initAction();
                float expected=80-100*source.ActualChiCost(def);
                Check.Equal(expected,chi.Severity,"loaded burst charges original chi once");
                Check.Equal(true,power.CooldownTicksLeft>0,"source cooldown starts");
                int cooldown=power.CooldownTicksLeft;
                saved=RoundTripSummon(r,"chi-burst-after",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original);
                driver=saved.Driver;Attach();Toils()[1].initAction();
                Check.Equal(expected,chi.Severity,"loaded completed burst cannot repay");
                Check.Equal(cooldown,power.CooldownTicksLeft,"loaded completed burst cannot restart cooldown");
                Check.Equal(false,c.Creator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD),"host receives no duplicate chi pool");
                var victim=PawnWithComps(true,true).pawn;
                victim.ageTracker=new Pawn_AgeTracker(victim);
                Set(victim.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
                victim.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult}}];
                Set(victim.ageTracker,"cachedLifeStageIndex",0);
                var stamina=new Need_Stamina(victim){def=TorannMagicDefOf.TM_Stamina,CurLevel=.9f};
                var mana=new Need_Mana(victim){def=TorannMagicDefOf.TM_Mana,CurLevel=.9f};
                victim.needs.AllNeeds.Add(stamina);victim.needs.AllNeeds.Add(mana);
                source.MightData.MightPowerSkill_Chi.Single(s=>s.label=="TM_Chi_pwr").level=3;
                var type=typeof(MightAbility).Assembly.GetType("TorannMagic.Verb_ChiBurst",true)!;
                var verb=(AbilityUser.Verb_UseAbility)Activator.CreateInstance(type)!;verb.caster=c.Creator;verb.Ability=power;
                Check.Equal(true,(bool)type.GetMethod("PreparePower",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(verb,null)!,"prepare energy burn from soul rank");
                using var damage=new Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage),[typeof(DamageInfo)])!,nameof(CaptureDragon));
                dragonHits=0;
                type.GetMethod("DisruptClassPawn",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(verb,[victim]);
                Check.Equal(true,Math.Abs(stamina.CurLevel-.1f)<.0001f,"rank3 burst burns 0.8 stamina");
                Check.Equal(.9f,mana.CurLevel,"native hybrid priority remains stamina first");
                Check.Equal(2,dragonHits,"energy burn emits damage and stun packets");
                Check.Equal(expected,chi.Severity,"burn does not recharge caster chi");
            } finally {
                TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_ChiBurstED=oldEffect;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
                def.MainVerb=oldVerb;def.chiCost=oldCost;def.consumeEnergy=oldConsume;
            }
        }));
        yield return new("MonkProjection.ChiBurstRanks", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var oldBurstEffect=TorannMagicDefOf.TM_ChiBurstED;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                var source=c.Original.GetCompAbilityUserMight();
                source.MightData.MightPowerSkill_Chi.Single(s=>s.label=="TM_Chi_pwr").level=3;
                var ability=new MightAbility(source){Def=TorannMagicDefOf.TM_ChiBurst};
                var type=typeof(MightAbility).Assembly.GetType("TorannMagic.Verb_ChiBurst",true)!;
                var verb=(AbilityUser.Verb_UseAbility)Activator.CreateInstance(type)!;
                verb.caster=c.Creator;verb.Ability=ability;
                var prepare=type.GetMethod("PreparePower",BindingFlags.Instance|BindingFlags.NonPublic);
                Check.Equal(true,prepare!=null,"Chi Burst must resolve exact ability owner before effects");
                Check.Equal(true,(bool)prepare!.Invoke(verb,null)!,"valid possession supplies Chi Burst");
                Check.Equal(3,(int)type.GetField("pwrVal",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(verb)!,"Chi power belongs to soul");
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1f});
                c.Original.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=80});
                source.AbilityData.Powers.Add(ability);source.AbilityData.AllPowers=null;
                var hosted=Production("SpiritHostedMartial");
                Check.Same(ability,hosted.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,c.Original,"TM_SpiritChiBurst"])!,"command finds exact source burst");
                var ready=hosted.GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Equal(true,(bool)ready.Invoke(null,[c.Creator,ability,"TM_SpiritChiBurst"])!,"funded source enables burst");
                ability.CooldownTicksLeft=10;
                Check.Equal(false,(bool)ready.Invoke(null,[c.Creator,ability,"TM_SpiritChiBurst"])!,"source cooldown blocks burst");
                ability.CooldownTicksLeft=0;
                verb.verbProps=new AbilityUser.VerbProperties_Ability { abilityDef=ability.mightDef,
                    TargetAoEProperties=new AbilityUser.TargetAoEProperties { range=0 } };
                TorannMagicDefOf.TM_ChiBurstED=new EffecterDef{children=[]};
                Set(verb,"currentTarget",new LocalTargetInfo(c.Creator));
                var shoot=type.GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!;
                Check.Equal(true,(bool)shoot.Invoke(verb,null)!,"empty successful burst returns success");
                var candidates=(List<Pawn>)type.GetMethod("GetMapClassPawnsAround")!.Invoke(verb,[c.Map,c.Creator.Position,0f])!;
                Check.Equal(0,candidates.Count,"caster excluded even when carrier has no native class");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(false,(bool)prepare.Invoke(verb,null)!,"depleted soul cannot supply burst");
                Check.Equal(false,(bool)shoot.Invoke(verb,null)!,"depleted soul cannot execute effects");
                verb.caster=c.Original;
                Check.Equal(true,(bool)prepare.Invoke(verb,null)!,"native caster retains existing rank path");
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_ChiBurstED=oldBurstEffect;}
        }));
        foreach (var (saveFlight,trajectory,jobFlight,edge) in new[]{(false,false,false,false),(true,false,false,false),(false,true,false,false),(false,true,true,false),(false,true,false,true)})
        yield return new("MonkProjection.DragonNativeFlight"+(edge?"MapEdge":jobFlight?"Job":saveFlight?"Save":trajectory?"Trajectory":""), r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="FlyingObject_DragonStrike";def.thingClass=typeof(FlyingObject_DragonStrike);def.category=ThingCategory.Projectile;def.comps=[];
            def.size=new IntVec2(1,1);def.destroyable=true;def.drawerType=DrawerType.RealtimeOnly;def.stackLimit=1;
            DefDatabase<ThingDef>.Add(def);
            c.Creator.drafter=new Pawn_DraftController(c.Creator);
            c.Creator.def.comps ??= [];
            c.Creator.def.thingClass=typeof(Pawn);
            c.Creator.kindDef=c.Original.kindDef;
            c.Creator.ageTracker=new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult,healthScaleFactor=1}}];
            Set(c.Creator.ageTracker,"cachedLifeStageIndex",0);
            Set(c.Creator.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
            c.Map.attackTargetsCache.Notify_ThingSpawned(c.Creator);
            Set(c.Creator.drafter,"draftedInt",true);
            c.Creator.carryTracker=new Pawn_CarryTracker(c.Creator);
            var cargoDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            cargoDef.defName="RegressionDragonNativeCargo";cargoDef.thingClass=typeof(Thing);cargoDef.category=ThingCategory.Item;cargoDef.stackLimit=75;cargoDef.comps=[];
            DefDatabase<ThingDef>.Add(cargoDef);
            var cargo=new Thing{def=cargoDef,thingIDNumber=19901,stackCount=7};
            Check.Equal(true,c.Creator.carryTracker.innerContainer.TryAdd(cargo,false),"native launch carries cargo");
            if(edge)
            {
                var invalid=(FlyingObject_DragonStrike)GenSpawn.Spawn(def,c.Creator.Position,c.Map);
                invalid.Launch(c.Creator,new IntVec3(-1,0,-1),c.Creator);
                Check.Equal(true,invalid.Destroyed,"out-of-map destination rejected before departure");
                Check.Equal(true,c.Creator.Spawned,"invalid destination leaves carrier on map");
                Check.Same(cargo,c.Creator.carryTracker.CarriedThing,"invalid launch leaves carried cargo intact");
                Check.Same(c.Creator,c.Original.ParentHolder.ParentHolder,"invalid launch preserves soul ownership");
            }
            FlyingObject_DragonStrike flight;
            using var visuals=new Patch(typeof(FlyingObject_DragonStrike).GetMethod("Initialize",BindingFlags.NonPublic|BindingFlags.Instance)!,nameof(CancelNative));
            if(jobFlight)
            {
                var oldSettings=TorannMagic.ModOptions.Settings.Instance;var oldDef=TorannMagicDefOf.TM_DragonStrike;var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
                try {
                    TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                    c.Map.weatherManager=(WeatherManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
                    c.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
                    TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="DragonHostedActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                    var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                    using var notifyHistory=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                    using var endJob=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
                    using var abilityOwner=new Patch(typeof(Verb).GetProperty(nameof(Verb.DirectOwner))!.GetMethod!,nameof(NativeAbilityOwner));
                    using var comfort=new Patch(typeof(GenTemperature).GetMethod(nameof(GenTemperature.SafeTemperatureRange),[typeof(Pawn)])!,nameof(DragonSafeTemperature));
                    var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                    c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                    var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                    source.MightData.MightPowerSkill_DragonStrike.Single(s=>s.label=="TM_DragonStrike_ver").level=2;
                    var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=80};c.Original.health.hediffSet.hediffs.Add(chi);
                    var abilityDef=(TMAbilityDef)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(oldDef,null)!;
                    abilityDef.consumeEnergy=true;abilityDef.chiCost=.2f;abilityDef.staminaCost=0;
                    abilityDef.MainVerb=new AbilityUser.VerbProperties_Ability{verbClass=typeof(Effect_DragonStrike),range=20,warmupTime=.5f,SecondsToRecharge=20};
                    TorannMagicDefOf.TM_DragonStrike=abilityDef;
                    var power=new MightAbility(source){Def=abilityDef};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                    var jobDef=new JobDef{defName="TM_SpiritDragonStrike",driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
                    var driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=JobMaker.MakeJob(jobDef,c.Original,new IntVec3(17,0,17))};
                    Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);
                    var toils=((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                    toils[0].initAction();Check.Equal(80f,chi.Severity,"dragon warmup free");toils[1].initAction();
                    flight=(FlyingObject_DragonStrike)c.Creator.ParentHolder;
                    Check.Equal(2,(int)typeof(FlyingObject_DragonStrike).GetField("verVal",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(flight)!,"flight snapshots original versatility before despawn");
                    Check.Equal(80-100*source.ActualChiCost(abilityDef),chi.Severity,"successful dragon launch pays source chi");
                    Check.Equal(true,power.CooldownTicksLeft>0,"dragon launch starts original cooldown");
                    float paid=chi.Severity;toils[1].initAction();Check.Equal(paid,chi.Severity,"repeated job completion cannot pay twice");
                } finally {TorannMagic.ModOptions.Settings.Instance=oldSettings;TorannMagicDefOf.TM_DragonStrike=oldDef;TorannMagicDefOf.TM_UsedManeuver=oldEvent;}
            }
            else {flight=(FlyingObject_DragonStrike)GenSpawn.Spawn(def,c.Creator.Position,c.Map);flight.Launch(c.Creator,new IntVec3(17,0,17),c.Creator);}
            Check.Equal(false,c.Creator.Spawned,"native launch removes carrier from map");
            Check.Same(flight,c.Creator.ParentHolder,"actual pawn held by flight");
            Check.Equal(true,c.Creator.inventory.innerContainer.Contains(cargo),"native despawn preserves original cargo");
            Check.Same(c.Creator,c.Original.ParentHolder.ParentHolder,"soul remains inside carrier during flight");
            var actor=c.Creator;
            if(saveFlight)
            {
                TorannMagicDefOf.TM_SpiritPossessionHD.comps=[new HediffCompProperties{compClass=typeof(HediffComp_SpiritPossession)}];
                foreach(var body in new[]{c.Creator,c.Original})
                    if(!body.def.comps.Any(p=>p.compClass==typeof(CompClassProgression)))body.def.comps.Add(new CompProperties{compClass=typeof(CompClassProgression)});
                void Register<T>(T definition) where T:Def,new() {if(definition!=null && DefDatabase<T>.GetNamedSilentFail(definition.defName)==null)DefDatabase<T>.Add(definition);}
                foreach(var defOf in new[]{typeof(BodyDefOf),typeof(BeardDefOf),typeof(TattooDefOf)})RuntimeLog.EnsureHeadlessDefOfInitialized(defOf);
                var hair=new HairDef{defName="RegressionDragonHair"};Register(hair);
                var head=new HeadTypeDef{defName="RegressionDragonHead"};Register(head);
                foreach(var body in new[]{c.Creator,c.Original}){body.story.hairDef=hair;body.story.headType=head;}
                Register(c.Original.def.race.body);
                Register(c.Magicyte.def);Register(c.Silver.def);
                foreach(var skill in c.Original.skills.skills)Register(skill.def);
                foreach(var assignment in c.Original.timetable.times)Register(assignment);
                Register(PrisonerInteractionModeDefOf.MaintainOnly);
                var references=new List<ILoadReferenceable>{c.Creator.Faction,c.Circle,c.Map};
                foreach(var relation in c.Original.relations.DirectRelations){Register(relation.def);references.Add(relation.otherPawn);}
                int hediffId=19000;
                foreach(var pawn in new[]{c.Creator,c.Original})foreach(var hediff in pawn.health.hediffSet.hediffs)hediff.loadID=hediffId++;
                var ideology=new Ideo();Set(ideology,"id",19001);Set(c.Original.ideo,"ideo",ideology);references.Add(ideology);
                if(DefDatabase<ThingDef>.GetNamedSilentFail(c.Creator.def.defName)==null)DefDatabase<ThingDef>.Add(c.Creator.def);
                if(DefDatabase<ThingDef>.GetNamedSilentFail(c.Original.def.defName)==null)DefDatabase<ThingDef>.Add(c.Original.def);
                if(DefDatabase<PawnKindDef>.GetNamedSilentFail(c.Creator.kindDef.defName)==null)DefDatabase<PawnKindDef>.Add(c.Creator.kindDef);
                if(DefDatabase<HediffDef>.GetNamedSilentFail(TorannMagicDefOf.TM_SpiritPossessionHD.defName)==null)DefDatabase<HediffDef>.Add(TorannMagicDefOf.TM_SpiritPossessionHD);
                var location=flight.Position;flight.DeSpawn();
                flight=RoundTripSummon(r,"monk-dragon-native-flight",flight,references.ToArray());
                RuntimeLog.AssertClean();
                actor=(Pawn)flight.GetDirectlyHeldThings().Single();
                Check.Equal(c.Creator.ThingID,actor.ThingID,"loaded carrier preserves identity");
                var possession=actor.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD).TryGetComp<HediffComp_SpiritPossession>();
                var soul=(Pawn)possession.GetDirectlyHeldThings().Single();
                Check.Equal(c.Original.ThingID,soul.ThingID,"loaded flight retains original soul identity");
                Check.Same(actor,soul.ParentHolder.ParentHolder,"loaded soul remains inside its carrier");
                Check.Equal(13,soul.skills.skills.Single().levelInt,"loaded soul retains leveled ordinary skill");
                Check.Equal(123f,soul.skills.skills.Single().xpSinceLastLevel,"loaded soul retains accumulated skill XP");
                Check.NotNull(ClassIdentity.Component(soul),"loaded soul retains progression component");
                Check.Equal(true,ClassIdentity.Component(soul).SpiritRebirth!=null,"loaded soul retains rebirth state");
                Check.Equal(true,SpiritRebirthState.TryGetPossessingSpirit(actor,out var activeSoul),"loaded possession remains authorized");
                Check.Same(soul,activeSoul!,"loaded authority resolves same saved soul");
                cargo=actor.inventory.innerContainer.Single(t=>t.thingIDNumber==19901);
                Check.Equal(7,cargo.stackCount,"loaded carrier preserves cargo");
                GenSpawn.Spawn(flight,location,c.Map);
            }
            if(trajectory)
            {
                if(edge)
                {
                    // Simulate an invalid restored/changed trajectory after a valid launch.
                    Set(flight,"destination",new UnityEngine.Vector3(-50,0,actor.Position.z));
                    int duration=(int)typeof(FlyingObject_DragonStrike).GetProperty("StartingTicksToImpact",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly)!.GetValue(flight)!;
                    Set(flight,"ticksToImpact",duration);
                }
                using var dust=new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowDustPuff),[typeof(IntVec3),typeof(Map),typeof(float)])!,nameof(CancelNative));
                var tick=typeof(FlyingObject_DragonStrike).GetMethod("TickInterval",BindingFlags.NonPublic|BindingFlags.Instance)!;
                for(int i=0;i<100 && !flight.Destroyed;i++)tick.Invoke(flight,[1]);
                Check.Equal(true,flight.Destroyed,"native ticking reaches destination and releases carrier");
                Check.Equal(true,edge ? actor.Position.InBounds(c.Map) : actor.Position.InHorDistOf(new IntVec3(17,0,17),3.9f),"native ticking lands at valid destination or last in-bounds cell");
            }
            else {
            var target=PawnWithComps(false,false).pawn;
            flight.impactDamage=new DamageInfo(new DamageDef{defName="RegressionDragonLandingImpact"},23f,0,-1,actor);
            var impact=typeof(FlyingObject_DragonStrike).GetMethod("Impact",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly)!;
            using(var capture=new Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(CaptureDragon)))
            {
                dragonHits=0;
                using(var blocked=new Patch(typeof(FlyingObject_DragonStrike).GetMethod("ReleaseNear",BindingFlags.NonPublic|BindingFlags.Instance)!,nameof(RejectDragonLanding)))
                {
                    impact.Invoke(flight,[target]);
                    Check.Equal(1,dragonHits,"landing delivers one damage packet");
                    Check.Equal(false,flight.Destroyed,"blocked landing keeps occupied flight alive");
                    impact.Invoke(flight,[target]);
                    Check.Equal(1,dragonHits,"repeated landing must not deal damage again");
                    Check.Same(flight,actor.ParentHolder,"blocked landing retains actual carrier");
                }
                flight.Destroy();
                Check.Equal(1,dragonHits,"successful retry does not repeat damage");
            }
            }
            Check.Equal(true,actor.Spawned,"native landing restores carrier to map");
            Check.Equal(0,flight.GetDirectlyHeldThings().Count,"native landing releases ownership once");
            Check.Equal(true,flight.Destroyed,"empty flight cleaned up");
            Check.Equal(true,actor.Drafted,"native landing restores drafted state");
            Check.Same(cargo,actor.carryTracker.CarriedThing,"native landing restores same cargo");
            Check.Equal(7,cargo.stackCount,"native flight retains complete cargo stack");
            if(edge)
            {
                var spinningDef=(ThingDef)typeof(object).GetMethod("MemberwiseClone",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(def,null)!;
                spinningDef.defName="RegressionMonkKnockback";spinningDef.thingClass=typeof(FlyingObject_Spinning);DefDatabase<ThingDef>.Add(spinningDef);
                using var spinningVisuals=new Patch(typeof(FlyingObject_Spinning).GetMethod("Initialize",BindingFlags.NonPublic|BindingFlags.Instance)!,nameof(CancelNative));
                using var spinningDust=new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowDustPuff),[typeof(IntVec3),typeof(Map),typeof(float)])!,nameof(CancelNative));
                var wind=SoundDefOf.Ambient_AltitudeWind;
                try {
                    SoundDefOf.Ambient_AltitudeWind=new SoundDef{defName="RegressionKnockbackWind"};
                    foreach(var destination in new[]{new IntVec3(17,0,17),new IntVec3(-50,0,17)})
                    {
                        var spinning=(FlyingObject_Spinning)GenSpawn.Spawn(spinningDef,actor.Position,c.Map);
                        spinning.Launch(actor,destination,actor);
                        Check.Equal(false,actor.Spawned,"knockback departs through native launch");
                        var spinningTick=typeof(FlyingObject_Spinning).GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly)!;
                        for(int i=0;i<500 && !spinning.Destroyed;i++)spinningTick.Invoke(spinning,null);
                        Check.Equal(true,spinning.Destroyed,"knockback completes or stops at map edge");
                        Check.Equal(true,actor.Spawned && actor.Position.InBounds(c.Map),"knockback restores physical carrier on map");
                        Check.Same(actor,c.Original.ParentHolder.ParentHolder,"knockback retains hosted soul");
                        Check.Equal(true,actor.Drafted,"knockback retains drafted state");
                        Check.Equal(false,cargo.Destroyed,"knockback cannot destroy cargo");
                        Check.Equal(7,cargo.stackCount,"knockback retains complete cargo stack");
                        Check.Equal(true,cargo.Spawned || cargo.ParentHolder==actor.inventory || cargo.ParentHolder==actor.carryTracker,"cargo remains accessible after knockback");
                    }
                } finally {SoundDefOf.Ambient_AltitudeWind=wind;}
            }
        }));
        yield return new("MonkProjection.DragonCargo", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            c.Creator.carryTracker=new Pawn_CarryTracker(c.Creator);
            c.Creator.inventory=new Pawn_InventoryTracker(c.Creator);
            var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="RegressionDragonCargo";def.thingClass=typeof(Thing);def.category=ThingCategory.Item;def.stackLimit=75;def.comps=[];
            var cargo=new Thing{def=def,thingIDNumber=19898,stackCount=7};
            Check.Equal(true,c.Creator.carryTracker.innerContainer.TryAdd(cargo,false),"native carry container accepts cargo");
            var flight=new FlyingObject_DragonStrike();
            var prepare=typeof(FlyingObject_DragonStrike).GetMethod("PrepareCargo",BindingFlags.Instance|BindingFlags.NonPublic);
            var restore=typeof(FlyingObject_DragonStrike).GetMethod("RestoreCargo",BindingFlags.Instance|BindingFlags.NonPublic);
            Check.NotNull(prepare,"dragon flight must preserve carried cargo before despawn");Check.NotNull(restore,"dragon landing restores carried cargo");
            Check.Equal(true,(bool)prepare!.Invoke(flight,[c.Creator])!,"cargo preparation succeeds");
            Check.Equal(true,c.Creator.inventory.innerContainer.Contains(cargo),"same cargo moved into inventory");
            Check.Equal(true,c.Creator.carryTracker.CarriedThing==null,"hands empty before despawn");
            restore!.Invoke(flight,[c.Creator]);
            Check.Same(cargo,c.Creator.carryTracker.CarriedThing,"same cargo returns to hands");
            restore.Invoke(flight,[c.Creator]);Check.Equal(7,cargo.stackCount,"repeated restore cannot duplicate cargo");
            Check.Equal(true,(bool)prepare.Invoke(flight,[c.Creator])!,"second takeoff retains cargo");
            var other=new Thing{def=def,thingIDNumber=19899,stackCount=3};
            Check.Equal(true,c.Creator.carryTracker.innerContainer.TryAdd(other,false),"hands occupied after takeoff");
            restore.Invoke(flight,[c.Creator]);
            Check.Same(other,c.Creator.carryTracker.CarriedThing,"landing does not replace occupied hands");
            Check.Equal(true,c.Creator.inventory.innerContainer.Contains(cargo),"original cargo remains safely in inventory");
            var inventory=c.Creator.inventory;c.Creator.inventory=null;
            Check.Equal(false,(bool)prepare.Invoke(flight,[c.Creator])!,"takeoff refused if carried cargo cannot be preserved");
            Check.Same(other,c.Creator.carryTracker.CarriedThing,"refused takeoff leaves carried item intact");c.Creator.inventory=inventory;
        }));
        yield return new("MonkProjection.DragonRanks", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                var source=c.Original.GetCompAbilityUserMight();
                source.MightData.MightPowerSkill_DragonStrike.Single(s=>s.label=="TM_DragonStrike_pwr").level=3;
                source.MightData.MightPowerSkill_global_strength.Single(s=>s.label=="TM_global_strength_pwr").level=2;
                var ability=new MightAbility(source){Def=TorannMagicDefOf.TM_DragonStrike};
                source.AbilityData.Powers.Add(ability);source.AbilityData.AllPowers=null;
                c.Original.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=80});
                var api=Production("SpiritHostedMartial");
                Check.Same(ability,api.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,c.Original,"TM_SpiritDragonStrike"])!,"dragon command resolves original ability");
                Check.Equal(true,(bool)api.GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,ability,"TM_SpiritDragonStrike"])!,"dragon command uses original chi");
                var method=typeof(Effect_DragonStrike).GetMethod("PrepareImpact",BindingFlags.NonPublic|BindingFlags.Instance);
                Check.NotNull(method,"dragon launch prepares impact from validated ability owner");
                DamageInfo? Hit(Pawn body){Rand.PushState(54321);try{return (DamageInfo?)method!.Invoke(new Effect_DragonStrike{caster=body,Ability=ability},null);}finally{Rand.PopState();}}
                var native=Hit(c.Original);var hosted=Hit(c.Creator);
                Check.Equal(true,hosted.HasValue,"valid possession supplies dragon impact");
                Check.Equal(native!.Value.Amount,hosted!.Value.Amount,"hosted dragon uses original power and global strength");
                Rand.PushState(54321);try {Check.Equal((float)UnityEngine.Mathf.RoundToInt(Rand.Range(8f,15f)*1.4f),hosted.Value.Amount,"power3 and strength2 affect impact");}finally{Rand.PopState();}
                Check.Same(c.Creator,hosted.Value.Instigator,"dragon impact belongs to physical carrier");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(false,Hit(c.Creator).HasValue,"depleted source cannot prepare dragon impact");
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;}
        }));
        yield return new("MonkProjection.DragonFlightSave", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="RegressionDragonFlight";def.thingClass=typeof(FlyingObject_DragonStrike);def.category=ThingCategory.Projectile;def.comps=[];
            DefDatabase<ThingDef>.Add(def);
            var damage=new DamageDef{defName="RegressionDragonImpact"};DefDatabase<DamageDef>.Add(damage);
            var shot=new FlyingObject_DragonStrike{def=def,thingIDNumber=19895,usedTarget=IntVec3.Zero,intendedTarget=IntVec3.Zero,
                impactDamage=new DamageInfo(damage,23f,.4f,72f,c.Creator)};
            Set(shot,"drafted",true);Set(shot,"verVal",3);Set(shot,"ticksToImpact",17);
            Set(shot,"impactApplied",true);
            var cargoDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            cargoDef.defName="RegressionDragonPassenger";cargoDef.thingClass=typeof(Thing);cargoDef.category=ThingCategory.Item;cargoDef.stackLimit=75;cargoDef.comps=[];
            DefDatabase<ThingDef>.Add(cargoDef);
            Set(shot,"flyingThing",new Thing{def=cargoDef,thingIDNumber=19896,stackCount=7});
            var loaded=RoundTripSummon(r,"monk-dragon-flight",shot,c.Creator);
            Check.Equal(true,loaded is IThingHolder,"saved flight exposes passenger ownership");
            var holder=(IThingHolder)(object)loaded;var held=holder.GetDirectlyHeldThings();
            Check.Equal(1,held.Count,"legacy deep passenger adopted on load");Check.Equal(7,held[0].stackCount,"passenger contents preserved");
            Check.Same(loaded,held[0].holdingOwner.Owner,"passenger points to actual flight holder");
            Check.Equal(true,ThingOwnerUtility.ContentsSuspended(holder),"passenger suspended while airborne");
            Check.Equal(true,(bool)typeof(FlyingObject_DragonStrike).GetField("drafted",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,"flight retains drafted state");
            Check.Equal(true,(bool)typeof(FlyingObject_DragonStrike).GetField("impactApplied",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,"completed impact marker survives save");
            Check.Equal(true,loaded.impactDamage.HasValue,"flight retains prepared impact");
            var hit=loaded.impactDamage!.Value;
            Check.Equal(23f,hit.Amount,"impact amount retained");Check.Equal(.4f,hit.ArmorPenetrationInt,"impact penetration retained");
            Check.Equal(72f,hit.Angle,"impact angle retained");Check.Same(c.Creator,hit.Instigator,"physical damage instigator retained");
            loaded.Destroy();
            Check.Equal(false,loaded.Destroyed,"cannot destroy occupied flight without a landing map");
            Check.Equal(1,holder.GetDirectlyHeldThings().Count,"failed landing retains passenger");
            loaded=RoundTripSummon(r,"monk-dragon-return-pending",loaded,c.Creator);
            holder=(IThingHolder)(object)loaded;
            Check.Equal(1,holder.GetDirectlyHeldThings().Count,"repeated save retains exactly one passenger");
            Check.Same(loaded,holder.GetDirectlyHeldThings()[0].holdingOwner.Owner,"reloaded pending return restores holder");
        }));
        yield return new("MonkProjection.TigerJobSave", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.JobDriver_SpiritTigerStrike");
            Check.NotNull(type,"hosted tiger needs a persistent native burst job");
            var settings=TorannMagic.ModOptions.Settings.Instance;var oldDef=TorannMagicDefOf.TM_TigerStrike;var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var jobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="TigerHostedActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notifyHistory=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};c.Original.health.hediffSet.hediffs.Add(chi);
                var def=(TMAbilityDef)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(oldDef,null)!;
                def.consumeEnergy=true;def.chiCost=.12f;def.staminaCost=0;def.MainVerb=new AbilityUser.VerbProperties_Ability{burstShotCount=10,ticksBetweenBurstShots=6,range=1.42f,warmupTime=.25f,SecondsToRecharge=20};
                TorannMagicDefOf.TM_TigerStrike=def;
                var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var jobDef=new JobDef{defName="TM_SpiritTigerStrike",driverClass=type};DefDatabase<JobDef>.Add(jobDef);
                var api=Production("SpiritHostedMartial");
                Check.Same(power,api.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,c.Original,jobDef.defName])!,"tiger command resolves original ability");
                Check.Equal(true,(bool)api.GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,power,jobDef.defName])!,"tiger command accepts source chi");
                using var stat=new Patch(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(MonkBodyStat));
                using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
                using var notify=new Patch(typeof(Pawn).GetMethod(nameof(Pawn.Notify_UsedVerb))!,nameof(CancelNative));
                using var health=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.Notify_UsedVerb))!,nameof(CancelNative));
                using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.CheckForStateChange))!,nameof(CancelNative));
                MonkNativeTrialTests.Verify(c.Creator,false,c.Original,target=>{
                    c.Creator.stances=new Pawn_StanceTracker(c.Creator);c.Creator.mindState=new Pawn_MindState(c.Creator);c.Creator.equipment??=new Pawn_EquipmentTracker(c.Creator);c.Creator.verbTracker??=new VerbTracker(c.Creator);
                    var driver=(JobDriver)Activator.CreateInstance(type!)!;driver.pawn=c.Creator;driver.job=JobMaker.MakeJob(jobDef,c.Original,target);
                    object? Call(string name)=>type!.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null);
                    void Attach(){Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);}
                    Toil[] Toils()=>((IEnumerable<Toil>)type!.GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                    Attach();var toils=Toils();toils[0].initAction();
                    Check.Equal(47f,chi.Severity,"preparation does not spend chi");
                    Check.Equal(0,MonkNativeTrialTests.HitCount,"preparation does not strike");
                    Call("Finish");
                    Check.Equal(47f,chi.Severity,"cancelled preparation is free");
                    driver=(JobDriver)Activator.CreateInstance(type!)!;driver.pawn=c.Creator;driver.job=JobMaker.MakeJob(jobDef,c.Original,target);
                    Attach();toils=Toils();toils[0].initAction();toils[1].initAction();
                    Check.Equal(1,MonkNativeTrialTests.HitCount,"first hit emitted");
                    for(int i=0;i<5;i++)Call("Step");
                    Check.Equal(1,MonkNativeTrialTests.HitCount,"interval preserved before save");
                    var saved=RoundTripSummon(r,"tiger-burst-active",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original,target);
                    driver=saved.Driver;Call("Step");Check.Equal(2,MonkNativeTrialTests.HitCount,"loaded remaining interval emits next hit");
                    for(int i=0;i<40;i++)Call("Step");
                    Check.Equal(5,MonkNativeTrialTests.HitCount,"save preserves native total hits");
                    Check.Equal(47-100*source.ActualChiCost(def),chi.Severity,"one completion payment from source chi");
                    Check.Equal(true,power.CooldownTicksLeft>0,"source cooldown started");
                    float paid=chi.Severity;int cooldown=power.CooldownTicksLeft;
                    saved=RoundTripSummon(r,"tiger-burst-paid",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original,target);
                    driver=saved.Driver;Call("Step");Call("Finish");
                    Check.Equal(paid,chi.Severity,"loaded completed job cannot charge twice");Check.Equal(cooldown,power.CooldownTicksLeft,"loaded completed job cannot restart cooldown");
                    power.CooldownTicksLeft=0;chi.Severity=47;
                    driver=(JobDriver)Activator.CreateInstance(type!)!;driver.pawn=c.Creator;driver.job=JobMaker.MakeJob(jobDef,c.Original,target);
                    Attach();Toils()[1].initAction();
                    int hits=MonkNativeTrialTests.HitCount;
                    var position=target.Position;Set(target,"positionInt",new IntVec3(position.x+10,0,position.z));
                    Check.Equal(false,(bool)Call("Step")!,"target leaving reach interrupts series");
                    Check.Equal(hits,MonkNativeTrialTests.HitCount,"interrupted series emits no extra hit");
                    Check.Equal(47-100*source.ActualChiCost(def),chi.Severity,"started interrupted series pays original chi");
                    paid=chi.Severity;Call("Finish");Check.Equal(paid,chi.Severity,"interruption cannot charge twice");Set(target,"positionInt",position);
                });
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_TigerStrike=oldDef;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
                DefDatabase<JobDef>.Clear();foreach(var d in jobs)DefDatabase<JobDef>.Add(d);DefDatabase<HistoryEventDef>.Clear();foreach(var d in events)DefDatabase<HistoryEventDef>.Add(d);}
        }));
        yield return new("MonkProjection.TigerNativeBurst", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                var ability=new MightAbility(source){Def=TorannMagicDefOf.TM_TigerStrike};
                using var stat=new Patch(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(MonkBodyStat));
                using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
                using var notify=new Patch(typeof(Pawn).GetMethod(nameof(Pawn.Notify_UsedVerb))!,nameof(CancelNative));
                using var health=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.Notify_UsedVerb))!,nameof(CancelNative));
                MonkNativeTrialTests.Verify(c.Creator,false,c.Original,target=>{
                    c.Creator.stances=new Pawn_StanceTracker(c.Creator);c.Creator.mindState=new Pawn_MindState(c.Creator);
                    c.Creator.equipment??=new Pawn_EquipmentTracker(c.Creator);
                    c.Creator.verbTracker??=new VerbTracker(c.Creator);
                    var props=new AbilityUser.VerbProperties_Ability{burstShotCount=10,ticksBetweenBurstShots=6,range=1.42f,warmupTime=.25f};
                    var verb=new Verb_TigerStrike{caster=c.Creator,Ability=ability,verbProps=props};
                    Set(verb,"currentTarget",new LocalTargetInfo(target));
                    verb.WarmupComplete();
                    Check.Equal(1,MonkNativeTrialTests.HitCount,"native warmup emits first strike");
                    for(int i=0;i<5;i++)verb.VerbTick();
                    Check.Equal(1,MonkNativeTrialTests.HitCount,"native interval does not emit early");
                    verb.VerbTick();Check.Equal(2,MonkNativeTrialTests.HitCount,"native interval emits at six ticks");
                    for(int i=0;i<40;i++)verb.VerbTick();
                    Check.Equal(5,MonkNativeTrialTests.HitCount,"existing zero-versatility implementation emits five hits");
                    Check.Equal(false,verb.Bursting,"native burst terminates");
                });
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;}
        }));
        yield return new("MonkProjection.TigerDrain", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var source=c.Original.GetCompAbilityUserMight();
            var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};c.Original.health.hediffSet.hediffs.Add(chi);
            var verb=new Verb_TigerStrike{caster=c.Creator,Ability=new MightAbility(source){Def=TorannMagicDefOf.TM_TigerStrike}};
            var drain=typeof(Verb_TigerStrike).GetMethod("DrainEnergy",BindingFlags.Instance|BindingFlags.NonPublic);
            Check.NotNull(drain,"tiger energy transfer needs an owner-aware path");
            var victim=PawnWithComps(true,true).pawn;
            victim.ageTracker=new Pawn_AgeTracker(victim);
            Set(victim.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
            victim.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult}}];
            Set(victim.ageTracker,"cachedLifeStageIndex",0);
            var mana=new Need_Mana(victim){def=TorannMagicDefOf.TM_Mana,CurLevel=.5f};
            var stamina=new Need_Stamina(victim){def=TorannMagicDefOf.TM_Stamina,CurLevel=.5f};
            victim.needs.AllNeeds.Add(mana);victim.needs.AllNeeds.Add(stamina);
            drain!.Invoke(verb,[victim]);
            Check.Equal(.25f,mana.CurLevel,"hybrid victim loses mana first");
            Check.Equal(.5f,stamina.CurLevel,"hybrid second pool unchanged");
            Check.Equal(72f,chi.Severity,"stolen energy returns to original chi");
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD),"carrier gains no chi pool");
            mana.CurLevel=0;drain.Invoke(verb,[victim]);Check.Equal(72f,chi.Severity,"empty mana cannot create chi or drain second pool");
            victim.needs.AllNeeds.Remove(mana);stamina.CurLevel=.125f;
            drain.Invoke(verb,[victim]);Check.Equal(0f,stamina.CurLevel,"fighter loses only available stamina");Check.Equal(84.5f,chi.Severity,"partial stamina converts exactly");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            stamina.CurLevel=.5f;drain.Invoke(verb,[victim]);Check.Equal(.5f,stamina.CurLevel,"invalid source cannot steal energy");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            c.Original.health.hediffSet.hediffs.Remove(chi);drain.Invoke(verb,[victim]);Check.Equal(.5f,stamina.CurLevel,"missing recipient chi cannot destroy victim energy");
            c.Original.health.hediffSet.hediffs.Add(chi);chi.Severity=47;
            var native=new Verb_TigerStrike{caster=c.Original,Ability=verb.Ability};
            drain.Invoke(native,[victim]);
            Check.Equal(.25f,stamina.CurLevel,"native tiger retains stamina drain");
            Check.Equal(72f,chi.Severity,"native tiger retains chi gain");
        }));
        yield return new("MonkProjection.TigerRanks", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            try {
            TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var source=c.Original.GetCompAbilityUserMight();
            c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1f});
            source.MightData.MightPowerSkill_TigerStrike.Single(s=>s.label=="TM_TigerStrike_pwr").level=3;
            source.MightData.MightPowerSkill_TigerStrike.Single(s=>s.label=="TM_TigerStrike_ver").level=2;
            var ability=new MightAbility(source){Def=TorannMagicDefOf.TM_TigerStrike};
            var verb=new Verb_TigerStrike{caster=c.Creator,Ability=ability,verbProps=ability.Def.MainVerb};
            using var stat=new Patch(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(MonkBodyStat));
            monkStatBody=null;
            Check.Equal(true,verb.GetAttackDmg(c.Creator)>0,"hosted tiger computes damage");
            Check.Equal(3,verb.pwrVal,"tiger power comes from original training");
            Check.Equal(2,verb.verVal,"tiger versatility comes from original training");
            Check.Same(c.Creator,monkStatBody!,"melee DPS comes from physical carrier");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(0,verb.GetAttackDmg(c.Creator),"depleted original cannot supply tiger damage");
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;}
        }));
        yield return new("MonkProjection.Meridian", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var path=state.Find("rwom.monk")!;Set(path,"stage",InitiationStage.Ascended);
            var data=Production("MonkMartialPath").GetMethod("Writable",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[path])!;Set(data,"Meridian",true);
            c.Original.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47});
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            var power=new MonkMeridianAbility(source){Def=DefDatabase<TMAbilityDef>.GetNamed("TM_MonkSilentMeridian")};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            var api=Production("SpiritHostedMartial");
            Check.Same(power,api.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,c.Original,"TM_SpiritMonkMeridian"])!,"meridian command resolves original ability");
            var ready=api.GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!;
            bool Ready()=>(bool)ready.Invoke(null,[c.Creator,power,"TM_SpiritMonkMeridian"])!;
            Check.Equal(false,Ready(),"meridian needs sixty percent source chi");
            c.Original.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD).Severity=60;
            Check.Equal(true,Ready(),"funded ascended source enables meridian; direct="+typeof(JobDriver_MonkMeridian).GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator])+" chi="+c.Original.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD).Severity+" threshold="+(100f*.6f));
            power.CooldownTicksLeft=10;Check.Equal(false,Ready(),"source ability cooldown disables meridian");power.CooldownTicksLeft=0;
            MonkMeridianTests.Verify(r,c.Creator,source,c.Original);
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD),"meridian never creates carrier chi");
        }));
        yield return new("MonkProjection.PalmJobSave", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            var jobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="MonkPalmActivation"};
                DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));
                history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                using var finish=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
                using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.CheckForStateChange))!,nameof(CancelNative));
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};c.Original.health.hediffSet.hediffs.Add(chi);
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                var def=DefDatabase<TMAbilityDef>.GetNamed("TM_MonkPalmSeal");
                def.consumeEnergy=true;def.chiCost=.2f;def.staminaCost=0;
                def.MainVerb=new AbilityUser.VerbProperties_Ability{SecondsToRecharge=30,warmupTime=1,range=1.42f};
                var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var jobDef=new JobDef{defName="TM_SpiritPalmSeal",driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
                MonkNativeTrialTests.Verify(c.Creator,false,c.Original,target=>{
                    JobDriver driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=JobMaker.MakeJob(jobDef,c.Original,target)};
                    void Attach(){Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);}
                    Toil[] Toils()=>((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                    Attach();var toils=Toils();toils[0].initAction();
                    float expectedChi=47-100*source.ActualChiCost(def);
                    Check.Equal(47f,chi.Severity,"warmup does not charge chi");
                    var saved=RoundTripSummon(r,"monk-palm-before-hit",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original,target);
                    driver=saved.Driver;Attach();toils=Toils();toils[1].initAction();
                    Check.Equal(1,MonkNativeTrialTests.HitCount,"loaded job strikes once");
                    Check.Equal(true,target.stances.stunner.Stunned,"loaded strike applies native stun");
                    Check.Equal(expectedChi,chi.Severity,"native payment consumes exact source chi cost");
                    Check.Equal(true,power.CooldownTicksLeft>0,"native payment starts source cooldown");
                    Check.Equal(false,c.Creator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD),"no carrier chi pool created");
                    float paid=chi.Severity;int cooldown=power.CooldownTicksLeft;
                    saved=RoundTripSummon(r,"monk-palm-after-hit",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original,target);
                    driver=saved.Driver;Attach();Toils()[1].initAction();
                    Check.Equal(1,MonkNativeTrialTests.HitCount,"saved attempted flag prevents repeated strike");
                    Check.Equal(paid,chi.Severity,"saved attempted flag prevents second payment");
                    Check.Equal(cooldown,power.CooldownTicksLeft,"saved attempted flag preserves cooldown");
                });
            } finally {
                TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
                DefDatabase<HistoryEventDef>.Clear();foreach(var def in events)DefDatabase<HistoryEventDef>.Add(def);
                DefDatabase<JobDef>.Clear();foreach(var def in jobs)DefDatabase<JobDef>.Add(def);
            }
        }));
        yield return new("MonkProjection.PalmOrder", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};c.Original.health.hediffSet.hediffs.Add(chi);
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            var def=DefDatabase<TMAbilityDef>.GetNamed("TM_MonkPalmSeal");
            def.chiCost=.2f;def.consumeEnergy=true;
            var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            var api=Production("SpiritHostedMartial");
            var find=api.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
            var ready=api.GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!;
            const string order="TM_SpiritPalmSeal";
            Check.Same(power,find.Invoke(null,[c.Creator,c.Original,order])!,"palm command uses original ability");
            bool Ready()=>(bool)ready.Invoke(null,[c.Creator,power,order])!;
            Check.Equal(true,Ready(),"source chi funds carrier palm");
            chi.Severity=0;Check.Equal(false,Ready(),"empty source pool rejects palm");
            chi.Severity=47;power.CooldownTicksLeft=5;Check.Equal(false,Ready(),"source cooldown rejects palm");
            power.CooldownTicksLeft=0;
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(false,Ready(),"lost source cannot fund palm");
            Check.Equal(true,(bool)api.GetMethod("UsesCombatTarget",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[order])!,"palm uses targeted job path");
        }));
        yield return new("MonkProjection.TendingCredit", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            Set(state.Find("rwom.monk")!,"MartialAlternate",true);
            MonkNativeTrialTests.Verify(c.Creator,true,c.Original);
        }));
        yield return new("MonkProjection.PalmSeal", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            MonkNativeTrialTests.Verify(c.Creator,false,c.Original);
        }));
        yield return new("MonkProjection.InterruptionCredit", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var path=state.Find("rwom.monk")!;
            var type=Production("MonkMartialPath");
            var record=type.GetMethod("Interrupted",BindingFlags.Static|BindingFlags.NonPublic)!;
            var writable=type.GetMethod("Writable",BindingFlags.Static|BindingFlags.NonPublic)!;
            var data=writable.Invoke(null,[path])!;
            int Count()=>(int)data.GetType().GetField("Interrupts",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(data)!;
            record.Invoke(null,[c.Creator]);
            Check.Equal(1,Count(),"carrier interruption advances original discipline");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            record.Invoke(null,[c.Creator]);
            Check.Equal(1,Count(),"depleted possession cannot earn trial credit");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            c.Creator.health.hediffSet.hediffs.Remove(c.Creator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD));
            record.Invoke(null,[c.Creator]);
            Check.Equal(1,Count(),"detached possession cannot earn trial credit");
        }));
        yield return new("MonkProjection.RestorativeJobSave", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var source=c.Original.GetCompAbilityUserMight();
            source.MightData.MightPowerSkill_Meditate.Single(s=>s.label=="TM_Meditate_pwr").level=3;
            var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};c.Original.health.hediffSet.hediffs.Add(chi);
            c.Creator.kindDef=new PawnKindDef{race=c.Creator.def};
            c.Creator.ageTracker??=new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult}}];
            Set(c.Creator.ageTracker,"cachedLifeStageIndex",0);
            var wound=new Hediff_Injury{pawn=c.Creator,Part=c.Creator.RaceProps.body.corePart,def=new HediffDef{defName="MonkCarrierCut",injuryProps=new InjuryProps()}};
            Set(wound,"severityInt",20f);c.Creator.health.hediffSet.hediffs.Add(wound);
            using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.CheckForStateChange))!,nameof(CancelNative));
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.JobDriver_Meditate",true)!;
            var driver=(JobDriver)Activator.CreateInstance(type,true)!;driver.pawn=c.Creator;
            driver.job=JobMaker.MakeJob(new JobDef{defName="TM_SpiritMeditate"},c.Creator.Position,c.Original);
            Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);
            Toil Work(JobDriver d)=>((IEnumerable<Toil>)type.GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(d,null)!).Last();
            int sourceXp=source.MightUserXP;
            int hostXp=c.Creator.GetCompAbilityUserMight().MightUserXP;
            var work=Work(driver);work.initAction();Set(Find.TickManager,"ticksGameInt",600);
            Rand.PushState(117);try{work.tickAction();}finally{Rand.PopState();}
            int originalPower=(int)type.GetField("pwrVal",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!;
            float firstHeal=20f-wound.Severity;
            Check.Equal(true,firstHeal>0,"real meditation heals carrier wound");
            Check.Equal(46f,chi.Severity,"healing debits original chi");
            Check.Equal(sourceXp+10,source.MightUserXP,"healing awards original experience");
            Check.Equal(hostXp,c.Creator.GetCompAbilityUserMight().MightUserXP,"host receives no borrowed experience");
            var prior=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
            try {
                DefDatabase<JobDef>.Add(driver.job.def);
                Set(driver,"age",6500);
                var saved=RoundTripSummon(r,"monk-restorative-job",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original);
                driver=saved.Driver;Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);
                Check.Equal(6500,(int)type.GetField("age",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!,"load retains meditation elapsed time");
                work=Work(driver); // Native load resumes this toil without running initAction again.
                Set(wound,"severityInt",20f);chi.Severity=47;
                Rand.PushState(117);try{work.tickAction();}finally{Rand.PopState();}
                Check.Equal(originalPower,(int)type.GetField("pwrVal",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!,"loaded active meditation restores original healing rank");
                float restoredHeal=20f-wound.Severity;
                Check.Equal(true,restoredHeal>=1.625f && restoredHeal<=2.6f,"native wound healing uses rank3 and chi multiplier5 after load");
                Check.Equal(46f,chi.Severity,"loaded pulse debits source chi once");
            } finally {DefDatabase<JobDef>.Clear();foreach(var def in prior)DefDatabase<JobDef>.Add(def);}
        }));
        yield return new("MonkProjection.MeditationActivation", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var oldDef=TorannMagicDefOf.TM_Meditate;
            var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            var jobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                var def=(TMAbilityDef)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(oldDef,null)!;
                def.consumeEnergy=false;def.staminaCost=0;
                def.MainVerb=new AbilityUser.VerbProperties_Ability{SecondsToRecharge=10};
                TorannMagicDefOf.TM_Meditate=def;
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="MonkHostedActivation"};
                DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));
                history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
                c.Original.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47});
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                var power=new MightAbility(source){Def=def,CooldownTicksLeft=87};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var type=typeof(MightAbility).Assembly.GetType("TorannMagic.JobDriver_Meditate",true)!;
                var driver=(JobDriver)Activator.CreateInstance(type,true)!;driver.pawn=c.Creator;
                driver.job=JobMaker.MakeJob(new JobDef{defName="TM_SpiritMeditate"},c.Creator.Position,c.Original);
                Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);
                var begin=type.GetMethod("BeginHosted",BindingFlags.Instance|BindingFlags.NonPublic);
                Check.NotNull(begin,"hosted meditation needs once-only native activation");
                Check.Equal(false,(bool)begin!.Invoke(driver,null)!,"cooldown rejects a stale job before payment");
                Check.Equal(87,power.CooldownTicksLeft,"rejection leaves cooldown intact");
                power.CooldownTicksLeft=0;
                Check.Equal(true,(bool)begin.Invoke(driver,null)!,"valid accepted job activates");
                Check.Equal(true,power.CooldownTicksLeft>0,"native PostAbilityAttempt starts original cooldown");
                int paid=power.CooldownTicksLeft;power.CooldownTicksLeft=paid-1;
                DefDatabase<JobDef>.Add(driver.job.def);
                var loaded=RoundTripSummon(r,"monk-paid-activation",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original);
                driver=loaded.Driver;
                Check.Equal(true,(bool)begin.Invoke(driver,null)!,"paid loaded job resumes despite its cooldown");
                Check.Equal(paid-1,power.CooldownTicksLeft,"reload does not restart cooldown");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(false,(bool)begin.Invoke(driver,null)!,"paid job still rejects a depleted source");
            } finally {
                TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_Meditate=oldDef;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
                DefDatabase<HistoryEventDef>.Clear();foreach(var def in events)DefDatabase<HistoryEventDef>.Add(def);
                DefDatabase<JobDef>.Clear();foreach(var def in jobs)DefDatabase<JobDef>.Add(def);
            }
        }));
        yield return new("MonkProjection.MeditationAvailability", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            c.Original.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47});
            var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
            var power=new MightAbility(source){Def=TorannMagicDefOf.TM_Meditate};
            source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
            var ready=Production("SpiritHostedMartial").GetMethod("Ready",BindingFlags.Static|BindingFlags.NonPublic)!;
            const string job="TM_SpiritMeditate";
            Check.Same(power,find.Invoke(null,[c.Creator,c.Original,job])!,"hosted meditation resolves exact original power");
            Check.Equal(true,(bool)ready.Invoke(null,[c.Creator,power,job])!,"zero-cost meditation can start");
            power.CooldownTicksLeft=87;
            Check.Equal(false,(bool)ready.Invoke(null,[c.Creator,power,job])!,"original cooldown gates command");
            power.CooldownTicksLeft=0;
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(false,(bool)ready.Invoke(null,[c.Creator,power,job])!,"stale command cannot use depleted soul");
            Check.Equal(true,find.Invoke(null,[c.Creator,c.Original,job])==null,"depletion removes borrowed command");
        }));
        yield return new("MonkProjection.CultivationJob", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            Set(state.Find("rwom.monk")!,"MartialAlternate",true);
            var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};
            c.Original.health.hediffSet.hediffs.Add(chi);
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.JobDriver_Meditate",true)!;
            var driver=(JobDriver)Activator.CreateInstance(type,true)!;
            driver.pawn=c.Creator;
            driver.job=JobMaker.MakeJob(new JobDef{defName="TM_SpiritMeditate"},c.Creator.Position,c.Original);
            Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);
            var toils=((IEnumerable<Toil>)type.GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
            Check.Equal(3,toils.Length,"hosted cultivation follows native walk/wait/finish path");
            Check.Equal(600,toils[1].defaultDuration,"original cultivation duration retained");
            toils[2].initAction();
            Check.Equal(53f,chi.Severity,"native job completion awards original pool");
            toils[2].initAction();
            Check.Equal(53f,chi.Severity,"repeated completion does not grant a second reward");
            var priorJobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
            try {
                DefDatabase<JobDef>.Add(driver.job.def);
                var saved=RoundTripSummon(r,"monk-completed-job",new MonkJobSave{Body=c.Creator,Order=driver.job,Driver=driver},c.Creator,c.Original);
                driver=saved.Driver;
                Set(c.Creator.jobs,"curJob",driver.job);Set(c.Creator.jobs,"curDriver",driver);
                Check.Same(c.Original,driver.job.targetB.Pawn,"save retains original source");
                toils=((IEnumerable<Toil>)type.GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                toils[2].initAction();
                Check.Equal(53f,chi.Severity,"loaded completion cannot award cultivation twice");
            } finally {
                DefDatabase<JobDef>.Clear();foreach(var def in priorJobs)DefDatabase<JobDef>.Add(def);
            }
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            toils[2].initAction();
            Check.Equal(53f,chi.Severity,"stale job completion after depletion grants nothing");
            driver.job.targetB=LocalTargetInfo.Invalid;
            Check.Equal(false,driver.TryMakePreToilReservations(false),"missing saved soul reference cannot become a native job");
        }));
        yield return new("MonkProjection.HostedChiTick", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var chi=new HediffWithComps{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};
            var tracker=new HediffComp_Chi{parent=chi};chi.comps=[tracker];
            c.Original.health.hediffSet.hediffs.Add(chi);
            Set(tracker,"initialized",true);
            Set(Find.TickManager,"ticksGameInt",240);
            var tick=typeof(HediffComp_Chi).GetMethod("TickHosted",BindingFlags.Static|BindingFlags.NonPublic);
            Check.NotNull(tick,"held monk chi needs a native tick path");
            tick!.Invoke(null,[c.Original]);
            Check.Equal(true,chi.Severity<47 && chi.Severity>46.9f,"host colonist status gives normal chi upkeep");
            float after=chi.Severity;
            var loaded=RoundTripSummon(r,"monk-chi-tick",new ChiTickSave{Effect=tracker}).Effect;
            loaded.parent=chi;chi.comps=[loaded];Set(loaded,"initialized",true);
            tick.Invoke(null,[c.Original]);
            Check.Equal(after,chi.Severity,"same saved tick cannot apply upkeep twice");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Set(Find.TickManager,"ticksGameInt",360);tick.Invoke(null,[c.Original]);
            Check.Equal(after,chi.Severity,"depleted spirit upkeep is dormant");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            var receipt=c.Creator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
            c.Creator.health.hediffSet.hediffs.Remove(receipt);
            Set(Find.TickManager,"ticksGameInt",480);tick.Invoke(null,[c.Original]);
            Check.Equal(after,chi.Severity,"outside a valid body chi is dormant");
        }));
        yield return new("MonkProjection.CultivationOwner", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            Set(state.Find("rwom.monk")!,"MartialAlternate",true);
            var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};
            c.Original.health.hediffSet.hediffs.Add(chi);
            var finish=Production("MonkMeditation").GetMethod("Finish",BindingFlags.Static|BindingFlags.NonPublic)!;
            Check.Equal(true,(bool)finish.Invoke(null,[c.Creator])!,"physical host completes original cultivation");
            Check.Equal(53f,chi.Severity,"only original pool gains base chi");
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD),"no host chi created");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(false,(bool)finish.Invoke(null,[c.Creator])!,"stale completion after depletion is rejected");
            Check.Equal(53f,chi.Severity,"stale completion grants nothing");
        }));
        yield return new("MonkProjection.OwnerAndChi", r => SpiritRebirthRitualTransactionTests.Run(r,c => {
            using var fixture=new Fixture(r,c);
            using var definitions=MonkCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            var chi=new Hediff{def=TorannMagicDefOf.TM_ChiHD,pawn=c.Original,Severity=47};
            c.Original.health.hediffSet.hediffs.Add(chi);
            var method=Production("MonkMartialPath").GetMethod("Owner",BindingFlags.Static|BindingFlags.NonPublic);
            Check.NotNull(method,"Monk physical actor must resolve its original training owner");
            Pawn? Owner(Pawn actor)=>(Pawn?)method!.Invoke(null,[actor]);
            Check.Same(c.Original,Owner(c.Creator)!,"possessed actor uses soul discipline");
            Check.Same(c.Original,Owner(c.Original)!,"native class lookup stays native");
            var hostState=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Creator),"state",hostState);
            hostState.TryAddFoundation("rwom.monk",DisciplineDomain.Might);
            Check.Same(c.Creator,Owner(c.Creator)!,"native host class takes precedence");
            Set(ClassIdentity.Component(c.Creator),"state",new ClassProgressionState(Vocation.Warrior));
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(true,Owner(c.Creator)==null,"depleted soul cannot lend class effects");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            var receipt=c.Creator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
            c.Creator.health.hediffSet.hediffs.Remove(receipt);
            Check.Equal(true,Owner(c.Creator)==null,"detached receipt removes access");
            Check.Equal(47f,chi.Severity,"ownership lookup never spends or copies chi");
            Check.Equal(false,c.Creator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD),"no duplicate pool on host");
        }));
    }
}

