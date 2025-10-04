using System.Collections.Generic;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems.Development;
using AFLCoachSim.Core.Development;
using AFLCoachSim.Core.Training;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Integration component that bridges the enhanced development framework with Unity systems
    /// </summary>
    public class PlayerDevelopmentIntegration : MonoBehaviour
    {
        [Header("Development Settings")]
        [SerializeField] private bool useEnhancedDevelopment = true;
        [SerializeField] private int developmentSeed = 12345;
        
        private PlayerDevelopmentFramework _enhancedFramework;
        private Dictionary<int, PlayerDevelopment> _legacyDevelopment; // Your existing system
        
        void Awake()
        {
            _enhancedFramework = new PlayerDevelopmentFramework(developmentSeed);
            _legacyDevelopment = new Dictionary<int, PlayerDevelopment>();
        }
        
        /// <summary>
        /// Process weekly development for a player integrating training and match experience
        /// </summary>
        public PlayerStatsDelta ProcessPlayerDevelopment(Player player, TrainingProgram trainingProgram, 
            int matchesPlayed, float averageMatchRating)
        {
            if (!useEnhancedDevelopment)
            {
                // Fall back to existing development system
                return ProcessLegacyDevelopment(player, trainingProgram);
            }
            
            // Convert Unity Player to Core Player for enhanced framework
            var corePlayer = ConvertToCorePlayer(player);
            
            // Create mock training outcome (in real implementation, this would come from your TrainingEngine)
            var trainingOutcome = CreateMockTrainingOutcome(trainingProgram, player);
            
            // Process enhanced development
            var developmentUpdate = _enhancedFramework.ProcessDevelopment(
                corePlayer, trainingOutcome, matchesPlayed, averageMatchRating);
            
            // Convert results back to Unity format
            var statsDelta = ConvertToStatsDelta(developmentUpdate);
            
            // Log breakthrough events for player
            if (developmentUpdate.BreakthroughEvent != null)
            {
                LogBreakthroughEvent(player, developmentUpdate.BreakthroughEvent);
            }
            
            return statsDelta;
        }
        
        /// <summary>
        /// Get player's development profile for UI display
        /// </summary>
        public PlayerDevelopmentProfile GetPlayerProfile(Player player)
        {
            var corePlayer = ConvertToCorePlayer(player);
            return _enhancedFramework.GetOrCreateProfile(corePlayer);
        }
        
        /// <summary>
        /// Get available specializations for a player's position
        /// </summary>
        public List<PlayerSpecialization> GetAvailableSpecializations(Player player)
        {
            return PlayerDevelopmentHelpers.GetSpecializationsForPosition(player.Role.ToString());
        }
        
        #region Conversion Methods
        
        private Player ConvertToCorePlayer(AFLManager.Models.Player unityPlayer)
        {
            // Convert Unity player model to Core player model
            // This is a simplified example - you'd need proper conversion logic
            return new Player
            {
                Id = unityPlayer.GetHashCode(), // Use proper ID system
                Name = unityPlayer.Name,
                Position = ConvertPosition(unityPlayer.Role),
                DateOfBirth = System.DateTime.Now.AddYears(-unityPlayer.Age), // Approximate
                Attributes = ConvertAttributes(unityPlayer.Stats)
            };
        }
        
        private Position ConvertPosition(PlayerRole role)
        {
            // Convert Unity PlayerRole to Core Position enum
            // This mapping would need to match your enum structures
            return role switch
            {
                PlayerRole.FullBack => Position.FullBack,
                PlayerRole.HalfBack => Position.HalfBack,
                PlayerRole.Centre => Position.Centre,
                PlayerRole.Wing => Position.Wing,
                PlayerRole.FullForward => Position.FullForward,
                PlayerRole.HalfForward => Position.HalfForward,
                PlayerRole.Ruckman => Position.Ruckman,
                _ => Position.Utility
            };
        }
        
        private Attributes ConvertAttributes(PlayerStats stats)
        {
            // Convert Unity PlayerStats to Core Attributes
            return new Attributes
            {
                Kicking = stats.Kicking,
                Marking = stats.Marking,
                Handballing = stats.Handballing,
                Contested = stats.Tackling, // Map tackling to contested
                Endurance = stats.Stamina,
                // Add other attributes as needed
            };
        }
        
        private PlayerStatsDelta ConvertToStatsDelta(PlayerDevelopmentFramework.PlayerDevelopmentUpdate update)
        {
            var delta = new PlayerStatsDelta();
            var totalGains = update.GetTotalGains();
            
            // Map gains to Unity stat structure
            delta.Kicking = totalGains.GetValueOrDefault("Kicking", 0f);
            delta.Handballing = totalGains.GetValueOrDefault("Handballing", 0f);
            delta.Tackling = totalGains.GetValueOrDefault("Contested", 0f);
            delta.Speed = totalGains.GetValueOrDefault("Speed", 0f);
            delta.Stamina = totalGains.GetValueOrDefault("Endurance", 0f);
            delta.Knowledge = totalGains.GetValueOrDefault("DecisionMaking", 0f);
            delta.Playmaking = totalGains.GetValueOrDefault("Leadership", 0f);
            
            return delta;
        }
        
        #endregion
        
        #region Mock/Helper Methods
        
        private TrainingOutcome CreateMockTrainingOutcome(TrainingProgram program, AFLManager.Models.Player player)
        {
            // Create mock training outcome for demonstration
            // In real implementation, this would come from your TrainingEngine
            var outcome = new TrainingOutcome
            {
                AttributeGains = new Dictionary<string, float>
                {
                    ["Kicking"] = UnityEngine.Random.Range(0f, 0.5f),
                    ["Handballing"] = UnityEngine.Random.Range(0f, 0.4f),
                    ["Contested"] = UnityEngine.Random.Range(0f, 0.3f),
                    ["Endurance"] = UnityEngine.Random.Range(0f, 0.6f),
                    ["DecisionMaking"] = UnityEngine.Random.Range(0f, 0.2f)
                },
                InjuryRisk = 0.01f,
                FatigueAccumulation = 5f,
                MoraleImpact = 1f,
                TeamChemistryImpact = 0.5f
            };
            
            return outcome;
        }
        
        private PlayerStatsDelta ProcessLegacyDevelopment(AFLManager.Models.Player player, TrainingProgram trainingProgram)
        {
            // Get or create legacy development profile
            if (!_legacyDevelopment.ContainsKey(player.GetHashCode()))
            {
                _legacyDevelopment[player.GetHashCode()] = new PlayerDevelopment();
            }
            
            var legacyDev = _legacyDevelopment[player.GetHashCode()];
            
            // Use your existing development calculation
            // This is a placeholder - use your actual method
            return legacyDev.CalculateDevelopment(player, null); // Pass appropriate training program
        }
        
        #endregion
        
        #region Event Logging
        
        private void LogBreakthroughEvent(AFLManager.Models.Player player, BreakthroughEvent breakthroughEvent)
        {
            string color = breakthroughEvent.IsPositive ? "green" : "red";
            Debug.Log($"<color={color}>[BREAKTHROUGH]</color> {breakthroughEvent.Description}");
            
            // Here you could trigger UI notifications, save to player history, etc.
            // For example:
            // NotificationSystem.ShowPlayerBreakthrough(player, breakthroughEvent);
            // PlayerHistoryManager.RecordBreakthroughEvent(player, breakthroughEvent);
        }
        
        #endregion
        
        #region Testing Methods
        
        [ContextMenu("Test Development for Random Player")]
        private void TestDevelopmentForRandomPlayer()
        {
            // Create a test player for demonstration
            var testPlayer = new AFLManager.Models.Player
            {
                Name = "Test Player",
                Age = 22,
                Role = PlayerRole.Centre,
                Stats = new PlayerStats
                {
                    Kicking = 65,
                    Handballing = 70,
                    Tackling = 60,
                    Speed = 75,
                    Stamina = 80,
                    Knowledge = 55,
                    Playmaking = 62
                }
            };
            
            // Process development
            var result = ProcessPlayerDevelopment(testPlayer, null, 2, 7.5f);
            
            Debug.Log($"Development Result for {testPlayer.Name}:");
            Debug.Log($"Total Change: {result.GetTotalChange():F2}");
            Debug.Log($"Biggest Gains: Kicking +{result.Kicking:F2}, Stamina +{result.Stamina:F2}");
            
            // Show specialization info
            var profile = GetPlayerProfile(testPlayer);
            if (profile.CurrentSpecialization != null)
            {
                Debug.Log($"Current Specialization: {profile.CurrentSpecialization.Name} (Progress: {profile.SpecializationProgress:F1}%)");
            }
        }
        
        #endregion
    }
    
    #region Supporting Enums (if not already defined in your project)
    
    // These would need to match your existing enum structures
    public enum Position
    {
        FullBack, HalfBack, BackPocket, CentreHalfBack,
        Centre, Wing, Rover, RuckRover,
        FullForward, HalfForward, ForwardPocket, CentreHalfForward,
        Ruckman, Utility
    }
    
    public class Attributes
    {
        public float Kicking { get; set; }
        public float Marking { get; set; }
        public float Handballing { get; set; }
        public float Contested { get; set; }
        public float Endurance { get; set; }
        // Add other attributes as needed
    }
    
    #endregion
}