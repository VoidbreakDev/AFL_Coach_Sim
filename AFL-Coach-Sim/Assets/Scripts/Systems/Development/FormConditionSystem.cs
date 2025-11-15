using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Tracks player form, condition, and recovery patterns
    /// </summary>
    [System.Serializable]
    public class PlayerFormCondition
    {
        [Header("Current State")]
        [Range(0f, 100f)] public float Form = 75f;           // Current form level (affects performance)
        [Range(0f, 100f)] public float Condition = 100f;    // Physical condition (fitness/freshness)  
        [Range(0f, 100f)] public float Confidence = 75f;    // Mental confidence level
        [Range(0f, 100f)] public float Fatigue = 0f;        // Accumulated tiredness
        
        [Header("Recovery")]
        public float RecoveryRate = 1.0f;                    // How quickly they recover (0.5-2.0)
        public DateTime LastMatchDate = DateTime.MinValue;   // When they last played
        public int ConsecutiveMatches = 0;                   // Matches without a break
        
        [Header("Form History")]
        public List<FormSnapshot> RecentForm;                // Last 10 performances
        public FormTrend CurrentTrend = FormTrend.Stable;    // Current form trajectory
        
        [Header("Injuries & Issues")]
        public List<PlayerIssue> ActiveIssues;               // Current injuries, suspensions, etc.
        public float InjuryProneness = 1.0f;                 // Multiplier for injury risk (0.5-2.0)
        
        public PlayerFormCondition()
        {
            RecentForm = new List<FormSnapshot>();
            ActiveIssues = new List<PlayerIssue>();
        }
        
        /// <summary>
        /// Updates form and condition after a match performance
        /// </summary>
        public void UpdateAfterMatch(MatchPerformanceRating performance, int minutesPlayed, bool injured = false)
        {
            // Update form based on performance
            UpdateForm(performance);
            
            // Update condition based on minutes played and current fitness
            UpdateCondition(minutesPlayed, injured);
            
            // Update confidence based on performance and form trend
            UpdateConfidence(performance);
            
            // Record the performance
            RecordPerformance(performance, minutesPlayed);
            
            // Update match tracking
            LastMatchDate = DateTime.Now;
            ConsecutiveMatches++;
            
            // Check for injuries
            if (injured)
            {
                AddInjury(GenerateInjury(performance, minutesPlayed));
            }
        }
        
        /// <summary>
        /// Processes daily recovery and form changes
        /// </summary>
        public void ProcessDailyRecovery()
        {
            // Recover fatigue
            float recoveryAmount = RecoveryRate * CalculateRecoveryBonus();
            Fatigue = Mathf.Max(0f, Fatigue - recoveryAmount);
            
            // Improve condition if not playing
            if (!PlayedRecently(1)) // If haven't played in last day
            {
                Condition = Mathf.Min(100f, Condition + recoveryAmount * 0.5f);
            }
            
            // Natural form fluctuations
            ApplyNaturalFormChange();
            
            // Process injuries and issues
            ProcessActiveIssues();
            
            // Reset consecutive matches if rested
            if (!PlayedRecently(7)) // If haven't played in a week
            {
                ConsecutiveMatches = 0;
            }
        }
        
        /// <summary>
        /// Gets overall performance modifier based on current state
        /// </summary>
        public float GetPerformanceModifier()
        {
            float formMod = (Form - 50f) / 50f * 0.15f;      // ±15% based on form
            float conditionMod = (Condition - 75f) / 25f * 0.1f; // ±10% based on condition
            float confidenceMod = (Confidence - 50f) / 50f * 0.08f; // ±8% based on confidence
            float fatigueMod = -Fatigue / 100f * 0.12f;       // Up to -12% when fully fatigued
            float injuryMod = CalculateInjuryModifier();      // Varies based on injuries
            
            return Mathf.Clamp(1f + formMod + conditionMod + confidenceMod + fatigueMod + injuryMod, 0.6f, 1.3f);
        }
        
        /// <summary>
        /// Gets a descriptive status of the player's current state
        /// </summary>
        public PlayerStatus GetCurrentStatus()
        {
            if (HasSignificantInjury()) return PlayerStatus.Injured;
            if (IsAvailableForSelection()) return PlayerStatus.Available;
            if (Fatigue > 80f) return PlayerStatus.Exhausted;
            if (Form < 40f) return PlayerStatus.OutOfForm;
            if (Form > 85f && Confidence > 85f) return PlayerStatus.InExcellentForm;
            return PlayerStatus.Available;
        }
        
        protected void UpdateForm(MatchPerformanceRating performance)
        {
            float change = 0f;
            
            switch (performance)
            {
                case MatchPerformanceRating.Poor:
                    change = UnityEngine.Random.Range(-8f, -4f);
                    break;
                case MatchPerformanceRating.Below:
                    change = UnityEngine.Random.Range(-4f, -1f);
                    break;
                case MatchPerformanceRating.Average:
                    change = UnityEngine.Random.Range(-1f, 2f);
                    break;
                case MatchPerformanceRating.Good:
                    change = UnityEngine.Random.Range(2f, 6f);
                    break;
                case MatchPerformanceRating.Excellent:
                    change = UnityEngine.Random.Range(6f, 10f);
                    break;
                case MatchPerformanceRating.Exceptional:
                    change = UnityEngine.Random.Range(8f, 15f);
                    break;
            }
            
            // Adjust based on current form (harder to improve when already high)
            if (Form > 80f && change > 0) change *= 0.5f;
            if (Form < 30f && change < 0) change *= 0.5f; // Cushion against very low form
            
            Form = Mathf.Clamp(Form + change, 10f, 95f);
            
            UpdateFormTrend();
        }
        
        protected void UpdateCondition(int minutesPlayed, bool injured)
        {
            // Condition decreases based on minutes played
            float conditionLoss = minutesPlayed / 90f * 15f; // Up to 15 points for full game
            
            // Extra loss if already fatigued
            if (Fatigue > 50f) conditionLoss *= 1.3f;
            
            // More loss if injured during match
            if (injured) conditionLoss *= 1.5f;
            
            Condition = Mathf.Max(20f, Condition - conditionLoss);
            
            // Add to fatigue
            Fatigue = Mathf.Min(100f, Fatigue + conditionLoss * 0.8f);
        }
        
        protected void UpdateConfidence(MatchPerformanceRating performance)
        {
            float change = ((float)performance - 5f) * 2f; // -8 to +10 based on performance
            
            // Confidence changes more slowly than form
            change *= 0.6f;
            
            // Boost if on a good form run
            if (CurrentTrend == FormTrend.Rising && change > 0) change *= 1.3f;
            if (CurrentTrend == FormTrend.Declining && change < 0) change *= 1.2f;
            
            Confidence = Mathf.Clamp(Confidence + change, 15f, 95f);
        }
        
        protected void RecordPerformance(MatchPerformanceRating performance, int minutesPlayed)
        {
            var snapshot = new FormSnapshot
            {
                Date = DateTime.Now,
                Performance = performance,
                MinutesPlayed = minutesPlayed,
                FormLevel = Form,
                ConditionLevel = Condition
            };
            
            RecentForm.Add(snapshot);
            
            // Keep only last 10 performances
            if (RecentForm.Count > 10)
                RecentForm.RemoveAt(0);
        }
        
        private void UpdateFormTrend()
        {
            if (RecentForm.Count < 3) return;
            
            var recent3 = RecentForm.TakeLast(3).ToList();
            var avg3 = recent3.Average(s => (float)s.Performance);
            
            if (RecentForm.Count >= 6)
            {
                var previous3 = RecentForm.Skip(RecentForm.Count - 6).Take(3).ToList();
                var prevAvg = previous3.Average(s => (float)s.Performance);
                
                if (avg3 > prevAvg + 0.8f) CurrentTrend = FormTrend.Rising;
                else if (avg3 < prevAvg - 0.8f) CurrentTrend = FormTrend.Declining;
                else CurrentTrend = FormTrend.Stable;
            }
        }
        
        private float CalculateRecoveryBonus()
        {
            float bonus = 1.0f;
            
            // Better recovery if low condition
            if (Condition < 50f) bonus *= 1.3f;
            
            // Slower recovery if old or injury-prone
            // This would need player age - assume accessible from parent
            // if (player.Age > 30) bonus *= 0.8f;
            
            if (InjuryProneness > 1.2f) bonus *= 0.9f;
            
            return bonus;
        }
        
        private void ApplyNaturalFormChange()
        {
            // Small random fluctuations in form
            float naturalChange = UnityEngine.Random.Range(-0.5f, 0.5f);
            
            // Trend towards average form over time
            if (Form > 75f) naturalChange -= 0.3f;
            else if (Form < 50f) naturalChange += 0.3f;
            
            Form = Mathf.Clamp(Form + naturalChange, 10f, 95f);
        }
        
        private void ProcessActiveIssues()
        {
            for (int i = ActiveIssues.Count - 1; i >= 0; i--)
            {
                var issue = ActiveIssues[i];
                issue.DaysRemaining--;
                
                if (issue.DaysRemaining <= 0)
                {
                    ActiveIssues.RemoveAt(i);
                    Debug.Log($"Player issue resolved: {issue.Type}");
                }
            }
        }
        
        private bool PlayedRecently(int days)
        {
            return LastMatchDate != DateTime.MinValue && 
                   (DateTime.Now - LastMatchDate).TotalDays <= days;
        }
        
        private float CalculateInjuryModifier()
        {
            float modifier = 0f;
            
            foreach (var issue in ActiveIssues)
            {
                modifier += issue.PerformanceImpact;
            }
            
            return Mathf.Clamp(modifier, -0.5f, 0f); // Only negative impact
        }
        
        private bool HasSignificantInjury()
        {
            return ActiveIssues.Any(i => i.Type == PlayerIssueType.Injury && i.Severity >= IssueSeverity.Moderate);
        }
        
        public bool IsAvailableForSelection()
        {
            return !ActiveIssues.Any(i => i.Type == PlayerIssueType.Suspension || 
                                         (i.Type == PlayerIssueType.Injury && i.Severity >= IssueSeverity.Major));
        }
        
        private void AddInjury(PlayerIssue injury)
        {
            ActiveIssues.Add(injury);
            
            // Immediate condition impact
            Condition = Mathf.Max(10f, Condition - injury.Severity switch
            {
                IssueSeverity.Minor => 5f,
                IssueSeverity.Moderate => 15f,
                IssueSeverity.Major => 30f,
                IssueSeverity.Severe => 50f,
                _ => 0f
            });
        }
        
        protected PlayerIssue GenerateInjury(MatchPerformanceRating performance, int minutesPlayed)
        {
            // More likely to be injured if played poorly (may indicate struggling)
            // or if played full game (fatigue)
            
            var severities = new[] { IssueSeverity.Minor, IssueSeverity.Moderate, IssueSeverity.Major };
            var severity = severities[UnityEngine.Random.Range(0, severities.Length)];
            
            // Adjust severity based on circumstances
            if (Fatigue > 80f || performance == MatchPerformanceRating.Poor)
            {
                severity = (IssueSeverity)Mathf.Min((int)severity + 1, (int)IssueSeverity.Major);
            }
            
            return new PlayerIssue
            {
                Type = PlayerIssueType.Injury,
                Severity = severity,
                DaysRemaining = severity switch
                {
                    IssueSeverity.Minor => UnityEngine.Random.Range(3, 8),
                    IssueSeverity.Moderate => UnityEngine.Random.Range(10, 21),
                    IssueSeverity.Major => UnityEngine.Random.Range(28, 84),
                    IssueSeverity.Severe => UnityEngine.Random.Range(84, 365),
                    _ => 7
                },
                PerformanceImpact = severity switch
                {
                    IssueSeverity.Minor => -0.05f,
                    IssueSeverity.Moderate => -0.15f,
                    IssueSeverity.Major => -0.35f,
                    IssueSeverity.Severe => -0.6f,
                    _ => 0f
                }
            };
        }
    }
    
    /// <summary>
    /// Snapshot of player form at a point in time
    /// </summary>
    [System.Serializable]
    public class FormSnapshot
    {
        public DateTime Date;
        public MatchPerformanceRating Performance;
        public int MinutesPlayed;
        public float FormLevel;
        public float ConditionLevel;
    }
    
    /// <summary>
    /// Current form trajectory
    /// </summary>
    public enum FormTrend
    {
        Declining,      // Form getting worse
        Stable,         // Form relatively consistent
        Rising          // Form improving
    }
    
    /// <summary>
    /// Player availability status
    /// </summary>
    public enum PlayerStatus
    {
        Available,
        OutOfForm,
        Exhausted,
        Injured,
        Suspended,
        InExcellentForm
    }
    
    /// <summary>
    /// Issues that can affect a player
    /// </summary>
    [System.Serializable]
    public class PlayerIssue
    {
        public PlayerIssueType Type;
        public IssueSeverity Severity;
        public int DaysRemaining;
        public float PerformanceImpact; // Negative value representing performance reduction
        public string Description;
        
        public override string ToString()
        {
            return $"{Type} ({Severity}) - {DaysRemaining} days remaining";
        }
    }
    
    public enum PlayerIssueType
    {
        Injury,
        Illness,
        Suspension,
        PersonalLeave,
        Disciplinary
    }
    
    public enum IssueSeverity
    {
        Minor,      // 1-2 weeks
        Moderate,   // 2-6 weeks
        Major,      // 6-16 weeks
        Severe      // Season-ending
    }
}
