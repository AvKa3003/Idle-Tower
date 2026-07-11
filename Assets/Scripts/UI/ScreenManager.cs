using System.Collections.Generic;
using IdleTower.Core;
using UnityEngine;

namespace IdleTower.UI
{
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
        }

        public void Hide(ScreenId id)
        {
            if (_screens.TryGetValue(id, out var screen))
                screen.Hide();
        }
    }
}
