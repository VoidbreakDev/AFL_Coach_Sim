using System;
using System.Collections.Generic;

namespace AFLCoachSim.Core.DTO
{
    /// <summary>
    /// Serializable DTO for player development potential
    /// </summary>
    [Serializable]
    public class DevelopmentPotentialDTO
    {
        public int PlayerId;
        public float OverallPotential;
        public float DevelopmentRate;
        public float InjuryProneness;
        public List<string> AttributePotentialKeys = new List<string>();
        public List<float> AttributePotentialValues = new List<float>();
        public List<int> PreferredTrainingFoci = new List<int>(); // TrainingFocus enum as int
        public string LastUpdated; // DateTime as ISO string
        
        public DevelopmentPotentialDTO() { }
        
        public static DevelopmentPotentialDTO FromDomain(Training.DevelopmentPotential domain)
        {
            if (domain == null) return null;
            
            var dto = new DevelopmentPotentialDTO
            {
                PlayerId = domain.PlayerId,
                OverallPotential = domain.OverallPotential,
                DevelopmentRate = domain.DevelopmentRate,
                InjuryProneness = domain.InjuryProneness,
                LastUpdated = domain.LastUpdated.ToString("O") // ISO 8601
            };
            
            foreach (var kvp in domain.AttributePotentials)
            {
                dto.AttributePotentialKeys.Add(kvp.Key);
                dto.AttributePotentialValues.Add(kvp.Value);
            }
            
            foreach (var focus in domain.PreferredTraining)
            {
                dto.PreferredTrainingFoci.Add((int)focus);
            }
            
            return dto;
        }
        
        public Training.DevelopmentPotential ToDomain()
        {
            var domain = new Training.DevelopmentPotential
            {
                PlayerId = this.PlayerId,
                OverallPotential = this.OverallPotential,
                DevelopmentRate = this.DevelopmentRate,
                InjuryProneness = this.InjuryProneness,
                LastUpdated = DateTime.Parse(this.LastUpdated)
            };
            
            for (int i = 0; i < AttributePotentialKeys.Count && i < AttributePotentialValues.Count; i++)
            {
                domain.AttributePotentials[AttributePotentialKeys[i]] = AttributePotentialValues[i];
            }
            
            foreach (var focusInt in PreferredTrainingFoci)
            {
                domain.PreferredTraining.Add((Training.TrainingFocus)focusInt);
            }
            
            return domain;
        }
    }

    /// <summary>
    /// Serializable DTO for training enrollment
    /// </summary>
    [Serializable]
    public class PlayerTrainingEnrollmentDTO
    {
        public int PlayerId;
        public string ProgramId;
        public string StartDate;
        public string EndDate; // nullable DateTime as string
        public float ProgressPercentage;
        public int SessionsCompleted;
        public int SessionsMissed;
        public List<string> CumulativeGainKeys = new List<string>();
        public List<float> CumulativeGainValues = new List<float>();
        public float TotalFatigueAccumulated;
        public float TotalInjuryRisk;
        public bool IsActive;
        
        public PlayerTrainingEnrollmentDTO() { }
        
        public static PlayerTrainingEnrollmentDTO FromDomain(Training.PlayerTrainingEnrollment domain)
        {
            if (domain == null) return null;
            
            var dto = new PlayerTrainingEnrollmentDTO
            {
                PlayerId = domain.PlayerId,
                ProgramId = domain.ProgramId,
                StartDate = domain.StartDate.ToString("O"),
                EndDate = domain.EndDate?.ToString("O"),
                ProgressPercentage = domain.ProgressPercentage,
                SessionsCompleted = domain.SessionsCompleted,
                SessionsMissed = domain.SessionsMissed,
                TotalFatigueAccumulated = domain.TotalFatigueAccumulated,
                TotalInjuryRisk = domain.TotalInjuryRisk,
                IsActive = domain.IsActive
            };
            
            foreach (var kvp in domain.CumulativeGains)
            {
                dto.CumulativeGainKeys.Add(kvp.Key);
                dto.CumulativeGainValues.Add(kvp.Value);
            }
            
            return dto;
        }
        
        public Training.PlayerTrainingEnrollment ToDomain()
        {
            var domain = new Training.PlayerTrainingEnrollment
            {
                PlayerId = this.PlayerId,
                ProgramId = this.ProgramId,
                StartDate = DateTime.Parse(this.StartDate),
                EndDate = string.IsNullOrEmpty(this.EndDate) ? null : (DateTime?)DateTime.Parse(this.EndDate),
                ProgressPercentage = this.ProgressPercentage,
                SessionsCompleted = this.SessionsCompleted,
                SessionsMissed = this.SessionsMissed,
                TotalFatigueAccumulated = this.TotalFatigueAccumulated,
                TotalInjuryRisk = this.TotalInjuryRisk,
                IsActive = this.IsActive
            };
            
            for (int i = 0; i < CumulativeGainKeys.Count && i < CumulativeGainValues.Count; i++)
            {
                domain.CumulativeGains[CumulativeGainKeys[i]] = CumulativeGainValues[i];
            }
            
            return domain;
        }
    }

    /// <summary>
    /// Serializable DTO for training sessions
    /// </summary>
    [Serializable]
    public class TrainingSessionDTO
    {
        public string Id;
        public string ProgramId;
        public string ScheduledDate;
        public string CompletedDate; // nullable DateTime as string
        public int Intensity; // TrainingIntensity as int
        public List<int> ParticipatingPlayers = new List<int>();
        public string Notes;
        public bool IsCompleted;
        
        // Simplified outcomes - full outcomes would be too complex for simple persistence
        public List<int> OutcomePlayerIds = new List<int>();
        public List<float> OutcomeInjuryRisks = new List<float>();
        public List<float> OutcomeFatigueAccumulations = new List<float>();
        
        public TrainingSessionDTO() { }
        
        public static TrainingSessionDTO FromDomain(Training.TrainingSession domain)
        {
            if (domain == null) return null;
            
            var dto = new TrainingSessionDTO
            {
                Id = domain.Id,
                ProgramId = domain.ProgramId,
                ScheduledDate = domain.ScheduledDate.ToString("O"),
                CompletedDate = domain.CompletedDate?.ToString("O"),
                Intensity = (int)domain.Intensity,
                ParticipatingPlayers = new List<int>(domain.ParticipatingPlayers),
                Notes = domain.Notes,
                IsCompleted = domain.IsCompleted
            };
            
            // Store simplified outcomes
            foreach (var outcome in domain.Outcomes)
            {
                dto.OutcomePlayerIds.Add(outcome.Key);
                dto.OutcomeInjuryRisks.Add(outcome.Value.InjuryRisk);
                dto.OutcomeFatigueAccumulations.Add(outcome.Value.FatigueAccumulation);
            }
            
            return dto;
        }
        
        public Training.TrainingSession ToDomain()
        {
            var domain = new Training.TrainingSession
            {
                Id = this.Id,
                ProgramId = this.ProgramId,
                ScheduledDate = DateTime.Parse(this.ScheduledDate),
                CompletedDate = string.IsNullOrEmpty(this.CompletedDate) ? null : (DateTime?)DateTime.Parse(this.CompletedDate),
                Intensity = (Training.TrainingIntensity)this.Intensity,
                ParticipatingPlayers = new List<int>(this.ParticipatingPlayers),
                Notes = this.Notes,
                IsCompleted = this.IsCompleted
            };
            
            // Restore simplified outcomes (attribute gains would be lost - acceptable for basic persistence)
            for (int i = 0; i < OutcomePlayerIds.Count && i < OutcomeInjuryRisks.Count && i < OutcomeFatigueAccumulations.Count; i++)
            {
                var outcome = new Training.TrainingOutcome
                {
                    InjuryRisk = OutcomeInjuryRisks[i],
                    FatigueAccumulation = OutcomeFatigueAccumulations[i]
                };
                domain.Outcomes[OutcomePlayerIds[i]] = outcome;
            }
            
            return domain;
        }
    }

    /// <summary>
    /// Serializable DTO for training efficiency tracking
    /// </summary>
    [Serializable]
    public class TrainingEfficiencyDTO
    {
        public int PlayerId;
        public int TrainingType; // TrainingType enum as int
        public int Focus; // TrainingFocus enum as int
        public float EfficiencyRating;
        public int SessionsCompleted;
        public float AverageGain;
        public float InjuryIncidence;
        public string LastMeasured;
        
        public TrainingEfficiencyDTO() { }
        
        public static TrainingEfficiencyDTO FromDomain(Training.TrainingEfficiency domain)
        {
            if (domain == null) return null;
            
            return new TrainingEfficiencyDTO
            {
                PlayerId = domain.PlayerId,
                TrainingType = (int)domain.TrainingType,
                Focus = (int)domain.Focus,
                EfficiencyRating = domain.EfficiencyRating,
                SessionsCompleted = domain.SessionsCompleted,
                AverageGain = domain.AverageGain,
                InjuryIncidence = domain.InjuryIncidence,
                LastMeasured = domain.LastMeasured.ToString("O")
            };
        }
        
        public Training.TrainingEfficiency ToDomain()
        {
            return new Training.TrainingEfficiency
            {
                PlayerId = this.PlayerId,
                TrainingType = (Training.TrainingType)this.TrainingType,
                Focus = (Training.TrainingFocus)this.Focus,
                EfficiencyRating = this.EfficiencyRating,
                SessionsCompleted = this.SessionsCompleted,
                AverageGain = this.AverageGain,
                InjuryIncidence = this.InjuryIncidence,
                LastMeasured = DateTime.Parse(this.LastMeasured)
            };
        }
    }

    /// <summary>
    /// Container DTO for all training data persistence
    /// </summary>
    [Serializable]
    public class TrainingDataDTO
    {
        public string Version = "1.0";
        public string SavedAt;
        public List<DevelopmentPotentialDTO> PlayerPotentials = new List<DevelopmentPotentialDTO>();
        public List<PlayerTrainingEnrollmentDTO> Enrollments = new List<PlayerTrainingEnrollmentDTO>();
        public List<TrainingSessionDTO> CompletedSessions = new List<TrainingSessionDTO>();
        public List<TrainingSessionDTO> ScheduledSessions = new List<TrainingSessionDTO>();
        public List<TrainingEfficiencyDTO> EfficiencyHistory = new List<TrainingEfficiencyDTO>();
        
        public TrainingDataDTO()
        {
            SavedAt = DateTime.Now.ToString("O");
        }
    }
}