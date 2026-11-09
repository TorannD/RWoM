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
    private sealed class SoldierOrdnanceSave : IExposable
    {
        public Building_60mmMortar Mortar=new();
        public Projectile_BreachingCharge Charge=new();
        public Projectile_Mk203GL Grenade=new();
        public void ExposeData()
        {
            if(Scribe.EnterNode("mortar")) {try {Mortar.ExposeData();} finally {Scribe.ExitNode();}}
            if(Scribe.EnterNode("charge")) {try {Charge.ExposeData();} finally {Scribe.ExitNode();}}
            if(Scribe.EnterNode("grenade")) {try {Grenade.ExposeData();} finally {Scribe.ExitNode();}}
        }
    }
    private sealed class SoldierWound : Hediff_Injury
    {
        internal bool Bandaged;
        public override bool TendableNow(bool ignoreTimer=false)=>!Bandaged;
        public override void Tended(float quality,float maxQuality,int batchPosition=0)=>Bandaged=true;
    }
    private sealed class SoldierBurstSave : IExposable
    {
        public Verb_SuppressingFire Verb=new();
        public void ExposeData() => Verb.ExposeData();
    }
    private static bool SoldierMedicinePotency(ref float __result){__result=1;return false;}
    private static float soldierPistolChance;
    private static bool SoldierPistolChance(float chance,ref bool __result){soldierPistolChance=chance;__result=true;return false;}
    private static void SoldierPistolCooldown(Pawn body,Pawn soul)
    {
        SoldierWeaponTrial(body,soul,false,(target,_)=>{
            var source=soul.GetCompAbilityUserMight();var local=body.GetCompAbilityUserMight();
            var power=source.MightData.MightPowersSS.First(p=>p.abilityDef==TorannMagicDefOf.TM_PistolSpec);power.learned=true;
            source.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_PistolSpec).level=2;
            source.MightData.MightPowerSkill_global_seff.First(s=>s.label=="TM_global_seff_pwr").level=3;
            var training=Production("SuperSoldierMartialPath").GetMethod("Training",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[ClassIdentity.Component(soul).State.Find("rwom.supersoldier")])!;
            Set(training,"School","TM_PistolSpec");
            var oldStances=body.stances;var oldRegistry=local.specWpnRegNum;
            var oldAge=body.ageTracker;var oldMax=local.maxSP;
            body.ageTracker=new Pawn_AgeTracker(body);Set(body.ageTracker,"ageBiologicalTicksInt",25L*GenDate.TicksPerYear);local.maxSP=1;
            body.stances=new Pawn_StanceTracker(body);local.specWpnRegNum=1;
            using var chance=new Patch(typeof(Rand).GetMethod(nameof(Rand.Chance))!,nameof(SoldierPistolChance));
            try {
                var verb=new Verb_Shoot{caster=body,verbProps=new VerbProperties{verbClass=typeof(Verb_Shoot)}};
                float before=source.Stamina.CurLevel;float xp=source.MightUserXP;
                TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,target,1);
                Check.Equal(before,source.Stamina.CurLevel,"no pistol proc before last burst shot");
                TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,target,0);
                Check.Equal(true,body.stances.curStance is Stance_Cooldown,"source pistol training changes physical stance");
                Check.Equal(5,((Stance_Cooldown)body.stances.curStance).ticksLeft,"native five tick cooldown preserved");
                Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(before-.03f*(1-.2f*(1-.09f))))<.00001f,"source ranks and stamina pay native formula");
                Check.Equal(.35f,soldierPistolChance,"source efficiency chance");Check.Equal(xp+5,source.MightUserXP,"source gets native proc XP");
                before=source.Stamina.CurLevel;body.stances.SetStance(new Stance_Mobile());
                Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);
                TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,target,0);
                Check.Equal(before,source.Stamina.CurLevel,"depleted source cannot pay or proc");
                Check.Equal(true,body.stances.curStance is Stance_Mobile,"depleted source leaves physical stance unchanged");
                Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",false);
                var nativeTrait=new Trait(TorannMagicDefOf.TM_SuperSoldier);body.story.traits.allTraits.Add(nativeTrait);
                var nativePower=local.MightData.MightPowersSS.First(p=>p.abilityDef==TorannMagicDefOf.TM_PistolSpec);
                bool nativeLearned=nativePower.learned;nativePower.learned=true;
                var addedNeed=local.Stamina==null?new Need_Stamina(body){def=TorannMagicDefOf.TM_Stamina,CurLevel=1}:null;
                if(addedNeed!=null)body.needs.AllNeeds.Add(addedNeed);
                try {
                    local.Stamina.CurLevel=1;float physicalBefore=local.Stamina.CurLevel;
                    int rank=local.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_PistolSpec).level;
                    int global=local.MightData.MightPowerSkill_global_seff.First(s=>s.label=="TM_global_seff_pwr").level;
                    TorannMagicMod.TryCastNextBurstShot_Monk_Patch.Postfix(verb,target,0);
                    Check.Equal(before,source.Stamina.CurLevel,"native specialist takes precedence over borrowed source");
                    Check.Equal(true,Math.Abs(local.Stamina.CurLevel-(physicalBefore-.03f*(1-.1f*rank*(1-.03f*global))))<.00001f,"native specialist pays own stamina");
                    Check.Equal(.25f+.05f*rank,soldierPistolChance,"native ranks take precedence");
                } finally {body.story.traits.allTraits.Remove(nativeTrait);nativePower.learned=nativeLearned;if(addedNeed!=null)body.needs.AllNeeds.Remove(addedNeed);}
            } finally {body.stances=oldStances;local.specWpnRegNum=oldRegistry;body.ageTracker=oldAge;local.maxSP=oldMax;}
        });
    }
    private static bool soldierAcceptDrop;
    private static bool SoldierRefuseDrop(Thing thing,ref Thing resultingThing,ref bool __result)
    {__result=soldierAcceptDrop;resultingThing=soldierAcceptDrop?thing:null!;return false;}
    private static void SoldierOriginalRetention(Pawn body,RuntimeFixture? r=null,bool ambiguous=false)
    {
        var comp=body.GetCompAbilityUserMight();var oldContainer=comp.equipmentContainer;
        var savedBindingField=typeof(CompAbilityUserMight).GetField("specializedWeapon",BindingFlags.NonPublic|BindingFlags.Instance);var oldBinding=savedBindingField?.GetValue(comp);
        var oldEquipment=body.equipment;body.equipment=new Pawn_EquipmentTracker(body);
        var trait=new Trait(TorannMagicDefOf.TM_SuperSoldier);body.story.traits.allTraits.Add(trait);
        using var placement=new Patch(typeof(GenDrop).GetMethod(nameof(GenDrop.TryDropSpawn))!,nameof(SoldierRefuseDrop));
        using var damage=new Patch(typeof(TM_Calc).GetMethod(nameof(TM_Calc.GetSkillDamage))!,nameof(SoldierMedicinePotency));
        try {
            foreach(string school in new[]{"Pistol","Rifle","Shotgun"}) {
                if(savedBindingField!=null)savedBindingField.SetValue(comp,Activator.CreateInstance(savedBindingField.FieldType));
                soldierAcceptDrop=false;
                ThingWithComps Item(string name,string label){var d=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));d.defName=name;d.label=label;d.category=ThingCategory.Item;d.stackLimit=1;d.comps=new();return new ThingWithComps{def=d,stackCount=1};}
                var original=Item("SoldierOriginal"+school,school);var unrelated=Item("SoldierOther"+school,r!=null||ambiguous?school:"Other");var copy=Item("TM_"+school+"Spec_Base1",school+"++");
                original.thingIDNumber=69101;unrelated.thingIDNumber=69102;copy.thingIDNumber=69103;
                comp.equipmentContainer=new ThingOwner<ThingWithComps>();Check.Equal(true,comp.equipmentContainer.TryAdd(unrelated),"unrelated retained item is actually owned");Check.Equal(true,comp.equipmentContainer.TryAdd(original),"original is actually owned");
                if(r!=null) {
                    var bindingField=typeof(CompAbilityUserMight).GetField("specializedWeapon",BindingFlags.NonPublic|BindingFlags.Instance);
                    Check.NotNull(bindingField,"specialized copies need persistent exact original identity");
                    var binding=bindingField!.GetValue(comp)!;
                    binding.GetType().GetMethod("Bind",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(binding,[original,copy]);
                    binding=RoundTripSummon(r,"soldier-weapon-binding-"+school,(IExposable)binding,original,unrelated,copy);
                    bindingField.SetValue(comp,binding);
                    copy.def.label="Renamed specialized weapon";
                }
                var container=comp.equipmentContainer;bool dropped=true;
                TorannMagic.TorannMagicMod.PawnEquipment_Drop_Postfix(body.equipment,copy,ref dropped);
                Check.Equal(true,container.Contains(original),"failed map placement retains original "+school);
                Check.Same(container,original.holdingOwner,"original keeps physical owner "+school);
                Check.Equal(true,container.Contains(unrelated),"restoring one weapon never clears unrelated originals "+school);
                soldierAcceptDrop=true;
                TorannMagic.TorannMagicMod.PawnEquipment_Drop_Postfix(body.equipment,copy,ref dropped);
                Check.Equal(ambiguous,container.Contains(original),"only unambiguous successful retry transfers original "+school);
                Check.Equal(true,container.Contains(unrelated),"successful retry keeps unrelated original "+school);
            }
        } finally {soldierAcceptDrop=false;body.story.traits.allTraits.Remove(trait);comp.equipmentContainer=oldContainer;body.equipment=oldEquipment;savedBindingField?.SetValue(comp,oldBinding);}
    }
    private static Building_60mmMortar? soldierMortar;
    private static Job? soldierManning;
    private static bool SoldierMortarSpawn(ref Thing __result){__result=soldierMortar!;return false;}
    private static bool SoldierMortarCell(ref IntVec3 __result){__result=new IntVec3(31,0,30);return false;}
    private static bool SoldierManningOrder(Job job){soldierManning=job;return false;}
    private static void SoldierMortarDeployment(Pawn body,Pawn soul)
    {
        var def=TorannMagicDefOf.TM_60mmMortar;var source=soul.GetCompAbilityUserMight();
        var oldMortar=TorannMagicDefOf.TM_60mmMortar_Base;
        var oldVerb=def.MainVerb;var oldCosts=(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost);
        var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
        try {
            TorannMagicDefOf.TM_60mmMortar_Base=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            TorannMagicDefOf.TM_60mmMortar_Base.defName="HostedMortarFixture";TorannMagicDefOf.TM_60mmMortar_Base.category=ThingCategory.Building;TorannMagicDefOf.TM_60mmMortar_Base.interactionCellOffset=IntVec3.South;
            def.consumeEnergy=true;def.staminaCost=.3f;def.manaCost=def.chiCost=def.bloodCost=0;
            def.MainVerb=new AbilityUser.VerbProperties_Ability{abilityDef=def,verbClass=typeof(Verb_60mmMortar),SecondsToRecharge=45,isViolent=true};
            var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;source.Stamina.CurLevelPercentage=1;
            TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="SoldierMortarActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
            var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
            using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
            using var floor=new Patch(typeof(GenGrid).GetMethod(nameof(GenGrid.Standable))!,nameof(SoldierAim));
            using var cell=new Patch(typeof(TM_Calc).GetMethod(nameof(TM_Calc.GetEmptyCellForNewBuilding))!,nameof(SoldierMortarCell));
            using var spawn=new Patch(typeof(TM_Action).GetMethod(nameof(TM_Action.SingleSpawnLoop))!,nameof(SoldierMortarSpawn));
            using var order=new Patch(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.TryTakeOrderedJob))!,nameof(SoldierManningOrder));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericFleck))!,nameof(CancelNative));
            using var smoke=new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowSmoke))!,nameof(CancelNative));
            var execute=Production("SuperSoldierCast").GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic)!;
            bool Deploy()=>(bool)execute.Invoke(null,[body,soul,"TM_SpiritCast60mmMortar",new LocalTargetInfo(body)])!;
            soldierMortar=null;soldierManning=null;float before=source.Stamina.CurLevel;int cooldownBefore=power.CooldownTicksLeft;
            Check.Equal(false,Deploy(),"failed placement rejected");Check.Equal(before,source.Stamina.CurLevel,"failed placement costs nothing");Check.Equal(cooldownBefore,power.CooldownTicksLeft,"failed placement preserves cooldown");Check.Equal<Job?>(null,soldierManning,"failed placement gives no manning order");
            soldierMortar=new Building_60mmMortar{def=TorannMagicDefOf.TM_60mmMortar_Base};Set(soldierMortar,"mapIndexOrState",(sbyte)0);Set(soldierMortar,"positionInt",body.Position+IntVec3.East);
            var offset=soldierMortar.def.interactionCellOffset;
            Check.Equal(true,Deploy(),"successful deployment");Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(before-source.ActualStaminaCost(def)))<.001f,"deployment pays original soul once");
            Check.Equal(true,power.CooldownTicksLeft>0,"deployment starts source cooldown");Check.NotNull(soldierManning,"carrier receives manning order");Check.Same(JobDefOf.ManTurret,soldierManning!.def,"native turret job");Check.Same(soldierMortar,soldierManning.targetA.Thing,"manning deployed instance");
            Check.Equal(offset,soldierMortar.def.interactionCellOffset,"deployment does not mutate shared building definition");
            float paid=source.Stamina.CurLevel;Check.Equal(false,Deploy(),"cooldown prevents duplicate deployment");Check.Equal(paid,source.Stamina.CurLevel,"duplicate has no payment");
        } finally {
            soldierMortar=null;soldierManning=null;TorannMagicDefOf.TM_60mmMortar_Base=oldMortar;def.MainVerb=oldVerb;(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost)=oldCosts;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
            DefDatabase<HistoryEventDef>.Clear();foreach(var d in events)DefDatabase<HistoryEventDef>.Add(d);
        }
    }
    private static int soldierLaunched;
    private static bool SoldierCountLaunch(){soldierLaunched++;return false;}
    private static void SoldierNativeBurst(Pawn actor,Pawn owner,bool buckshot,Action<Pawn,MightAbility,AbilityUser.VerbProperties_Ability>? hosted=null)
    {
        SoldierWeaponTrial(actor,owner,false,(target,_)=>{
            var source=owner.GetCompAbilityUserMight();source.mightPwr=2;
            source.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_RifleSpec).level=2;
            var projectile=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));projectile.defName="SoldierBurstBullet";projectile.projectile=new ProjectileProperties{damageDef=DamageDefOf.Bullet,stoppingPower=0};Set(projectile.projectile,"damageAmountBase",10);
            var weapon=actor.equipment.Primary.def.Verbs[0];weapon.defaultProjectile=projectile;weapon.burstShotCount=3;
            using var stats=new Patch(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValue"&&m.GetParameters().Length==4&&m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(SoldierMedicinePotency));
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var smoke=new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowSmoke))!,nameof(CancelNative));
            using var launch=new Patch(typeof(TM_CopyAndLaunchProjectile).GetMethod(nameof(TM_CopyAndLaunchProjectile.CopyAndLaunchThing))!,nameof(SoldierCountLaunch));
            using var notify=new Patch(typeof(Pawn).GetMethod(nameof(Pawn.Notify_UsedVerb))!,nameof(CancelNative));
            using var health=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.Notify_UsedVerb))!,nameof(CancelNative));
            actor.stances=new Pawn_StanceTracker(actor);actor.mindState=new Pawn_MindState(actor);actor.verbTracker??=new VerbTracker(actor);
            var def=buckshot?TorannMagicDefOf.TM_Buckshot:TorannMagicDefOf.TM_SuppressingFire;
            var props=new AbilityUser.VerbProperties_Ability{verbClass=buckshot?typeof(Verb_Buckshot):typeof(Verb_SuppressingFire),abilityDef=def,defaultProjectile=projectile,burstShotCount=buckshot?3:30,ticksBetweenBurstShots=buckshot?1:5,range=30};
            if(hosted!=null) {hosted(target,new MightAbility(source){Def=def},props);return;}
            AbilityUser.Verb_UseAbility verb=buckshot?new Verb_Buckshot():new Verb_SuppressingFire();verb.caster=actor;verb.Ability=new MightAbility(source){Def=def};verb.verbProps=props;
            int volleys=buckshot?3:30,pellets=buckshot?Verb_Buckshot.GetShotCount(actor):1;
            for(int cast=0;cast<2;cast++) {
                soldierLaunched=0;Set(verb,"currentTarget",new LocalTargetInfo(target));verb.WarmupComplete();
                Check.Equal(pellets,soldierLaunched,"native warmup emits first volley");
                for(int i=1;i<props.ticksBetweenBurstShots;i++)verb.VerbTick();
                Check.Equal(pellets,soldierLaunched,"no early volley");
                verb.VerbTick();Check.Equal(pellets*2,soldierLaunched,"next volley at native interval");
                for(int tick=0;tick<200;tick++)verb.VerbTick();
                Check.Equal(volleys*pellets,soldierLaunched,"complete native cast "+cast);
                Check.Equal(false,verb.Bursting,"native burst finishes");
            }
        });
    }
    private static bool SoldierAim(ref bool __result){__result=true;return false;}
    private static void SoldierHostedBurst(RuntimeFixture r,Pawn body,Pawn soul,bool buckshot)
    {
        var type=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.JobDriver_SpiritSoldierBurst");
        Check.NotNull(type,"hosted soldier burst needs a persistent native verb job");
        SoldierNativeBurst(body,soul,buckshot,(target,power,props)=>{
            var def=power.mightDef;var source=power.MightUser;var oldVerb=def.MainVerb;
            var oldCosts=(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost);
            var jobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            try {
                def.MainVerb=props;props.SecondsToRecharge=40;def.consumeEnergy=true;def.staminaCost=.2f;def.manaCost=def.chiCost=def.bloodCost=0;
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;source.Stamina.CurLevelPercentage=1;
                body.GetCompAbilityUserMight().specWpnRegNum=1;body.equipment.Primary.thingIDNumber=65137;
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="SoldierBurstActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                using var aim=new Patch(typeof(Verb).GetMethod(nameof(Verb.CanHitTargetFrom))!,nameof(SoldierAim));
                var jd=new JobDef{defName=buckshot?"TM_SpiritCastBuckshot":"TM_SpiritCastSuppressingFire",driverClass=type};DefDatabase<JobDef>.Add(jd);
                JobDriver New(){var d=(JobDriver)Activator.CreateInstance(type!)!;d.pawn=body;d.job=JobMaker.MakeJob(jd,soul,target,body.equipment.Primary);return d;}
                var driver=New();object? Call(string name)=>type!.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null);
                float before=source.Stamina.CurLevel;soldierLaunched=0;Call("Finish");
                Check.Equal(before,source.Stamina.CurLevel,"cancel before first shot is free");Check.Equal(0,soldierLaunched,"cancel preparation has no shots");
                Check.Equal(false,(bool)Call("Begin")!,"cancelled preparation cannot be restarted by a stale callback");
                driver=New();Check.Equal(true,(bool)Call("Begin")!,"first burst starts");
                int pellets=buckshot?Verb_Buckshot.GetShotCount(body):1;Check.Equal(pellets,soldierLaunched,"one initial volley");
                Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(before-source.ActualStaminaCost(def)))<.001f,"first volley pays once");
                for(int i=1;i<props.ticksBetweenBurstShots;i++)Call("Step");
                var saved=RoundTripSummon(r,"soldier-burst-active-"+buckshot,new MonkJobSave{Body=body,Order=driver.job,Driver=driver},body,soul,target,body.equipment.Primary);
                driver=saved.Driver;Call("Step");Check.Equal(2*pellets,soldierLaunched,"saved interval continues without a new warmup");
                for(int i=0;i<200;i++)Call("Step");Check.Equal((buckshot?3:30)*pellets,soldierLaunched,"loaded native total preserved");
                float paid=source.Stamina.CurLevel;int cd=power.CooldownTicksLeft,xp=source.MightUserXP,hits=soldierLaunched;
                saved=RoundTripSummon(r,"soldier-burst-done-"+buckshot,new MonkJobSave{Body=body,Order=driver.job,Driver=driver},body,soul,target,body.equipment.Primary);driver=saved.Driver;
                Call("Begin");Call("Step");Call("Finish");Check.Equal(hits,soldierLaunched,"completed save cannot restart burst");Check.Equal(paid,source.Stamina.CurLevel,"completed save no second charge");Check.Equal(cd,power.CooldownTicksLeft,"cooldown not reset");Check.Equal(xp,source.MightUserXP,"XP not duplicated");
                foreach(bool deplete in new[]{false,true}) {
                    power.CooldownTicksLeft=0;source.Stamina.CurLevelPercentage=1;driver=New();Check.Equal(true,(bool)Call("Begin")!,"new interruptible cast");hits=soldierLaunched;paid=source.Stamina.CurLevel;
                    var weapon=body.equipment.Primary;
                    if(deplete)Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);else body.equipment.Remove(weapon);
                    Check.Equal(false,(bool)Call("Step")!,"source loss or weapon removal stops the burst");Call("Finish");
                    Check.Equal(hits,soldierLaunched,"no shot after invalidation");Check.Equal(paid,source.Stamina.CurLevel,"no extra charge or refund on interruption");
                    if(deplete)Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",false);else body.equipment.AddEquipment(weapon);
                    Check.Equal(false,(bool)Call("Begin")!,"restoring source or weapon does not revive cancelled order");
                }
            } finally {
                def.MainVerb=oldVerb;(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost)=oldCosts;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
                DefDatabase<JobDef>.Clear();foreach(var d in jobs)DefDatabase<JobDef>.Add(d);DefDatabase<HistoryEventDef>.Clear();foreach(var d in events)DefDatabase<HistoryEventDef>.Add(d);
            }
        });
    }
    private static void SoldierDressingJob(RuntimeFixture r,Pawn body,Pawn soul,SoldierWound wound,Medicine med,bool cancel)
    {
        var source=soul.GetCompAbilityUserMight();var def=DefDatabase<TMAbilityDef>.GetNamed("TM_SuperSoldierFieldDressing");
        var oldVerb=def.MainVerb;var oldCosts=(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost);
        var oldEvent=TorannMagicDefOf.TM_UsedManeuver;var oldEvents=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
        var oldJobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        try {
            TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="HostedDressingActivation"};DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
            var history=(History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
            using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
            using var endJob=new Patch(typeof(JobDriver).GetMethod(nameof(JobDriver.EndJobWith))!,nameof(CancelNative));
            def.consumeEnergy=true;def.staminaCost=.2f;def.manaCost=def.chiCost=def.bloodCost=0;
            def.MainVerb=new AbilityUser.VerbProperties_Ability{verbClass=typeof(Verb_SuperSoldierFieldDressing),abilityDef=def,warmupTime=3,SecondsToRecharge=45,isViolent=false};
            var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;source.Stamina.CurLevelPercentage=1;
            var jobDef=new JobDef{defName="TM_SpiritCastSuperSoldierFieldDressing",driverClass=typeof(JobDriver_SpiritHostedMartial)};DefDatabase<JobDef>.Add(jobDef);
            var job=JobMaker.MakeJob(jobDef,soul,body);var driver=new JobDriver_SpiritHostedMartial{pawn=body,job=job};Set(body.jobs,"curJob",job);Set(body.jobs,"curDriver",driver);
            Toil[] Toils(JobDriver d)=>((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(d,null)!).ToArray();
            float before=source.Stamina.CurLevel;Toils(driver)[0].initAction();
            Check.Equal(before,source.Stamina.CurLevel,"preparation does not charge");Check.Equal(3,med.stackCount,"preparation preserves medicine");
            var resumed=RoundTripSummon(r,"soldier-dressing-warmup-"+cancel,new MonkJobSave{Body=body,Order=job,Driver=driver},body,soul);
            Set(body.jobs,"curJob",resumed.Order);Set(body.jobs,"curDriver",resumed.Driver);
            if(cancel) Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);
            Toils(resumed.Driver)[1].initAction();
            Check.Equal(!cancel,wound.Bandaged,"loaded job treats only with surviving source");Check.Equal(cancel?3:2,med.stackCount,"one physical medicine only on success");
            Check.Equal(true,Math.Abs(source.Stamina.CurLevel-(cancel?before:before-source.ActualStaminaCost(def)))<.001f,"one source payment on success");
            Check.Equal(!cancel,power.CooldownTicksLeft>0,"source cooldown only on success");
            float paid=source.Stamina.CurLevel;int xp=source.MightUserXP,cd=power.CooldownTicksLeft;
            var completed=RoundTripSummon(r,"soldier-dressing-completed-"+cancel,resumed,body,soul);Set(body.jobs,"curJob",completed.Order);Set(body.jobs,"curDriver",completed.Driver);
            wound.Bandaged=false;Toils(completed.Driver)[1].initAction();
            Check.Equal(false,wound.Bandaged,"saved completed job cannot repeat treatment");Check.Equal(cancel?3:2,med.stackCount,"no duplicate medicine");
            Check.Equal(paid,source.Stamina.CurLevel,"no duplicate payment");Check.Equal(xp,source.MightUserXP,"no duplicate XP");Check.Equal(cd,power.CooldownTicksLeft,"no cooldown reset");
        } finally {
            def.MainVerb=oldVerb;(def.consumeEnergy,def.staminaCost,def.manaCost,def.chiCost,def.bloodCost)=oldCosts;TorannMagicDefOf.TM_UsedManeuver=oldEvent;
            DefDatabase<HistoryEventDef>.Clear();foreach(var d in oldEvents)DefDatabase<HistoryEventDef>.Add(d);
            DefDatabase<JobDef>.Clear();foreach(var d in oldJobs)DefDatabase<JobDef>.Add(d);
        }
    }
    public sealed class SoldierTrialDamage : DamageWorker
    {
        public static Action? DuringHit;
        public static int Hits;
        public override DamageResult Apply(DamageInfo info,Thing victim)
        {
            Hits++;DuringHit?.Invoke();
            return new DamageResult{totalDamageDealt=info.Amount};
        }
    }
    private static bool SoldierSkipNotifications()=>false;
    private static readonly List<DamageInfo> SoldierWhipHits=new();
    private static float soldierDisarmChance;
    private static bool SoldierWhipDamage(DamageInfo dinfo,ref DamageWorker.DamageResult __result)
    {SoldierWhipHits.Add(dinfo);__result=new DamageWorker.DamageResult{totalDamageDealt=dinfo.Amount};return false;}
    private static bool SoldierWhipPart(ref BodyPartRecord __result){__result=null!;return false;}
    private static bool SoldierDisarmChance(float chance,ref bool __result){soldierDisarmChance=chance;__result=false;return false;}
    private static void SoldierWhip(Pawn actor,Pawn owner)
    {
        SoldierWeaponTrial(actor,owner,false,(target,_)=>{
            var source=owner.GetCompAbilityUserMight();source.mightPwr=2;source.weaponDamage=17;
            source.MightData.MightPowerSkill_PistolSpec.First(s=>s.label=="TM_PistolSpec_ver").level=3;
            var def=TorannMagicDefOf.TM_PistolWhip;float oldFactor=def.weaponDamageFactor;
            var projectile=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));projectile.defName="WhipPhysicalBullet";projectile.projectile=new ProjectileProperties{damageDef=DamageDefOf.Bullet,stoppingPower=0};Set(projectile.projectile,"damageAmountBase",10);
            var weaponVerb=actor.equipment.Primary.def.Verbs[0];weaponVerb.defaultProjectile=projectile;weaponVerb.warmupTime=1;weaponVerb.defaultCooldownTime=1;
            using var stats=new Patch(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValue"&&m.GetParameters().Length==4&&m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(SoldierMedicinePotency));
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            using var hits=new Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(SoldierWhipDamage));
            using var part=new Patch(typeof(HediffSet).GetMethod("GetRandomNotMissingPart")!,nameof(SoldierWhipPart));
            using var chance=new Patch(typeof(Rand).GetMethod(nameof(Rand.Chance))!,nameof(SoldierDisarmChance));
            try {
                def.weaponDamageFactor=1;SoldierWhipHits.Clear();
                var verb=new Verb_PistolWhip{caster=actor,verbProps=new AbilityUser.VerbProperties_Ability()};Set(verb,"currentTarget",new LocalTargetInfo(target));
                Check.Equal(true,(bool)typeof(Verb_PistolWhip).GetMethod("TryCastShot",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(verb,null)!,"whip resolves against physical enemy");
                Check.Equal(2,SoldierWhipHits.Count,"one blunt hit and one stun");
                Check.Same(DamageDefOf.Blunt,SoldierWhipHits[0].Def,"physical blow first");Check.Same(DamageDefOf.Stun,SoldierWhipHits[1].Def,"stun second");
                Check.Equal(true,SoldierWhipHits[0].Amount>=18 && SoldierWhipHits[0].Amount<=22,"source power and versatility scale blunt hit");
                Check.Equal(actor==owner?20f:15f,SoldierWhipHits[1].Amount,"native cache or physical host weapon with source versatility");
                Check.Same(actor,SoldierWhipHits[0].Instigator,"carrier remains damage instigator");
                Check.Equal(.5f,soldierDisarmChance,"source versatility sets disarm chance");
            } finally {def.weaponDamageFactor=oldFactor;SoldierWhipHits.Clear();}
        });
    }
    private static void SoldierWeaponTrial(Pawn actor,Pawn owner,bool deplete,Action<Pawn,DamageDef>? exercise=null)
    {
        const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Static;
        var api=Production("SuperSoldierMartialPath");
        var path=ClassIdentity.Component(owner).State.Find("rwom.supersoldier")!;
        Set(path,"stage",InitiationStage.Adept);
        var data=api.GetMethod("Training",hidden)!.Invoke(null,[path])!;Set(data,"School","TM_RifleSpec");
        var hostiles=(List<int>)data.GetType().GetField("Hostiles",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(data)!;
        var weaponDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        weaponDef.defName="SoldierTrialRifle";weaponDef.category=ThingCategory.Item;weaponDef.equipmentType=EquipmentType.Primary;weaponDef.comps=new();
        Set(weaponDef,"verbs",new List<VerbProperties>{new(){verbClass=typeof(Verb_Shoot)}});
        var weapon=new ThingWithComps{def=weaponDef};var equippable=new CompEquippable{parent=weapon};equippable.Initialize(new CompProperties{compClass=typeof(CompEquippable)});Set(weapon,"comps",new List<ThingComp>{equippable});
        var equipment=actor.equipment;actor.equipment=new Pawn_EquipmentTracker(actor);actor.equipment.AddEquipment(weapon);
        var oldRelations=typeof(Faction).GetField("relations",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(actor.Faction);
        var enemy=new Faction{def=new FactionDef(),loadID=65128};
        Set(enemy,"relations",new List<FactionRelation>{new(){other=actor.Faction,kind=FactionRelationKind.Hostile}});
        Set(actor.Faction,"relations",new List<FactionRelation>{new(){other=enemy,kind=FactionRelationKind.Hostile}});
        var target=PawnWithComps(false,false).pawn;target.kindDef=new PawnKindDef{race=target.def};target.guest=new Pawn_GuestTracker(target);target.mindState=new Pawn_MindState(target);
        target.thingIDNumber=65129;Set(target,"factionInt",enemy);Set(target,"mapIndexOrState",(sbyte)0);Set(target,"positionInt",actor.Position+IntVec3.East);
        var damage=new DamageDef{defName="SoldierTrialDamage",harmsHealth=false,workerClass=typeof(SoldierTrialDamage)};
        var enabled=ClassProgressionFeature.Enabled;ClassProgressionFeature.Enabled=true;
        var harmonyType=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var patchId="rwom.test.soldierweapons."+Guid.NewGuid();var harmony=Activator.CreateInstance(harmonyType,patchId)!;
        try {
            var processor=harmonyType.GetMethod("CreateClassProcessor")!.Invoke(harmony,[Production("SuperSoldierWeaponsTrial")])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
            using var pre=new Patch(typeof(Pawn_HealthTracker).GetMethod("PreApplyDamage")!,nameof(SoldierSkipNotifications));
            using var post=new Patch(typeof(Pawn).GetMethod("PostApplyDamage")!,nameof(SoldierSkipNotifications));
            using var flammable=new Patch(typeof(Thing).GetProperty("FlammableNow")!.GetMethod!,nameof(SoldierSkipNotifications));
            if(exercise!=null) {SoldierTrialDamage.Hits=0;exercise(target,damage);return;}
            void Hit(float amount=5,ThingDef? used=null)=>target.TakeDamage(new DamageInfo(damage,amount,instigator:actor,weapon:used??weaponDef));
            Hit(0);Check.Equal(0,hostiles.Count,"zero damage gives no receipt");
            var other=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));other.defName="NotHeldRifle";
            Hit(5,other);Check.Equal(0,hostiles.Count,"other weapon cannot satisfy school");
            Set(target,"factionInt",actor.Faction);Hit();Check.Equal(0,hostiles.Count,"ally excluded");Set(target,"factionInt",enemy);
            if(deplete) SoldierTrialDamage.DuringHit=()=>Set(ClassIdentity.Component(owner).SpiritRebirth,"depletionPending",true);
            Hit();Check.Equal(deplete?0:1,hostiles.Count,"damage receipt belongs to exact surviving soldier");
            SoldierTrialDamage.DuringHit=null;
            Hit();Check.Equal(deplete?0:1,hostiles.Count,"repeat victim or depleted source gives no extra receipt");
            if(!deplete) {
                Set(path,"stage",InitiationStage.Master);Hit();Hit();
                var missions=(List<int>)data.GetType().GetField("Missions",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(data)!;
                Check.Equal(1,missions.Count,"master's mission day is credited once");
            }
        } finally {SoldierTrialDamage.DuringHit=null;harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]);ClassProgressionFeature.Enabled=enabled;Set(actor.Faction,"relations",oldRelations);actor.equipment=equipment;}
    }
    private static IEnumerable<TestCase> HostedSuperSoldierCases()
    {
        yield return new("SuperSoldierProjection.NativeEquippedPawnSave", SoldierNativeEquipmentSave);
        yield return new("SuperSoldierProjection.HostedEquippedPawnSave", SoldierHostedEquipmentSave);
        foreach(bool charge in new[]{false,true})
        yield return new("SuperSoldierProjection."+(charge?"ChargeSave":"MortarSave"),r=>{
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            using var buildingBase=new Patch(typeof(Building).GetMethod("ExposeData",flags)!,nameof(CancelNative));
            using var projectileBase=new Patch(typeof(AbilityUser.Projectile_AbilityBase).GetMethod("ExposeData",flags)!,nameof(CancelNative));
            var before=new SoldierOrdnanceSave();
            Set(before.Mortar,"operationStarted",true);
            Set(before.Mortar,"initialized",true);Set(before.Mortar,"mortarCount",1);Set(before.Mortar,"mortarAccuracy",1.9f);Set(before.Mortar,"mortarMaxRange",110);Set(before.Mortar,"mortarTicksToFire",734);
            before.Mortar.setTarget=new LocalTargetInfo(new IntVec3(15,0,17));
            Set(before.Charge,"initialized",true);Set(before.Charge,"verVal",3);Set(before.Charge,"ticksToDetonation",7);Set(before.Charge,"explosionCount",2);Set(before.Charge,"mightPwr",1f);
            var after=RoundTripSummon(r,"soldier-ordnance",before);
            object Field(object value,string name)=>value.GetType().GetField(name,flags)!.GetValue(value)!;
            if(!charge) {
            Check.Equal(true,(bool)Field(after.Mortar,"initialized"),"load does not reinitialize mortar volley");
            Check.Equal(true,(bool)Field(after.Mortar,"operationStarted"),"loaded operated mortar cannot regain approach grace");
            Check.Equal(1,(int)Field(after.Mortar,"mortarCount"),"spent mortar rounds stay spent");
            Check.Equal(1.9f,(float)Field(after.Mortar,"mortarAccuracy"),"saved mortar spread");
            Check.Equal(110,(int)Field(after.Mortar,"mortarMaxRange"),"range is not upgraded twice");
            Check.Equal(734,(int)Field(after.Mortar,"mortarTicksToFire"),"remaining firing deadline");
            Check.Equal(new IntVec3(15,0,17),after.Mortar.setTarget.Cell,"manual mortar target survives");
            } else {
            Check.Equal(true,(bool)Field(after.Charge,"initialized"),"charge does not read a new caster on load");
            Check.Equal(7,(int)Field(after.Charge,"ticksToDetonation"),"charge detonation delay survives");
            Check.Equal(2,(int)Field(after.Charge,"explosionCount"),"spent explosions cannot replay");
            Check.Equal(3,(int)Field(after.Charge,"verVal"),"captured charge rank survives");
            }
        });
        foreach(string check in new[]{"ChargeBlastSave","MortarResume","MortarVolley","MortarApproach","OriginalMigrationDepleted","OriginalMigration","WeaponProfile","PistolCooldown","WeaponReload","SerumHostEligibility","OriginalIdentity","OriginalAmbiguous","OriginalRetention","MortarDeployment","MortarTraining","SuppressingJob","BuckshotJob","SuppressingSave","SuppressingNativeRepeat","BuckshotNativeRepeat","DressingJob","DressingCancel","InstantOrders","Owner","CqcSource","CqcCombat","ImpactDamage","PistolWhip","NativePistolWhip","GrenadeSnapshot","ChargeSnapshot","Buckshot","Doctrine","Rifle","Dressing","MedicalWork","MedicalDepletion","WeaponService","NativeWeaponService","WeaponTrial","WeaponTrialDepletion","NativeWeaponTrial"})
            yield return new("SuperSoldierProjection."+check,r=>SpiritRebirthRitualTransactionTests.Run(r,c=> {
                using var fixture=new Fixture(r,c);
                using var definitions=SuperSoldierCareerTests.Setup(r);
                var oldSettings=TorannMagic.ModOptions.Settings.Instance;
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                try {
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);
                var path=state.Find("rwom.supersoldier")!;
                var source=c.Original.GetCompAbilityUserMight();_=source.MightData;
                source.mightPwr=2;c.Creator.GetCompAbilityUserMight().mightPwr=.5f;
                source.maxSP=1;c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;
                c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
                var api=Production("SuperSoldierMartialPath");
                if(check=="ChargeBlastSave") { SoldierChargeBlastSave(c.Creator,c.Original,r); } else if(check=="MortarResume") { SoldierMortarResume(c.Creator,c.Original,r); } else if(check=="MortarVolley") { SoldierMortarVolley(c.Creator,c.Original,r); } else if(check=="MortarApproach") { SoldierMortarApproach(c.Creator,c.Original); } else if(check=="OriginalMigrationDepleted") {
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    SoldierOriginalMigration(c.Creator);
                    Check.Equal(null,api.GetMethod("Owner",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[c.Creator]),"migration does not reactivate depleted training");
                } else if(check=="OriginalMigration") { SoldierOriginalMigration(c.Creator); } else if(check=="WeaponProfile") { SoldierWeaponProfile(c.Creator,c.Original,()=>{ var rebirth=ClassIdentity.Component(c.Original).SpiritRebirth; Set(rebirth,"recoveryTicks",SpiritRebirthState.RecoveryDurationTicks); fixture.Possession.GetDirectlyHeldThings().Remove(c.Original); GenSpawn.Spawn(c.Original,new IntVec3(15,0,15),c.Map); Check.Equal(true,SpiritRebirthStateTests.Invoke(rebirth,"TryLeaveHost",c.Original,c.Creator),"native placed soul completes host release"); }); } else if(check=="PistolCooldown") { SoldierPistolCooldown(c.Creator,c.Original); } else if(check=="WeaponReload") { foreach(var school in new[]{"Pistol","Rifle","Shotgun"}) SoldierReload(c.Creator,school); } else if(check=="SerumHostEligibility") {
                    var oldSerum=TorannMagicDefOf.TM_SS_SerumHD;
                    TorannMagicDefOf.TM_SS_SerumHD ??= new HediffDef{defName="SoldierHostedSerum",hediffClass=typeof(Hediff)};
                    try {
                        bool Eligible(Pawn p)=>(bool)api.GetMethod("CanInject",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[p])!;
                        Check.Equal(true,Eligible(c.Creator),"active hosted soldier may inject the physical carrier");
                        Check.Equal(false,Eligible(c.Original),"unspawned soul cannot receive the physical injection");
                        var serum=new Hediff{def=TorannMagicDefOf.TM_SS_SerumHD,pawn=c.Creator};
                        c.Creator.health.hediffSet.hediffs.Add(serum);
                        Check.Equal(false,Eligible(c.Creator),"existing carrier serum blocks another dose");
                        c.Creator.health.hediffSet.hediffs.Remove(serum);
                        Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                        Check.Equal(false,Eligible(c.Creator),"depleted source cannot confer injection eligibility");
                    } finally {TorannMagicDefOf.TM_SS_SerumHD=oldSerum;}
                } else if(check=="OriginalRetention" || check=="OriginalIdentity" || check=="OriginalAmbiguous") {
                    SoldierOriginalRetention(c.Creator,check=="OriginalIdentity"?r:null,check=="OriginalAmbiguous");
                } else if(check=="MortarDeployment") {
                    SoldierMortarDeployment(c.Creator,c.Original);
                } else if(check=="MortarTraining") {
                    var capture=typeof(Building_60mmMortar).GetMethod("CaptureTraining",BindingFlags.Instance|BindingFlags.NonPublic);
                    var operate=typeof(Building_60mmMortar).GetMethod("CanOperate",BindingFlags.Instance|BindingFlags.NonPublic);
                    Check.NotNull(capture,"hosted mortar must capture exact training before first manning tick");Check.NotNull(operate,"mortar binds original operator and source");
                    source.MightData.GetSkill_Power(TorannMagicDefOf.TM_60mmMortar).level=2;
                    source.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_60mmMortar).level=3;
                    source.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_60mmMortar).level=1;
                    var save=new SoldierOrdnanceSave();var mortar=save.Mortar;
                    Check.Equal(true,(bool)operate!.Invoke(mortar,[c.Creator])!,"unbound legacy mortar keeps ordinary operator policy");
                    Check.Equal(true,(bool)capture!.Invoke(mortar,[c.Creator,c.Original])!,"exact active source captured");
                    object Field(string name)=>typeof(Building_60mmMortar).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(mortar)!;
                    Check.Equal(110,(int)Field("mortarMaxRange"),"source versatility range");Check.Equal(4,(int)Field("mortarCount"),"source versatility ammunition");Check.Equal(2,(int)Field("pwrVal"),"source explosive power");
                    Check.Equal(true,Math.Abs((float)Field("mortarAccuracy")-3.3f)<.001f,"source efficiency accuracy");
                    Set(mortar,"mortarCount",2);
                    Check.Equal(false,(bool)capture.Invoke(mortar,[c.Creator,c.Original])!,"capture cannot replenish initialized mortar");
                    using var buildingBase=new Patch(typeof(Building).GetMethod("ExposeData")!,nameof(CancelNative));
                    using var projectileBase=new Patch(typeof(AbilityUser.Projectile_AbilityBase).GetMethod("ExposeData")!,nameof(CancelNative));
                    save=RoundTripSummon(r,"soldier-mortar-source",save,c.Creator,c.Original);mortar=save.Mortar;
                    source.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_60mmMortar).level=0;
                    Check.Equal(2,(int)Field("mortarCount"),"remaining ammunition survives source save");Check.Equal(110,(int)Field("mortarMaxRange"),"snapshot survives later source ranks");
                    Check.Equal(true,(bool)operate!.Invoke(mortar,[c.Creator])!,"saved original carrier can operate");
                    Check.Equal(false,(bool)operate.Invoke(mortar,[c.Original])!,"another operator cannot borrow source");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    Check.Equal(false,(bool)operate.Invoke(mortar,[c.Creator])!,"depleted soul cannot continue manning");
                } else if(check=="SuppressingJob" || check=="BuckshotJob") {
                    SoldierHostedBurst(r,c.Creator,c.Original,check=="BuckshotJob");
                } else if(check=="SuppressingSave") {
                    using var baseVerb=new Patch(typeof(Verb).GetMethod(nameof(Verb.ExposeData))!,nameof(CancelNative));
                    var save=new SoldierBurstSave();Set(save.Verb,"initialized",true);Set(save.Verb,"shotcount",7);
                    var restored=RoundTripSummon(r,"soldier-suppressing-counter",save);
                    Check.Equal(true,(bool)typeof(Verb_SuppressingFire).GetField("initialized",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(restored.Verb)!,"in-progress quota remains initialized");
                    Check.Equal(7,(int)typeof(Verb_SuppressingFire).GetField("shotcount",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(restored.Verb)!,"loading does not grant a fresh ammunition quota");
                } else if(check=="SuppressingNativeRepeat" || check=="BuckshotNativeRepeat") {
                    SoldierNativeBurst(c.Creator,c.Original,check=="BuckshotNativeRepeat");
                } else if(check=="InstantOrders") {
                    var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                    foreach(string spell in new[]{"PistolWhip","Mk203GL","BreachingCharge","SuperSoldierFieldDressing","60mmMortar"}) {
                        var def=DefDatabase<TMAbilityDef>.GetNamed("TM_"+spell);
                        var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                        string order="TM_SpiritCast"+spell;
                        Check.Same(power,find.Invoke(null,[c.Creator,c.Original,order])!,"hosted order resolves exact learned source: "+spell);
                        Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                        Check.Equal<object?>(null,find.Invoke(null,[c.Creator,c.Original,order]),"depletion invalidates order: "+spell);
                        Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
                        var local=c.Creator.GetCompAbilityUserMight();var duplicate=new MightAbility(local){Def=def};local.AbilityData.Powers.Add(duplicate);local.AbilityData.AllPowers=null;
                        Check.Equal<object?>(null,find.Invoke(null,[c.Creator,c.Original,order]),"local spell keeps priority: "+spell);
                        local.AbilityData.Powers.Remove(duplicate);local.AbilityData.AllPowers=null;
                        source.AbilityData.Powers.Remove(power);source.AbilityData.AllPowers=null;
                        Check.Equal<object?>(null,find.Invoke(null,[c.Creator,c.Original,order]),"unlearned spell is not granted: "+spell);
                    }
                } else if(check=="GrenadeSnapshot" || check=="ChargeSnapshot") {
                    var snapshotPatch=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.SuperSoldierOrdnanceLaunch");
                    Check.NotNull(snapshotPatch,"hosted ordnance captures training on actual launch");
                    var harmonyType=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;string id="rwom.test.ordnance."+Guid.NewGuid();var harmony=Activator.CreateInstance(harmonyType,id)!;
                    var saved=new SoldierOrdnanceSave();AbilityUser.Projectile_AbilityBase shot=check=="ChargeSnapshot"?saved.Charge:saved.Grenade;
                    var rankDef=check=="ChargeSnapshot"?TorannMagicDefOf.TM_ShotgunSpec:TorannMagicDefOf.TM_RifleSpec;
                    source.MightData.GetSkill_Versatility(rankDef).level=3;
                    var shotDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));shotDef.defName="SoldierSnapshot";shotDef.projectile=new ProjectileProperties{speed=35};shot.def=shotDef;
                    Set(shot,"mapIndexOrState",(sbyte)0);Set(shot,"positionInt",c.Creator.Position);
                    using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                    using var buildingBase=new Patch(typeof(Building).GetMethod("ExposeData")!,nameof(CancelNative));
                    using var projectileBase=new Patch(typeof(AbilityUser.Projectile_AbilityBase).GetMethod("ExposeData")!,nameof(CancelNative));
                    try {
                        var processor=harmonyType.GetMethod("CreateClassProcessor")!.Invoke(harmony,[snapshotPatch])!;processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
                        shot.Launch(c.Creator,rankDef,c.Creator.Position.ToVector3Shifted(),new LocalTargetInfo(c.Creator.Position+IntVec3.East*5),ProjectileHitFlags.All);
                        Check.Equal(3,(int)shot.GetType().GetField("verVal",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(shot)!,"rank captured before impact");
                        source.MightData.GetSkill_Versatility(rankDef).level=0;
                        Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                        var loaded=RoundTripSummon(r,"soldier-snapshot-"+check,saved);
                        var after=check=="ChargeSnapshot"?(object)loaded.Charge:loaded.Grenade;
                        Check.Equal(3,(int)after.GetType().GetField("verVal",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(after)!,"in-flight saved rank remains after source changes");
                        Check.Equal(true,(bool)after.GetType().GetField("trainingCaptured",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(after)!,"load cannot replace launch training");
                        var native=new ClassProgressionState(Vocation.Warrior);native.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);Set(ClassIdentity.Component(c.Creator),"state",native);
                        AbilityUser.Projectile_AbilityBase ordinary=check=="ChargeSnapshot"?new Projectile_BreachingCharge():new Projectile_Mk203GL();ordinary.def=shotDef;
                        Set(ordinary,"mapIndexOrState",(sbyte)0);Set(ordinary,"positionInt",c.Creator.Position);
                        ordinary.Launch(c.Creator,rankDef,c.Creator.Position.ToVector3Shifted(),new LocalTargetInfo(c.Creator.Position+IntVec3.East*5),ProjectileHitFlags.All);
                        Check.Equal(false,(bool)ordinary.GetType().GetField("trainingCaptured",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(ordinary)!,"native caster retains impact-time training rules");
                    } finally {harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
                } else if(check=="PistolWhip") {
                    SoldierWhip(c.Creator,c.Original);
                } else if(check=="NativePistolWhip") {
                    var native=new ClassProgressionState(Vocation.Warrior);native.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);Set(ClassIdentity.Component(c.Creator),"state",native);
                    SoldierWhip(c.Creator,c.Creator);
                } else if(check=="CqcCombat") {
                    SoldierWeaponTrial(c.Creator,c.Original,false,(target,damage)=>{
                        var damageField=typeof(TM_Calc).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf")!.GetField("TM_CQC")!;
                        var oldDamage=damageField.GetValue(null);var def=TorannMagicDefOf.TM_CQC;float oldCost=def.staminaCost;
                        var oldHediffs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
                        foreach(string name in new[]{"TM_Mecha-Golem_LightningCoreHD","TM_PsionicHD"})
                            if(DefDatabase<HediffDef>.GetNamedSilentFail(name)==null) DefDatabase<HediffDef>.Add(new HediffDef{defName=name});
                        using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                        using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
                        using var chance=new Patch(typeof(Rand).GetMethod(nameof(Rand.Chance))!,nameof(ShroudChance));
                        try {
                            damageField.SetValue(null,damage);def.staminaCost=.1f;
                            source.useCQCToggle=true;
                            source.Stamina.CurLevel=1;
                            source.MightData.MightPowerSkill_CQC.First(s=>s.label=="TM_CQC_pwr").level=2;
                            float before=source.Stamina.CurLevel;float cost=source.ActualStaminaCost(def);int xp=source.MightUserXP;
                            int bodyXp=c.Creator.GetCompAbilityUserMight().MightUserXP;
                            Check.Equal(true,c.Creator.RaceProps.Humanlike && c.Creator.story!=null,"CQC physical human fixture");
                            Check.Equal(true,target.Map!=null && !target.Dead && !target.Destroyed,"CQC living mapped target fixture");
                            Check.Equal(true,source.useCQCToggle && cost>0 && before>=cost,$"CQC enabled and funded fixture: stamina={before}, cost={cost}");
                            Check.Equal(true,(target.Position-c.Creator.Position).LengthHorizontal<=1.6f,"CQC adjacent target fixture");
                            Check.Same(source,api.GetMethod("CqcSource",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator])!,"combat uses exact source");
                            var hit=new DamageInfo(new DamageDef{defName="SoldierIncomingHit",harmsHealth=false},5,instigator:c.Creator);
                            void Proc(){TorannMagicMod.PostApplyDamage_Patch.Postfix(target.health,ref hit,target);}
                            Proc();Check.Equal(1,SoldierTrialDamage.Hits,"one actual CQC damage delivery");
                            Check.Equal(before-cost,source.Stamina.CurLevel,"CQC pays original stamina once");
                            Check.Equal(true,source.MightUserXP>xp,"CQC experience goes to original");
                            Check.Equal(bodyXp,c.Creator.GetCompAbilityUserMight().MightUserXP,"carrier receives no class experience");
                            float paid=source.Stamina.CurLevel;int earned=source.MightUserXP;
                            var trigger=hit;hit=new DamageInfo(damage,5,instigator:c.Creator);Proc();hit=trigger;
                            Check.Equal(1,SoldierTrialDamage.Hits,"CQC damage cannot recursively trigger CQC");
                            source.useCQCToggle=false;Proc();source.useCQCToggle=true;
                            source.Stamina.CurLevel=0;Proc();
                            Check.Equal(1,SoldierTrialDamage.Hits,"unfunded CQC cannot strike");source.Stamina.CurLevel=paid;
                            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);Proc();
                            Check.Equal(1,SoldierTrialDamage.Hits,"toggle and depletion suppress further blows");
                            Check.Equal(paid,source.Stamina.CurLevel,"rejected procs do not spend");Check.Equal(earned,source.MightUserXP,"rejected procs do not grant XP");
                        } finally {damageField.SetValue(null,oldDamage);def.staminaCost=oldCost;DefDatabase<HediffDef>.Clear();foreach(var hd in oldHediffs) DefDatabase<HediffDef>.Add(hd);}
                    });
                } else if(check=="CqcSource") {
                    var resolve=api.GetMethod("CqcSource",BindingFlags.NonPublic|BindingFlags.Static);
                    Check.NotNull(resolve,"one CQC source for command and combat");
                    Check.Same(source,resolve!.Invoke(null,[c.Creator])!,"borrowed CQC uses soul training");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    Check.Equal(true,resolve.Invoke(null,[c.Creator])==null,"depletion removes borrowed CQC");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
                    var native=new ClassProgressionState(Vocation.Warrior);native.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);Set(ClassIdentity.Component(c.Creator),"state",native);
                    Check.Same(c.Creator.GetCompAbilityUserMight(),resolve.Invoke(null,[c.Creator])!,"native CQC keeps priority");
                } else if(check=="ImpactDamage") {
                    var damage=api.GetMethod("ImpactDamage",BindingFlags.NonPublic|BindingFlags.Static);
                    Check.NotNull(damage,"physical weapon damage with source strength");
                    using var neutralStats=new Patch(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValue" && m.GetParameters().Length==4 && m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(SoldierMedicinePotency));
                    source.weaponDamage=99;
                    Check.Equal(2f,(float)damage!.Invoke(null,[c.Creator,source])!,"unarmed physical DPS with soul strength, not soul weapon cache");
                    Check.Equal(99f,(float)damage.Invoke(null,[c.Original,source])!,"native cached damage unchanged");
                } else if(check=="WeaponTrial" || check=="WeaponTrialDepletion") {
                    SoldierWeaponTrial(c.Creator,c.Original,check=="WeaponTrialDepletion");
                } else if(check=="NativeWeaponTrial") {
                    var native=new ClassProgressionState(Vocation.Warrior);native.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);
                    Set(ClassIdentity.Component(c.Creator),"state",native);
                    SoldierWeaponTrial(c.Creator,c.Creator,false);
                } else if(check=="NativeWeaponService") {
                    var native=new ClassProgressionState(Vocation.Warrior);native.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);
                    Set(ClassIdentity.Component(c.Creator),"state",native);
                    BladeServiceTests.Verify(r,c.Creator,soldier:true);
                } else if(check=="WeaponService") {
                    BladeServiceTests.Verify(r,c.Creator,c.Original,soldier:true);
                } else if(check=="Owner") {
                    var owner=api.GetMethod("Owner",BindingFlags.NonPublic|BindingFlags.Static);
                    Check.NotNull(owner,"shared soldier ownership resolver");
                    Check.Same(c.Original,owner!.Invoke(null,[c.Creator])!,"physical carrier uses original training");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    Check.Equal(true,owner.Invoke(null,[c.Creator])==null,"depletion revokes borrowed training");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
                    var native=new ClassProgressionState(Vocation.Warrior);native.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);
                    Set(ClassIdentity.Component(c.Creator),"state",native);
                    Check.Same(c.Creator,owner.Invoke(null,[c.Creator])!,"local soldier retains priority");
                } else if(check=="Buckshot") {
                    source.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_ShotgunSpec).level=2;
                    Check.Equal(14,Verb_Buckshot.GetShotCount(c.Original),"native learned pellet count");
                    Check.Equal(14,Verb_Buckshot.GetShotCount(c.Creator),"hosted pellets use soul power and efficiency");
                } else if(check=="Rifle") {
                    using var neutralStats=new Patch(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValue" && m.GetParameters().Length==4 && m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(SoldierMedicinePotency));
                    source.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_RifleSpec).level=2;
                    var projectile=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));projectile.defName="HostedSoldierBullet";projectile.comps=new();projectile.projectile=new ProjectileProperties{damageDef=DamageDefOf.Bullet,stoppingPower=0};
                    var weaponDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));weaponDef.defName="HostedSoldierRifle";weaponDef.category=ThingCategory.Item;weaponDef.equipmentType=EquipmentType.Primary;weaponDef.comps=new();
                    Set(projectile.projectile,"damageAmountBase",10);
                    Set(weaponDef,"verbs",new List<VerbProperties>{new(){verbClass=typeof(Verb_Shoot),defaultProjectile=projectile,burstShotCount=3,warmupTime=1,defaultCooldownTime=1}});
                    var weapon=new ThingWithComps{def=weaponDef};var equippable=new CompEquippable{parent=weapon};equippable.Initialize(new CompProperties{compClass=typeof(CompEquippable)});Set(weapon,"comps",new List<ThingComp>{equippable});
                    c.Creator.equipment=new Pawn_EquipmentTracker(c.Creator);c.Creator.equipment.AddEquipment(weapon);
                    Check.Equal(33,Verb_SuppressingFire.GetShotCount(c.Creator),"physical rifle damage/burst with source training and power");
                    TM_Calc.DamageCache_Ranged[c.Creator]=777;
                    try {
                        var damage=api.GetMethod("ImpactDamage",BindingFlags.NonPublic|BindingFlags.Static)!;
                        Check.Equal(12f,(float)damage.Invoke(null,[c.Creator,source])!,"borrowed impact uses physical rifle and source strength despite stale body cache");
                        source.mightPwr=3;
                        Check.Equal(18f,(float)damage.Invoke(null,[c.Creator,source])!,"strength changes apply without reusing a previous soul's damage");
                    } finally {TM_Calc.DamageCache_Ranged.Remove(c.Creator);}
                } else if(check=="MedicalWork" || check=="MedicalDepletion") {
                    Set(path,"stage",InitiationStage.Adept);
                    var enabled=ClassProgressionFeature.Enabled;ClassProgressionFeature.Enabled=true;
                    try {
                        SuperSoldierCareerTests.Supplies(c.Creator,c.Original,check=="MedicalDepletion"?()=>Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true):null,check!="MedicalDepletion");
                        Check.Equal(true,ClassIdentity.Component(c.Creator).State.Find("rwom.supersoldier")==null,"medical work cannot create body discipline");
                    } finally {ClassProgressionFeature.Enabled=enabled;}
                } else if(check=="Dressing" || check=="DressingJob" || check=="DressingCancel") {
                    Set(path,"stage",InitiationStage.Adept);
                    var wound=new SoldierWound{pawn=c.Creator,def=new HediffDef{defName="HostedSoldierWound",tendable=true}};
                    var item=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));item.defName="HostedSoldierMedicine";item.category=ThingCategory.Item;item.stackLimit=20;item.thingClass=typeof(Medicine);item.comps=new();item.destroyable=true;
                    var med=new Medicine{def=item,stackCount=3,thingIDNumber=61322};
                    using var potency=new Patch(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValue" && m.GetParameters().Length==4 && m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(SoldierMedicinePotency));
                    Check.Equal(true,c.Creator.inventory.innerContainer.TryAdd(med),"medicine owned by physical body");
                    c.Creator.health.hediffSet.hediffs.Add(wound);
                    if(check!="Dressing") {SoldierDressingJob(r,c.Creator,c.Original,wound,med,check=="DressingCancel");return;}
                    var dress=typeof(Verb_SuperSoldierFieldDressing).GetMethod("Dress",BindingFlags.NonPublic|BindingFlags.Static)!;
                    Check.Equal(true,(bool)dress.Invoke(null,[c.Creator])!,"source-trained field dressing treats carrier");
                    Check.Equal(true,wound.Bandaged,"physical wound tended");
                    Check.Equal(2,med.stackCount,"one physical medicine consumed");
                    Check.Equal(false,(bool)dress.Invoke(null,[c.Creator])!,"no second charge for already tended wound");
                    Check.Equal(2,med.stackCount,"rejected repeat keeps medicine");
                    wound.Bandaged=false;Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    Check.Equal(false,(bool)dress.Invoke(null,[c.Creator])!,"depleted spirit cannot supply field training");
                    Check.Equal(2,med.stackCount,"depletion does not consume body medicine");
                } else {
                    Set(path,"stage",InitiationStage.Ascended);
                    var training=api.GetMethod("Training",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[path])!;
                    Set(training,"Support",true);
                    var stat=new SuperSoldierDoctrineStat{fieldMedicine=true};
                    float value=1;stat.TransformValue(StatRequest.For(c.Creator),ref value);
                    Check.Equal(1.15f,value,"physical healer receives original ascended doctrine");
                    Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                    value=1;stat.TransformValue(StatRequest.For(c.Creator),ref value);
                    Check.Equal(1f,value,"depleted spirit cannot grant doctrine");
                }
                } finally { TorannMagic.ModOptions.Settings.Instance=oldSettings; }
            }));
    }
}
