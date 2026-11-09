using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class MonkMeditation
    {
        internal static bool Finish(Pawn pawn)
        {
            var owner=MonkMartialPath.Owner(pawn);
            var path=MonkMartialPath.Path(owner);
            var chi=MonkMartialPath.Chi(owner);
            if (path?.MartialAlternate!=true || chi==null || !pawn.Spawned || pawn.Dead || pawn.Downed) return false;
            chi.Severity = UnityEngine.Mathf.Min(MonkMartialPath.Capacity(chi), chi.Severity+4+2*MonkMartialPath.Rank(owner));
            MonkMartialPath.Cultivated(owner);
            return true;
        }
    }

    [HarmonyPatch(typeof(TendUtility),nameof(TendUtility.DoTend),new[]{typeof(Pawn),typeof(Pawn),typeof(Medicine)})]
    internal static class MonkTendingTrial
    {
        private static void Prefix(Pawn doctor,Pawn patient,out List<Hediff_Injury> __state)
        {
            __state=null;
            if (doctor?.Spawned!=true || doctor.Dead || doctor.Downed || patient?.Spawned!=true || doctor==patient ||
                patient.Map!=doctor.Map || patient.Faction==null || patient.Faction!=doctor.Faction || patient.IsPrisoner ||
                doctor.CurJob?.targetA.Thing!=patient || doctor.jobs.curDriver is not JobDriver_TendPatient ||
                MonkMartialPath.Path(MonkMartialPath.Owner(doctor)) is not DisciplineProgress path || path.Stage>InitiationStage.Adept || MonkMartialPath.Ready(path)) return;
            foreach(var h in patient.health.hediffSet.hediffs)
                if (h is Hediff_Injury wound && wound.TendableNow()) (__state??=new List<Hediff_Injury>()).Add(wound);
        }
        private static void Postfix(Pawn doctor,List<Hediff_Injury> __state)
        {
            if (__state!=null) MonkMartialPath.Treatment(doctor,__state);
        }
    }

    [HarmonyPatch(typeof(DamageWorker_Extinguish),nameof(DamageWorker_Extinguish.Apply))]
    internal static class MonkFireRelief
    {
        private static void Prefix(DamageInfo dinfo,Thing victim,out bool __state)
        {
            __state = victim is Fire fire && fire.Spawned && dinfo.Instigator is Pawn p && p.Spawned && p.Map==fire.Map &&
                p.CurJob?.targetA.Thing==fire && p.jobs.curDriver is JobDriver_BeatFire && p.equipment?.Primary==null &&
                fire.instigator?.Faction?.IsPlayer!=true && fire.Map.areaManager.Home[fire.Position] &&
                MonkMartialPath.Path(p)?.Stage==InitiationStage.Master;
        }
        private static void Postfix(DamageInfo dinfo,Thing victim,bool __state)
        {
            if(__state && victim.Destroyed) MonkMartialPath.Relief((Pawn)dinfo.Instigator);
        }
    }
}
