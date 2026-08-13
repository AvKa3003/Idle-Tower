using System;
using IdleTower.Data.Definitions;

namespace IdleTower.Data.Definitions
{
    /// <summary>Типизированный Id клетки карты (контент / сейв later).</summary>
    public readonly struct MapCellId : IEquatable<MapCellId>
    {
        private readonly string _value;

        public MapCellId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("MapCellId не может быть пустым.", nameof(value));

            _value = value.Trim();
        }

        public static MapCellId Empty => default;

        public static MapCellId FromSerialized(string value)
            => string.IsNullOrWhiteSpace(value) ? Empty : new MapCellId(value);

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public string Value => _value ?? string.Empty;

        public bool Equals(MapCellId other)
            => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj)
            => obj is MapCellId other && Equals(other);

        public override int GetHashCode()
            => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        public override string ToString() => Value;

        public static bool operator ==(MapCellId left, MapCellId right) => left.Equals(right);

        public static bool operator !=(MapCellId left, MapCellId right) => !left.Equals(right);
    }
}
