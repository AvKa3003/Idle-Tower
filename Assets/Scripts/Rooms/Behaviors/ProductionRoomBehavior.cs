using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Rooms;
using IdleTower.Rooms.Production;
using IdleTower.Systems;
using UnityEngine;

namespace IdleTower.Rooms.Behaviors
{
    [CreateAssetMenu(fileName = "ProductionRoom", menuName = "IdleTower/Production Room Behavior")]
    public class ProductionRoomBehavior : RoomBehaviorBase
    {
        [SerializeField] private OperationMode[] modes;

        public OperationMode[] Modes => modes ?? Array.Empty<OperationMode>();

        public override void OnRoomBuilt(RoomBehaviorContext context)
        {
            var state = GetState(context);
            if (state == null)
                return;

            for (var i = 0; i < Modes.Length; i++)
            {
                if (i == 0 || Modes[i].UnlockedByDefault)
                    state.UnlockMode(i);
            }

            state.ActiveModeIndex = 0;
        }

        public override RoomClickResult OnRoomClicked(RoomBehaviorContext context)
        {
            if (Modes.Length <= 1 && !HasLockedModes(context))
                return RoomClickResult.None;

            return new RoomClickResult(RoomUiId.ProductionMode);
        }

        public override void Tick(RoomBehaviorContext context, TickContext tickContext)
        {
            var state = GetState(context);
            if (state == null || Modes.Length == 0)
                return;

            var modeIndex = Mathf.Clamp(state.ActiveModeIndex, 0, Modes.Length - 1);
            state.ActiveModeIndex = modeIndex;

            var mode = Modes[modeIndex];
            var duration = mode.CycleDuration.TotalSeconds;
            if (duration <= 0f)
                return;

            var resources = context.Services.Resources;
            var canAfford = mode.HasCraftInput
                ? resources.CanAfford(mode.InputPerCycle)
                : true;

            var elapsed = state.ActiveElapsedSeconds;
            if (SimulationTimer.AdvanceCycle(ref elapsed, tickContext.TickDelta, duration, canAfford))
            {
                if (mode.HasCraftInput)
                    resources.TrySpend(mode.InputPerCycle);

                resources.Add(mode.OutputPerCycle);
            }

            state.ActiveElapsedSeconds = elapsed;
        }

        public override RoomBehaviorState CreateDefaultState()
            => new ProductionBehaviorState { ActiveModeIndex = 0 };

        public override string SerializeState(RoomBehaviorState state)
        {
            if (state is not ProductionBehaviorState productionState)
                return string.Empty;

            var data = ProductionStateDto.FromState(productionState);
            return JsonUtility.ToJson(data);
        }

        public override RoomBehaviorState DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json))
                return CreateDefaultState();

            var data = JsonUtility.FromJson<ProductionStateDto>(json);
            return data?.ToState() ?? CreateDefaultState();
        }

        public override RoomStatusInfo GetRoomStatusInfo(RoomBehaviorContext context)
        {
            var state = GetState(context);
            if (state == null || Modes.Length == 0)
                return new RoomStatusInfo();

            var modeIndex = Mathf.Clamp(state.ActiveModeIndex, 0, Modes.Length - 1);
            var mode = Modes[modeIndex];
            var duration = mode.CycleDuration.TotalSeconds;
            var resources = context.Services.Resources;
            var canAfford = mode.HasCraftInput
                ? resources.CanAfford(mode.InputPerCycle)
                : true;
            var elapsed = state.ActiveElapsedSeconds;

            var info = new RoomStatusInfo
            {
                ModeLabel = mode.DisplayName,
                OutputPerCycle = mode.OutputPerCycle,
                Progress01 = SimulationTimer.GetProgress01(elapsed, duration, canAfford),
                ElapsedSeconds = elapsed,
                CycleTotalSeconds = duration,
                WaitingForInput = mode.HasCraftInput && !canAfford
            };

            var outputs = mode.OutputPerCycle;
            if (outputs.Length > 0 && outputs[0].Resource != null)
            {
                info.OutputIcon = outputs[0].Resource.Icon;
                info.AmountPerCycle = outputs[0].Amount;
            }

            return info;
        }

        public bool TryUnlockMode(RoomBehaviorContext context, int modeIndex)
        {
            var state = GetState(context);
            if (state == null || modeIndex < 0 || modeIndex >= Modes.Length)
                return false;

            if (state.IsModeUnlocked(modeIndex))
                return false;

            var mode = Modes[modeIndex];
            var unlockTree = context.Services.UnlockTree;
            if (!AreModeUnlockRulesMet(mode, unlockTree, context.Tower))
                return false;

            var resources = context.Services.Resources;
            if (!resources.CanAfford(mode.UnlockCost))
                return false;

            if (!resources.TrySpend(mode.UnlockCost))
                return false;

            state.UnlockMode(modeIndex);
            GameEvents.RaiseOperationModeUnlocked(context.RoomIndex, modeIndex);
            return true;
        }

        public bool TrySetMode(RoomBehaviorContext context, int modeIndex)
        {
            var state = GetState(context);
            if (state == null || modeIndex < 0 || modeIndex >= Modes.Length)
                return false;

            if (!state.IsModeUnlocked(modeIndex))
                return false;

            if (state.ActiveModeIndex == modeIndex)
                return true;

            state.ActiveModeIndex = modeIndex;
            GameEvents.RaiseProductionModeChanged(context.RoomIndex, modeIndex);
            return true;
        }

        public IReadOnlyList<OperationModeInfo> GetOperationModes(RoomBehaviorContext context)
        {
            var state = GetState(context);
            if (state == null || Modes.Length == 0)
                return Array.Empty<OperationModeInfo>();

            var unlockTree = context.Services.UnlockTree;
            var resources = context.Services.Resources;
            var list = new List<OperationModeInfo>(Modes.Length);

            for (var i = 0; i < Modes.Length; i++)
            {
                var mode = Modes[i];
                var rulesMet = AreModeUnlockRulesMet(mode, unlockTree, context.Tower);
                var isUnlocked = state.IsModeUnlocked(i);

                list.Add(new OperationModeInfo
                {
                    ModeIndex = i,
                    Mode = mode,
                    IsUnlocked = isUnlocked,
                    IsActive = state.ActiveModeIndex == i,
                    RulesMet = rulesMet,
                    CanAffordUnlock = isUnlocked || (rulesMet && resources.CanAfford(mode.UnlockCost))
                });
            }

            return list;
        }

        private static bool AreModeUnlockRulesMet(
            OperationMode mode,
            UnlockTreeSystem unlockTree,
            TowerState tower)
        {
            var rules = mode.UnlockRules;
            if (rules == null || rules.Length == 0)
                return true;

            return unlockTree.AreUnlockRulesMet(rules, tower);
        }

        private static ProductionBehaviorState GetState(RoomBehaviorContext context)
            => context.TowerRoom.State as ProductionBehaviorState;

        private bool HasLockedModes(RoomBehaviorContext context)
        {
            var state = GetState(context);
            if (state == null)
                return false;

            for (var i = 0; i < Modes.Length; i++)
            {
                if (!state.IsModeUnlocked(i))
                    return true;
            }

            return false;
        }

        [Serializable]
        private class ProductionStateDto
        {
            public int activeModeIndex;
            public int[] unlockedModeIndices = Array.Empty<int>();
            public int[] elapsedModeIndices = Array.Empty<int>();
            public float[] elapsedSeconds = Array.Empty<float>();

            public static ProductionStateDto FromState(ProductionBehaviorState state)
            {
                var dto = new ProductionStateDto
                {
                    activeModeIndex = state.ActiveModeIndex,
                    unlockedModeIndices = state.UnlockedModeIndices.ToArray()
                };

                var elapsed = state.ElapsedByMode;
                dto.elapsedModeIndices = new int[elapsed.Count];
                dto.elapsedSeconds = new float[elapsed.Count];

                var index = 0;
                foreach (var pair in elapsed)
                {
                    dto.elapsedModeIndices[index] = pair.Key;
                    dto.elapsedSeconds[index] = pair.Value;
                    index++;
                }

                return dto;
            }

            public ProductionBehaviorState ToState()
            {
                var state = new ProductionBehaviorState
                {
                    ActiveModeIndex = activeModeIndex,
                    UnlockedModeIndices = unlockedModeIndices != null
                        ? new List<int>(unlockedModeIndices)
                        : new List<int>()
                };

                if (elapsedModeIndices == null || elapsedSeconds == null)
                    return state;

                var count = Mathf.Min(elapsedModeIndices.Length, elapsedSeconds.Length);
                for (var i = 0; i < count; i++)
                    state.SetElapsedSeconds(elapsedModeIndices[i], elapsedSeconds[i]);

                return state;
            }
        }
    }
}
