using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal abstract class AdditionalMartialData : IExposable
    {
        public abstract void ExposeData();
    }

    internal abstract class AdditionalMartialPath
    {
        internal abstract string Id { get; }
        internal abstract string Book { get; }
        internal abstract string[] Choices { get; }
        internal virtual bool SupportsBook(ThingDef book) => true;
        internal abstract bool CanEnroll(CompAbilityUserMight might, string choice);
        internal abstract void Restore(CompAbilityUserMight might, DisciplineProgress path);
        internal abstract IEnumerable<MightPower> Powers(CompAbilityUserMight might);
        internal abstract string Requirements(Pawn pawn, DisciplineProgress path);
        internal abstract float ControlsHeight(Pawn pawn, DisciplineProgress path);
        internal abstract void DrawControls(Pawn pawn, DisciplineProgress path, float width, ref float y);
    }
    internal static class AdditionalMartialRegistry
    {
        internal static readonly AdditionalMartialPath[] Paths = {
            new SuperSoldierMartialPath(), new CommanderMartialPath(), new MonkMartialPath(), new PsionicMartialPath(), new ApothecaryMartialPath(), new DeathKnightMartialPath(), new FacelessMartialPath(), new ShadowWalkerMartialPath()
        };

        internal static DisciplineProgress ActingPath(Pawn body,string id,out Pawn owner)
        {
            owner=body;
            if (!ClassProgressionFeature.Enabled || body==null) return null;
            if (ClassIdentity.TryGetState(body,out var local) && local.Find(id) is DisciplineProgress native) return native;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var held) || held.DepletionPending ||
                !ClassIdentity.TryGetState(soul,out var original)) return null;
            var path=original.Find(id);
            if (path!=null) owner=soul;
            return path;
        }

        internal static AdditionalMartialPath ForId(string id) => Paths.FirstOrDefault(p => p.Id == id);
        internal static AdditionalMartialPath ForBook(string book) => Paths.FirstOrDefault(p => p.Book == book);
        internal static AdditionalMartialPath Current(ClassProgressionState state, out DisciplineProgress discipline)
        {
            foreach (var path in Paths)
            {
                discipline = state.Find(path.Id);
                if (discipline != null) return path;
            }
            discipline = null;
            return null;
        }
    }
}
