using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    // A launched spell keeps its original caster and rolled stats even if the
    // host changes allegiance, dies or holds another soul before impact.
    public abstract class Projectile_SpiritBolt : Projectile_AbilityBase
    {
        protected int pwrVal, verVal;
        protected float arcaneDmg = 1f;
        private Pawn projectedOwner;
        protected bool projected;
        protected Thing SpellInstigator => projected ? projectedOwner : launcher;
        internal Pawn SourcePawn => SpellInstigator as Pawn;

        protected abstract List<MagicPowerSkill> Skills(MagicData data);
        protected abstract string SkillStem { get; }

        internal static int SpellRank(Pawn owner, List<MagicPowerSkill> skills, string stem, string suffix)
        {
            if (owner.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true)
                return owner.GetCompAbilityUserMight()?.ExistingMightData?.MightPowerSkill_Mimic
                    ?.FirstOrDefault(s => s.label == "TM_Mimic" + suffix)?.level ?? 0;
            return skills.FirstOrDefault(s => s.label == stem + suffix)?.level ?? 0;
        }

        internal virtual void PrepareProjection(MagicAbility owner)
        {
            projectedOwner = owner.Pawn;
            projected = true;
            var skills = Skills(owner.MagicUser.MagicData);
            pwrVal = SpellRank(owner.Pawn, skills, SkillStem, "_pwr");
            verVal = SpellRank(owner.Pawn, skills, SkillStem, "_ver");
            arcaneDmg = owner.MagicUser.arcaneDmg;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref projected,"TM_spiritProjectedBolt");
            if (!projected) return;
            Scribe_References.Look(ref projectedOwner,"TM_boltSoul");
            Scribe_Values.Look(ref pwrVal,"TM_boltPower");
            Scribe_Values.Look(ref verVal,"TM_boltVersatility");
            Scribe_Values.Look(ref arcaneDmg,"TM_boltScale",1f);
        }
    }
}
