using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.Thoughts
{
    public class InteractionWorker_Bard : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            CompAbilityUserMagic compInit = initiator.GetComp<CompAbilityUserMagic>();
            bool flag = !initiator.IsColonist || !recipient.IsColonist;
            float result;            
            if (flag)
            {
                result = 0f;
            }
            else
            {
                bool flag2 = !compInit.IsMagicUser;
                if (flag2)
                {
                    result = 0f;
                }
                else
                {
                    if(compInit.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                    {
                        if (initiator.jobs.curDriver.asleep)
                        {
                            result = 0f;
                        }
                        else
                        {
                            if (recipient.jobs.curDriver.asleep)
                            {
                                result = 0f;
                            }
                            else
                            {
                                bool learned = compInit.MagicData.MagicPowersB[1].learned;
                                if (learned)
                                {
                                    result = 1f;
                                }
                                else
                                {
                                    result = 0f;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = 0f;
                    }                    
                }
            }
            return result;
        }
    }
}
