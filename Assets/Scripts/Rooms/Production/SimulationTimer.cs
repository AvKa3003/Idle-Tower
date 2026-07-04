namespace IdleTower.Rooms.Production
{
    /// <summary>Логика таймера операции (производство и крафт — одна операция, см. architecture п. 3.4).</summary>
    public static class SimulationTimer
    {
        /// <summary>
        /// Шаг цикла операции. Производство: canAffordInput = true.
        /// Крафт: canAffordInput = хватает ли InputPerCycle.
        /// Возвращает true, если цикл завершён (пора spend + output).
        /// </summary>
        public static bool AdvanceCycle(
            ref float elapsedSeconds,
            float tickDelta,
            float cycleDuration,
            bool canAffordInput)
        {
            if (!canAffordInput)
            {
                elapsedSeconds = 0f;
                return false;
            }

            elapsedSeconds += tickDelta;

            if (elapsedSeconds < cycleDuration)
                return false;

            elapsedSeconds = 0f;
            return true;
        }

        public static float GetProgress01(float elapsedSeconds, float cycleDuration, bool canAffordInput)
        {
            if (cycleDuration <= 0f)
                return 0f;

            if (!canAffordInput)
                return 0f;

            return elapsedSeconds / cycleDuration;
        }
    }
}
