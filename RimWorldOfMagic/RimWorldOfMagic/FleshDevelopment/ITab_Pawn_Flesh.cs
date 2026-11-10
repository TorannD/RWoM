using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    public sealed class ITab_Pawn_Flesh : ITab
    {
        private bool pendingDeselect;
        public ITab_Pawn_Flesh() { labelKey = "TM_Flesh_Title"; }
        public override bool IsVisible => FleshPresentation.ShowTab(SelPawn);
        public override void OnOpen() { base.OnOpen(); FleshPresentation.Open(SelPawn); pendingDeselect = true; }
        public override void TabUpdate()
        {
            base.TabUpdate();
            if (!pendingDeselect) return;
            pendingDeselect = false;
            if (MainButtonDefOf.Inspect.TabWindow is MainTabWindow_Inspect pane && pane.OpenTabType == GetType()) pane.OpenTabType = null;
        }
        protected override void FillTab() { }
    }
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectTabs))]
    public static class FleshInspectTabsPatch
    {
        public static void Postfix(Thing __instance, ref IEnumerable<InspectTabBase> __result)
        {
            if (!(__instance is Pawn pawn) || !FleshPresentation.ShowTab(pawn)) return;
            var tabs = (__result ?? Enumerable.Empty<InspectTabBase>()).ToList();
            if (!tabs.Any(tab => tab is ITab_Pawn_Flesh)) tabs.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Flesh)));
            __result = tabs;
        }
    }
}
