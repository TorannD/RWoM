using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace TorannMagic
{
    /// <summary>
    /// Inspect-pane tab variant of the Grimoire button (magic side). Enabled by
    /// turning off the "grimoireOnAbilityBar" mod setting: the tab shows for
    /// class holders and opens the Grimoire window on click, then deselects
    /// itself so no empty pane is left behind. Faceless uses the might tab; other hybrids use magic —
    /// both tabs open the same window, showing two of them made no sense.
    /// </summary>
    public class ITab_Pawn_Magic : ITab
    {
        private bool pendingDeselect;

        public override bool IsVisible
        {
            get
            {
                if (ModOptions.Settings.Instance.grimoireOnAbilityBar)
                {
                    return false;
                }
                Pawn pawn = base.SelPawn;
                if (pawn == null || !pawn.IsColonist)
                {
                    return false;
                }
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                bool faceless = pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true;
                return comp != null && comp.IsMagicUser && !faceless;
            }
        }

        public ITab_Pawn_Magic()
        {
            this.labelKey = "TM_Grimoire";
        }

        public override void OnOpen()
        {
            base.OnOpen();
            OpenGrimoire();
            pendingDeselect = true;
        }

        public override void TabUpdate()
        {
            base.TabUpdate();
            if (pendingDeselect)
            {
                // ToggleTab sets OpenTabType after OnOpen returns, so the
                // deselect has to happen on the next frame
                pendingDeselect = false;
                if (MainButtonDefOf.Inspect.TabWindow is MainTabWindow_Inspect pane && pane.OpenTabType == GetType())
                {
                    pane.OpenTabType = null;
                }
            }
        }

        protected override void FillTab()
        {
            // the Grimoire window opens on tab click; nothing to draw in the pane
        }

        private void OpenGrimoire()
        {
            Pawn pawn = base.SelPawn;
            if (pawn == null)
            {
                return;
            }
            Window_TMGrimoire open = Find.WindowStack.Windows.OfType<Window_TMGrimoire>().FirstOrDefault();
            if (open != null)
            {
                open.Close();
            }
            Window_TMGrimoire.OpenFor(pawn);
        }
    }
}
