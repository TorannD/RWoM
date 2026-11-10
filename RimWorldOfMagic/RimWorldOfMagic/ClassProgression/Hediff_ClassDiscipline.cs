using Verse;

namespace TorannMagic.ClassProgression
{
    // Health-tab projection only. Removing a marker cannot remove a class.
    public sealed class Hediff_ClassDiscipline : Hediff
    {
        public string profileId;
        public override string LabelBase => profileId == "rwom.necromancer" && ClassIdentity.IsLich(pawn)
            ? TorannMagicDefOf.Lich?.DataAtDegree(0)?.label ?? ClassIdentity.DisciplineLabel(profileId)
            : ClassIdentity.DisciplineLabel(profileId);
        public override string TipStringExtra
        {
            get
            {
                var progress = ClassIdentity.Component(pawn)?.State?.Find(profileId);
                return progress == null ? base.TipStringExtra
                    : ("TM_ProgressionStage_" + progress.Stage).Translate().ToString();
            }
        }
        public override bool TryMergeWith(Hediff other) => false;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref profileId, "TM_disciplineId");
        }
    }
}
