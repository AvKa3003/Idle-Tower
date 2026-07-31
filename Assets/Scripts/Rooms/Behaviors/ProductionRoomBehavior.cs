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
                throw new InvalidOperationException(
                    $"[ProductionRoomBehavior] '{name}': нет ProductionBehaviorState на этаже {context.RoomIndex}.");

            if (Modes.Length == 0)
                throw new InvalidOperationException($"[ProductionRoomBehavior] '{name}': Modes пуст.");

            for (var i = 0; i < Modes.Length; i++)
            {
                var mode = Modes[i];
                if (i == 0 || mode.UnlockedByDefault)
                    state.UnlockMode(mode.Id);
            }

            state.ActiveModeId = Modes[0].Id;
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
            if (state == null)
                throw new InvalidOperationException(
                    $"[ProductionRoomBehavior] '{name}': нет ProductionBehaviorState на этаже {context.RoomIndex}.");

            var mode = GetModeOrThrow(state.ActiveModeId);
            var duration = mode.CycleDuration.TotalSeconds;
            if (duration <= 0f)
                return;

            var resources = context.Services.Resources;
            var canAfford = mode.HasCraftInput
                ? resources.CanAfford(mode.InputPerCycle)
                : true;

            var elapsed = state.GetElapsedSeconds(mode.Id);
            if (SimulationTimer.AdvanceCycle(ref elapsed, tickContext.TickDelta, duration, canAfford))
            {
                if (mode.HasCraftInput)
                    resources.TrySpend(mode.InputPerCycle);

                resources.Add(mode.OutputPerCycle);
            }

            state.SetElapsedSeconds(mode.Id, elapsed);
        }

        public override RoomBehaviorState CreateDefaultState()
            => new ProductionBehaviorState();

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
            if (state == null)
                throw new InvalidOperationException(
                    $"[ProductionRoomBehavior] '{name}': нет ProductionBehaviorState на этаже {context.RoomIndex}.");

            var mode = GetModeOrThrow(state.ActiveModeId);
            var duration = mode.CycleDuration.TotalSeconds;
            var resources = context.Services.Resources;
            var canAfford = mode.HasCraftInput
                ? resources.CanAfford(mode.InputPerCycle)
                : true;
            var elapsed = state.GetElapsedSeconds(mode.Id);

            var info = new RoomStatusInfo
            {
                ModeLabel = string.IsNullOrEmpty(mode.DisplayName) ? mode.Id.Value : mode.DisplayName,
                InputPerCycle = mode.InputPerCycle,
                OutputPerCycle = mode.OutputPerCycle,
                CycleSummary = ResourceTextFormat.FormatCycle(mode.InputPerCycle, mode.OutputPerCycle),
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

        public bool TryUnlockMode(RoomBehaviorContext context, ModeId modeId)
        {
            var state = GetState(context);
            if (state == null || !TryGetMode(modeId, out var mode))
                return false;

            if (state.IsModeUnlocked(mode.Id))
                return false;

            var unlockTree = context.Services.UnlockTree;
            if (!AreModeUnlockRulesMet(mode, unlockTree, context.Tower))
                return false;

            var resources = context.Services.Resources;
            if (!resources.CanAfford(mode.UnlockCost))
                return false;

            if (!resources.TrySpend(mode.UnlockCost))
                return false;

            state.UnlockMode(mode.Id);
            GameEvents.RaiseOperationModeUnlocked(context.RoomIndex, mode.Id);
            return true;
        }

        public bool TrySetMode(RoomBehaviorContext context, ModeId modeId)
        {
            var state = GetState(context);
            if (state == null || !TryGetMode(modeId, out var mode))
                return false;

            if (!state.IsModeUnlocked(mode.Id))
                return false;

            if (state.ActiveModeId == mode.Id)
                return true;

            state.ActiveModeId = mode.Id;
            GameEvents.RaiseProductionModeChanged(context.RoomIndex, mode.Id);
            return true;
        }

        public IReadOnlyList<OperationModeInfo> GetOperationModes(RoomBehaviorContext context)
        {
            var state = GetState(context);
            if (state == null)
                throw new InvalidOperationException(
                    $"[ProductionRoomBehavior] '{name}': нет ProductionBehaviorState на этаже {context.RoomIndex}.");

            var unlockTree = context.Services.UnlockTree;
            var resources = context.Services.Resources;
            var list = new List<OperationModeInfo>(Modes.Length);

            for (var i = 0; i < Modes.Length; i++)
            {
                var mode = Modes[i];
                var rulesMet = AreModeUnlockRulesMet(mode, unlockTree, context.Tower);
                var isUnlocked = state.IsModeUnlocked(mode.Id);

                list.Add(new OperationModeInfo
                {
                    ModeId = mode.Id,
                    Mode = mode,
                    IsUnlocked = isUnlocked,
                    IsActive = state.ActiveModeId == mode.Id,
                    RulesMet = rulesMet,
                    CanAffordUnlock = isUnlocked || (rulesMet && resources.CanAfford(mode.UnlockCost))
                });
            }

            return list;
        }

        private OperationMode GetModeOrThrow(ModeId modeId)
        {
            if (!TryGetMode(modeId, out var mode))
            {
                throw new InvalidOperationException(
                    $"[ProductionRoomBehavior] '{name}': режим '{modeId.Value}' не найден в Modes.");
            }

            return mode;
        }

        private bool TryGetMode(ModeId modeId, out OperationMode mode)
        {
            mode = null;
            if (modeId.IsEmpty)
                return false;

            for (var i = 0; i < Modes.Length; i++)
            {
                if (Modes[i].Id != modeId)
                    continue;

                mode = Modes[i];
                return true;
            }

            return false;
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
                if (!state.IsModeUnlocked(Modes[i].Id))
                    return true;
            }

            return false;
        }

        [Serializable]
        private class ProductionStateDto
        {
            public string activeModeId;
            public string[] unlockedModeIds = Array.Empty<string>();
            public string[] elapsedModeIds = Array.Empty<string>();
            public float[] elapsedSeconds = Array.Empty<float>();

            public static ProductionStateDto FromState(ProductionBehaviorState state)
            {
                var unlocked = state.UnlockedModeIds;
                var unlockedIds = new string[unlocked.Count];
                for (var i = 0; i < unlocked.Count; i++)
                    unlockedIds[i] = unlocked[i].Value;

                var dto = new ProductionStateDto
                {
                    activeModeId = state.ActiveModeId.Value,
                    unlockedModeIds = unlockedIds
                };

                var elapsed = state.ElapsedByMode;
                dto.elapsedModeIds = new string[elapsed.Count];
                dto.elapsedSeconds = new float[elapsed.Count];

                var index = 0;
                foreach (var pair in elapsed)
                {
                    dto.elapsedModeIds[index] = pair.Key.Value;
                    dto.elapsedSeconds[index] = pair.Value;
                    index++;
                }

                return dto;
            }

            public ProductionBehaviorState ToState()
            {
                var unlocked = new List<ModeId>();
                if (unlockedModeIds != null)
                {
                    for (var i = 0; i < unlockedModeIds.Length; i++)
                        unlocked.Add(ModeId.FromSerialized(unlockedModeIds[i]));
                }

                var state = new ProductionBehaviorState
                {
                    ActiveModeId = ModeId.FromSerialized(activeModeId),
                    UnlockedModeIds = unlocked
                };

                if (elapsedModeIds == null || elapsedSeconds == null)
                    return state;

                var count = Mathf.Min(elapsedModeIds.Length, elapsedSeconds.Length);
                for (var i = 0; i < count; i++)
                    state.SetElapsedSeconds(ModeId.FromSerialized(elapsedModeIds[i]), elapsedSeconds[i]);

                return state;
            }
        }
    }
}
