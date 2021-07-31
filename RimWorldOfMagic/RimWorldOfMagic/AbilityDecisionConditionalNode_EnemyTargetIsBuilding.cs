using Verse;
using AbilityUserAI;

namespace TorannMagic
{
    public class AbilityDecisionConditionalNode_EnemyTargetIsBuilding : AbilityDecisionNode
    {
        public override bool CanContinueTraversing(Pawn caster)
        {
            bool flag = caster.mindState.enemyTarget == null;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                Building bldg = caster.mindState.enemyTarget as Building;
                bool flag2 = bldg == null;
                if (flag2)
                {
                    result = false;
                }
                else
                {

                    bool flag4 = true;
                    bool invert = this.invert;
                    if (invert)
                    {
                        result = !flag4;
                    }
                    else
                    {
                        result = flag4;
                    }
                }
            }
            return result;
        }
    }
}
