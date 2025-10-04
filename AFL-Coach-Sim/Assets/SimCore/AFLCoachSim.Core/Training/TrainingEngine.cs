using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Core training engine responsible for calculating development, managing programs, and processing outcomes
    /// </summary>
    public class TrainingEngine
    {
        private readonly Random _random;
        private readonly Dictionary<int, DevelopmentPotential> _playerPotentials;
        private readonly Dictionary<int, List<TrainingEfficiency>> _efficiencyHistory;

        public TrainingEngine(int seed = 0)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
            _playerPotentials = new Dictionary<int, DevelopmentPotential>();
            _efficiencyHistory = new Dictionary<int, List<TrainingEfficiency>>();
        }

        /// <summary>
        /// Calculate development potential for a player based on age, attributes, and position
        /// </summary>
        public DevelopmentPotential CalculatePlayerPotential(Player player)
        {
            int age = player.Age;
            var stage = GetDevelopmentStage(age);
            
            var potential = new DevelopmentPotential
            {
                PlayerId = player.Id,
                OverallPotential = CalculateOverallPotential(player, age),
                DevelopmentRate = CalculateDevelopmentRate(age, stage)
            };

            // Calculate attribute-specific potentials based on position and current ratings
            var attributes = player.Attr;
            potential.AttributePotentials["Kicking"] = CalculateAttributePotential(attributes.Kicking, GetRoleBonus(player.PrimaryRole, "Kicking"));
            potential.AttributePotentials["Marking"] = CalculateAttributePotential(attributes.Marking, GetRoleBonus(player.PrimaryRole, "Marking"));
            potential.AttributePotentials["Handball"] = CalculateAttributePotential(attributes.Handball, GetRoleBonus(player.PrimaryRole, "Handball"));
            potential.AttributePotentials["Clearance"] = CalculateAttributePotential(attributes.Clearance, GetRoleBonus(player.PrimaryRole, "Clearance"));
            potential.AttributePotentials["WorkRate"] = CalculateAttributePotential(attributes.WorkRate, GetRoleBonus(player.PrimaryRole, "WorkRate"));

            // Set preferred training based on position
            potential.PreferredTraining = GetPreferredTrainingForRole(player.PrimaryRole);
            
            // Injury proneness based on age, position, and random factors
            potential.InjuryProneness = CalculateInjuryProneness(age, player.PrimaryRole);

            _playerPotentials[player.Id] = potential;
            return potential;
        }

        /// <summary>
        /// Execute a training session and calculate outcomes for participating players
        /// </summary>
        public Dictionary<int, TrainingOutcome> ExecuteTrainingSession(TrainingProgram program, TrainingSession session, List<Player> players)
        {
            var outcomes = new Dictionary<int, TrainingOutcome>();

            foreach (var player in players)
            {
                var potential = GetOrCreatePlayerPotential(player);
                var stage = GetDevelopmentStage(player.Age);
                var outcome = CalculateTrainingOutcome(program, session, player, potential, stage);
                
                outcomes[player.Id] = outcome;
                
                // Update efficiency tracking
                UpdateEfficiencyTracking(player.Id, program, outcome);
            }

            return outcomes;
        }

        /// <summary>
        /// Calculate the outcome of a training session for a specific player
        /// </summary>
        private TrainingOutcome CalculateTrainingOutcome(TrainingProgram program, TrainingSession session, Player player, DevelopmentPotential potential, DevelopmentStage stage)
        {
            var outcome = new TrainingOutcome();
            
            // Base effectiveness calculation
            float effectiveness = program.CalculateEffectiveness(player, stage, potential);
            
            // Intensity modifier
            float intensityMultiplier = (int)session.Intensity * 0.25f; // Light=0.25, Moderate=0.5, High=0.75, Elite=1.0
            
            // Development rate modifier
            effectiveness *= potential.DevelopmentRate;
            
            // Random variation (±20%)
            float randomFactor = 0.8f + ((float)_random.NextDouble() * 0.4f);
            effectiveness *= randomFactor;

            // Calculate attribute gains
            foreach (var target in program.AttributeTargets)
            {
                string attribute = target.Key;
                float targetImprovement = target.Value;
                
                if (potential.AttributePotentials.ContainsKey(attribute))
                {
                    float currentRating = GetPlayerAttributeValue(player, attribute);
                    float attributePotential = potential.AttributePotentials[attribute];
                    
                    // Calculate gain (diminishing returns as player approaches potential)
                    float potentialRemaining = Math.Max(0, attributePotential - currentRating);
                    float gainMultiplier = Math.Min(1.0f, potentialRemaining / 20f); // Slow down when within 20 points of potential
                    
                    float gain = targetImprovement * effectiveness * intensityMultiplier * gainMultiplier;
                    outcome.AttributeGains[attribute] = Math.Max(0, gain);
                }
            }

            // Calculate side effects
            outcome.InjuryRisk = CalculateInjuryRisk(program, session, player, potential);
            outcome.FatigueAccumulation = CalculateFatigue(program, session, intensityMultiplier);
            outcome.MoraleImpact = CalculateMoraleImpact(effectiveness, session.Intensity);
            outcome.TeamChemistryImpact = CalculateTeamChemistryImpact(program.Type);

            // Special effects based on training type and outcomes
            AddSpecialEffects(outcome, program, effectiveness);

            return outcome;
        }

        /// <summary>
        /// Get or create development potential for a player
        /// </summary>
        private DevelopmentPotential GetOrCreatePlayerPotential(Player player)
        {
            if (_playerPotentials.ContainsKey(player.Id))
                return _playerPotentials[player.Id];
            
            return CalculatePlayerPotential(player);
        }

        /// <summary>
        /// Calculate overall potential for a player (0-100 scale)
        /// </summary>
        private float CalculateOverallPotential(Player player, int age)
        {
            // Base potential influenced by current attributes and age
            var attrs = player.Attr;
            float currentAverage = (attrs.Kicking + attrs.Marking + attrs.Handball + attrs.Clearance + attrs.WorkRate) / 5f;
            
            // Younger players have higher potential ceiling
            float ageFactor = age <= 20 ? 1.3f : age <= 25 ? 1.1f : age <= 29 ? 1.0f : 0.8f;
            
            // Random potential variation (±15 points)
            float randomVariation = -15f + ((float)_random.NextDouble() * 30f);
            
            float potential = (currentAverage * ageFactor) + randomVariation;
            return Math.Max(30f, Math.Min(100f, potential)); // Clamp between 30-100
        }

        /// <summary>
        /// Calculate development rate based on age and stage
        /// </summary>
        private float CalculateDevelopmentRate(int age, DevelopmentStage stage)
        {
            switch (stage)
            {
                case DevelopmentStage.Rookie:
                    return 1.4f + ((float)_random.NextDouble() * 0.3f); // 1.4-1.7x rate
                case DevelopmentStage.Developing:
                    return 1.1f + ((float)_random.NextDouble() * 0.2f); // 1.1-1.3x rate
                case DevelopmentStage.Prime:
                    return 0.8f + ((float)_random.NextDouble() * 0.3f); // 0.8-1.1x rate
                case DevelopmentStage.Veteran:
                    return 0.5f + ((float)_random.NextDouble() * 0.3f); // 0.5-0.8x rate
                case DevelopmentStage.Declining:
                    return 0.2f + ((float)_random.NextDouble() * 0.3f); // 0.2-0.5x rate
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Calculate attribute potential considering position and natural talent
        /// </summary>
        private float CalculateAttributePotential(float currentValue, float positionBonus)
        {
            // Base potential is current value plus growth room modified by position relevance
            float growthRoom = (100 - currentValue) * (0.6f + positionBonus * 0.4f);
            float randomVariation = (float)_random.NextDouble() * 20f - 10f; // ±10 points
            
            return Math.Max(currentValue + 5f, Math.Min(100f, currentValue + growthRoom + randomVariation));
        }

        /// <summary>
        /// Get role-specific bonus for different attributes
        /// </summary>
        private float GetRoleBonus(Role role, string attribute)
        {
            var bonuses = new Dictionary<(Role, string), float>
            {
                // Key Position Forwards
                {(Role.KPF, "Kicking"), 0.8f},
                {(Role.KPF, "Marking"), 0.9f},
                {(Role.SMLF, "Kicking"), 0.7f},
                {(Role.HFF, "Kicking"), 0.6f},
                {(Role.HFF, "Clearance"), 0.5f},
                
                // Midfielders  
                {(Role.MID, "WorkRate"), 0.9f},
                {(Role.MID, "Handball"), 0.8f},
                {(Role.MID, "Clearance"), 0.8f},
                {(Role.WING, "WorkRate"), 0.8f},
                
                // Defenders
                {(Role.KPD, "Marking"), 0.8f},
                {(Role.HBF, "Kicking"), 0.7f},
                {(Role.SMLB, "Clearance"), 0.6f},
                
                // Ruck
                {(Role.RUC, "Marking"), 0.9f},
                {(Role.RUC, "Clearance"), 0.8f}
            };
            
            return bonuses.ContainsKey((role, attribute)) ? bonuses[(role, attribute)] : 0.3f;
        }

        /// <summary>
        /// Get preferred training focuses for each role
        /// </summary>
        private List<TrainingFocus> GetPreferredTrainingForRole(Role role)
        {
            var preferences = new Dictionary<Role, List<TrainingFocus>>
            {
                {Role.KPF, new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.Strength}},
                {Role.HFF, new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Speed, TrainingFocus.Contested}},
                {Role.MID, new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Handballing, TrainingFocus.DecisionMaking}},
                {Role.WING, new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Speed, TrainingFocus.Kicking}},
                {Role.HBF, new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.DecisionMaking}},
                {Role.KPD, new List<TrainingFocus> {TrainingFocus.Marking, TrainingFocus.Strength, TrainingFocus.Positioning}},
                {Role.RUC, new List<TrainingFocus> {TrainingFocus.Strength, TrainingFocus.Contested, TrainingFocus.Marking}}
            };

            return preferences.ContainsKey(role) ? preferences[role] : new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Kicking};
        }

        private float CalculateInjuryRisk(TrainingProgram program, TrainingSession session, Player player, DevelopmentPotential potential)
        {
            float baseRisk = 0.01f * (int)session.Intensity; // 1-4% base risk
            baseRisk *= program.InjuryRiskModifier;
            baseRisk *= potential.InjuryProneness;
            
            // Age factor - older players more injury prone
            int age = player.Age;
            if (age > 30) baseRisk *= 1.5f;
            else if (age > 26) baseRisk *= 1.2f;
            
            return baseRisk;
        }

        private float CalculateFatigue(TrainingProgram program, TrainingSession session, float intensityMultiplier)
        {
            float baseFatigue = 5f + (10f * intensityMultiplier); // 7.5 to 15 fatigue points
            return baseFatigue * program.FatigueRateModifier;
        }

        private float CalculateMoraleImpact(float effectiveness, TrainingIntensity intensity)
        {
            float baseMorale = effectiveness > 1.0f ? 2f : effectiveness > 0.8f ? 1f : 0f;
            float intensityPenalty = (int)intensity > 3 ? -1f : 0f; // Elite intensity can reduce morale
            return baseMorale + intensityPenalty;
        }

        private float CalculateTeamChemistryImpact(TrainingType type)
        {
            switch (type)
            {
                case TrainingType.Leadership:
                    return 3f;
                case TrainingType.Tactical:
                    return 2f;
                case TrainingType.Skills:
                    return 1f;
                default:
                    return 0.5f;
            }
        }

        private void AddSpecialEffects(TrainingOutcome outcome, TrainingProgram program, float effectiveness)
        {
            if (effectiveness > 1.5f)
            {
                outcome.SpecialEffects.Add("Exceptional training response - accelerated development");
            }
            
            if (program.Type == TrainingType.Leadership && effectiveness > 1.0f)
            {
                outcome.SpecialEffects.Add("Leadership qualities improved - positive influence on teammates");
            }
            
            if (program.Type == TrainingType.Recovery)
            {
                outcome.SpecialEffects.Add("Enhanced recovery - reduced injury risk for next match");
                outcome.InjuryRisk *= 0.5f; // Recovery training reduces injury risk
            }
        }

        private float GetPlayerAttributeValue(Player player, string attribute)
        {
            var attrs = player.Attr;
            switch (attribute)
            {
                case "Kicking":
                    return attrs.Kicking;
                case "Marking":
                    return attrs.Marking;
                case "Handball":
                    return attrs.Handball;
                case "Clearance":
                    return attrs.Clearance;
                case "WorkRate":
                    return attrs.WorkRate;
                default:
                    return 50f;
            }
        }

        private void UpdateEfficiencyTracking(int playerId, TrainingProgram program, TrainingOutcome outcome)
        {
            if (!_efficiencyHistory.ContainsKey(playerId))
                _efficiencyHistory[playerId] = new List<TrainingEfficiency>();

            var efficiency = new TrainingEfficiency
            {
                PlayerId = playerId,
                TrainingType = program.Type,
                Focus = program.PrimaryFocus,
                AverageGain = outcome.AttributeGains.Values.Any() ? outcome.AttributeGains.Values.Average() : 0f,
                InjuryIncidence = outcome.InjuryRisk,
                SessionsCompleted = 1
            };

            _efficiencyHistory[playerId].Add(efficiency);
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


        private float CalculateInjuryProneness(int age, Role role)
        {
            float baseProneness = 1.0f;
            
            // Age factor
            if (age > 30) baseProneness += 0.3f;
            else if (age < 20) baseProneness += 0.1f; // Young players slightly more prone due to inexperience
            
            // Role factor - more physical roles have higher injury risk
            var physicalRoles = new[] { Role.RUC, Role.KPF, Role.KPD, Role.MID };
            if (physicalRoles.Contains(role))
                baseProneness += 0.2f;
                
            // Random individual variation
            baseProneness += ((float)_random.NextDouble() * 0.4f) - 0.2f; // ±0.2 variation
            
            return Math.Max(0.5f, Math.Min(2.0f, baseProneness)); // Clamp between 0.5x and 2.0x
        }

        /// <summary>
        /// Get efficiency history for a player
        /// </summary>
        public List<TrainingEfficiency> GetPlayerEfficiencyHistory(int playerId)
        {
            return _efficiencyHistory.ContainsKey(playerId) ? _efficiencyHistory[playerId] : new List<TrainingEfficiency>();
        }

        /// <summary>
        /// Get development potential for a player
        /// </summary>
        public DevelopmentPotential GetPlayerPotential(int playerId)
        {
            return _playerPotentials.ContainsKey(playerId) ? _playerPotentials[playerId] : null;
        }
    }
}