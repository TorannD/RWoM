using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic.AutoCast;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class Verb_RangerSafeTrail : Verb_UseAbility
    {
        internal const string Order="TM_SpiritCastSafeTrail";
        internal static Verb_RangerSafeTrail For(Pawn body,MightAbility power) =>
            new Verb_RangerSafeTrail{caster=body,Ability=power,verbProps=power.mightDef.MainVerb};
        internal static void Choose(Pawn body,MightAbility power)
        {
            var verb=For(body,power);
            var map=body.Map;
            bool Valid(LocalTargetInfo t)=>body.Map==map && SpiritHostedMartial.Find(body,power.Pawn,Order)==power &&
                SpiritHostedMartial.Ready(body,power,Order) && verb.CanHitTargetFrom(body.Position,t);
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,t=>{
                if(Valid(t))body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(Order),power.Pawn,t));
            },highlightAction:verb.DrawHighlight,targetValidator:Valid,caster:body);
        }
        internal static bool Open(Pawn ranger, IntVec3 cell) =>
            cell.InBounds(ranger.Map) && !cell.Fogged(ranger.Map) && cell.StandableBy(ranger.Map,ranger) &&
            (cell.GetDoor(ranger.Map) is not Building_Door door || door.Open);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var ranger = CasterPawn;
            var source=Ability?.Pawn??ranger;
            if (ranger?.Spawned != true || ranger.Dead || ranger.Downed || !RangerMastery.HasTrail(source) ||
                (source!=ranger && RangerCareer.CareBody(source)!=ranger) ||
                target.HasThing || !target.IsValid || root == target.Cell || !root.InBounds(ranger.Map) ||
                (target.Cell-root).LengthManhattan > 8 || !target.Cell.InBounds(ranger.Map) ||
                !GenSight.LineOfSight(root,target.Cell,ranger.Map)) return false;
            return GenSight.PointsOnLineOfSight(root,target.Cell).All(cell => Open(ranger,cell));
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            var actor = CasterPawn;
            if (actor?.Spawned != true || !target.IsValid || !target.Cell.InBounds(actor.Map) ||
                (target.Cell-actor.Position).LengthManhattan > 8) return;
            GenDraw.DrawFieldEdges(GenSight.PointsOnLineOfSight(actor.Position,target.Cell).ToList(),
                CanHitTargetFrom(actor.Position,target) ? RangerSafeTrails.Tint : Color.red);
        }
        protected override bool TryCastShot() => MarkTrail(currentTarget);
        internal bool MarkTrail(LocalTargetInfo target)
        {
            var ranger = CasterPawn;
            if (ranger == null || !CanHitTargetFrom(ranger.Position,target)) return false;
            ranger.Map.GetComponent<RangerSafeTrails>().Create(ranger,Ability?.Pawn??ranger,target.Cell);
            return true;
        }
    }

    public sealed class RangerSafeTrails : MapComponent
    {
        internal static readonly Color Tint = new Color(.33f,.84f,.58f,.75f);
        private static readonly ConditionalWeakTable<Map,RangerSafeTrails> active = new();
        private List<Trail> trails = new();
        public RangerSafeTrails(Map map) : base(map) { }

        internal void Create(Pawn body,Pawn owner,IntVec3 end)
        {
            trails.RemoveAll(route=>route.Source==owner);
            var trail = new Trail(body,owner,end);
            trails.Add(trail);
            active.GetValue(map,_=>this);
            trail.Observe();
        }
        public override void MapComponentTick()
        {
            if (trails.Count == 0) return;
            for (int i=trails.Count-1;i>=0;i--)
            {
                var trail = trails[i];
                if (!trail.Live || !trail.Traversable()) trails.RemoveAt(i);
                else trail.Observe();
            }
            if (trails.Count == 0) active.Remove(map);
        }
        public override void MapComponentDraw()
        {
            if (trails.Count == 0 || map != Find.CurrentMap) return;
            foreach (var trail in trails) if (trail.Live) GenDraw.DrawFieldEdges(trail.Cells,Tint);
        }
        internal static float Speed(Pawn pawn)
        {
            if (pawn?.Spawned == true && active.TryGetValue(pawn.Map,out var comp))
                foreach (var trail in comp.trails)
                    if (trail.Live && trail.Boosts(pawn)) return 1.15f;
            return 1f;
        }
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && trails.Count == 0) return;
            Scribe_Collections.Look(ref trails,"TM_rangerTrails",LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                trails ??= new List<Trail>();
                trails.RemoveAll(t=>t?.Caster == null);
                if (trails.Count != 0) active.GetValue(map,_=>this);
            }
        }
        public override void MapRemoved() => active.Remove(map);

        private sealed class Trail : IExposable
        {
            public Pawn Caster;
            private Pawn owner;
            private bool projected;
            internal Pawn Source => projected ? owner : Caster;
            public List<IntVec3> Cells;
            private int expires;
            private int mapId;
            private List<Traveler> travelers = new();
            public Trail() { }
            public Trail(Pawn caster,Pawn source,IntVec3 end)
            {
                Caster = caster;
                projected=source!=caster;
                if(projected)owner=source;
                Cells = GenSight.PointsOnLineOfSight(caster.Position,end).ToList();
                mapId = caster.Map.uniqueID;
                expires = Find.TickManager.TicksGame + 480;
            }
            internal bool Live => Caster?.Spawned == true && !Caster.Dead && Caster.Map.uniqueID == mapId &&
                Find.TickManager.TicksGame < expires && RangerMastery.HasTrail(Source) &&
                (!projected || RangerCareer.CareBody(owner)==Caster);
            internal bool Traversable() => Cells.All(cell=>Verb_RangerSafeTrail.Open(Caster,cell));
            private bool Ally(Pawn p) => p != null && p != Caster && p.Spawned && !p.Dead && !p.Downed && !p.IsPrisoner &&
                p.Map == Caster.Map && p.Faction != null && p.Faction == Caster.Faction && !p.HostileTo(Caster);
            internal bool Boosts(Pawn pawn) => Ally(pawn) && Cells.Contains(pawn.Position) && travelers.Any(t=>t.Ally == pawn);
            internal void Observe()
            {
                if (travelers.Count < 2)
                    foreach (var cell in Cells)
                    {
                        var things = cell.GetThingList(Caster.Map);
                        for (int i=0; i<things.Count && travelers.Count<2; i++)
                            if (things[i] is Pawn p && Ally(p) && !travelers.Any(t=>t.Ally == p))
                                travelers.Add(new Traveler(p));
                        if (travelers.Count == 2) break;
                    }
                foreach (var traveler in travelers) traveler.Check(Caster,Source,Cells);
            }
            public void ExposeData()
            {
                Scribe_References.Look(ref Caster,"caster");
                Scribe_Values.Look(ref projected,"projected");
                if(projected)Scribe_References.Look(ref owner,"ranger");
                Scribe_Values.Look(ref expires,"expires"); Scribe_Values.Look(ref mapId,"mapId");
                Scribe_Collections.Look(ref Cells,"cells",LookMode.Value);
                Scribe_Collections.Look(ref travelers,"travelers",LookMode.Deep);
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    Cells ??= new List<IntVec3>(); travelers ??= new List<Traveler>();
                    travelers.RemoveAll(t=>t?.Ally == null);
                }
            }
        }

        private sealed class Traveler : IExposable
        {
            public Pawn Ally;
            private Pawn threat;
            private IntVec3 entry;
            private IntVec3 previous;
            private bool closed;
            public Traveler() { }
            public Traveler(Pawn pawn)
            {
                Ally=pawn; entry=previous=pawn.Position;
                int best=int.MaxValue;
                foreach(var enemy in pawn.Map.mapPawns.AllPawnsSpawned)
                {
                    int distance=entry.DistanceToSquared(enemy.Position);
                    if(distance <= 18*18 && distance < best && TM_AutocastCombat.IsActiveOpponent(pawn,enemy) &&
                        GenSight.LineOfSight(entry,enemy.Position,pawn.Map))
                    { threat=enemy;best=distance; }
                }
            }
            internal void Check(Pawn ranger,Pawn owner,List<IntVec3> cells)
            {
                if (closed || threat == null) return;
                if (Ally?.Spawned != true || Ally.Dead || Ally.Downed || Ally.Map != ranger.Map ||
                    Ally.Faction != ranger.Faction || !cells.Contains(Ally.Position) ||
                    !previous.AdjacentTo8WayOrInside(Ally.Position))
                { closed=true;return; }
                if (previous == Ally.Position) return;
                if (Ally.pather == null || Find.TickManager.TicksGame - Ally.pather.LastMovedTick > 1 ||
                    !TM_AutocastCombat.IsActiveOpponent(Ally,threat))
                { closed=true;return; }
                previous=Ally.Position;
                // Compare both positions against the SAME current threat position; an enemy backing
                // away by itself cannot satisfy the retreat.
                if ((entry-Ally.Position).LengthManhattan >= 2 && Ally.Position.DistanceToSquared(threat.Position) >
                    entry.DistanceToSquared(threat.Position) && GenSight.LineOfSight(Ally.Position,threat.Position,ranger.Map))
                { closed=true; RangerAscension.RetreatFrom(ranger,owner,Ally); }
            }
            public void ExposeData()
            {
                Scribe_References.Look(ref Ally,"ally");Scribe_References.Look(ref threat,"threat");
                Scribe_Values.Look(ref entry,"entry");Scribe_Values.Look(ref previous,"previous");
                Scribe_Values.Look(ref closed,"closed");
            }
        }
    }
}
