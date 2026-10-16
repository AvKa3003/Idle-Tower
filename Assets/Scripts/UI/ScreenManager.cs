using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using UnityEngine;

namespace IdleTower.UI
{
    /// <summary>
    /// Переключает только зарегистрированные IScreen (их root).
    /// Не трогает UI_Root / общий хром — экраны не должны Hide-ить весь UI_Root.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        private readonly Dictionary<ScreenId, IScreen> _screens = new();

        public void Register(IScreen screen)
        {
            if (screen == null)
                return;

            _screens[screen.Id] = screen;
        }

        public void Show(ScreenId id)
        {
            foreach (var pair in _screens)
                pair.Value.Hide();

            if (_screens.TryGetValue(id, out var screen))
                screen.Show();

            GameEvents.RaiseScreenChanged(id);
        }

        public void Hide(ScreenId id)
        {
            if (_screens.TryGetValue(id, out var screen))
                screen.Hide();
        }
    }
}
