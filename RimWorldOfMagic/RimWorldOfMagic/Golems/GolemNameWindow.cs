using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using Verse;
using RimWorld;
using System.Diagnostics;

namespace TorannMagic.Golems
{
    public class GolemNameWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 180f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool apply = false;

        public Vector2 scrollPosition = Vector2.zero;
        public CompGolem cg = null;
        List<TM_GolemUpgrade> upgrades = new List<TM_GolemUpgrade>();
        public string golemName = "";


        public GolemNameWindow()
        {
            base.closeOnCancel = true;
            base.doCloseButton = true;
            base.doCloseX = true;
            base.absorbInputAroundWindow = true;
            base.forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {                       
            int num = 0;
            float rowHeight = 28f;
            //GUI.BeginGroup(inRect);
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width -= 36f;
            golemName = Widgets.TextField(GetRowRect(rect1,num), golemName);
            num+=2;           

            Rect rowRect99 = GetRowRect(rect1, num);
            rowRect99.width = 100f;
            apply = Widgets.ButtonText(rowRect99, "Apply", true, false, true);
            if (apply)
            {
                cg.GolemName = NameTriple.FromString(golemName);
                cg.PawnGolem.Name = cg.GolemName;
            }
            GUI.EndScrollView();
        }

        public static Rect GetRowRect(Rect inRect, int row, float addedHeight = 0f)
        {
            float y = 24f * (float)row;
            Rect result = new Rect(inRect.x, y, inRect.width,  24f + addedHeight);
            return result;
        }
    }   
}
