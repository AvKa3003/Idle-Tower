using System.Collections.Generic;
using IdleTower.Data.Runtime;

namespace IdleTower.Rooms.Production
{
    public class ProductionBehaviorState : RoomBehaviorState
    {
        private readonly Dictionary<ModeId, float> _elapsedByMode = new();

        public ModeId ActiveModeId;
        public List<ModeId> UnlockedModeIds = new();

        public float GetElapsedSeconds(ModeId modeId)
        {
            if (modeId.IsEmpty)
                return 0f;

            return _elapsedByMode.TryGetValue(modeId, out var elapsed) ? elapsed : 0f;
        }

        public void SetElapsedSeconds(ModeId modeId, float seconds)
        {
            if (modeId.IsEmpty)
                return;

            _elapsedByMode[modeId] = seconds < 0f ? 0f : seconds;
        }

        public float ActiveElapsedSeconds
        {
            get => GetElapsedSeconds(ActiveModeId);
            set => SetElapsedSeconds(ActiveModeId, value);
        }

        public IReadOnlyDictionary<ModeId, float> ElapsedByMode => _elapsedByMode;

        public bool IsModeUnlocked(ModeId modeId)
            => !modeId.IsEmpty && UnlockedModeIds.Contains(modeId);

        public void UnlockMode(ModeId modeId)
        {
            if (modeId.IsEmpty)
                return;

            if (!UnlockedModeIds.Contains(modeId))
                UnlockedModeIds.Add(modeId);
        }

        public override RoomBehaviorState Clone()
        {
            var clone = new ProductionBehaviorState
            {
                ActiveModeId = ActiveModeId,
                UnlockedModeIds = new List<ModeId>(UnlockedModeIds)
            };

            foreach (var pair in _elapsedByMode)
                clone.SetElapsedSeconds(pair.Key, pair.Value);

            return clone;
        }
    }
}
