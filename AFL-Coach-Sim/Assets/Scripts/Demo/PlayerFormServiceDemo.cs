using UnityEngine;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.Services;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLManager
{
    /// <summary>
    /// Demo script to test the PlayerFormService functionality
    /// </summary>
    public class PlayerFormServiceDemo : MonoBehaviour
    {
        [Header("Demo Controls")]
        [SerializeField] private bool runDemo = false;
        [SerializeField] private int simulationDays = 30;
        [SerializeField] private int matchesPerWeek = 1;
        
        private PlayerFormService formService;
        private Player testPlayer;

        void Start()
        {
            if (runDemo)
            {
                RunFormServiceDemo();
            }
        }

        [ContextMenu("Run Form Service Demo")]
        public void RunFormServiceDemo()
        {
            Debug.Log("=== Player Form Service Demo ===");
            
            // Initialize service and test player
            formService = new PlayerFormService(42); // Fixed seed for consistency
            testPlayer = CreateTestPlayer();
            
            Debug.Log($"Initial State: {GetPlayerStateString(testPlayer)}");
            
            // Simulate a season with matches and recovery
            SimulatePlayerSeason();
            
            Debug.Log($"Final State: {GetPlayerStateString(testPlayer)}");
            Debug.Log("=== Demo Complete ===");
        }

        private Player CreateTestPlayer()
        {
            return new Player
            {
                Id = new PlayerId(1),
                Name = "Demo Player",
                Age = 25,
                PrimaryRole = Role.MID,
                Condition = 100,
                Form = 0,
                Endurance = 70,
                Durability = 60,
                Discipline = 50,
                Attr = new Attributes
                {
                    Kicking = 75,
                    Handball = 80,
                    Speed = 70,
                    Tackling = 65,
                    WorkRate = 75,
                    DecisionMaking = 60,
                    Composure = 70
                }
            };
        }

        private void SimulatePlayerSeason()
        {
            int day = 0;
            int matchesPlayed = 0;
            
            while (day < simulationDays)
            {
                // Check if it's a match day (roughly once per week)
                bool isMatchDay = day % (7 / matchesPerWeek) == 0 && day > 0;
                
                if (isMatchDay)
                {
                    SimulateMatch(matchesPlayed);
                    matchesPlayed++;
                }
                else
                {
                    // Rest day - daily recovery
                    formService.ProcessDailyRecovery(testPlayer);
                }
                
                // Log significant changes
                if (day % 7 == 0) // Weekly update
                {
                    LogWeeklyUpdate(day / 7, matchesPlayed);
                }
                
                day++;
            }
        }

        private void SimulateMatch(int matchNumber)
        {
            // Random performance between 1-10 (with some bias toward average)
            int performance = Random.Range(3, 9); // Slightly biased toward average performances
            
            // Random minutes played (60-90 minutes)
            int minutesPlayed = Random.Range(60, 91);
            
            // Small chance of injury (5%)
            bool wasInjured = Random.Range(0f, 1f) < 0.05f;
            
            Debug.Log($"Match {matchNumber + 1}: Performance={performance}/10, Minutes={minutesPlayed}, Injured={wasInjured}");
            
            formService.UpdateAfterMatch(testPlayer, performance, minutesPlayed, wasInjured);
            
            // Show immediate post-match state
            var status = formService.GetPlayerStatus(testPlayer);
            var modifier = formService.GetPerformanceModifier(testPlayer);
            
            Debug.Log($"  Post-Match: {GetPlayerStateString(testPlayer)}, Status={status}, Modifier={modifier:F3}");
        }

        private void LogWeeklyUpdate(int week, int totalMatches)
        {
            var status = formService.GetPlayerStatus(testPlayer);
            var modifier = formService.GetPerformanceModifier(testPlayer);
            
            Debug.Log($"Week {week}: {GetPlayerStateString(testPlayer)}, " +
                      $"Status={status}, Modifier={modifier:F3}, Matches Played={totalMatches}");
        }

        private string GetPlayerStateString(Player player)
        {
            return $"Form={player.Form}, Condition={player.Condition}%";
        }

        [ContextMenu("Test Form Boundaries")]
        public void TestFormBoundaries()
        {
            Debug.Log("=== Testing Form Boundaries ===");
            
            formService = new PlayerFormService();
            testPlayer = CreateTestPlayer();
            
            // Test maximum form
            testPlayer.Form = 19;
            Debug.Log($"Starting near max form: {testPlayer.Form}");
            
            for (int i = 0; i < 5; i++)
            {
                formService.UpdateAfterMatch(testPlayer, 10, 90); // Exceptional performances
                Debug.Log($"After exceptional match {i + 1}: Form={testPlayer.Form}");
            }
            
            // Test minimum form
            testPlayer.Form = -19;
            Debug.Log($"Starting near min form: {testPlayer.Form}");
            
            for (int i = 0; i < 5; i++)
            {
                formService.UpdateAfterMatch(testPlayer, 1, 90); // Poor performances
                Debug.Log($"After poor match {i + 1}: Form={testPlayer.Form}");
            }
        }

        [ContextMenu("Test Age Effects")]
        public void TestAgeEffects()
        {
            Debug.Log("=== Testing Age Effects ===");
            
            formService = new PlayerFormService();
            
            // Create players of different ages
            var youngPlayer = CreateTestPlayer();
            youngPlayer.Age = 20;
            youngPlayer.Condition = 50;
            
            var oldPlayer = CreateTestPlayer();
            oldPlayer.Age = 35;
            oldPlayer.Condition = 50;
            
            Debug.Log($"Young Player (Age {youngPlayer.Age}): Condition={youngPlayer.Condition}%");
            Debug.Log($"Old Player (Age {oldPlayer.Age}): Condition={oldPlayer.Condition}%");
            
            // Simulate 10 days of recovery
            for (int i = 0; i < 10; i++)
            {
                formService.ProcessDailyRecovery(youngPlayer);
                formService.ProcessDailyRecovery(oldPlayer);
            }
            
            Debug.Log($"After 10 days recovery:");
            Debug.Log($"Young Player: Condition={youngPlayer.Condition}% (recovered {youngPlayer.Condition - 50}%)");
            Debug.Log($"Old Player: Condition={oldPlayer.Condition}% (recovered {oldPlayer.Condition - 50}%)");
            
            // Test performance modifiers
            var youngModifier = formService.GetPerformanceModifier(youngPlayer);
            var oldModifier = formService.GetPerformanceModifier(oldPlayer);
            
            Debug.Log($"Performance Modifiers - Young: {youngModifier:F3}, Old: {oldModifier:F3}");
        }
    }
}
