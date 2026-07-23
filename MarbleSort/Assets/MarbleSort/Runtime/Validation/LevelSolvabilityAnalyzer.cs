using System;
using System.Collections.Generic;
using System.Text;
using MarbleSort.Data;

namespace MarbleSort.Validation
{
    public sealed class LevelSolvabilityResult
    {
        internal LevelSolvabilityResult(
            bool isSolvable,
            string message,
            string[] selectionSequence,
            int peakConveyorOccupancy,
            int exploredStateCount)
        {
            IsSolvable = isSolvable;
            Message = message;
            SelectionSequence = selectionSequence;
            PeakConveyorOccupancy = peakConveyorOccupancy;
            ExploredStateCount = exploredStateCount;
        }

        public bool IsSolvable { get; }

        public string Message { get; }

        public IReadOnlyList<string> SelectionSequence { get; }

        public int PeakConveyorOccupancy { get; }

        public int ExploredStateCount { get; }
    }

    public static class LevelSolvabilityAnalyzer
    {
        public const int MarblesPerTopBox = 9;
        public const int MarblesPerReceiverBox = 3;

        private static readonly string[] ColorIds = { "green", "blue", "orange", "yellow", "pink" };

        public static LevelSolvabilityResult Analyze(LevelData level, int conveyorCapacity)
        {
            if (level == null)
            {
                return Failed("The level is null.");
            }

            if (conveyorCapacity < 1)
            {
                return Failed("The conveyor capacity must be positive.");
            }

            if (!TryBuildModel(level, out LevelModel model, out string error))
            {
                return Failed(error);
            }

            SearchState initial = new SearchState(model.TopBoxes.Length, model.ReceiverLanes.Length);
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            List<string> path = new List<string>(model.TopBoxCount);
            List<string> solution = new List<string>(model.TopBoxCount);
            int exploredStateCount = 0;

            bool solved = Search(
                model,
                conveyorCapacity,
                initial,
                visited,
                path,
                solution,
                ref exploredStateCount,
                out int peakOccupancy);

            if (!solved)
            {
                return new LevelSolvabilityResult(
                    false,
                    $"No exposed-box selection sequence completes the level without blocking the {conveyorCapacity}-slot conveyor.",
                    Array.Empty<string>(),
                    0,
                    exploredStateCount);
            }

            return new LevelSolvabilityResult(
                true,
                $"Solved in {solution.Count} selections with peak conveyor occupancy {peakOccupancy}/{conveyorCapacity}.",
                solution.ToArray(),
                peakOccupancy,
                exploredStateCount);
        }

        private static bool Search(
            LevelModel model,
            int conveyorCapacity,
            SearchState state,
            HashSet<string> visited,
            List<string> path,
            List<string> solution,
            ref int exploredStateCount,
            out int peakOccupancy)
        {
            exploredStateCount++;
            StabilizeReceivers(model, state);

            if (IsSolved(model, state))
            {
                solution.Clear();
                solution.AddRange(path);
                peakOccupancy = state.PeakOccupancy;
                return true;
            }

            string key = BuildStateKey(state);
            if (!visited.Add(key))
            {
                peakOccupancy = 0;
                return false;
            }

            for (int boxIndex = 0; boxIndex < model.TopBoxes.Length; boxIndex++)
            {
                if (!IsTopBoxExposed(model, state, boxIndex))
                {
                    continue;
                }

                TopBoxModel box = model.TopBoxes[boxIndex];
                SearchState next = state.Clone();
                next.RemovedTopBoxes[boxIndex] = true;
                if (!TryReleaseBox(model, next, box.ColorIndex, conveyorCapacity))
                {
                    continue;
                }

                path.Add(box.Id);
                if (Search(
                        model,
                        conveyorCapacity,
                        next,
                        visited,
                        path,
                        solution,
                        ref exploredStateCount,
                        out peakOccupancy))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            peakOccupancy = 0;
            return false;
        }

        private static bool IsTopBoxExposed(
            LevelModel model,
            SearchState state,
            int boxIndex)
        {
            if (state.RemovedTopBoxes[boxIndex])
            {
                return false;
            }

            TopBoxModel box = model.TopBoxes[boxIndex];
            if (box.Row == 0)
            {
                return true;
            }

            for (int index = 0; index < model.TopBoxes.Length; index++)
            {
                if (!state.RemovedTopBoxes[index])
                {
                    continue;
                }

                TopBoxModel cleared = model.TopBoxes[index];
                bool directlyInFront =
                    cleared.Column == box.Column && cleared.Row == box.Row - 1;
                bool directlyBeside =
                    cleared.Row == box.Row && Math.Abs(cleared.Column - box.Column) == 1;
                if (directlyInFront || directlyBeside)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReleaseBox(
            LevelModel model,
            SearchState state,
            int colorIndex,
            int conveyorCapacity)
        {
            for (int marbleIndex = 0; marbleIndex < MarblesPerTopBox; marbleIndex++)
            {
                StabilizeReceivers(model, state);
                int occupiedCount = GetConveyorOccupancy(state);
                if (occupiedCount >= conveyorCapacity)
                {
                    return false;
                }

                state.ConveyorByColor[colorIndex]++;
                state.PeakOccupancy = Math.Max(state.PeakOccupancy, occupiedCount + 1);
                StabilizeReceivers(model, state);
            }

            return true;
        }

        private static void StabilizeReceivers(LevelModel model, SearchState state)
        {
            for (int laneIndex = 0; laneIndex < model.ReceiverLanes.Length; laneIndex++)
            {
                ReceiverBoxModel[] lane = model.ReceiverLanes[laneIndex];
                while (state.ActiveReceiverByLane[laneIndex] < lane.Length)
                {
                    ReceiverBoxModel active = lane[state.ActiveReceiverByLane[laneIndex]];
                    int available = state.ConveyorByColor[active.ColorIndex];
                    if (available <= 0)
                    {
                        break;
                    }

                    int required = MarblesPerReceiverBox - state.ActiveReceiverFillByLane[laneIndex];
                    int transferred = Math.Min(available, required);
                    state.ConveyorByColor[active.ColorIndex] -= transferred;
                    state.ActiveReceiverFillByLane[laneIndex] += transferred;

                    if (state.ActiveReceiverFillByLane[laneIndex] < MarblesPerReceiverBox)
                    {
                        break;
                    }

                    state.ActiveReceiverByLane[laneIndex]++;
                    state.ActiveReceiverFillByLane[laneIndex] = 0;
                }
            }
        }

        private static bool IsSolved(LevelModel model, SearchState state)
        {
            for (int index = 0; index < state.RemovedTopBoxes.Length; index++)
            {
                if (!state.RemovedTopBoxes[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < model.ReceiverLanes.Length; index++)
            {
                if (state.ActiveReceiverByLane[index] < model.ReceiverLanes[index].Length ||
                    state.ActiveReceiverFillByLane[index] != 0)
                {
                    return false;
                }
            }

            return GetConveyorOccupancy(state) == 0;
        }

        private static int GetConveyorOccupancy(SearchState state)
        {
            int count = 0;
            for (int index = 0; index < state.ConveyorByColor.Length; index++)
            {
                count += state.ConveyorByColor[index];
            }

            return count;
        }

        private static string BuildStateKey(SearchState state)
        {
            StringBuilder builder = new StringBuilder(96);
            AppendArray(builder, state.RemovedTopBoxes);
            AppendArray(builder, state.ActiveReceiverByLane);
            AppendArray(builder, state.ActiveReceiverFillByLane);
            AppendArray(builder, state.ConveyorByColor);
            return builder.ToString();
        }

        private static void AppendArray(StringBuilder builder, int[] values)
        {
            for (int index = 0; index < values.Length; index++)
            {
                builder.Append(values[index]);
                builder.Append(',');
            }

            builder.Append('|');
        }

        private static void AppendArray(StringBuilder builder, bool[] values)
        {
            for (int index = 0; index < values.Length; index++)
            {
                builder.Append(values[index] ? '1' : '0');
            }

            builder.Append('|');
        }

        private static bool TryBuildModel(LevelData level, out LevelModel model, out string error)
        {
            TopGridData grid = level.topGrid;
            if (grid == null || grid.columns < 1 || grid.boxes == null)
            {
                model = null;
                error = "Top-grid data is incomplete.";
                return false;
            }

            TopBoxModel[] topBoxes = new TopBoxModel[grid.boxes.Length];

            for (int index = 0; index < grid.boxes.Length; index++)
            {
                TopBoxData box = grid.boxes[index];
                if (box == null || box.column < 0 || box.column >= grid.columns ||
                    !TryGetColorIndex(box.color, out int colorIndex))
                {
                    model = null;
                    error = $"Top box {index} is incomplete or uses an unsupported color.";
                    return false;
                }

                topBoxes[index] = new TopBoxModel(
                    box.id,
                    colorIndex,
                    box.column,
                    box.row);
            }

            ReceiverLaneData[] laneData = level.receiverLanes ?? Array.Empty<ReceiverLaneData>();
            ReceiverBoxModel[][] lanes = new ReceiverBoxModel[laneData.Length][];
            for (int laneIndex = 0; laneIndex < laneData.Length; laneIndex++)
            {
                BottomBoxData[] boxes = laneData[laneIndex]?.boxes ?? Array.Empty<BottomBoxData>();
                lanes[laneIndex] = new ReceiverBoxModel[boxes.Length];
                for (int boxIndex = 0; boxIndex < boxes.Length; boxIndex++)
                {
                    BottomBoxData box = boxes[boxIndex];
                    if (box == null || !TryGetColorIndex(box.color, out int colorIndex))
                    {
                        model = null;
                        error = $"Receiver box {boxIndex} in lane {laneIndex} is incomplete or uses an unsupported color.";
                        return false;
                    }

                    lanes[laneIndex][boxIndex] = new ReceiverBoxModel(colorIndex);
                }
            }

            model = new LevelModel(topBoxes, lanes);
            error = string.Empty;
            return true;
        }

        private static bool TryGetColorIndex(string colorId, out int colorIndex)
        {
            for (int index = 0; index < ColorIds.Length; index++)
            {
                if (string.Equals(ColorIds[index], colorId?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    colorIndex = index;
                    return true;
                }
            }

            colorIndex = -1;
            return false;
        }

        private static LevelSolvabilityResult Failed(string message)
        {
            return new LevelSolvabilityResult(false, message, Array.Empty<string>(), 0, 0);
        }

        private sealed class LevelModel
        {
            public LevelModel(TopBoxModel[] topBoxes, ReceiverBoxModel[][] receiverLanes)
            {
                TopBoxes = topBoxes;
                ReceiverLanes = receiverLanes;
            }

            public TopBoxModel[] TopBoxes { get; }

            public ReceiverBoxModel[][] ReceiverLanes { get; }

            public int TopBoxCount => TopBoxes.Length;
        }

        private readonly struct TopBoxModel
        {
            public TopBoxModel(string id, int colorIndex, int column, int row)
            {
                Id = id ?? string.Empty;
                ColorIndex = colorIndex;
                Column = column;
                Row = row;
            }

            public string Id { get; }

            public int ColorIndex { get; }

            public int Column { get; }

            public int Row { get; }
        }

        private readonly struct ReceiverBoxModel
        {
            public ReceiverBoxModel(int colorIndex)
            {
                ColorIndex = colorIndex;
            }

            public int ColorIndex { get; }
        }

        private sealed class SearchState
        {
            public SearchState(int topBoxCount, int laneCount)
            {
                RemovedTopBoxes = new bool[topBoxCount];
                ActiveReceiverByLane = new int[laneCount];
                ActiveReceiverFillByLane = new int[laneCount];
                ConveyorByColor = new int[ColorIds.Length];
            }

            private SearchState(SearchState source)
            {
                RemovedTopBoxes = (bool[])source.RemovedTopBoxes.Clone();
                ActiveReceiverByLane = (int[])source.ActiveReceiverByLane.Clone();
                ActiveReceiverFillByLane = (int[])source.ActiveReceiverFillByLane.Clone();
                ConveyorByColor = (int[])source.ConveyorByColor.Clone();
                PeakOccupancy = source.PeakOccupancy;
            }

            public bool[] RemovedTopBoxes { get; }

            public int[] ActiveReceiverByLane { get; }

            public int[] ActiveReceiverFillByLane { get; }

            public int[] ConveyorByColor { get; }

            public int PeakOccupancy { get; set; }

            public SearchState Clone()
            {
                return new SearchState(this);
            }
        }
    }
}
