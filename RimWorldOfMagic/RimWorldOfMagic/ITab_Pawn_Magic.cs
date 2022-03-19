using RimWorld;
using System;
using UnityEngine;
using Verse;
using System.Linq;

namespace TorannMagic
{
    public class ITab_Pawn_Magic : ITab  //code by Jecrell
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
                    bool flag2 = corpse != null;
                    if (flag2)
                    {
                        pawn = corpse.InnerPawn;
                    }
                }
                bool flag3 = pawn == null;
                Pawn result;
                if (flag3)
                {
                    Log.Error("Character tab found no selected pawn to display.");
                    result = null;
                }
                else
                {
                    result = pawn;
                }
                return result;
            }
        }

        public override bool IsVisible
        {
            get
            {                
                bool flag = base.SelPawn.story != null && base.SelPawn.IsColonist;
                if (flag)
                {
                    CompAbilityUserMagic compMagic = base.SelPawn.TryGetComp<CompAbilityUserMagic>();
                    if (compMagic != null && compMagic.customClass != null)
                    {
                        return true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Druid))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Priest))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                    {
                        return flag && true;
                    }
                    if (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                    {
                        return flag && true;
                    }
                }
                return false;
                //return flag && (base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || base.SelPawn.story.traits.HasTrait(TorannMagicDefOf.Paladin));
            }
        }

        public ITab_Pawn_Magic()
        {
            this.size = MagicCardUtility.MagicCardSize + new Vector2(17f, 17f) * 2f;
            this.labelKey = "TM_TabMagic";
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(17f, 17f, MagicCardUtility.MagicCardSize.x, MagicCardUtility.MagicCardSize.y);
            MagicCardUtility.DrawMagicCard(rect, this.PawnToShowInfoAbout);
            
        }
    }
}
