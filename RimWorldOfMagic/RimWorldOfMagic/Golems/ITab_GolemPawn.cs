using RimWorld;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    [StaticConstructorOnStartup]
    internal class ITab_GolemPawn : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(400f, 550f);

        private static List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
                if (singleSelectedThing != null && singleSelectedThing is TMPawnGolem)
                {
                    TMPawnGolem golem_pawn = singleSelectedThing as TMPawnGolem;
                    if(golem_pawn != null)
                    {
                        CompGolem cg = golem_pawn.TryGetComp<CompGolem>();
                        if (cg != null)
                        {
                            return cg.Upgrades;
                        }
                    }
                }
                return null;
            }
        }

        public override bool IsVisible
        {
            get
            {
                return Upgrades != null && Upgrades.Count > 0;
            }
        }

        public ITab_GolemPawn()
        {
            this.size = ITab_GolemPawn.WinSize;
            this.labelKey = "TabGolem";
        }

        protected override void FillTab()
        {
            TMPawnGolem golem_pawn  = Find.Selector.SingleSelectedThing as TMPawnGolem;
            if(golem_pawn != null && Upgrades != null)
            {
                Rect rect = GenUI.ContractedBy(new Rect(0f, 0f, ITab_GolemPawn.WinSize.x, ITab_GolemPawn.WinSize.y), 10f);
                Rect rect2 = rect;
                Text.Font = GameFont.Small;
                string rectLabel = "Upgrades:";
                Widgets.Label(rect2, rectLabel);
                int num = 2;
                Text.Font = GameFont.Tiny;
                Rect rect3 = GetRowRect(rect2, num);
                foreach(TM_GolemUpgrade gu in Upgrades)
                {
                    Rect rectZ = GetRowRect(rect3, num);
                    GUI.color = Color.white;
                    if(gu.currentLevel == gu.maxLevel)
                    {
                        GUI.color = Color.cyan;
                    }

                    rectLabel = gu.name + ": " + gu.currentLevel + "/" + gu.maxLevel;
                    Widgets.Label(rectZ, rectLabel);
                    num++;
                }
            }

            
        }

        public static Rect GetRowRect(Rect inRect, int row)
        {
            float y = 20f * (float)row;
            Rect result = new Rect(inRect.x, y, inRect.width, 18f);
            return result;
        }
    }
}
