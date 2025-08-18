// Engine/Simulation/IRandom.cs
namespace AFLCoachSim.Core.Engine.Simulation
{
    public interface IRandom
    {
        int NextInt(int minInclusive, int maxExclusive);
        double NextDouble(); // [0,1)
    }
}