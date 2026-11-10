using TorannMagic.Core;
using Verse;

namespace ExampleAddon;

public static class GlassWard
{
    // Example owner callback: use your actual active cast job/stance here.
    private static readonly System.Func<Pawn, bool> OwnsCast = pawn =>
        pawn?.CurJob?.def?.defName == "MyAddon_CastGlassWard";
    public static bool RegisterCastOwner() => CastOwnership.Register("myaddon.glassward", OwnsCast);
    public static bool UnregisterCastOwner() => CastOwnership.Unregister("myaddon.glassward", OwnsCast);

    public static bool CanSuggestCast(Verb verb, LocalTargetInfo target)
        => AbilityCasting.Check(verb, target, CastMode.Automatic) == CastAdmission.Allowed;

    public static bool HasFireFoundation(Pawn pawn)
    {
        if (!PawnDisciplines.TryRead(pawn, out var rows)) return false;
        foreach (var row in rows)
            if (row.Id == "rwom.fire" && row.Stage >= DisciplineStage.Foundation) return true;
        return false;
    }

    public static bool HasLearnedWard(Pawn pawn)
    {
        return PawnSpellKnowledge.Read(pawn, "myaddon.glassward", out var spell) == KnowledgeStatus.Available
            && spell.Learned;
    }

    public static bool CanOffer(Pawn pawn)
    {
        return AbilityCore.TryResolve("myaddon.glassward", out _)
            && TMResource.TryGetLevel(pawn, TMResource.Mana, out float mana, out float max)
            && max > 0f && mana >= max * 0.10f;
    }
}
