using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public class MapComponent_MagicalCombat : MapComponent
    {
        public const int Interval = 60;
        public const float Radius = 5f;
        public const int PeaceDelayTicks = 3600;
        readonly HashSet<CompMagicalCombatSource> sources = new HashSet<CompMagicalCombatSource>();
        readonly List<Group> groups = new List<Group>();
        readonly Dictionary<object, int> receipts = new Dictionary<object, int>();
        List<CompMagicalCombatSource> snapshot = new List<CompMagicalCombatSource>();
        List<CompManaBeacon> beacons = new List<CompManaBeacon>();
        List<Placement> placements = new List<Placement>();
        int lastPulse = -1, receiptTick = -1;
        int lastThreatTick = -1;
        bool hadHostilePawns;
        bool pulsing, absorbing;
        public MapComponent_MagicalCombat(Map map) : base(map)
        {
        }

        public void Register(CompMagicalCombatSource source)
        {
            sources.Add(source);
        }

        public void Unregister(CompMagicalCombatSource source)
        {
            sources.Remove(source);
        }

        public void Invalidate()
        {
            groups.Clear();
        }

        bool Live(CompMagicalCombatSource s) => s?.parent != null && s.parent.Map == map && s.CanOperate && (!(s is CompMagicalCombat main) || ValidProperties(main.Props));
        // Physical hit times also preserve the cooldown when migrating older saves.
        // Unknown/uninitialized pawn registries conservatively retain full upkeep.
        public bool IsPeaceMode(Faction owner)
        {
            if (owner == null || map.mapPawns == null) return false;
            int now = Find.TickManager.TicksGame;
            if (lastThreatTick >= 0 && (long)now - lastThreatTick < PeaceDelayTicks) return false;
            foreach (var source in sources)
                if (source?.parent != null && source.parent.Spawned && source.parent.Map == map && source.parent.Faction == owner && source.LastDamageTick >= 0 &&
                    (long)now - source.LastDamageTick < PeaceDelayTicks) return false;
            return !HasHostilePawns(owner);
        }
        bool HasHostilePawns(Faction owner)
        {
            if (map.mapPawns == null) return false;
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                if (pawn != null && pawn.Spawned && pawn.Map == map && !pawn.Dead && pawn.HostileTo(owner)) return true;
            return false;
        }
        void ObserveHostiles(int now)
        {
            var owner = Verse.Current.Game?.World?.factionManager?.AllFactionsListForReading.FirstOrDefault(f => f.IsPlayer)
                ?? sources.FirstOrDefault(s => s?.parent != null && s.parent.Spawned && s.parent.Map == map && s.parent.Faction?.IsPlayer == true)?.parent.Faction;
            if (owner == null) { hadHostilePawns = false; return; }
            if (map.mapPawns == null) return;
            bool present = HasHostilePawns(owner);
            // Start the hold when the last enemy disappears, not its previous observation.
            if (present || hadHostilePawns) lastThreatTick = now;
            hadHostilePawns = present;
        }
        static bool ValidProperties(CompProperties_MagicalCombat p) => CompMagofuelReservoir.PositiveFinite(p.regenerationPerSecond) && CompMagofuelReservoir.PositiveFinite(p.manaPerHitPoint) && CompMagofuelReservoir.PositiveFinite(p.manaPerCellSecond) && p.regenerationDelayTicks >= 0 && p.rebootTicks >= 0;
        public bool IsStandby(CompMagicalCombat source)
        {
            if (!Live(source))
                return false;
            var nodes = Nodes(source);
            if (nodes.Count == 0)
                return false;
            return sources.OfType<CompMagicalCombat>().Any(other => other != source && Live(other) && other.parent.thingIDNumber < source.parent.thingIDNumber && Nodes(other).Any(nodes.Contains));
        }

        List<CompManaBeacon> Nodes(CompMagicalCombatSource source)
        {
            var endpoint = source.parent.GetComp<CompManaNetworkEndpoint>();
            return endpoint == null ? new List<CompManaBeacon>() : map.GetComponent<MapComponent_ManaNetwork>().GetConnectedBeacons(endpoint);
        }

        bool Current()
        {
            if (!ModsConfig.OdysseyActive) return false;
            foreach (var source in snapshot)
                if (!Live(source) || !sources.Contains(source)) return false;
            int liveCount = 0;
            foreach (var source in sources)
                if (Live(source)) liveCount++;
            if (liveCount != snapshot.Count) return false;
            foreach (var placement in placements)
                if (!placement.Current) return false;
            return map.GetComponent<MapComponent_ManaNetwork>().HasSameLiveBeacons(beacons);
        }
        bool Valid()
        {
            int now = Find.TickManager.TicksGame;
            if (pulsing || !ModsConfig.OdysseyActive || now < lastPulse || (long)now - lastPulse >= Interval)
            {
                Invalidate();
                return false;
            }

            if (!Current())
            {
                // Validation never buys a replacement interval or regenerates HP.
                // Only the network whose paid receipt changed loses protection.
                groups.RemoveAll(g => !ReceiptCurrent(g));
                CaptureSnapshot();
            }
            return true;
        }

        void CaptureSnapshot()
        {
            snapshot = sources.Where(Live).OrderBy(s => s.parent.thingIDNumber).ToList();
            beacons = map.GetComponent<MapComponent_ManaNetwork>().GetLiveBeacons();
            placements = snapshot.Select(s => new Placement(s.parent)).Concat(beacons.Select(b => new Placement(b.parent))).ToList();
        }

        bool ReceiptCurrent(Group group)
        {
            if (group.Members.Any(s => !sources.Contains(s) || !Live(s)) || group.Placements.Any(p => !p.Current) || IsStandby(group.Main)) return false;
            var nodes = Nodes(group.Main);
            if (nodes.Count != group.Nodes.Count || !nodes.All(group.Nodes.Contains)) return false;
            var members = NetworkSources(group.Main).ToList();
            return members.Count == group.Members.Count() && members.All(group.Members.Contains);
        }

        bool Ready(Group group) => group.Main.CurrentHitPoints > 0f && Find.TickManager.TicksGame >= group.Main.RebootUntilTick;
        public bool Covers(Vector3 hit)
        {
            if (groups.Count == 0 || !Valid()) return false;
            foreach (var group in groups)
                if (Ready(group) && Inside(group, hit)) return true;
            return false;
        }
        public bool CanEmit(Thing emitter) => groups.Count > 0 && Valid() && groups.Any(g => Ready(g) && g.Emitters.Contains(emitter));
        Group FindGroup(CompMagicalCombat main) => groups.FirstOrDefault(g => g.Main == main);
        IEnumerable<CompMagicalCombatSource> NetworkSources(CompMagicalCombat main)
        {
            if (!Live(main) || IsStandby(main))
                return Enumerable.Empty<CompMagicalCombatSource>();
            var nodes = Nodes(main);
            return new CompMagicalCombatSource[]
            {
                main
            }.Concat(sources.OfType<CompMagicalCombatBooster>().Where(b => Live(b) && b.parent.Faction == main.parent.Faction && nodes.Count > 0 && Nodes(b).Any(nodes.Contains)));
        }

        public float HitPointsFor(CompMagicalCombat main) => NetworkSources(main).Sum(s => s.CurrentHitPoints);
        public float MaxHitPointsFor(CompMagicalCombat main) => NetworkSources(main).Sum(s => s.MaxHitPoints);
        public bool ShouldDrawEmitter(Thing emitter) => groups.Count > 0 && Valid() && groups.Any(g => Ready(g) && g.Emitters.Contains(emitter) && g.Emitters.Any(e => Find.Selector.IsSelected(e)));
        public bool TryAbsorb(CompMagicalCombat main, float damage, Vector3 hit)
        {
            if (absorbing || groups.Count == 0 || !CompMagofuelReservoir.PositiveFinite(damage) || !Valid())
                return false;
            var group = FindGroup(main);
            if (group == null || !Ready(group))
                return false;
            absorbing = true;
            try
            {
                int now = Find.TickManager.TicksGame;
                lastThreatTick = now;
                foreach (var source in group.Members)
                    source.NotifyHit(now);
                float remaining = damage;
                foreach (var booster in group.Boosters.OrderBy(b => HorizontalSquared(b.parent.Position.ToVector3Shifted(), hit)).ThenBy(b => b.parent.thingIDNumber))
                {
                    remaining -= booster.Spend(remaining);
                    if (remaining <= 0f)
                        break;
                }

                if (remaining > 0f)
                    main.Spend(remaining);
                if (main.CurrentHitPoints <= 0f)
                    main.Collapse(now);
                return true;
            }
            finally
            {
                absorbing = false;
            }
        }

        static float HorizontalSquared(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x, z = a.z - b.z;
            return x * x + z * z;
        }

        static bool Inside(Group g, Vector3 point)
        {
            foreach (var center in g.Centers)
                if (HorizontalSquared(center, point) <= Radius * Radius) return true;
            return false;
        }
        static bool Entry(Group g, Vector3 origin, Vector3 from, Vector3 to, out float fraction)
        {
            fraction = float.PositiveInfinity;
            if (Inside(g, origin) || Inside(g, from))
                return false;
            float dx = to.x - from.x, dz = to.z - from.z, a = dx * dx + dz * dz;
            if (!(a > 0f) || float.IsInfinity(a) || float.IsNaN(a))
                return false;
            foreach (var center in g.Centers)
            {
                float ox = from.x - center.x, oz = from.z - center.z, b = 2f * (ox * dx + oz * dz), c = ox * ox + oz * oz - Radius * Radius;
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f)
                    continue;
                float t = (-b - (float)Math.Sqrt(discriminant)) / (2f * a);
                if (t >= 0f && t <= 1f && t < fraction)
                    fraction = t;
            }

            return fraction <= 1f;
        }

        public bool CrossesBoundary(CompMagicalCombat main, Vector3 from, Vector3 to, Vector3 origin)
        {
            if (groups.Count == 0 || !Valid())
                return false;
            var group = FindGroup(main);
            return group != null && Ready(group) && Entry(group, origin, from, to, out _);
        }

        bool Receipt(object key)
        {
            int now = Find.TickManager.TicksGame;
            if (receiptTick != now)
            {
                receipts.Clear();
                receiptTick = now;
            }

            return key != null && receipts.ContainsKey(key);
        }

        public bool HasInterceptReceipt(object key) => Receipt(key);
        void Remember(object key)
        {
            if (key != null)
                receipts[key] = Find.TickManager.TicksGame;
        }

        // Adapter API: origin and hit must be coordinates on this map, including remote-map entry points.
        public bool TryInterceptIncoming(Thing launcher, Vector3 origin, Vector3 hit, float damage, object receiptKey = null)
        {
            if (Receipt(receiptKey))
                return true;
            if (groups.Count == 0 || !Valid())
                return false;
            var hits = new List<Tuple<Group, float>>();
            foreach (var g in groups)
                if (Ready(g) && launcher != null && launcher.HostileTo(g.Main.parent.Faction) && Entry(g, origin, origin, hit, out var t))
                    hits.Add(Tuple.Create(g, t));
            foreach (var item in hits.OrderBy(x => x.Item2).ThenBy(x => x.Item1.Main.parent.thingIDNumber))
                if (TryAbsorb(item.Item1.Main, damage, origin + (hit - origin) * item.Item2))
                {
                    Remember(receiptKey);
                    return true;
                }

            return false;
        }

        public bool TryInterceptOrbital(Thing launcher, Vector3 hit, float damage)
        {
            if (launcher == null || groups.Count == 0 || !Valid())
                return false;
            foreach (var group in groups.OrderBy(g => g.Main.parent.thingIDNumber))
                if (Ready(group) && Inside(group, hit) && launcher.HostileTo(group.Main.parent.Faction) && TryAbsorb(group.Main, damage, hit))
                    return true;
            return false;
        }

        public bool TryInterceptProjectile(Thing emitter, Projectile projectile, Vector3 origin, Vector3 from, Vector3 to)
        {
            var adapter = MagicalShieldProjectileAdapters.Find(projectile);
            if (adapter?.AlreadyIntercepted(projectile) == true) return true;
            if (Receipt(projectile))
                return true;
            if (groups.Count == 0 || !Valid() || projectile.Launcher == null)
                return false;
            foreach (var g in groups)
                if (g.Emitters.Contains(emitter) && Ready(g) && projectile.Launcher.HostileTo(g.Main.parent.Faction) && Entry(g, origin, from, to, out var t))
                {
                    var hit = from + (to - from) * t;
                    IMagicalShieldInterception landing = null;
                    float damage = projectile.DamageAmount;
                    if (adapter != null)
                    {
                        landing = adapter.Plan(projectile, this, hit, groups.Where(Ready).SelectMany(group => group.Centers).Distinct().ToList());
                        if (landing == null || !landing.CanCommit) return false;
                        damage = landing.Damage;
                    }
                    if (!TryAbsorb(g.Main, damage, hit)) return false;
                    landing?.Commit();
                    Remember(projectile);
                    return true;
                }

            return false;
        }

        public bool TryInterceptBombardment(Thing emitter, Thing launcher, IntVec3 target, float damage, object receiptKey)
        {
            if (Receipt(receiptKey))
                return true;
            if (groups.Count == 0 || !Valid() || launcher == null)
                return false;
            foreach (var g in groups)
                if (g.Emitters.Contains(emitter) && Ready(g) && launcher.HostileTo(g.Main.parent.Faction) && Inside(g, target.ToVector3Shifted()) && TryAbsorb(g.Main, damage, target.ToVector3Shifted()))
                {
                    Remember(receiptKey);
                    return true;
                }

            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPulse, "TM_combatLastPulse", -1);
            Scribe_Values.Look(ref lastThreatTick, "TM_combatLastThreat", -1);
            Scribe_Values.Look(ref hadHostilePawns, "TM_combatHadHostiles", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Invalidate();
                receipts.Clear();
                pulsing = false;
                absorbing = false;
            }
        }

        public override void MapComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (pulsing || now < 60 || (lastPulse >= 0 && (long)now - lastPulse < Interval))
                return;
            lastPulse = now;
            Invalidate();
            if (!ModsConfig.OdysseyActive)
                return;
            ObserveHostiles(now);
            pulsing = true;
            try
            {
                CaptureSnapshot();
                var elected = new HashSet<Thing>();
                foreach (var main in snapshot.OfType<CompMagicalCombat>())
                {
                    var nodes = Nodes(main);
                    Thing key = nodes.Count == 0 ? main.parent : nodes.OrderBy(n => n.parent.thingIDNumber).First().parent;
                    if (!elected.Add(key) || !main.BeginService(now) || now < main.RebootUntilTick)
                        continue;
                    var g = new Group(main, nodes);
                    foreach (var booster in snapshot.OfType<CompMagicalCombatBooster>())
                        if (nodes.Count > 0 && booster.parent.Faction == main.parent.Faction && Nodes(booster).Any(nodes.Contains))
                            g.Boosters.Add(booster);
                    var bank = main.parent.GetComp<CompShieldManaBuffer>();
                    if (bank == null)
                        continue;
                    float upkeep = g.Cells(map).Count * main.Props.manaPerCellSecond * (IsPeaceMode(main.parent.Faction) ? .1f : 1f);
                    if (!Current())
                    {
                        Invalidate();
                        return;
                    }

                    float paid = bank.WithdrawMana(upkeep);
                    if (!Current())
                    {
                        Invalidate();
                        return;
                    }

                    if (paid < upkeep)
                        continue;
                    int latestDamage = g.Members.Max(s => s.LastDamageTick);
                    if (latestDamage < 0 || (long)now - latestDamage >= main.Props.regenerationDelayTicks)
                    {
                        float budget = main.Props.regenerationPerSecond;
                        foreach (var member in g.Members)
                        {
                            if (!member.MayRegenerate(now))
                                continue;
                            member.MarkRegeneration(now);
                            float demand = Math.Min(budget, Math.Max(0f, member.MaxHitPoints - member.CurrentHitPoints));
                            if (demand <= 0f)
                                continue;
                            float mana = bank.WithdrawMana(demand * main.Props.manaPerHitPoint);
                            if (!Current())
                            {
                                Invalidate();
                                return;
                            }

                            float hp = Math.Min(demand, mana / main.Props.manaPerHitPoint);
                            member.Restore(hp);
                            budget -= hp;
                            if (budget <= 0f)
                                break;
                        }
                    }

                    if (!Current())
                    {
                        Invalidate();
                        return;
                    }

                    g.Placements = g.Members.Select(s => new Placement(s.parent)).Concat(nodes.Select(n => new Placement(n.parent))).ToList();
                    groups.Add(g);
                    if (Ready(g))
                        foreach (var member in g.Members)
                            member.parent.GetComp<CompCrystalGlow>()?.NotifyService(now, Interval);
                }
            }
            catch
            {
                Invalidate();
                throw;
            }
            finally
            {
                pulsing = false;
            }
        }

        sealed class Group
        {
            public readonly CompMagicalCombat Main;
            public readonly List<CompMagicalCombatBooster> Boosters = new List<CompMagicalCombatBooster>();
            public readonly List<Thing> Emitters;
            public readonly List<Vector3> Centers;
            public readonly List<CompManaBeacon> Nodes;
            public List<Placement> Placements;
            public IEnumerable<CompMagicalCombatSource> Members => new CompMagicalCombatSource[]
            {
                Main
            }.Concat(Boosters);

            public Group(CompMagicalCombat main, List<CompManaBeacon> nodes)
            {
                Main = main;
                Nodes = nodes;
                Emitters = new List<Thing>
                {
                    main.parent
                };
                Emitters.AddRange(nodes.Select(b => b.parent));
                Centers = Emitters.Select(e => e.Position.ToVector3Shifted()).ToList();
            }

            public HashSet<IntVec3> Cells(Map map)
            {
                var cells = new HashSet<IntVec3>();
                foreach (var emitter in Emitters)
                    for (int x = -5; x <= 5; x++)
                        for (int z = -5; z <= 5; z++)
                            if (x * x + z * z <= 25)
                            {
                                var c = emitter.Position + new IntVec3(x, 0, z);
                                if (c.InBounds(map))
                                    cells.Add(c);
                            }

                return cells;
            }
        }

        sealed class Placement
        {
            readonly Thing thing;
            readonly Map map;
            readonly Faction faction;
            readonly IntVec3 position;
            readonly Rot4 rotation;
            readonly ManaNetworkMode manaMode, fuelMode;
            public Placement(Thing thing)
            {
                this.thing = thing;
                map = thing.Map;
                faction = thing.Faction;
                position = thing.Position;
                rotation = thing.Rotation;
                var ep = thing.TryGetComp<CompManaNetworkEndpoint>();
                if (ep != null)
                {
                    manaMode = ep.ManaMode;
                    fuelMode = ep.FuelMode;
                }
            }

            public bool Current
            {
                get
                {
                    if (!thing.Spawned || thing.Destroyed || thing.Map != map || thing.Faction != faction || thing.Position != position || thing.Rotation != rotation)
                        return false;
                    var ep = thing.TryGetComp<CompManaNetworkEndpoint>();
                    return ep == null || (ep.ManaMode == manaMode && ep.FuelMode == fuelMode);
                }
            }
        }
    }
}
