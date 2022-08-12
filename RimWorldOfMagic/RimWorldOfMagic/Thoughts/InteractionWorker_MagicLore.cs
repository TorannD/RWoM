using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace TorannMagic.Thoughts
{
    public class InteractionWorker_MagicLore : InteractionWorker
    {

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
            CompAbilityUserMagic compInit = initiator.GetCompAbilityUserMagic();
            CompAbilityUserMagic compRec = recipient.GetCompAbilityUserMagic();
            //base.Interacted(initiator, recipient, extraSentencePacks);
            int num = compInit.MagicUserLevel - compRec.MagicUserLevel;
            int num2 = Mathf.RoundToInt(Mathf.Clamp((int)(25f + Rand.Range(5f, 75f) + num),0, 200) * compRec.xpGain);
            compRec.MagicUserXP += num2;
            MoteMaker.ThrowText(recipient.DrawPos, recipient.MapHeld, "XP +" + num2, -1f);
        }

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            CompAbilityUserMagic compInit = initiator.GetCompAbilityUserMagic();
            CompAbilityUserMagic compRec = recipient.GetCompAbilityUserMagic();
            bool flag = !initiator.IsColonist || !recipient.IsColonist;
            float result = 0f;
            if (flag)
            {
                result = 0f;
            }
            else if(compInit != null && compRec != null)
            {
                bool flag2 = !compInit.IsMagicUser;
                if (flag2)
                {
                    result = 0f;
                }
                else
                {
                    bool flag3 = !compRec.IsMagicUser;
                    if (flag3)
                    {
                        result = 0f;
                    }
                    else
                    {
                        if (initiator.jobs != null && initiator.jobs.curDriver != null && initiator.jobs.curDriver.asleep)
                        {
                            result = 0f;
                        }
                        else
                        {
                            if (recipient.jobs != null && recipient.jobs.curDriver != null && recipient.jobs.curDriver.asleep)
                            {
                                result = 0f;
                            }
                            else
                            {
                                int levelInit = compInit.MagicUserLevel;
                                int levelRec = compRec.MagicUserLevel;
                                if (levelInit <= levelRec)
                                {
                                    result = 0f;
                                }
                                else
                                {
                                    bool flag5 = (initiator.relations !=  null && initiator.relations.OpinionOf(recipient) > 0) || (recipient.relations != null && recipient.relations.OpinionOf(initiator) > 0);
                                    if (flag5)
                                    {
                                        result = Rand.Range(0.6f, 0.8f);
                                    }
                                    else
                                    {
                                        result = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
