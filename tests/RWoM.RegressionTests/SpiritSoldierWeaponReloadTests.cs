using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private static int soldierMortarDestructions;
    private static readonly List<FlyingObject_Advanced> soldierMortarShots=new();
    private static readonly List<(IntVec3 Cell,float Radius,int Damage)> soldierMortarBlasts=new();
    private static bool SoldierObserveMortarBlast(IntVec3 center,float radius,int damAmount)
    {soldierMortarBlasts.Add((center,radius,damAmount));return false;}
    private static void SoldierObserveMortarLaunch(FlyingObject_Advanced __instance)=>soldierMortarShots.Add(__instance);
    private static bool SoldierFinishMortarJob()=>false;
    private sealed class SoldierBombDefinition:IDisposable
    {
        private readonly DamageDef previous=DamageDefOf.Bomb;
        private readonly DamageDef[] definitions=DefDatabase<DamageDef>.AllDefsListForReading.ToArray();
        public SoldierBombDefinition(){DamageDefOf.Bomb=new DamageDef{defName="SoldierFixtureBomb",workerClass=typeof(DamageWorker)};DefDatabase<DamageDef>.Add(DamageDefOf.Bomb);}
        public void Dispose(){DamageDefOf.Bomb=previous;DefDatabase<DamageDef>.Clear();foreach(var def in definitions)DefDatabase<DamageDef>.Add(def);}
    }
    private static void SoldierMortarVolley(Pawn body,Pawn soul,RuntimeFixture r)
    {
        using var damageDefinition=new SoldierBombDefinition();
        var shellDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        shellDef.defName="SoldierMortarShell";shellDef.thingClass=typeof(FlyingObject_Advanced);shellDef.category=ThingCategory.Projectile;
        shellDef.size=new IntVec2(1,1);shellDef.stackLimit=1;shellDef.comps=new();shellDef.drawerType=DrawerType.RealtimeOnly;
        shellDef.projectile=new ProjectileProperties{speed=40};shellDef.destroyable=true;
        DefDatabase<ThingDef>.Add(shellDef);
        var mortar=new Building_60mmMortar{def=shellDef,thingIDNumber=86610};
        Set(mortar,"mapIndexOrState",(sbyte)0);Set(mortar,"positionInt",body.Position);
        Set(mortar,"projectileDef",shellDef);Set(mortar,"factionInt",body.Faction);
        var mannable=new CompMannable{parent=mortar};mannable.Initialize(new CompProperties_Mannable());Set(mortar,"mannableComp",mannable);
        body.mindState??=new Verse.AI.Pawn_MindState(body);body.jobs??=new Verse.AI.Pawn_JobTracker(body);
        var api=typeof(Building_60mmMortar);
        Check.Equal(true,(bool)api.GetMethod("CaptureTraining",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(mortar,[body,soul])!,"bound volley training");
        Set(mortar,"mortarTicksToFire",Find.TickManager.TicksGame-1);Set(mortar,"mortarCount",2);
        mortar.setTarget=new LocalTargetInfo(new IntVec3(20,0,20));
        if(DefDatabase<SoundDef>.GetNamedSilentFail("Mortar_LaunchA")==null)DefDatabase<SoundDef>.Add(new SoundDef{defName="Mortar_LaunchA"});
        using var sound=new Patch(typeof(Verse.Sound.SoundStarter).GetMethod("PlayOneShot")!,nameof(CancelNative));
        using var destroy=new Patch(api.GetMethod("Destroy")!,nameof(SoldierObserveMortarDestroy));
        using var endJob=new Patch(typeof(Verse.AI.Pawn_JobTracker).GetMethod("EndCurrentJob")!,nameof(SoldierFinishMortarJob));
        using var launch=new Patch(typeof(FlyingObject_Advanced).GetMethod("AdvancedLaunch")!,nameof(SoldierObserveMortarLaunch),true);
        soldierMortarShots.Clear();soldierMortarDestructions=0;
        var tick=api.GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance)!;
        mannable.ManForATick(body);tick.Invoke(mortar,null);
        Check.Equal(1,soldierMortarShots.Count,"actual spawn and AdvancedLaunch create first shell");
        Check.Equal(true,soldierMortarShots[0].Spawned,"shell actually registered on map");
        Check.Same(body.Map,soldierMortarShots[0].Map,"shell belongs to carrier map");
        var shot=soldierMortarShots[0];
        var loaded=RoundTripSummon(r,"mortar-airborne",shot,mortar);
        foreach(string field in new[]{"trueOrigin","trueDestination","speed","curveVariance","destinationCurvePoint","impactRadius","explosionDamage","isExplosive","impactDamageType","fliesOverhead","doublesidedVariance"}) {
            var info=typeof(FlyingObject_Advanced).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!;
            Check.Equal(info.GetValue(shot),info.GetValue(loaded),"airborne mortar save retains "+field);
        }
        var curveField=typeof(FlyingObject_Advanced).GetField("curvePoints",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Check.Equal(true,((List<UnityEngine.Vector3>)curveField.GetValue(shot)!).SequenceEqual((List<UnityEngine.Vector3>)curveField.GetValue(loaded)!),"saved flight curve unchanged");
        Check.Equal(1,(int)api.GetField("mortarCount",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(mortar)!,"one round consumed");
        tick.Invoke(mortar,null);Check.Equal(1,soldierMortarShots.Count,"same tick cannot duplicate volley");
        Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);
        Set(mortar,"mortarTicksToFire",Find.TickManager.TicksGame-1);tick.Invoke(mortar,null);
        Check.Equal(1,soldierMortarShots.Count,"source lost before next volley prevents second shell");
        Check.Equal(1,soldierMortarDestructions,"invalid operator shuts down mortar");
        // Continue the actual saved projectile's curve and impact logic. Only the
        // engine blast boundary is recorded here; this is not a pawn injury test.
        var cell=shot.Position;shot.DeSpawn();GenSpawn.Spawn(loaded,cell,body.Map);
        using var dust=new Patch(typeof(FleckMaker).GetMethod("ThrowDustPuff",new[]{typeof(UnityEngine.Vector3),typeof(Map),typeof(float)})!,nameof(CancelNative));
        using var blast=new Patch(typeof(GenExplosion).GetMethods().Single(m=>m.Name=="DoExplosion"),nameof(SoldierObserveMortarBlast));
        soldierMortarBlasts.Clear();
        var flightTick=typeof(FlyingObject_Advanced).GetMethod("TickInterval",BindingFlags.NonPublic|BindingFlags.Instance)!;
        for(int i=0;i<2000 && !loaded.Destroyed;i++)flightTick.Invoke(loaded,[1]);
        Check.Equal(true,loaded.Destroyed,"saved projectile finishes native flight and destruction");
        Check.Equal(1,soldierMortarBlasts.Count,"saved shell detonates exactly once despite depleted source");
        var destination=(UnityEngine.Vector3)typeof(FlyingObject_Advanced).GetField("trueDestination",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!;
        Check.Equal(destination.ToIntVec3(),soldierMortarBlasts[0].Cell,"saved shell reaches originally chosen destination");
        Check.Equal((float)typeof(FlyingObject_Advanced).GetField("impactRadius",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,soldierMortarBlasts[0].Radius,"native blast receives saved radius");
        Check.Equal((int)typeof(FlyingObject_Advanced).GetField("explosionDamage",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,soldierMortarBlasts[0].Damage,"native blast receives saved damage");
        soldierMortarShots.Clear();
    }
    private static void SoldierMortarResume(Pawn body,Pawn soul,RuntimeFixture r)
    {
        Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",false);
        var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName="SoldierResumeMortar";def.thingClass=typeof(Building_60mmMortar);def.category=ThingCategory.Building;
        def.comps=new(){new CompProperties_Mannable()};def.size=new IntVec2(1,1);def.stackLimit=1;
        DefDatabase<ThingDef>.Add(def);
        var mortar=(Building_60mmMortar)ThingMaker.MakeThing(def);
        var api=typeof(Building_60mmMortar);
        Check.Equal(true,(bool)api.GetMethod("CaptureTraining",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(mortar,[body,soul])!,"resume captures exact source");
        Set(mortar,"operationStarted",true);Set(mortar,"mortarCount",1);
        Set(mortar,"mortarTicksToFire",Find.TickManager.TicksGame+100);
        var loaded=RoundTripSummon(r,"mortar-operated",mortar,body,soul);
        Set(loaded,"mapIndexOrState",(sbyte)0);Set(loaded,"mannableComp",loaded.GetComp<CompMannable>());
        Check.Equal(true,loaded.GetComp<CompMannable>().ManningPawn==null,"native CompMannable does not save its heartbeat");
        var previousJob=body.CurJob;
        var previousManJob=JobDefOf.ManTurret;
        JobDefOf.ManTurret=new JobDef{defName="SoldierResumeManTurret"};
        Set(body.jobs,"curJob",JobMaker.MakeJob(JobDefOf.ManTurret,loaded));
        using var observe=new Patch(api.GetMethod("Destroy")!,nameof(SoldierObserveMortarDestroy));
        var tick=api.GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance)!;
        try {
            soldierMortarDestructions=0;tick.Invoke(loaded,null);
            Check.Equal(0,soldierMortarDestructions,"loaded mortar waits for exact saved operator's first heartbeat");
            Check.Equal(1,(int)api.GetField("mortarCount",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,"resume wait never fires or refills ammunition");
            Set(body.jobs,"curJob",null);tick.Invoke(loaded,null);
            Check.Equal(1,soldierMortarDestructions,"missing operator job cannot use reload grace");
            Set(body.jobs,"curJob",JobMaker.MakeJob(JobDefOf.ManTurret,mortar));tick.Invoke(loaded,null);
            Check.Equal(2,soldierMortarDestructions,"a different mortar's job cannot use reload grace");
            Set(body.jobs,"curJob",JobMaker.MakeJob(JobDefOf.ManTurret,loaded));
            Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);tick.Invoke(loaded,null);
            Check.Equal(3,soldierMortarDestructions,"depleted source cannot use reload grace");
            Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",false);
            int now=Find.TickManager.TicksGame;
            try {
                Find.TickManager.DebugSetTicksGame(now+2);tick.Invoke(loaded,null);
                Check.Equal(4,soldierMortarDestructions,"missing heartbeat expires after one tick even with same job");
            } finally {Find.TickManager.DebugSetTicksGame(now);}
            loaded.GetComp<CompMannable>().ManForATick(body);tick.Invoke(loaded,null);
            Check.Equal(4,soldierMortarDestructions,"native operator heartbeat resumes service");
            Check.Equal(1,(int)api.GetField("mortarCount",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,"resume preserves spent ammunition");
            Set(loaded.GetComp<CompMannable>(),"lastManPawn",null);tick.Invoke(loaded,null);
            Check.Equal(5,soldierMortarDestructions,"after first heartbeat abandonment is immediate again");
        } finally {Set(body.jobs,"curJob",previousJob);JobDefOf.ManTurret=previousManJob;}
    }
    private static void SoldierChargeBlastSave(Pawn body,Pawn soul,RuntimeFixture r)
    {
        using var damageDefinition=new SoldierBombDefinition();
        var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName="SoldierSaveCharge";def.thingClass=typeof(Projectile_BreachingCharge);def.category=ThingCategory.Projectile;
        def.comps=new();def.size=new IntVec2(1,1);def.stackLimit=1;def.destroyable=true;def.drawerType=DrawerType.RealtimeOnly;
        def.projectile=new ProjectileProperties{speed=20,explosionRadius=7,explosionDelay=210,damageDef=DamageDefOf.Bomb};
        Set(def.projectile,"damageAmountBase",15);
        DefDatabase<ThingDef>.Add(def);
        var shot=(Projectile_BreachingCharge)GenSpawn.Spawn(def,body.Position,body.Map);
        SpellItemEffectTests.SetProjectileField(shot,"launcher",body);SpellItemEffectTests.SetProjectileField(shot,"landed",true);
        // This fixture starts with a landed, initialized charge and two of its
        // six blasts already spent; launch/impact admission is tested separately.
        Set(shot,"initialized",true);Set(shot,"trainingCaptured",true);Set(shot,"verVal",3);
        Set(shot,"explosionCount",4);Set(shot,"ticksToDetonation",2);
        var loaded=RoundTripSummon(r,"charge-mid-detonation",shot,body);
        var position=shot.Position;shot.DeSpawn();GenSpawn.Spawn(loaded,position,body.Map);
        var previousEffect=TorannMagicDefOf.GiantExplosion;
        TorannMagicDefOf.GiantExplosion=new EffecterDef{defName="SoldierEmptyBlastEffect",children=new()};
        using var blast=new Patch(typeof(GenExplosion).GetMethods().Single(m=>m.Name=="DoExplosion"),nameof(SoldierObserveMortarBlast));
        soldierMortarBlasts.Clear();
        try {
            var tick=typeof(Projectile_BreachingCharge).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!;
            tick.Invoke(loaded,null);Check.Equal(0,soldierMortarBlasts.Count,"saved detonation delay respected");
            for(int i=0;i<5;i++)tick.Invoke(loaded,null);
            Check.Equal(4,soldierMortarBlasts.Count,"only four unspent charge blasts execute after load");
            Check.Equal(true,soldierMortarBlasts.Select(b=>b.Radius).SequenceEqual(new[]{3f,4f,5f,6f}),"saved blast radius progression continues without restarting");
            Check.Equal(true,soldierMortarBlasts.All(b=>b.Damage==22),"captured rank keeps blast damage");
            Check.Equal(true,loaded.Destroyed,"finished charge leaves no persistent ticking projectile");
        } finally {TorannMagicDefOf.GiantExplosion=previousEffect;}
    }
    private static bool SoldierObserveMortarDestroy(){soldierMortarDestructions++;return false;}
    private static void SoldierMortarApproach(Pawn body,Pawn soul)
    {
        var mortar=new Building_60mmMortar();
        var api=typeof(Building_60mmMortar);
        Check.Equal(true,(bool)api.GetMethod("CaptureTraining",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(mortar,[body,soul])!,"deployment binds training before operator approaches");
        Set(mortar,"mapIndexOrState",(sbyte)0);
        var tick=api.GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance)!;
        using var observe=new Patch(api.GetMethod("Destroy")!,nameof(SoldierObserveMortarDestroy));
        soldierMortarDestructions=0;
        tick.Invoke(mortar,null);
        Check.Equal(0,soldierMortarDestructions,"deployed mortar survives first tick while assigned carrier approaches");
        Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);
        tick.Invoke(mortar,null);
        Check.Equal(1,soldierMortarDestructions,"depleted training cannot keep abandoned deployment alive");
        Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",false);
        Set(mortar,"mortarTicksToFire",Find.TickManager.TicksGame-1);
        tick.Invoke(mortar,null);
        Check.Equal(2,soldierMortarDestructions,"unattended deployment expires after initial setup window");
        Set(mortar,"mortarTicksToFire",Find.TickManager.TicksGame+300);
        Set(mortar,"operationStarted",true);
        tick.Invoke(mortar,null);
        Check.Equal(3,soldierMortarDestructions,"previously operated mortar does not receive another approach window");
        Set(mortar,"operationStarted",false);Set(mortar,"hostedTraining",false);
        tick.Invoke(mortar,null);
        Check.Equal(4,soldierMortarDestructions,"ordinary legacy unmanned behavior unchanged");
    }
    private sealed class SoldierRetainedComponent:ThingComp
    {
        internal int Payload=97;
        internal bool DestroyNotified;
        public override void PostDestroy(DestroyMode mode,Map previousMap){DestroyNotified=true;}
    }
    private static ThingWithComps? soldierRejectedOriginal;
    private static void SoldierRejectOriginal(ThingWithComps eq)
    {
        if(eq==soldierRejectedOriginal)throw new InvalidOperationException("Injected original equip failure");
    }
    private static void SoldierOriginalMigration(Pawn body)
    {
        var comp=body.GetCompAbilityUserMight();var oldEquipment=body.equipment;var oldContainer=comp.equipmentContainer;int oldRegistry=comp.specWpnRegNum;
        var field=typeof(CompAbilityUserMight).GetField("specializedWeapon",BindingFlags.NonPublic|BindingFlags.Instance)!;var oldBinding=field.GetValue(comp);
        var rebuild=Production("SuperSoldierWeaponReload").GetMethod("Rebuild",BindingFlags.NonPublic|BindingFlags.Static)!;
        using var failure=new Patch(typeof(Pawn_EquipmentTracker).GetMethod("Notify_EquipmentAdded")!,nameof(SoldierRejectOriginal));
        try {
            foreach(string school in new[]{"Pistol","Rifle","Shotgun"}) {
                body.equipment=new Pawn_EquipmentTracker(body);comp.specWpnRegNum=19;
                var original=SoldierReloadItem("Retained"+school,school);var other=SoldierReloadItem("Other"+school,school);
                var copy=SoldierReloadItem("TM_"+school+"Spec_Base19","renamed copy");original.HitPoints=100;copy.HitPoints=73;
                var retained=new SoldierRetainedComponent{parent=original};original.AllComps.Add(retained);copy.AllComps.Add(retained);
                comp.equipmentContainer=new ThingOwner<ThingWithComps>();comp.equipmentContainer.TryAdd(other);comp.equipmentContainer.TryAdd(original);body.equipment.AddEquipment(copy);
                var binding=Activator.CreateInstance(field.FieldType)!;field.SetValue(comp,binding);
                binding.GetType().GetMethod("Bind",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(binding,[original,copy]);
                soldierRejectedOriginal=original;
                try {rebuild.Invoke(null,[comp]);throw new InvalidOperationException("expected equip failure");}
                catch(TargetInvocationException ex) when(ex.InnerException?.Message=="Injected original equip failure") {}
                Check.Same(copy,body.equipment.Primary,"failed original equip restores old copy "+school);
                Check.Same(comp.equipmentContainer,original.holdingOwner,"failed original equip restores container ownership");
                Check.Equal(100,original.HitPoints,"failed original equip restores cached condition");
                Check.Equal(false,copy.Destroyed,"failed original equip keeps copy alive");
                Check.Equal(19,comp.specWpnRegNum,"failed original equip keeps registry");
                soldierRejectedOriginal=null;
                Check.Equal(true,(bool)rebuild.Invoke(null,[comp])!,"new progression restores original despite historical registry");
                Check.Same(original,body.equipment.Primary,"exact original equipped, not first same-name item");
                Check.Same(body.equipment.GetDirectlyHeldThings(),original.holdingOwner,"real equipment owner");
                Check.Equal(73,original.HitPoints,"physical wear preserved");Check.Equal(-1,comp.specWpnRegNum,"new profile no longer reserves registry");
                Check.Equal(true,copy.Destroyed,"proxy discarded only after transfer");
                Check.Equal(false,retained.DestroyNotified,"discarding legacy alias never destroys original component state");
                Check.Equal(97,retained.Payload,"arbitrary addon state retained");Check.Same(original,retained.parent,"component still belongs to original");
                Check.Equal(true,original.AllComps.Contains(retained),"original component identity preserved");
                Check.Equal(true,comp.equipmentContainer.Contains(other),"unrelated original remains held");
                Check.Equal(false,(bool)rebuild.Invoke(null,[comp])!,"repeat load cannot replace original or duplicate items");
            }
        } finally {soldierRejectedOriginal=null;body.equipment=oldEquipment;comp.equipmentContainer=oldContainer;field.SetValue(comp,oldBinding);comp.specWpnRegNum=oldRegistry;}
    }
    private static bool soldierRebuildFails;
    private static string soldierReloadSchool="Rifle";
    private static ThingWithComps? soldierRebuildOriginal;
    private static bool SoldierRebuildFactory(Pawn caster,ThingWithComps thing)
    {
        soldierRebuildOriginal=thing;
        caster.GetCompAbilityUserMight().specWpnRegNum=2;
        if(soldierRebuildFails) throw new InvalidOperationException("Injected weapon creation failure");
        var replacement=SoldierReloadItem("TM_"+soldierReloadSchool+"Spec_Base2","replacement");
        replacement.HitPoints=thing.HitPoints;
        caster.equipment.AddEquipment(replacement);
        var binding=typeof(CompAbilityUserMight).GetField("specializedWeapon",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(caster.GetCompAbilityUserMight())!;
        binding.GetType().GetMethod("Bind",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(binding,[thing,replacement]);
        return false;
    }
    private static ThingWithComps SoldierReloadItem(string name,string label)
    {
        var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName=name;def.label=label;def.category=ThingCategory.Item;def.stackLimit=1;def.destroyable=true;
        def.equipmentType=EquipmentType.Primary;def.comps=new();
        Set(def,"verbs",new List<VerbProperties>{new(){verbClass=typeof(Verb_Shoot)}});
        var item=new ThingWithComps{def=def,stackCount=1};
        var eq=new CompEquippable{parent=item};eq.Initialize(new CompProperties{compClass=typeof(CompEquippable)});
        Set(item,"comps",new List<ThingComp>{eq});
        return item;
    }
    private static void SoldierReload(Pawn body,string school)
    {
        var api=Production("SuperSoldierWeaponReload");
        Check.NotNull(api,"weapon reload transaction exists");
        var rebuild=api.GetMethod("Rebuild",BindingFlags.NonPublic|BindingFlags.Static)!;
        var comp=body.GetCompAbilityUserMight();var oldEquipment=body.equipment;var oldContainer=comp.equipmentContainer;
        var field=typeof(CompAbilityUserMight).GetField("specializedWeapon",BindingFlags.NonPublic|BindingFlags.Instance)!;var oldBinding=field.GetValue(comp);
        var oldRegistry=comp.specWpnRegNum;
        var nativeTrait=new Trait(TorannMagicDefOf.TM_SuperSoldier);body.story.traits.allTraits.Add(nativeTrait);
        soldierReloadSchool=school;
        using var factory=new Patch(typeof(TM_Action).GetMethod("DoAction_"+school+"SpecCopy")!,nameof(SoldierRebuildFactory));
        try {
            body.equipment=new Pawn_EquipmentTracker(body);comp.specWpnRegNum=-1;
            var original=SoldierReloadItem("SavedRifle","rifle");var other=SoldierReloadItem("OtherSavedRifle","rifle");
            var copy=SoldierReloadItem("TM_"+school+"Spec_Base1","renamed copy");original.HitPoints=100;copy.HitPoints=73;
            comp.equipmentContainer=new ThingOwner<ThingWithComps>();comp.equipmentContainer.TryAdd(other);comp.equipmentContainer.TryAdd(original);
            body.equipment.AddEquipment(copy);
            var binding=Activator.CreateInstance(field.FieldType)!;field.SetValue(comp,binding);
            binding.GetType().GetMethod("Bind",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(binding,[original,copy]);
            var power=comp.MightData.MightPowersSS.First(p=>p.abilityDef.defName=="TM_"+school+"Spec");var oldLearned=power.learned;power.learned=true;
            try {
                soldierRebuildFails=true;
                try {rebuild.Invoke(null,[comp]);throw new InvalidOperationException("expected creation failure");}
                catch(TargetInvocationException ex) when(ex.InnerException?.Message=="Injected weapon creation failure") {}
                Check.Same(original,soldierRebuildOriginal!,"reload uses exact original, not first same-name item");
                Check.Same(copy,body.equipment.Primary,"creation failure restores equipped copy");
                Check.Equal(false,copy.Destroyed,"failed replacement does not destroy old equipment");
                Check.Equal(100,original.HitPoints,"failure restores original cached condition");
                Check.Equal(-1,comp.specWpnRegNum,"failure rolls back provisional registration");
                Check.Same(comp.equipmentContainer,original.holdingOwner,"failed replacement retains original owner");
                soldierRebuildFails=false;
                Check.Equal(true,(bool)rebuild.Invoke(null,[comp])!,"successful replacement committed");
                Check.Equal(true,copy.Destroyed,"old copy destroyed only after successful replacement");
                Check.Equal(true,comp.equipmentContainer.Contains(other),"unrelated original retained");
                Check.Equal(true,comp.equipmentContainer.Contains(original),"exact original retained for future return");
                Check.Equal(false,ReferenceEquals(copy,body.equipment.Primary),"new equipment attached");
                Check.Equal(73,body.equipment.Primary.HitPoints,"reload does not repair equipped weapon");
                var attached=body.equipment.Primary;
                Check.Equal(false,(bool)rebuild.Invoke(null,[comp])!,"already restored registration is not rebuilt again");
                Check.Same(attached,body.equipment.Primary,"repeat initialization preserves attached copy");
            } finally {power.learned=oldLearned;}
        } finally {body.story.traits.allTraits.Remove(nativeTrait);body.equipment=oldEquipment;comp.equipmentContainer=oldContainer;field.SetValue(comp,oldBinding);comp.specWpnRegNum=oldRegistry;soldierRebuildOriginal=null;soldierRebuildFails=false;}
    }
}
