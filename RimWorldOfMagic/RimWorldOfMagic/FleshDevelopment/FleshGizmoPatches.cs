using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class FleshGizmoPatches
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = Append(__instance, __result);
        }

        private static IEnumerable<Gizmo> Append(Pawn pawn, IEnumerable<Gizmo> original)
        {
            foreach (var gizmo in original) yield return gizmo;
            if (pawn == null || pawn.Faction?.IsPlayer != true || pawn.Dead) yield break;
            if (FleshPresentation.Eligible(pawn)) {
                yield return new Gizmo_FleshThirst(pawn);
                foreach (var skill in FleshPresentation.ActiveSkills(pawn)) {
                    var command = new Command_Action {
                        defaultLabel = FleshPresentation.SkillLabel(skill), defaultDesc = ("TM_Flesh_" + skill + "Desc").Translate(),
                        icon = Icon(FleshPresentation.ActiveIcon(skill)), action = () => FleshPresentation.Activate(pawn, skill)
                    };
                    string rejection = FleshActions.Rejection(pawn, skill);
                    if (rejection != null) command.Disable(rejection.Translate());
                    yield return command;
                }
            }
            if (FleshPresentation.ShowOnBar(pawn))
                yield return new Command_Action {
                    defaultLabel = "TM_Flesh_Title".Translate(), defaultDesc = "TM_Flesh_Open".Translate(),
                    icon = Icon("10-flesh-grimoire"), action = () => FleshPresentation.Open(pawn)
                };
            if (PawnsFinder.AllMapsWorldAndTemporary_Alive.Any(p => FleshRepository.Owner(p) == pawn && FleshRepository.TryGet(p) != null))
                yield return new Command_Action {
                    defaultLabel = "TM_Flesh_StopAll".Translate(), defaultDesc = "TM_Flesh_StopDesc".Translate(),
                    icon = Icon("11-stop-feast"), action = () => FleshActions.StopAll(pawn)
                };
        }

        internal static Texture2D Icon(string name) => ContentFinder<Texture2D>.Get("UI/FleshDevelopment/" + name, true);
    }
}
