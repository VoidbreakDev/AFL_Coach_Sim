using System;

namespace AFLCoachSim.Core.Domain.ValueObjects
{
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public int Value { get; }
        public PlayerId(int value) { Value = value; }
        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId o && Equals(o);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
        public static implicit operator int(PlayerId id) => id.Value;
        public static implicit operator string(PlayerId id) => id.Value.ToString();
        
        // Guid conversion operators for compatibility
        public static implicit operator Guid(PlayerId id) => new Guid(id.Value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static explicit operator PlayerId(Guid guid) => new PlayerId(guid.GetHashCode());
        
        // String parsing methods for compatibility
        public static PlayerId FromString(string value) => int.TryParse(value, out int intValue) ? new PlayerId(intValue) : new PlayerId(value.GetHashCode());
        public static implicit operator PlayerId(string value) => FromString(value);
        
        // Factory method for creating from Guid
        public static PlayerId FromGuid(Guid guid) => new PlayerId(guid.GetHashCode());
        
        // Explicit equality operators to resolve ambiguity
        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);
        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }
}
