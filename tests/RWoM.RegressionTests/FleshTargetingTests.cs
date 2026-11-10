using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.FleshDevelopment;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;
internal static class FleshTargetingTests
{
    public static void Initialize() {
        RuntimeHelpers.RunClassConstructor(typeof(ThingDefOf).TypeHandle);
        RuntimeLog.AcceptHeadlessDefOfInitialization("ThingDefOf");
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshTargeting.CorpsePassesRealEngineSelector", _ => InWorld((actor, map, targeter) => {
            var corpse = new Corpse { def = Def(ThingCategory.Item) };
            corpse.InnerPawn = Pawn(false);
            Spawn(corpse);
            FleshPresentation.Activate(actor, FleshSkill.Devour);
            var parameters = Parameters(targeter);
            Check.Equal(true, FleshActions.ValidTarget(actor, corpse), "edible spawned corpse passes action policy");
            Check.Equal(true, parameters.CanTarget(new TargetInfo(corpse)), "actual engine accepts corpse, even non-autoattackable");
            Set(corpse, "mapIndexOrState", (sbyte)-1);
            Check.Equal(false, parameters.CanTarget(new TargetInfo(corpse)), "despawned corpse rejected at selection");
        }));
        yield return new("FleshTargeting.NeutralLivingCreatureAndInvalidTargets", _ => InWorld((actor, map, targeter) => {
            var animal = Pawn(false); Spawn(animal);
            FleshPresentation.Activate(actor, FleshSkill.Devour);
            var parameters = Parameters(targeter);
            Check.Equal(true, parameters.CanTarget(new TargetInfo(animal)), "manual organic target need not already be hostile");
            animal.RaceProps.intelligence = Intelligence.Humanlike;
            Check.Equal(true, parameters.CanTarget(new TargetInfo(animal)), "living human target also selectable");
            Set(animal,"mapIndexOrState",(sbyte)1);
            Check.Equal(false, parameters.CanTarget(new TargetInfo(animal)), "other map excluded");
            Set(animal,"mapIndexOrState",(sbyte)0);
            Check.Equal(false, parameters.CanTarget(new TargetInfo(actor)), "self excluded");
            Check.Equal(false, parameters.CanTarget(new TargetInfo(new IntVec3(4,0,4),map)), "empty cells excluded");
            var item = new Thing { def = Def(ThingCategory.Item) }; Spawn(item);
            Check.Equal(false, parameters.CanTarget(new TargetInfo(item)), "ordinary items excluded");
            Set(animal.def.race, "fleshType", new FleshTypeDef { isOrganic = false });
            Check.Equal(false, parameters.CanTarget(new TargetInfo(animal)), "nonorganic pawn excluded");
        }));
        yield return new("FleshTargeting.ThirstUsesManaPanelPriority", _ => {
            var gizmo = new Gizmo_FleshThirst(null!);
            Check.Equal(-101f, gizmo.Order, "same resource priority as mana gizmo");
            Check.Equal(100f, gizmo.GetWidth(1000), "same width as mana");
        });
        // Delayed self-buff UI scheduling is exercised by FleshCastingTests.
        foreach (var skill in new[] { FleshSkill.Restore }) {
            var selected = skill;
            yield return new("FleshTargeting.OtherActive." + selected, _ => InWorld((actor,map,targeter) => {
                var p = FleshRepository.TryGet(actor); p.SkillPoints = 100; p.Experience = 40;
                foreach (FleshSkill s in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Feast)) while(p.TryUpgrade(s)) { }
                p.TryUpgrade(FleshSkill.Feast);
                FleshPresentation.Activate(actor, selected);
                Check.Equal(false, targeter.IsTargeting, "self skill does not require external target");
                Check.Equal(true, p.GetReadyTick(selected) > FleshActions.Now, "actual UI activation starts cooldown");
                if(selected == FleshSkill.Restore) Check.Equal(0f,p.Experience,"restore consumes current XP");
                if(selected == FleshSkill.Empower) Check.Equal(FleshActions.Now+18000,p.EmpowerUntil,"empower active");
                if(selected == FleshSkill.Harden) Check.Equal(FleshActions.Now+18000,p.HardenUntil,"harden active");
                if(selected == FleshSkill.Feast) Check.Equal(p.MaxThirst*.5f,p.Thirst,"feast starts at half thirst");
                float thirst=p.Thirst; int ready=p.GetReadyTick(selected);
                FleshPresentation.Activate(actor,selected);
                Check.Equal(thirst,p.Thirst,"rejected repeated UI activation costs nothing");
                Check.Equal(ready,p.GetReadyTick(selected),"rejection does not extend cooldown");
            }));
        }
    }
    static TargetingParameters Parameters(Targeter targeter) => (TargetingParameters)typeof(Targeter).GetField("targetParams", BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(targeter)!;
    static void InWorld(Action<Pawn,Map,Targeter> test) {
        object? oldRoot=typeof(Current).GetField("rootInt",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public)!.GetValue(null);
        var oldGame=Current.Game; var oldMeat=StatDefOf.MeatAmount;
        try {
            var game=Blank<Game>(); Set(game,"tickManager",new TickManager()); Current.Game=game;
            var map=Blank<Map>(); Set(game,"maps",new List<Map>{map,Blank<Map>()});
            var root=Blank<Root_Play>(); var ui=Blank<UIRoot_Play>(); var mapUi=Blank<MapInterface>(); var targeter=new Targeter();
            root.uiRoot=ui; ui.mapUI=mapUi; mapUi.targeter=targeter; Set(typeof(Current),"rootInt",root);
            StatDefOf.MeatAmount=new StatDef { defName="TargetFixtureMeat", defaultBaseValue=75f, workerClass=typeof(MeatWorker) };
            var actor=Pawn(true); Spawn(actor); FleshRepository.Ensure(actor);
            test(actor,map,targeter);
        } finally { StatDefOf.MeatAmount=oldMeat; Current.Game=oldGame; Set(typeof(Current),"rootInt",oldRoot!); }
    }
    static Pawn Pawn(bool actor) {
        var def=Def(ThingCategory.Pawn); def.race=new RaceProperties { meatDef=Def(ThingCategory.Item), intelligence=Intelligence.Animal };
        Set(def.race,"fleshType",new FleshTypeDef { isOrganic=true });
        var pawn=new Pawn { def=def, kindDef=new PawnKindDef { race=def } }; pawn.health=new Pawn_HealthTracker(pawn);
        pawn.mindState=Blank<Pawn_MindState>(); pawn.mindState.mentalStateHandler=new MentalStateHandler(pawn);
        if(actor) {
            Set(pawn,"factionInt",new Faction { def=new FactionDef { isPlayer=true } });
            pawn.health.hediffSet.hediffs.Add(new Hediff { pawn=pawn, def=new HediffDef { defName="TM_UndeadHD" } });
        }
        return pawn;
    }
    static ThingDef Def(ThingCategory category) { var def=Blank<ThingDef>(); def.category=category; def.stackLimit=1; return def; }
    static void Spawn(Thing thing) { Set(thing,"mapIndexOrState",(sbyte)0); Set(thing,"positionInt",new IntVec3(3,0,3)); }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static void Set(object target,string name,object value) {
        for(Type? t=target as Type??target.GetType();t!=null;t=t.BaseType) {
            var f=t.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);
            if(f!=null) { f.SetValue(target is Type?null:target,value); return; }
        }
        throw new Exception("Missing fixture field: "+name);
    }
    public sealed class MeatWorker:StatWorker {
        public override float GetValueUnfinalized(StatRequest req,bool applyPostProcess=true)=>75f;
        public override void FinalizeValue(StatRequest req,ref float val,bool applyPostProcess) { }
    }
}
