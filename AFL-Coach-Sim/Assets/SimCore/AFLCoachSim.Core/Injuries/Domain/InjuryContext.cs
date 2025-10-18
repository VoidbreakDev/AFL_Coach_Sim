using System;

namespace AFLCoachSim.Core.Injuries.Domain
{
    /// <summary>
    /// Contextual information about an injury event
    /// </summary>
    public class InjuryContext
    {
        public int PlayerId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Location { get; set; }
        public string Activity { get; set; }
        public string EnvironmentalFactors { get; set; }
        public IntensityLevel IntensityLevel { get; set; }
        public string InjuryDescription { get; set; }
        public string AdditionalNotes { get; set; }
    }
}