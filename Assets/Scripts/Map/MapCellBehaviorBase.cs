using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using UnityEngine;

namespace IdleTower.Map
{
    public abstract class MapCellBehaviorBase : ScriptableObject
    {
        /// <summary>Распахивает ли клетка frontier, когда сама Interactive (без runtime).</summary>
        public abstract bool RevealsNeighborsWhenInteractive { get; }

        /// <summary>Учёт runtime (например Captured у рейда).</summary>
        public virtual bool ShouldRevealNeighbors(MapCellRuntime runtime)
            => RevealsNeighborsWhenInteractive;

        /// <summary>Спрайт вместо Definition.Sprite; null — брать из definition.</summary>
        public virtual Sprite GetDisplaySprite(MapCellRuntime runtime) => null;

        [SerializeField] private bool hasFunctionalClick = true;

        /// <summary>
        /// Игровой/UI функционал по клику (рейд, лут, переход).
        /// Без галочки — нет затемнения «ещё не в зоне интеракции»; позже можно добавить чисто визуальный клик.
        /// </summary>
        public virtual bool HasFunctionalClick => hasFunctionalClick;

        /// <summary>Кнопка активна при Interactive только если HasFunctionalClick.</summary>
        public virtual bool AcceptsClick => HasFunctionalClick;

        public abstract MapCellClickResult OnClicked(MapCellBehaviorContext context);

        public virtual MapCellRuntimeState CreateDefaultState() => MapCellRuntimeState.Empty;

        public virtual string SerializeState(MapCellRuntimeState state) => string.Empty;

        public virtual MapCellRuntimeState DeserializeState(string json) => MapCellRuntimeState.Empty;
    }
}
