using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using UnityEngine;

namespace RWoM.RegressionTests;
internal static partial class OdysseyCombatTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Combat.NativeInterceptorPaymentAndBypass", f => Run(Native));
        yield return new("Odyssey.FuelReserve.CombatDeniedEvenAtHighPriority", f => Run(() => Network(true)));
        yield return new("Odyssey.Combat.NetworkAutomaticFuel", f => Run(Network));
        yield return new("Odyssey.Combat.ThrowingPaymentInvalidates", f => Run(Throwing));
        yield return new("Odyssey.Combat.BoosterDetachAndTransfer", f => Run(Detach));
        yield return new("Odyssey.Combat.PaidChargeAndHit", f => Run(Paid));
        yield return new("Odyssey.Combat.BoostersElectionAndCollapse", f => Run(Boosters));
        yield return new("Odyssey.Combat.UnionBoundaryAndOwnership", f => Run(Boundary));
        yield return new("Odyssey.Combat.SaveTimersAndCrossMap", f => Run(() => Save(f)));
        yield return new("Odyssey.Combat.IndependentNetworkReceipts", f => Run(IndependentNetworks));
        yield return new("Odyssey.Combat.PeaceUpkeepAndRemoteHit", f => Run(() => PeaceUpkeep(f)));
    }

    static Type T(string name) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + name) ?? throw new Exception("Missing combat production type: " + name);
    static void Set(object obj, string name, object value)
    {
        for (Type? t = obj.GetType(); t != null; t = t.BaseType)
        {
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (fi != null)
            {
                fi.SetValue(obj, value);
                return;
            }
        }

        throw new Exception(name);
    }

    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name)!.Invoke(obj, args)!;
    static float HP(ThingComp c) => (float)c.GetType().GetProperty("CurrentHitPoints")!.GetValue(c)!;
    static void Near(float want, float got, string why)
    {
        if (Math.Abs(want - got) > .0001f)
            throw new Exception($"{why}: {want} != {got}");
    }

    static Map map = null !;
    static MapComponent field = null !;
    static Faction faction = null !;
    static int id;
    static void Run(Action action)
    {
        var old = Current.Game;
        var oldBomb = DamageDefOf.Bomb;
        var oldEmp = DamageDefOf.EMP;
        DamageDefOf.Bomb ??= new DamageDef
        {
            defaultDamage = 50
        };
        DamageDefOf.EMP ??= new DamageDef
        {
            defaultDamage = 10
        };
        var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldFlag = flag.GetValue(null);
        try
        {
            flag.SetValue(null, true);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Current.Game.Scenario = new Scenario();
            map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo
            {
                Size = new IntVec3(80, 1, 80)
            };
            map.cellIndices = new CellIndices(map);
            map.edificeGrid = new EdificeGrid(map);
            map.thingGrid = new ThingGrid(map);
            Set(Current.Game, "maps", new List<Map> { map });
            field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalCombat"), map)!;
            map.components = new List<MapComponent>
            {
                field,
                (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), map)!
            };
            faction = new Faction
            {
                def = new FactionDef
                {
                    isPlayer = true
                }
            };
            id = 900;
            action();
        }
        finally
        {
            saved = null;
            DamageDefOf.Bomb = oldBomb;
            DamageDefOf.EMP = oldEmp;
            Current.Game = old;
            flag.SetValue(null, oldFlag);
        }
    }

    static Building Building(int x, int z)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = "CombatTest";
        d.size = new IntVec2(1, 1);
        var b = new Building
        {
            def = d,
            thingIDNumber = ++id
        };
        Set(b, "positionInt", new IntVec3(x, 0, z));
        Set(b, "mapIndexOrState", (sbyte)0);
        Set(b, "factionInt", faction);
        Set(b, "comps", new List<ThingComp>());
        return b;
    }

    static ThingComp Comp(Building b, string n)
    {
        var c = (ThingComp)Activator.CreateInstance(T("Comp" + n))!;
        c.parent = b;
        c.props = (CompProperties)Activator.CreateInstance(T("CompProperties_" + n))!;
        b.AllComps.Add(c);
        c.Initialize(c.props);
        return c;
    }

    static ThingComp Source(int x = 15, int z = 15, bool booster = false)
    {
        var b = Building(x, z);
        var c = Comp(b, booster ? "MagicalCombatBooster" : "MagicalCombat");
        if (!booster)
        {
            var bank = Comp(b, "ShieldManaBuffer");
            Set(bank, "storedMana", 100f);
        }

        var endpoint = Comp(b, "ManaNetworkEndpoint");
        endpoint.PostSpawnSetup(false);
        c.PostSpawnSetup(false);
        return c;
    }

    static ThingComp Beacon(int x, int z)
    {
        var b = Building(x, z);
        var c = Comp(b, "ManaBeacon");
        c.PostSpawnSetup(false);
        return c;
    }

    static float Mana(ThingComp c) => (float)c.parent.AllComps.Single(x => x.GetType().Name == "CompShieldManaBuffer").GetType().GetProperty("StoredMana")!.GetValue(c.parent.AllComps.Single(x => x.GetType().Name == "CompShieldManaBuffer"))!;
    static void Tick(int t)
    {
        Set(Find.TickManager, "ticksGameInt", t);
        field.MapComponentTick();
    }

    static bool Hit(ThingComp main, float damage, int x = 15, int z = 15) => (bool)Call(field, "TryAbsorb", main, damage, new Vector3(x + .5f, 0, z + .5f));
    static void IndependentNetworks()
    {
        var a = Source(); var b = Source(40, 40); var ownBeacon = Beacon(15, 16); Beacon(40, 41);
        Tick(60); float paid = Mana(a), hp = HP(a);
        var flick = new CompFlickable { parent = b.parent, props = new CompProperties_Flickable() };
        b.parent.AllComps.Add(flick); Set(flick, "switchOnInt", false);
        Set(Find.TickManager, "ticksGameInt", 61);
        Check.Equal(true, (bool)Call(field, "Covers", new Vector3(15.5f, 0, 15.5f)), "unrelated switch preserves shield");
        Check.Equal(false, Hit(b, 1, 40, 40), "switched source loses coverage immediately");
        var unrelated = Source(60, 60); Check.Equal(true, (bool)Call(field, "CanEmit", a.parent), "unrelated registration preserves shield");
        unrelated.PostDeSpawn(map); Check.Equal(true, (bool)Call(field, "CanEmit", a.parent), "unrelated removal preserves shield");
        Near(paid, Mana(a), "validation never repays"); Near(hp, HP(a), "validation never regenerates");
        ownBeacon.PostDeSpawn(map); Check.Equal(false, Hit(a, 1), "own topology change invalidates immediately");
        Near(paid, Mana(a), "topology validation never repays"); Near(hp, HP(a), "topology validation never regenerates");
        Tick(120); Check.Equal(true, (bool)Call(field, "CanEmit", a.parent), "next paid interval restores valid source");
        Set(a.parent, "factionInt", new Faction { def = new FactionDef() }); Check.Equal(false, Hit(a, 1), "own ownership change invalidates immediately");
    }
    static void Paid()
    {
        var main = Source();
        Near(0, HP(main), "new generator empty");
        Check.Equal(false, Hit(main, 1), "unpaid no interception");
        Tick(60);
        Near(25, HP(main), "paid 25 HP per network per second");
        Near(100 - .25f - .0405f, Mana(main), "union upkeep and exact recharge paid");
        Check.Equal(true, Hit(main, 10), "paid interception");
        Near(15, HP(main), "hit deducted");
        var mana = Mana(main);
        Tick(60);
        Near(mana, Mana(main), "repeated pulse free of duplicate spend");
        Tick(120);
        Near(15, HP(main), "combat recovery delay");
        Tick(660);
        Near(40, HP(main), "recovery after ten seconds");
        var bank = main.parent.AllComps.Single(x => x.GetType().Name == "CompShieldManaBuffer");
        Set(bank, "storedMana", 0f);
        Tick(720);
        Check.Equal(false, Hit(main, 1), "unpaid upkeep drops coverage");
        Near(40, HP(main), "unpaid hit no HP payment");
    }

    static void PeaceUpkeep(RuntimeFixture fixture)
    {
        map.mapPawns = new MapPawns(map);
        var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
        world.factionManager = new FactionManager();
        world.factionManager.AllFactionsListForReading.Add(faction);
        Set(Current.Game, "worldInt", world);
        var main = Source();
        Set(main, "currentHitPoints", 1000f);
        Tick(60);
        Near(100f - .00405f, Mana(main), "empty map pays ten percent upkeep");
        var before = Mana(main);
        Tick(60);
        Near(before, Mana(main), "peace mode cannot double bill same pulse");
        Check.Equal(true, Hit(main, 1), "peace shield still intercepts remote attacks");
        Tick(120);
        Near(before - .0405f, Mana(main), "recent hit restores combat upkeep without a local hostile pawn");
        before = Mana(main);
        Tick(3660);
        Near(before - .00405f - .01f, Mana(main), "after one minute peace upkeep resumes but HP repair stays full price");
        Near(1000f, HP(main), "repair restores only lost HP");

        var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var enemy = new Pawn { def = pawnDef, kindDef = new PawnKindDef() };
        enemy.health = new Pawn_HealthTracker(enemy);
        enemy.mindState = new Verse.AI.Pawn_MindState(enemy);
        var hostile = new Faction { def = new FactionDef() };
        Set(hostile, "relations", new List<FactionRelation> { new FactionRelation { other = faction, kind = FactionRelationKind.Hostile } });
        Set(enemy, "factionInt", hostile); Set(enemy, "mapIndexOrState", (sbyte)0);
        ((List<Pawn>)map.mapPawns.AllPawnsSpawned).Add(enemy);
        before = Mana(main); Tick(3720);
        Near(before - .0405f, Mana(main), "hostile pawn restores full upkeep");
        Set(enemy, "factionInt", faction);
        before = Mana(main); Tick(3780);
        Near(before - .0405f, Mana(main), "last hostile disappearing starts one-minute combat hold");
        string path = Path.Combine(fixture.WorkingDirectory, "peace-timer.xml");
        try { Scribe.saver.InitSaving(path, "regression"); field.ExposeData(); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        var loaded = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalCombat"), map)!;
        try { Scribe.loader.InitLoading(path); loaded.ExposeData(); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        map.components.Remove(field); field = loaded; map.components.Add(field);
        main.PostSpawnSetup(true);
        before = Mana(main); Tick(7320);
        Near(before - .0405f, Mana(main), "saved combat hold remains full at 59 seconds");
        before = Mana(main); Tick(7380);
        Near(before - .00405f, Mana(main), "nonhostile pawn allows discount after full minute");
        Check.Equal(true, Hit(main, 1), "remote hit before generator switch");
        var flick = new CompFlickable { parent = main.parent, props = new CompProperties_Flickable() };
        main.parent.AllComps.Add(flick); Set(flick, "switchOnInt", false);
        var replacement = Source(40, 40); Set(replacement, "currentHitPoints", 1000f);
        before = Mana(replacement); Tick(7440);
        Near(before - .0405f, Mana(replacement), "switching off hit source must not bypass combat cooldown for another generator");
        main.PostDeSpawn(map); replacement.PostDeSpawn(map);
        Set(main.parent, "mapIndexOrState", (sbyte)-1); Set(replacement.parent, "mapIndexOrState", (sbyte)-1);
        Set(enemy, "factionInt", hostile); Tick(12000);
        Set(enemy, "factionInt", faction); Tick(12060);
        var rebuilt = Source(55, 55); Set(rebuilt, "currentHitPoints", 1000f);
        before = Mana(rebuilt); Tick(12120);
        Near(before - .0405f, Mana(rebuilt), "enemy departure tracked even while all generators were removed");
    }

    static void Boosters()
    {
        Beacon(15, 15);
        Beacon(20, 15);
        var main = Source();
        var standby = Source(17, 15);
        var a = Source(19, 15, true);
        var b = Source(20, 16, true);
        Set(main, "currentHitPoints", 100f);
        Set(a, "currentHitPoints", 20f);
        Set(b, "currentHitPoints", 30f);
        Tick(60);
        Near(0, HP(standby), "standby no charge");
        Check.Equal(true, (bool)Call(field, "IsStandby", standby), "one main elected");
        var mainBefore = HP(main);
        Check.Equal(true, Hit(main, 40, 20, 16), "booster hit accepted");
        Near(0, HP(b), "nearest booster first");
        Near(10, HP(a), "remaining damage next booster");
        Near(mainBefore, HP(main), "main protected by boosters");
        Check.Equal(true, Hit(main, 10000), "breaking hit fully absorbed");
        Near(0, HP(main), "main collapsed");
        Near(0, HP(a), "booster exhausted");
        Check.Equal(false, Hit(main, 1), "collapsed field down");
        Tick(1800);
        Near(0, HP(main), "not yet 30 seconds");
        Tick(1860);
        Near(25, HP(main), "paid reboot recharge");
    }

    static void Boundary()
    {
        var node = Beacon(20, 15);
        var main = Source();
        Tick(60);
        Check.Equal(true, (bool)Call(field, "CrossesBoundary", main, new Vector3(30.5f, 0, 15.5f), new Vector3(20.5f, 0, 15.5f), new Vector3(30.5f, 0, 15.5f)), "incoming segment enters");
        Check.Equal(false, (bool)Call(field, "CrossesBoundary", main, new Vector3(15.5f, 0, 15.5f), new Vector3(20.5f, 0, 15.5f), new Vector3(15.5f, 0, 15.5f)), "inside-origin union passes seams");
        Set(node.parent, "positionInt", new IntVec3(50, 0, 50));
        Check.Equal(false, Hit(main, 1), "moved beacon invalidates prepaid field");
        Tick(120);
        Check.Equal(true, Hit(main, 1), "new paid topology works");
        Set(main.parent, "factionInt", new Faction { def = new FactionDef() });
        Check.Equal(false, Hit(main, 1), "changed owner cannot use field");
    }

    sealed class Patch : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string patchId = "rwom.combat." + Guid.NewGuid();
        public Patch()
        {
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, patchId)!;
        }

        public void Add(MethodBase method, MethodInfo callback, bool postfix = false)
        {
            var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, callback)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object? [] { method, postfix ? null : hm, postfix ? hm : null, null, null });
        }

        public void Production(MethodBase method, string name) => Add(method, T(name).GetMethod("Prefix")!);
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { patchId });
    }

    static Thing Enemy()
    {
        var b = Building(35, 15);
        var hostile = new Faction
        {
            def = new FactionDef()
        };
        Set(hostile, "relations", new List<FactionRelation> { new FactionRelation { other = faction, kind = FactionRelationKind.Hostile } });
        Set(b, "factionInt", hostile);
        return b;
    }

    public sealed class ProbeBullet : Bullet
    {
        public bool Blocked;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Blocked = blockedByShield;
        }
    }

    static Projectile Shot(Thing launcher, bool overhead = false)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "CombatBullet";
        def.projectile = new ProjectileProperties
        {
            damageDef = new DamageDef
            {
                defaultDamage = 8
            },
            speed = 60,
            flyOverhead = overhead
        };
        var shot = new ProbeBullet
        {
            def = def
        };
        Set(shot, "launcher", launcher);
        Set(shot, "origin", new Vector3(35.5f, 0, 15.5f));
        Set(shot, "mapIndexOrState", (sbyte)0);
        return shot;
    }

    static bool SkipVisual() => false;
    static void Native()
    {
        Check.Equal(true, T("CompMagicalCombatInterceptor").IsDefined(typeof(StaticConstructorOnStartup), false), "native graphics cache initializes on startup main thread");
        using var patches = new Patch();
        var skip = typeof(OdysseyCombatTests).GetMethod(nameof(SkipVisual), BindingFlags.Static | BindingFlags.NonPublic)!;
        patches.Add(typeof(ContentFinder<Texture2D>).GetMethod("Get", new[] { typeof(string), typeof(bool) })!, skip);
        foreach (var method in typeof(MaterialPool).GetMethods().Where(m => m.Name == "MatFrom"))
            patches.Add(method, skip);
        var mods = LoadedModManager.RunningModsListForReading.ToArray();
        try
        {
            LoadedModManager.RunningModsListForReading.Clear();
            RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle);
        }
        finally
        {
            LoadedModManager.RunningModsListForReading.AddRange(mods);
        }

        patches.Production(typeof(CompProjectileInterceptor).GetProperty("Active")!.GetMethod!, "MagicalCombatActivePatch");
        patches.Production(typeof(CompProjectileInterceptor).GetMethod("CheckIntercept")!, "MagicalCombatProjectilePatch");
        patches.Production(typeof(CompProjectileInterceptor).GetMethod("CheckBombardmentIntercept")!, "MagicalCombatBombardmentPatch");
        patches.Production(typeof(CompProjectileInterceptor).GetMethod("BombardmentCanStartFireAt")!, "MagicalCombatBombardmentFirePatch");
        var node = Beacon(20, 15);
        var main = Source();
        Set(main, "currentHitPoints", 500f);
        Tick(60);
        var first = (CompProjectileInterceptor)Comp((Building)main.parent, "MagicalCombatInterceptor");
        var second = (CompProjectileInterceptor)Comp((Building)node.parent, "MagicalCombatInterceptor");
        main.parent.def.comps = main.parent.AllComps.Select(c => c.props).ToList();
        Check.Equal(true, ThingRequestGroup.ProjectileInterceptor.Includes(main.parent.def), "native lister recognizes derived interceptor");
        main.parent.def.size = new IntVec2(2, 2);
        Check.Equal(main.parent.Position.ToVector3Shifted(), (Vector3)first.GetType().GetProperty("FieldCenter")!.GetValue(first)!, "2x2 ring anchor matches coverage not offset draw center");
        Check.Equal(true, first.Active, "native Active uses paid shared field");
        var enemy = Enemy();
        var shot = Shot(enemy);
        var from = new Vector3(35.5f, 0, 15.5f);
        var to = new Vector3(15.5f, 0, 15.5f);
        float before = HP(main);
        Check.Equal(true, second.CheckIntercept(shot, from, to), "real native bullet intercepted");
        Near(before - 8, HP(main), "native bullet costs HP");
        Check.Equal(true, first.CheckIntercept(shot, from, to), "duplicate projectile receipt");
        Near(before - 8, HP(main), "duplicate emitter does not charge twice");
        Check.Equal(false, second.CheckIntercept(Shot(main.parent), from, to), "friendly passes");
        Check.Equal(true, second.CheckIntercept(Shot(enemy), from, new Vector3(0.5f, 0, 15.5f)), "high-speed outside-to-outside crossed union");
        Check.Equal(false, second.CheckIntercept(Shot(enemy), to, new Vector3(20.5f, 0, 15.5f)), "internal seam does not intercept");
        var inside = Shot(enemy);
        Set(inside, "origin", to);
        Check.Equal(false, second.CheckIntercept(inside, to, from), "hostile born inside passes");
        Check.Equal(true, second.CheckIntercept(Shot(enemy, true), from, to), "native overhead intercepted");
        var bomb = new Bombardment
        {
            instigator = enemy
        };
        var projectile = new Bombardment.BombardmentProjectile(60, new IntVec3(15, 0, 15));
        before = HP(main);
        Check.Equal(true, first.CheckBombardmentIntercept(bomb, projectile), "native bombardment intercepted");
        Near(before - DamageDefOf.Bomb.defaultDamage, HP(main), "bombardment not free");
        Check.Equal(true, second.CheckBombardmentIntercept(bomb, projectile), "bombardment duplicate accepted");
        Near(before - DamageDefOf.Bomb.defaultDamage, HP(main), "bombardment duplicate paid once");
        Check.Equal(true, first.BombardmentCanStartFireAt(bomb, new IntVec3(15, 0, 15)), "no free orbital random fire protection");
        before = HP(main);
        first.CompTick();
        Near(before, HP(main), "native emitter tick cannot regenerate");
        map.listerThings = new ListerThings(ListerThingsUse.Global);
        var lists = (List<Thing>[])typeof(ListerThings).GetField("listsByGroup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map.listerThings)!;
        lists[(int)ThingRequestGroup.ProjectileInterceptor] = new List<Thing>
        {
            main.parent,
            node.parent
        };
        var travelling = (ProbeBullet)Shot(enemy);
        before = HP(main);
        Check.Equal(true, (bool)typeof(Projectile).GetMethod("CheckForFreeInterceptBetween", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(travelling, new object[] { from, to })!, "native travel pipeline discovers interceptor");
        Check.Equal(true, travelling.Blocked, "native pipeline delivers blocked impact");
        Near(before - 8, HP(main), "native traveling projectile paid once");
        var emp = new DamageInfo(DamageDefOf.EMP, 100);
        first.PostPreApplyDamage(ref emp, out var absorbed);
        Check.Equal(false, absorbed, "building EMP damage not magically eaten");
        Check.Equal(true, first.Active, "EMP does not disable magical field");
        var args = new object[]
        {
            new CompProjectileInterceptor(),
            false
        };
        Check.Equal(true, (bool)T("MagicalCombatActivePatch").GetMethod("Prefix")!.Invoke(null, args)!, "unrelated native shield method retained");
    }

    static void Network() => Network(false);
    static void Network(bool reserve)
    {
        Beacon(15, 15);
        var main = Source();
        var standby = Source(16, 15);
        foreach (var source in new[]
        {
            main,
            standby
        }

        )
        {
            Set(source.parent.AllComps.Single(c => c.GetType().Name == "CompShieldManaBuffer"), "storedMana", 0f);
            var ep = source.parent.AllComps.Single(c => c.GetType().Name == "CompManaNetworkEndpoint");
            ep.GetType().GetProperty("ManaMode")!.SetValue(ep, Enum.Parse(T("ManaNetworkMode"), "Receive"));
            ep.GetType().GetProperty("FuelMode")!.SetValue(ep, Enum.Parse(T("ManaNetworkMode"), "Off"));
        }

        var parent = Building(14, 15);
        var bank = Comp(parent, "MagofuelReservoir");
        var sender = Comp(parent, "ManaNetworkEndpoint");
        sender.PostSpawnSetup(false);
        var fd = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        fd.defName = "TM_Magofuel";
        fd.virtualDefs = new List<ThingDef>();
        var filter = new ThingFilter();
        filter.SetAllow(fd, true);
        var fuel = new CompRefuelable
        {
            parent = parent,
            props = new CompProperties_Refuelable
            {
                fuelCapacity = 500,
                fuelFilter = filter,
                consumeFuelOnlyWhenUsed = true
            }
        };
        parent.AllComps.Add(fuel);
        fuel.Refuel(3);
        if (reserve) { parent.def.defName = "TM_MagofuelReservoir"; bank.GetType().GetProperty("FuelReserve")!.SetValue(bank, 3f); foreach (var source in new[] { main, standby }) source.parent.GetComp<TorannMagic.Odyssey.CompManaNetworkEndpoint>().ManaPriority = 3; }
        var graph = map.components.Single(c => c.GetType().Name == "MapComponent_ManaNetwork");
        Set(Find.TickManager, "ticksGameInt", 60);
        graph.MapComponentTick();
        if (reserve) { Near(3, fuel.Fuel, "combat high priority cannot spend protected fuel"); Near(0, main.parent.GetComp<TorannMagic.Odyssey.CompMagofuelReservoir>().StoredMana, "no free mana from protected fuel"); return; }
        Near(2, fuel.Fuel, "combat need converts one fuel");
        Tick(60);
        Near(25, HP(main), "native network converted fuel charges HP");
        Set(main.parent.AllComps.Single(c => c.GetType().Name == "CompShieldManaBuffer"), "storedMana", 100f);
        Set(bank, "storedMana", 0f);
        Set(Find.TickManager, "ticksGameInt", 120);
        graph.MapComponentTick();
        Near(2, fuel.Fuel, "standby cannot trigger fuel conversion");
        foreach (var source in new[]
        {
            main,
            standby
        }

        )
        {
            var flick = new CompFlickable
            {
                parent = source.parent,
                props = new CompProperties_Flickable()
            };
            source.parent.AllComps.Add(flick);
            Set(flick, "switchOnInt", false);
        }

        Set(main.parent.AllComps.Single(c => c.GetType().Name == "CompShieldManaBuffer"), "storedMana", 0f);
        Set(Find.TickManager, "ticksGameInt", 180);
        graph.MapComponentTick();
        Near(2, fuel.Fuel, "off generators cannot convert");
    }

    static void AfterWithdraw() => throw new InvalidOperationException("test payment callback");
    static void Throwing()
    {
        var main = Source();
        Tick(60);
        float before = HP(main);
        using (var patch = new Patch())
        {
            patch.Add(T("CompMagofuelReservoir").GetMethod("WithdrawMana")!, typeof(OdysseyCombatTests).GetMethod(nameof(AfterWithdraw), BindingFlags.Static | BindingFlags.NonPublic)!, true);
            try
            {
                Tick(120);
                throw new Exception("expected callback");
            }
            catch (InvalidOperationException)
            {
            }
        }

        Check.Equal(false, Hit(main, 1), "throwing payment clears old protection");
        Near(before, HP(main), "throwing payment gives no free regen");
        var mana = Mana(main);
        Tick(120);
        Near(mana, Mana(main), "same tick cannot retry payment");
        Tick(180);
        Check.Equal(true, Hit(main, 1), "later paid recovery works");
    }

    static void Detach()
    {
        Beacon(15, 15);
        var main = Source();
        var booster = Source(16, 15, true);
        Set(main, "currentHitPoints", 1000f);
        Set(booster, "currentHitPoints", 100f);
        Tick(60);
        Hit(main, 20);
        Near(105, HP(booster), "paid initial charge then booster damage");
        booster.PostDeSpawn(map);
        Set(booster.parent, "mapIndexOrState", (sbyte)-1);
        Check.Equal(false, Hit(main, 1), "detached booster invalidates field");
        Set(booster.parent, "mapIndexOrState", (sbyte)0);
        booster.PostSpawnSetup(true);
        Tick(120);
        Near(105, HP(booster), "reconnect cannot heal or clear damage timer");
        var old = map;
        var moved = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        moved.info = new MapInfo
        {
            Size = new IntVec3(80, 1, 80)
        };
        moved.cellIndices = new CellIndices(moved);
        Set(Current.Game, "maps", new List<Map> { old, moved });
        main.PostDeSpawn(old);
        Set(main.parent, "mapIndexOrState", (sbyte)1);
        moved.components = new List<MapComponent>
        {
            (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalCombat"), moved)!,
            (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), moved)!
        };
        main.PostSpawnSetup(true);
        var mana = Mana(main);
        moved.components[0].MapComponentTick();
        Near(mana, Mana(main), "physical main receipt survives actual map transfer");
    }

    static ThingComp? saved;
    public sealed class SourceSave : IExposable
    {
        public void ExposeData()
        {
            saved!.PostExposeData();
            field.ExposeData();
        }
    }

    static void Save(RuntimeFixture f)
    {
        Beacon(15, 15);
        var booster = Source(16, 15, true);
        var main = Source();
        Tick(60);
        Hit(main, 100);
        saved = main;
        var state = new SourceSave();
        var path = Path.Combine(f.WorkingDirectory, "combat-source.xml");
        try
        {
            Scribe.saver.InitSaving(path, "test");
            Scribe_Deep.Look(ref state, "state");
            Scribe.saver.FinalizeSaving();
        }
        finally
        {
            Scribe.ForceStop();
        }

        Set(main, "currentHitPoints", 999f);
        Set(main, "rebootUntilTick", 0);
        Set(main, "lastDamageTick", -1);
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref state, "state");
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            Scribe.ForceStop();
        }

        Near(0, HP(main), "HP restored from physical source");
        Tick(120);
        Near(0, HP(main), "collapse persists load");
        Check.Equal(false, Hit(main, 1), "load did not restore coverage");
        var mana = Mana(main);
        field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalCombat"), map)!;
        map.components[0] = field;
        main.PostSpawnSetup(true);
        Tick(120);
        Near(mana, Mana(main), "new map controller cannot repay source same tick");
        Tick(1860);
        Near(25, HP(main), "saved reboot recovers paid");
        saved = booster;
        Set(booster, "currentHitPoints", 123f);
        Set(booster, "lastDamageTick", 1800);
        Set(booster, "lastRegenerationTick", 1860);
        try
        {
            Scribe.saver.InitSaving(path, "test");
            Scribe_Deep.Look(ref state, "state");
            Scribe.saver.FinalizeSaving();
        }
        finally
        {
            Scribe.ForceStop();
        }

        Set(booster, "currentHitPoints", 0f);
        Set(booster, "lastDamageTick", -1);
        Set(booster, "lastRegenerationTick", -1);
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref state, "state");
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            Scribe.ForceStop();
        }

        Near(123, HP(booster), "booster physical HP saved independently");
        var xml = System.Xml.Linq.XDocument.Load(path);
        Check.Equal("1800", xml.Descendants("TM_combatLastDamageTick").Single().Value, "booster damage receipt persisted");
        Check.Equal("1860", xml.Descendants("TM_combatLastRegenerationTick").Single().Value, "booster recovery receipt persisted");
    }
}



