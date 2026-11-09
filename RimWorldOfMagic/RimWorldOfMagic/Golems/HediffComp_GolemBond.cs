using System.Collections.Generic;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic.Golems
{
    public sealed class HediffCompProperties_GolemBond : HediffCompProperties
    {
        public HediffCompProperties_GolemBond() { compClass = typeof(HediffComp_GolemBond); }
    }

    public sealed class HediffComp_GolemBond : HediffComp
    {
        private List<Pawn> golems = new List<Pawn>();
        private bool tracked;

        internal void Register(Pawn golem)
        {
            tracked = true;
            if (!golems.Contains(golem)) golems.Add(golem);
            Refresh();
        }

        private void Refresh()
        {
            float severity = 0;
            for (int i = golems.Count - 1; i >= 0; i--)
            {
                var golem = golems[i];
                var master = golem?.GetComp<CompGolem>()?.pawnMaster;
                if (golem.DestroyedOrNull() || golem.Dead || GolemancerLifecycle.MasterBody(master) != Pawn ||
                    !GolemancerLifecycle.KnowsGolemancy(master))
                {
                    golems.RemoveAt(i);
                    continue;
                }
                var rank = master.GetCompAbilityUserMagic().MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_Golemancy);
                severity = System.Math.Max(severity,.5f + rank.level);
            }
            if (severity > 0 && parent.Severity != severity) parent.Severity = severity;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (tracked && Pawn.IsHashIntervalTick(60)) Refresh();
        }

        public override bool CompShouldRemove => base.CompShouldRemove || (tracked && golems.Count == 0);

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tracked, "rwomTrackedGolemBond");
            Scribe_Collections.Look(ref golems, "rwomBondGolems", LookMode.Reference);
            golems ??= new List<Pawn>();
        }
    }
}
