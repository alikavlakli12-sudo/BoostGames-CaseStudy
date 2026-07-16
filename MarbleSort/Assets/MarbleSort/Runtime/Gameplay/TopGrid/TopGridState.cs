using System;
using System.Collections.Generic;
using MarbleSort.Data;

namespace MarbleSort.Gameplay.TopGrid
{
    public sealed class TopBoxState
    {
        internal TopBoxState(TopBoxData data)
        {
            Id = data.id.Trim();
            ColorId = data.color.Trim().ToLowerInvariant();
            Column = data.column;
            InitialRow = data.row;
            CurrentRow = data.row;
        }

        public string Id { get; }

        public string ColorId { get; }

        public int Column { get; }

        public int InitialRow { get; }

        public int CurrentRow { get; internal set; }

        public bool IsRemoved { get; internal set; }
    }

    public readonly struct TopBoxMove
    {
        public TopBoxMove(string boxId, int fromRow, int toRow)
        {
            BoxId = boxId;
            FromRow = fromRow;
            ToRow = toRow;
        }

        public string BoxId { get; }

        public int FromRow { get; }

        public int ToRow { get; }
    }

    public sealed class TopBoxRemovalResult
    {
        internal TopBoxRemovalResult(TopBoxState removedBox, TopBoxMove[] moves)
        {
            RemovedBox = removedBox;
            Moves = moves;
        }

        public TopBoxState RemovedBox { get; }

        public IReadOnlyList<TopBoxMove> Moves { get; }
    }

    public sealed class TopGridState
    {
        private readonly List<TopBoxState> boxes = new List<TopBoxState>();
        private readonly Dictionary<string, TopBoxState> boxesById =
            new Dictionary<string, TopBoxState>(StringComparer.OrdinalIgnoreCase);

        public TopGridState(TopGridData grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            TopBoxData[] source = grid.boxes ?? Array.Empty<TopBoxData>();
            for (int index = 0; index < source.Length; index++)
            {
                TopBoxData data = source[index];
                if (data == null || string.IsNullOrWhiteSpace(data.id) || string.IsNullOrWhiteSpace(data.color))
                {
                    throw new ArgumentException($"Top box {index} is incomplete.", nameof(grid));
                }

                TopBoxState box = new TopBoxState(data);
                if (boxesById.ContainsKey(box.Id))
                {
                    throw new ArgumentException($"Duplicate top-box ID '{box.Id}'.", nameof(grid));
                }

                boxes.Add(box);
                boxesById.Add(box.Id, box);
            }

            ActiveCount = boxes.Count;
        }

        public IReadOnlyList<TopBoxState> Boxes => boxes;

        public int ActiveCount { get; private set; }

        public TopBoxState GetBox(string boxId)
        {
            return !string.IsNullOrWhiteSpace(boxId) && boxesById.TryGetValue(boxId, out TopBoxState box)
                ? box
                : null;
        }

        public bool IsExposed(string boxId)
        {
            TopBoxState box = GetBox(boxId);
            return box != null && IsExposed(box);
        }

        public bool CanSelect(string boxId)
        {
            return IsExposed(boxId);
        }

        public bool TryRemoveExposed(string boxId, out TopBoxRemovalResult result)
        {
            TopBoxState removed = GetBox(boxId);
            if (removed == null || !IsExposed(removed))
            {
                result = null;
                return false;
            }

            removed.IsRemoved = true;
            ActiveCount--;

            List<TopBoxState> collapsing = new List<TopBoxState>();
            for (int index = 0; index < boxes.Count; index++)
            {
                TopBoxState candidate = boxes[index];
                if (!candidate.IsRemoved &&
                    candidate.Column == removed.Column &&
                    candidate.CurrentRow > removed.CurrentRow)
                {
                    collapsing.Add(candidate);
                }
            }

            collapsing.Sort(CompareByRowThenId);
            TopBoxMove[] moves = new TopBoxMove[collapsing.Count];
            for (int index = 0; index < collapsing.Count; index++)
            {
                TopBoxState box = collapsing[index];
                int fromRow = box.CurrentRow;
                box.CurrentRow--;
                moves[index] = new TopBoxMove(box.Id, fromRow, box.CurrentRow);
            }

            result = new TopBoxRemovalResult(removed, moves);
            return true;
        }

        private bool IsExposed(TopBoxState box)
        {
            if (box.IsRemoved)
            {
                return false;
            }

            for (int index = 0; index < boxes.Count; index++)
            {
                TopBoxState candidate = boxes[index];
                if (!candidate.IsRemoved &&
                    candidate.Column == box.Column &&
                    candidate.CurrentRow < box.CurrentRow)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareByRowThenId(TopBoxState left, TopBoxState right)
        {
            int rowComparison = left.CurrentRow.CompareTo(right.CurrentRow);
            return rowComparison != 0
                ? rowComparison
                : string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
        }
    }
}
