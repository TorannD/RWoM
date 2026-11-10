using RimWorld;
using Verse;

namespace TorannMagic.SihvRMagicScrollScribe
{
    public class CompUseEffect_WriteTornScript : CompUseEffect
    {

        public override void DoEffect(Pawn user)
        {

            ThingDef tempPod = null;
            IntVec3 currentPos = parent.PositionHeld;
            Map map = parent.Map;
            CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();
            if (parent.def != null && comp != null && user.IsSlave && !ModOptions.Settings.Instance.SlavesCanScribe)
            {
                Messages.Message("TM_SlaveScribeFail".Translate(
                        parent.def.label
                    ), MessageTypeDefOf.RejectInput);
                tempPod = null;
            }
            else if (parent.def != null && comp != null && comp.customClass != null && (comp.customClass.tornScript != null || comp.customClass.fullScript != null))
            {
                if (comp.customClass.tornScript != null)
                {
                    tempPod = comp.customClass.tornScript;
                }
                else
                {
                    tempPod = comp.customClass.fullScript;
                }
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsShaman(user))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfShamanism;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsGolemancer(user))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfGolemancy;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsBrightmage(user))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfTheSun;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsFireMage(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfInnerFire");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsIceMage(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfHeartOfFrost");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsLightningMage(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfStormBorn");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsArcanist(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfArcanist");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsPaladin(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfValiant");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsSummoner(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfSummoner");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsDruid(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfNature");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (ClassProgression.ClassIdentity.IsBaseNecromancer(user) || ClassProgression.ClassIdentity.IsLich(user)))
            {
                tempPod = ThingDef.Named("Torn_BookOfUndead");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsPriest(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfPriest");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ClassProgression.ClassIdentity.IsBard(user))
            {
                tempPod = ThingDef.Named("Torn_BookOfBard");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (TorannMagic.ClassProgression.ClassIdentity.IsSuccubus(user) || TorannMagic.ClassProgression.ClassIdentity.IsWarlock(user)))
            {
                tempPod = ThingDef.Named("Torn_BookOfDemons");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (ClassProgression.ClassIdentity.IsGeomancer(user)))
            {
                tempPod = ThingDef.Named("Torn_BookOfEarth");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && ((user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user))))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfMagitech;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (ClassProgression.ClassIdentity.IsBloodMage(user)))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfHemomancy;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (ClassProgression.ClassIdentity.IsEnchanter(user)))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfEnchanter;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (ClassProgression.ClassIdentity.IsChronomancer(user)))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfChronomancer;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage)))
            {
                tempPod = TorannMagicDefOf.Torn_BookOfChaos;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) || user.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || user.story.traits.HasTrait(TorannMagicDefOf.TM_Empath)))
            {
                tempPod = TorannMagicDefOf.BookOfQuestion;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else
            {
                Messages.Message("NotGiftedPawn".Translate(
                        user.LabelShort
                    ), MessageTypeDefOf.RejectInput);
            }
            
            if (tempPod != null)
            {                    
                SihvSpawnThings.SpawnThingDefOfCountAt(tempPod, 1, new TargetInfo(currentPos, map));
            }

        }
    }
}
