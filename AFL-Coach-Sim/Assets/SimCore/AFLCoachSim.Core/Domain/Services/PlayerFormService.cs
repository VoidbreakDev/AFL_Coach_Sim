using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLCoachSim.Core.Domain.Services
{
    /// <summary>
    /// Core service for managing player form and condition
    /// Provides the business logic for form changes separate from Unity dependencies
    /// </summary>
    public class PlayerFormService
    {
        private readonly Random _random;

        public PlayerFormService(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Updates player form based on match performance
        /// </summary>
        /// <param name="player">Player to update</param>
        /// <param name="performance">Match performance rating (1-10 scale)</param>
        /// <param name="minutesPlayed">Minutes played in the match</param>
        /// <param name="wasInjured">Whether player was injured during the match</param>
        public void UpdateAfterMatch(Player player, int performance, int minutesPlayed, bool wasInjured = false)
        {
            if (player == null) return;

            // Update form based on performance (1-10 scale)
            UpdateForm(player, performance);
            
            // Update condition based on minutes played and current state
            UpdateCondition(player, minutesPlayed, wasInjured);
        }

        /// <summary>
        /// Processes daily recovery for a player
        /// </summary>
        /// <param name="player">Player to process recovery for</param>
        public void ProcessDailyRecovery(Player player)
        {
            if (player == null) return;

            // Recover condition gradually
            float recoveryAmount = CalculateRecoveryRate(player);
            player.Condition = Math.Min(100, player.Condition + (int)recoveryAmount);

            // Apply natural form fluctuations
            ApplyNaturalFormChange(player);
        }

        /// <summary>
        /// Gets performance modifier based on current form and condition
        /// </summary>
        /// <param name="player">Player to calculate modifier for</param>
        /// <returns>Performance modifier (0.6 to 1.3 range)</returns>
        public float GetPerformanceModifier(Player player)
        {
            if (player == null) return 1.0f;

            // Form modifier: -20 to +20 form gives ±15% performance
            float formMod = player.Form / 20.0f * 0.15f;
            
            // Condition modifier: condition below 75 reduces performance
            float conditionMod = Math.Max(0, player.Condition - 75) / 25.0f * 0.1f;
            
            // Age factor: players over 30 have slightly reduced peak
            float ageMod = player.Age > 30 ? -0.02f : 0f;
            
            float totalModifier = 1.0f + formMod + conditionMod + ageMod;
            
            // Clamp to reasonable range
            return Math.Max(0.6f, Math.Min(1.3f, totalModifier));
        }

        /// <summary>
        /// Gets a descriptive status of the player's current state
        /// </summary>
        public PlayerFormStatus GetPlayerStatus(Player player)
        {
            if (player == null) return PlayerFormStatus.Available;
            
            if (player.Condition < 30) return PlayerFormStatus.Exhausted;
            if (player.Form < -15) return PlayerFormStatus.OutOfForm;
            if (player.Form > 15 && player.Condition > 85) return PlayerFormStatus.InExcellentForm;
            
            return PlayerFormStatus.Available;
        }

        private void UpdateForm(Player player, int performance)
        {
            float change = 0f;
            
            // Convert 1-10 performance to form change
            switch (performance)
            {
                case 1:
                case 2: // Poor
                    change = _random.Next(-8, -4);
                    break;
                case 3:
                case 4: // Below
                    change = _random.Next(-4, -1);
                    break;
                case 5:
                case 6: // Average
                    change = _random.Next(-1, 3);
                    break;
                case 7:
                case 8: // Good
                    change = _random.Next(2, 6);
                    break;
                case 9: // Excellent
                    change = _random.Next(6, 10);
                    break;
                case 10: // Exceptional
                    change = _random.Next(8, 15);
                    break;
                default:
                    change = 0;
                    break;
            }
            
            // Adjust based on current form (harder to improve when already high)
            if (player.Form > 15 && change > 0) change *= 0.5f;
            if (player.Form < -15 && change < 0) change *= 0.5f;
            
            player.Form = Math.Max(-20, Math.Min(20, player.Form + (int)change));
        }

        private void UpdateCondition(Player player, int minutesPlayed, bool wasInjured)
        {
            // Condition decreases based on minutes played
            float conditionLoss = minutesPlayed / 90.0f * 15.0f; // Up to 15 points for full game
            
            // Extra loss if already low condition
            if (player.Condition < 50) conditionLoss *= 1.3f;
            
            // More loss if injured during match
            if (wasInjured) conditionLoss *= 1.5f;
            
            player.Condition = Math.Max(10, player.Condition - (int)conditionLoss);
        }

        private float CalculateRecoveryRate(Player player)
        {
            float baseRecovery = 2.0f;
            
            // Better recovery if low condition
            if (player.Condition < 50) baseRecovery *= 1.3f;
            
            // Slower recovery if older
            if (player.Age > 30) baseRecovery *= 0.8f;
            if (player.Age > 35) baseRecovery *= 0.7f;
            
            // Recovery affected by durability
            baseRecovery *= player.Durability / 50.0f;
            
            return baseRecovery;
        }

        private void ApplyNaturalFormChange(Player player)
        {
            // Small random fluctuations in form
            float naturalChange = ((float)_random.NextDouble() - 0.5f) * 1.0f; // -0.5 to +0.5
            
            // Trend towards average form over time
            if (player.Form > 10) naturalChange -= 0.3f;
            else if (player.Form < -10) naturalChange += 0.3f;
            
            player.Form = Math.Max(-20, Math.Min(20, player.Form + (int)naturalChange));
        }
    }

    public enum PlayerFormStatus
    {
        Available,
        OutOfForm,
        Exhausted,
        InExcellentForm
    }
}
