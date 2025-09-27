using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using AFLCoachSim.Core.Models;
using AFLCoachSim.Core.Training;

namespace AFLCoachSim.Training
{
    /// <summary>
    /// Comprehensive demo showcasing the advanced training system capabilities
    /// </summary>
    public class TrainingSystemDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private int numberOfPlayers = 10;
        [SerializeField] private int simulationDays = 30;

        private TrainingManager _trainingManager;
        private List&lt;Player&gt; _demoPlayers;

        void Start()
        {
            if (runOnStart)
            {
                RunDemo();
            }
        }

        [ContextMenu("Run Training System Demo")]
        public void RunDemo()
        {
            Debug.Log("=== AFL Coach Sim Training System Demo ===");
            
            // Initialize system
            InitializeSystem();
            
            // Create demo players
            CreateDemoPlayers();
            
            // Demonstrate core features
            DemonstratePlayerPotentialCalculation();
            DemonstrateTrainingProgramRecommendations();
            DemonstratePlayerEnrollmentAndTraining();
            DemonstrateTrainingAnalytics();
            DemonstrateAIInsights();
            
            Debug.Log("=== Training System Demo Complete ===");
        }

        private void InitializeSystem()
        {
            Debug.Log("\n--- Initializing Training System ---");
            
            _trainingManager = new TrainingManager(randomSeed);
            
            // Subscribe to events for demonstration
            _trainingManager.SessionCompleted += OnSessionCompleted;
            _trainingManager.PlayerEnrolled += OnPlayerEnrolled;
            _trainingManager.PlayerProgramCompleted += OnPlayerProgramCompleted;
            
            var availablePrograms = _trainingManager.GetAvailablePrograms();
            Debug.Log($"Training system initialized with {availablePrograms.Count} available programs");
            
            // Log program categories
            var programsByType = availablePrograms.GroupBy(p =&gt; p.Type).ToDictionary(g =&gt; g.Key, g =&gt; g.Count());
            foreach (var category in programsByType)
            {
                Debug.Log($"- {category.Key}: {category.Value} programs");
            }
        }

        private void CreateDemoPlayers()
        {
            Debug.Log("\n--- Creating Demo Players ---");
            
            _demoPlayers = new List&lt;Player&gt;();
            var positions = new[] 
            { 
                Position.FullForward, Position.HalfForward, Position.Centre, Position.Wing, 
                Position.Rover, Position.HalfBack, Position.FullBack, Position.Ruckman,
                Position.ForwardPocket, Position.BackPocket
            };

            for (int i = 0; i &lt; numberOfPlayers; i++)
            {
                var player = CreateRealisticPlayer(
                    id: i + 1,
                    name: $"Player {i + 1}",
                    position: positions[i % positions.Length],
                    age: UnityEngine.Random.Range(18, 34)
                );
                
                _demoPlayers.Add(player);
                Debug.Log($"Created {player.NameOf()} ({player.Position}, Age {CalculateAge(player.DateOfBirth)})");
            }
        }

        private Player CreateRealisticPlayer(int id, string name, Position position, int age)
        {
            var player = new Player
            {
                Id = id,
                Name = name,
                Position = position,
                DateOfBirth = DateTime.Today.AddYears(-age),
                Attributes = GeneratePositionAppropriateAttributes(position)
            };

            return player;
        }

        private Attributes GeneratePositionAppropriateAttributes(Position position)
        {
            var attrs = new Attributes();
            
            // Base random attributes (50-80 range)
            attrs.Kicking = UnityEngine.Random.Range(50f, 80f);
            attrs.Marking = UnityEngine.Random.Range(50f, 80f);
            attrs.Handballing = UnityEngine.Random.Range(50f, 80f);
            attrs.Contested = UnityEngine.Random.Range(50f, 80f);
            attrs.Endurance = UnityEngine.Random.Range(50f, 80f);

            // Apply position-specific bonuses
            switch (position)
            {
                case Position.FullForward:
                    attrs.Kicking += UnityEngine.Random.Range(5f, 15f);
                    attrs.Marking += UnityEngine.Random.Range(10f, 20f);
                    break;
                case Position.Centre:
                    attrs.Endurance += UnityEngine.Random.Range(10f, 20f);
                    attrs.Handballing += UnityEngine.Random.Range(5f, 15f);
                    break;
                case Position.Ruckman:
                    attrs.Contested += UnityEngine.Random.Range(10f, 20f);
                    attrs.Marking += UnityEngine.Random.Range(8f, 15f);
                    break;
                case Position.HalfBack:
                    attrs.Kicking += UnityEngine.Random.Range(8f, 15f);
                    attrs.Marking += UnityEngine.Random.Range(5f, 12f);
                    break;
                case Position.Wing:
                    attrs.Endurance += UnityEngine.Random.Range(8f, 18f);
                    break;
            }

            // Clamp to reasonable ranges
            attrs.Kicking = Mathf.Clamp(attrs.Kicking, 40f, 95f);
            attrs.Marking = Mathf.Clamp(attrs.Marking, 40f, 95f);
            attrs.Handballing = Mathf.Clamp(attrs.Handballing, 40f, 95f);
            attrs.Contested = Mathf.Clamp(attrs.Contested, 40f, 95f);
            attrs.Endurance = Mathf.Clamp(attrs.Endurance, 40f, 95f);

            return attrs;
        }

        private void DemonstratePlayerPotentialCalculation()
        {
            Debug.Log("\n--- Player Development Potential Analysis ---");
            
            foreach (var player in _demoPlayers.Take(5)) // Show first 5 players
            {
                var potential = _trainingManager.CalculatePlayerPotential(player);
                var age = CalculateAge(player.DateOfBirth);
                var stage = GetDevelopmentStage(age);
                
                Debug.Log($"\n{player.NameOf()} ({player.Position}, Age {age}, Stage: {stage}):");
                Debug.Log($"  Overall Potential: {potential.OverallPotential:F1}/100");
                Debug.Log($"  Development Rate: {potential.DevelopmentRate:F2}x");
                Debug.Log($"  Injury Proneness: {potential.InjuryProneness:F2}x");
                Debug.Log($"  Attribute Potentials:");
                
                foreach (var attrPotential in potential.AttributePotentials)
                {
                    var currentValue = GetPlayerAttributeValue(player, attrPotential.Key);
                    var growthRoom = attrPotential.Value - currentValue;
                    Debug.Log($"    {attrPotential.Key}: {currentValue:F1} → {attrPotential.Value:F1} (+{growthRoom:F1})");
                }
                
                Debug.Log($"  Preferred Training: {string.Join(", ", potential.PreferredTraining)}");
            }
        }

        private void DemonstrateTrainingProgramRecommendations()
        {
            Debug.Log("\n--- Training Program Recommendations ---");
            
            var samplePlayer = _demoPlayers[0];
            var recommendations = _trainingManager.GetRecommendedPrograms(samplePlayer);
            
            Debug.Log($"\nTop 5 training recommendations for {samplePlayer.NameOf()}:");
            foreach (var (program, priority, reason) in recommendations)
            {
                Debug.Log($"  {program.Name} (Priority: {priority:F2})");
                Debug.Log($"    Type: {program.Type}, Focus: {program.PrimaryFocus}");
                Debug.Log($"    Duration: {program.DurationDays} days, Intensity: {program.BaseIntensity}");
                Debug.Log($"    Reason: {reason}");
                Debug.Log($"    Targets: {string.Join(", ", program.AttributeTargets.Select(kv =&gt; $"{kv.Key} +{kv.Value}"))}");
                Debug.Log("");
            }
        }

        private void DemonstratePlayerEnrollmentAndTraining()
        {
            Debug.Log("\n--- Player Enrollment and Training Simulation ---");
            
            // Enroll players in various programs
            var programs = _trainingManager.GetAvailablePrograms();
            
            foreach (var player in _demoPlayers)
            {
                var suitable = _trainingManager.GetSuitablePrograms(player);
                if (suitable.Any())
                {
                    var selectedProgram = suitable.First();
                    bool enrolled = _trainingManager.EnrollPlayer(player.Id, selectedProgram.Id);
                    
                    if (enrolled)
                    {
                        Debug.Log($"Enrolled {player.NameOf()} in '{selectedProgram.Name}'");
                    }
                }
            }

            // Schedule and execute training sessions
            Debug.Log("\n--- Scheduling Training Sessions ---");
            
            var enrolledPrograms = _demoPlayers
                .SelectMany(p =&gt; _trainingManager.GetPlayerEnrollments(p.Id))
                .GroupBy(e =&gt; e.ProgramId)
                .Select(g =&gt; g.Key)
                .Distinct()
                .Take(3) // Limit to first 3 programs for demo
                .ToList();

            foreach (var programId in enrolledPrograms)
            {
                var program = programs.First(p =&gt; p.Id == programId);
                var enrollees = _trainingManager.GetProgramEnrollees(programId);
                
                Debug.Log($"\nScheduling sessions for '{program.Name}' with {enrollees.Count} players");
                
                // Schedule multiple sessions over time
                for (int day = 0; day &lt; simulationDays; day += 7) // Weekly sessions
                {
                    var session = _trainingManager.ScheduleSession(
                        programId, 
                        DateTime.Now.AddDays(day),
                        program.BaseIntensity,
                        enrollees
                    );
                    
                    if (session != null)
                    {
                        // Execute the session
                        var players = _demoPlayers.Where(p =&gt; enrollees.Contains(p.Id)).ToList();
                        bool executed = _trainingManager.ExecuteSession(session.Id, players);
                        
                        if (executed)
                        {
                            Debug.Log($"  Session {day/7 + 1} completed with {players.Count} players");
                        }
                    }
                }
            }
        }

        private void DemonstrateTrainingAnalytics()
        {
            Debug.Log("\n--- Training Analytics and Reports ---");
            
            // Generate player reports
            var samplePlayer = _demoPlayers[0];
            var playerReport = _trainingManager.GeneratePlayerReport(samplePlayer.Id);
            
            Debug.Log($"\nTraining report for {samplePlayer.NameOf()}:");
            Debug.Log($"  Active Programs: {playerReport.ActiveEnrollments.Count}");
            Debug.Log($"  Completed Programs: {playerReport.CompletedPrograms.Count}");
            Debug.Log($"  Total Sessions: {playerReport.TotalSessionsCompleted}");
            Debug.Log($"  Training Days: {playerReport.TotalTrainingTime}");
            
            if (playerReport.DevelopmentPotential != null)
            {
                Debug.Log($"  Development Rate: {playerReport.DevelopmentPotential.DevelopmentRate:F2}x");
            }
            
            // Show program progress
            foreach (var enrollment in playerReport.ActiveEnrollments)
            {
                var program = _trainingManager.GetAvailablePrograms().First(p =&gt; p.Id == enrollment.ProgramId);
                var progress = enrollment.CalculateProgress(program);
                Debug.Log($"    {program.Name}: {progress:F1}% complete ({enrollment.SessionsCompleted} sessions)");
                
                if (enrollment.CumulativeGains.Any())
                {
                    Debug.Log($"      Gains: {string.Join(", ", enrollment.CumulativeGains.Select(kv =&gt; $"{kv.Key} +{kv.Value:F1}"))}");
                }
            }

            // Generate team report
            Debug.Log("\n--- Team Training Summary ---");
            var playerIds = _demoPlayers.Select(p =&gt; p.Id).ToList();
            var teamReport = _trainingManager.GenerateTeamReport(playerIds);
            
            Debug.Log($"Team Training Report ({teamReport.ReportDate:yyyy-MM-dd}):");
            Debug.Log($"  Total Players: {teamReport.TotalPlayers}");
            Debug.Log($"  Sessions Completed: {teamReport.TotalSessionsCompleted}");
            Debug.Log($"  Training Hours: {teamReport.TotalTrainingHours}");
            Debug.Log($"  Avg Development Rate: {teamReport.AverageDevelopmentRate:F2}x");
            Debug.Log($"  Injury Incidence: {teamReport.InjuryIncidenceRate:F3}");
            
            Debug.Log("  Program Distribution:");
            foreach (var breakdown in teamReport.ProgramTypeBreakdown)
            {
                Debug.Log($"    {breakdown.Key}: {breakdown.Value} sessions");
            }
        }

        private void DemonstrateAIInsights()
        {
            Debug.Log("\n--- AI Training Insights ---");
            
            foreach (var player in _demoPlayers.Take(3)) // Show insights for first 3 players
            {
                var insights = _trainingManager.GetTrainingInsights(player.Id);
                
                Debug.Log($"\nTraining insights for {player.NameOf()}:");
                if (insights.Any())
                {
                    foreach (var insight in insights)
                    {
                        Debug.Log($"  [{insight.Priority}] {insight.Title}");
                        Debug.Log($"    {insight.Description}");
                        Debug.Log($"    Action: {insight.RecommendedAction}");
                    }
                }
                else
                {
                    Debug.Log($"  No specific insights - training progressing normally");
                }
            }
        }

        #region Event Handlers

        private void OnSessionCompleted(TrainingSession session, Dictionary&lt;int, TrainingOutcome&gt; outcomes)
        {
            var program = _trainingManager.GetAvailablePrograms().First(p =&gt; p.Id == session.ProgramId);
            Debug.Log($"Training session completed: '{program.Name}' ({session.Intensity} intensity)");
            
            foreach (var outcome in outcomes.Take(2)) // Show first 2 outcomes
            {
                var player = _demoPlayers.First(p =&gt; p.Id == outcome.Key);
                var result = outcome.Value;
                
                Debug.Log($"  {player.NameOf()}: Gains={string.Join(",", result.AttributeGains.Select(kv =&gt; $"{kv.Key}+{kv.Value:F1}"))} " +
                         $"Risk={result.InjuryRisk:F3} Fatigue={result.FatigueAccumulation:F1}");
                
                if (result.SpecialEffects.Any())
                {
                    Debug.Log($"    Special Effects: {string.Join("; ", result.SpecialEffects)}");
                }
            }
        }

        private void OnPlayerEnrolled(int playerId, PlayerTrainingEnrollment enrollment)
        {
            var player = _demoPlayers.First(p =&gt; p.Id == playerId);
            var program = _trainingManager.GetAvailablePrograms().First(p =&gt; p.Id == enrollment.ProgramId);
            Debug.Log($"Player enrolled: {player.NameOf()} started '{program.Name}'");
        }

        private void OnPlayerProgramCompleted(int playerId, string programId)
        {
            var player = _demoPlayers.First(p =&gt; p.Id == playerId);
            var program = _trainingManager.GetAvailablePrograms().First(p =&gt; p.Id == programId);
            Debug.Log($"Program completed: {player.NameOf()} finished '{program.Name}'!");
        }

        #endregion

        #region Helper Methods

        private float GetPlayerAttributeValue(Player player, string attribute)
        {
            var attrs = player.Attributes;
            switch (attribute)
            {
                case "Kicking":
                    return attrs.Kicking;
                case "Marking":
                    return attrs.Marking;
                case "Handballing":
                    return attrs.Handballing;
                case "Contested":
                    return attrs.Contested;
                case "Endurance":
                    return attrs.Endurance;
                default:
                    return 50f;
            }
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
            if (dateOfBirth.Date &gt; today.AddYears(-age))
                age--;
            return age;
        }

        /// <summary>
        /// Create demo potential for display purposes
        /// </summary>
        private DevelopmentPotential CreateDemoPotential(Player player)
        {
            // Use the training manager's GetRecommendedPrograms which calculates potential internally
            var recommendations = _trainingManager.GetRecommendedPrograms(player);
            
            // Create a demo potential based on player attributes and position
            var age = CalculateAge(player.DateOfBirth);
            var stage = GetDevelopmentStage(age);
            
            var potential = new DevelopmentPotential
            {
                PlayerId = player.Id,
                OverallPotential = UnityEngine.Random.Range(60f, 95f),
                DevelopmentRate = stage == DevelopmentStage.Rookie ? 1.5f : 
                                stage == DevelopmentStage.Developing ? 1.2f : 
                                stage == DevelopmentStage.Prime ? 1.0f : 0.7f,
                InjuryProneness = UnityEngine.Random.Range(0.8f, 1.3f),
                PreferredTraining = GetPreferredTrainingForPosition(player.Position)
            };
            
            // Set attribute potentials
            var attrs = player.Attributes;
            potential.AttributePotentials["Kicking"] = Mathf.Min(95f, attrs.Kicking + UnityEngine.Random.Range(5f, 25f));
            potential.AttributePotentials["Marking"] = Mathf.Min(95f, attrs.Marking + UnityEngine.Random.Range(5f, 25f));
            potential.AttributePotentials["Handballing"] = Mathf.Min(95f, attrs.Handballing + UnityEngine.Random.Range(5f, 25f));
            potential.AttributePotentials["Contested"] = Mathf.Min(95f, attrs.Contested + UnityEngine.Random.Range(5f, 25f));
            potential.AttributePotentials["Endurance"] = Mathf.Min(95f, attrs.Endurance + UnityEngine.Random.Range(5f, 25f));
            
            return potential;
        }
        
        private List<TrainingFocus> GetPreferredTrainingForPosition(Position position)
        {
            switch (position)
            {
                case Position.FullForward:
                    return new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.Strength};
                case Position.Centre:
                    return new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Handballing, TrainingFocus.DecisionMaking};
                case Position.Ruckman:
                    return new List<TrainingFocus> {TrainingFocus.Strength, TrainingFocus.Contested, TrainingFocus.Marking};
                case Position.HalfBack:
                    return new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.DecisionMaking};
                default:
                    return new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Kicking};
            }
        }

        #endregion

        #region Static Demo Runner (For Batch Mode)

        /// <summary>
        /// Static method for running training demo in batch mode
        /// </summary>
        public static void RunBatchDemo()
        {
            var demoObject = new GameObject("TrainingSystemDemo");
            var demo = demoObject.AddComponent&lt;TrainingSystemDemo&gt;();
            demo.RunDemo();
            DestroyImmediate(demoObject);
        }

        #endregion
    }
}