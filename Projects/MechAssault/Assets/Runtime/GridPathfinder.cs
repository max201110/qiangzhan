using System;
using System.Collections.Generic;

namespace Frontier
{
    public struct Cell : IEquatable<Cell>
    {
        public int X, Z;
        public Cell(int x, int z) { X = x; Z = z; }
        public bool Equals(Cell other) { return X == other.X && Z == other.Z; }
        public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
        public override int GetHashCode() { return (X * 397) ^ Z; }
    }

    // Small arena, four-connected A*. No NavMesh bake or third-party package required.
    public sealed class GridPathfinder
    {
        readonly bool[,] blocked;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public GridPathfinder(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
            Width = width; Height = height; blocked = new bool[width, height];
        }
        public bool IsOpen(Cell cell)
        {
            return cell.X >= 0 && cell.Z >= 0 && cell.X < Width && cell.Z < Height && !blocked[cell.X, cell.Z];
        }
        public void Block(int x, int z)
        {
            if (x >= 0 && z >= 0 && x < Width && z < Height) blocked[x, z] = true;
        }
        public Cell NearestOpen(Cell origin)
        {
            if (IsOpen(origin)) return origin;
            for (int r = 1; r < Math.Max(Width, Height); r++)
                for (int x = -r; x <= r; x++)
                    for (int z = -r; z <= r; z++)
                    {
                        if (Math.Abs(x) != r && Math.Abs(z) != r) continue;
                        Cell test = new Cell(origin.X + x, origin.Z + z);
                        if (IsOpen(test)) return test;
                    }
            return origin;
        }
        public List<Cell> FindPath(Cell start, Cell goal)
        {
            var result = new List<Cell>();
            start = NearestOpen(start); goal = NearestOpen(goal);
            if (!IsOpen(start) || !IsOpen(goal)) return result;
            var open = new List<Cell> { start };
            var closed = new HashSet<Cell>();
            var parents = new Dictionary<Cell, Cell>();
            var costs = new Dictionary<Cell, int> { { start, 0 } };
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (open.Count > 0)
            {
                int best = 0, bestCost = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    Cell n = open[i];
                    int f = costs[n] + Math.Abs(n.X - goal.X) + Math.Abs(n.Z - goal.Z);
                    if (f < bestCost) { best = i; bestCost = f; }
                }
                Cell current = open[best]; open.RemoveAt(best);
                if (current.Equals(goal))
                {
                    while (!current.Equals(start)) { result.Add(current); current = parents[current]; }
                    result.Reverse(); return result;
                }
                closed.Add(current);
                for (int i = 0; i < 4; i++)
                {
                    Cell next = new Cell(current.X + dx[i], current.Z + dz[i]);
                    if (!IsOpen(next) || closed.Contains(next)) continue;
                    int g = costs[current] + 1;
                    if (costs.TryGetValue(next, out int previous) && g >= previous) continue;
                    parents[next] = current; costs[next] = g;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return result;
        }
    }
}
