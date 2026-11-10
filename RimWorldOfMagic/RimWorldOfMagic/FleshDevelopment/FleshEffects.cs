using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    // Flecks are transient visuals, not Things: no gameplay IDs, hediffs or save data.
    public static class FleshEffects
    {
        public static bool CanDraw(Pawn pawn) => pawn != null && pawn.Spawned && pawn.Map?.flecks != null;
        public static bool PulseDue(int previousTick, int now, int pawnId)
        {
            int offset = (pawnId & int.MaxValue) % 90;
            return now > previousTick && (now + offset) / 90 != (previousTick + offset) / 90;
        }
        public static void Preparing(Pawn pawn, FleshSkill skill)
        {
            if (JobDriver_FleshCast.PreparationTicks(skill) == 0 || !CanDraw(pawn) || (FleshActions.Now + (pawn.thingIDNumber & int.MaxValue) % 30) % 30 != 0) return;
            // A small gathering pulse, separate from the larger success flash.
            Emit(pawn.Map, pawn.DrawPos, skill == FleshSkill.Harden ? "TM_FleshHardenFX" : "TM_FleshSiphonFX", .65f);
        }
        public static void Activated(Pawn pawn, FleshSkill skill)
        {
            if (!CanDraw(pawn)) return;
            string def = skill == FleshSkill.Restore ? "TM_FleshRegenFX" : skill == FleshSkill.Empower ? "TM_FleshEmpowerFX"
                : skill == FleshSkill.Harden ? "TM_FleshHardenFX" : skill == FleshSkill.Feast ? "TM_FleshFeastFX" : null;
            if (def == null) return;
            Emit(pawn.Map, pawn.DrawPos, def, skill == FleshSkill.Feast ? 2.5f : 1.8f);
            if (skill == FleshSkill.Restore || skill == FleshSkill.Harden)
                for (int i = 0; i < 4; i++) Emit(pawn.Map, pawn.DrawPos, def, .65f, i * 90f, .7f);
        }
        public static void Tick(Pawn pawn, FleshProgress p, int previousTick, int now)
        {
            if (!PulseDue(previousTick, now, pawn?.thingIDNumber ?? 0) || !CanDraw(pawn)) return;
            // At most three flecks per pawn per 90 ticks, staggered by pawn ID; no catch-up loops.
            if (p.EmpowerUntil > now) Emit(pawn.Map, pawn.DrawPos, "TM_FleshEmpowerFX", 1.25f);
            if (p.HardenUntil > now) Emit(pawn.Map, pawn.DrawPos, "TM_FleshHardenFX", 1.3f);
            if (p.FeastUntil > now || p.FeastRage) Emit(pawn.Map, pawn.DrawPos, "TM_FleshFeastFX", 1.8f);
        }
        // Capture the impact position BEFORE lethal damage / corpse destruction unspawns the target.
        public static void Consumed(Pawn actor, Map map, Vector3 position)
        {
            if (!CanDraw(actor) || map != actor.Map) return;
            Emit(map, position, "TM_FleshBloodFX", 1.3f);
            Vector3 toActor = actor.DrawPos - position;
            float distance = toActor.MagnitudeHorizontal();
            Emit(map, position, "TM_FleshSiphonFX", .9f, toActor.AngleFlat(), distance / .65f);
            Emit(map, actor.DrawPos, "TM_FleshRegenFX", .7f);
        }
        private static void Emit(Map map, Vector3 position, string name, float scale, float angle = 0f, float speed = 0f)
        {
            // Check map before any camera/mote API: world pawns also tick undead hediffs.
            if (map?.flecks == null || !position.ToIntVec3().InBounds(map) || !position.ShouldSpawnMotesAt(map, false)) return;
            var def = DefDatabase<FleckDef>.GetNamedSilentFail(name);
            if (def == null) return;
            var data = FleckMaker.GetDataStatic(position, map, def, scale);
            data.velocityAngle = angle;
            data.velocitySpeed = speed;
            data.rotationRate = name == "TM_FleshEmpowerFX" ? 55f : 0f;
            Submit(map, data);
        }
        private static void Submit(Map map, FleckCreationData data)
        {
            // The renderer may randomize fleck setup. Local visibility must never advance gameplay RNG.
            Rand.PushState();
            try { map.flecks.CreateFleck(data); }
            finally { Rand.PopState(); }
        }
    }
}
