using System.Collections.Generic;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class Projectile_SpiritMissile : Projectile_SpiritImpact
    {
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_MagicMissile;
        protected override string SkillStem => "TM_MagicMissile";
    }

    public sealed class Projectile_SpiritAmp : Projectile_SpiritImpact
    {
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_AMP;
        protected override string SkillStem => "TM_AMP";
    }

    public abstract class Projectile_SpiritImpact : Projectile_SpiritBolt
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (!projected) { base.Impact(hitThing, blockedByShield); return; }
            // The flight originates at the host, but Jecs uses launcher as the
            // instigator for every native damage packet during impact.
            var host = launcher;
            launcher = SpellInstigator;
            try { base.Impact(hitThing, blockedByShield); }
            finally { launcher = host; }
        }
    }
}
