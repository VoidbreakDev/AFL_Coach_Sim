// Engine/Simulation/DeterministicRandom.cs
using System;

namespace AFLCoachSim.Core.Engine.Simulation
{
    public sealed class DeterministicRandom : IRandom
    {
        private readonly Random _rng;
        public DeterministicRandom(int seed) => _rng = new Random(seed);
        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public double NextDouble() => _rng.NextDouble();

        public float NextFloat()
        {
            // If you already have NextDouble(), wrap it:
            return (float)NextDouble();
        }
    }
}