using System.Reflection;
using System.Runtime.Loader;
using TorannMagic;
using Verse;
using RimWorld;
namespace RWoM.RegressionTests;
internal static class OdysseyCombatBeamTests
{
    public static IEnumerable<TestCase> Cases() {
        yield return new("Odyssey.CombatBeam.OptionalPatchDiscovery", f => Optional());
        yield return new("Odyssey.CombatBeam.NativeBeforeDamageAndFire", f => { T("MagicalCombatNativeBeamPatch"); Fixture(Native); });
        yield return new("Odyssey.CombatBeam.RemoteTargetNetworkOnly", f => Fixture(RemoteTarget));
        yield return new("Odyssey.CombatBeam.InstalledVgePaths", f => Fixture(Installed));
        yield return new("Odyssey.CombatBeam.OptionalAndPulsePayment", f => { T("MagicalCombatAnticraftPulsePatch"); Fixture(Pulses); });
    }
    static Type T(string n)
    {
        var owner = n == "MagicalCombatNativeBeamPatch" ? typeof(MagicData).Assembly
            : Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER") ?? throw new Exception("Missing VGE adapter build"));
        return owner.GetType("TorannMagic.Odyssey." + n) ?? throw new Exception("Missing beam adapter: " + n);
    }
    static object Core(string n, params object[] args) => typeof(OdysseyCombatTests).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args)!;
    static void Fixture(Action action) => Core("Run",action);
    static void Set(object o,string n,object v) => Core("Set",o,n,v);
    static ThingComp Generator() { var main=(ThingComp)Core("Source",15,15,false); Set(main,"currentHitPoints",500f); Core("Tick",60); return main; }
    static float HP(ThingComp c) => (float)Core("HP",c);
    static Thing Enemy() { var b=(Building)Core("Building",30,15); var faction=new Faction{def=new FactionDef()}; var own=b.Faction; Set(faction,"relations",new List<FactionRelation>{new FactionRelation{other=own,kind=FactionRelationKind.Hostile}}); Set(b,"factionInt",faction); return b; }
        static void Optional() {
        using var patches=new Patch();
        foreach(var n in new[]{"MagicalCombatArtilleryBeamPatch","MagicalCombatAnticraftImpactPatch","MagicalCombatAnticraftPulsePatch"}) {
            Check.Equal(true, typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey."+n) == null,"VGE beam patch has one external owner");
            var type=T(n); var target=type.GetMethod("TargetMethod")!.Invoke(null,null);
            Check.Equal(target!=null,(bool)type.GetMethod("Prepare")!.Invoke(null,null)!,"optional Prepare agrees with actual assembly availability");
            patches.Discover(type);
        }
    }
    static void Native() {
        var main=Generator(); var enemy=Enemy(); var beam=new Verb_ShootBeam{verbProps=new VerbProperties{beamDamageDef=new DamageDef{defaultDamage=20},beamTotalDamage=80,beamSetsGroundOnFire=true,beamChanceToStartFire=1}};
        Set(beam,"caster",enemy); Set(beam,"pathCells",new HashSet<IntVec3>{new(1,0,1),new(2,0,1),new(3,0,1),new(4,0,1)});
        using var patches=new Patch(); patches.Apply(typeof(Verb_ShootBeam).GetMethod("HitCell",BindingFlags.Instance|BindingFlags.NonPublic)!,T("MagicalCombatNativeBeamPatch"));
        float before=HP(main);
        typeof(Verb_ShootBeam).GetMethod("HitCell",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(beam,new object[]{new IntVec3(15,0,15),enemy.Position,.5f});
        Check.Equal(before-10,HP(main),"real HitCell stopped before damage and guaranteed ground fire; proportional damage paid");
    }
    static void Pulses() {
        var main=Generator(); var enemy=Enemy(); var strike=new OrbitalStrike{instigator=enemy}; Set(strike,"mapIndexOrState",(sbyte)0);
        var method=T("MagicalCombatAnticraftPulsePatch").GetMethod("AllowPulse")!;
        bool Allow(IntVec3 cell)=>(bool)method.Invoke(null,new object[]{cell,main.parent.Map,strike})!;
        float before=HP(main);
        Check.Equal(false,Allow(new(15,0,15)),"covered hostile pulse blocked"); Check.Equal(before-100,HP(main),"fixed100 per cell");
        Check.Equal(false,Allow(new(15,0,15)),"second same cell pulse still blocked"); Check.Equal(before-200,HP(main),"distinct pulse pays again even same tick/cell");
        Check.Equal(true,Allow(new(50,0,50)),"uncovered passes"); Check.Equal(false,Allow(new(-1,0,-1)),"out of bounds preserves native skip"); Check.Equal(before-200,HP(main),"no off-field payment");
        Set(enemy,"positionInt",new IntVec3(15,0,15)); Check.Equal(true,Allow(new(15,0,15)),"inside hostile emitter passes");
        strike.instigator=main.parent; Check.Equal(true,Allow(new(15,0,15)),"friendly passes"); Check.Equal(before-200,HP(main),"friendly no payment");
        Check.Equal(false,typeof(MagicData).Assembly.GetReferencedAssemblies().Any(a=>a.Name!.StartsWith("VanillaGravship")),"optional assembly has no core reference");
    }
            static void RemoteTarget() {
        var first=Generator(); var target=(ThingComp)Core("Source",35,15,false); Set(target,"currentHitPoints",500f); Core("Tick",120);
        var enemy=Enemy(); Set(enemy,"mapIndexOrState",(sbyte)-1); var strike=new OrbitalStrike{instigator=enemy};
        float a=HP(first), b=HP(target);
        var allowed=(bool)T("MagicalCombatAnticraftPulsePatch").GetMethod("AllowPulse")!.Invoke(null,new object[]{new IntVec3(35,0,15),target.parent.Map,strike})!;
        Check.Equal(false,allowed,"remote incoming pulse intercepted at target"); Check.Equal(a,HP(first),"unrelated nearer network not charged"); Check.Equal(b-100,HP(target),"only target network charged");
    }
    static bool Skip() => false;
    static OrbitalStrike? observedStrike;
    static int observedStrikeSpawns;
    static bool CaptureStrikeSpawn(ThingDef def, IntVec3 loc, Map map, ref Thing __result) {
        if (observedStrike == null || def != observedStrike.def) return true;
        observedStrikeSpawns++;
        Set(observedStrike,"positionInt",loc);
        Set(observedStrike,"mapIndexOrState",(sbyte)0);
        __result=observedStrike;
        return false;
    }
    static bool Minimum(int minInclusive, ref int __result) { __result=minInclusive; return false; }
    static void Installed() {
        const string root="C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/";
        Assembly? Resolve(AssemblyLoadContext context,AssemblyName name) {
            foreach(var id in new[]{"2023507013","3609835606","3799737423"}) {
                var path=Path.Combine(root,id,"1.7/Assemblies",name.Name+".dll");
                if(File.Exists(path)) return context.LoadFromAssemblyPath(path);
            }
            return null;
        }
        AssemblyLoadContext.Default.Resolving+=Resolve;
        try {
            var vge=Assembly.LoadFrom(root+"3609835606/1.7/Assemblies/VanillaGravshipExpanded.dll");
            var vge2=Assembly.LoadFrom(root+"3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll");
            var main=Generator(); var enemy=Enemy();
            using var patches=new Patch();
                        patches.Apply(typeof(ContentFinder<UnityEngine.Texture2D>).GetMethod("Get",new[]{typeof(string),typeof(bool)})!,typeof(OdysseyCombatBeamTests),nameof(Skip));
            foreach(var method in typeof(MaterialPool).GetMethods().Where(m=>m.Name=="MatFrom")) patches.Apply(method,typeof(OdysseyCombatBeamTests),nameof(Skip));
            var mods=LoadedModManager.RunningModsListForReading.ToArray();
            try { LoadedModManager.RunningModsListForReading.Clear(); System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle); }
            finally { LoadedModManager.RunningModsListForReading.AddRange(mods); }
            patches.Apply(typeof(Thing).GetMethod("Destroy")!,typeof(OdysseyCombatBeamTests),nameof(Skip));
            var projectile=(Projectile)Activator.CreateInstance(vge.GetType("VanillaGravshipExpanded.Projectile_ArtilleryBeam")!)!;
            projectile.def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); projectile.def.projectile=new ProjectileProperties{damageDef=new DamageDef()}; projectile.def.size=new IntVec2(1,1);
            Set(projectile.def.projectile,"damageAmountBase",20); Set(projectile,"mapIndexOrState",(sbyte)0); Set(projectile,"launcher",enemy);
            Set(projectile,"origin",enemy.Position.ToVector3Shifted()); Set(projectile,"destination",new UnityEngine.Vector3(15.5f,0,15.5f));
            var impact=projectile.GetType().GetMethod("Impact")!;
            patches.Apply(impact,T("MagicalCombatArtilleryBeamPatch"));
            float before=HP(main); impact.Invoke(projectile,new object?[]{null,false});
            Check.Equal(before-20,HP(main),"installed artillery Impact paid before native fire");
            impact.Invoke(projectile,new object?[]{null,false}); Check.Equal(before-20,HP(main),"same projectile delivery receipt prevents repeat debit");
            Check.Equal(false,(bool)T("MagicalCombatArtilleryBeamPatch").GetMethod("Prefix")!.Invoke(null,new object[]{projectile,true,enemy.Position.ToVector3Shifted()})!,"already magic-paid blocked delivery skips native fire branch");
            Check.Equal(before-20,HP(main),"blocked delivery does not pay twice");
            var strike=(OrbitalStrike)Activator.CreateInstance(vge2.GetType("VanillaGravshipExpanded2.AnticraftBeamStrike")!)!;
            strike.instigator=enemy; Set(strike,"mapIndexOrState",(sbyte)0); Set(strike,"positionInt",new IntVec3(15,0,15));
            // A remote saved strike has no target-map launcher coordinates.
            Set(enemy,"mapIndexOrState",(sbyte)-1);
            patches.Apply(typeof(OrbitalStrike).GetMethod("Tick",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!,typeof(OdysseyCombatBeamTests),nameof(Skip));
            patches.Apply(typeof(Rand).GetMethod("Range",new[]{typeof(int),typeof(int)})!,typeof(OdysseyCombatBeamTests),nameof(Minimum));
            patches.Apply(strike.GetType().GetMethod("Tick")!,T("MagicalCombatAnticraftPulsePatch"),"Transpiler",true);
            before=HP(main); strike.GetType().GetMethod("Tick")!.Invoke(strike,null);
            Check.Equal(before-400,HP(main),"installed remote Tick pays four pulses and skips fire explosion terrain for each");
            // Exercise the derived entry point: it creates its strike BEFORE calling VGE1 Impact.
            // Only map insertion and rendering are substituted; native strike setup and assignment run.
            var emitter=(Building)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(vge2.GetType("VanillaGravshipExpanded2.Building_EnemyAnticraftEmitter")!);
            emitter.def=enemy.def; Set(emitter,"positionInt",new IntVec3(30,0,15));
            Set(emitter,"mapIndexOrState",(sbyte)0); Set(emitter,"factionInt",enemy.Faction);
            Set(emitter,"comps",new List<ThingComp>());
            var derived=(Projectile)Activator.CreateInstance(vge2.GetType("VanillaGravshipExpanded2.Projectile_AnticraftBeam")!)!;
            derived.def=projectile.def; Set(derived,"mapIndexOrState",(sbyte)0); Set(derived,"launcher",emitter);
            Set(derived,"origin",emitter.Position.ToVector3Shifted()); Set(derived,"destination",new UnityEngine.Vector3(15.5f,0,15.5f));
            Set(derived,"intendedTarget",new LocalTargetInfo(new IntVec3(15,0,15)));
            RuntimeLog.EnsureHeadlessDefOfInitialized(vge2.GetType("VanillaGravshipExpanded2.InternalDefOf")!);
            var strikeDefField=vge2.GetType("VanillaGravshipExpanded2.InternalDefOf")!.GetField("VGE_EnemyAnticraftBeamStrike")!;
            var oldStrikeDef=strikeDefField.GetValue(null);
            observedStrike=(OrbitalStrike)Activator.CreateInstance(strike.GetType())!;
            observedStrike.def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            var beamComp=new CompOrbitalBeam(); beamComp.parent=observedStrike;
            Set(observedStrike,"comps",new List<ThingComp>{beamComp});
            observedStrikeSpawns=0;
            try {
                strikeDefField.SetValue(null,observedStrike.def);
                patches.Apply(typeof(GenSpawn).GetMethod("Spawn",new[]{typeof(ThingDef),typeof(IntVec3),typeof(Map),typeof(WipeMode)})!,typeof(OdysseyCombatBeamTests),nameof(CaptureStrikeSpawn));
                foreach(var method in typeof(CompOrbitalBeam).GetMethods().Where(m=>m.Name=="StartAnimation")) patches.Apply(method,typeof(OdysseyCombatBeamTests),nameof(Skip));
                foreach(var method in typeof(MoteMaker).GetMethods().Where(m=>m.Name=="MakePowerBeamMote")) patches.Apply(method,typeof(OdysseyCombatBeamTests),nameof(Skip));
                var derivedPatch=T("MagicalCombatAnticraftImpactPatch");
                if(derivedPatch!=null) patches.Apply(derived.GetType().GetMethod("Impact")!,derivedPatch);
                Set(main,"currentHitPoints",500f); before=HP(main);
                derived.GetType().GetMethod("Impact")!.Invoke(derived,new object?[]{null,false});
                Check.Equal(before-20,HP(main),"derived impact is charged exactly once");
                Check.Equal(0,observedStrikeSpawns,"intercepted derived anticraft impact must not spawn lasting strike");
                Check.Equal<object?>(null,emitter.GetType().GetField("currentStrike")!.GetValue(emitter),"intercepted emitter retains no strike");
                derived.GetType().GetMethod("Impact")!.Invoke(derived,new object?[]{null,true});
                Check.Equal(before-20,HP(main),"blocked delivery with magic receipt does not pay again");
                Check.Equal(0,observedStrikeSpawns,"blocked duplicate creates no strike");
                var pass=(Projectile)Activator.CreateInstance(derived.GetType())!;
                pass.def=derived.def; Set(pass,"mapIndexOrState",(sbyte)0); Set(pass,"launcher",emitter);
                Set(pass,"destination",new UnityEngine.Vector3(15.5f,0,15.5f));
                var prefix=T("MagicalCombatAnticraftImpactPatch").GetMethod("Prefix")!;
                bool Allows(bool blocked, UnityEngine.Vector3 origin)=>(bool)prefix.Invoke(null,new object[]{pass,blocked,origin})!;
                float remaining=HP(main);
                Check.Equal(true,Allows(true,emitter.Position.ToVector3Shifted()),"native blocked delivery without magic receipt is preserved");
                Set(pass,"launcher",main.parent);
                Check.Equal(true,Allows(false,emitter.Position.ToVector3Shifted()),"friendly derived beam passes");
                Set(pass,"launcher",emitter);
                Check.Equal(true,Allows(false,new UnityEngine.Vector3(15.5f,0,15.5f)),"hostile beam originating inside passes");
                Set(pass,"destination",new UnityEngine.Vector3(45.5f,0,15.5f));
                Check.Equal(true,Allows(false,emitter.Position.ToVector3Shifted()),"uncovered derived beam passes");
                Check.Equal(remaining,HP(main),"all derived bypasses leave shield balance unchanged");
            } finally { strikeDefField.SetValue(null,oldStrikeDef); observedStrike=null; observedStrikeSpawns=0; }
        } finally { AssemblyLoadContext.Default.Resolving-=Resolve; }
    }
    sealed class Patch:IDisposable {
        readonly Assembly a=Assembly.Load("0Harmony"); readonly object h; readonly string id="rwom.beams."+Guid.NewGuid();
        public Patch(){h=Activator.CreateInstance(a.GetType("HarmonyLib.Harmony")!,id)!;}
        public void Apply(MethodBase target,Type patch,string name="Prefix",bool transpiler=false){var hm=Activator.CreateInstance(a.GetType("HarmonyLib.HarmonyMethod")!,patch.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)!)!; h.GetType().GetMethod("Patch")!.Invoke(h,new object?[]{target,transpiler?null:hm,null,transpiler?hm:null,null});}
        public void Discover(Type type) { var processor=h.GetType().GetMethod("CreateClassProcessor")!.Invoke(h,new object[]{type})!; processor.GetType().GetMethod("Patch")!.Invoke(processor,null); }
        public void Dispose()=>h.GetType().GetMethod("UnpatchAll")!.Invoke(h,new object[]{id});
    }
}










