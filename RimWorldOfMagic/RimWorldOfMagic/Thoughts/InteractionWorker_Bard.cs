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
            CompAbilityUserMagic compInit = initiator.GetCompAbilityUserMagic();
            bool flag = !initiator.IsColonist || !recipient.IsColonist || compInit is null;
            float result = 0f;            
            if (flag)
            {
                result = 0f;
            }
            else
            {
                if (!TM_Calc.IsMagicUser(initiator))
                {
                    result = 0f;
                }
                else
                {
                    if(initiator.story != null && initiator.story.traits != null && initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
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
