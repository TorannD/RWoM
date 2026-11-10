using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    // A successful delivery can involve a split stack, so track how much left the
    // worker's actual carry container during the native deposit toil.
    internal static class CommanderWorkReceipts
    {
        private sealed class Disaster { }
        private static readonly ConditionalWeakTable<Building,Disaster> damaged = new();

        [HarmonyPatch(typeof(Thing),nameof(Thing.PostApplyDamage))]
        private static class DisasterDamage
        {
            private static void Postfix(Thing __instance, DamageInfo dinfo, float totalDamageDealt)
            {
                if (!ClassProgressionFeature.Enabled || totalDamageDealt<=0 || __instance is not Building building ||
                    building.Destroyed || building.Faction?.IsPlayer!=true) return;
                bool disaster=dinfo.Instigator?.HostileTo(building)==true ||
                    dinfo.Def==DamageDefOf.Flame && dinfo.Instigator is Fire fire && fire.instigator?.Faction?.IsPlayer!=true;
                if(disaster) damaged.GetValue(building,_=>new Disaster());
            }
        }
        [HarmonyPatch(typeof(Thing),nameof(Thing.ExposeData))]
        private static class SaveDisaster
        {
            private static void Postfix(Thing __instance)
            {
                if (__instance is not Building building || (Scribe.mode!=LoadSaveMode.Saving && Scribe.mode!=LoadSaveMode.LoadingVars)) return;
                bool has=damaged.TryGetValue(building,out _);
                if(Scribe.mode==LoadSaveMode.Saving && !has) return;
                Scribe_Values.Look(ref has,"rwomCommanderDisasterDamage");
                if(Scribe.mode==LoadSaveMode.LoadingVars && has) damaged.GetValue(building,_=>new Disaster());
            }
        }

        // The completion tick raises HP before the repair job terminates. Keep the
        // provenance on the order itself, then expire the building mark at full repair.
        internal static bool WasDisaster(Thing thing) => thing is Building b && damaged.TryGetValue(b,out _);
        [HarmonyPatch(typeof(Thing),nameof(Thing.HitPoints),MethodType.Setter)]
        private static class FullRepair
        {
            private static void Postfix(Thing __instance)
            {
                if (__instance is Building b && damaged.TryGetValue(b,out _) && b.HitPoints>=b.MaxHitPoints) damaged.Remove(b);
            }
        }

        private static void Wrap(Toil deposit,TargetIndex destination,bool toContainer)
        {
            var native=deposit.initAction;
            if(native==null || !ClassProgressionFeature.Enabled) return;
            deposit.initAction=() =>
            {
                var worker=deposit.actor;
                var effect=CommanderOrderEffects.Current(worker);
                var job=worker.CurJob;
                var cargo=worker.carryTracker.CarriedThing;
                bool crew=effect?.Order==CommanderOrder.Crew && !effect.Credited && effect.JobId==job?.loadID;
                var relief=PsionicService.Relief(worker,cargo);
                bool valid=(crew || relief!=null) && cargo!=null && CommanderOrders.SupplyJob(worker.jobs.curDriver);
                var dest=valid ? job.GetTarget(destination) : LocalTargetInfo.Invalid;
                int Count() => toContainer ? dest.Thing?.TryGetInnerInteractableThingOwner()?.TotalStackCountOfDef(cargo.def) ?? 0
                    : dest.Cell.GetThingList(worker.Map).Where(t=>t.def==cargo.def).Sum(t=>t.stackCount);
                valid = valid && (toContainer ? dest.Thing?.def.category==ThingCategory.Building && dest.Thing.TryGetInnerInteractableThingOwner()!=null
                    : dest.Cell.InBounds(worker.Map) && dest.Cell.GetSlotGroup(worker.Map)?.Settings.AllowedToAccept(cargo)==true);
                int before=valid?Count():0;
                native();
                if(valid && Count()>before)
                {
                    if(relief!=null && PsionicMartialPath.ActivePath(worker,out _)==relief) PsionicMartialPath.RecordService(relief,relief:true);
                    if(!crew)return;
                    effect.Credited=true;
                    CommanderMartialPath.WorkDone(effect.Source,effect.Serial,true,false,false);
                }
            };
        }
        [HarmonyPatch(typeof(Toils_Haul),nameof(Toils_Haul.PlaceHauledThingInCell))]
        private static class DeliveredToCell { private static void Postfix(Toil __result,TargetIndex cellInd)=>Wrap(__result,cellInd,false); }
        [HarmonyPatch(typeof(Toils_Haul),nameof(Toils_Haul.DepositHauledThingInContainer))]
        private static class DeliveredToContainer { private static void Postfix(Toil __result,TargetIndex containerInd)=>Wrap(__result,containerInd,true); }

        [HarmonyPatch(typeof(Pawn_JobTracker),"CleanupCurrentJob")]
        private static class JobEnded
        {
            private static void Prefix(Pawn ___pawn, JobCondition condition)
            {
                if(condition!=JobCondition.Succeeded) return;
                var order=CommanderOrderEffects.Current(___pawn);
                if(order?.Order!=CommanderOrder.Crew || order.Credited || ___pawn.CurJob?.loadID!=order.JobId) return;
                var driver=___pawn.jobs.curDriver;
                var target=___pawn.CurJob.targetA.Thing;
                bool repair=driver is JobDriver_Repair && target is Building building &&
                    building.Faction==___pawn.Faction && order.DisasterRepair && order.InitialHP>0 && order.InitialHP<building.MaxHitPoints &&
                    building.HitPoints==building.MaxHitPoints;
                bool rescue=___pawn.CurJob.def==JobDefOf.Rescue && target is Pawn casualty && casualty.Downed && !casualty.Dead &&
                    casualty.Faction==___pawn.Faction && casualty.CurrentBed()!=null && casualty.CurrentBed()==___pawn.CurJob.targetB.Thing;
                if(!repair && !rescue) return;
                order.Credited=true;
                CommanderMartialPath.WorkDone(order.Source,order.Serial,false,repair,repair || rescue);
            }
        }
    }
}
