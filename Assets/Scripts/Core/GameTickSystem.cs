using System.Collections.Generic;
using IdleTower.Core.Events;

namespace IdleTower.Core
{
    public class GameTickSystem
    {
        private readonly GameServices _services;
        private readonly List<ITickable> _tickables = new();
        private float _accumulator;
        private ulong _currentTick;

        public ulong CurrentTick => _currentTick;
        public float Accumulator => _accumulator;

        public GameTickSystem(GameServices services)
        {
            _services = services;
        }

        public void RegisterTickable(ITickable tickable)
        {
            if (tickable != null && !_tickables.Contains(tickable))
                _tickables.Add(tickable);
        }

        public void ClearTickables()
        {
            _tickables.Clear();
        }

        public void ProcessUpdate(float deltaTime)
        {
            var balance = _services.Balance;
            if (balance == null || deltaTime <= 0f)
                return;

            var tickInterval = balance.TickInterval;
            var maxTicksPerFrame = balance.MaxTicksPerFrame;

            _accumulator += deltaTime;

            var ticksThisFrame = 0;
            while (_accumulator >= tickInterval && ticksThisFrame < maxTicksPerFrame)
            {
                _accumulator -= tickInterval;
                ProcessSingleTick(balance.TicksPerSecond, tickInterval);
                ticksThisFrame++;
            }
        }

        public void SimulateForSeconds(float elapsedSeconds)
        {
            var balance = _services.Balance;
            if (balance == null || elapsedSeconds <= 0f)
                return;

            var tickInterval = balance.TickInterval;
            var ticksToRun = (int)(elapsedSeconds / tickInterval);

            for (var i = 0; i < ticksToRun; i++)
                ProcessSingleTick(balance.TicksPerSecond, tickInterval);
        }

        private void ProcessSingleTick(int ticksPerSecond, float tickInterval)
        {
            _currentTick++;
            var context = new TickContext(_currentTick, tickInterval, ticksPerSecond, _services);

            foreach (var tickable in _tickables)
                tickable.OnTick(context);

            GameEvents.RaiseGameTick(context);
        }
    }
}
