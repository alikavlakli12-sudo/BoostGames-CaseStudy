using System;

namespace MarbleSort.Session
{
    public sealed class GameSession
    {
        private readonly int levelCount;

        public GameSession(int levelCount, int startingLevelIndex = 0)
        {
            if (levelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelCount), "A session needs at least one level.");
            }

            if (startingLevelIndex < 0 || startingLevelIndex >= levelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startingLevelIndex));
            }

            this.levelCount = levelCount;
            CurrentLevelIndex = startingLevelIndex;
        }

        public int CurrentLevelIndex { get; private set; }

        public int AdvanceToNextLevel()
        {
            CurrentLevelIndex = (CurrentLevelIndex + 1) % levelCount;
            return CurrentLevelIndex;
        }
    }
}
