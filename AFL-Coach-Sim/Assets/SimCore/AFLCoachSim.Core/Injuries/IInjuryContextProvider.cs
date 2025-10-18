using AFLCoachSim.Core.Injuries.Domain;

namespace AFLCoachSim.Core.Injuries
{
    /// <summary>
    /// Interface for providing contextual information about injury events
    /// </summary>
    public interface IInjuryContextProvider
    {
        /// <summary>
        /// Get contextual information for an injury event
        /// </summary>
        InjuryContext GetInjuryContext(int playerId, InjuryType injuryType, InjurySeverity severity);
    }
}