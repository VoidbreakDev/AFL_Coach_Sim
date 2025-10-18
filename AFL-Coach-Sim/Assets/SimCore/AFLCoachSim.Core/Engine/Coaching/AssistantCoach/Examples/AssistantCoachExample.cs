using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Tactics;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Coaching.AssistantCoach.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating the Assistant Coach system and its integration
    /// </summary>
    public class AssistantCoachExample
    {
        public static void RunAssistantCoachExample()
        {
            CoreLogger.Log("=== Assistant Coach System Example ===");
            
            // 1. Initialize the assistant coach system
            var assistantSystem = new AssistantCoachSystem(seed: 12345);
            var teamId = new TeamId(1); // Use constructor with int value
            
            CoreLogger.Log($"Initialized assistant coach system for team {teamId}");

            // 2. Demonstrate hiring different types of assistants
            DemonstrateHiringProcess(assistantSystem, teamId);
            
            // 3. Show training benefits
            DemonstrateTrainingBenefits(assistantSystem, teamId);
            
            // 4. Show match day insights
            DemonstrateMatchDayInsights(assistantSystem, teamId);
            
            // 5. Show player development bonuses
            DemonstratePlayerDevelopment(assistantSystem, teamId);
            
            // 6. Performance tracking and management
            DemonstratePerformanceTracking(assistantSystem, teamId);
            
            // 7. League progression benefits
            DemonstrateLeagueProgressionBenefits();
            
            CoreLogger.Log("\n=== Assistant Coach Example Complete ===");
        }

        private static void DemonstrateHiringProcess(AssistantCoachSystem system, TeamId teamId)
        {
            CoreLogger.Log("\n--- HIRING ASSISTANT COACHES ---");
            
            // Generate a hiring pool (what the player would see)
            var availableCoaches = AssistantCoachFactory.GenerateHiringPool(12);
            
            CoreLogger.Log("Available Assistant Coaches for Hire:");
            foreach (var coach in availableCoaches.Take(6)) // Show top 6
            {
                CoreLogger.Log($"  {coach.Name} - {coach.Specialization}");
                CoreLogger.Log($"    Skill: {coach.SkillLevel}/100, Experience: {coach.YearsExperience} years");
                CoreLogger.Log($"    Salary: ${coach.SalaryPerWeek:F0}/week, Reputation: {coach.Reputation:F0}/100");
                CoreLogger.Log($"    Specialty: {coach.GetSpecializationDescription()}");
                CoreLogger.Log("");
            }
            
            // Player decides to hire specific assistants for their VFL/AFL team
            var tacticalCoach = availableCoaches.First(c => c.Specialization == AssistantCoachSpecialization.TacticalCoach);
            var fitnessCoach = availableCoaches.First(c => c.Specialization == AssistantCoachSpecialization.FitnessCoach);
            var developmentCoach = availableCoaches.First(c => c.Specialization == AssistantCoachSpecialization.DevelopmentCoach);
            
            // Hire the coaches
            system.HireAssistantCoach(teamId, tacticalCoach);
            system.HireAssistantCoach(teamId, fitnessCoach);
            system.HireAssistantCoach(teamId, developmentCoach);
            
            var currentAssistants = system.GetTeamAssistants(teamId);
            CoreLogger.Log($"Successfully hired {currentAssistants.Count} assistant coaches:");
            foreach (var assistant in currentAssistants)
            {
                CoreLogger.Log($"  {assistant.Name} ({assistant.Specialization}) - ${assistant.SalaryPerWeek:F0}/week");
            }
        }

        private static void DemonstrateTrainingBenefits(AssistantCoachSystem system, TeamId teamId)
        {
            CoreLogger.Log("\n--- TRAINING BENEFITS FROM ASSISTANTS ---");
            
            // Simulate different types of training sessions
            var trainingTypes = new[]
            {
                TrainingType.FitnessConditioning,
                TrainingType.TacticalAnalysis,
                TrainingType.YouthDevelopment,
                TrainingType.SkillsDevelopment
            };
            
            foreach (var trainingType in trainingTypes)
            {
                var trainingSession = new TrainingSession
                {
                    TrainingType = trainingType,
                    IntensityLevel = 75f,
                    DurationMinutes = 90,
                    ParticipatingPlayers = GeneratePlayerIds(20)
                };
                
                // Calculate bonuses from assistant coaches
                var bonuses = system.CalculateTrainingBonuses(teamId, trainingSession);
                
                CoreLogger.Log($"{trainingType} Training Session:");
                if (bonuses.OverallEffectivenessBonus > 0)
                    CoreLogger.Log($"  Overall Effectiveness Bonus: +{bonuses.OverallEffectivenessBonus:P1}");
                if (bonuses.InjuryReductionBonus > 0)
                    CoreLogger.Log($"  Injury Risk Reduction: -{bonuses.InjuryReductionBonus:P1}");
                if (bonuses.SkillDevelopmentBonus > 0)
                    CoreLogger.Log($"  Skill Development Bonus: +{bonuses.SkillDevelopmentBonus:P1}");
                if (bonuses.FitnessGainBonus > 0)
                    CoreLogger.Log($"  Fitness Gain Bonus: +{bonuses.FitnessGainBonus:P1}");
                
                // Get training recommendations from assistants
                var recommendations = system.GetTrainingRecommendations(teamId, GeneratePlayerTrainingData());
                if (recommendations.Any())
                {
                    CoreLogger.Log("  Assistant Recommendations:");
                    foreach (var rec in recommendations.Take(2))
                    {
                        CoreLogger.Log($"    {rec.GetFormattedRecommendation()}");
                    }
                }
                CoreLogger.Log("");
            }
        }

        private static void DemonstrateMatchDayInsights(AssistantCoachSystem system, TeamId teamId)
        {
            CoreLogger.Log("\n--- MATCH DAY INSIGHTS FROM ASSISTANTS ---");
            
            // Simulate different match situations
            var matchSituations = new[]
            {
                new MatchSituation 
                { 
                    ScoreDifferential = -18, // Behind by 3 goals
                    TimeRemainingPercent = 0.6f,
                    TeamMomentum = -0.3f
                },
                new MatchSituation 
                { 
                    ScoreDifferential = 12, // Ahead by 2 goals  
                    TimeRemainingPercent = 0.2f, // Final quarter
                    TeamMomentum = 0.1f
                },
                new MatchSituation 
                { 
                    ScoreDifferential = -3, // Close game
                    TimeRemainingPercent = 0.1f, // Final 6 minutes
                    TeamMomentum = -0.1f
                }
            };
            
            var currentGamePlan = new TacticalGamePlan
            {
                Name = "Standard Game Plan",
                Formation = FormationLibrary.GetFormation("Standard")
            };
            
            foreach (var situation in matchSituations)
            {
                string situationDesc = GetSituationDescription(situation);
                CoreLogger.Log($"{situationDesc}:");
                
                var insights = system.GetMatchDayInsights(teamId, situation, currentGamePlan);
                
                if (insights.HasActionableInsights())
                {
                    var allInsights = insights.GetAllInsights();
                    foreach (var insight in allInsights.Take(3)) // Show top 3 insights
                    {
                        CoreLogger.Log($"  {insight}");
                    }
                    CoreLogger.Log($"  Insight Quality: {insights.InsightQuality:P0}");
                }
                else
                {
                    CoreLogger.Log("  No specific recommendations at this time");
                }
                CoreLogger.Log("");
            }
        }

        private static void DemonstratePlayerDevelopment(AssistantCoachSystem system, TeamId teamId)
        {
            CoreLogger.Log("\n--- PLAYER DEVELOPMENT BONUSES ---");
            
            var players = GeneratePlayerTrainingData();
            var developmentBonuses = system.CalculateDevelopmentBonuses(teamId, players);
            
            CoreLogger.Log("Development bonuses for selected players:");
            foreach (var player in players.Take(5)) // Show first 5 players
            {
                var bonus = developmentBonuses.GetPlayerBonus(player.PlayerId);
                
                if (bonus.GetCombinedDevelopmentMultiplier() > 1.0f)
                {
                    CoreLogger.Log($"{player.Name} ({player.PrimaryRole}, Age {player.Age}):");
                    
                    if (bonus.OverallDevelopmentRate > 0)
                        CoreLogger.Log($"  Overall Development: +{bonus.OverallDevelopmentRate:P1}");
                    if (bonus.SpecializedSkillBonus > 0)
                        CoreLogger.Log($"  Specialized Skills: +{bonus.SpecializedSkillBonus:P1}");
                    if (bonus.PhysicalAttributeBonus > 0)
                        CoreLogger.Log($"  Physical Attributes: +{bonus.PhysicalAttributeBonus:P1}");
                    if (bonus.PotentialUnlockRate > 0)
                        CoreLogger.Log($"  Potential Unlocking: +{bonus.PotentialUnlockRate:P1}");
                        
                    CoreLogger.Log($"  Total Development Multiplier: {bonus.GetCombinedDevelopmentMultiplier():F2}x");
                    CoreLogger.Log("");
                }
            }
        }

        private static void DemonstratePerformanceTracking(AssistantCoachSystem system, TeamId teamId)
        {
            CoreLogger.Log("\n--- PERFORMANCE TRACKING ---");
            
            var assistants = system.GetTeamAssistants(teamId);
            
            // Simulate several weeks of performance
            for (int week = 0; week < 8; week++)
            {
                system.UpdateWeeklyPerformance(teamId);
            }
            
            CoreLogger.Log("Assistant Coach Performance Reports:");
            foreach (var assistant in assistants)
            {
                var report = system.GetPerformanceReport(assistant.Id);
                if (report != null)
                {
                    CoreLogger.Log($"{assistant.Name} ({assistant.Specialization}):");
                    CoreLogger.Log($"  Overall Rating: {report.OverallRating:F1}/100 (Grade: {report.GetPerformanceGrade()})");
                    CoreLogger.Log($"  Training Effectiveness: {report.TrainingEffectiveness:F2}");
                    CoreLogger.Log($"  Match Contribution: {report.MatchContribution:F2}");
                    CoreLogger.Log($"  Development Impact: {report.DevelopmentImpact:F2}");
                    CoreLogger.Log($"  Contract Renewal: {(report.ShouldRenewContract() ? "Recommended" : "Consider Replacement")}");
                    CoreLogger.Log("");
                }
            }
        }

        private static void DemonstrateLeagueProgressionBenefits()
        {
            CoreLogger.Log("\n--- LEAGUE PROGRESSION BENEFITS ---");
            
            var leagueLimits = new Dictionary<string, int>
            {
                ["Local League"] = 0,     // No assistants
                ["Regional League"] = 1,  // 1 assistant maximum  
                ["State League"] = 2,     // 2 assistants maximum
                ["VFL"] = 3,             // 3 assistants maximum
                ["AFL"] = 4              // 4 assistants maximum
            };
            
            CoreLogger.Log("Assistant Coach limits by league level:");
            foreach (var league in leagueLimits)
            {
                CoreLogger.Log($"{league.Key}: {league.Value} assistant{(league.Value == 1 ? "" : "s")} maximum");
            }
            
            CoreLogger.Log("\nProgression benefits:");
            CoreLogger.Log("• Local → Regional: Unlock 1 assistant coach position");
            CoreLogger.Log("• Regional → State: +1 assistant position, access to better coaches");
            CoreLogger.Log("• State → VFL: +1 assistant position, elite coaches available");
            CoreLogger.Log("• VFL → AFL: +1 assistant position, world-class specialists available");
            
            CoreLogger.Log("\nSpecialization unlock progression:");
            CoreLogger.Log("• Regional+: Fitness Coach, Skills Coach available");
            CoreLogger.Log("• State+: Forward Coach, Defensive Coach, Midfielder Coach available");  
            CoreLogger.Log("• VFL+: Tactical Coach, Development Coach available");
            CoreLogger.Log("• AFL+: Recovery Coach, all elite specialists available");
        }

        #region Helper Methods

        private static List<Guid> GeneratePlayerIds(int count)
        {
            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                ids.Add(Guid.NewGuid());
            }
            return ids;
        }

        private static List<PlayerTrainingData> GeneratePlayerTrainingData()
        {
            var random = new Random(12345);
            var roles = Enum.GetValues(typeof(Role)).Cast<Role>().ToArray();
            var players = new List<PlayerTrainingData>();
            
            var playerNames = new[] 
            { 
                "Jack Thompson", "Sam Wilson", "Matt Davis", "Luke Miller", "Tom Anderson",
                "Josh Brown", "Alex Johnson", "Ryan Clark", "Ben Taylor", "Jake Wilson"
            };
            
            for (int i = 0; i < playerNames.Length; i++)
            {
                players.Add(new PlayerTrainingData
                {
                    PlayerId = Guid.NewGuid(),
                    Name = playerNames[i],
                    PrimaryRole = roles[random.Next(roles.Length)],
                    Age = random.Next(18, 35),
                    CurrentSkillLevel = random.Next(40, 85),
                    Potential = random.Next(50, 95),
                    FitnessLevel = random.Next(60, 95),
                    InjuryProneness = random.Next(5, 30)
                });
            }
            
            return players;
        }

        private static string GetSituationDescription(MatchSituation situation)
        {
            string scoreDiff = situation.ScoreDifferential switch
            {
                < -12 => $"Behind by {Math.Abs(situation.ScoreDifferential)} points",
                > 12 => $"Ahead by {situation.ScoreDifferential} points",
                _ => "Close game"
            };
            
            string timeDesc = situation.TimeRemainingPercent switch
            {
                < 0.15f => "Final 10 minutes",
                < 0.25f => "Final quarter",
                < 0.5f => "Second half",
                _ => "First half"
            };
            
            return $"{scoreDiff}, {timeDesc}";
        }

        #endregion

        public static void Main()
        {
            RunAssistantCoachExample();
            
            CoreLogger.Log("\nThe Assistant Coach system provides:");
            CoreLogger.Log("• 8 specialized assistant coach types with unique benefits");
            CoreLogger.Log("• Training effectiveness bonuses and injury reduction");
            CoreLogger.Log("• Real-time match insights and tactical recommendations");  
            CoreLogger.Log("• Accelerated player development, especially for young players");
            CoreLogger.Log("• Performance tracking and contract management");
            CoreLogger.Log("• League progression rewards (more assistants in higher leagues)");
            CoreLogger.Log("• Realistic coaching personalities and backgrounds");
            CoreLogger.Log("• Integration with existing tactical and training systems");
        }
    }
}