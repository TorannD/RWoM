using RimWorld;
using Verse;

namespace TorannMagic.SihvRMagicScrollScribe
{
    public class CompUseEffect_WriteFullScript : CompUseEffect
    {

        public override void DoEffect(Pawn user)
        {
            ThingDef tempPod = null;
            IntVec3 currentPos = parent.PositionHeld;
            Map map = parent.Map;
            CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();

            if (parent.def != null && comp != null)
            {
                if (user.IsSlave)
                {                    
                    Messages.Message("TM_SlaveScribeFail".Translate(
                        parent.def.label
                    ), MessageTypeDefOf.RejectInput);
                    tempPod = null;
                }
                else
                {
                    if (comp.customClass != null && comp.customClass.fullScript != null)
                    {
                        tempPod = comp.customClass.fullScript;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                    {
                        tempPod = ThingDef.Named("BookOfInnerFire");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                    {
                        tempPod = ThingDef.Named("BookOfHeartOfFrost");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                    {
                        tempPod = ThingDef.Named("BookOfStormBorn");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        tempPod = ThingDef.Named("BookOfArcanist");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                    {
                        tempPod = ThingDef.Named("BookOfValiant");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                    {
                        tempPod = ThingDef.Named("BookOfSummoner");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Druid))
                    {
                        tempPod = ThingDef.Named("BookOfDruid");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || user.story.traits.HasTrait(TorannMagicDefOf.Lich)))
                    {
                        tempPod = ThingDef.Named("BookOfNecromancer");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Priest))
                    {
                        tempPod = ThingDef.Named("BookOfPriest");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                    {
                        tempPod = ThingDef.Named("BookOfBard");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.Succubus) || user.story.traits.HasTrait(TorannMagicDefOf.Warlock)))
                    {
                        tempPod = ThingDef.Named("BookOfDemons");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.Geomancer)))
                    {
                        tempPod = TorannMagicDefOf.BookOfEarth;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.Technomancer)))
                    {
                        tempPod = TorannMagicDefOf.BookOfMagitech;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.BloodMage)))
                    {
                        tempPod = TorannMagicDefOf.BookOfHemomancy;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.Enchanter)))
                    {
                        tempPod = TorannMagicDefOf.BookOfEnchanter;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.Chronomancer)))
                    {
                        tempPod = TorannMagicDefOf.BookOfChronomancer;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage)))
                    {
                        tempPod = TorannMagicDefOf.BookOfChaos;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.TM_Brightmage)))
                    {
                        tempPod = TorannMagicDefOf.BookOfTheSun;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.TM_Shaman)))
                    {
                        tempPod = TorannMagicDefOf.BookOfShamanism;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) || user.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || TM_Calc.IsMagicUser(user)))
                    {
                        tempPod = TM_Data.MageBookList().RandomElement();
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Messages.Message("NotGiftedPawn".Translate(
                                user.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                    }
                }
            }
            if (tempPod != null)
            {                
                SihvSpawnThings.SpawnThingDefOfCountAt(tempPod, 1, new TargetInfo(currentPos, map));
            }

        }
    }
}
