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
    }
}