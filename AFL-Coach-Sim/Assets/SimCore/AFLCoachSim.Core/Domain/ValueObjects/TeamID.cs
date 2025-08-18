// Domain/ValueObjects/TeamId.cs
namespace AFLCoachSim.Core.Domain.ValueObjects
{
    public readonly struct TeamId
    {
        public int Value { get; }
        public TeamId(int value) => Value = value;
        public override string ToString() => Value.ToString();
    }
}