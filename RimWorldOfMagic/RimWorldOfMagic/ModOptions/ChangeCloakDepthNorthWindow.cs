using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using Verse;
using RimWorld;
using System.Diagnostics;

namespace TorannMagic.ModOptions
{
    public class ChangeCloakDepthNorthWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 180f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool apply = false;

        public Vector2 scrollPosition = Vector2.zero;
        public string currentOffset;

        public ChangeCloakDepthNorthWindow()
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

            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width -= 36f;
            currentOffset = Widgets.TextField(GetRowRect(rect1, num), currentOffset);
            num+=2;           

            Rect rowRect99 = GetRowRect(rect1, num);
            rowRect99.width = 100f;
            apply = Widgets.ButtonText(rowRect99, "Apply", true, false, true);
            if (apply)
            {
                ModOptions.Settings.Instance.cloakDepthNorth = float.Parse(currentOffset);
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
