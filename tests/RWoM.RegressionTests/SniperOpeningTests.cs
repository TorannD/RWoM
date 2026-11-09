using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperOpeningTests
{
    private static Thing? testGun;
    public static bool WeaponStat(Thing thing, ref float __result)
    {
        if (thing != testGun) return true;
        __result = 1f;
        return false;
    }
    private static int shots;
    public static bool Fired(ref bool __result) { shots++; __result=true; return false; }

    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight comp, TMAbilityDef ability)
    {
        var path = ClassIdentity.Component(pawn).State.Find("rwom.sniper")!;
        var impl=BindingFlags.NonPublic|BindingFlags.Instance;
        var oldType=ability.MainVerb.verbClass;
        var oldGear=pawn.equipment;
        var oldWeather=pawn.Map.weatherManager;
        var oldDraft=pawn.jobs;
        var oldStaminaDef=TorannMagicDefOf.TM_Stamina;
        var oldJobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        var harmonyType=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        string id="rwom.regression.opening."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(harmonyType,id)!;
        var pos=pawn.Position;
        var oldFactions=(List<FactionRelation>)typeof(Faction).GetField("relations",impl)!.GetValue(pawn.Faction)!;
        try
        {
            Set(Current.Game, "questManager", new QuestManager());
            ability.MainVerb.verbClass=typeof(Verb_SniperSingleOpening);
            var owner=comp.AbilityData.Powers.OfType<SniperOpeningAbility>().Single();
            Set(owner,"verb",null!);
            var gunDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            gunDef.category=ThingCategory.Item;gunDef.equipmentType=EquipmentType.Primary;gunDef.comps=[];
            var gun=new ThingWithComps{def=gunDef};
            var eq=new CompEquippable{parent=gun};
            eq.Initialize(new CompProperties{compClass=typeof(CompEquippable)});
            var ammo=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            ammo.thingClass=typeof(Bullet);ammo.projectile=new ProjectileProperties{damageDef=new DamageDef(),speed=55};
            Set(ammo.projectile,"damageAmountBase",20);
            var shot=new Verb_Shoot{caster=pawn,verbProps=new VerbProperties{range=50,verbClass=typeof(Verb_Shoot),defaultProjectile=ammo},verbTracker=eq.verbTracker};
            Set(eq.verbTracker,"verbs",new List<Verb>{shot});Set(gun,"comps",new List<ThingComp>{eq});
            pawn.equipment=new Pawn_EquipmentTracker(pawn);pawn.equipment.AddEquipment(gun);
            pawn.Map.coverGrid=new CoverGrid(pawn.Map);
            pawn.Map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
            pawn.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
            Set(pawn,"positionInt",new IntVec3(10,0,10));
            var faction=new Faction{def=new FactionDef()};
            Set(faction,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=faction,kind=FactionRelationKind.Hostile}});
            var enemy=PawnWithComps(false,false).pawn;
            enemy.kindDef=new PawnKindDef{race=enemy.def};enemy.mindState=new Pawn_MindState(enemy);
            enemy.guest=new Pawn_GuestTracker(enemy);enemy.def.category=ThingCategory.Pawn;enemy.def.size=new IntVec2(1,1);
            Set(enemy,"mapIndexOrState",(sbyte)0);Set(enemy,"factionInt",faction);Set(enemy,"positionInt",pawn.Position+IntVec3.East*12);
            var bullet=new SniperOpeningBullet{def=ammo};
            bullet.Launch(pawn,pawn.Position.ToVector3Shifted(),enemy,enemy,ProjectileHitFlags.IntendedTarget);
            Check.Equal(24,bullet.DamageAmount,"ultimate projectile actual weapon damage x1.2");
            testGun=gun;
            var harmonyMethod=Assembly.Load("0Harmony").GetType("HarmonyLib.HarmonyMethod")!;
            var stat=typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!;
            harmonyType.GetMethod("Patch")!.Invoke(harmony,[stat,Activator.CreateInstance(harmonyMethod,typeof(SniperOpeningTests).GetMethod(nameof(WeaponStat))!),null,null,null]);
            var gunVerb=(Verb_SniperSingleOpening)owner.Verb;
            Check.Equal(45f,gunVerb.EffectiveRange,"gun range capped");
            Set(enemy,"positionInt",pawn.Position+IntVec3.East*46);
            Check.Equal(false,gunVerb.CanHitTargetFrom(pawn.Position,enemy),"beyond 45 blocked");
            Set(enemy,"positionInt",pawn.Position+IntVec3.East*12);
            if(DefDatabase<JobDef>.GetNamedSilentFail("TM_SniperSingleOpeningCast")==null)
                DefDatabase<JobDef>.Add(new JobDef{defName="TM_SniperSingleOpeningCast",driverClass=typeof(JobDriver_SniperSingleOpening)});
            if(comp.Stamina==null)
            {
                TorannMagicDefOf.TM_Stamina=new NeedDef{defName="OpeningStamina",needClass=typeof(Need_Stamina)};
                pawn.needs.AllNeeds.Add(new Need_Stamina(pawn){def=TorannMagicDefOf.TM_Stamina});
            }
            Set(pawn.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
            comp.Stamina.CurLevelPercentage=1;
            var job=owner.GetJob(AbilityContext.Player,enemy);
            var driver=new JobDriver_SniperSingleOpening{pawn=pawn,job=job};
            Check.Equal(true,driver.TryMakePreToilReservations(false),"normal readiness with ordinary gun and intended target");
            object? Call(string method,params object[] args)=>typeof(JobDriver_SniperSingleOpening).GetMethod(method,impl)!.Invoke(driver,args);
            Set(driver,"begun",true);Set(driver,"mapId",pawn.Map.uniqueID);
            pawn.jobs=new Pawn_JobTracker(pawn);
            Set(pawn.jobs,"curDriver",driver);
            float Damage(bool external)
            {
                var damage=new DamageDef();
                Set(damage,"externalViolence",external);
                var hit = new DamageInfo(damage,20f);
                ClassIdentity.Component(pawn).PostPreApplyDamage(ref hit,out _);
                return hit.Amount;
            }
            Check.Equal(24f,Damage(true),"incoming violent damage while aiming");
            Check.Equal(20f,Damage(false),"internal damage unchanged");
            Set(pawn,"positionInt",pawn.Position+IntVec3.North);
            Check.Equal(false,(bool)Call("CanContinue")!,"displacement breaks preparation");
            Set(pawn,"positionInt",job.targetC.Cell);
            int now=Find.TickManager.TicksGame;
            Call("Interrupt");Call("Interrupt");
            Check.Equal(20f,Damage(true),"ending aim clears incoming vulnerability");
            Check.Equal(true,Math.Abs(comp.Stamina.CurLevelPercentage-.94f)<.001f,"interrupt pays six percent only once");
            Check.Equal(now+6000,(int)path.GetType().GetField("OpeningReadyAt",impl)!.GetValue(path)!,"short interruption cooldown");
            Set(path,"OpeningReadyAt",0);owner.CooldownTicksLeft=0;comp.Stamina.CurLevelPercentage=1;
            driver=new JobDriver_SniperSingleOpening{pawn=pawn,job=job};
            Set(driver,"begun",true);Set(driver,"mapId",pawn.Map.uniqueID);
            var hm=Assembly.Load("0Harmony").GetType("HarmonyLib.HarmonyMethod")!;
            var prefix=Activator.CreateInstance(hm,typeof(SniperOpeningTests).GetMethod(nameof(Fired))!)!;
            harmonyType.GetMethod("Patch")!.Invoke(harmony,[typeof(Verb_SniperSingleOpening).GetMethod("Fire",impl),prefix,null,null,null]);
            shots=0;
            Call("Shoot",0);Call("Shoot",0);
            Check.Equal(1,shots,"duplicate first entry cannot fire twice");
            Check.Equal(now+120000,(int)path.GetType().GetField("OpeningReadyAt",impl)!.GetValue(path)!,"paid cooldown");
            Check.Equal(true,Math.Abs(comp.Stamina.CurLevelPercentage-.4f)<.001f,"full payment once");
            var wall=new Building{def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
            wall.def.fillPercent=1f;wall.def.size=new IntVec2(1,1);wall.def.building=new BuildingProperties();
            Set(wall,"positionInt",pawn.Position+IntVec3.East*6);Set(wall,"mapIndexOrState",(sbyte)0);
            pawn.Map.edificeGrid.Register(wall);
            Check.Equal(false,(bool)Call("CanContinue")!,"los obstruction breaks between paid rounds");
            pawn.Map.edificeGrid.DeRegister(wall);
            var file=Path.Combine(f.WorkingDirectory,"opening-job.xml");
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref driver,"driver");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            driver=null!;
            try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref driver,"driver");driver.pawn=pawn;driver.job=job;Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Call("Shoot",0);Call("Shoot",1);Call("Shoot",2);Call("Shoot",2);Call("Interrupt");
            Check.Equal(3,shots,"saved sequence completes exactly three single rounds");
            Check.Equal(true,Math.Abs(comp.Stamina.CurLevelPercentage-.4f)<.001f,"reload and interruption cannot charge twice");
        }
        finally
        {
            testGun=null;
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            DefDatabase<JobDef>.Clear();foreach(var d in oldJobs) DefDatabase<JobDef>.Add(d);
            Set(pawn.Faction,"relations",oldFactions);Set(pawn,"positionInt",pos);
            ability.MainVerb.verbClass=oldType;pawn.equipment=oldGear;pawn.jobs=oldDraft;
            pawn.Map.weatherManager=oldWeather;TorannMagicDefOf.TM_Stamina=oldStaminaDef;
        }
    }
}
