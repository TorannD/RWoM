using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace TorannMagic.Thoughts
{
    public class InteractionWorker_MightLore : InteractionWorker
    {

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
            CompAbilityUserMight compInit = initiator.GetCompAbilityUserMight();
            CompAbilityUserMight compRec = recipient.GetCompAbilityUserMight();
            //base.Interacted(initiator, recipient, extraSentencePacks);
            int num = compInit.MightUserLevel - compRec.MightUserLevel;
            int num2 = Mathf.RoundToInt(Mathf.Clamp((int)(25f + Rand.Range(25f, 100f) + num), 0, 250) * compRec.xpGain);
            compRec.MightUserXP += num2;
            MoteMaker.ThrowText(recipient.DrawPos, recipient.MapHeld, "XP +" + num2, -1f);
        }

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            CompAbilityUserMight compInit = initiator.GetCompAbilityUserMight();
            CompAbilityUserMight compRec = recipient.GetCompAbilityUserMight();
            bool flag = !initiator.IsColonist || !recipient.IsColonist;
            float result = 0f;
            if (flag)
            {
                result = 0f;
            }
            else if (compInit != null && compRec != null)
            {
                bool flag2 = !compInit.IsMightUser;
                if (flag2)
                {
                    result = 0f;
                }
                else
                {
                    bool flag3 = !compRec.IsMightUser;
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
                                int levelInit = compInit.MightUserLevel;
                                int levelRec = compRec.MightUserLevel;
                                if (levelInit <= levelRec)
                                {
                                    result = 0f;
                                }
                                else
                                {
                                    bool flag5 = (initiator.relations != null && initiator.relations.OpinionOf(recipient) > 0) || (recipient.relations != null && recipient.relations.OpinionOf(initiator) > 0);
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
