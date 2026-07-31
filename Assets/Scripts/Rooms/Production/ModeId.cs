using System;

namespace IdleTower.Rooms.Production
{
    /// <summary>
    /// Типизированный идентификатор режима производства.
    /// В рантайме не путать с <c>RoomId</c> / <c>ResourceId</c> / произвольными string.
    /// В сейве и Unity-полях — string через <see cref="Value"/> / <see cref="FromSerialized"/>.
    /// </summary>
    public readonly struct ModeId : IEquatable<ModeId>
    {
        private readonly string _value;

        public ModeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ModeId не может быть пустым.", nameof(value));

            _value = value.Trim();
        }

        /// <summary>Пустой Id (не назначен). Для десериализации до валидации.</summary>
        public static ModeId Empty => default;

        public static ModeId FromSerialized(string value)
            => string.IsNullOrWhiteSpace(value) ? Empty : new ModeId(value);

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public string Value => _value ?? string.Empty;

        public bool Equals(ModeId other)
            => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj)
            => obj is ModeId other && Equals(other);

        public override int GetHashCode()
            => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        public override string ToString() => Value;

        public static bool operator ==(ModeId left, ModeId right) => left.Equals(right);

        public static bool operator !=(ModeId left, ModeId right) => !left.Equals(right);
    }
}
