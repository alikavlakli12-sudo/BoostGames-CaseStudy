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

            // The level is a fixed spatial puzzle. Clearing a tray can reveal
            // neighbours, but it must never compact a column or change a tray's
            // authored grid coordinate.
            result = new TopBoxRemovalResult(removed, Array.Empty<TopBoxMove>());
            return true;
        }

        private bool IsExposed(TopBoxState box)
        {
            if (box.IsRemoved)
            {
                return false;
            }

            if (box.InitialRow == 0)
            {
                return true;
            }

            for (int index = 0; index < boxes.Count; index++)
            {
                TopBoxState cleared = boxes[index];
                if (!cleared.IsRemoved)
                {
                    continue;
                }

                bool directlyInFront =
                    cleared.Column == box.Column &&
                    cleared.InitialRow == box.InitialRow - 1;
                bool directlyBeside =
                    cleared.InitialRow == box.InitialRow &&
                    Math.Abs(cleared.Column - box.Column) == 1;
                if (directlyInFront || directlyBeside)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
