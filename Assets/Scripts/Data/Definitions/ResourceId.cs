using System;

namespace IdleTower.Data.Definitions
{
    /// <summary>
    /// Типизированный Id ресурса для сейва и API.
    /// Runtime-ключ кошелька — <see cref="ResourceDefinition"/>; в JSON — <see cref="Value"/>.
    /// </summary>
    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        private readonly string _value;

        public ResourceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ResourceId не может быть пустым.", nameof(value));

            _value = value.Trim();
        }

        public static ResourceId Empty => default;

        public static ResourceId FromSerialized(string value)
            => string.IsNullOrWhiteSpace(value) ? Empty : new ResourceId(value);

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public string Value => _value ?? string.Empty;

        public bool Equals(ResourceId other)
            => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj)
            => obj is ResourceId other && Equals(other);

        public override int GetHashCode()
            => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        public override string ToString() => Value;

        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);

        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
    }
}
