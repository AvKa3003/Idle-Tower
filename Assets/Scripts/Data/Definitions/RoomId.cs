using System;

namespace IdleTower.Data.Definitions
{
    /// <summary>
    /// Типизированный Id комнаты для сейва и API.
    /// Runtime-ключ башни — <see cref="RoomDefinition"/>; в JSON — <see cref="Value"/>.
    /// </summary>
    public readonly struct RoomId : IEquatable<RoomId>
    {
        private readonly string _value;

        public RoomId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("RoomId не может быть пустым.", nameof(value));

            _value = value.Trim();
        }

        public static RoomId Empty => default;

        public static RoomId FromSerialized(string value)
            => string.IsNullOrWhiteSpace(value) ? Empty : new RoomId(value);

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public string Value => _value ?? string.Empty;

        public bool Equals(RoomId other)
            => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj)
            => obj is RoomId other && Equals(other);

        public override int GetHashCode()
            => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        public override string ToString() => Value;

        public static bool operator ==(RoomId left, RoomId right) => left.Equals(right);

        public static bool operator !=(RoomId left, RoomId right) => !left.Equals(right);
    }
}
