using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public class Hediff_CoreReturnMark : Hediff
    {
        public Thing Core;
        public CompCoreReturn Comp => Core?.TryGetComp<CompCoreReturn>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Core, "TM_returnCore");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) Comp?.Register(pawn);
        }
        public override void PostAdd(DamageInfo? dinfo) { base.PostAdd(dinfo); Comp?.Register(pawn); }
        public override void PostRemoved() { Comp?.Unregister(pawn); base.PostRemoved(); }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // RimWorld 1.7: the vanilla base implementation returns null, so iterating
            // it threw NullReferenceException for every selected marked pawn — vanilla's
            // gizmo-grid guard then wiped ALL of the pawn's buttons, including drafting.
            foreach (Gizmo gizmo in base.GetGizmos() ?? Enumerable.Empty<Gizmo>()) yield return gizmo;
            if (pawn?.Faction != Faction.OfPlayer || pawn.Dead) yield break;
            var recall = new Command_Action
            {
                defaultLabel = "TM_CoreReturnSingle".Translate(), defaultDesc = "TM_CoreReturnSingleDesc".Translate(10),
                icon = ContentFinder<Texture2D>.Get("UI/transfermana"), action = () => Comp?.RequestReturn(new[] { pawn })
            };
            string reason = null;
            if (Comp == null || !Comp.CanOperate(out reason)) recall.Disable(reason ?? "TM_CoreReturnUnavailable".Translate().ToString());
            yield return recall;
            yield return new Command_Action
            {
                defaultLabel = "TM_CoreReturnUnbind".Translate(), defaultDesc = "TM_CoreReturnUnbindDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
                action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TM_CoreReturnUnbindConfirm".Translate(pawn.LabelShortCap), () => pawn.health.RemoveHediff(this)))
            };
        }
    }

    [HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
    public static class CoreReturnCaravanGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            if (__instance.Faction != Faction.OfPlayer) yield break;
            var marked = __instance.PawnsListForReading.Where(p => CompCoreReturn.FindMark(p)?.Comp?.IsBound(p) == true).ToList();
            if (marked.Count == 0) yield break;
            yield return new Command_Action
            {
                defaultLabel = "TM_CoreReturnSingle".Translate(), defaultDesc = "TM_CoreReturnSingleDesc".Translate(10),
                icon = ContentFinder<Texture2D>.Get("UI/transfermana"), action = () => Find.WindowStack.Add(new FloatMenu(marked.Select(p =>
                    new FloatMenuOption(p.LabelShortCap, () => CompCoreReturn.FindMark(p)?.Comp?.RequestReturn(new[] { p }))).ToList()))
            };
            yield return new Command_Action
            {
                defaultLabel = "TM_CoreReturnCaravan".Translate(), defaultDesc = "TM_CoreReturnCaravanDesc".Translate(10),
                icon = ContentFinder<Texture2D>.Get("UI/transfermana"), action = () =>
                {
                    var cores = marked.Select(p => CompCoreReturn.FindMark(p)?.Comp).Where(c => c != null).Distinct().ToList();
                    var roster = __instance.PawnsListForReading.ToList();
                    if (cores.Count == 1) cores[0].RequestReturn(roster);
                    else Find.WindowStack.Add(new FloatMenu(cores.Select(c => new FloatMenuOption(c.parent.LabelCap, () => c.RequestReturn(roster))).ToList()));
                }
            };
        }
    }
}
