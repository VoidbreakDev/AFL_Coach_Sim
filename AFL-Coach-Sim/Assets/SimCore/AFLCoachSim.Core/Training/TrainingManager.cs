using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Main training manager that orchestrates all training activities, schedules, and development tracking
    /// </summary>
    public class TrainingManager
    {
        private readonly TrainingEngine _engine;
        private readonly Dictionary<string, TrainingProgram> _availablePrograms;
        private readonly Dictionary<int, List<PlayerTrainingEnrollment>> _playerEnrollments;
        private readonly List<TrainingSession> _scheduledSessions;
        private readonly List<TrainingSession> _completedSessions;
        private readonly Random _random;

        // Events for UI and integration
        public event Action<TrainingSession, Dictionary<int, TrainingOutcome>> SessionCompleted;
        public event Action<int, PlayerTrainingEnrollment> PlayerEnrolled;
        public event Action<int, string> PlayerProgramCompleted;

        public TrainingManager(int seed = 0)
        {
            _engine = new TrainingEngine(seed);
            _random = seed == 0 ? new Random() : new Random(seed);
            
            _availablePrograms = new Dictionary<string, TrainingProgram>();
            _playerEnrollments = new Dictionary<int, List<PlayerTrainingEnrollment>>();
            _scheduledSessions = new List<TrainingSession>();
            _completedSessions = new List<TrainingSession>();
            
            // Load all available training programs
            LoadAvailablePrograms();
        }

        #region Program Management

        /// <summary>
        /// Load all available training programs from the library
        /// </summary>
        private void LoadAvailablePrograms()
        {
            var programs = TrainingProgramLibrary.GetAllPrograms();
            foreach (var program in programs)
            {
                _availablePrograms[program.Id] = program;
            }
        }

        /// <summary>
        /// Get all available training programs
        /// </summary>
        public List<TrainingProgram> GetAvailablePrograms()
        {
            return _availablePrograms.Values.ToList();
        }

        /// <summary>
        /// Get programs suitable for a specific player
        /// </summary>
        public List<TrainingProgram> GetSuitablePrograms(Player player)
        {
            var age = CalculateAge(player.DateOfBirth);
            var stage = GetDevelopmentStage(age);
            
            return _availablePrograms.Values
                .Where(p => p.IsSuitableFor(player, stage))
                .OrderByDescending(p => p.CalculateEffectiveness(player, stage, _engine.GetPlayerPotential(player.Id)))
                .ToList();
        }

        /// <summary>
        /// Get recommended programs for a player based on their attributes and potential
        /// </summary>
        public List<(TrainingProgram Program, float Priority, string Reason)> GetRecommendedPrograms(Player player)
        {
            var suitable = GetSuitablePrograms(player);
            var potential = _engine.GetPlayerPotential(player.Id) ?? _engine.CalculatePlayerPotential(player);
            var age = CalculateAge(player.DateOfBirth);
            var stage = GetDevelopmentStage(age);
            
            var recommendations = new List<(TrainingProgram, float, string)>();
            
            foreach (var program in suitable)
            {
                float priority = CalculateProgramPriority(player, program, potential, stage);
                string reason = GenerateRecommendationReason(player, program, potential, stage);
                recommendations.Add((program, priority, reason));
            }
            
            return recommendations
                .OrderByDescending(r => r.Item2)
                .Take(5) // Top 5 recommendations
                .ToList();
        }

        #endregion

        #region Player Enrollment

        /// <summary>
        /// Enroll a player in a training program
        /// </summary>
        public bool EnrollPlayer(int playerId, string programId, DateTime? startDate = null)
        {
            if (!_availablePrograms.ContainsKey(programId))
                return false;

            var program = _availablePrograms[programId];
            var actualStartDate = startDate ?? DateTime.Now;
            
            var enrollment = new PlayerTrainingEnrollment
            {
                PlayerId = playerId,
                ProgramId = programId,
                StartDate = actualStartDate,
                IsActive = true
            };

            if (!_playerEnrollments.ContainsKey(playerId))
                _playerEnrollments[playerId] = new List<PlayerTrainingEnrollment>();

            _playerEnrollments[playerId].Add(enrollment);
            PlayerEnrolled?.Invoke(playerId, enrollment);
            
            return true;
        }

        /// <summary>
        /// Remove a player from a training program
        /// </summary>
        public bool WithdrawPlayer(int playerId, string programId)
        {
            if (!_playerEnrollments.ContainsKey(playerId))
                return false;

            var enrollment = _playerEnrollments[playerId]
                .FirstOrDefault(e => e.ProgramId == programId && e.IsActive);
                
            if (enrollment != null)
            {
                enrollment.IsActive = false;
                enrollment.EndDate = DateTime.Now;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Get all active enrollments for a player
        /// </summary>
        public List<PlayerTrainingEnrollment> GetPlayerEnrollments(int playerId)
        {
            return _playerEnrollments.ContainsKey(playerId) 
                ? _playerEnrollments[playerId].Where(e => e.IsActive).ToList()
                : new List<PlayerTrainingEnrollment>();
        }

        /// <summary>
        /// Get all players enrolled in a specific program
        /// </summary>
        public List<int> GetProgramEnrollees(string programId)
        {
            return _playerEnrollments.Values
                .SelectMany(enrollments => enrollments)
                .Where(e => e.ProgramId == programId && e.IsActive)
                .Select(e => e.PlayerId)
                .ToList();
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Schedule a training session for a program
        /// </summary>
        public TrainingSession ScheduleSession(string programId, DateTime scheduledDate, TrainingIntensity intensity, List<int> participatingPlayers = null)
        {
            if (!_availablePrograms.ContainsKey(programId))
                return null;

            var players = participatingPlayers ?? GetProgramEnrollees(programId);
            
            var session = new TrainingSession
            {
                ProgramId = programId,
                ScheduledDate = scheduledDate,
                Intensity = intensity,
                ParticipatingPlayers = players
            };

            _scheduledSessions.Add(session);
            return session;
        }

        /// <summary>
        /// Execute a scheduled training session
        /// </summary>
        public bool ExecuteSession(string sessionId, List<Player> players)
        {
            var session = _scheduledSessions.FirstOrDefault(s => s.Id == sessionId && !s.IsCompleted);
            if (session == null || !_availablePrograms.ContainsKey(session.ProgramId))
                return false;

            var program = _availablePrograms[session.ProgramId];
            var outcomes = _engine.ExecuteTrainingSession(program, session, players);

            // Update player enrollments with outcomes
            foreach (var outcome in outcomes)
            {
                int playerId = outcome.Key;
                var result = outcome.Value;
                
                var enrollment = GetPlayerEnrollments(playerId)
                    .FirstOrDefault(e => e.ProgramId == session.ProgramId);
                    
                if (enrollment != null)
                {
                    enrollment.ProcessSessionOutcome(result);
                    
                    // Check if program is completed
                    var progress = enrollment.CalculateProgress(program);
                    if (progress >= 100f)
                    {
                        enrollment.IsActive = false;
                        enrollment.EndDate = DateTime.Now;
                        PlayerProgramCompleted?.Invoke(playerId, session.ProgramId);
                    }
                }
            }

            // Mark session as completed
            session.Complete(outcomes);
            _scheduledSessions.Remove(session);
            _completedSessions.Add(session);
            
            SessionCompleted?.Invoke(session, outcomes);
            return true;
        }

        /// <summary>
        /// Get all scheduled sessions for a date range
        /// </summary>
        public List<TrainingSession> GetScheduledSessions(DateTime startDate, DateTime endDate)
        {
            return _scheduledSessions
                .Where(s => s.ScheduledDate >= startDate && s.ScheduledDate <= endDate)
                .OrderBy(s => s.ScheduledDate)
                .ToList();
        }

        /// <summary>
        /// Get completed sessions for analysis
        /// </summary>
        public List<TrainingSession> GetCompletedSessions(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _completedSessions.AsQueryable();
            
            if (startDate.HasValue)
                query = query.Where(s => s.CompletedDate >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(s => s.CompletedDate <= endDate.Value);
                
            return query.OrderBy(s => s.CompletedDate).ToList();
        }

        #endregion

        #region Analytics and Insights

        /// <summary>
        /// Generate training effectiveness report for a player
        /// </summary>
        public PlayerTrainingReport GeneratePlayerReport(int playerId)
        {
            var enrollments = _playerEnrollments.ContainsKey(playerId) 
                ? _playerEnrollments[playerId] 
                : new List<PlayerTrainingEnrollment>();
                
            var efficiencyHistory = _engine.GetPlayerEfficiencyHistory(playerId);
            var potential = _engine.GetPlayerPotential(playerId);
            
            return new PlayerTrainingReport
            {
                PlayerId = playerId,
                ActiveEnrollments = enrollments.Where(e => e.IsActive).ToList(),
                CompletedPrograms = enrollments.Where(e => !e.IsActive).ToList(),
                EfficiencyMetrics = efficiencyHistory,
                DevelopmentPotential = potential,
                TotalSessionsCompleted = enrollments.Sum(e => e.SessionsCompleted),
                TotalTrainingTime = enrollments
                    .Where(e => e.EndDate.HasValue)
                    .Sum(e => (e.EndDate.Value - e.StartDate).Days),
                RecommendedPrograms = new List<(TrainingProgram, float, string)>()
            };
        }

        /// <summary>
        /// Get team training summary
        /// </summary>
        public TeamTrainingReport GenerateTeamReport(List<int> playerIds, DateTime? startDate = null, DateTime? endDate = null)
        {
            var report = new TeamTrainingReport
            {
                ReportDate = DateTime.Now,
                PlayerIds = playerIds,
                TotalPlayers = playerIds.Count
            };

            // Calculate team-wide metrics
            var allSessions = GetCompletedSessions(startDate, endDate)
                .Where(s => s.ParticipatingPlayers.Any(p => playerIds.Contains(p)))
                .ToList();

            report.TotalSessionsCompleted = allSessions.Count;
            report.TotalTrainingHours = allSessions.Count * 2; // Assume 2 hours per session

            // Program type distribution
            report.ProgramTypeBreakdown = allSessions
                .GroupBy(s => _availablePrograms[s.ProgramId].Type)
                .ToDictionary(g => g.Key, g => g.Count());

            // Average development metrics
            var playerReports = playerIds.Select(GeneratePlayerReport).ToList();
            var potentialReports = playerReports.Where(r => r.DevelopmentPotential != null).ToList();
            report.AverageDevelopmentRate = potentialReports.Any() 
                ? potentialReports.Average(r => r.DevelopmentPotential.DevelopmentRate)
                : 0f;

            var completedPrograms = playerReports.SelectMany(r => r.CompletedPrograms).ToList();
            report.InjuryIncidenceRate = completedPrograms.Any() 
                ? completedPrograms.Average(e => e.TotalInjuryRisk)
                : 0f;

            return report;
        }

        /// <summary>
        /// Get AI-powered training insights for a player
        /// </summary>
        public List<TrainingInsight> GetTrainingInsights(int playerId)
        {
            var insights = new List<TrainingInsight>();
            var report = GeneratePlayerReport(playerId);
            var potential = report.DevelopmentPotential;
            
            if (potential == null) return insights;

            // Development rate insights
            if (potential.DevelopmentRate < 0.8f)
            {
                insights.Add(new TrainingInsight
                {
                    Type = InsightType.DevelopmentConcern,
                    Title = "Slow Development Rate",
                    Description = "Player is developing below expected rate. Consider adjusting training intensity or program selection.",
                    Priority = InsightPriority.High,
                    RecommendedAction = "Switch to more suitable programs or reduce training load"
                });
            }

            // Injury risk insights
            var avgInjuryRisk = report.CompletedPrograms.Any() 
                ? report.CompletedPrograms.Average(e => e.TotalInjuryRisk)
                : 0f;
                
            if (avgInjuryRisk > 0.15f) // 15% cumulative risk
            {
                insights.Add(new TrainingInsight
                {
                    Type = InsightType.InjuryRisk,
                    Title = "High Injury Risk",
                    Description = "Player has accumulated significant injury risk from training. Consider recovery programs.",
                    Priority = InsightPriority.High,
                    RecommendedAction = "Enroll in injury prevention or active recovery programs"
                });
            }

            // Training efficiency insights
            var recentEfficiency = report.EfficiencyMetrics.TakeLast(5);
            if (recentEfficiency.Any() && recentEfficiency.Average(e => e.AverageGain) < 0.5f)
            {
                insights.Add(new TrainingInsight
                {
                    Type = InsightType.EfficiencyIssue,
                    Title = "Low Training Efficiency",
                    Description = "Recent training sessions show diminished returns. Player may need different program types.",
                    Priority = InsightPriority.Medium,
                    RecommendedAction = "Evaluate program suitability and consider alternative training approaches"
                });
            }

            return insights;
        }

        #endregion

        #region Helper Methods

        private float CalculateProgramPriority(Player player, TrainingProgram program, DevelopmentPotential potential, DevelopmentStage stage)
        {
            float priority = program.CalculateEffectiveness(player, stage, potential);
            
            // Boost priority for programs targeting weak areas
            var attributes = player.Attr;
            var weakestAttribute = GetWeakestAttribute(attributes);
            if (program.AttributeTargets.ContainsKey(weakestAttribute))
            {
                priority *= 1.3f;
            }
            
            // Consider player's preferred training
            if (potential.PreferredTraining.Contains(program.PrimaryFocus))
            {
                priority *= 1.2f;
            }
            
            return priority;
        }

        private string GenerateRecommendationReason(Player player, TrainingProgram program, DevelopmentPotential potential, DevelopmentStage stage)
        {
            var reasons = new List<string>();
            
            float effectiveness = program.CalculateEffectiveness(player, stage, potential);
            if (effectiveness > 1.3f)
                reasons.Add("High effectiveness for this player");
            
            if (potential.PreferredTraining.Contains(program.PrimaryFocus))
                reasons.Add("Matches player's training preferences");
                
            var weakestAttribute = GetWeakestAttribute(player.Attr);
            if (program.AttributeTargets.ContainsKey(weakestAttribute))
                reasons.Add($"Addresses weak area: {weakestAttribute}");
                
            if (program.InjuryRiskModifier < 0.8f)
                reasons.Add("Low injury risk");
                
            return reasons.Any() ? string.Join(", ", reasons) : "General development benefits";
        }

        private string GetWeakestAttribute(Attributes attributes)
        {
            var attributeValues = new Dictionary<string, int>
            {
                {"Kicking", attributes.Kicking},
                {"Marking", attributes.Marking},
                {"Handball", attributes.Handball},
                {"Tackling", attributes.Tackling},
                {"Speed", attributes.Speed}
            };
            
            return attributeValues.OrderBy(kv => kv.Value).First().Key;
        }

        private DevelopmentStage GetDevelopmentStage(int age)
        {
            if (age <= 20)
                return DevelopmentStage.Rookie;
            else if (age <= 25)
                return DevelopmentStage.Developing;
            else if (age <= 29)
                return DevelopmentStage.Prime;
            else if (age <= 34)
                return DevelopmentStage.Veteran;
            else
                return DevelopmentStage.Declining;
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }

        /// <summary>
        /// Calculate development potential for a player - exposed for demo and testing purposes
        /// </summary>
        public DevelopmentPotential CalculatePlayerPotential(Player player)
        {
            return _engine.CalculatePlayerPotential(player);
        }

        #endregion
    }

    #region Report Classes

    /// <summary>
    /// Comprehensive training report for a single player
    /// </summary>
    public class PlayerTrainingReport
    {
        public int PlayerId { get; set; }
        public List<PlayerTrainingEnrollment> ActiveEnrollments { get; set; }
        public List<PlayerTrainingEnrollment> CompletedPrograms { get; set; }
        public List<TrainingEfficiency> EfficiencyMetrics { get; set; }
        public DevelopmentPotential DevelopmentPotential { get; set; }
        public int TotalSessionsCompleted { get; set; }
        public int TotalTrainingTime { get; set; }
        public List<(TrainingProgram Program, float Priority, string Reason)> RecommendedPrograms { get; set; }

        public PlayerTrainingReport()
        {
            ActiveEnrollments = new List<PlayerTrainingEnrollment>();
            CompletedPrograms = new List<PlayerTrainingEnrollment>();
            EfficiencyMetrics = new List<TrainingEfficiency>();
            RecommendedPrograms = new List<(TrainingProgram, float, string)>();
        }
    }

    /// <summary>
    /// Team-wide training summary report
    /// </summary>
    public class TeamTrainingReport
    {
        public DateTime ReportDate { get; set; }
        public List<int> PlayerIds { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalSessionsCompleted { get; set; }
        public int TotalTrainingHours { get; set; }
        public Dictionary<TrainingType, int> ProgramTypeBreakdown { get; set; }
        public float AverageDevelopmentRate { get; set; }
        public float InjuryIncidenceRate { get; set; }

        public TeamTrainingReport()
        {
            PlayerIds = new List<int>();
            ProgramTypeBreakdown = new Dictionary<TrainingType, int>();
        }
    }

    /// <summary>
    /// AI-powered training insight for decision support
    /// </summary>
    public class TrainingInsight
    {
        public InsightType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public InsightPriority Priority { get; set; }
        public string RecommendedAction { get; set; }
    }

    public enum InsightType
    {
        DevelopmentConcern,
        InjuryRisk,
        EfficiencyIssue,
        Opportunity,
        Achievement
    }

    public enum InsightPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}