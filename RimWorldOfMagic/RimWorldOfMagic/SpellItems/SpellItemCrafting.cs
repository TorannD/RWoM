using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.SpellItems
{
    /// <summary>
    /// Opt-in knowledge requirement for the explicit spell-item recipes only.
    /// </summary>
    public class SpellItemRecipeRequirement : DefModExtension
    {
        public TMAbilityDef requiredSpell;

        public bool Allows(Pawn pawn)
        {
            if (pawn == null || pawn.story == null || requiredSpell == null)
            {
                return false;
            }

            CompAbilityUserMagic magic = pawn.GetCompAbilityUserMagic();
            if (magic == null || !magic.IsMagicUser)
            {
                return false;
            }

            var powers = magic.MagicData?.AllMagicPowers;
            if (powers == null)
            {
                return false;
            }

            // Match the whole learned family, not its currently selected rank.
            foreach (MagicPower power in powers)
            {
                if (power != null && power.learned && power.TMabilityDefs != null
                    && power.TMabilityDefs.Contains(requiredSpell))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Bill), nameof(Bill.PawnAllowedToStartAnew), new[] { typeof(Pawn) })]
    internal static class SpellItemBillKnowledgePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Bill __instance, Pawn p, ref bool __result)
        {
            // Never override vanilla restrictions, nor affect recipes without our extension.
            if (!__result)
            {
                return;
            }

            SpellItemRecipeRequirement requirement = __instance.recipe
                ?.GetModExtension<SpellItemRecipeRequirement>();
            if (requirement != null)
            {
                __result = requirement.Allows(p);
                if (!__result)
                {
                    // Native work menus omit unavailable jobs entirely without a reason.
                    JobFailReason.Is("TM_SpellItem_RequiredKnowledge".Translate(
                        requirement.requiredSpell?.LabelCap ?? "?"), __instance.recipe.LabelCap);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Toils_Recipe), nameof(Toils_Recipe.FinishRecipeAndStartStoringProduct))]
    public static class SpellItemRecipeCompletionPatch
    {
        public static void Postfix(Toil __result)
        {
            var finish = __result.initAction;
            __result.initAction = () =>
            {
                Pawn maker = __result.actor;
                SpellItemRecipeRequirement requirement = maker?.CurJob?.RecipeDef
                    ?.GetModExtension<SpellItemRecipeRequirement>();
                if (requirement != null && !requirement.Allows(maker))
                {
                    maker.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                finish?.Invoke();
            };
        }
    }
}
