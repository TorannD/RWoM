using System.Linq;
using RimWorld;
using Verse;
using TorannMagic.ClassProgression;

namespace TorannMagic
{
    public class Verb_EnchantedBody : Verb_SpiritTargetable
    {
        protected virtual bool Aura => false;

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            burstShotsLeft = 0;
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(body, Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            if (magic == null || body.Downed || !body.Spawned) return false;
            var spell = Aura ? TorannMagicDefOf.TM_EnchantedAura : TorannMagicDefOf.TM_EnchantedBody;
            var other = Aura ? TorannMagicDefOf.TM_EnchantedBody : TorannMagicDefOf.TM_EnchantedAura;
            var active = body.health.hediffSet.GetFirstHediffOfDef(spell.abilityHediff);
            var alternate = body.health.hediffSet.GetFirstHediffOfDef(other.abilityHediff);
            // Each caster may dismiss only their own enchantment on the shared body.
            if ((active != null && HediffComp_EnchanterAspect.Owner(active) != owner) ||
                (alternate != null && HediffComp_EnchanterAspect.Owner(alternate) != owner)) return false;
            if (active == null && magic.maxMP < spell.upkeepEnergyCost)
            {
                Messages.Message("TM_NotEnoughManaToSustain".Translate(body.LabelShort, spell.label), MessageTypeDefOf.RejectInput);
                return false;
            }
            void Toggle(TMAbilityDef def, bool enabled)
            {
                var powers = def == TorannMagicDefOf.TM_EnchantedAura ? magic.MagicData.MagicPowersStandalone : magic.MagicData.MagicPowersE;
                AutoCast.AutocastPolicy.SetAutocast(magic, powers.FirstOrDefault(power => power.abilityDef == def), enabled);
            }
            if (alternate != null)
            {
                Toggle(other, false);
                body.health.RemoveHediff(alternate);
                FleckMaker.ThrowHeatGlow(body.Position, body.Map, 1f);
            }
            if (active != null)
            {
                Toggle(spell, false);
                body.health.RemoveHediff(active);
                FleckMaker.ThrowHeatGlow(body.Position, body.Map, 1f);
                return true;
            }
            int rank = !Aura && ModOptions.Settings.Instance.AIHardMode && !body.IsColonist ? 3 :
                magic.MagicData.MagicPowerSkill_EnchantedBody.First(s => s.label == "TM_EnchantedBody_pwr").level;
            var effect = HediffMaker.MakeHediff(spell.abilityHediff, body);
            if (owner != body)
            {
                var aspect = effect.TryGetComp<HediffComp_EnchanterAspect>();
                if (aspect == null) return false;
                aspect.projectedOwner = owner;
            }
            effect.Severity = .5f + rank;
            body.health.AddHediff(effect);
            Toggle(spell, true);
            for (int i = 0; i < 3; i++)
                FleckMaker.ThrowSmoke(body.DrawPos, body.Map, Rand.Range(.6f, .8f));
            FleckMaker.ThrowLightningGlow(body.DrawPos, body.Map, 1f);
            return true;
        }
    }

    public sealed class HediffCompProperties_EnchanterAspect : HediffCompProperties
    {
        public HediffCompProperties_EnchanterAspect() { compClass = typeof(HediffComp_EnchanterAspect); }
    }

    public sealed class HediffComp_EnchanterAspect : HediffComp
    {
        internal Pawn projectedOwner;
        internal static Pawn Owner(Hediff effect)
        {
            var soul = effect?.TryGetComp<HediffComp_EnchanterAspect>()?.projectedOwner;
            if (soul == null) return effect?.pawn;
            return SpiritRebirthState.TryGetPossessingSpirit(effect.pawn, out var original) && original == soul ? soul : null;
        }

        public override bool CompShouldRemove => base.CompShouldRemove || (projectedOwner != null && Owner(parent) == null);

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref projectedOwner, "rwomAspectOwner");
        }
    }
}
