using System;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Injuries.Domain;

namespace AFLCoachSim.Core.Domain.Entities
{
    public sealed class Player
    {
        public PlayerId Id { get; set; }
        public string Name { get; set; } = "Player";
        public int Age { get; set; }
        public TeamId TeamId { get; set; } // Team the player belongs to
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-25);
        public Role PrimaryRole { get; set; }
        public Role Role => PrimaryRole; // Backwards compatibility alias
        public Role Position { get; set; } = Role.MID; // Default to midfielder
        public Attributes Attr { get; set; } = new Attributes();

        // meta
        public int Endurance { get; set; } = 50;   // stamina pool baseline
        public int Durability { get; set; } = 50;  // injury resistance
        public int Discipline { get; set; } = 50;  // frees against / reports

        // season/match state
        public int Condition { get; set; } = 100;  // 0..100 fatigue/health
        public int Form { get; set; } = 0;         // -20..+20 short-term drift
        
        // Stats property wrapper for compatibility
        public Attributes Stats => Attr;
        
        // Additional properties for compatibility
        public Guid ID => Id; // Alias for Id property
        public int Confidence { get; set; } = 50; // Player confidence level
        public int Focus { get; set; } = 50; // Player focus/concentration
        public int DecisionMaking => Attr.DecisionMaking; // Map to attribute
        public int FormRating { get; set; } = 50; // Current form rating
        public int FatigueLevel { get; set; } = 0; // Current fatigue level
        public int Strength => Attr.Strength; // Map to attribute
        public int JumpReach => Attr.Marking; // Map to marking as closest approximation
        public int GroundBall => Attr.GroundBall; // Map to attribute
        
        // Derived/calculated properties for compatibility
        public int OverallRating => Attr.CalculateOverallRating();
        public int Speed { get => Attr.Speed; set => Attr.Speed = value; } // Make settable
        public int Accuracy { get => Attr.Kicking; set => Attr.Kicking = value; } // Make settable
        public int Agility { get => Attr.Agility; set => Attr.Agility = value; } // Add missing Agility property
        public int ContestedBall => (Attr.Tackling + Attr.Strength + Attr.Clearance) / 3;
        public int Aggression { get; set; } = 50; // Make settable instead of readonly calculation
        public string PlayingStyle => "Balanced"; // Default style
        public int RiskTaking => 50; // Default moderate risk taking
        
        // Match simulation properties
        public float CurrentMatchRating { get; set; } = 0f; // Dynamic rating during match
        public float MatchRating => CurrentMatchRating; // Alias for CurrentMatchRating
        public System.Collections.Generic.Dictionary<string, float> MatchAttributeModifiers { get; set; } = new System.Collections.Generic.Dictionary<string, float>();
        
        // Match status properties
        public bool IsStartingPlayer { get; set; } = true; // Whether player starts the match
        public bool IsOnField { get; set; } = true; // Whether player is currently on field
        public System.DateTime? SubstitutionTime { get; set; } = null; // When player was substituted off
        public System.DateTime? FieldEntryTime { get; set; } = null; // When player entered the field
        
        // Additional properties for coaching decision system
        public string CurrentMatchPosition { get; set; } = "MID"; // Current position in match
        public Role CurrentRole { get; set; } = Role.MID; // Current tactical role
        public InjuryStatus InjuryStatus { get; set; } = InjuryStatus.Healthy; // Current injury status
        public float TimeOnGround { get; set; } = 0f; // Minutes played in current match
        
        // Additional attribute mappings for match simulation
        public int Leading => Attr.Speed + Attr.Positioning; // Combination for leading ability
        public int OneOnOne => (Attr.Tackling + Attr.Positioning + Attr.DecisionMaking) / 3; // One-on-one defensive ability
        public int Marking => Attr.Marking; // Direct mapping to marking attribute
        public int Spoiling => Attr.Spoiling; // Direct mapping to spoiling attribute
        public int Evasion => (Attr.Agility + Attr.Speed + Attr.Acceleration) / 3; // Evasion ability
        public int Tackling => Attr.Tackling; // Direct mapping to tackling attribute
        
        // Parameterless constructor for object initialization syntax
        public Player()
        {
            Attr = new Attributes();
            MatchAttributeModifiers = new System.Collections.Generic.Dictionary<string, float>();
        }
        
        // Additional constructors for compatibility
        public Player(Guid id, string name, Role role)
        {
            Id = new PlayerId(id.GetHashCode()); // Convert Guid to PlayerId
            Name = name;
            PrimaryRole = role;
            Position = role; // Set both role properties
            Attr = new Attributes();
            MatchAttributeModifiers = new System.Collections.Generic.Dictionary<string, float>();
        }
    }
}
