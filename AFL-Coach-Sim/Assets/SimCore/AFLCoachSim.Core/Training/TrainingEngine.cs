using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Models;

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
            int age = CalculateAge(player.DateOfBirth);
            var stage = GetDevelopmentStage(age);
            
            var potential = new DevelopmentPotential
            {
                PlayerId = player.Id,
                OverallPotential = CalculateOverallPotential(player, age),
                DevelopmentRate = CalculateDevelopmentRate(age, stage)
            };

            // Calculate attribute-specific potentials based on position and current ratings
            var attributes = player.Attributes;
            potential.AttributePotentials["Kicking"] = CalculateAttributePotential(attributes.Kicking, GetPositionBonus(player.Position, "Kicking"));
            potential.AttributePotentials["Marking"] = CalculateAttributePotential(attributes.Marking, GetPositionBonus(player.Position, "Marking"));
            potential.AttributePotentials["Handballing"] = CalculateAttributePotential(attributes.Handballing, GetPositionBonus(player.Position, "Handballing"));
            potential.AttributePotentials["Contested"] = CalculateAttributePotential(attributes.Contested, GetPositionBonus(player.Position, "Contested"));
            potential.AttributePotentials["Endurance"] = CalculateAttributePotential(attributes.Endurance, GetPositionBonus(player.Position, "Endurance"));

            // Set preferred training based on position
            potential.PreferredTraining = GetPreferredTrainingForPosition(player.Position);
            
            // Injury proneness based on age, position, and random factors
            potential.InjuryProneness = CalculateInjuryProneness(age, player.Position);

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
                var stage = GetDevelopmentStage(CalculateAge(player.DateOfBirth));
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
            var attrs = player.Attributes;
            float currentAverage = (attrs.Kicking + attrs.Marking + attrs.Handballing + attrs.Contested + attrs.Endurance) / 5f;
            
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
        /// Get position-specific bonus for different attributes
        /// </summary>
        private float GetPositionBonus(Position position, string attribute)
        {
            var bonuses = new Dictionary<(Position, string), float>
            {
                // Forwards
                {(Position.FullForward, "Kicking"), 0.8f},
                {(Position.FullForward, "Marking"), 0.9f},
                {(Position.ForwardPocket, "Kicking"), 0.7f},
                {(Position.HalfForward, "Kicking"), 0.6f},
                {(Position.HalfForward, "Contested"), 0.5f},
                
                // Midfielders  
                {(Position.Centre, "Endurance"), 0.9f},
                {(Position.Centre, "Handballing"), 0.8f},
                {(Position.Rover, "Contested"), 0.7f},
                {(Position.RuckRover, "Contested"), 0.8f},
                {(Position.Wing, "Endurance"), 0.8f},
                
                // Defenders
                {(Position.FullBack, "Marking"), 0.8f},
                {(Position.HalfBack, "Kicking"), 0.7f},
                {(Position.BackPocket, "Contested"), 0.6f},
                
                // Ruck
                {(Position.Ruckman, "Marking"), 0.9f},
                {(Position.Ruckman, "Contested"), 0.8f}
            };
            
            return bonuses.ContainsKey((position, attribute)) ? bonuses[(position, attribute)] : 0.3f;
        }

        /// <summary>
        /// Get preferred training focuses for each position
        /// </summary>
        private List<TrainingFocus> GetPreferredTrainingForPosition(Position position)
        {
            var preferences = new Dictionary<Position, List<TrainingFocus>>
            {
                {Position.FullForward, new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.Strength}},
                {Position.HalfForward, new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Speed, TrainingFocus.Contested}},
                {Position.Centre, new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Handballing, TrainingFocus.DecisionMaking}},
                {Position.Wing, new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Speed, TrainingFocus.Kicking}},
                {Position.HalfBack, new List<TrainingFocus> {TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.DecisionMaking}},
                {Position.FullBack, new List<TrainingFocus> {TrainingFocus.Marking, TrainingFocus.Strength, TrainingFocus.Positioning}},
                {Position.Ruckman, new List<TrainingFocus> {TrainingFocus.Strength, TrainingFocus.Contested, TrainingFocus.Marking}}
            };

            return preferences.ContainsKey(position) ? preferences[position] : new List<TrainingFocus> {TrainingFocus.Endurance, TrainingFocus.Kicking};
        }

        private float CalculateInjuryRisk(TrainingProgram program, TrainingSession session, Player player, DevelopmentPotential potential)
        {
            float baseRisk = 0.01f * (int)session.Intensity; // 1-4% base risk
            baseRisk *= program.InjuryRiskModifier;
            baseRisk *= potential.InjuryProneness;
            
            // Age factor - older players more injury prone
            int age = CalculateAge(player.DateOfBirth);
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
            float base = effectiveness > 1.0f ? 2f : effectiveness > 0.8f ? 1f : 0f;
            float intensityPenalty = (int)intensity > 3 ? -1f : 0f; // Elite intensity can reduce morale
            return base + intensityPenalty;
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

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }

        private float CalculateInjuryProneness(int age, Position position)
        {
            float base = 1.0f;
            
            // Age factor
            if (age > 30) base += 0.3f;
            else if (age < 20) base += 0.1f; // Young players slightly more prone due to inexperience
            
            // Position factor - more physical positions have higher injury risk
            var physicalPositions = new[] { Position.Ruckman, Position.FullForward, Position.FullBack, Position.Centre };
            if (physicalPositions.Contains(position))
                base += 0.2f;
                
            // Random individual variation
            base += ((float)_random.NextDouble() * 0.4f) - 0.2f; // ±0.2 variation
            
            return Math.Max(0.5f, Math.Min(2.0f, base)); // Clamp between 0.5x and 2.0x
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