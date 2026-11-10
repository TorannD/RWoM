using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class RegrowthSurgery
    {
        internal static CompAbilityUserMagic QualifiedOwner(Pawn surgeon)
        {
            if (surgeon == null || SpiritRebirthEnergy.Marked(surgeon)) return null;
            var def = TorannMagicDefOf.TM_RegrowLimb;
            var local = surgeon.GetCompAbilityUserMagic();
            if (local != null && (local.spell_RegrowLimb || (ClassIdentity.IsDruid(surgeon) ||
                TM_ClassUtility.ClassHasAbility(def, local)) && local.ExistingMagicData?.MagicPowersD.Any(p =>
                p.learned && p.TMabilityDefs.Contains(def)) == true)) return local;
            return SpiritRebirthState.TryGetPossessingSpirit(surgeon, out var soul)
                ? SpiritHostedBolt.Find(surgeon, soul, def)?.MagicUser : null;
        }

        private static float BaseCost(CompAbilityUserMagic owner, float efficiency)
        {
            var ranks = owner.ExistingMagicData?.MagicPowerSkill_RegrowLimb;
            int rank = ranks?.FirstOrDefault(p => p.label == "TM_RegrowLimb_eff")?.level ?? 0;
            return .9f * Mathf.Clamp01(1f - rank * efficiency);
        }

        private static bool CanPay(CompAbilityUserMagic owner, float efficiency)
        {
            if (owner?.Mana == null || !(owner.arcaneDmg > 0f) || float.IsInfinity(owner.arcaneDmg)) return false;
            float cost = BaseCost(owner, efficiency);
            return owner.Mana.CurLevel >= cost && owner.Mana.CurLevel >= cost / owner.arcaneDmg;
        }

        internal static bool TryPay(CompAbilityUserMagic owner, float efficiency)
        {
            if (!CanPay(owner, efficiency)) return false;
            owner.Mana.CurLevel -= BaseCost(owner, efficiency) / owner.arcaneDmg;
            return true;
        }

        internal static bool CanOperate(Pawn surgeon, RecipeDef recipe)
        {
            var owner = QualifiedOwner(surgeon);
            float efficiency = recipe == TorannMagicDefOf.UniversalRegrowth ? .08f : TorannMagicDefOf.TM_RegrowLimb.efficiencyReductionPercent;
            return surgeon?.RaceProps?.Humanlike == true && surgeon.skills != null &&
                surgeon.workSettings?.WorkIsActive(WorkTypeDefOf.Doctor) == true && recipe.PawnSatisfiesSkillRequirements(surgeon) &&
                surgeon.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                surgeon.health.capacities.CapableOf(PawnCapacityDefOf.Moving) && CanPay(owner, efficiency);
        }

        // Manual priority can override work priorities, never spell ownership or payment.
        internal static bool CanFundOperation(Pawn surgeon, RecipeDef recipe)
        {
            float efficiency = recipe == TorannMagicDefOf.UniversalRegrowth ? .08f : TorannMagicDefOf.TM_RegrowLimb.efficiencyReductionPercent;
            return CanPay(QualifiedOwner(surgeon), efficiency);
        }

        internal static bool Fails(Pawn surgeon, Pawn patient, RecipeDef recipe, float efficiency)
        {
            var owner = QualifiedOwner(surgeon);
            if (TryPay(owner, efficiency))
            {
                int xp = Mathf.RoundToInt(Rand.Range(160, 280) * owner.xpGain);
                owner.MagicUserXP += xp;
                MoteMaker.ThrowText(surgeon.DrawPos, surgeon.MapHeld, "XP +" + xp, -1f);
                TM_MoteMaker.ThrowRegenMote(patient.Position.ToVector3(), patient.Map, 1.2f);
                TM_MoteMaker.ThrowRegenMote(patient.Position.ToVector3(), patient.Map, .8f);
                TM_MoteMaker.ThrowRegenMote(patient.Position.ToVector3(), patient.Map, .8f);
                return false;
            }
            var payer = owner;
            if (payer == null && !SpiritRebirthEnergy.Marked(surgeon) && !SpiritRebirthState.TryGetPossessingSpirit(surgeon, out _))
                payer = surgeon.GetCompAbilityUserMagic();
            bool mage = owner != null || payer?.IsMagicUser == true;
            if (mage && payer.Mana != null) payer.Mana.CurLevel *= .5f;
            string reason = owner != null ? "TM_InsufficientManaForSurgery" : mage ? "TM_NoRegrowthSpell" : "TM_NotMagicUser";
            Find.LetterStack.ReceiveLetter("LetterLabelRegrowthSurgeryFail".Translate(),
                "LetterRegrowthSurgeryFail".Translate(surgeon.LabelCap, recipe.defName, patient.Label, reason.Translate(), surgeon.LabelShort),
                LetterDefOf.NegativeEvent);
            return true;
        }
    }
}
