using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.Core
{
    [Flags]
    public enum DisciplineKind { Magic = 1, Might = 2 }
    public enum DisciplineStage { Classic, Foundation, Adept, Master, Ascended }
    public readonly record struct DisciplineSnapshot(string Id, DisciplineKind Kind, DisciplineStage Stage);

    public static class PawnDisciplines
    {
        internal static Action<Pawn, Action<string, int, int>> HostReader { private get; set; }

        public static bool TryRead(Pawn pawn, out IReadOnlyList<DisciplineSnapshot> disciplines)
        {
            disciplines = Array.Empty<DisciplineSnapshot>();
            if (pawn == null || HostReader == null) return false;
            var rows = new List<DisciplineSnapshot>();
            HostReader(pawn, (id, kind, stage) => rows.Add(new(id, (DisciplineKind)kind, (DisciplineStage)stage)));
            disciplines = rows.AsReadOnly();
            return true;
        }
    }
}
