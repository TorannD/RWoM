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

internal static partial class SpiritMapCollapseTests
{
    private sealed class ReversalSave : IExposable
    {
        public HediffComp Effect=(HediffComp)Activator.CreateInstance(typeof(MightAbility).Assembly.GetType("TorannMagic.HediffComp_Reversal",true)!,true)!;
        public void ExposeData() => Effect.CompExposeData();
    }

    private sealed class HateSave : IExposable
    {
        public HediffComp_Hate Effect=new();
        public void ExposeData()=>Effect.CompExposeData();
    }

    private sealed class KnightJobSave : IExposable
    {
        public Pawn Body=null!;
        public Job Order=null!;
        public JobDriver_SpiritHostedMartial Driver=null!;
        public void ExposeData()
        {
            Scribe_References.Look(ref Body,"body");
            Scribe_Deep.Look(ref Order,"order");
            if(Driver!=null){Driver.pawn=Body;Driver.job=Order;}
            Scribe_Deep.Look(ref Driver,"driver");
            if(Driver!=null){Driver.pawn=Body;Driver.job=Order;}
        }
    }

    private sealed class PsionicAttackSave : IExposable
    {
        public HediffComp Effect=(HediffComp)Activator.CreateInstance(typeof(MightAbility).Assembly.GetType("TorannMagic.HediffComp_Psionic",true)!,true)!;
        public void ExposeData()=>Effect.CompExposeData();
    }

    private sealed class TestMartialTend : HediffComp_TendDuration
    {
        public float RequestedQuality;
        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            RequestedQuality = quality;
            base.CompTended(quality, maxQuality, 1); // Skip only the headless text mote.
        }
    }

    private static bool ShroudBodyDraw(Pawn_DrawTracker __instance, ref UnityEngine.Vector3 __result)
    {
        var pawn=(Pawn)typeof(Pawn_DrawTracker).GetField("pawn",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!.GetValue(__instance)!;
        __result=pawn.Position.ToVector3Shifted();return false; // Rendering interpolation only.
    }
    public sealed class PersistentShroudTestBullet : Bullet
    {
        public int Attempts;
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (++Attempts > 2) throw new InvalidOperationException("Shroud revisited a projectile that refused destruction");
        }
    }
    private static bool ShroudChance(ref bool __result) { __result=true; return false; }
    private static int knightLaunches;
    private static bool CountKnightLaunch(ref bool __result) { knightLaunches++; __result=true; return false; }
    private static bool RejectKnightLaunch(ref bool __result) { __result=false; return false; }
    private static bool? spiteBaseBlocked;
    private static bool ObserveSpiteBaseImpact(bool blockedByShield) { spiteBaseBlocked=blockedByShield;return false; }
    private static Pawn? fearSource;
    private static int fearChecks;
    private static bool ObserveFearChance(Pawn caster, ref float __result)
    { fearSource=caster;fearChecks++;__result=1f;return false; }
    private static DamageInfo? pommelHit;
    private static bool CapturePommel(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
    {
        pommelHit = dinfo;
        __result = new DamageWorker.DamageResult();
        return false;
    }
    private static IEnumerable<TestCase> HostedMartialCases()
    {
        foreach (var test in HostedMonkCases()) yield return test;
        foreach (var test in HostedCommanderCases()) yield return test;
        foreach (var test in HostedSuperSoldierCases()) yield return test;
        foreach (var test in HostedApothecaryCases()) yield return test;
        yield return new("SpiritProjectedCast.Martial.Sniper.AllStockRoundsCarryOriginalOwner", f =>
        {
            var xml=System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/AbilityDefs/Abilities_TMagic_Sniper.xml"));
            var defs=xml.Root!.Elements().Where(d=>d.Element("defName")!=null).ToDictionary(d=>(string)d.Element("defName")!);
            var shots=defs.Values.Where(d=>(string?)d.Element("MainVerb")?.Element("verbClass")=="TorannMagic.Verb_SniperShot").ToList();
            Check.Equal(6,shots.Count,"verify new sniper families explicitly when adding another shot");
            var assembly=typeof(Verb_SniperShot).Assembly;
            var savedRound=assembly.GetType("TorannMagic.Projectile_SniperRound",true)!;
            foreach(var shot in shots)
            {
                var projectile=defs[(string)shot.Element("MainVerb")!.Element("defaultProjectile")!];
                var actual=assembly.GetType((string)projectile.Element("thingClass")!,true)!;
                Check.Equal(true,savedRound.IsAssignableFrom(actual),"registered shot preserves ownership: "+shot.Element("defName")!.Value);
            }
        });
        foreach (var (id, job) in new[] {
            ("TM_Headshot","TM_SpiritCastHeadshot"), ("TM_AntiArmor","TM_SpiritCastAntiArmor"),
            ("TM_DisablingShot","TM_SpiritCastDisablingShot"), ("TM_DisablingShot_I","TM_SpiritCastDisablingShot"),
            ("TM_DisablingShot_II","TM_SpiritCastDisablingShot"), ("TM_DisablingShot_III","TM_SpiritCastDisablingShot") })
        yield return new("SpiritProjectedCast.Martial.Sniper."+id, f =>
            SpiritRebirthRitualTransactionTests.Run(f, c => {
                using var possession = new Fixture(f,c);
                var lookup = Production("SpiritHostedMartial").GetMethod("Find", BindingFlags.Static | BindingFlags.NonPublic)!;
                var source = c.Original.GetCompAbilityUserMight();
                var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField(id)!.GetValue(null)!;
                var original = new MightAbility(source) { Def = def, CooldownTicksLeft = 87 };
                source.AbilityData.Powers.Add(original);
                source.AbilityData.AllPowers = null;
                object? Find() => lookup.Invoke(null,[c.Creator,c.Original,job]);
                Check.Same(original,Find()!,"retained shot resolves to the soul's exact runtime power");
                Check.Equal(87,original.CooldownTicksLeft,"looking up the command retains its cooldown");
                var host = c.Creator.GetCompAbilityUserMight();
                host.AbilityData.Powers.Add(new MightAbility(host) { Def = def });
                host.AbilityData.AllPowers = null;
                Check.Equal(true,Find()==null,"local shot takes precedence without adding a duplicate");
            }));

        foreach (string id in new[]{"TM_SniperAimedShot","TM_SniperArmorFracture"})
        yield return new("SpiritProjectedCast.Martial.Reforged."+id,f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail(id)??f.AddAbility(id);
                var source=c.Original.GetCompAbilityUserMight();
                var power=new MightAbility(source){Def=def};
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                string job=id=="TM_SniperAimedShot"?"TM_SpiritCastAimedShot":"TM_SpiritCastArmorFracture";
                const string file = "TM_SpiritSniperBallistics.xml";
                var xml = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/JobDefs",file));
                var order = xml.Root!.Elements().Single(e => (string?)e.Element("defName") == job);
                Check.Equal(typeof(JobDriver_SpiritHostedMartial).FullName,(string?)order.Element("driverClass"),"order uses the targeted cast job");
                foreach (var language in new[] {"English","Russian","Spanish","Portuguese","PortugueseBrazilian","Ukrainian","ChineseSimplified"})
                {
                    var translations = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Languages",language,"DefInjected/JobDef",file));
                    Check.Equal(true,translations.Root!.Element(job+".reportString")?.Value.Contains("TargetB")==true,
                        "translated report names the actual target in "+language);
                }
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,job])!,"borrowed weapon job resolves the exact discipline power");
            }));

        yield return new("SpiritProjectedCast.Martial.SniperOpening.UsesOriginalThreeShotJob", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                const string id = "TM_SniperSingleOpening";
                var def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(id) ?? f.AddAbility(id);
                var source = c.Original.GetCompAbilityUserMight();
                var power = new SniperOpeningAbility(source) { Def = def };
                source.AbilityData.Powers.Add(power);
                source.AbilityData.AllPowers = null;
                var find = Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,"TM_SniperSingleOpeningCast"])!,
                    "only the original's learned ascension provides the three-shot job");
            }));

        yield return new("SpiritProjectedCast.Martial.ShadowSlayer.UsesOriginalCloak", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                var source = c.Original.GetCompAbilityUserMight();
                var power = new MightAbility(source) { Def=TorannMagicDefOf.TM_ShadowSlayer };
                source.AbilityData.Powers.Add(power); source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastShadowSlayer"])!,
                    "the host can project the retained cloak without learning it itself");
            }));

        yield return new("SpiritProjectedCast.Martial.GladiatorSweep.UsesLearnedSource", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                var def=f.AddAbility("TM_GladiatorSweep");
                var source=c.Original.GetCompAbilityUserMight();
                var power=new MightAbility(source){Def=def};
                source.AbilityData.Powers.Add(power); source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastGladiatorSweep"])!,
                    "the original learned Reforged technique provides the host's melee order");
            }));

        yield return new("GladiatorWhirlwind.AirbornePawnHasPhysicalOwner", f =>
        {
            Check.Equal(true, typeof(IThingHolder).IsAssignableFrom(typeof(FlyingObject_Whirlwind)),
                "an airborne pawn and the pawn's inventory must remain reachable from the map's holder hierarchy");
        });
        foreach(var (defName,order) in new[] {("TM_BladeSpin","TM_SpiritCastBladeSpin"),("TM_SeismicSlash","TM_SpiritCastSeismicSlash"),("TM_BladePassage","TM_SpiritCastBladePassage")})
            yield return new("BladeProjection.OriginalOwner."+defName,f =>
                SpiritRebirthRitualTransactionTests.Run(f,c => {
                    using var held=new Fixture(f,c);
                    var def=typeof(TorannMagicDefOf).GetField(defName)?.GetValue(null) as TMAbilityDef ??
                        DefDatabase<TMAbilityDef>.GetNamedSilentFail(defName) ?? f.AddAbility(defName);
                    var source=c.Original.GetCompAbilityUserMight();
                    var original=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(original);source.AbilityData.AllPowers=null;
                    var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                    object? Power()=>find.Invoke(null,[c.Creator,c.Original,order]);
                    Check.Same(original,Power()!,"project the exact learned blade technique");
                    var local=c.Creator.GetCompAbilityUserMight();local.AbilityData.Powers.Add(new MightAbility(local){Def=def});local.AbilityData.AllPowers=null;
                    Check.Equal(true,Power()==null,"the host's own technique has priority");
                }));
        foreach(string id in new[]{"TM_ArrowStorm","TM_ArrowStorm_I","TM_ArrowStorm_II","TM_ArrowStorm_III"})
            yield return new("RangerProjection.VolleyOwner."+id,f=>
                SpiritRebirthRitualTransactionTests.Run(f,c=>{
                    using var held=new Fixture(f,c);
                    var source=c.Original.GetCompAbilityUserMight();
                    var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField(id)!.GetValue(null)!;
                    var power=new MightAbility(source){Def=def,CooldownTicksLeft=31};
                    source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                    var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                    object? Owner()=>find.Invoke(null,[c.Creator,c.Original,"TM_SpiritArrowStorm"]);
                    Check.Same(power,Owner()!,"volley order must use the original exact rank");
                    var host=c.Creator.GetCompAbilityUserMight();host.AbilityData.Powers.Add(new MightAbility(host){Def=TorannMagicDefOf.TM_ArrowStorm});
                    host.AbilityData.AllPowers=null;
                    Check.Equal(true,Owner()==null,"a lower native rank takes priority");
                }));
        foreach(string id in new[]{"TM_Transpose","TM_Transpose_I","TM_Transpose_II","TM_Transpose_III"})
            yield return new("FacelessProjection.TransposeOwner."+id,f=>
                SpiritRebirthRitualTransactionTests.Run(f,c=>{
                    using var held=new Fixture(f,c);
                    var source=c.Original.GetCompAbilityUserMight();
                    var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField(id)!.GetValue(null)!;
                    var power=new MightAbility(source){Def=def};
                    source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                    var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                    object? Power()=>find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastTranspose"]);
                    Check.Same(power,Power()!,"exact retained transpose rank");
                    var host=c.Creator.GetCompAbilityUserMight();
                    host.AbilityData.Powers.Add(new MightAbility(host){Def=TorannMagicDefOf.TM_Transpose});host.AbilityData.AllPowers=null;
                    Check.Equal(true,Power()==null,"native transpose has priority");
                }));
        yield return new("FacelessProjection.ReversalHitKinds",f=>{
            var martial=typeof(MightAbility).Assembly.GetType("TorannMagic.Verb_Reversal",true)!;
            var round=martial.GetMethod("IncomingRound",BindingFlags.Static|BindingFlags.NonPublic);
            Check.Equal(true,round!=null,"resolve the incoming round using the recorded weapon");
            var physical=new DamageDef{defName="ReversalTestBullet"};
            var magic=new DamageDef{defName="ReversalTestDirect"};
            var explosive=new DamageDef{defName="ReversalTestExplosion",isExplosive=true};
            var bullet=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            bullet.thingClass=typeof(Bullet);bullet.projectile=new ProjectileProperties{damageDef=physical};
            var weapon=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            weapon.defName="ReversalTestGun";
            Set(weapon,"verbs",new List<VerbProperties>{new(){defaultProjectile=bullet}});
            object? Incoming(DamageDef damage,ThingDef? recorded)=>round!.Invoke(null,[new DamageInfo(damage,12,weapon:recorded)]);
            Check.Same(bullet,Incoming(physical,weapon)!,"use recorded bullet even after a weapon swap");
            Check.Equal(true,Incoming(physical,null)==null,"direct damage or a reflected round cannot bounce again");
            Check.Equal(true,Incoming(magic,weapon)==null,"direct effect does not copy an unrelated projectile");
            Check.Equal(true,Incoming(explosive,weapon)==null,"explosion passes through");
            bullet.projectile.explosionRadius=2;
            Check.Equal(true,Incoming(physical,weapon)==null,"exclude explosive ammunition");
        });
        foreach(var (defName,order) in new[]{("TM_PsionicStorm","TM_SpiritPsionicStorm"),("TM_PsionicDash","TM_SpiritPsionicDash"),("TM_PsionicBarrier","TM_SpiritPsionicBarrier"),("TM_PsionicBarrier_Projected","TM_SpiritProjectedBarrier"),("TM_PsionicBlast","TM_SpiritPsionicBlast"),("TM_PsionicBlast_III","TM_SpiritPsionicBlast")})
            yield return new("PsionicProjection.BarrierOwner."+defName,f=>SpiritRebirthRitualTransactionTests.Run(f,c=>{
                using var held=new Fixture(f,c);
                var comp=c.Original.GetCompAbilityUserMight();
                var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField(defName)!.GetValue(null)!;
                var power=new MightAbility(comp){Def=def};comp.AbilityData.Powers.Add(power);comp.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,order])!,"original barrier used by host");
                var native=c.Creator.GetCompAbilityUserMight();native.AbilityData.Powers.Add(new MightAbility(native){Def=def});
                native.AbilityData.AllPowers=null;
                Check.Equal(true,find.Invoke(null,[c.Creator,c.Original,order])==null,"native barrier takes precedence");
            }));
        yield return new("PsionicProjection.VolleyInitialEnergy",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.Verb_PsionicBlast",true)!;
            var pay=type.GetMethod("PayInitialEnergy",BindingFlags.NonPublic|BindingFlags.Static)!;
            var previous=TorannMagicDefOf.TM_PsionicHD;
            try {
            TorannMagicDefOf.TM_PsionicHD=new HediffDef{defName="RegressionPsionicEnergy",hediffClass=typeof(Hediff),maxSeverity=100};
            var energy=HediffMaker.MakeHediff(TorannMagicDefOf.TM_PsionicHD,c.Original);
            c.Original.health.AddHediff(energy);
            energy.Severity=19;
            Check.Equal(false,(bool)pay.Invoke(null,[c.Original])!,"underfunded volley rejected");
            Check.Equal(19f,energy.Severity,"rejection retains energy");
            energy.Severity=73;
            Check.Equal(true,(bool)pay.Invoke(null,[c.Original])!,"funded volley accepted");
            Check.Equal(53f,energy.Severity,"initial twenty comes from original soul");
            } finally {TorannMagicDefOf.TM_PsionicHD=previous;}
        }));
        foreach(bool rescue in new[]{false,true})
        yield return new(rescue?"DeathKnightProjection.RescueCredit":"DeathKnightProjection.BattleCredit",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            DisciplineProgress Enroll(Pawn pawn){
                var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(pawn),"state",state);
                state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);return state.Find("rwom.deathknight")!;
            }
            var path=Enroll(c.Original);
            var method=Production("DeathKnightMartialPath").GetMethod(rescue?"Rescue":"Battle",BindingFlags.NonPublic|BindingFlags.Static)!;
            var patient=PawnWithComps(false,false).pawn;
            void Record(int id){
                patient.thingIDNumber=88000+id;
                Set(Find.TickManager,"ticksGameInt",id*60000);
                method.Invoke(null,rescue?[c.Creator,patient]:[c.Creator]);
            }
            int goal=rescue?3:5;
            bool Done(DisciplineProgress p)=>p.CompletedTrials.Contains("rwom.deathknight.Foundation");
            for(int n=1;n<goal;n++){Record(n);Record(n);}
            Check.Equal(false,Done(path),"duplicate rescue or same-day battle is not new credit");
            Record(goal);
            Check.Equal(true,Done(path),"host service advances original knight");
            var local=Enroll(c.Creator);
            for(int n=1;n<=goal;n++)Record(n);
            Check.Equal(true,Done(local),"native path takes priority");
        }));
        foreach(string name in new[]{"Spite","Spite_I","Spite_II","Spite_III","WaveOfFear","GraveBlade","GraveBlade_I","GraveBlade_II","GraveBlade_III"})
        yield return new("DeathKnightProjection.OrderOwner."+name,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var source=c.Original.GetCompAbilityUserMight();
            var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_"+name)!.GetValue(null)!;
            var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
            var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
            object? Resolve()=>find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCast"+name.Split('_')[0]]);
            Check.Same(power,Resolve()!,"hosted order finds exact original ability");
            var local=c.Creator.GetCompAbilityUserMight();local.AbilityData.Powers.Add(new MightAbility(local){Def=def});local.AbilityData.AllPowers=null;
            Check.Equal(true,Resolve()==null,"native power takes precedence");
        }));
        foreach(var projectileType in new[]{typeof(Projectile_Spite),typeof(Projectile_WaveOfFear),typeof(Projectile_GraveBlade)})
        yield return new("DeathKnightProjection.NativeLaunch."+projectileType.Name,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
            string id="rwom.knight.launch."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings{AIHardMode=false};
                var patch=typeof(MightAbility).Assembly.GetType("TorannMagic.SniperRoundSpawn",true)!;
                using var ownerPatch=new Patch(typeof(Verb).GetProperty("DirectOwner")!.GetMethod!,nameof(NativeAbilityOwner));
                c.Map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
                c.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
                var processor=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[patch])!;processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
                using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
                var presentation=typeof(MaterialPool).GetMethods().Where(m=>m.Name==nameof(MaterialPool.MatFrom)).Select(m=>new Patch(m,nameof(CancelNative))).ToArray();
                var mods=LoadedModManager.RunningModsListForReading.ToArray();
                try{LoadedModManager.RunningModsListForReading.Clear();RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle);}
                finally{LoadedModManager.RunningModsListForReading.AddRange(mods);foreach(var p in presentation)p.Dispose();}
                var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName="KnightNative"+projectileType.Name;def.thingClass=projectileType;def.category=ThingCategory.Projectile;def.stackLimit=1;
                def.size=new IntVec2(1,1);def.comps=[];def.drawerType=DrawerType.RealtimeOnly;def.destroyable=true;
                def.projectile=new ProjectileProperties{damageDef=new DamageDef{defaultDamage=8},speed=60};
                var source=c.Original.GetCompAbilityUserMight();source.mightPwr=1.73f;
                if(source.Stamina==null)c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina});
                var abilityDef=projectileType==typeof(Projectile_Spite)?TorannMagicDefOf.TM_Spite:projectileType==typeof(Projectile_WaveOfFear)?TorannMagicDefOf.TM_WaveOfFear:TorannMagicDefOf.TM_GraveBlade;
                var power=new MightAbility(source){Def=abilityDef};
                var verb=new Verb_SB{caster=c.Creator,Ability=power,verbProps=new VerbProperties_Ability{range=30,defaultProjectile=def}};
                Check.Equal(true,verb.TryLaunchProjectileCheck(def,c.Creator),"native JecsLite launch succeeds");
                var shot=c.Map.thingGrid.ThingsListAt(c.Creator.Position).Single(t=>t.GetType()==projectileType);
                Check.Same(c.Creator,((Projectile)shot).Launcher,"native launch retains physical actor");
                Check.Equal(1.73f,(float)projectileType.GetField("arcaneDmg",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"production spawn hook binds original multiplier");
                Check.Equal(true,shot.Spawned && !shot.Destroyed,"real projectile exists in map grid");
            } finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);TorannMagic.ModOptions.Settings.Instance=settings;}
        }));
        yield return new("DeathKnightProjection.WaveJobPayment",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var originalDef=TorannMagicDefOf.TM_WaveOfFear;
            var oldEvent=TorannMagicDefOf.TM_UsedManeuver;
            var events=DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            var jobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
                var def=(TMAbilityDef)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(originalDef,null)!;
                def.consumeEnergy=true;def.staminaCost=.3f;
                def.MainVerb=new VerbProperties_Ability{verbClass=typeof(Verb_SB),warmupTime=1,SecondsToRecharge=25,targetParams=new TargetingParameters{canTargetSelf=true}};
                TorannMagicDefOf.TM_WaveOfFear=def;
                TorannMagicDefOf.TM_UsedManeuver=new HistoryEventDef{defName="KnightWaveUsed"};
                DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                var history=(History)RuntimeHelpers.GetUninitializedObject(typeof(History));history.historyEventsManager=new HistoryEventsManager();Set(Current.Game,"history",history);
                using var notify=new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!,nameof(CancelNative));
                using var launch=new Patch(typeof(Verb_UseAbility).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CountKnightLaunch));
                var source=c.Original.GetCompAbilityUserMight();source.maxSP=1;
                c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;
                _=source.MightData;
                if(source.Stamina==null)c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina});
                source.Stamina.CurLevel=.9f;
                var power=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var job=JobMaker.MakeJob(new JobDef{defName="TM_SpiritCastWaveOfFear"},c.Original,c.Creator);
                DefDatabase<JobDef>.Add(job.def);
                var driver=new JobDriver_SpiritHostedMartial{pawn=c.Creator,job=job};
                Set(c.Creator.jobs,"curJob",job);Set(c.Creator.jobs,"curDriver",driver);
                var toils=((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                knightLaunches=0;
                float before=source.Stamina.CurLevel,price=source.ActualStaminaCost(def);
                var execute=Production("DeathKnightCast").GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic)!;
                // Reject at the projectile boundary after owner/resource/target validation.
                launch.Dispose();
                using(var rejection=new Patch(typeof(Verb_UseAbility).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(RejectKnightLaunch)))
                {
                    int cooldownBefore=power.CooldownTicksLeft;
                    Check.Equal(false,(bool)execute.Invoke(null,[c.Creator,c.Original,job.def.defName,new LocalTargetInfo(c.Creator)])!,"failed projectile launch reports failure");
                    Check.Equal(before,source.Stamina.CurLevel,"failed projectile launch does not pay stamina");
                    Check.Equal(cooldownBefore,power.CooldownTicksLeft,"failed projectile launch does not start cooldown");
                }
                using var successfulLaunch=new Patch(typeof(Verb_UseAbility).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CountKnightLaunch));
                toils[0].initAction();
                Check.Equal(0,knightLaunches,"warmup does not launch");
                Check.Equal(before,source.Stamina.CurLevel,"warmup is free");
                driver.ticksLeftThisToil=23;
                var loaded=RoundTripSummon(r,"knight-warmup-job",new KnightJobSave{Body=c.Creator,Order=job,Driver=driver},c.Creator,c.Original);
                driver=loaded.Driver;job=loaded.Order;
                Check.Equal(23,driver.ticksLeftThisToil,"remaining preparation survives save/load");
                Check.Same(c.Original,job.targetA.Thing,"loaded order retains soul owner");
                Check.Same(c.Creator,job.targetB.Thing,"loaded wave targets physical carrier");
                Set(c.Creator.jobs,"curJob",job);Set(c.Creator.jobs,"curDriver",driver);
                toils=((IEnumerable<Toil>)typeof(JobDriver_SpiritHostedMartial).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToArray();
                toils[1].initAction();
                Check.Equal(1,knightLaunches,"job launches once through physical verb");
                Check.Equal(true,Math.Abs(before-price-source.Stamina.CurLevel)<.0001f,"source stamina paid exactly once");
                Check.Equal(true,power.CooldownTicksLeft>0,"source cooldown starts");
                Check.Equal(true,(bool)typeof(JobDriver_SpiritHostedMartial).GetField("attempted",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!,"driver records completed attempt");
                var receipt=RoundTripSummon(r,"knight-paid-job",new KnightJobSave{Body=c.Creator,Order=job,Driver=driver},c.Creator,c.Original);
                Check.Equal(true,(bool)typeof(JobDriver_SpiritHostedMartial).GetField("attempted",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(receipt.Driver)!,"completed attempt survives reload");
                bool Repeat()=>(bool)execute.Invoke(null,[c.Creator,c.Original,job.def.defName,new LocalTargetInfo(c.Creator)])!;
                float paid=source.Stamina.CurLevel;
                Check.Equal(false,Repeat(),"cooldown prevents another launch");
                power.CooldownTicksLeft=0;
                source.Stamina.CurLevel=0;
                Check.Equal(false,Repeat(),"resource spent during warmup prevents launch");
                source.Stamina.CurLevel=paid;
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(false,Repeat(),"depletion during warmup prevents launch");
                Check.Equal(1,knightLaunches,"all rejected attempts remain free of launch");
                Check.Equal(paid,source.Stamina.CurLevel,"rejected attempt preserves stamina");
            } finally {TorannMagic.ModOptions.Settings.Instance=settings;TorannMagicDefOf.TM_WaveOfFear=originalDef;TorannMagicDefOf.TM_UsedManeuver=oldEvent;DefDatabase<HistoryEventDef>.Clear();foreach(var item in events)DefDatabase<HistoryEventDef>.Add(item);DefDatabase<JobDef>.Clear();foreach(var item in jobs)DefDatabase<JobDef>.Add(item);}
        }));
        yield return new("DeathKnightProjection.SpiteSoulPayment",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var hate=new Hediff{def=TorannMagicDefOf.TM_HateHD,pawn=c.Original,Severity=35};
            c.Original.health.hediffSet.hediffs.Add(hate);
            var source=c.Original.GetCompAbilityUserMight();
            var ability=new MightAbility(source){Def=TorannMagicDefOf.TM_Spite};
            var verb=new Verb_DeathKnightSpite{caster=c.Creator,Ability=ability,verbProps=ability.mightDef.MainVerb};
            var shot=typeof(Verb_DeathKnightSpite).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!;
            using var launch=new Patch(typeof(Verb_UseAbility).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(ShroudChance));
            Check.Equal(true,(bool)shot.Invoke(verb,null)!,"funded hosted projectile launch allowed");
            Check.Equal(15f,hate.Severity,"successful launch spends original twenty hate");
            Check.Equal(false,(bool)shot.Invoke(verb,null)!,"resource rechecked after warmup");
            Check.Equal(15f,hate.Severity,"unfunded retry is free");
            hate.Severity=35;
            var spirit=ClassIdentity.Component(c.Original).SpiritRebirth;
            Set(spirit,"depletionPending",true);
            Check.Equal(false,(bool)shot.Invoke(verb,null)!,"depleted soul cannot launch through carrier");
            Check.Equal(35f,hate.Severity,"rejected owner retains hate");
            Set(spirit,"depletionPending",false);
            verb.caster=c.Circle;
            Check.Equal(false,(bool)shot.Invoke(verb,null)!,"non-carrier cannot use soul's power");
            Check.Equal(35f,hate.Severity,"invalid carrier retains hate");
            verb.caster=c.Creator;
            launch.Dispose();
            using var refused=new Patch(typeof(Verb_UseAbility).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CancelNative));
            Check.Equal(false,(bool)shot.Invoke(verb,null)!,"failed launch is propagated");
            Check.Equal(35f,hate.Severity,"failed launch consumes no hate");
        }));
        yield return new("DeathKnightProjection.SpiteSnapshotSave",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            var type=typeof(Projectile_Spite);
            var bind=type.GetMethod("BindShot",BindingFlags.Instance|BindingFlags.NonPublic);
            Check.NotNull(bind,"Spite binds original skill parameters at launch");
            var source=c.Original.GetCompAbilityUserMight();
            var skills=source.MightData.MightPowerSkill_Spite;
            source.mightPwr=1.73f;
            skills.First(x=>x.label=="TM_Spite_pwr").level=2;
            skills.First(x=>x.label=="TM_Spite_ver").level=3;
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="RegressionSpiteRound";def.thingClass=type;def.category=ThingCategory.Projectile;def.comps=[];
            DefDatabase<ThingDef>.Add(def);
            var shot=new Projectile_Spite{def=def,thingIDNumber=19890,usedTarget=IntVec3.Zero,intendedTarget=IntVec3.Zero};
            Set(shot,"launcher",c.Creator);
            var previousSettings=TorannMagic.ModOptions.Settings.Instance;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings{AIHardMode=false};
                bind!.Invoke(shot,[c.Original,c.Creator]);
            } finally {TorannMagic.ModOptions.Settings.Instance=previousSettings;}
            float factor=(float)type.GetField("arcaneDmg",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!;
            skills.First(x=>x.label=="TM_Spite_pwr").level=0;
            shot=RoundTripSummon(r,"knight-spite-shot",shot,c.Creator);
            Check.Same(c.Creator,shot.Launcher,"physical damage instigator survives load");
            Check.Equal(2,(int)type.GetField("pwrVal",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"source power captured before later rank change");
            Check.Equal(3,(int)type.GetField("verVal",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"source versatility saved");
            Check.Equal(factor,(float)type.GetField("arcaneDmg",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"source damage multiplier saved");
        }));
        foreach(bool fear in new[]{true,false})
        yield return new("DeathKnightProjection.ActiveFieldSave."+(fear?"Fear":"Blade"),r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            var type=fear?typeof(Projectile_WaveOfFear):typeof(Projectile_GraveBlade);
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="RegressionKnightField"+(fear?"Fear":"Blade");def.thingClass=type;def.category=ThingCategory.Projectile;def.comps=[];
            DefDatabase<ThingDef>.Add(def);
            var shot=(Projectile_AbilityBase)Activator.CreateInstance(type)!;
            shot.def=def;shot.thingIDNumber=19891;shot.usedTarget=shot.intendedTarget=IntVec3.Zero;
            Set(shot,"launcher",c.Creator);Set(shot,"caster",c.Creator);Set(shot,"initialized",true);
            Set(shot,"arcaneDmg",1.73f);Set(shot,"radius",7.2f);
            if(fear){Set(shot,"effVal",2);Set(shot,"affectedPawns",new List<Pawn>{c.Creator});}
            shot=RoundTripSummon(r,"knight-field-"+(fear?"fear":"blade"),shot,c.Creator);
            object Value(string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!;
            Check.Equal(1.73f,(float)Value("arcaneDmg"),"active field keeps original damage multiplier");
            Check.Equal(7.2f,(float)Value("radius"),"active field keeps its radius");
            Check.Same(c.Creator,Value("caster"),"physical actor retained");
            if(fear){
                Check.Equal(2,(int)Value("effVal"),"wave efficiency retained");
                var affected=(List<Pawn>)Value("affectedPawns");
                Check.NotNull(affected,"loaded wave retains deduplication set");
                Check.Equal(1,affected.Count,"no duplicate application after reload");
                Check.Same(c.Creator,affected[0],"affected pawn reference resolved");
            }
        }));
        foreach(string outcome in new[]{"Hit","Fallback","Shield","MissingTarget","OtherMap"})
        yield return new("DeathKnightProjection.SpiteImpact."+outcome,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            using var baseImpact=new Patch(typeof(Projectile_AbilityBase).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(ObserveSpiteBaseImpact));
            using var damage=new Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(CapturePommel));
            var victim=PawnWithComps(false,false).pawn;
            Set(victim,"mapIndexOrState",(sbyte)0);
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="KnightSpiteImpact";def.projectile=new ProjectileProperties{damageDef=new DamageDef{defaultDamage=20}};
            var shot=new Projectile_Spite{def=def,intendedTarget=victim};
            Set(shot,"mapIndexOrState",(sbyte)0);Set(shot,"launcher",c.Creator);
            Set(shot,"shotBound",true);Set(shot,"pwrVal",2);Set(shot,"verVal",0);Set(shot,"arcaneDmg",1.5f);
            if(outcome=="MissingTarget")Set(victim,"mapIndexOrState",(sbyte)-1);
            if(outcome=="OtherMap"){
                Current.Game.Maps.Add((Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)));
                Set(victim,"mapIndexOrState",(sbyte)(Current.Game.Maps.Count-1));
            }
            pommelHit=null;spiteBaseBlocked=null;
            typeof(Projectile_Spite).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,[outcome=="Hit"?victim:null,outcome=="Shield"]);
            Check.Equal((bool?)(outcome=="Shield"),spiteBaseBlocked,"base impact receives shield interception flag");
            if(outcome=="Hit" || outcome=="Fallback"){
                Check.Equal(true,pommelHit.HasValue,"Spite reaches real TakeDamage boundary");
                Check.Same(c.Creator,pommelHit.Value.Instigator,"physical carrier is damage instigator");
                Check.Equal(true,pommelHit.Value.Amount>=27f && pommelHit.Value.Amount<=45f,"source power and multiplier determine randomized damage");
            } else Check.Equal(false,pommelHit.HasValue,"shield or unavailable target cannot receive fallback damage");
        }));
        yield return new("DeathKnightProjection.HateBodyStats",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.ClassProgression.SpiritHateStats");
            Check.NotNull(type,"hosted hate stat patch exists");
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var stat=new StatDef{defName="KnightStat"};
            var stage=new HediffStage{statOffsets=[new StatModifier{stat=stat,value=.1f}]};
            var def=TorannMagicDefOf.TM_SpiritPossessionHD;
            var previous=def.stages;
            var previousHate=TorannMagicDefOf.TM_HateHD;
            var ht=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
            string id="rwom.knight-stat-test."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
            try {
                def.stages=[stage];
                var hateDef=new HediffDef{defName="TM_HateHD_StatTest",maxSeverity=200,stages=[
                    new HediffStage{statOffsets=[new StatModifier{stat=stat,value=.2f}]},
                    new HediffStage{minSeverity=25,statOffsets=[new StatModifier{stat=stat,value=.35f}]}]};
                var hate=new Hediff{def=hateDef,pawn=c.Original,Severity=30};
                TorannMagicDefOf.TM_HateHD=hateDef;
                c.Original.health.hediffSet.hediffs.Add(hate);
                var processor=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[type]);processor!.GetType().GetMethod("Patch")!.Invoke(processor,null);
                float Read()=>HediffStatsUtility.GetStatOffsetForSeverity(stat,stage,c.Creator,fixture.Possession.parent.Severity);
                Check.Equal(.45f,Read(),"carrier gets current source hate stage plus its existing offset");
                hate.Severity=10;Check.Equal(.3f,Read(),"source severity change changes projected offset");
                hateDef.stages[0].statOffsets[0].value=-.2f;
                Check.Equal(-.1f,Read(),"negative movement-style offset is projected too");
                hateDef.stages[0].statOffsets[0].value=.2f;
                var unrelated=new HediffStage{statOffsets=[new StatModifier{stat=stat,value=.7f}]};
                Check.Equal(.7f,HediffStatsUtility.GetStatOffsetForSeverity(stat,unrelated,c.Creator,1),"other hediff stages remain unchanged");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                Check.Equal(.1f,Read(),"depleted soul supplies no bonus");
                Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
                c.Creator.health.hediffSet.hediffs.Remove(fixture.Possession.parent);
                Check.Equal(.1f,Read(),"removed possession supplies no stale bonus");
                c.Creator.health.hediffSet.hediffs.Add(fixture.Possession.parent);
                var local=new Hediff{def=hateDef,pawn=c.Creator,Severity=30};c.Creator.health.hediffSet.hediffs.Add(local);
                Check.Equal(.1f,Read(),"native hate prevents a second borrowed bonus");
                c.Creator.health.hediffSet.hediffs.Remove(local);
                var localState=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Creator),"state",localState);
                localState.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
                Check.Equal(.1f,Read(),"native knight class takes precedence even before its hate marker is restored");
            } finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);def.stages=previous;TorannMagicDefOf.TM_HateHD=previousHate;}
        }));
        foreach(bool hosted in new[]{true,false})
        foreach(bool factioned in new[]{true,false})
        yield return new("DeathKnightProjection.FearVictimAcrossLoad."+(hosted?"Soul":"Native")+"."+(factioned?"Faction":"Wild"),r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            using var chance=new Patch(typeof(TM_Calc).GetMethod(nameof(TM_Calc.GetSpellSuccessChance))!,nameof(ObserveFearChance));
            using var draw=new Patch(typeof(Pawn).GetProperty(nameof(Pawn.DrawPos))!.GetMethod!,nameof(CancelNative));
            using var healthState=new Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.CheckForStateChange))!,nameof(CancelNative));
            var victim=PawnWithComps(false,false).pawn;
            victim.def=c.Creator.def;
            victim.mindState=new Pawn_MindState(victim);victim.jobs=null;
            if(factioned)Set(victim,"factionInt",new Faction{def=new FactionDef()});
            Set(victim,"mapIndexOrState",(sbyte)0);Set(victim,"positionInt",c.Creator.Position+IntVec3.North);
            ((IList<Pawn>)c.Map.mapPawns.AllPawnsSpawned).Add(victim);
            var fear=DefDatabase<HediffDef>.GetNamedSilentFail("TM_WaveOfFearHD");
            if(fear==null){fear=new HediffDef{defName="TM_WaveOfFearHD",hediffClass=typeof(Hediff),maxSeverity=10};DefDatabase<HediffDef>.Add(fear);}
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="KnightFearVictimField";def.thingClass=typeof(Projectile_WaveOfFear);def.category=ThingCategory.Projectile;def.comps=[];
            DefDatabase<ThingDef>.Add(def);
            var shot=new Projectile_WaveOfFear{def=def,thingIDNumber=19981};
            Set(shot,"mapIndexOrState",(sbyte)0);Set(shot,"caster",c.Creator);Set(shot,"launcher",c.Creator);
            Set(shot,"skillOwner",hosted?c.Original:null);Set(shot,"shotBound",hosted);Set(shot,"initialized",true);
            Set(shot,"pwrVal",2);Set(shot,"waveRange",4);Set(shot,"affectedPawns",new List<Pawn>());
            fearSource=null;fearChecks=0;
            shot.SearchAndFear();
            Check.Same(hosted?c.Original:c.Creator,fearSource!,"fear resistance uses source with legacy carrier fallback");
            Check.Equal(2.5f,victim.health.hediffSet.GetFirstHediffOfDef(fear).Severity,"real fear hediff receives original rank");
            shot=RoundTripSummon(r,"knight-fear-victim",shot,c.Creator,c.Original,victim);
            Set(shot,"mapIndexOrState",(sbyte)0);
            shot.SearchAndFear();
            Check.Equal(1,fearChecks,"already affected victim is not rolled again after load");
            Check.Equal(2.5f,victim.health.hediffSet.GetFirstHediffOfDef(fear).Severity,"reload does not stack fear");
        }));
        foreach(string state in new[]{"MissingCarrier","DespawnedCarrier","OtherMap"})
        yield return new("DeathKnightProjection.WaveOrphan."+state,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            var shot=new Projectile_WaveOfFear();
            Set(shot,"mapIndexOrState",(sbyte)0);
            Set(shot,"affectedPawns",new List<Pawn>());
            Set(shot,"caster",state=="MissingCarrier"?null:c.Creator);
            if(state=="DespawnedCarrier")Set(c.Creator,"mapIndexOrState",(sbyte)-1);
            if(state=="OtherMap"){
                var other=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
                Current.Game.Maps.Add(other);
                Set(c.Creator,"mapIndexOrState",(sbyte)(Current.Game.Maps.Count-1));
            }
            // An abandoned field must return before reading the carrier's map pawn registry.
            shot.SearchAndFear();
            Set(shot,"launcher",state=="MissingCarrier"?null:c.Creator);
            Set(shot,"initialized",true);
            using var baseImpact=new Patch(typeof(Projectile_AbilityBase).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CancelNative));
            typeof(Projectile_WaveOfFear).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,[null,false]);
        }));
        foreach(bool fear in new[]{true,false})
        yield return new("DeathKnightProjection.FieldSource."+(fear?"Fear":"Blade"),r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var type=fear?typeof(Projectile_WaveOfFear):typeof(Projectile_GraveBlade);
            var bind=type.GetMethod("BindShot",BindingFlags.Instance|BindingFlags.NonPublic);
            Check.NotNull(bind,"field binds original soul before physical impact");
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var comp=c.Original.GetCompAbilityUserMight();comp.mightPwr=1.73f;
            if(comp.Stamina==null)c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina});
            var skills=fear?comp.MightData.MightPowerSkill_WaveOfFear:comp.MightData.MightPowerSkill_GraveBlade;
            string prefix=fear?"TM_WaveOfFear_":"TM_GraveBlade_";
            skills.First(x=>x.label==prefix+"pwr").level=2;
            skills.First(x=>x.label==prefix+"ver").level=3;
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            var shot=Activator.CreateInstance(type)!;
            var previousSettings=TorannMagic.ModOptions.Settings.Instance;
            try {
                TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings{AIHardMode=false};
                bind!.Invoke(shot,[c.Original]);
            } finally {TorannMagic.ModOptions.Settings.Instance=previousSettings;}
            object Value(string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!;
            Check.Equal(2,(int)Value("pwrVal"),"source power used");
            Check.Equal(3,(int)Value("verVal"),"source versatility used");
            Check.Equal(1.73f,(float)Value("arcaneDmg"),"source multiplier used");
        }));
        yield return new("DeathKnightProjection.DamageFeedsSoul",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var hate=new Hediff{def=TorannMagicDefOf.TM_HateHD,pawn=c.Original,Severity=1};
            c.Original.health.hediffSet.hediffs.Add(hate);
            c.Original.GetCompAbilityUserMight().MightData.MightPowerSkill_Shroud.First(x=>x.label=="TM_Shroud_eff").level=2;
            var enemy=PawnWithComps(false,false).pawn;
            var faction=new Faction{def=new FactionDef()};
            Set(faction,"relations",new List<FactionRelation>{new(){other=c.Creator.Faction,kind=FactionRelationKind.Hostile}});
            Set(enemy,"factionInt",faction);
            enemy.kindDef=new PawnKindDef{race=enemy.def};
            enemy.guest=new Pawn_GuestTracker(enemy);enemy.mindState=new Pawn_MindState(enemy);
            var damage=new DamageDef{defName="KnightHostDamage",harmsHealth=true};
            var hit=new DamageInfo(damage,10,0,-1,enemy);
            TorannMagicMod.PreApplyDamage_Patch.Prefix(c.Creator.health,c.Creator,ref hit,out _);
            Check.Equal(13f,hate.Severity,"hit on body feeds original soul hate");
            Check.Equal(true,TM_Calc.GetHateHediff(c.Creator)==null,"no copied hate reservoir in host");
            hit=new DamageInfo(damage,10,0,-1,c.Creator);
            TorannMagicMod.PreApplyDamage_Patch.Prefix(c.Creator.health,c.Creator,ref hit,out _);
            Check.Equal(13f,hate.Severity,"self damage cannot feed staged soul");
        }));
        yield return new("DeathKnightProjection.LifeStealSource",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);Set(state.Find("rwom.deathknight")!,"stage",InitiationStage.Master);
            var source=c.Original.GetCompAbilityUserMight();source.maxSP=1;
            c.Original.ageTracker.AgeBiologicalTicks=25L*GenDate.TicksPerYear;
            if(source.Stamina==null)c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina});
            source.Stamina.CurLevel=.4f;
            Check.Equal(.4f,source.Stamina.CurLevel,"adult source starts with forty percent stamina");
            source.MightData.MightPowerSkill_LifeSteal.First(x=>x.label=="TM_LifeSteal_eff").level=2;
            source.MightData.MightPowerSkill_LifeSteal.First(x=>x.label=="TM_LifeSteal_pwr").level=2;
            c.Creator.kindDef=new PawnKindDef{race=c.Creator.def};
            c.Creator.ageTracker ??= new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult}}];
            Set(c.Creator.ageTracker,"cachedLifeStageIndex",0);
            var wound=new Hediff_Injury{pawn=c.Creator,Part=c.Creator.RaceProps.body.corePart,
                def=new HediffDef{defName="KnightCarrierCut",injuryProps=new InjuryProps()}};
            Set(wound,"severityInt",8f);c.Creator.health.hediffSet.hediffs.Add(wound);
            // The minimal fixture has no complete capacity/body simulation; keep real Heal/Severity,
            // excluding only unrelated downing/death notifications after the wound changes.
            using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!,nameof(CancelNative));
            var hate=new Hediff{def=TorannMagicDefOf.TM_HateHD,pawn=c.Original,Severity=10};c.Original.health.hediffSet.hediffs.Add(hate);
            var enemy=PawnWithComps(false,false).pawn;
            var faction=new Faction{def=new FactionDef()};Set(faction,"relations",new List<FactionRelation>{new(){other=c.Creator.Faction,kind=FactionRelationKind.Hostile}});
            Set(enemy,"factionInt",faction);Set(enemy,"mapIndexOrState",(sbyte)0);Set(enemy,"positionInt",c.Creator.Position+IntVec3.East);
            enemy.kindDef=new PawnKindDef{race=enemy.def};enemy.guest=new Pawn_GuestTracker(enemy);enemy.mindState=new Pawn_MindState(enemy);
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            using var siphon=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowSiphonMote))!,nameof(CancelNative));
            var weapon=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));weapon.equipmentType=EquipmentType.Primary;weapon.weaponTags=[];
            weapon.category=ThingCategory.Item;weapon.tools=[new Tool{power=10}];weapon.comps=[];
            var hit=new DamageInfo(new DamageDef{defName="KnightDrainHit",harmsHealth=true},10,0,-1,c.Creator,weapon:weapon);
            Check.Equal(true,weapon.IsMeleeWeapon,"test uses a native melee weapon");
            TorannMagicMod.PreApplyDamage_Patch.Prefix(enemy.health,enemy,ref hit,out _);
            Check.Equal(13.5f,hate.Severity,"life tap feeds attacking soul rather than victim or carrier");
            float healed=10*(TorannMagicDefOf.TM_LifeSteal.weaponDamageFactor+.04f);
            Check.Equal(true,healed>0 && Math.Abs(wound.Severity-(8f-healed))<.0001f,"native HealPawn heals carrier using source power rank");
            Check.Equal(true,Math.Abs(source.Stamina.CurLevel-.42f)<.0001f,"efficiency restores original stamina");
            Check.Equal(true,TM_Calc.GetHateHediff(c.Creator)==null,"carrier has no duplicate resource");
            Set(enemy,"factionInt",c.Creator.Faction);
            TorannMagicMod.PreApplyDamage_Patch.Prefix(enemy.health,enemy,ref hit,out _);
            Check.Equal(13.5f,hate.Severity,"friendly victim cannot fuel staged life steal");
            Set(enemy,"factionInt",faction);Set(state.Find("rwom.deathknight")!,"stage",InitiationStage.Foundation);
            TorannMagicMod.PreApplyDamage_Patch.Prefix(enemy.health,enemy,ref hit,out _);
            Check.Equal(13.5f,hate.Severity,"foundation cannot borrow later passive");
            Set(state.Find("rwom.deathknight")!,"stage",InitiationStage.Master);
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            TorannMagicMod.PreApplyDamage_Patch.Prefix(enemy.health,enemy,ref hit,out _);
            Check.Equal(13.5f,hate.Severity,"depleted soul cannot drain");
            Check.Equal(true,Math.Abs(source.Stamina.CurLevel-.42f)<.0001f,"rejected drains restore no extra stamina");
        }));
        yield return new("DeathKnightProjection.HateSave",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var before=new HateSave();
            Set(before.Effect,"lastHateTick",7301);Set(before.Effect,"lastHate",53f);
            var loaded=RoundTripSummon(r,"knight-hate-clock",before,c.Creator).Effect;
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Check.Equal(7301,(int)typeof(HediffComp_Hate).GetField("lastHateTick",flags)!.GetValue(loaded)!,"last hate gain survives load");
            Check.Equal(53f,(float)typeof(HediffComp_Hate).GetField("lastHate",flags)!.GetValue(loaded)!,"comparison amount retained");
        }));
        yield return new("DeathKnightProjection.ShroudTwoProjectiles",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var hate=new HediffWithComps{def=TorannMagicDefOf.TM_HateHD,pawn=c.Original,Severity=35};
            var tracker=new HediffComp_Hate{parent=hate};hate.comps=[tracker];
            c.Original.health.hediffSet.hediffs.Add(hate);
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var chance=new Patch(typeof(Rand).GetMethod(nameof(Rand.Chance))!,nameof(ShroudChance));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            using var fleck=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericFleck))!,nameof(CancelNative));
            var presentation=typeof(MaterialPool).GetMethods().Where(m=>m.Name==nameof(MaterialPool.MatFrom))
                .Select(m=>new Patch(m,nameof(CancelNative))).ToArray();
            var mods=LoadedModManager.RunningModsListForReading.ToArray();
            try {LoadedModManager.RunningModsListForReading.Clear();RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle);}
            finally {LoadedModManager.RunningModsListForReading.AddRange(mods);foreach(var patch in presentation)patch.Dispose();}
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="KnightIncomingBullet";def.thingClass=typeof(Bullet);def.category=ThingCategory.Projectile;def.stackLimit=1;
            def.size=new IntVec2(1,1);def.comps=[];def.drawerType=DrawerType.RealtimeOnly;def.destroyable=true;
            def.projectile=new ProjectileProperties{damageDef=new DamageDef{defaultDamage=8},speed=60};
            c.Creator.pather=new Pawn_PathFollower(c.Creator);
            Bullet Shot(){
                var b=(Bullet)ThingMaker.MakeThing(def);
                Set(b,"launcher",c.Circle);
                Set(b,"origin",c.Creator.DrawPos+new UnityEngine.Vector3(20,0,0));
                Set(b,"destination",c.Creator.DrawPos);Set(b,"ticksToImpact",10);
                GenSpawn.Spawn(b,c.Creator.Position,c.Map);return b;
            }
            var first=Shot();var second=Shot();
            Check.Equal(true,first.Spawned && second.Spawned && !first.Destroyed && !second.Destroyed,"real bullets alive before interception");
            Set(Find.TickManager,"ticksGameInt",2004);
            float delta=0;tracker.CompPostTick(ref delta);
            Check.Equal(true,first.Destroyed && second.Destroyed,"both real bullets destroyed from same cell list");
            Check.Equal(27f,hate.Severity,"two interceptions debit original hate exactly once each");
            Check.Equal(true,TM_Calc.GetHateHediff(c.Creator)==null,"no host resource created");
        }));
        yield return new("DeathKnightProjection.ShroudPersistentProjectile",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var hate=new HediffWithComps{def=TorannMagicDefOf.TM_HateHD,pawn=c.Original,Severity=35};
            var tracker=new HediffComp_Hate{parent=hate};hate.comps=[tracker];
            c.Original.health.hediffSet.hediffs.Add(hate);
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            using var chance=new Patch(typeof(Rand).GetMethod(nameof(Rand.Chance))!,nameof(ShroudChance));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            using var fleck=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericFleck))!,nameof(CancelNative));
            var presentation=typeof(MaterialPool).GetMethods().Where(m=>m.Name==nameof(MaterialPool.MatFrom))
                .Select(m=>new Patch(m,nameof(CancelNative))).ToArray();
            var mods=LoadedModManager.RunningModsListForReading.ToArray();
            try {LoadedModManager.RunningModsListForReading.Clear();RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle);}
            finally {LoadedModManager.RunningModsListForReading.AddRange(mods);foreach(var patch in presentation)patch.Dispose();}
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="KnightIncomingBullet";def.thingClass=typeof(Bullet);def.category=ThingCategory.Projectile;def.stackLimit=1;
            def.size=new IntVec2(1,1);def.comps=[];def.drawerType=DrawerType.RealtimeOnly;def.destroyable=true;
            def.projectile=new ProjectileProperties{damageDef=new DamageDef{defaultDamage=8},speed=60};
            c.Creator.pather=new Pawn_PathFollower(c.Creator);
            Bullet Shot(){
                var b=(Bullet)ThingMaker.MakeThing(def);
                Set(b,"launcher",c.Circle);
                Set(b,"origin",c.Creator.DrawPos+new UnityEngine.Vector3(20,0,0));
                Set(b,"destination",c.Creator.DrawPos);Set(b,"ticksToImpact",10);
                GenSpawn.Spawn(b,c.Creator.Position,c.Map);return b;
            }
            def.thingClass=typeof(PersistentShroudTestBullet);
            var first=(PersistentShroudTestBullet)Shot();
            def.thingClass=typeof(Bullet);var second=Shot();
            Check.Equal(true,first.Spawned && second.Spawned && !first.Destroyed && !second.Destroyed,"real bullets alive before interception");
            Set(Find.TickManager,"ticksGameInt",2004);
            float delta=0;tracker.CompPostTick(ref delta);
            Check.Equal(1,first.Attempts,"persistent projectile is attempted only once per scan");
            Check.Equal(true,first.Spawned && !first.Destroyed && second.Destroyed,"refused destruction does not prevent the next interception");
            Check.Equal(27f,hate.Severity,"two interceptions debit original hate exactly once each");
            Check.Equal(true,TM_Calc.GetHateHediff(c.Creator)==null,"no host resource created");
        }));
        yield return new("DeathKnightProjection.WavePaysOnceAcrossLoad",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            using var baseImpact=new Patch(typeof(Projectile_AbilityBase).GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CancelNative));
            var hateDef=new HediffDef{defName="TM_HateHDRegressionWave",minSeverity=.001f,maxSeverity=200};
            var sourceHate=new Hediff{def=hateDef,pawn=c.Original,Severity=35};
            var hostHate=new Hediff{def=hateDef,pawn=c.Creator,Severity=90};
            c.Original.health.hediffSet.hediffs.Add(sourceHate);c.Creator.health.hediffSet.hediffs.Add(hostHate);
            var type=typeof(Projectile_WaveOfFear);
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="RegressionPaidWave";def.thingClass=type;def.category=ThingCategory.Projectile;def.comps=[];
            DefDatabase<ThingDef>.Add(def);
            var shot=new Projectile_WaveOfFear{def=def,thingIDNumber=19892,usedTarget=IntVec3.Zero,intendedTarget=IntVec3.Zero};
            Set(shot,"launcher",c.Creator);Set(shot,"shotBound",true);Set(shot,"skillOwner",c.Original);Set(shot,"effVal",2);
            Set(Find.TickManager,"ticksGameInt",1);
            var impact=type.GetMethod("Impact",BindingFlags.Instance|BindingFlags.NonPublic)!;
            shot=RoundTripSummon(r,"wave-before-payment",shot,c.Creator,c.Original);
            impact.Invoke(shot,[null,false]);
            Check.Equal(15f,sourceHate.Severity,"actual impact charges twenty source hate at efficiency two");
            Check.Equal(90f,hostHate.Severity,"host hate untouched");
            shot=RoundTripSummon(r,"wave-after-payment",shot,c.Creator,c.Original);
            impact.Invoke(shot,[null,false]);
            Check.Equal(15f,sourceHate.Severity,"loaded initialized wave never charges again");
        }));
        foreach(string scenario in new[]{"Clear","Wall","OutOfRange"})
        yield return new("DeathKnightProjection.TargetLine."+scenario,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var directOwner=new Patch(typeof(Verb).GetProperty("DirectOwner")!.GetMethod!,nameof(NativeAbilityOwner));
            c.Map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
            c.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
            Set(c.Creator,"positionInt",new IntVec3(5,0,5));
            var def=(TMAbilityDef)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(TorannMagicDefOf.TM_Spite,null)!;
            def.MainVerb=new VerbProperties_Ability{verbClass=typeof(Verb_SB),range=8,requireLineOfSight=true,targetParams=new TargetingParameters{canTargetLocations=true}};
            var power=new MightAbility(c.Original.GetCompAbilityUserMight()){Def=def};
            if(scenario=="Wall"){
                var wallDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));wallDef.defName="KnightSightWall";wallDef.size=new IntVec2(1,1);wallDef.fillPercent=1;wallDef.passability=Traversability.Impassable;
                var wall=new Building{def=wallDef};Set(wall,"positionInt",new IntVec3(8,0,5));Set(wall,"mapIndexOrState",(sbyte)0);c.Map.edificeGrid.Register(wall);
            }
            var target=new LocalTargetInfo(new IntVec3(scenario=="OutOfRange"?20:11,0,5));
            bool valid=(bool)Production("DeathKnightCast").GetMethod("Valid",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[c.Creator,power,"TM_SpiritCastSpite",target])!;
            Check.Equal(scenario=="Clear",valid,"native physical range and sight decide targeting");
        }));
        yield return new("DeathKnightProjection.WaveTickCadence",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            using var baseTick=new Patch(typeof(Projectile_AbilityBase).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CancelNative));
            var shot=new Projectile_WaveOfFear();Set(shot,"mapIndexOrState",(sbyte)0);
            Set(shot,"initialized",true);Set(shot,"caster",c.Creator);Set(shot,"affectedPawns",new List<Pawn>());
            Set(Find.TickManager,"ticksGameInt",10);
            typeof(Projectile_WaveOfFear).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,null);
            Check.Equal(2,(int)typeof(Projectile_WaveOfFear).GetField("waveRange",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"landed field expands on its own tick without another Impact");
        }));
        foreach(bool departed in new[]{false,true})
        yield return new("DeathKnightProjection.BladePhysicalEffects."+(departed?"Departed":"Present"),r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            c.Map.info=new MapInfo{Size=new IntVec3(40,1,40)};
            c.Map.cellIndices=new CellIndices(c.Map);c.Map.thingGrid=new ThingGrid(c.Map);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            using var baseImpact=new Patch(typeof(Projectile_AbilityBase).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CancelNative));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            using var damage=new Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(CapturePommel));
            using var physiology=new Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!,nameof(CancelNative));
            using var draw=new Patch(typeof(Pawn_DrawTracker).GetProperty(nameof(Pawn_DrawTracker.DrawPos))!.GetMethod!,nameof(ShroudBodyDraw));
            c.Creator.kindDef=new PawnKindDef{race=c.Creator.def};c.Creator.ageTracker ??= new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult}}];
            Set(c.Creator.ageTracker,"cachedLifeStageIndex",0);
            var wound=new Hediff_Injury{pawn=c.Creator,Part=c.Creator.RaceProps.body.corePart,def=new HediffDef{defName="KnightBladeCut",injuryProps=new InjuryProps()}};
            Set(wound,"severityInt",40f);c.Creator.health.hediffSet.hediffs.Add(wound);
            var victim=PawnWithComps(false,false).pawn;victim.kindDef=new PawnKindDef{race=victim.def};
            Set(victim,"mapIndexOrState",(sbyte)0);Set(victim,"positionInt",new IntVec3(10,0,10));
            c.Map.thingGrid.ThingsListAtFast(victim.Position).Add(victim);
            var shot=new Projectile_GraveBlade();
            Set(shot,"mapIndexOrState",(sbyte)0);Set(shot,"positionInt",victim.Position);
            Set(shot,"initialized",true);Set(shot,"caster",c.Creator);Set(shot,"launcher",c.Creator);
            Set(shot,"pwrVal",2);Set(shot,"verVal",1);Set(shot,"arcaneDmg",1.5f);
            Set(shot,"innerCellList",new List<IntVec3>{victim.Position});Set(shot,"ringCellList",new List<IntVec3>{victim.Position});
            if(departed)Set(c.Creator,"mapIndexOrState",(sbyte)-1);
            Set(Find.TickManager,"ticksGameInt",15);pommelHit=null;
            typeof(Projectile_GraveBlade).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,null);
            Check.Equal(true,pommelHit.HasValue,"placed field still damages its local victim");
            Check.Same(c.Creator,pommelHit.Value.Instigator,"field damage credited to physical caster");
            Check.Equal(true,pommelHit.Value.Amount>=15.75f && pommelHit.Value.Amount<=37.5f,"field uses captured source rank and multiplier");
            if(departed)Check.Equal(40f,wound.Severity,"field cannot heal carrier after leaving its map");
            else Check.Equal(true,wound.Severity<=28f && wound.Severity>=23.5f,"real wound healing uses captured versatility and multiplier");
        }));
        yield return new("DeathKnightProjection.BladeMapEdge",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            c.Map.info=new MapInfo{Size=new IntVec3(40,1,40)};
            c.Map.cellIndices=new CellIndices(c.Map);c.Map.thingGrid=new ThingGrid(c.Map);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            using var baseImpact=new Patch(typeof(Projectile_AbilityBase).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(CancelNative));
            using var mote=new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!,nameof(CancelNative));
            var shot=new Projectile_GraveBlade();
            Set(shot,"mapIndexOrState",(sbyte)0);Set(shot,"positionInt",IntVec3.Zero);
            Set(shot,"initialized",true);Set(shot,"caster",c.Creator);
            Set(shot,"innerCellList",new List<IntVec3>{IntVec3.Zero});
            Set(shot,"ringCellList",new List<IntVec3>{IntVec3.Zero});
            Set(Find.TickManager,"ticksGameInt",15);
            typeof(Projectile_GraveBlade).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,null);
        }));
        yield return new("DeathKnightProjection.ShroudMapEdge",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            c.Map.info=new MapInfo{Size=new IntVec3(40,1,40)};
            c.Map.cellIndices=new CellIndices(c.Map);c.Map.thingGrid=new ThingGrid(c.Map);
            Set(c.Creator,"positionInt",new IntVec3(0,0,0));
            var hate=new HediffWithComps{def=TorannMagicDefOf.TM_HateHD,pawn=c.Creator,Severity=35};
            var tracker=new HediffComp_Hate{parent=hate};hate.comps=[tracker];
            var absorb=typeof(HediffComp_Hate).GetMethod("AbsorbProjectiles",BindingFlags.Instance|BindingFlags.NonPublic)!;
            absorb.Invoke(tracker,absorb.GetParameters().Length==0?null:[c.Creator]);
            Check.Equal(35f,hate.Severity,"empty boundary scan costs no hate");
        }));
        yield return new("DeathKnightProjection.HateTickOwner",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var hate=new HediffWithComps{def=TorannMagicDefOf.TM_HateHD,pawn=c.Original,Severity=1};
            var tracker=new HediffComp_Hate{parent=hate};hate.comps=[tracker];
            c.Original.health.hediffSet.hediffs.Add(hate);
            Set(Find.TickManager,"ticksGameInt",2000);
            float delta=0;tracker.CompPostTick(ref delta);
            Check.Equal(true,delta>0,"held soul hate follows living host instead of freezing off-map");
            Check.Equal(false,tracker.CompShouldRemove,"hosting does not remove original hate");
            float repeat=0;tracker.CompPostTick(ref repeat);
            Check.Equal(0f,repeat,"held tick and runtime tick cannot regenerate twice");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Set(Find.TickManager,"ticksGameInt",2010);
            delta=0;tracker.CompPostTick(ref delta);
            Check.Equal(0f,delta,"depleted soul passive is dormant");
        }));
        yield return new("DeathKnightProjection.OathSoulPayment",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var definitions=DeathKnightCareerTests.Setup(r);
            var state=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Original),"state",state);
            state.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            var path=state.Find("rwom.deathknight")!;
            Set(path,"stage",InitiationStage.Ascended);
            var data=Activator.CreateInstance(Production("DeathKnightDisciplineData"),true)!;
            Set(data,"Oath",true);Set(path,"MartialData",data);
            foreach(var pawn in new[]{c.Creator,c.Original})
                pawn.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_HateHD,pawn=pawn,Severity=35});
            var spend=Production("DeathKnightGuard").GetMethod("SpendGuard",BindingFlags.NonPublic|BindingFlags.Static)!;
            Check.Equal(true,(bool)spend.Invoke(null,[c.Creator])!,"host can shield using retained soul oath");
            Check.Equal(25f,TM_Calc.GetHateHediff(c.Original).Severity,"soul pays ten hate");
            Check.Equal(35f,TM_Calc.GetHateHediff(c.Creator).Severity,"host hate untouched");
            Check.Equal(false,(bool)spend.Invoke(null,[c.Creator])!,"soul cooldown prevents repeated payment");
            Check.Equal(25f,TM_Calc.GetHateHediff(c.Original).Severity,"repeat does not debit soul");
            Set(data,"OathReadyAt",0);
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
            Check.Equal(false,(bool)spend.Invoke(null,[c.Creator])!,"depleted soul cannot shield");
            Check.Equal(25f,TM_Calc.GetHateHediff(c.Original).Severity,"depletion does not debit hate");
            Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",false);
            var local=new ClassProgressionState(Vocation.Warrior);
            Set(ClassIdentity.Component(c.Creator),"state",local);
            local.TryAddFoundation("rwom.deathknight",DisciplineDomain.Might);
            Check.Equal(false,(bool)spend.Invoke(null,[c.Creator])!,"native knight without oath does not borrow a second path");
            Check.Equal(25f,TM_Calc.GetHateHediff(c.Original).Severity,"local path precedence leaves soul unchanged");
        }));
        yield return new("PsionicProjection.CrucibleSoulPayment",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var prior=TorannMagicDefOf.TM_PsionicHD;
            try {
                TorannMagicDefOf.TM_PsionicHD=new HediffDef{defName="CrucibleSourceTest",hediffClass=typeof(Hediff),maxSeverity=200};
                foreach(var pawn in new[]{c.Creator,c.Original}){
                    pawn.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_PsionicHD,pawn=pawn,Severity=100});
                }
                var bound=typeof(JobDriver_PsionicCrucible).GetMethod("Bound",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Equal(true,(bool)bound.Invoke(null,[c.Creator,c.Original])!,"actual carrier accepted");
                Check.Equal(false,(bool)bound.Invoke(null,[c.Original,c.Creator])!,"unrelated reverse binding rejected");
                Check.Equal(true,(bool)bound.Invoke(null,[c.Creator,c.Creator])!,"native caster accepted");
                var job=new Job{targetC=c.Original};
                var driver=new JobDriver_PsionicCrucible{pawn=c.Creator,job=job};
                typeof(JobDriver_PsionicCrucible).GetMethod("Charge",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,[.6f,0f]);
                Check.Equal(true,Math.Abs(40f-c.Original.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD).Severity)<.0001f,"original soul pays psi");
                Check.Equal(100f,c.Creator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD).Severity,"physical host pool untouched");
            } finally {TorannMagicDefOf.TM_PsionicHD=prior;}
        }));
        yield return new("PsionicProjection.FlightEnergyCosts",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            var cost=Production("PsionicDashCast").GetMethod("PsiCost",BindingFlags.NonPublic|BindingFlags.Static)!;
            foreach(var (def,baseCost,perRank) in new[]{(TorannMagicDefOf.TM_PsionicDash,8f,1f),(TorannMagicDefOf.TM_PsionicStorm,65f,5f)})
            {
                var skill=c.Original.GetCompAbilityUserMight().MightData.GetSkill_Efficiency(def);
                Check.Equal(true,skill!=null,"efficiency record available");
                foreach(int rank in new[]{0,1,2,3}) {
                    skill.level=rank;
                    Check.Equal(baseCost-perRank*rank,(float)cost.Invoke(null,[c.Original,def])!,"cost follows source efficiency");
                }
            }
        }));
        yield return new("PsionicProjection.DashTrainingOwner",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            var previousSettings=TorannMagic.ModOptions.Settings.Instance;
            try {
            TorannMagic.ModOptions.Settings.Instance=new TorannMagic.ModOptions.Settings();
            var flight=new FlyingObject_PsionicDash();
            foreach(var owner in new[]{c.Creator,c.Original})
                foreach(var suffix in new[]{"_pwr","_ver"}){
                    var skills=owner.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips;
                    if(!skills.Any(s=>s.label=="TM_Cantrips"+suffix))
                        skills.Add(new MagicPowerSkill{label="TM_Cantrips"+suffix});
                }
            Set(flight,"pawn",c.Creator);
            c.Creator.GetCompAbilityUserMight().mightPwr=1.1f;
            c.Original.GetCompAbilityUserMight().mightPwr=1.8f;
            var type=typeof(FlyingObject_PsionicDash);
            var capture=type.GetMethod("CaptureTraining",BindingFlags.NonPublic|BindingFlags.Instance)!;
            capture.Invoke(flight,null);
            Check.Equal(1.1f,(float)type.GetField("arcaneDmg",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(flight)!,"native dash uses body training");
            type.GetMethod("SetSkillOwner",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(flight,[c.Original]);
            capture.Invoke(flight,null);
            Check.Equal(1.8f,(float)type.GetField("arcaneDmg",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(flight)!,"hosted dash uses original training");
            } finally {TorannMagic.ModOptions.Settings.Instance=previousSettings;}
        }));
        yield return new("PsionicProjection.DashPassengerOwner",_=>{
            Check.Equal(true,typeof(IThingHolder).IsAssignableFrom(typeof(FlyingObject_PsionicDash)),
                "the airborne pawn needs physical ownership");
            Check.Equal(true,typeof(ISuspendableThingHolder).IsAssignableFrom(typeof(FlyingObject_PsionicDash)),
                "airborne passenger must not independently tick");
        });
        foreach(var (kind,fields) in new (string,Dictionary<string,object>)[]{
            ("Dash",new() {["direction"]=new UnityEngine.Vector3(.6f,0,.8f),["speed"]=20f,["drafted"]=true,["pwrVal"]=3,["verVal"]=2,["arcaneDmg"]=1.7f,["drawTicks"]=41,["shouldDrawPawn"]=false}),
            ("Storm",new() {["stage"]=2,["nextAttackTick"]=9021,["targetCenter"]=new UnityEngine.Vector3(17,0,25),["curveAngle"]=73f,["speed"]=10f,["timesToDamage"]=1}),
            ("Leap",new() {["returnPending"]=true,["resumeAttack"]=true,["drafted"]=false,["effVal"]=3,["trueOrigin"]=new UnityEngine.Vector3(3,0,5),["trueDestination"]=new UnityEngine.Vector3(17,0,25),["trueAngle"]=73f})
        })
            yield return new("PsionicProjection.FlightSave."+kind,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
                using var fixture=new Fixture(r,c);
                using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
                var type=typeof(MightAbility).Assembly.GetType("TorannMagic.FlyingObject_Psionic"+kind,true)!;
                var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName="RegressionPsiFlight"+kind;def.thingClass=type;def.category=ThingCategory.Projectile;def.comps=[];
                DefDatabase<ThingDef>.Add(def);
                var shot=(Projectile)Activator.CreateInstance(type)!;
                shot.def=def;shot.thingIDNumber=19890;shot.usedTarget=shot.intendedTarget=IntVec3.Zero;
                fields["origin"]=new UnityEngine.Vector3(11,0,13);
                fields["destination"]=new UnityEngine.Vector3(31,0,43);
                fields["ticksToImpact"]=17;
                foreach(var pair in fields)Set(shot,pair.Key,pair.Value);
                if(kind=="Dash" || kind=="Leap" || kind=="Storm"){
                    var cargoDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    cargoDef.defName="RegressionPsiPassenger";cargoDef.thingClass=typeof(Thing);cargoDef.category=ThingCategory.Item;cargoDef.stackLimit=75;cargoDef.comps=[];
                    DefDatabase<ThingDef>.Add(cargoDef);
                    var cargo=new Thing{def=cargoDef,thingIDNumber=19891,stackCount=7};
                    Set(shot,"flyingThing",cargo);
                }
                var loaded=RoundTripSummon(r,"psionic-flight-"+kind,shot);
                if(kind=="Storm")
                    Check.Equal(true,(bool)type.GetField("returnPending",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!,
                        "an incomplete orbit requests safe return instead of indexing missing curve points");
                if(kind=="Dash" || kind=="Leap" || kind=="Storm"){
                    var held=((IThingHolder)loaded).GetDirectlyHeldThings();
                    Check.Equal(1,held.Count,"old deep passenger adopted on load");
                    Check.Equal(7,held[0].stackCount,"passenger data preserved");
                    Check.Same(loaded,held[0].holdingOwner.Owner,"physical parent restored");
                }
                foreach(var pair in fields)
                    Check.Equal(pair.Value,type.GetField(pair.Key,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance)!.GetValue(loaded)!,"in-flight "+kind+" retains "+pair.Key);
            }));
        yield return new("PsionicProjection.LegacyStormSegments",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);
            using var material=new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
            var type=typeof(FlyingObject_PsionicStorm);
            var near=new UnityEngine.Vector3(35.5f,0,19.5f);
            var far=new UnityEngine.Vector3(35.5f,0,39.5f);
            var home=new UnityEngine.Vector3(35.5f,0,9.5f);
            foreach(int phase in new[]{0,1,2,3,4})foreach(int sign in new[]{-1,1}){
                var shot=new FlyingObject_PsionicStorm();
                var start=phase<=1?near:new UnityEngine.Vector3(38.184658f,0,36.450264f);
                var end=start+new UnityEngine.Vector3(sign,0,0);
                Set(shot,"nearApex",near);Set(shot,"farApex",far);Set(shot,"trueOrigin",home);
                Set(shot,"destination",phase==4?home:phase==3||phase==0?near:end);
                Set(shot,"curvePoints",phase==0?new List<UnityEngine.Vector3>():new List<UnityEngine.Vector3>{start,end});
                type.GetMethod("RestoreLegacyPhase",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,null);
                Check.Equal(phase,(int)type.GetField("stage",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"old segment resumes its original phase");
                Check.Equal(phase==0?40f:phase==4?25f:10f,(float)type.GetField("speed",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"phase speed restored");
                if(phase==1)Check.Equal(sign*90f,(float)type.GetField("curveAngle",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"second arc preserves orbit direction");
            }
        }));
        yield return new("PsionicProjection.InFlightBlastSave",r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var held=new Fixture(r,c);
            using var material = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string),typeof(UnityEngine.Shader)])!, nameof(CancelNative));
            var type=typeof(MightAbility).Assembly.GetType("TorannMagic.Projectile_PsionicBlast",true)!;
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="RegressionPsiRound";def.thingClass=type;def.category=ThingCategory.Projectile;def.comps=[];
            DefDatabase<ThingDef>.Add(def);
            var shot=(Projectile_AbilityBase)Activator.CreateInstance(type)!;
            shot.def=def;shot.thingIDNumber=19889;shot.usedTarget=shot.intendedTarget=IntVec3.Zero;
            Set(shot,"launcher",c.Creator);
            foreach(float factor in new[]{1.73f,0f}){
                Set(shot,"damageFactor",factor);
                shot=RoundTripSummon(r,"psionic-in-flight-"+(factor==0f?"zero":"trained"),shot,c.Creator);
                Check.Same(c.Creator,shot.Launcher,"physical launcher retained");
                Check.Equal(factor,(float)type.GetField("damageFactor",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(shot)!,"snapshotted factor, including zero, survives load");
            }
        }));
        yield return new("PsionicProjection.BlastSpreadAtMapEdge",_=>{
            var verbType=typeof(MightAbility).Assembly.GetType("TorannMagic.Verb_PsionicBlast",true)!;
            var constrain=verbType.GetMethod("ConstrainSpread",BindingFlags.NonPublic|BindingFlags.Static);
            Check.Equal(true,constrain!=null,"spread must remain inside the physical map");
            var size=new IntVec3(80,1,120);
            IntVec3 Cell(int x,int z)=>(IntVec3)constrain!.Invoke(null,[new IntVec3(x,0,z),size])!;
            Check.Equal(new IntVec3(0,0,119),Cell(-2,122),"northwest corner");
            Check.Equal(new IntVec3(79,0,0),Cell(81,-1),"southeast corner");
            Check.Equal(new IntVec3(7,0,8),Cell(7,8),"ordinary spread unchanged");
            var verb=Activator.CreateInstance(verbType,true);
            var direction=(UnityEngine.Vector3)verbType.GetMethod("GetVector")!.Invoke(verb,[IntVec3.Zero,IntVec3.Zero])!;
            Check.Equal(0f,direction.sqrMagnitude,"zero displacement is a finite direction");
        });
        yield return new("PsionicProjection.IndividualEnergyLabels",_=>{
            var def=new HediffDef{minSeverity=0,maxSeverity=200,stages=[new HediffStage()]};
            HediffComp Energy(float amount)
            {
                var effect=new PsionicAttackSave().Effect;
                var energy=new HediffWithComps{def=def};
                Set(energy,"severityInt",amount);effect.parent=energy;
                return effect;
            }
            var one=Energy(12.5f);
            var two=Energy(83.25f);
            Check.Equal(12.5f.ToString("0.00")+"%",one.CompLabelInBracketsExtra,"first pawn's psi reading");
            Check.Equal(83.25f.ToString("0.00")+"%",two.CompLabelInBracketsExtra,"second pawn's psi reading");
            Check.Equal(true,string.IsNullOrEmpty(def.stages[0].label),"shared stage still has no pawn-specific text");
        });
        yield return new("PsionicProjection.PaidStrikeSave", f=>SpiritRebirthRitualTransactionTests.Run(f,c=>{
            using var held=new Fixture(f,c);
            var before=new PsionicAttackSave();
            Set(before.Effect,"initialized",true);
            Set(before.Effect,"doPsionicAttack",true);
            Set(before.Effect,"ticksTillPsionicStrike",13);
            Set(before.Effect,"nextPsionicAttack",7001);
            Set(before.Effect,"threat",c.Creator);
            var loaded=RoundTripSummon(f,"psionic-paid-strike",before,c.Creator).Effect;
            var instance=BindingFlags.NonPublic|BindingFlags.Instance;
            object? Value(string field)=>loaded.GetType().GetField(field,instance)!.GetValue(loaded);
            Check.Equal(true,(bool)Value("doPsionicAttack")!,"a paid pending strike survives load");
            Check.Equal(13,(int)Value("ticksTillPsionicStrike")!,"remaining windup retained");
            Check.Equal(7001,(int)Value("nextPsionicAttack")!,"absolute cooldown retained");
            Check.Same(c.Creator,Value("threat")!,"same target after references resolve");
        }));
        foreach(bool rescue in new[]{false,true})
            yield return new(rescue?"PsionicProjection.RescueTrial":"PsionicProjection.RepairTrial",f=>
                SpiritRebirthRitualTransactionTests.Run(f,c=>{
                    using var held=new Fixture(f,c);
                    var stage=rescue?InitiationStage.Adept:InitiationStage.Foundation;
                    DisciplineProgress Enroll(Pawn pawn)
                    {
                        var state=new ClassProgressionState(Vocation.Warrior);Set(ClassIdentity.Component(pawn),"state",state);
                        state.TryAddFoundation("rwom.psionic",DisciplineDomain.Might);
                        var path=state.Find("rwom.psionic")!;Set(path,"stage",stage);return path;
                    }
                    var soul=Enroll(c.Original);
                    var service=Production("PsionicMartialPath").GetMethod("Service",BindingFlags.NonPublic|BindingFlags.Static)!;
                    void Record(int id)=>service.Invoke(null,[c.Creator,0f,rescue?0:id,rescue?id:0,false]);
                    int goal=rescue?10:20;
                    bool Done(DisciplineProgress p)=>p.CompletedTrials.Contains("rwom.psionic."+stage);
                    for(int i=1;i<goal;i++)Record(i);
                    Record(1);
                    Check.Equal(false,Done(soul),"repeating a pawn or building cannot satisfy the trial");
                    Record(goal);
                    Check.Equal(true,Done(soul),"physical service must advance original Psionic");
                    var host=Enroll(c.Creator);
                    for(int i=1;i<=goal;i++)Record(i);
                    Check.Equal(true,Done(host),"host's own path takes priority");
                }));
        yield return new("FacelessProjection.ControlOwner",f=>SpiritRebirthRitualTransactionTests.Run(f,c=>{
            using var held=new Fixture(f,c);
            var comp=c.Original.GetCompAbilityUserMight();
            var power=new MightAbility(comp){Def=TorannMagicDefOf.TM_Possess};
            comp.AbilityData.Powers.Add(power);comp.AbilityData.AllPowers=null;
            var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
            Check.Same(power,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastFacelessControl"])!,"control must use the soul's trained ability");
            var shot=new Projectile_Possess();
            typeof(Projectile_Possess).GetMethod("BindSource",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(shot,[c.Original]);
            var source=typeof(Projectile_Possess).GetMethod("SourceFor",BindingFlags.NonPublic|BindingFlags.Instance)!;
            Check.Same(c.Original,source.Invoke(shot,[c.Creator])!,"physical launch uses soul's ranks");
            Check.Equal(true,source.Invoke(shot,[c.Original])==null,"cannot rebind a launched shot to another body");
        }));
        yield return new("FacelessProjection.ReversalOwner",f=>
            SpiritRebirthRitualTransactionTests.Run(f,c=>{
                using var held=new Fixture(f,c);
                var source=c.Original.GetCompAbilityUserMight();
                var def=TorannMagicDefOf.TM_Reversal;
                var power=new MightAbility(source){Def=def};
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastReversal"])!,"original exact Reversal owner");
                var marker=(HediffComp)Activator.CreateInstance(typeof(MightAbility).Assembly.GetType("TorannMagic.HediffComp_Reversal",true)!,true)!;
                marker.parent=new HediffWithComps{pawn=c.Creator};
                var tracker=(HediffComp_SpiritCastOrigin)marker;
                typeof(HediffComp_SpiritCastOrigin).GetMethod("Bind",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(tracker,[c.Original]);
                var resolver=marker.GetType().GetProperty("SkillOwner",BindingFlags.NonPublic|BindingFlags.Instance)!;
                Check.Same(c.Original,resolver.GetValue(marker)!,"hosted reversal siphons the soul");
                var host=c.Creator.GetCompAbilityUserMight();host.AbilityData.Powers.Add(new MightAbility(host){Def=def});
                host.AbilityData.AllPowers=null;
                Check.Equal(true,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastReversal"])==null,"local Reversal takes priority");
            }));
        yield return new("FacelessProjection.ReversalDecayAfterReload", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var hediff=new HediffWithComps {pawn=c.Creator};
                var saved=new ReversalSave();
                saved.Effect.parent=hediff;
                float delta=0;
                for(int i=0;i<58;i++)saved.Effect.CompPostTick(ref delta);
                Check.Equal(0f,delta,"no decay before sixty ticks");
                var loaded=RoundTripSummon(f,"faceless-reversal-phase",saved);
                loaded.Effect.parent=hediff;
                float after=0;
                for(int i=0;i<3;i++){saved.Effect.CompPostTick(ref delta);loaded.Effect.CompPostTick(ref after);}
                Check.Equal(-1f,delta,"ordinary decay after sixty ticks");
                Check.Equal(delta,after,"loaded shield decays on the same tick");
            }));
        yield return new("FacelessProjection.StudyOwner", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.faceless",DisciplineDomain.Might);
                using var defs=FacelessCareerTests.Setup(f);
                var source=c.Original.GetCompAbilityUserMight();
                var power=new MightAbility(source){Def=TorannMagicDefOf.TM_Mimic};
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,"TM_SpiritStudyFaceless"])!,"projected study must use the original Mimic");
                var model=PawnWithComps(true,false);
                Set(model.pawn,"mapIndexOrState",(sbyte)0);Set(model.pawn,"positionInt",c.Creator.Position+IntVec3.East);
                model.magic.AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                var learn=Production("FacelessImitation").GetMethod("LearnFrom",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Equal(true,(bool)learn.Invoke(null,[c.Creator,c.Original,model.pawn])!,"learn from the carrier's map");
                var echo=DefDatabase<TMAbilityDef>.GetNamed("TM_FacelessEcho");
                Check.Equal(1,source.AbilityData.Powers.Count(p=>p.Def==echo),"soul owns echo");
                Check.Equal(0,c.Creator.GetCompAbilityUserMight().AbilityData.Powers.Count(p=>p.Def==echo),"host does not acquire echo");
                Check.Equal(false,(bool)learn.Invoke(null,[c.Creator,c.Original,model.pawn])!,"same family cannot refresh the echo");
            }));
        yield return new("FacelessProjection.RescueCredit", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                DisciplineProgress Enroll(Pawn pawn)
                {
                    var state=new ClassProgressionState(Vocation.Warrior);
                    Set(ClassIdentity.Component(pawn),"state",state);
                    state.TryAddFoundation("rwom.faceless",DisciplineDomain.Might);
                    return state.Find("rwom.faceless")!;
                }
                var soul=Enroll(c.Original);
                var rescue=Production("FacelessMartialPath").GetMethod("Rescue",BindingFlags.Static|BindingFlags.NonPublic)!;
                var patients=Enumerable.Range(0,6).Select(i=>{
                    var patient=PawnWithComps(false,false).pawn;
                    patient.thingIDNumber=47120+i;
                    return patient;
                }).ToArray();
                void Credit(Pawn p)=>rescue.Invoke(null,[c.Creator,p]);
                bool Complete(DisciplineProgress path)=>path.CompletedTrials.Contains("rwom.faceless.Foundation");
                Credit(patients[0]);Credit(patients[0]);Credit(patients[1]);
                Check.Equal(false,Complete(soul),"same rescued pawn counts only once");
                Credit(patients[2]);
                Check.Equal(true,Complete(soul),"third distinct rescue by carrier advances original Faceless");
                var host=Enroll(c.Creator);
                foreach(var patient in patients.Skip(3))Credit(patient);
                Check.Equal(true,Complete(host),"host's own Faceless path takes precedence");
            }));
        yield return new("RangerProjection.BreakthroughOrder",f=>
            SpiritRebirthRitualTransactionTests.Run(f,c=>{
                using var held=new Fixture(f,c);
                var source=c.Original.GetCompAbilityUserMight();
                var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerBreakthrough") ?? f.AddAbility("TM_RangerBreakthrough");
                if(DefDatabase<JobDef>.GetNamedSilentFail("TM_RangerBreakthroughCast")==null)
                    DefDatabase<JobDef>.Add(new JobDef{defName="TM_RangerBreakthroughCast",driverClass=typeof(JobDriver_RangerBreakthrough)});
                var ability=new RangerHuntAbility(source){Def=def};
                source.AbilityData.Powers.Add(ability);source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Same(ability,find.Invoke(null,[c.Creator,c.Original,"TM_RangerBreakthroughCast"])!,"projected hunt belongs to the ranger");
                var job=(Verse.AI.Job)typeof(RangerHuntAbility).GetMethod("MakeJobFor",BindingFlags.Instance|BindingFlags.NonPublic)!
                    .Invoke(ability,[c.Creator,new LocalTargetInfo(c.Creator)])!;
                Check.Same(c.Original,job.targetQueueB.Single().Pawn,"saved order identifies original ranger");
                var host=c.Creator.GetCompAbilityUserMight();
                host.AbilityData.Powers.Add(new RangerHuntAbility(host){Def=def});host.AbilityData.AllPowers=null;
                Check.Equal(true,find.Invoke(null,[c.Creator,c.Original,"TM_RangerBreakthroughCast"])==null,"native hunt has precedence");
            }));
        yield return new("RangerProjection.ArrowStormNoBow", f =>
        {
            var pawn=PawnWithComps(false,false).pawn;
            pawn.equipment=new Pawn_EquipmentTracker(pawn);
            Check.Equal(0f,Projectile_ArrowStorm.GetWeaponAccuracy(pawn),"no bow cannot supply arrow accuracy");
            Check.Equal(0,Projectile_ArrowStorm.GetWeaponDmg(pawn),"no bow cannot supply arrow damage");
        });
        yield return new("RangerProjection.SafeTrailAfterLoad", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.ranger",DisciplineDomain.Might);
                var path=state.Find("rwom.ranger");Set(path,"stage",InitiationStage.Master);
                path.RecordCompletedTrial("rwom.ranger.routes");
                var might=c.Original.GetCompAbilityUserMight();
                var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerSafeTrail")??f.AddAbility("TM_RangerSafeTrail");
                var power=new MightAbility(might){Def=def};might.AbilityData.Powers.Add(power);might.AbilityData.AllPowers=null;
                var foe=new Faction{def=new FactionDef()};
                Set(foe,"relations",new List<FactionRelation>{new(){other=c.Creator.Faction,kind=FactionRelationKind.Hostile}});
                var rel=typeof(Faction).GetField("relations",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var previous=rel.GetValue(c.Creator.Faction);
                var pos=c.Creator.Position;
                Set(c.Creator.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
                try
                {
                    Pawn Traveler(int id,Faction faction)
                    {
                        var pawn=PawnWithComps(false,false).pawn;
                        pawn.kindDef=new PawnKindDef{race=pawn.def};pawn.def.category=ThingCategory.Pawn;pawn.def.size=new IntVec2(1,1);
                        pawn.mindState=new Pawn_MindState(pawn);pawn.guest=new Pawn_GuestTracker(pawn);pawn.thingIDNumber=id;
                        Set(pawn,"mapIndexOrState",(sbyte)0);Set(pawn,"factionInt",faction);
                        return pawn;
                    }
                    Set(c.Creator,"positionInt",new IntVec3(7,0,7));
                    RangerTrailTests.Trail(f,c.Creator,Traveler(21924+1,c.Creator.Faction),Traveler(21926,foe),power);
                }
                finally { rel.SetValue(c.Creator.Faction,previous);Set(c.Creator,"positionInt",pos); }
            }));
        foreach(bool trails in new[]{false,true})
            yield return new(trails?"RangerProjection.OutdoorHaul":"RangerProjection.ClassicTraining", f =>
                SpiritRebirthRitualTransactionTests.Run(f,c => {
                    using var held=new Fixture(f,c);
                    var state=new ClassProgressionState(Vocation.Warrior);
                    Set(ClassIdentity.Component(c.Original),"state",state);
                    state.TryAddFoundation("rwom.ranger",DisciplineDomain.Might);
                    Set(state.Find("rwom.ranger"),"RangerTrailLore",trails);
                    var source=c.Original.GetCompAbilityUserMight();
                    source.MightData.MightPowerSkill_RangerTraining.Single().levelMax=trails?4:3;
                    var original=TorannMagicDefOf.Ranger;
                    TorannMagicDefOf.Ranger=new TraitDef{defName="Ranger"};
                    try { RangerFoundationTests.VerifyPassive(f,c.Creator,source,trails); }
                    finally { TorannMagicDefOf.Ranger=original; }
                }));
        foreach (bool treatment in new[]{false,true})
            yield return new("RangerProjection."+ (treatment?"VeterinaryShift":"StockpileFire"), f =>
                SpiritRebirthRitualTransactionTests.Run(f,c => {
                    using var held=new Fixture(f,c);
                    var state=new ClassProgressionState(Vocation.Warrior);
                    Set(ClassIdentity.Component(c.Original),"state",state);
                    state.TryAddFoundation("rwom.ranger",DisciplineDomain.Might);
                    Set(state.Find("rwom.ranger"),"stage",treatment?InitiationStage.Master:InitiationStage.Adept);
                    if(treatment)RangerAnimalCareTrialTests.Verify(c.Creator,c.Original);
                    else RangerFireTrialTests.Verify(f,c.Creator,c.Original);
                }));
        yield return new("RangerProjection.RetainedTrapPlanter", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.ranger",DisciplineDomain.Might);
                Set(state.Find("rwom.ranger"),"stage",InitiationStage.Adept);
                var foe=new Faction{def=new FactionDef()};
                Set(foe,"relations",new List<FactionRelation>{new(){other=c.Creator.Faction,kind=FactionRelationKind.Hostile}});
                var old=typeof(Faction).GetField("relations",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(c.Creator.Faction);
                Set(c.Creator.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
                try
                {
                    var victim=PawnWithComps(false,false).pawn;
                    victim.kindDef=new PawnKindDef{race=victim.def};victim.mindState=new Pawn_MindState(victim);victim.guest=new Pawn_GuestTracker(victim);
                    Set(victim,"mapIndexOrState",(sbyte)0);Set(victim,"factionInt",foe);
                    RangerTrailTests.Trap(c.Creator,victim,c.Original.GetCompAbilityUserMight());
                }
                finally { Set(c.Creator.Faction,"relations",old!); }
            }));
        yield return new("RangerProjection.PoisonTrapOrder", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var comp=c.Original.GetCompAbilityUserMight();
                var power=new MightAbility(comp){Def=TorannMagicDefOf.TM_PoisonTrap,CooldownTicksLeft=45};
                comp.AbilityData.Powers.Add(power);comp.AbilityData.AllPowers=null;
                const string job="TM_SpiritCastPoisonTrap";
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,job])!,"learned trap gets an order bound to the original");
                Check.Equal(45,power.CooldownTicksLeft,"no payment when displaying the trap");
            }));
        yield return new("RangerProjection.AnimalFriendOwner", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                const string job="TM_SpiritCastAnimalFriend";
                var def=TorannMagicDefOf.TM_AnimalFriend ?? f.AddAbility("TM_AnimalFriend");
                var source=c.Original.GetCompAbilityUserMight();
                var power=new MightAbility(source){Def=def,CooldownTicksLeft=73};
                source.AbilityData.Powers.Add(power);source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                object? FindPower()=>find.Invoke(null,[c.Creator,c.Original,job]);
                Check.Same(power,FindPower()!,"original's bond ability supplies the command");
                Check.Equal(73,power.CooldownTicksLeft,"command enumeration preserves cooldown");
                const string file="TM_SpiritRanger.xml";
                var jobs=System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/JobDefs",file));
                var order=jobs.Root!.Elements().Single(node=>(string?)node.Element("defName")==job);
                Check.Equal(typeof(JobDriver_SpiritHostedMartial).FullName,(string?)order.Element("driverClass"),"bond order uses the saved job");
                foreach(var language in new[]{"English","Russian","Spanish","Portuguese","PortugueseBrazilian","Ukrainian","ChineseSimplified"})
                {
                    var strings=System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Languages",language,"DefInjected/JobDef",file));
                    Check.Equal(true,strings.Root!.Element(job+".reportString")?.Value.Contains("TargetB")==true,"animal remains named in "+language);
                }
                var host=c.Creator.GetCompAbilityUserMight();
                host.AbilityData.Powers.Add(new MightAbility(host){Def=def});host.AbilityData.AllPowers=null;
                Check.Equal(true,FindPower()==null,"host's own bond takes precedence");
            }));
        yield return new("RangerProjection.CareFromHost", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.WarriorMage);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.ranger",DisciplineDomain.Might);
                var might=c.Original.GetCompAbilityUserMight();
                var previous=TorannMagicDefOf.TM_RangerBondHD;
                try
                {
                    var pet=PawnWithComps(false,false).pawn;
                    pet.thingIDNumber=78124;
                    pet.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    pet.def.category=ThingCategory.Pawn; pet.def.size=new IntVec2(1,1);
                    pet.def.race=new RaceProperties {intelligence=Intelligence.Animal};
                    pet.kindDef=new PawnKindDef {race=pet.def};
                    Set(pet.def.race,"fleshType",new FleshTypeDef {isOrganic=true});
                    Set(pet,"mapIndexOrState",(sbyte)0); Set(pet,"positionInt",c.Creator.Position);
                    Set(pet,"factionInt",c.Creator.Faction);
                    TorannMagicDefOf.TM_RangerBondHD=new HediffDef {defName="RangerCareTrialBond"};
                    pet.health.hediffSet.hediffs.Add(new Hediff {def=TorannMagicDefOf.TM_RangerBondHD,pawn=pet});
                    might.bondedPet=pet;
                    var job=new Job {def=new JobDef{defName="TM_RangerCompanionCare"},targetA=pet,targetB=c.Original,targetC=c.Creator.Position};
                    var driver=new JobDriver_RangerCompanionCare {job=job,pawn=c.Creator};
                    var toils=(IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null)!;
                    var finish=toils.Last();
                    finish.initAction();
                    int Visits() => (int)Production("RangerCareer").GetMethod("Days",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[c.Original])!;
                    Check.Equal(1,Visits(),"physical care by host advances retained bond");
                    finish.initAction();
                    Check.Equal(1,Visits(),"same-day repeated completion cannot double count");
                    int now=Find.TickManager.TicksGame;
                    var markerHediff=held.Possession.parent;
                    Set(Find.TickManager,"ticksGameInt",now+GenDate.TicksPerDay);
                    c.Creator.health.hediffSet.hediffs.Remove(markerHediff);
                    try
                    {
                        finish.initAction();
                        Check.Equal(1,Visits(),"a former host cannot earn the next daily visit");
                    }
                    finally { c.Creator.health.hediffSet.hediffs.Add(markerHediff); Set(Find.TickManager,"ticksGameInt",now); }
                    Check.Same(pet,might.bondedPet,"the same original still owns the pet");
                    Check.Equal(true,c.Creator.GetCompAbilityUserMight().bondedPet==null,"host did not acquire the soul's companion");
                    var bondType=typeof(CompAbilityUserMight).Assembly.GetType("TorannMagic.HediffComp_RangerBond",true)!;
                    var marker=new HediffWithComps{pawn=pet,def=TorannMagicDefOf.TM_RangerBondHD};
                    var bond=(HediffComp)Activator.CreateInstance(bondType)!;bond.parent=marker;marker.comps=[bond];
                    using var draw=new Patch(typeof(Pawn).GetProperty(nameof(Pawn.DrawPos))!.GetMethod!,nameof(CancelNative));
                    using var glow=new Patch(typeof(FleckMaker).GetMethods().Single(m=>m.Name=="ThrowHeatGlow" && m.GetParameters()[0].ParameterType==typeof(IntVec3)),nameof(CancelNative));
                    var roster=(IList<Pawn>)c.Map.mapPawns.AllPawnsSpawned;
                    if(!roster.Contains(c.Creator))roster.Add(c.Creator);
                    bondType.GetMethod("Initialize",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(bond,null);
                    Check.Same(c.Original,bondType.GetField("bonderPawn")!.GetValue(bond)!,"old unlinked marker finds the held ranger with this exact pet");
                    var versatility=might.MightData.MightPowerSkill_AnimalFriend.Single(s=>s.label=="TM_AnimalFriend_ver");
                    versatility.level=2;
                    bondType.GetMethod("UpdateBond",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(bond,null);
                    Check.Equal(2.5f,marker.Severity,"the retained pet uses Animal Friend training even if the original also has a magic component");
                }
                finally { TorannMagicDefOf.TM_RangerBondHD=previous; }
            }));
        yield return new("BladeProjection.SevenStepsOrder", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                var def = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladeSevenSteps") ?? f.AddAbility("TM_BladeSevenSteps");
                var source = c.Original.GetCompAbilityUserMight();
                var power = new BladeFormAbility(source) { Def=def };
                source.AbilityData.Powers.Add(power);
                source.AbilityData.AllPowers=null;
                var state = new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                var path = state.Find("rwom.bladedancer");
                Set(path,"stage",InitiationStage.Ascended);
                Set(path,"BladeUltimate",Enum.Parse(Production("BladeUltimate"),"SevenSteps"));
                const string name="TM_BladeSevenStepsCast";
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Same(power,find.Invoke(null,[c.Creator,c.Original,name])!,"retained Seven Steps uses the exact learned form");
                if (DefDatabase<JobDef>.GetNamedSilentFail(name)==null)
                    DefDatabase<JobDef>.Add(new JobDef{defName=name,driverClass=typeof(JobDriver_BladeSevenSteps)});
                var order=(Job)typeof(BladeFormAbility).GetMethod("MakeJobFor",BindingFlags.NonPublic|BindingFlags.Instance)!
                    .Invoke(power,[c.Creator,new LocalTargetInfo(c.Creator.Position+IntVec3.North)])!;
                Check.Same(c.Original,order.targetQueueB.Single().Pawn,"source survives independently of later possession changes");
                Check.Equal(c.Creator.Position,order.targetC.Cell,"route starts at the physical host");
                c.Original.ageTracker.AgeBiologicalTicks = 25L * GenDate.TicksPerYear;
                source.maxSP = 1;
                _ = source.MightData;
                if (source.Stamina == null) c.Original.needs.AllNeeds.Add(new Need_Stamina(c.Original){def=TorannMagicDefOf.TM_Stamina});
                source.Stamina.CurLevelPercentage=1;
                var driver=new JobDriver_BladeSevenSteps{pawn=c.Creator,job=order};
                Set(driver,"preparing",true);
                var finish=driver.GetType().GetMethod("FinishForm",BindingFlags.Instance|BindingFlags.NonPublic)!;
                c.Creator.health.hediffSet.hediffs.Remove(held.Possession.parent);
                try
                {
                    finish.Invoke(driver,null);
                    finish.Invoke(driver,null);
                    Check.Equal(true,Math.Abs(source.Stamina.CurLevelPercentage-.94f)<.001f,"exit during preparation charges the original once");
                    Check.Equal(true,power.CooldownTicksLeft>0,"interrupted form cooldown remains with source");
                    var paidDriver=new JobDriver_BladeSevenSteps{pawn=c.Creator,job=order};
                    Set(paidDriver,"paid",true);
                    float before=source.Stamina.CurLevel;
                    finish.Invoke(paidDriver,null);
                    Check.Equal(before,source.Stamina.CurLevel,"an already paid series has no second exit price");
                }
                finally { c.Creator.health.hediffSet.hediffs.Add(held.Possession.parent); }
                var exhausted=typeof(JobDriver_BladeSevenSteps).GetMethod("Exhausted",BindingFlags.Static|BindingFlags.NonPublic);
                Check.NotNull(exhausted,"movement fatigue must use the held discipline");
                Check.Equal(true,(bool)exhausted!.Invoke(null,[c.Creator])!,"the occupied body moves more slowly after Seven Steps");
                c.Creator.health.hediffSet.hediffs.Remove(held.Possession.parent);
                try { Check.Equal(false,(bool)exhausted.Invoke(null,[c.Creator])!,"former body no longer receives class fatigue"); }
                finally { c.Creator.health.hediffSet.hediffs.Add(held.Possession.parent); }
            }));
        yield return new("BladeProjection.ServiceWornWeapon", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                var state = new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                BladeServiceTests.Verify(f,c.Creator,c.Original);
            }));
        yield return new("BladeProjection.RiposteAfterSave", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                Set(state.Find("rwom.bladedancer"),"stage",InitiationStage.Adept);
                BladeRiposteTests.Verify(f,c.Creator,c.Original);
            }));
        yield return new("BladeProjection.MemoryAlternatingTargets", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                var path=state.Find("rwom.bladedancer");
                Set(path,"BladeMemory",true);
                var skill=c.Original.GetCompAbilityUserMight().MightData.MightPowerSkill_BladeFocus.Single();
                skill.levelMax=4; skill.level=4;
                BladeFoundationTests.Melee(c.Creator);
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                Check.Equal(1720,(int)path.GetType().GetField("BladeMemoryReadyAt",flags)!.GetValue(path)!,
                    "last switch writes the cooldown to the original discipline");
            }));
        yield return new("BladeProjection.TrainedFocus", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                var old=TorannMagicDefOf.Bladedancer;
                TorannMagicDefOf.Bladedancer=new TraitDef {defName="Bladedancer"};
                try
                {
                    var skill=c.Original.GetCompAbilityUserMight().MightData.MightPowerSkill_BladeFocus.Single();
                    skill.levelMax=3;
                    BladeFoundationTests.Verify(f,c.Creator,c.Original.GetCompAbilityUserMight(),false);
                }
                finally { TorannMagicDefOf.Bladedancer=old; }
            }));
        yield return new("BladeProjection.DeliverMedicine", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(ClassIdentity.Component(c.Original),"state",state);
                state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                Set(state.Find("rwom.bladedancer"),"stage",InitiationStage.Master);
                BladeMedicalSupplyTests.Verify(f,c.Creator,c.Original);
            }));
        yield return new("BladeProjection.RescueTrialOwnership", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held = new Fixture(f,c);
                void Train(Pawn pawn)
                {
                    var state = new ClassProgressionState(Vocation.Warrior);
                    Set(ClassIdentity.Component(pawn),"state",state);
                    state.TryAddFoundation("rwom.bladedancer",DisciplineDomain.Might);
                    Set(state.Find("rwom.bladedancer"),"stage",InitiationStage.Adept);
                }
                var api = Production("BladeMastery");
                var delivered = api.GetMethod("Delivered",BindingFlags.NonPublic|BindingFlags.Static)!;
                var rescues = api.GetMethod("Rescues",BindingFlags.NonPublic|BindingFlags.Static)!;
                int Count(Pawn pawn) => (int)rescues.Invoke(null,[pawn])!;
                var ally = new Pawn { thingIDNumber = 100001 };
                Train(c.Original);
                delivered.Invoke(null,[c.Creator,ally]);
                delivered.Invoke(null,[c.Creator,ally]);
                Check.Equal(1,Count(c.Original),"one delivery per casualty per day on the retained path");
                var possession = held.Possession.parent;
                c.Creator.health.hediffSet.hediffs.Remove(possession);
                try
                {
                    delivered.Invoke(null,[c.Creator,new Pawn { thingIDNumber = 100002 }]);
                    Check.Equal(1,Count(c.Original),"former host cannot credit a different casualty");
                }
                finally { c.Creator.health.hediffSet.hediffs.Add(possession); }
                Train(c.Creator);
                delivered.Invoke(null,[c.Creator,ally]);
                Check.Equal(1,Count(c.Creator),"local discipline owns the next delivery");
                Check.Equal(1,Count(c.Original),"local rescue does not double-credit the soul");
            }));
        foreach(var id in new[]{"TM_PhaseStrike","TM_PhaseStrike_I","TM_PhaseStrike_II","TM_PhaseStrike_III"})
            yield return new("BladeProjection.PhaseStrike."+id,f =>
                SpiritRebirthRitualTransactionTests.Run(f,c => {
                    using var held=new Fixture(f,c);
                    var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField(id)!.GetValue(null)!;
                    var source=c.Original.GetCompAbilityUserMight();
                    var original=new MightAbility(source){Def=def};source.AbilityData.Powers.Add(original);source.AbilityData.AllPowers=null;
                    var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                    object? Power()=>find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastPhaseStrike"]);
                    Check.Same(original,Power()!,"use the real acquired rank and its targeting range");
                    var host=c.Creator.GetCompAbilityUserMight();host.AbilityData.Powers.Add(new MightAbility(host){Def=TorannMagicDefOf.TM_PhaseStrike});
                    host.AbilityData.AllPowers=null;Check.Equal(true,Power()==null,"a native lower rank takes precedence");
                }));
        yield return new("GladiatorLastCircle.ProjectOnlyChosenAscension", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                void Ascend(Pawn p)
                {
                    var state=new ClassProgressionState(Vocation.Warrior);
                    Set(p.GetComp<CompClassProgression>(),"state",state);
                    state.TryAddFoundation("rwom.gladiator",DisciplineDomain.Might);
                    var path=state.Find("rwom.gladiator");
                    Set(path,"stage",InitiationStage.Ascended);
                    Set(path,"Ultimate",GladiatorUltimate.LastCircle);
                }
                var owns=typeof(JobDriver_LastCircle).GetMethod("CanProject",BindingFlags.NonPublic|BindingFlags.Static);
                Check.NotNull(owns,"the native stance requires an explicit projected command");
                bool Projects()=>(bool)owns!.Invoke(null,[c.Creator,c.Original])!;
                Check.Equal(false,Projects(),"a spirit without the ascension does not receive the command");
                Ascend(c.Original);
                Check.Equal(true,Projects(),"only the soul has the chosen stance");
                Ascend(c.Creator);
                Check.Equal(false,Projects(),"the host's own stance has priority");
            }));
        yield return new("GladiatorCover.HostUsesLearnedDefenseAndTraining", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var api=typeof(JobDriver_GladiatorDefense).GetMethod("CanOfferProjected",BindingFlags.NonPublic|BindingFlags.Static);
                Check.NotNull(api,"the ally guard needs a source-aware job");
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(c.Original.GetComp<CompClassProgression>(),"state",state);
                state.TryAddFoundation("rwom.gladiator",DisciplineDomain.Might);
                bool Offers(bool drill)=>(bool)api!.Invoke(null,[c.Creator,c.Original,drill])!;
                Check.Equal(true,Offers(true),"a foundation gladiator can run the relief drill from its host");
                Check.Equal(false,Offers(false),"training alone cannot unlock a combat defense");
                var path=state.Find("rwom.gladiator");
                Set(path,"stage",InitiationStage.Adept);Set(path,"Defense",GladiatorDefense.Cover);
                Check.Equal(true,Offers(false),"a learned defense can be projected");
            }));
        yield return new("GladiatorWhirlwind.OriginalAbilityOwnsHostFlight", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var def=TorannMagicDefOf.TM_Whirlwind;
                var source=c.Original.GetCompAbilityUserMight();
                var original=new MightAbility(source){ Def=def };
                source.AbilityData.Powers.Add(original);source.AbilityData.AllPowers=null;
                var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                object? Power()=>find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastWhirlwind"]);
                Check.Same(original,Power()!,"takeoff must use the retained ultimate");
                Check.Equal(false,TorannMagic.Odyssey.ManualTravelSafety.TakePayment(original),"no charge before actual takeoff");
                var host=c.Creator.GetCompAbilityUserMight();
                host.AbilityData.Powers.Add(new MightAbility(host){Def=def});host.AbilityData.AllPowers=null;
                Check.Equal(true,Power()==null,"native whirlwind prevents a second projected command");
            }));

        foreach (var id in new[] { "TM_Grapple","TM_Grapple_I","TM_Grapple_II","TM_Grapple_III" })
            yield return new("SpiritProjectedCast.Martial.Grapple."+id, f =>
                SpiritRebirthRitualTransactionTests.Run(f,c => {
                    using var held = new Fixture(f,c);
                    var def=(TMAbilityDef)typeof(TorannMagicDefOf).GetField(id)!.GetValue(null)!;
                    var source=c.Original.GetCompAbilityUserMight();
                    var power=new MightAbility(source){Def=def};
                    source.AbilityData.Powers.Add(power); source.AbilityData.AllPowers=null;
                    var find=Production("SpiritHostedMartial").GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                    object? Held() => find.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastGrapple"]);
                    Check.Same(power,Held()!,"retained rank supplies its own grapple range and cooldown");
                    var local=c.Creator.GetCompAbilityUserMight();
                    local.AbilityData.Powers.Add(new MightAbility(local){Def=TorannMagicDefOf.TM_Grapple});
                    local.AbilityData.AllPowers=null;
                    Check.Equal(true,Held()==null,"one family command when the body already knows grapple");
                }));

        yield return new("SpiritProjectedCast.Martial.Grapple.RestoreRetainedRank",f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var state=new ClassProgressionState(Vocation.Warrior);
                Set(c.Original.GetComp<CompClassProgression>(),"state",state);
                state.TryAddFoundation("rwom.gladiator",DisciplineDomain.Might);
                var might=c.Original.GetCompAbilityUserMight();
                var row=might.MightData.MightPowersG.Single(r=>r.TMabilityDefs.Contains(TorannMagicDefOf.TM_Grapple_III));
                var restore=typeof(JobDriver_GladiatorInitiation).GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Static)!;
                void Refresh()=>restore.Invoke(null,[might]);
                row.level=3;row.learned=false;Refresh();
                Check.Equal(false,might.AbilityData.Powers.Any(p=>p.Def==TorannMagicDefOf.TM_Grapple_III),"no free rank for an unlearned row");
                var rank=TorannMagicDefOf.TM_Grapple_III;
                var before=rank.abilityClass;
                try
                {
                    rank.abilityClass=typeof(MightAbility);
                    row.learned=true;Refresh();
                    var replay=might.AbilityData.Powers.Single(p=>p.Def==rank);
                    replay.CooldownTicksLeft=72;Refresh();
                    Check.Same(replay,might.AbilityData.Powers.Single(p=>p.Def==rank),"exact learned rank restored once");
                    Check.Equal(72,replay.CooldownTicksLeft,"refresh preserves the cooldown");
                }
                finally { rank.abilityClass=before; }
            }));

        yield return new("SpiritProjectedCast.Martial.GladiatorCleave.PassiveOwnerPrecedence", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var held=new Fixture(f,c);
                var api=Production("GladiatorMastery").GetMethod("AutomaticSource",BindingFlags.NonPublic|BindingFlags.Static);
                Check.NotNull(api,"native passive hit must resolve the owner before rolling");
                void SelectCleave(Pawn p)
                {
                    var state=new ClassProgressionState(Vocation.Warrior);
                    Set(p.GetComp<CompClassProgression>(),"state",state);
                    state.TryAddFoundation("rwom.gladiator",DisciplineDomain.Might);
                    Set(state.Find("rwom.gladiator"),"Attack",GladiatorAttack.Cleave);
                }
                SelectCleave(c.Original);
                var local=c.Creator.GetCompAbilityUserMight();
                object? Source() => api!.Invoke(null,[c.Creator,local]);
                Check.Same(c.Original.GetCompAbilityUserMight(),Source()!,"retained passive uses the original's skill, toggle and resource");
                SelectCleave(c.Creator);
                Check.Same(local,Source()!,"a native host passive wins without stacking the soul's hit");
            }));
        yield return new("SpiritProjectedCast.Martial.PommelStrike", f =>
            SpiritRebirthRitualTransactionTests.Run(f, c => {
                using var possession = new Fixture(f,c);
                var source = c.Original.GetCompAbilityUserMight();
                _ = source.MightData;
                var power = new MightAbility(source) { Def = TorannMagicDefOf.TM_PommelStrike };
                source.AbilityData.Powers.Add(power); source.AbilityData.AllPowers = null;
                var api = Production("SpiritHostedMartial");
                const BindingFlags methods = BindingFlags.Static | BindingFlags.NonPublic;
                Check.Same(power,api.GetMethod("Find",methods)!.Invoke(null,[c.Creator,c.Original,"TM_SpiritCastPommelStrike"])!,
                    "physical technique belongs to original with the exact learned ability");
                var target = PawnWithComps(false,false).pawn;
                Set(target.def.race,"fleshType",new FleshTypeDef { isOrganic = false });
                Check.Equal(false,(bool)api.GetMethod("CanStrike",methods)!.Invoke(null,[c.Creator,target,power])!,
                    "off-map enemy cannot be hit by retained skill");
                Check.Equal(false,(bool)api.GetMethod("CanStrike",methods)!.Invoke(null,[c.Creator,c.Creator,power])!,
                    "host cannot select itself to generate a hit");
                using var capture = new Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(CapturePommel));
                try
                {
                    pommelHit = null;
                    typeof(Verb_PommelStrike).GetMethod("Strike",methods)!.Invoke(null,[c.Creator,target,source]);
                    Check.Equal(true,pommelHit.HasValue,"one native damage packet");
                    Check.Same(c.Creator,pommelHit!.Value.Instigator!,"host body is the physical attacker");
                    Check.Same(DamageDefOf.Stun,pommelHit.Value.Def,"native nonlethal attack");
                    Check.Equal(10f,pommelHit.Value.Amount,"native stun strength");
                }
                finally { pommelHit=null; }
            }));
        yield return new("SpiritProjectedCast.Martial.RetainedSelfAidAtZero", f =>
            SpiritRebirthRitualTransactionTests.Run(f,c => {
                using var possession = new Fixture(f,c);
                var might = c.Original.GetCompAbilityUserMight();
                var def = TorannMagicDefOf.TM_FirstAid;
                var row = might.MightData.MightPowersSS.Single(p=>p.TMabilityDefs.Contains(def));
                var priorClass = def.abilityClass;
                try
                {
                    def.abilityClass = typeof(MightAbility);
                    row.learned = true;
                    var original = new MightAbility(might) {Def=def};
                    might.AbilityData.Powers.Add(original);
                    var clock = (IExposable)Activator.CreateInstance(Production("SpiritRuntimeClock"))!;
                    const BindingFlags methods = BindingFlags.Instance | BindingFlags.NonPublic;
                    clock.GetType().GetMethod("CaptureCooldowns",methods)!.Invoke(clock,[might]);
                    string file=Path.Combine(f.WorkingDirectory,"retained-self-aid.xml");
                    try { Scribe.saver.InitSaving(file,"test"); Scribe_Deep.Look(ref clock,"clock"); Scribe.saver.FinalizeSaving(); }
                    finally { Scribe.ForceStop(); }
                    might.AbilityData.Powers.Remove(original);
                    foreach(bool learned in new[]{false,true})
                    {
                        row.learned = learned;
                        clock=null!;
                        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref clock,"clock"); Scribe.loader.FinalizeLoading(); }
                        finally { Scribe.ForceStop(); }
                        clock.GetType().GetMethod("RestoreCooldowns",methods)!.Invoke(clock,[might]);
                        Check.Equal(learned,might.AbilityData.Powers.Any(p=>p.Def==def),"only known and still learned techniques return after a zero-cooldown save");
                    }
                }
                finally { def.abilityClass = priorClass; }
            }));
        yield return new("SpiritProjectedCast.Martial.CooldownsBothOwners", f =>
            SpiritRebirthRitualTransactionTests.Run(f, c => {
                using var possession = new Fixture(f,c);
                foreach (bool magic in new[] {true,false})
                {
                    CompAbilityUser owner = magic ? c.Original.GetCompAbilityUserMagic() : c.Original.GetCompAbilityUserMight();
                    var def = magic ? TorannMagicDefOf.TM_Shield : TorannMagicDefOf.TM_FirstAid;
                    var original = magic ? (PawnAbility)new MagicAbility(owner) : new MightAbility(owner);
                    original.Def = def; original.CooldownTicksLeft = 731;
                    owner.AbilityData.Powers.Add(original);
                    var clock = (IExposable)Activator.CreateInstance(Production("SpiritRuntimeClock"))!;
                    var capture = clock.GetType().GetMethod("CaptureCooldowns", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    capture.Invoke(clock,[owner]);
                    string path = Path.Combine(f.WorkingDirectory, magic ? "cooldown-magic.xml" : "cooldown-might.xml");
                    void RoundTrip()
                    {
                        try { Scribe.saver.InitSaving(path,"test"); Scribe_Deep.Look(ref clock,"clock"); Scribe.saver.FinalizeSaving(); }
                        finally { Scribe.ForceStop(); }
                        clock = null!;
                        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref clock,"clock"); Scribe.loader.FinalizeLoading(); }
                        finally { Scribe.ForceStop(); }
                    }
                    RoundTrip();
                    owner.AbilityData.Powers.Remove(original);
                    bool beta = ClassProgressionFeature.Enabled;
                    try
                    {
                        ClassProgressionFeature.Enabled = false;
                        Set(c.Creator.AllComps.OfType<CompClassProgression>().Single(),"state",new ClassProgressionState(Vocation.Warrior));
                        Check.Equal(true, (bool)clock.GetType().GetMethod("TracksCooldowns",BindingFlags.Static|BindingFlags.NonPublic)!
                            .Invoke(null,[c.Creator])!, "ordinary enrolled pawn retains its receipt while beta is off");
                        clock.GetType().GetMethod("RestoreCooldowns",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(clock,[owner]);
                        Check.Equal(false,owner.AbilityData.Powers.Any(p=>p.Def==def),"no grant while beta is disabled");
                        capture.Invoke(clock,[owner]);
                        RoundTrip();
                    }
                    finally { ClassProgressionFeature.Enabled = beta; }
                    var restored = magic ? (PawnAbility)new MagicAbility(owner) : new MightAbility(owner);
                    restored.Def = def; owner.AbilityData.Powers.Add(restored);
                    var restore = clock.GetType().GetMethod("RestoreCooldowns", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    restore.Invoke(clock,[owner]);
                    Check.Equal(731, restored.CooldownTicksLeft, "Scribe restores actual rank on reconstructed original");
                    restored.CooldownTicksLeft = 730;
                    restore.Invoke(clock,[owner]);
                    Check.Equal(730, restored.CooldownTicksLeft, "restoration is consumed and cannot reset progress");
                }
            }));
        foreach (bool aid in new[] { false, true })
            yield return new("SpiritProjectedCast.Martial." + (aid ? "SuperSoldierSelfAid" : "GladiatorManeuver"),
                f => SpiritRebirthRitualTransactionTests.Run(f, c => {
                    using var possession = new Fixture(f, c);
                    var soul = c.Original;
                    var body = c.Creator;
                    var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
                    var abilities = DefDatabase<TMAbilityDef>.AllDefsListForReading.ToArray();
                    var priorAid = TorannMagicDefOf.TM_FirstAid;
                    var events = DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
                    var oldEvent = TorannMagicDefOf.TM_UsedManeuver;
                    var settings = TorannMagic.ModOptions.Settings.Instance;
                    try
                    {
                        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                        var state = new ClassProgressionState(Vocation.Warrior);
                        state.GetType().GetMethod("ChooseMartialFoundation", BindingFlags.NonPublic | BindingFlags.Instance)!
                            .Invoke(state, ["rwom.gladiator", true]);
                        Set(ClassIdentity.Component(soul), "state", state);
                        var def = (TMAbilityDef)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
                            .Invoke(TorannMagicDefOf.TM_Disguise, null)!;
                        def.defName = aid ? "TM_FirstAid" : "TM_GladiatorManeuver";
                        def.consumeEnergy = true;
                        def.staminaCost = aid ? .4f : .12f;
                        def.MainVerb = new VerbProperties_Ability { warmupTime = 1, SecondsToRecharge = 25 };
                        DefDatabase<TMAbilityDef>.Clear();
                        DefDatabase<TMAbilityDef>.Add(def);
                        if (aid) TorannMagicDefOf.TM_FirstAid = def;
                        var effect = new HediffDef { defName = "TM_GladiatorManeuverHD", hediffClass = typeof(HediffWithComps),
                            comps = [new HediffCompProperties { compClass = typeof(HediffComp_SpiritCastOrigin) }] };
                        DefDatabase<HediffDef>.Add(effect);
                        if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                            DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
                        TorannMagicDefOf.TM_UsedManeuver = new HistoryEventDef { defName = "HostedMartialUse" };
                        DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedManeuver);
                        var history = (History)RuntimeHelpers.GetUninitializedObject(typeof(History));
                        history.historyEventsManager = new HistoryEventsManager();
                        Set(Current.Game, "history", history);

                        using var notify = new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!, nameof(CancelNative));
                        using var physiology = new Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!, nameof(CancelNative));
                        var source = soul.GetCompAbilityUserMight();
                        soul.ageTracker.AgeBiologicalTicks = 25L * GenDate.TicksPerYear;
                        source.maxSP = 1;
                        _ = source.MightData;
                        if (source.Stamina == null) soul.needs.AllNeeds.Add(new Need_Stamina(soul) { def = TorannMagicDefOf.TM_Stamina });
                        var ability = new MightAbility(source) { Def = def };
                        source.AbilityData.Powers.Add(ability);
                        source.AbilityData.AllPowers = null;
                        var api = Production("SpiritHostedMartial");
                        var name = aid ? "TM_SpiritCastFirstAid" : "TM_SpiritCastManeuver";
                        object? Call(string method, params object[] args) =>
                            api.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);
                        Check.Same(ability, Call("Find", body, soul, name)!, "soul owns original power");
                        var local = body.GetCompAbilityUserMagic();
                        var overlap = new PawnAbility(local) { Def = def };
                        local.AbilityData.Powers.Add(overlap); local.AbilityData.AllPowers = null;
                        Check.Equal(true, Call("Find", body, soul, name) == null, "other host component blocks duplicate");
                        local.AbilityData.Powers.Remove(overlap); local.AbilityData.AllPowers = null;
                        source.Stamina.CurLevel = .9f;
                        var wounds = new List<Hediff_Injury>();
                        if (aid)
                        {
                            Check.Equal(false, (bool)Call("Apply", body, soul, name)!, "no wound cannot consume");
                            Set(body.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
                            Set(body.def.race, "bloodDef", body.def);
                            body.def.race.bleedRateFactor = 1f;
                            var skills = source.MightData.MightPowerSkill_FirstAid;
                            skills.Single(s => s.label == "TM_FirstAid_pwr").level = 1;
                            skills.Single(s => s.label == "TM_FirstAid_ver").level = 2;
                            for (int i = 0; i < 4; i++)
                            {
                                var wound = new Hediff_Injury { pawn = body, Part = body.RaceProps.body.corePart,
                                    def = new HediffDef { defName = "MartialCut", injuryProps = new InjuryProps(), tendable = true } };
                                Set(wound, "severityInt", 2f);
                                wound.comps = [new TestMartialTend { parent = wound, props = new HediffCompProperties_TendDuration() }];
                                body.health.hediffSet.hediffs.Add(wound);
                                wounds.Add(wound);
                            }
                        }
                        Set(ClassIdentity.Component(soul).SpiritRebirth, "depletionPending", true);
                        Check.Equal(false, (bool)Call("Apply", body, soul, name)!, "pending collapse rejects new actions");
                        Set(ClassIdentity.Component(soul).SpiritRebirth, "depletionPending", false);
                        source.Stamina.CurLevel = 0;
                        Check.Equal(false, (bool)Call("Apply", body, soul, name)!, "no credit for an unaffordable action");
                        source.Stamina.CurLevel = .9f;
                        float before = source.Stamina.CurLevel, price = source.ActualStaminaCost(def);
                        Check.Equal(true, (bool)Call("Ready", body, ability, name)!, "body can perform learned action");
                        var job = JobMaker.MakeJob(new JobDef { defName = name }, soul);
                        var driver = new JobDriver_SpiritHostedMartial { pawn = body, job = job };
                        Set(body.jobs, "curJob", job); Set(body.jobs, "curDriver", driver);
                        var toils = ((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null)!).ToArray();
                        toils[0].initAction();
                        Check.Equal(before, source.Stamina.CurLevel, "no payment during warmup");
                        toils[1].initAction();
                        Check.Equal(true, price > 0 && Math.Abs(before - price - source.Stamina.CurLevel) < .0001f, "original owner pays once");
                        Check.Equal(true, ability.CooldownTicksLeft > 0, "original cooldown");
                        Check.Equal(false, (bool)Call("Apply", body, soul, name)!, "repeat cannot spend");
                        var file = Path.Combine(f.WorkingDirectory, "martial-driver.xml");
                        try
                        {
                            Scribe.saver.InitSaving(file, "test");
                            Scribe_Deep.Look(ref driver, "driver");
                            Scribe.saver.FinalizeSaving();
                        }
                        finally { Scribe.ForceStop(); }
                        driver = null!;
                        try
                        {
                            Scribe.loader.InitLoading(file);
                            Scribe_Deep.Look(ref driver, "driver");
                            driver.pawn = body; driver.job = job;
                            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler)
                                .GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                            directory.RegisterLoaded(body); directory.RegisterLoaded(soul);
                            Scribe.loader.FinalizeLoading();
                        }
                        finally { Scribe.ForceStop(); }
                        Check.Equal(true, (bool)typeof(JobDriver_SpiritHostedMartial).GetField("attempted",
                            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver)!, "loaded driver retains one-shot receipt");
                        if (aid)
                        {
                            Check.Equal(3, wounds.Count(w => w.TryGetComp<HediffComp_TendDuration>().tendTicksLeft > 0), "source rank affects body wound count");
                            Check.Equal(true, wounds.Take(3).All(w => w.TryGetComp<TestMartialTend>().RequestedQuality >= .2f), "source versatility affects tend quality");
                        }
                        else
                        {
                            var applied = body.health.hediffSet.GetFirstHediffOfDef(effect);
                            Check.NotNull(applied, "effect on body");
                            Check.Equal(false, soul.health.hediffSet.HasHediff(effect), "soul never wears effect");
                            var origin = applied.TryGetComp<HediffComp_SpiritCastOrigin>();
                            Check.Equal(false, origin.CompShouldRemove, "active possession");
                            var localState = new ClassProgressionState(Vocation.Warrior);
                            localState.GetType().GetMethod("ChooseMartialFoundation", BindingFlags.NonPublic | BindingFlags.Instance)!
                                .Invoke(localState, ["rwom.gladiator", true]);
                            Set(ClassIdentity.Component(body), "state", localState);
                            var native = new Verb_GladiatorManeuver { caster = body };
                            Set(native, "currentTarget", new LocalTargetInfo(body));
                            Check.Equal(true, (bool)native.GetType().GetMethod("TryCastShot", BindingFlags.NonPublic | BindingFlags.Instance)!
                                .Invoke(native, null)!, "native recast succeeds while projected effect still exists");
                            var own = body.health.hediffSet.GetFirstHediffOfDef(effect);
                            Check.Equal(false, ReferenceEquals(applied, own), "native maneuver has independent owner and duration");
                            ability.CooldownTicksLeft = 0;
                            Check.Equal(false, (bool)Call("Apply", body, soul, name)!, "cannot overwrite host's active native maneuver");
                            body.health.SetDead();
                            Check.Equal(false, own.TryGetComp<HediffComp_SpiritCastOrigin>().CompShouldRemove, "native maneuver is unbound");
                            Check.Equal(true, origin.CompShouldRemove, "effect ends on host death");
                        }
                    }
                    finally
                    {
                        DefDatabase<HistoryEventDef>.Clear(); foreach (var d in events) DefDatabase<HistoryEventDef>.Add(d);
                        TorannMagicDefOf.TM_FirstAid = priorAid;
                        TorannMagicDefOf.TM_UsedManeuver = oldEvent;
                        TorannMagic.ModOptions.Settings.Instance = settings;
                        DefDatabase<TMAbilityDef>.Clear(); foreach (var d in abilities) DefDatabase<TMAbilityDef>.Add(d);
                        DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs) DefDatabase<HediffDef>.Add(d);
                    }
                }));
    }
}
