using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class PsionicService
    {
        internal static DisciplineProgress Relief(Pawn worker,Thing cargo)
        {
            if (cargo==null || worker?.Spawned!=true ||
                !(cargo is Medicine || cargo.def?.ingestible?.HumanEdible==true)) return null;
            var path=PsionicMartialPath.ActivePath(worker,out _);
            return path!=null && (path.Stage==InitiationStage.Adept || path.Stage==InitiationStage.Master) &&
                !PsionicMartialPath.Ready(path) && worker.Map.mapPawns.FreeColonistsSpawned.Any(c=>c.Downed && !c.Dead) ? path : null;
        }
        internal static bool Hostile(Pawn pawn,Thing enemy)=>enemy!=null && enemy.HostileTo(pawn) &&
            (enemy is not Pawn e || !e.Downed && !e.Dead && !e.IsPrisoner);

        [HarmonyPatch(typeof(JobDriver_Repair),"MakeNewToils")]
        private static class Repair
        {
            private static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result,JobDriver_Repair __instance)
            {
                foreach(var toil in __result)
                {
                    var native=toil.tickAction;
                    if(native!=null && PsionicMartialPath.ActivePath(__instance.pawn,out _)!=null)
                    {
                        toil.tickAction=()=>
                        {
                            var target=__instance.job.targetA.Thing as Building;
                            bool disaster=target!=null && target.Faction==__instance.pawn.Faction && target.HitPoints>0 &&
                                target.HitPoints<target.MaxHitPoints && CommanderWorkReceipts.WasDisaster(target);
                            var path=disaster?PsionicMartialPath.ActivePath(__instance.pawn,out _):null;
                            native();
                            if(path!=null && !target.Destroyed && target.HitPoints==target.MaxHitPoints &&
                                PsionicMartialPath.ActivePath(__instance.pawn,out _)==path)
                                PsionicMartialPath.RecordService(path,repairId:target.thingIDNumber);
                        };
                    }
                    yield return toil;
                }
            }
        }
        [HarmonyPatch(typeof(Pawn),"PostApplyDamage")]
        private static class Impulse
        {
            private static void Prefix(Pawn __instance,DamageInfo dinfo,out DisciplineProgress __state)
            {
                __state=dinfo.Def==TMDamageDefOf.DamageDefOf.TM_PsionicInjury && dinfo.Instigator is Pawn p && p.Spawned && !p.Downed && Hostile(p,__instance) ? PsionicMartialPath.ActivePath(p,out _) : null;
            }
            private static void Postfix(DisciplineProgress __state,DamageInfo dinfo,float totalDamageDealt)
            {
                if(__state!=null && totalDamageDealt>0 && PsionicMartialPath.ActivePath((Pawn)dinfo.Instigator,out _)==__state)
                    PsionicMartialPath.RecordService(__state,hostileDamage:totalDamageDealt);
            }
        }
    }
}
