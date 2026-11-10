using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.Utils
{
    // Incremental breadth-first search. The caller supplies live wall/faction checks
    // and runs Step only from the game thread. No map or pawn is stored statically.
    internal sealed class LivingWallSearch
    {
        private readonly Func<IntVec3, bool> canVisit;
        private readonly int maxCells;
        private readonly Queue<IntVec3> frontier = new Queue<IntVec3>();
        private readonly Dictionary<IntVec3, IntVec3> parents = new Dictionary<IntVec3, IntVec3>();
        private readonly List<IntVec3> cells = new List<IntVec3>();

        internal LivingWallSearch(IntVec3 start, Func<IntVec3, bool> canVisit, int maxCells)
        {
            if (canVisit == null) throw new ArgumentNullException(nameof(canVisit));
            if (maxCells < 1) throw new ArgumentOutOfRangeException(nameof(maxCells));
            this.canVisit = canVisit;
            this.maxCells = maxCells;
            Start = start;
            if (canVisit(start))
            {
                frontier.Enqueue(start);
                parents.Add(start, start);
                cells.Add(start);
            }
        }

        internal IntVec3 Start { get; }
        internal IReadOnlyList<IntVec3> Cells => cells;
        internal bool Completed => frontier.Count == 0;
        internal bool Truncated { get; private set; }

        internal int Step(int budget)
        {
            if (budget < 0) throw new ArgumentOutOfRangeException(nameof(budget));
            int expanded = 0;
            while (expanded < budget && frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                expanded++;
                // A wall can be demolished between updates.
                if (!canVisit(cell)) continue;
                Visit(new IntVec3(cell.x + 1, cell.y, cell.z), cell);
                Visit(new IntVec3(cell.x - 1, cell.y, cell.z), cell);
                Visit(new IntVec3(cell.x, cell.y, cell.z + 1), cell);
                Visit(new IntVec3(cell.x, cell.y, cell.z - 1), cell);
            }
            return expanded;
        }

        private void Visit(IntVec3 cell, IntVec3 parent)
        {
            if (parents.ContainsKey(cell) || !canVisit(cell)) return;
            if (cells.Count == maxCells)
            {
                Truncated = true;
                return;
            }
            parents.Add(cell, parent);
            cells.Add(cell);
            frontier.Enqueue(cell);
        }

        internal List<IntVec3> PathTo(IntVec3 destination)
        {
            var path = new List<IntVec3>();
            if (!parents.ContainsKey(destination)) return path;
            var cell = destination;
            while (true)
            {
                path.Add(cell);
                if (cell == Start) break;
                cell = parents[cell];
            }
            path.Reverse();
            return path;
        }
    }
}
