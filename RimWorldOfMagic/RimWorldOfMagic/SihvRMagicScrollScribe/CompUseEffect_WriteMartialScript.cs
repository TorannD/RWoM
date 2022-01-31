using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using TorannMagic.TMDefs;

namespace TorannMagic.SihvRMagicScrollScribe
{
    public class CompUseEffect_WriteMartialScript : CompUseEffect
    {

        public override void DoEffect(Pawn user)
        {
            ThingDef tempPod = null;
            IntVec3 currentPos = parent.PositionHeld;
            Map map = parent.Map;
            List<TMDefs.TM_CustomClass> cFighters = TM_ClassUtility.CustomFighterClasses;
            
            CompAbilityUserMight comp = user.TryGetComp<CompAbilityUserMight>();
            if (parent.def != null && comp != null && user.IsSlave)
            {
                Messages.Message("TM_SlaveScribeFail".Translate(
                        parent.def.label
                    ), MessageTypeDefOf.RejectInput);
                tempPod = null;
            }
            else if(parent.def != null && comp != null && comp.customClass != null)
            {
                tempPod = comp.customClass.fullScript;
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
            {
                tempPod = ThingDef.Named("BookOfGladiator");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
            {
                tempPod = ThingDef.Named("BookOfSniper");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
            {
                tempPod = ThingDef.Named("BookOfBladedancer");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Ranger))
            {
                tempPod = ThingDef.Named("BookOfRanger");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                tempPod = ThingDef.Named("BookOfFaceless");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
            {
                tempPod = ThingDef.Named("BookOfPsionic");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
            {
                tempPod = ThingDef.Named("BookOfDeathKnight");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.TM_Monk))
            {
                tempPod = ThingDef.Named("BookOfMonk");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && user.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
            {
                tempPod = ThingDef.Named("BookOfCommander");
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
            else if (parent.def != null && (user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy) || user.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || user.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier)))
            {
                int attempt = 0;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                RetryWrite:;
                if (attempt < 20)
                {
                    float rnd = Rand.Range(0, 9 + cFighters.Count);
                    if (rnd < 1)
                    {
                        if (settingsRef.Gladiator)
                        {
                            tempPod = ThingDef.Named("BookOfGladiator");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 2)
                    {
                        
                        if (settingsRef.Sniper)
                        {
                            tempPod = ThingDef.Named("BookOfSniper");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 3)
                    {                       
                        if (settingsRef.Bladedancer)
                        {
                            tempPod = ThingDef.Named("BookOfBladedancer");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 4)
                    {                        
                        if (settingsRef.Ranger)
                        {
                            tempPod = ThingDef.Named("BookOfRanger");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 5)
                    {                        
                        if (settingsRef.Psionic)
                        {
                            tempPod = ThingDef.Named("BookOfPsionic");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 6)
                    {
                        if (settingsRef.DeathKnight)
                        {
                            tempPod = ThingDef.Named("BookOfDeathKnight");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 7)
                    {
                        if (settingsRef.Monk)
                        {
                            tempPod = TorannMagicDefOf.BookOfMonk;
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if (rnd < 8)
                    {
                        if (settingsRef.Commander)
                        {
                            tempPod = TorannMagicDefOf.BookOfCommander;
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else if(rnd < 9)
                    {                        
                        if (settingsRef.Faceless)
                        {
                            tempPod = ThingDef.Named("BookOfFaceless");
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    else
                    {
                        if (cFighters.Count > 0)
                        {
                            tempPod = TM_ClassUtility.GetRandomCustomFighter().fullScript;
                        }
                        else
                        {
                            attempt++;
                            goto RetryWrite;
                        }
                    }
                    this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                }
                else
                {
                    Messages.Message("Unable to find a valid combat book type to write - ending attempt.", MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("NotPhyAdeptPawn".Translate(), MessageTypeDefOf.RejectInput);
            }
            
            if (tempPod != null)
            {
                SihvSpawnThings.SpawnThingDefOfCountAt(tempPod, 1, new TargetInfo(currentPos, map));
            }
        }
    }
}
