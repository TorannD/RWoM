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
    internal class ITab_GolemWorkstation : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(400f, 550f);

        private static List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
                if (singleSelectedThing != null && singleSelectedThing is Building_TMGolemBase)
                {
                    Building_TMGolemBase golem_building = singleSelectedThing as Building_TMGolemBase;
                    if(golem_building != null)
                    {
                        return golem_building.Upgrades;
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

        public ITab_GolemWorkstation()
        {
            this.size = ITab_GolemWorkstation.WinSize;
            this.labelKey = "TabGolem";
        }

        protected override void FillTab()
        {
            Building_TMGolemBase golem_building  = Find.Selector.SingleSelectedThing as Building_TMGolemBase;
            if(golem_building != null)
            {
                Rect rect = GenUI.ContractedBy(new Rect(0f, 0f, ITab_GolemWorkstation.WinSize.x, ITab_GolemWorkstation.WinSize.y), 10f);
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
