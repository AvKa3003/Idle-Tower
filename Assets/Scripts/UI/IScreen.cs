using IdleTower.Core;

namespace IdleTower.UI
{
    public interface IScreen
    {
        ScreenId Id { get; }
        void Show();
        void Hide();
    }
}
