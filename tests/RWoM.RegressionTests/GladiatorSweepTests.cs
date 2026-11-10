using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class GladiatorSweepTests
{
    static readonly List<(Thing victim, float amount, DamageDef def)> hits = new();
    public static bool Capture(Thing __instance, DamageInfo dinfo)
    { hits.Add((__instance,dinfo.Amount,dinfo.Def)); return false; }
    public static bool Damage(ref float __result) { __result=20f; return false; }
    internal static void Attack(Pawn actor, RuntimeFixture fixture)
    {
        Set(Current.Game, "questManager", new QuestManager());
        var map=actor.Map;
        map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager)); map.weatherManager.curWeather=new WeatherDef {maxRangeCap=-1};
        map.info=new MapInfo {Size=new IntVec3(40,1,40)};
        map.cellIndices=new CellIndices(map); map.thingGrid=new ThingGrid(map); map.edificeGrid=new EdificeGrid(map); map.fogGrid=new FogGrid(map); map.coverGrid=new CoverGrid(map);
        var pathGrid=new PathGrid(map,new PathGridDef {defName="SweepPath"});
        map.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));
        var context=new PathingContext(map,pathGrid); Set(map.pathing,"normal",context); Set(map.pathing,"fenceBlocked",context);
        var enemy=new Faction {def=new FactionDef()};
        Set(enemy,"relations",new List<FactionRelation> {new(){other=actor.Faction,kind=FactionRelationKind.Hostile}});
        Set(actor.Faction,"relations",new List<FactionRelation> {new(){other=enemy,kind=FactionRelationKind.Hostile}});
        Pawn Place(IntVec3 delta,Faction faction)
        {
            var p=PawnWithComps(false,false).pawn;
            p.kindDef=new PawnKindDef {race=p.def}; p.def.category=ThingCategory.Pawn; p.def.size=new IntVec2(1,1);
            p.guest=new Pawn_GuestTracker(p); p.mindState=new Pawn_MindState(p);
            Set(p,"mapIndexOrState",(sbyte)0);Set(p,"positionInt",actor.Position+delta);Set(p,"factionInt",faction);
            map.thingGrid.Register(p);return p;
        }
        var primary=Place(IntVec3.North,enemy); var friend=Place(IntVec3.West,actor.Faction);
        var secondary=Place(IntVec3.East,enemy); var distant=Place(IntVec3.South*3,enemy);
        var weaponDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        var tool=new Tool {power=20};
        weaponDef.defName="SweepBlade";weaponDef.category=ThingCategory.Item;weaponDef.equipmentType=EquipmentType.Primary;
        weaponDef.tools=[tool];weaponDef.comps=[];weaponDef.drawerType=DrawerType.RealtimeOnly;
        var weapon=new ThingWithComps {def=weaponDef};
        var eq=new CompEquippable {parent=weapon};eq.Initialize(new CompProperties {compClass=typeof(CompEquippable)});
        var melee=new Verb_MeleeAttackDamage { verbProps=new VerbProperties {verbClass=typeof(Verb_MeleeAttackDamage),meleeDamageDef=DamageDefOf.Blunt}, tool=tool };
        Set(eq.verbTracker,"verbs",new List<Verb>{melee}); Set(weapon,"comps",new List<ThingComp>{eq});
        actor.jobs=new Pawn_JobTracker(actor);actor.stances=new Pawn_StanceTracker(actor);actor.equipment=new Pawn_EquipmentTracker(actor);
        actor.equipment.AddEquipment(weapon);
        var verb=new Verb_GladiatorSweep {caster=actor,verbTracker=new VerbTracker(actor),verbProps=new AbilityUser.VerbProperties_Ability { range=1.5f, targetParams=new TargetingParameters {canTargetPawns=true} }};
        var cast=typeof(Verb_GladiatorSweep).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!;
        bool Swing(Pawn p) {Set(verb,"currentTarget",new LocalTargetInfo(p)); return (bool)cast.Invoke(verb,null)!;}
        var harmony=Assembly.Load("0Harmony");var ht=harmony.GetType("HarmonyLib.Harmony")!;
        var id="rwom.sweep."+Guid.NewGuid();var instance=Activator.CreateInstance(ht,id)!;
        void Patch(MethodBase method,string prefix) => ht.GetMethod("Patch")!.Invoke(instance,[method,Activator.CreateInstance(harmony.GetType("HarmonyLib.HarmonyMethod")!,typeof(GladiatorSweepTests).GetMethod(prefix)!),null,null,null]);
        var field=typeof(Verb_GladiatorSweep).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf")!.GetField("TM_Cleave")!;
        var old=field.GetValue(null); var cleave=new DamageDef {defName="TM_Cleave"};
        try
        {
            field.SetValue(null,cleave);
            Patch(typeof(Thing).GetMethod("TakeDamage")!,nameof(Capture));
            Patch(typeof(VerbProperties).GetMethod("AdjustedMeleeDamageAmount",[typeof(Verb),typeof(Pawn)])!,nameof(Damage));
            Check.Equal(false,Swing(friend),"friendly target refused");
            Check.Equal(false,Swing(distant),"distant target refused");
            Check.Equal(true,Swing(primary),"native manual cast");
            Check.Equal(2,hits.Count,"up to two hostile recipients");
            Check.Same(primary,hits[0].victim,"chosen enemy first"); Check.Same(secondary,hits[1].victim,"adjacent hostile second");
            Check.Equal(true,hits.All(h=>h.amount==13f && h.def==cleave),"65 percent melee hit and nonrecursive damage family");
            var friends = new[] {friend, Place(IntVec3.South,actor.Faction), Place(IntVec3.North*2,actor.Faction),
                Place(IntVec3.West*2,actor.Faction), Place(IntVec3.East*3,actor.Faction)};
            foreach (var ally in friends) ally.AllComps.Add(new CompClassProgression {parent=ally});
            var spDef = new NeedDef {defName="CircleStamina",needClass=typeof(Need_Stamina)};
            TorannMagicDefOf.TM_Stamina=spDef;
            var stamina=new Need_Stamina(actor) {def=spDef};
            Set(actor.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
            actor.needs.AllNeeds.Add(stamina); stamina.CurLevelPercentage=1f;
            var oldHediffs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            try
            {
                DefDatabase<HediffDef>.Add(new HediffDef {defName="TM_LastCircleStance",hediffClass=typeof(Hediff),initialSeverity=1});
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                var casterComp=ClassIdentity.Component(actor);
                var path=casterComp.State.Find("rwom.gladiator");
                var readyAt=typeof(DisciplineProgress).GetField("CircleReadyAt",flags)!;
                var circleField=typeof(CompClassProgression).GetField("Circle",flags)!;
                var wardField=typeof(CompClassProgression).GetField("Ward",flags)!;
                var start=Find.TickManager.TicksGame;
                JobDriver_LastCircle Driver() => new JobDriver_LastCircle {pawn=actor,job=new Job {def=new JobDef {defName="TM_GladiatorLastCircle"},targetA=actor}};
                Toil[] Steps(JobDriver_LastCircle driver) => ((IEnumerable<Toil>)typeof(JobDriver_LastCircle).GetMethod("MakeNewToils",flags)!.Invoke(driver,null)!).ToArray();
                var interrupted=Driver(); var interruptedSteps=Steps(interrupted);
                interruptedSteps[0].initAction();
                typeof(JobDriver_LastCircle).GetMethod("Interrupt",flags)!.Invoke(interrupted,null);
                typeof(JobDriver_LastCircle).GetMethod("Interrupt",flags)!.Invoke(interrupted,null);
                Check.Equal(true,Math.Abs(stamina.CurLevelPercentage-.94f)<.0001,"interruption pays 6 percent once");
                Check.Equal(start+6000,(int)readyAt.GetValue(path)!,"short interruption lock");
                Set(Find.TickManager,"ticksGameInt",start+6000); start+=6000;
                stamina.CurLevelPercentage=1f;
                var driver=Driver(); var steps=Steps(driver);
                Check.Equal(300,steps[1].defaultDuration,"5s of preparation");
                steps[0].initAction();
                Check.Equal(null,circleField.GetValue(casterComp),"no early effects");
                steps[2].initAction(); steps[2].initAction();
                Check.Equal(true,Math.Abs(stamina.CurLevelPercentage-.4f)<.0001,"activation cost");
                Check.Equal(start+120000,(int)readyAt.GetValue(path)!,"two day cooldown");
                var marked=friends.Where(p=>wardField.GetValue(ClassIdentity.Component(p))!=null).ToArray();
                Check.Equal(4,marked.Length,"four nearby allies marked once");
                var guard=ClassIdentity.Component(marked[0]);
                var shot=new DamageInfo(cleave,30f,0,-1,primary);
                guard.PostPreApplyDamage(ref shot,out var absorbed);
                Check.Equal(false,absorbed,"overflow remains"); Check.Equal(5f,shot.Amount,"25 points per ally");
                shot=new DamageInfo(cleave,10f,0,-1,primary); guard.PostPreApplyDamage(ref shot,out absorbed);
                Check.Equal(10f,shot.Amount,"ward cannot refill");
                hits.Clear();
                var circle=(GladiatorCircle)circleField.GetValue(casterComp)!;
                bool Pulse(int delta) => (bool)typeof(GladiatorCircle).GetMethod("Tick",flags)!.Invoke(circle,[actor,start+delta])!;
                Pulse(299); Check.Equal(0,hits.Count,"no early strike");
                Pulse(300); Check.Equal(3,hits.Count,"first strike hits up to three enemies");
                var file=Path.Combine(fixture.WorkingDirectory,"last-circle.xml");
                try { Scribe.saver.InitSaving(file,"regression"); Scribe_Deep.Look(ref circle,"circle"); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                circle=null!;
                try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref circle,"circle"); Scribe.loader.FinalizeLoading(); }
                finally { Scribe.ForceStop(); }
                circleField.SetValue(casterComp,circle);
                Pulse(300); Pulse(599); Check.Equal(3,hits.Count,"reload did not replay strike");
                Pulse(600); Pulse(900); Pulse(1199);
                Check.Equal(9,hits.Count,"exactly three strikes");
                Check.Equal(true,hits.All(h=>h.amount==16 && h.def==cleave),"80 percent weapon damage");
                Check.Equal(false,Pulse(1200),"circle expires after 20 seconds");
            }
            finally { DefDatabase<HediffDef>.Clear(); foreach (var d in oldHediffs) DefDatabase<HediffDef>.Add(d); }

        }
        finally { hits.Clear(); ht.GetMethod("UnpatchAll")!.Invoke(instance,[id]); field.SetValue(null,old); }
    }
}
