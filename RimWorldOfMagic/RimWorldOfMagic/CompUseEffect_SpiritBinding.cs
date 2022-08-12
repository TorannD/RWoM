using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class CompUseEffect_SpiritBinding : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();

            if (parent.def != null && (TM_Calc.IsMagicUser(user) || TM_Calc.IsWanderer(user)))
            {
                string previousType = "unassigned";
                string newType = "";
                if (comp.guardianSpiritType != null)
                {
                    previousType = comp.guardianSpiritType.label;
                    float rnd = Rand.Value;
                    if (rnd < .34f)
                    {
                        if (comp.guardianSpiritType == TorannMagicDefOf.TM_SpiritBearR)
                        {
                            if (Rand.Chance(.5f))
                            {
                                comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritMongooseR;
                            }
                            else
                            {
                                comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritCrowR;
                            }
                        }
                        else
                        {
                            comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritBearR;
                        }
                    }
                    else if(rnd < .67f)
                    {
                        if (comp.guardianSpiritType == TorannMagicDefOf.TM_SpiritMongooseR)
                        {
                            if (Rand.Chance(.5f))
                            {
                                comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritBearR;
                            }
                            else
                            {
                                comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritCrowR;
                            }
                        }
                        else
                        {
                            comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritMongooseR;
                        }
                    }
                    else
                    {
                        if (comp.guardianSpiritType == TorannMagicDefOf.TM_SpiritCrowR)
                        {
                            if (Rand.Chance(.5f))
                            {
                                comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritBearR;
                            }
                            else
                            {
                                comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritMongooseR;
                            }
                        }
                        else
                        {
                            comp.guardianSpiritType = TorannMagicDefOf.TM_SpiritCrowR;
                        }
                    }
                    newType = comp.guardianSpiritType.label;
                }
                else
                {
                    newType = comp.GuardianSpiritType.label;
                }
                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                Messages.Message("TM_SpiritAnimalChange".Translate(user.LabelShort, previousType, newType), MessageTypeDefOf.TaskCompletion);
                                
            }
            else
            {
                Messages.Message("NotMageToLearnSpell".Translate(), MessageTypeDefOf.RejectInput);
            }
        }
    }
}
