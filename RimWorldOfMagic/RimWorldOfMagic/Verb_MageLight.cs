using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_MageLight : Verb_UseAbility  
    {
        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn p = this.CasterPawn;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (this.currentTarget != null && base.CasterPawn != null)
            {                
                Map map = this.CasterPawn.Map;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if(this.currentTarget.Thing != null && this.currentTarget.Thing == base.CasterPawn)
                {
                    if (comp.mageLightActive == true)
                    {
                        comp.mageLightActive = false;
                        if (comp.mageLightThing != null)
                        {
                            comp.mageLightThing.Destroy(DestroyMode.Vanish);
                            comp.mageLightThing = null;
                        }
                        comp.mageLightSet = false;
                        Hediff hediff = base.CasterPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MageLightHD);
                        if (hediff != null)
                        {
                            base.CasterPawn.health.RemoveHediff(hediff);
                        }
                    }
                    //if (base.CasterPawn.health != null && base.CasterPawn.health.hediffSet != null && base.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MageLightHD))
                    //{
                    //    Hediff hediff = base.CasterPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MageLightHD);
                    //    if (hediff != null)
                    //    {
                    //        base.CasterPawn.health.RemoveHediff(hediff);
                    //    }
                    //    comp.mageLightActive = false;
                    //    comp.mageLightSet = false;
                    //}
                    else
                    {
                        if (comp.maxMP >= TorannMagicDefOf.TM_MageLight.upkeepEnergyCost)
                        {
                            comp.mageLightActive = true;
                            HealthUtility.AdjustSeverity(base.CasterPawn, TorannMagicDefOf.TM_MageLightHD, .5f);
                        }
                        else
                        {
                            Messages.Message("TM_NotEnoughManaToSustain".Translate(
                                            this.CasterPawn.LabelShort,
                                            TorannMagicDefOf.TM_MageLight.label
                                        ), MessageTypeDefOf.RejectInput);
                        }
                    }                    
                }
                else
                {
                    Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                }

                result = true;
            }

            this.burstShotsLeft = 0;
            //this.ability.TicksUntilCasting = (int)base.UseAbilityProps.SecondsToRecharge * 60;
            return result;
        }        
    }
}
