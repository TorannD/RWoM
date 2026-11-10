using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace TorannMagic.SpellItems
{
    public static class SpellItemCasting
    {
        public static CompSpellItem Comp(Thing thing)
        {
            ThingWithComps item = thing as ThingWithComps;
            if (item == null) return null;
            for (int i = 0; i < item.AllComps.Count; i++)
                if (item.AllComps[i] is CompSpellItem comp) return comp;
            return null;
        }

        public static IEnumerable<CompSpellItem> Sources(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer != null)
                foreach (Thing item in pawn.inventory.innerContainer)
                {
                    CompSpellItem comp = Comp(item);
                    if (comp != null && !comp.Props.requiresEquipped) yield return comp;
                }
            CompSpellItem weapon = Comp(pawn?.equipment?.Primary);
            if (weapon != null && weapon.Props.requiresEquipped) yield return weapon;
        }

        public static bool TryStart(Pawn pawn, Thing source, LocalTargetInfo target)
            => Submit(pawn, source, target, GlobalTargetInfo.Invalid);

        public static bool TryStartPortal(Pawn pawn, Thing source, LocalTargetInfo target, GlobalTargetInfo destination)
        {
            if (Comp(source)?.Props.spell != SpellItemKind.Portal || !SpellItemPortal.ValidDestination(pawn?.Map, target.Cell, destination)) return false;
            return Submit(pawn, source, target, destination);
        }

        private static bool Submit(Pawn pawn, Thing source, LocalTargetInfo target, GlobalTargetInfo destination)
        {
            CompSpellItem item = Comp(source);
            if (item == null || item.UseRejection(pawn) != null
                || SpellItemEffects.TargetRejection(pawn, target, item.Props) != null || pawn.jobs == null)
                return false;
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("TM_SpellItemCast");
            if (jobDef == null || pawn.CurJob?.def == jobDef) return false;
            if (item.Props.spell == SpellItemKind.Portal && !SpellItemPortal.ValidDestination(pawn.Map, target.Cell, destination)) return false;
            Job job = JobMaker.MakeJob(jobDef, source, target, pawn);
            job.globalTarget = destination; // Native Job Scribe and queue cloning preserve the second endpoint.
            job.count = 0;
            // Queued jobs can be discarded without driver cleanup. Reserve only
            // when the warmup toil actually starts, not when submitting the order.
            if (item.Props.spell == SpellItemKind.Blink)
            {
                Map map = pawn.Map;
                return Odyssey.ManualTravelSafety.Submit(pawn, job, map, target.Cell, true,
                    () => pawn.Spawned && pawn.Map == map && item.UseRejection(pawn) == null
                        && SpellItemEffects.TargetRejection(pawn, target, item.Props) == null,
                    () => pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc));
            }
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
