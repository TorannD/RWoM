using RimWorld;
using Verse;

namespace TorannMagic
{
    public class CompProperties_MarkIVStats : CompProperties
    {
        public CompProperties_MarkIVStats() => compClass = typeof(CompMarkIVStats);
    }

    public class CompMarkIVStats : ThingComp
    {
        public override float GetStatOffset(StatDef stat)
        {
            if (parent is Building_TechnoTurret turret)
                return stat == StatDefOf.MaxHitPoints ? 10f * turret.SkillEfficiency : 0f;
            if (stat != StatDefOf.AccuracyTouch && stat != StatDefOf.AccuracyShort &&
                stat != StatDefOf.AccuracyMedium && stat != StatDefOf.AccuracyLong) return 0f;
            return parent.TryGetComp<CompEquippable>()?.PrimaryVerb?.caster is Building_TechnoTurret source
                ? .01f * source.SkillPower : 0f;
        }
    }
}
