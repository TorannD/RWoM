using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class ITab_Pawn_Might : ITab  //code by Jecrell
    {
        private Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = null;
                bool flag = base.SelPawn != null;
                if (flag)
                {
                    pawn = base.SelPawn;
                }
                else
                {
                    Corpse corpse = base.SelThing as Corpse;
                    if (corpse != null)
                    {
                        pawn = corpse.InnerPawn;
                    }
                }
                if (pawn == null)
                {
                    Log.Error("Character tab found no selected pawn to display.");
                    return null;
                }
                return pawn;
            }
        }

        public override bool IsVisible
        {
            get
            {
                
                bool flag = base.SelPawn.story != null && base.SelPawn.IsColonist;
                if (flag)
                {
                    CompAbilityUserMight compMight = base.SelPawn.GetCompAbilityUserMight();
                    if (compMight != null)
                    {
                        if (compMight.customClass != null)
                        {
                            return true;
                        }
                        if(compMight.AdvancedClasses.Count > 0)
                        {
                            return true;
                        }
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Ranger))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
                    {
                        return flag && true;
                    }
                }

                return false;
                
            }
        }

        public ITab_Pawn_Might()
        {
            this.size = MightCardUtility.MightCardSize + new Vector2(17f, 17f) * 2f;
            this.labelKey = "TM_TabMight";
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(17f, 17f, MightCardUtility.MightCardSize.x, MightCardUtility.MightCardSize.y);
            MightCardUtility.DrawMightCard(rect, this.PawnToShowInfoAbout);
        }

    }
}
