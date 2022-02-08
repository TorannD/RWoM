using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using TorannMagic.Golems;

namespace TorannMagic.Golems
{
    public class MainTabWindow_Golems : MainTabWindow
    {
        List<TMPawnGolem> glist = new List<TMPawnGolem>();
        List<Building_TMGolemBase> blist = new List<Building_TMGolemBase>();
        List<Pawn> allGolems = new List<Pawn>();

        protected PawnTableDef PawnTableDef => TorannMagicDefOf.TM_Golems;
        private PawnTable table;

        protected virtual float ExtraBottomSpace => 53f;
        protected virtual float ExtraTopSpace => 0f;

        public override Vector2 RequestedTabSize
        {
            get
            {
                if (table == null)
                {
                    return Vector2.zero;
                }
                return new Vector2(table.Size.x + Margin * 2f, table.Size.y + ExtraBottomSpace + ExtraTopSpace + Margin * 2f);
            }
        }

        protected virtual IEnumerable<Pawn> Pawns
        {
            get
            {
                allGolems.Clear();
                foreach (TMPawnGolem pg in GolemPawns)
                {
                    allGolems.Add(pg);
                }
                foreach (Building_TMGolemBase bg in GolemBuildings)
                {
                    allGolems.Add(bg.GolemPawn);
                }
                return allGolems;
            }
        }

        protected virtual List<TMPawnGolem> GolemPawns
        {
            get
            {
                glist.Clear();
                List<Pawn> tmpList = Find.CurrentMap.mapPawns.AllPawnsSpawned;
                foreach (Pawn p in tmpList)
                {
                    if (TM_Calc.IsGolem(p))
                    {
                        TMPawnGolem pg = p as TMPawnGolem;
                        glist.Add(pg);
                    }
                }
                return glist;
            }
        }

        protected virtual List<Building_TMGolemBase> GolemBuildings
        {
            get
            {
                blist.Clear();
                List<Thing> tmpList = Find.CurrentMap.listerThings.AllThings.Where((Thing x) => TM_Calc.IsGolemBuilding(x)).ToList();
                foreach (Thing t in tmpList)
                {
                    Building_TMGolemBase bg = t as Building_TMGolemBase;
                    if (bg != null)
                    {
                        blist.Add(bg);
                    }
                }
                return blist;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            if (table == null)
            {
                table = CreateTable();
            }
            SetDirty();
        }

        public override void DoWindowContents(Rect rect)
        {
            try
            {
                table.PawnTableOnGUI(new Vector2(rect.x, rect.y + ExtraTopSpace));
            }
            catch
            {
                SetDirty();
            }
        }

        public void Notify_PawnsChanged()
        {
            SetDirty();
        }

        public override void Notify_ResolutionChanged()
        {
            table = CreateTable();
            base.Notify_ResolutionChanged();
        }

        private PawnTable CreateTable()
        {
            return (PawnTable)Activator.CreateInstance(PawnTableDef.workerClass, PawnTableDef, (Func<IEnumerable<Pawn>>)(() => Pawns), UI.screenWidth - (int)(Margin * 2f), (int)((float)(UI.screenHeight - 35) - ExtraBottomSpace - ExtraTopSpace - Margin * 2f));
        }

        protected void SetDirty()
        {
            table.SetDirty();
            SetInitialSizeAndPosition();
        }
    }
}
