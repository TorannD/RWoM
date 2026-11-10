using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthRitualJob
    {
        internal const string DefinitionName = "TM_SpiritRebirth";
        internal static IEnumerable<FloatMenuOption> Options(Pawn creator, Building_TMMagicCircle circle)
        {
            if (SpiritRebirthRitualEligibility.CreatorRejection(creator) != SpiritRebirthRitualRejection.None ||
                circle?.Spawned != true || circle.Map != creator.Map || circle.Faction != creator.Faction) yield break;
            var pending = creator.Map.mapPawns.AllPawnsSpawned.AsEnumerable();
            if (creator.carryTracker?.CarriedThing is Pawn carried) pending = pending.Append(carried);
            if (creator.inventory != null) pending = pending.Concat(creator.inventory.innerContainer.OfType<Pawn>());
            foreach (var original in pending.Distinct())
            {
                if (ClassIdentity.Component(original)?.SpiritRitual?.RecoveryCircle != circle ||
                    SpiritRebirthRecoveryJob.Create(creator, original) == null) continue;
                yield return new FloatMenuOption("TM_SpiritFinishRitual".Translate(original.LabelShortCap), () => {
                    var job = SpiritRebirthRecoveryJob.Create(creator, original);
                    if (job != null) creator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
            foreach (var corpse in creator.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
            {
                if (Create(creator,circle,corpse) == null) continue;
                var label = "TM_SpiritBeginRitual".Translate(corpse.InnerPawn.LabelShortCap);
                if (!SpiritRebirthHealthPlan.TryPrepare(corpse.InnerPawn, out _, out var unsupported))
                {
                    yield return new FloatMenuOption(unsupported?.def != null
                        ? "TM_SpiritRitualUnsafeEffect".Translate(label, unsupported.def.LabelCap).ToString() : label.ToString(), null);
                    continue;
                }
                var original = corpse.InnerPawn;
                void Order(bool confirmed)
                {
                    if (corpse.InnerPawn != original) return;
                    var job = Create(creator,circle,corpse);
                    if (job != null && confirmed) SpiritNecromancyRenunciation.Confirm(job, original);
                    if (job != null && SpiritRebirthHealthPlan.TryPrepare(original,out _,out _) &&
                        SpiritRebirthStaging.Discover(creator,job,circle,corpse) != null)
                        creator.jobs.TryTakeOrderedJob(job,JobTag.Misc);
                    else Messages.Message("TM_SpiritRitualUnreachable".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                yield return new FloatMenuOption(label, () => {
                    if (SpiritNecromancyRenunciation.Required(original))
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "TM_SpiritRitualRenounceNecromancy".Translate(original.LabelShortCap), () => Order(true)));
                    else Order(false);
                });
            }
        }
        internal static Job Create(Pawn creator, Building_TMMagicCircle circle, Corpse corpse)
        {
            var def = DefDatabase<JobDef>.GetNamedSilentFail(DefinitionName);
            if (def?.driverClass != typeof(JobDriver_SpiritRebirth) || creator?.carryTracker?.CarriedThing != null
                || SpiritRebirthRitualEligibility.Rejection(creator, circle, corpse, corpse?.InnerPawn) != SpiritRebirthRitualRejection.None)
                return null;
            return JobMaker.MakeJob(def, circle, corpse);
        }
    }
}
