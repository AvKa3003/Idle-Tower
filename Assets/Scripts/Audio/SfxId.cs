namespace IdleTower.Audio
{
    /// <summary>
    /// Идентификаторы SFX. Новый звук = новое значение + клип в SfxPlayer.bindings + строка подписки (если новый факт).
    /// </summary>
    public enum SfxId
    {
        RoomBuilt = 0,
        ModeChanged = 1,
        ModeUnlocked = 2
    }
}
