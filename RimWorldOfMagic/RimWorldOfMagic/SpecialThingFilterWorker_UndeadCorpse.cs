using RimWorld;
using Verse;

namespace TorannMagic
{
    // Scope of the "undead corpse" storage filters: corpses of pawns raised
    // by necromancy, carrying TM_UndeadHD (humanlike) or TM_UndeadAnimalHD
    // (animal). The hediff stays on the corpse's inner pawn after death, so
    // raised servants stay distinguishable from ordinary corpses. Each
    // storage branch carries its own SpecialThingFilterDef (allowedByDefault
    // =false; TM_AllowUndeadCorpse under CorpsesHumanlike,
    // TM_AllowUndeadAnimalCorpse under CorpsesAnimal) — the parentCategory
    // containment check scopes Matches to the right branch, and the postfix
    // on the vanilla colonist/stranger/slave corpse workers keeps undead
    // corpses out of their scope, making these toggles the sole gate.
    public class SpecialThingFilterWorker_UndeadCorpse : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            if (t is not Corpse corpse || corpse.InnerPawn == null) return false;
            var hediffs = corpse.InnerPawn.health?.hediffSet;
            if (hediffs == null) return false;
            if (TorannMagicDefOf.TM_UndeadHD != null && hediffs.HasHediff(TorannMagicDefOf.TM_UndeadHD)) return true;
            return TorannMagicDefOf.TM_UndeadAnimalHD != null && hediffs.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.thingClass == typeof(Corpse);
        }
    }
}
