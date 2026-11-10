using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_SummonTotemHealing : Verb_SummonTotem
    {
        protected override ThingDef TotemDef => TorannMagicDefOf.TM_HealingTotem;
        protected override ThingDef Material => ThingDefOf.WoodLog;

        protected override void Configure(Building totem, Pawn owner, int power, int versatility)
        {
            var structure = (Building_TMTotem_Healing)totem;
            structure.pwrVal = power;
            structure.verVal = versatility;
            structure.arcanePwr = owner.GetCompAbilityUserMagic().arcaneDmg;
            var trial = ClassProgression.ShamanTrials.CaptureSpawn(Ability);
            ClassProgression.ShamanTrials.CompleteSpawn(trial,structure);
        }
    }
}
