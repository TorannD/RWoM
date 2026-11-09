using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class BladeMedicalSupply
    {
        private sealed class Receipt
        {
            internal int Bed, Medicine, Owner;
        }

        private static readonly ConditionalWeakTable<Job,Receipt> fetched = new();

        private static Building_Bed Infirmary(Pawn doctor, Pawn patient)
        {
            if (doctor == null || patient == null || doctor == patient || !doctor.Spawned || !patient.Spawned ||
                doctor.Map != patient.Map || doctor.Dead || doctor.Downed || patient.Dead ||
                doctor.Faction?.IsPlayer != true || doctor.Faction != patient.Faction || patient.IsPrisoner ||
                doctor.workSettings?.WorkIsActive(WorkTypeDefOf.Doctor) != true) return null;
            var bed = patient.CurrentBed();
            return bed?.Spawned == true && bed.Medical && bed.Faction == doctor.Faction ? bed : null;
        }

        // The native job may split/merge the fetched stack. Record the resulting
        // carried instance only after it actually grew at a qualifying pickup.
        [HarmonyPatch(typeof(Toils_Tend), nameof(Toils_Tend.PickupMedicine))]
        private static class Pickup
        {
            private static void Postfix(Toil __result, TargetIndex ind, Pawn injured)
            {
                if (!ClassProgressionFeature.Enabled) return;
                var pickup = __result.initAction;
                __result.initAction = () =>
                {
                    var pawn = __result.actor;
                    var job = pawn.CurJob;
                    if (job!=null) fetched.Remove(job);
                    var source = job?.GetTarget(ind).Thing;
                    var owner = BladeCareer.TrialOwner(pawn);
                    var bed = source is Medicine && source.Spawned && pawn.jobs.curDriver is JobDriver_TendPatient &&
                        BladeAscension.SupplyAllowed(owner) ? Infirmary(pawn, injured) : null;
                    int before = pawn.carryTracker.CarriedThing?.stackCount ?? 0;
                    bool outside = bed != null && source.Map == bed.Map &&
                        (source.Position - bed.Position).LengthHorizontalSquared >= 36;
                    pickup();
                    var carried = pawn.carryTracker.CarriedThing;
                    if (outside && carried != null && carried.def == source.def && carried.stackCount > before &&
                        job.GetTarget(ind).Thing == carried && pawn.CurJob == job)
                    {
                        var receipt = fetched.GetOrCreateValue(job);
                        receipt.Bed = bed.thingIDNumber;
                        receipt.Medicine = carried.thingIDNumber;
                        receipt.Owner = owner.thingIDNumber;
                    }
                };
            }
        }

        [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend), new[] {typeof(Pawn),typeof(Pawn),typeof(Medicine)})]
        private static class Treatment
        {
            private static void Prefix(Pawn doctor, Pawn patient, Medicine medicine, out (Job job,Pawn owner,int quantity) __state)
            {
                __state = default;
                var job = doctor?.CurJob;
                if (job == null || medicine == null || !fetched.TryGetValue(job, out var receipt) ||
                    doctor.jobs.curDriver is not JobDriver_TendPatient || job.targetA.Thing != patient ||
                    job.targetB.Thing != medicine || doctor.carryTracker.CarriedThing != medicine ||
                    medicine.thingIDNumber != receipt.Medicine) return;
                var owner = BladeCareer.TrialOwner(doctor);
                // Receipts from before projected work always belonged to the doctor.
                if (owner.thingIDNumber!=(receipt.Owner>0?receipt.Owner:doctor.thingIDNumber) || !BladeAscension.SupplyAllowed(owner)) return;
                var bed = Infirmary(doctor, patient);
                if (bed != null && bed.thingIDNumber == receipt.Bed && doctor.CanReachImmediate(patient,PathEndMode.Touch))
                    __state = (job,owner,medicine.stackCount);
            }

            private static void Postfix(Pawn doctor, Medicine medicine, (Job job,Pawn owner,int quantity) __state)
            {
                if (__state.quantity > 0 && (medicine.Destroyed || medicine.stackCount < __state.quantity))
                {
                    fetched.Remove(__state.job);
                    if (BladeCareer.TrialOwner(doctor)==__state.owner) BladeAscension.Supply(__state.owner);
                }
            }
        }

        [HarmonyPatch(typeof(Job), nameof(Job.ExposeData))]
        private static class SavedReceipt
        {
            private static void Postfix(Job __instance)
            {
                if (Scribe.mode == LoadSaveMode.Saving && !fetched.TryGetValue(__instance,out _)) return;
                if (Scribe.mode != LoadSaveMode.Saving && Scribe.mode != LoadSaveMode.LoadingVars) return;
                if (__instance.def?.driverClass == null || !typeof(JobDriver_TendPatient).IsAssignableFrom(__instance.def.driverClass)) return;
                fetched.TryGetValue(__instance,out var receipt);
                int bed=receipt?.Bed ?? 0, med=receipt?.Medicine ?? 0, owner=receipt?.Owner ?? 0;
                Scribe_Values.Look(ref bed,"rwomBladeSupplyBed");
                Scribe_Values.Look(ref med,"rwomBladeFetchedMedicine");
                Scribe_Values.Look(ref owner,"rwomBladeSupplyOwner");
                if (Scribe.mode == LoadSaveMode.LoadingVars && bed>0 && med>0)
                {
                    receipt = fetched.GetOrCreateValue(__instance);
                    receipt.Bed = bed; receipt.Medicine = med; receipt.Owner=owner;
                }
            }
        }
    }
}
