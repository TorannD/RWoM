using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend), new[] {typeof(Pawn), typeof(Pawn), typeof(Medicine)})]
    internal static class RangerAnimalCareTrial
    {
        private static void Prefix(Pawn doctor, Pawn patient, out (Pawn owner,List<Hediff_Injury> wounds) __state)
        {
            __state = default;
            if (!ClassProgressionFeature.Enabled || doctor?.Spawned != true || patient == null || doctor == patient ||
                !patient.Spawned || patient.Map != doctor.Map || patient.Dead || doctor.Dead || doctor.Downed ||
                doctor.Faction?.IsPlayer != true || patient.Faction != doctor.Faction ||
                doctor.jobs?.curDriver is not JobDriver_TendPatient || doctor.CurJob.targetA.Thing != patient ||
                patient.RaceProps?.Animal != true || RangerCareer.ActivePath(doctor,out __state.owner)?.Stage != InitiationStage.Master) return;
            foreach (var h in patient.health.hediffSet.hediffs)
                if (h is Hediff_Injury wound && wound.TendableNow()) (__state.wounds ??= new List<Hediff_Injury>()).Add(wound);
        }

        private static void Postfix(Pawn doctor, Pawn patient, (Pawn owner,List<Hediff_Injury> wounds) __state)
        {
            if (__state.wounds == null) return;
            foreach (var wound in __state.wounds)
                if (!wound.TendableNow())
                {
                    RangerAscension.CareFrom(doctor,__state.owner,patient);
                    return;
                }
        }
    }
}
