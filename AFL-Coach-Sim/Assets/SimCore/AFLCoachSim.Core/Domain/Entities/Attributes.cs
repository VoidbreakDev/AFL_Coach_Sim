using System;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Domain.Entities
{
    public sealed class Attributes
    {
        // Physical
        public int Speed, Acceleration, Strength, Agility, Jump;
        // Skills
        public int Kicking, Marking, Handball, Tackling, Clearance, RuckWork, Spoiling;
        // Mental
        public int DecisionMaking, Composure, WorkRate, Positioning, Leadership;

        /// <summary>
        /// Calculates an overall skill rating based on weighted average of attributes
        /// </summary>
        public int CalculateOverallRating()
        {
            // Physical attributes (30% weight)
            float physicalScore = (Speed * 0.8f + Acceleration * 0.7f + Strength * 0.6f + 
                                  Agility * 0.7f + Jump * 0.5f) / 3.3f;
            
            // Skill attributes (50% weight)
            float skillScore = (Kicking * 1.0f + Marking * 0.9f + Handball * 1.0f + 
                              Tackling * 0.8f + Clearance * 0.7f + RuckWork * 0.4f + 
                              Spoiling * 0.6f) / 5.4f;
            
            // Mental attributes (20% weight)
            float mentalScore = (DecisionMaking * 1.0f + Composure * 0.8f + WorkRate * 0.9f + 
                               Positioning * 0.7f + Leadership * 0.6f) / 4.0f;
            
            float total = (physicalScore * 0.30f) + (skillScore * 0.50f) + (mentalScore * 0.20f);
            return Math.Max(0, Math.Min(100, (int)Math.Round(total)));
        }

        /// <summary>
        /// Updates a specific attribute by name
        /// </summary>
        public void UpdateAttribute(string attributeName, int value)
        {
            value = Math.Max(0, Math.Min(100, value)); // Clamp to 0-100
            
            switch (attributeName.ToLowerInvariant())
            {
                case "speed": Speed = value; break;
                case "acceleration": Acceleration = value; break;
                case "strength": Strength = value; break;
                case "agility": Agility = value; break;
                case "jump": Jump = value; break;
                case "kicking": Kicking = value; break;
                case "marking": Marking = value; break;
                case "handball": Handball = value; break;
                case "tackling": Tackling = value; break;
                case "clearance": Clearance = value; break;
                case "ruckwork": RuckWork = value; break;
                case "spoiling": Spoiling = value; break;
                case "decisionmaking": DecisionMaking = value; break;
                case "composure": Composure = value; break;
                case "workrate": WorkRate = value; break;
                case "positioning": Positioning = value; break;
                case "leadership": Leadership = value; break;
            }
        }

        /// <summary>
        /// Gets a specific attribute value by name
        /// </summary>
        public int GetAttribute(string attributeName)
        {
            switch (attributeName.ToLowerInvariant())
            {
                case "speed": return Speed;
                case "acceleration": return Acceleration;
                case "strength": return Strength;
                case "agility": return Agility;
                case "jump": return Jump;
                case "kicking": return Kicking;
                case "marking": return Marking;
                case "handball": return Handball;
                case "tackling": return Tackling;
                case "clearance": return Clearance;
                case "ruckwork": return RuckWork;
                case "spoiling": return Spoiling;
                case "decisionmaking": return DecisionMaking;
                case "composure": return Composure;
                case "workrate": return WorkRate;
                case "positioning": return Positioning;
                case "leadership": return Leadership;
                default: return 0;
            }
        }

        /// <summary>
        /// Calculates position-specific rating based on role requirements
        /// </summary>
        public int CalculatePositionRating(Role role)
        {
            float rating = 0f;
            
            switch (role)
            {
                case Role.KPD: // Key Position Defender
                    rating = (Marking * 0.25f + Spoiling * 0.20f + Strength * 0.15f + 
                             Jump * 0.15f + Positioning * 0.15f + Leadership * 0.10f) / 1.0f;
                    break;
                    
                case Role.KPF: // Key Position Forward
                    rating = (Marking * 0.30f + Kicking * 0.20f + Strength * 0.15f + 
                             Jump * 0.15f + Positioning * 0.20f) / 1.0f;
                    break;
                    
                case Role.MID: // Midfielder
                    rating = (Kicking * 0.20f + Handball * 0.20f + DecisionMaking * 0.20f + 
                             WorkRate * 0.15f + Clearance * 0.15f + Speed * 0.10f) / 1.0f;
                    break;
                    
                case Role.WING: // Winger
                    rating = (Speed * 0.25f + Kicking * 0.20f + WorkRate * 0.20f + 
                             Agility * 0.15f + Acceleration * 0.20f) / 1.0f;
                    break;
                    
                case Role.RUC: // Ruckman
                    rating = (Jump * 0.30f + RuckWork * 0.25f + Strength * 0.20f + 
                             Marking * 0.15f + Positioning * 0.10f) / 1.0f;
                    break;
                    
                default: // General rating for other positions
                    rating = CalculateOverallRating();
                    break;
            }
            
            return Math.Max(0, Math.Min(100, (int)Math.Round(rating)));
        }
    }
}
