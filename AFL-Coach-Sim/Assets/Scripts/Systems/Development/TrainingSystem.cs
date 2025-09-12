using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Comprehensive training system with programs, drills, and effectiveness tracking
    /// </summary>
    [System.Serializable]
    public class TrainingProgram
    {
        [Header("Program Details")]
        public string Name;
        public TrainingFocus FocusType;
        public TrainingIntensity IntensityLevel;

        [Header("Effectiveness")]
        [Range(0.5f, 2.0f)] public float Intensity = 1.0f;        // Training load multiplier
        [Range(0.1f, 1.0f)] public float InjuryRisk = 0.1f;       // Risk of training injuries
        [Range(1, 12)] public int DurationWeeks = 4;              // How long the program runs

        [Header("Focus Areas")]
        public TrainingFocusWeights FocusWeights;

        [Header("Requirements")]
        public List<TrainingFacility> RequiredFacilities;
        public int MinimumCoachRating = 60;
        public int WeeklyHours = 20;

        public TrainingProgram()
        {
            Name = "Basic Training";
            FocusType = TrainingFocus.General;
            IntensityLevel = TrainingIntensity.Moderate;
            FocusWeights = new TrainingFocusWeights();
            RequiredFacilities = new List<TrainingFacility>();
        }

        /// <summary>
        /// Calculates how effective this training program is for a specific player
        /// </summary>
        public float GetEffectivenessMultiplier(Player player)
        {
            float effectiveness = 1.0f;

            // Age affects response to training
            effectiveness *= CalculateAgeEffectiveness(player.Age);

            // Player's current fitness affects training response
            effectiveness *= CalculateFitnessEffectiveness(player.Stamina);

            // Match focus type to player's role
            effectiveness *= CalculateRoleMatch(player.Role);

            // Morale affects training engagement
            effectiveness *= CalculateMoraleEffect(player.Morale);

            // Apply intensity modifier
            effectiveness *= Intensity;

            return Mathf.Clamp(effectiveness, 0.2f, 3.0f);
        }

        /// <summary>
        /// Calculates injury risk for a player in this program
        /// </summary>
        public float GetInjuryRisk(Player player)
        {
            float risk = InjuryRisk;

            // Age increases injury risk
            if (player.Age > 30) risk *= 1.3f;
            else if (player.Age < 20) risk *= 1.1f; // Young players also slightly more prone

            // Poor condition increases risk
            if (player.Stamina < 70) risk *= 1.2f;

            // High intensity increases risk
            if (IntensityLevel == TrainingIntensity.VeryHigh) risk *= 1.5f;
            else if (IntensityLevel == TrainingIntensity.High) risk *= 1.2f;

            return Mathf.Clamp(risk, 0.01f, 0.5f);
        }

        private float CalculateAgeEffectiveness(int age)
        {
            if (age < 20) return 1.3f;       // Young players respond very well
            if (age < 25) return 1.1f;       // Prime learning age
            if (age < 30) return 1.0f;       // Normal response
            if (age < 33) return 0.8f;       // Declining response
            return 0.6f;                     // Veterans respond poorly
        }

        private float CalculateFitnessEffectiveness(float stamina)
        {
            if (stamina < 50) return 0.7f;   // Poor fitness limits training
            if (stamina < 70) return 0.9f;   // Below average fitness
            if (stamina > 85) return 1.1f;   // Excellent fitness enhances training
            return 1.0f;                     // Average fitness
        }

        private float CalculateRoleMatch(PlayerRole role)
        {
            // Different training types suit different positions better
            switch (FocusType)
            {
                case TrainingFocus.Skills:
                    return IsSkillIntensiveRole(role) ? 1.2f : 0.9f;

                case TrainingFocus.Fitness:
                    return IsEnduranceRole(role) ? 1.3f : 1.0f;

                case TrainingFocus.Defense:
                    return IsDefensiveRole(role) ? 1.4f : 0.8f;

                case TrainingFocus.Tactical:
                    return IsLeadershipRole(role) ? 1.2f : 1.0f;

                case TrainingFocus.Recovery:
                    return 1.0f; // All positions benefit equally

                default:
                    return 1.0f;
            }
        }

        private float CalculateMoraleEffect(float morale)
        {
            if (morale > 80) return 1.2f;    // High morale boosts training
            if (morale < 50) return 0.7f;    // Low morale hurts training
            return 1.0f;                     // Average morale
        }

        private bool IsSkillIntensiveRole(PlayerRole role) =>
            role == PlayerRole.Centre || role == PlayerRole.Wing ||
            role == PlayerRole.FullForward || role == PlayerRole.CentreHalfForward;

        private bool IsEnduranceRole(PlayerRole role) =>
            role == PlayerRole.Centre || role == PlayerRole.Wing || role == PlayerRole.Rover;

        private bool IsDefensiveRole(PlayerRole role) =>
            role == PlayerRole.FullBack || role == PlayerRole.CentreHalfBack ||
            role == PlayerRole.BackPocket || role == PlayerRole.HalfBack;

        private bool IsLeadershipRole(PlayerRole role) =>
            role == PlayerRole.Centre || role == PlayerRole.FullBack || role == PlayerRole.FullForward;

        /// <summary>
        /// Creates common training programs with preset configurations
        /// </summary>
        public static List<TrainingProgram> GetDefaultTrainingPrograms()
        {
            return new List<TrainingProgram>
            {
                new TrainingProgram
                {
                    Name = "Skills Development",
                    FocusType = TrainingFocus.Skills,
                    IntensityLevel = TrainingIntensity.Moderate,
                    Intensity = 1.1f,
                    InjuryRisk = 0.05f,
                    DurationWeeks = 6,
                    RequiredFacilities = new List<TrainingFacility> { TrainingFacility.TrainingOvals },
                    MinimumCoachRating = 65,
                    WeeklyHours = 18
                },
                
                new TrainingProgram
                {
                    Name = "Intensive Fitness",
                    FocusType = TrainingFocus.Fitness,
                    IntensityLevel = TrainingIntensity.High,
                    Intensity = 1.4f,
                    InjuryRisk = 0.15f,
                    DurationWeeks = 4,
                    RequiredFacilities = new List<TrainingFacility> { TrainingFacility.AdvancedGym },
                    MinimumCoachRating = 70,
                    WeeklyHours = 25
                },
                
                new TrainingProgram
                {
                    Name = "Defensive Tactics",
                    FocusType = TrainingFocus.Defense,
                    IntensityLevel = TrainingIntensity.Moderate,
                    Intensity = 1.0f,
                    InjuryRisk = 0.08f,
                    DurationWeeks = 5,
                    RequiredFacilities = new List<TrainingFacility> { TrainingFacility.TacticalRoom, TrainingFacility.TrainingOvals },
                    MinimumCoachRating = 75,
                    WeeklyHours = 22
                },
                
                new TrainingProgram
                {
                    Name = "Recovery Program",
                    FocusType = TrainingFocus.Recovery,
                    IntensityLevel = TrainingIntensity.Light,
                    Intensity = 0.7f,
                    InjuryRisk = 0.02f,
                    DurationWeeks = 3,
                    RequiredFacilities = new List<TrainingFacility> { TrainingFacility.RecoveryCenter },
                    MinimumCoachRating = 60,
                    WeeklyHours = 15
                }
            };
        }
    }

    /// <summary>
    /// Manages team training programs and tracks progress
    /// </summary>
    public class TeamTrainingManager : MonoBehaviour
    {
        [Header("Current Programs")]
        [SerializeField] private List<ActiveTrainingProgram> activePrograms;
        
        [Header("Facilities")]
        [SerializeField] private List<TrainingFacility> availableFacilities;
        [SerializeField] private int coachRating = 75;
        
        [Header("Training History")]
        [SerializeField] private List<TrainingSession> sessionHistory;
        
        private void Start()
        {
            activePrograms = new List<ActiveTrainingProgram>();
            sessionHistory = new List<TrainingSession>();
            
            // Initialize with basic facilities
            if (availableFacilities == null || availableFacilities.Count == 0)
            {
                availableFacilities = new List<TrainingFacility>
                {
                    TrainingFacility.BasicGym,
                    TrainingFacility.TrainingOvals
                };
            }
        }
        
        /// <summary>
        /// Starts a new training program for the team
        /// </summary>
        public bool StartTrainingProgram(TrainingProgram program, List<Player> participants)
        {
            // Check if we have required facilities
            if (!HasRequiredFacilities(program))
            {
                Debug.LogWarning($"Cannot start {program.Name} - missing required facilities");
                return false;
            }
            
            // Check coach rating
            if (coachRating < program.MinimumCoachRating)
            {
                Debug.LogWarning($"Coach rating too low for {program.Name}");
                return false;
            }
            
            var activeProgram = new ActiveTrainingProgram
            {
                Program = program,
                Participants = new List<Player>(participants),
                StartDate = DateTime.Now,
                WeeksRemaining = program.DurationWeeks,
                IsActive = true
            };
            
            activePrograms.Add(activeProgram);
            
            Debug.Log($"Started training program: {program.Name} with {participants.Count} players");
            return true;
        }
        
        /// <summary>
        /// Processes one week of training for all active programs
        /// </summary>
        public List<TrainingResult> ProcessWeeklyTraining()
        {
            var results = new List<TrainingResult>();
            
            foreach (var activeProgram in activePrograms.ToArray()) // ToArray to avoid modification during iteration
            {
                if (!activeProgram.IsActive) continue;
                
                var weekResult = ProcessProgramWeek(activeProgram);
                results.Add(weekResult);
                
                // Check if program is complete
                activeProgram.WeeksRemaining--;
                if (activeProgram.WeeksRemaining <= 0)
                {
                    activeProgram.IsActive = false;
                    Debug.Log($"Training program completed: {activeProgram.Program.Name}");
                }
            }
            
            return results;
        }
        
        private TrainingResult ProcessProgramWeek(ActiveTrainingProgram activeProgram)
        {
            var result = new TrainingResult
            {
                ProgramName = activeProgram.Program.Name,
                Week = activeProgram.Program.DurationWeeks - activeProgram.WeeksRemaining + 1,
                PlayerImprovements = new List<PlayerTrainingResult>()
            };
            
            foreach (var player in activeProgram.Participants)
            {
                // Calculate development for this player - need to access Development property
                var development = player.Development?.CalculateDevelopment(player, activeProgram.Program, 1f) ?? new PlayerStatsDelta();
                
                // Check for training injuries
                bool injured = CheckTrainingInjury(player, activeProgram.Program);
                
                var playerResult = new PlayerTrainingResult
                {
                    Player = player,
                    StatChanges = development,
                    WasInjured = injured,
                    EffectivenessRating = activeProgram.Program.GetEffectivenessMultiplier(player)
                };
                
                // Apply the improvements to the player
                if (!injured)
                {
                    development.ApplyTo(player.Stats);
                }
                else
                {
                    Debug.Log($"{player.Name} was injured during training");
                    // TODO: Apply injury to player
                }
                
                result.PlayerImprovements.Add(playerResult);
            }
            
            // Record training session
            var session = new TrainingSession
            {
                Date = DateTime.Now,
                ProgramName = activeProgram.Program.Name,
                ParticipantCount = activeProgram.Participants.Count,
                InjuryCount = result.PlayerImprovements.Count(p => p.WasInjured),
                AverageEffectiveness = result.PlayerImprovements.Average(p => p.EffectivenessRating)
            };
            sessionHistory.Add(session);
            
            return result;
        }
        
        private bool HasRequiredFacilities(TrainingProgram program)
        {
            return program.RequiredFacilities.All(required => 
                availableFacilities.Contains(required));
        }
        
        private bool CheckTrainingInjury(Player player, TrainingProgram program)
        {
            float injuryRisk = program.GetInjuryRisk(player);
            return UnityEngine.Random.Range(0f, 1f) < injuryRisk;
        }
        
        /// <summary>
        /// Gets statistics about training effectiveness
        /// </summary>
        public TrainingAnalytics GetTrainingAnalytics(int weeksBack = 8)
        {
            var recentSessions = sessionHistory
                .Where(s => (DateTime.Now - s.Date).TotalDays <= weeksBack * 7)
                .ToList();
                
            if (recentSessions.Count == 0)
                return new TrainingAnalytics();
                
            return new TrainingAnalytics
            {
                TotalSessions = recentSessions.Count,
                AverageEffectiveness = recentSessions.Average(s => s.AverageEffectiveness),
                TotalInjuries = recentSessions.Sum(s => s.InjuryCount),
                InjuryRate = recentSessions.Average(s => (float)s.InjuryCount / s.ParticipantCount),
                MostUsedProgram = recentSessions.GroupBy(s => s.ProgramName)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "None"
            };
        }
        
        public void AddFacility(TrainingFacility facility)
        {
            if (!availableFacilities.Contains(facility))
            {
                availableFacilities.Add(facility);
                Debug.Log($"Added training facility: {facility}");
            }
        }
        
        public void SetCoachRating(int rating)
        {
            coachRating = Mathf.Clamp(rating, 30, 99);
        }
    }
    
    /// <summary>
    /// Represents an active training program with participants
    /// </summary>
    [System.Serializable]
    public class ActiveTrainingProgram
    {
        public TrainingProgram Program;
        public List<Player> Participants;
        public DateTime StartDate;
        public int WeeksRemaining;
        public bool IsActive;
    }
    
    /// <summary>
    /// Results from a week of training
    /// </summary>
    [System.Serializable]
    public class TrainingResult
    {
        public string ProgramName;
        public int Week;
        public List<PlayerTrainingResult> PlayerImprovements;
        
        public int InjuryCount => PlayerImprovements.Count(p => p.WasInjured);
        public float AverageImprovement => PlayerImprovements.Average(p => p.StatChanges.GetTotalChange());
    }
    
    /// <summary>
    /// Individual player results from training
    /// </summary>
    [System.Serializable]
    public class PlayerTrainingResult
    {
        public Player Player;
        public PlayerStatsDelta StatChanges;
        public bool WasInjured;
        public float EffectivenessRating;
    }
    
    /// <summary>
    /// Historical record of a training session
    /// </summary>
    [System.Serializable]
    public class TrainingSession
    {
        public DateTime Date;
        public string ProgramName;
        public int ParticipantCount;
        public int InjuryCount;
        public float AverageEffectiveness;
    }
    
    /// <summary>
    /// Analytics about training effectiveness over time
    /// </summary>
    [System.Serializable]
    public class TrainingAnalytics
    {
        public int TotalSessions;
        public float AverageEffectiveness;
        public int TotalInjuries;
        public float InjuryRate;
        public string MostUsedProgram;
    }
    
    /// <summary>
    /// Different focuses for training programs
    /// </summary>
    public enum TrainingFocus
    {
        General,        // Balanced improvement
        Skills,         // Kicking, handballing, playmaking
        Fitness,        // Speed, stamina
        Defense,        // Tackling, positioning
        Tactical,       // Knowledge, decision making
        Recovery        // Injury prevention, stamina recovery
    }
    
    /// <summary>
    /// Training intensity levels
    /// </summary>
    public enum TrainingIntensity
    {
        Light,          // 0.7x multiplier, low injury risk
        Moderate,       // 1.0x multiplier, normal injury risk
        High,           // 1.3x multiplier, higher injury risk
        VeryHigh        // 1.6x multiplier, high injury risk
    }
    
    /// <summary>
    /// Available training facilities
    /// </summary>
    public enum TrainingFacility
    {
        BasicGym,
        AdvancedGym,
        TrainingOvals,
        IndoorCourt,
        SwimmingPool,
        RecoveryCenter,
        TacticalRoom,
        MedicalFacility
    }
    
    /// <summary>
    /// Weightings for different focus areas in training
    /// </summary>
    [System.Serializable]
    public class TrainingFocusWeights
    {
        [Range(0f, 2f)] public float SkillsWeight = 1f;
        [Range(0f, 2f)] public float FitnessWeight = 1f;
        [Range(0f, 2f)] public float DefenseWeight = 1f;
        [Range(0f, 2f)] public float TacticalWeight = 1f;
        [Range(0f, 2f)] public float RecoveryWeight = 1f;
        
        public static TrainingFocusWeights CreateForFocus(TrainingFocus focus)
        {
            var weights = new TrainingFocusWeights();
            
            switch (focus)
            {
                case TrainingFocus.Skills:
                    weights.SkillsWeight = 1.8f;
                    weights.TacticalWeight = 1.2f;
                    break;
                    
                case TrainingFocus.Fitness:
                    weights.FitnessWeight = 1.8f;
                    weights.RecoveryWeight = 1.1f;
                    break;
                    
                case TrainingFocus.Defense:
                    weights.DefenseWeight = 1.8f;
                    weights.TacticalWeight = 1.3f;
                    break;
                    
                case TrainingFocus.Tactical:
                    weights.TacticalWeight = 1.8f;
                    weights.SkillsWeight = 1.1f;
                    break;
                    
                case TrainingFocus.Recovery:
                    weights.RecoveryWeight = 1.8f;
                    weights.FitnessWeight = 1.1f;
                    break;
                    
                default: // General
                    // All weights remain at 1.0
                    break;
            }
            
            return weights;
        }
    }
}
