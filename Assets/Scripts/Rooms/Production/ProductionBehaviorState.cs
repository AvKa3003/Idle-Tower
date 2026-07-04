using System.Collections.Generic;
using IdleTower.Data.Runtime;

namespace IdleTower.Rooms.Production
{
    public class ProductionBehaviorState : RoomBehaviorState
    {
        private readonly Dictionary<int, float> _elapsedByMode = new();

        public int ActiveModeIndex;
        public List<int> UnlockedModeIndices = new();

        public float GetElapsedSeconds(int modeIndex)
            => _elapsedByMode.TryGetValue(modeIndex, out var elapsed) ? elapsed : 0f;

        public void SetElapsedSeconds(int modeIndex, float seconds)
            => _elapsedByMode[modeIndex] = seconds < 0f ? 0f : seconds;

        public float ActiveElapsedSeconds
        {
            get => GetElapsedSeconds(ActiveModeIndex);
            set => SetElapsedSeconds(ActiveModeIndex, value);
        }

        public IReadOnlyDictionary<int, float> ElapsedByMode => _elapsedByMode;

        public bool IsModeUnlocked(int modeIndex)
            => UnlockedModeIndices.Contains(modeIndex);

        public void UnlockMode(int modeIndex)
        {
            if (!UnlockedModeIndices.Contains(modeIndex))
                UnlockedModeIndices.Add(modeIndex);
        }

        public override RoomBehaviorState Clone()
        {
            var clone = new ProductionBehaviorState
            {
                ActiveModeIndex = ActiveModeIndex,
                UnlockedModeIndices = new List<int>(UnlockedModeIndices)
            };

            foreach (var pair in _elapsedByMode)
                clone.SetElapsedSeconds(pair.Key, pair.Value);

            return clone;
        }
    }
}
