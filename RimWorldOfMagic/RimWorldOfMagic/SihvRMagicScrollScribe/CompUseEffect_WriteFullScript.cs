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
                if (user.IsSlave && !ModOptions.Settings.Instance.SlavesCanScribe)
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
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsFireMage(user))
                    {
                        tempPod = ThingDef.Named("BookOfInnerFire");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsIceMage(user))
                    {
                        tempPod = ThingDef.Named("BookOfHeartOfFrost");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsLightningMage(user))
                    {
                        tempPod = ThingDef.Named("BookOfStormBorn");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsArcanist(user))
                    {
                        tempPod = ThingDef.Named("BookOfArcanist");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsPaladin(user))
                    {
                        tempPod = ThingDef.Named("BookOfValiant");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsSummoner(user))
                    {
                        tempPod = ThingDef.Named("BookOfSummoner");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsDruid(user))
                    {
                        tempPod = ThingDef.Named("BookOfDruid");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsBaseNecromancer(user) || ClassProgression.ClassIdentity.IsLich(user)))
                    {
                        tempPod = ThingDef.Named("BookOfNecromancer");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsPriest(user))
                    {
                        tempPod = ThingDef.Named("BookOfPriest");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ClassProgression.ClassIdentity.IsBard(user))
                    {
                        tempPod = ThingDef.Named("BookOfBard");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (TorannMagic.ClassProgression.ClassIdentity.IsSuccubus(user) || TorannMagic.ClassProgression.ClassIdentity.IsWarlock(user)))
                    {
                        tempPod = ThingDef.Named("BookOfDemons");
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsGeomancer(user)))
                    {
                        tempPod = TorannMagicDefOf.BookOfEarth;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && ((user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user))))
                    {
                        tempPod = TorannMagicDefOf.BookOfMagitech;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsBloodMage(user)))
                    {
                        tempPod = TorannMagicDefOf.BookOfHemomancy;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsEnchanter(user)))
                    {
                        tempPod = TorannMagicDefOf.BookOfEnchanter;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsChronomancer(user)))
                    {
                        tempPod = TorannMagicDefOf.BookOfChronomancer;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage)))
                    {
                        tempPod = TorannMagicDefOf.BookOfChaos;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsBrightmage(user)))
                    {
                        tempPod = TorannMagicDefOf.BookOfTheSun;
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def != null && (ClassProgression.ClassIdentity.IsShaman(user)))
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
