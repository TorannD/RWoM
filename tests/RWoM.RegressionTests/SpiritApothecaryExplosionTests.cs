using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static partial class SpiritMapCollapseTests
{
    private static bool QuietApothecaryExplosion(ref bool doVisualEffects,ref bool doSoundEffects)
    { doVisualEffects=false;doSoundEffects=false;return true; }

    private static void ApothecaryPoisonImpact(RuntimeFixture runtime,Map map,Projectile_PoisonFlask shot,Pawn body)
    {
        using var quiet=new Patch(typeof(GenExplosion).GetMethod(nameof(GenExplosion.DoExplosion))!,nameof(QuietApothecaryExplosion));
        using var sounds=new SavedDefinitions<SoundDef>();using var damages=new SavedDefinitions<DamageDef>();
        using var hediffs=new SavedDefinitions<HediffDef>();
        var oldExplosion=ThingDefOf.Explosion;var oldFog=TorannMagicDefOf.Fog_Poison;
        var oldFlammability=StatDefOf.Flammability;
        var damageField=typeof(MightAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf")!.GetField("TM_Poison")!;
        var oldDamage=damageField.GetValue(null);var tick=Find.TickManager.TicksGame;
        try {
            ApothecaryExplosionMap(map);
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(BodyPartGroupDefOf));
            StatDefOf.Flammability=new StatDef{defName="Flammability",defaultBaseValue=1,workerClass=typeof(StatWorker)};
            DefDatabase<SoundDef>.Add(new SoundDef{defName="TinyBell"});
            var fog=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            fog.defName="NativePoisonFog";fog.thingClass=typeof(Gas);fog.category=ThingCategory.Gas;
            fog.comps=[];fog.size=new IntVec2(1,1);fog.stackLimit=1;fog.destroyable=true;fog.drawerType=DrawerType.RealtimeOnly;
            fog.gas=new GasProperties{expireSeconds=new FloatRange(5,5)};DefDatabase<ThingDef>.Add(fog);TorannMagicDefOf.Fog_Poison=fog;
            var damage=new DamageDef{defName="NativePoisonDamage",workerClass=typeof(DamageWorker_PoisonFlask),defaultDamage=0,canInterruptJobs=false,makesBlood=false,hasForcefulImpact=false};
            DefDatabase<DamageDef>.Add(damage);damageField.SetValue(null,damage);
            shot.Position=new IntVec3(24,0,24);
            var impact=typeof(Projectile_PoisonFlask).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!;
            impact.Invoke(shot,[null,false]);
            var explosion=(Explosion)map.thingGrid.ThingsListAt(shot.Position).Single(t=>t is Explosion);
            Check.Equal(6.4f,explosion.radius,"native poison explosion uses captured source rank");
            Set(Find.TickManager,"ticksGameInt",tick+100);
            typeof(Explosion).GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(explosion,null);
            Check.Equal(true,explosion.Destroyed,"native poison propagation finishes");
            var cloud=shot.Position.GetGas(map);
            Check.Equal(true,cloud is Gas,"native explosion creates actual gas at impact cell");
            Check.Equal(tick+640,((Gas)cloud).destroyTick,"spawned fog lifetime follows captured rank");
            Check.Equal(false,shot.Destroyed,"poison controller remains for later damage pulses");
            var lung=ApothecaryPatient(body);lung.def.tags.Add(BodyPartTagDefOf.BreathingSource);
            Set(body.jobs,"curJob",null!);Set(body.jobs,"curDriver",null!);
            map.damageWatcher=new DamageWatcher();
            body.Position=new IntVec3(25,0,24);
            if(!map.mapPawns.AllPawnsSpawned.Contains(body))map.mapPawns.RegisterPawn(body);
            var injury=new HediffDef{defName="NativePoisonInjury",hediffClass=typeof(Hediff_Injury),maxSeverity=100,injuryProps=new InjuryProps()};
            DefDatabase<HediffDef>.Add(injury);damage.hediff=injury;damage.hediffSkin=injury;damage.hediffSolid=injury;
            var interval=typeof(Projectile_PoisonFlask).GetMethod("TickInterval",BindingFlags.NonPublic|BindingFlags.Instance)!;
            interval.Invoke(shot,[39]);
            Check.Equal(39,(int)typeof(Projectile_PoisonFlask).GetField("age",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(shot)!,"cloud age follows native interval delta");
            float Wounds()=>body.health.hediffSet.hediffs.Where(h=>h.def==injury).Sum(h=>h.Severity);
            Check.Equal(0f,Wounds(),"no injury before first scheduled pulse");
            Check.Equal(0,(int)typeof(Projectile_PoisonFlask).GetField("lastStrike",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(shot)!,"no early pulse");
            var loaded=RoundTripSummon(runtime,"native-active-poison",shot,body);
            var cell=shot.Position;shot.DeSpawn();GenSpawn.Spawn(loaded,cell,map,Rot4.North,WipeMode.Vanish,true);
            interval.Invoke(loaded,[1]);
            RuntimeLog.AssertClean();
            Check.Equal(true,body.health.hediffSet.hediffs.Any(h=>h.def==injury && h.Severity>0),"poison pulse actually injures a breathing body");
            Check.Equal(40,(int)typeof(Projectile_PoisonFlask).GetField("lastStrike",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,"pulse clock resumes at40 after native save");
            Check.Same(cloud,cell.GetGas(map),"loaded controller does not recreate gas");
            Check.Equal(false,map.thingGrid.ThingsListAt(cell).Any(t=>t is Explosion),"loaded controller does not replay explosion");
            float first=Wounds();
            interval.Invoke(loaded,[39]);
            Check.Equal(first,Wounds(),"no repeated injury between pulses");
            interval.Invoke(loaded,[1]);
            Check.Equal(true,Wounds()>first,"second scheduled pulse deals further real damage");
            body.Position=new IntVec3(5,0,24);float outside=Wounds();
            interval.Invoke(loaded,[460]);
            Check.Equal(outside,Wounds(),"leaving the cloud prevents later pulse damage");
            Check.Equal(true,loaded.Destroyed,"poison controller expires at captured lifetime");
            Check.Equal(5f,fog.gas.expireSeconds.min,"native effects never change shared trap lifetime");
        } finally {ThingDefOf.Explosion=oldExplosion;TorannMagicDefOf.Fog_Poison=oldFog;StatDefOf.Flammability=oldFlammability;damageField.SetValue(null,oldDamage);Set(Find.TickManager,"ticksGameInt",tick);}
    }

    private static void ApothecaryExplosionMap(Map map)
    {
            map.terrainGrid=new TerrainGrid(map);map.snowGrid=new SnowGrid(map);
            Set(map.pathing,"map",map);
            Set(map.pathing,"contexts",new Dictionary<PathGridDef,Verse.AI.PathingContext>());
            map.events=new MapEvents(map);
            var floor=(TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef));floor.defName="ApothecaryDryFloor";floor.tags=[];
            Array.Fill(map.terrainGrid.topGrid,floor);
            var explosionDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            explosionDef.defName="ApothecaryNativeExplosion";explosionDef.thingClass=typeof(Explosion);
            explosionDef.category=ThingCategory.Ethereal;explosionDef.size=new IntVec2(1,1);explosionDef.stackLimit=1;
            explosionDef.destroyable=true;explosionDef.drawerType=DrawerType.RealtimeOnly;explosionDef.comps=[];
            ThingDefOf.Explosion=explosionDef;DefDatabase<ThingDef>.Add(explosionDef);
    }

    private static void ApothecaryFireImpact(RuntimeFixture runtime,string rank,Map map,Projectile_FireSuppressionFlask shot)
    {
        using var quiet=new Patch(typeof(GenExplosion).GetMethod(nameof(GenExplosion.DoExplosion))!,nameof(QuietApothecaryExplosion));
        using var lesson=new Patch(typeof(LessonAutoActivator).GetMethod(nameof(LessonAutoActivator.TeachOpportunity),[typeof(ConceptDef),typeof(Thing),typeof(OpportunityType)])!,nameof(CancelNative));
        var oldExplosion=ThingDefOf.Explosion;var oldSmoke=DamageDefOf.Smoke;var oldFire=ThingDefOf.Fire;
        try {
            ApothecaryExplosionMap(map);
            DamageDefOf.Smoke=new DamageDef{defName="ApothecarySmoke"};
            var xml=System.Xml.Linq.XDocument.Load(Path.Combine(runtime.Repository,"v1.7/Defs/AbilityDefs/Abilities_TMagic_Apothecary.xml"));
            var ability=xml.Root!.Elements().Single(e=>(string?)e.Element("defName")=="TM_"+rank);
            string projectileName=ability.Element("MainVerb")!.Element("defaultProjectile")!.Value;
            var props=xml.Root.Elements().Single(e=>(string?)e.Element("defName")==projectileName).Element("projectile")!;
            float radius=(float)props.Element("explosionRadius")!;int damage=(int)props.Element("damageAmountBase")!;
            Check.Equal("Extinguish",props.Element("damageDef")!.Value,"shipped flask uses native extinguish worker");
            shot.def.projectile.explosionRadius=radius;Set(shot.def.projectile,"damageAmountBase",damage);
            shot.def.projectile.damageDef=new DamageDef{defName="ApothecaryExtinguish",workerClass=typeof(DamageWorker_Extinguish),defaultDamage=999999,harmsHealth=false,consideredHelpful=true};
            Check.Equal("Filth_FireFoam",props.Element("postExplosionSpawnThingDef")!.Value,"shipped flask leaves firefoam");
            var foam=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            foam.defName="NativeApothecaryFoam";foam.thingClass=typeof(Filth);foam.category=ThingCategory.Filth;
            foam.comps=[];foam.size=new IntVec2(1,1);foam.stackLimit=1;foam.destroyable=true;foam.drawerType=DrawerType.RealtimeOnly;
            foam.filth=new FilthProperties{placementMask=FilthSourceFlags.Natural,disappearsInDays=new FloatRange(1,1)};
            DefDatabase<ThingDef>.Add(foam);
            foreach(var terrain in map.terrainGrid.topGrid)terrain.filthAcceptanceMask=FilthSourceFlags.Natural;
            shot.def.projectile.postExplosionSpawnThingDef=foam;
            shot.def.projectile.postExplosionSpawnChance=(float)props.Element("postExplosionSpawnChance")!;
            shot.Position=new IntVec3(24,0,24);
            var fireDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            fireDef.defName="ApothecaryTestFire";fireDef.thingClass=typeof(Fire);fireDef.category=ThingCategory.Attachment;
            fireDef.size=new IntVec2(1,1);fireDef.stackLimit=1;fireDef.destroyable=true;fireDef.comps=[];fireDef.drawerType=DrawerType.RealtimeOnly;
            ThingDefOf.Fire=fireDef;DefDatabase<ThingDef>.Add(fireDef);
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(ConceptDefOf));
            var fire=(Fire)GenSpawn.Spawn(fireDef,new IntVec3(25,0,24),map);fire.fireSize=1;
            var outside=(Fire)GenSpawn.Spawn(fireDef,new IntVec3(5,0,24),map);outside.fireSize=1;
            typeof(Projectile_FireSuppressionFlask).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,[null,false]);
            var explosion=(Explosion)map.thingGrid.ThingsListAt(new IntVec3(24,0,24)).Single(t=>t is Explosion);
            Check.Equal(true,shot.Destroyed,"fire flask completes native impact");
            Check.Equal(radius,explosion.radius,"native explosion receives shipped rank radius");
            Check.Equal(damage,explosion.damAmount,"native explosion receives shipped extinguish strength");
            var tick=Find.TickManager.TicksGame;
            try {
                Set(Find.TickManager,"ticksGameInt",tick+100);
                typeof(Explosion).GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(explosion,null);
                Check.Equal(true,explosion.Destroyed,"native explosion propagates and finishes");
                Check.Equal(true,fire.Destroyed,"native extinguish damage removes actual fire");
                Check.Equal(false,outside.Destroyed,"fire outside the flask radius survives");
                Check.Equal(1f,outside.fireSize,"outside fire receives no extinguish damage");
                var deposited=(Filth)map.thingGrid.ThingsListAt(new IntVec3(24,0,24)).Single(t=>t.def==foam);
                Check.Equal(1,deposited.thickness,"native explosion deposits firefoam once");
                var saved=RoundTripSummon(runtime,"native-firefoam-"+rank,deposited);
                Check.Equal(deposited.thickness,saved.thickness,"native foam save preserves thickness");
                Check.Equal(deposited.DisappearAfterTicks,saved.DisappearAfterTicks,"native foam save preserves lifetime");
            } finally {Set(Find.TickManager,"ticksGameInt",tick);}
        } finally {ThingDefOf.Explosion=oldExplosion;DamageDefOf.Smoke=oldSmoke;ThingDefOf.Fire=oldFire;}
    }
}
