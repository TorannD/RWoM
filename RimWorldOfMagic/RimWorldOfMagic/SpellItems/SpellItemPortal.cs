using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace TorannMagic.SpellItems
{
    public static class SpellItemPortal
    {
        public static bool ValidCell(Map map, IntVec3 cell) => map != null && cell.InBounds(map)
            && !cell.Fogged(map) && cell.Standable(map) && cell.GetEdifice(map) == null
            && !cell.GetThingList(map).Any(t => t is Pawn || t is Building_SpellItemPortal);

        public static bool ValidDestination(Map originMap, IntVec3 origin, GlobalTargetInfo target)
            => originMap != null && target.IsValid && target.Map != null && Find.Maps.Contains(target.Map)
                && (target.Map != originMap || (origin - target.Cell).LengthHorizontal >= 3)
                && ValidCell(target.Map, target.Cell);

        public static bool TryCreate(Pawn caster, IntVec3 origin, GlobalTargetInfo destination)
        {
            if (!ValidCell(caster.Map, origin) || !ValidDestination(caster.Map, origin, destination)) return false;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_ItemPortal");
            if (def?.thingClass != typeof(Building_SpellItemPortal)) return false;
            Building_SpellItemPortal a = null, b = null;
            try
            {
                a = (Building_SpellItemPortal)ThingMaker.MakeThing(def);
                b = (Building_SpellItemPortal)ThingMaker.MakeThing(def);
                a.Partner = b; b.Partner = a;
                a.ExpiresAt = b.ExpiresAt = Find.TickManager.TicksGame + 60000;
                a.SetFaction(caster.Faction); b.SetFaction(caster.Faction);
                GenSpawn.Spawn(a, origin, caster.Map);
                GenSpawn.Spawn(b, destination.Cell, destination.Map);
            }
            catch (Exception e) { Compatibility.TM_Log.Warning("SpellItems", "Item portal creation interrupted: " + e); }
            if (a?.Spawned == true && b?.Spawned == true) return true;
            // A partial pair is not an effect. Remove it before allowing a fresh cast.
            if (a != null && !a.Destroyed) a.Destroy();
            if (b != null && !b.Destroyed) b.Destroy();
            return false;
        }
    }

    public class Building_SpellItemPortal : Building
    {
        public Building_SpellItemPortal Partner;
        public int ExpiresAt;
        public bool Active => Spawned && Partner?.Spawned == true && Find.TickManager.TicksGame < ExpiresAt;

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame >= ExpiresAt || Partner == null || Partner.Destroyed) Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Partner, "itemPortalPartner");
            Scribe_Values.Look(ref ExpiresAt, "itemPortalExpires");
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            foreach (var option in base.GetFloatMenuOptions(pawn)) yield return option;
            if (pawn.Faction?.IsPlayer != true || !Active) yield break;
            if (!pawn.CanReach(this, PathEndMode.OnCell, Danger.Deadly)) yield break;
            yield return new FloatMenuOption("TM_SpellItem_EnterPortal".Translate(), () =>
            {
                if (!Active) return;
                Map destination = Partner.Map; IntVec3 cell = Partner.Position;
                var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_ItemPortalTravel"), this);
                Odyssey.ManualTravelSafety.Submit(pawn, job, destination, cell, true,
                    () => Active && pawn.Spawned && pawn.Map == Map && Partner.Map == destination && Partner.Position == cell,
                    () => pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc));
            });
        }

        public bool TryTravel(Pawn pawn)
        {
            if (!Active || pawn?.Map != Map || pawn.Position != Position || pawn.Dead || pawn.Downed
                || pawn.Faction?.IsPlayer != true) return false;
            Map destination = Partner.Map;
            IntVec3 cell = Partner.Position;
            if (!cell.Standable(destination) || cell.GetThingList(destination).Any(t => t is Pawn)) return false;
            if (!Odyssey.ManualTravelSafety.CanComplete(pawn, destination, cell)) return false;
            Map original = pawn.Map; IntVec3 start = pawn.Position;
            try
            {
                if (original == destination) { pawn.Position = cell; pawn.Notify_Teleported(endCurrentJob: false); }
                else { pawn.DeSpawn(); GenSpawn.Spawn(pawn, cell, destination); }
                return pawn.Spawned && pawn.Map == destination && pawn.Position == cell;
            }
            catch (Exception e)
            {
                Compatibility.TM_Log.Error("SpellItems", "Portal travel interrupted: " + e);
                if (!pawn.Spawned && pawn.holdingOwner == null) GenSpawn.Spawn(pawn, start, original);
                return pawn.Spawned && pawn.Map == destination && pawn.Position == cell;
            }
        }
    }

    public class JobDriver_SpellItemPortalTravel : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !(job.targetA.Thing is Building_SpellItemPortal portal) || !portal.Active);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            yield return Toils_General.Do(() => ((Building_SpellItemPortal)job.targetA.Thing).TryTravel(pawn));
        }
    }
}
