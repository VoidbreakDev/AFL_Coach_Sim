// File: Assets/Scripts/Models/PlayerTeamModel.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AFLCoachSim.Models
{
    /// <summary>
    /// Roles a player can occupy on the field.
    /// </summary>
    public enum PlayerRole
    {
        FullBack,
        BackPocket,          // Added
        HalfBack,           // Added (was HalfBackFlank)
        FullBackFlank,      // Keep original
        HalfBackFlank,      // Keep original  
        CentreHalfBack,
        Wing,
        Centre,
        Ruckman,            // Added (was Ruck)
        Ruck,               // Keep original
        RuckRover,          // Added
        Rover,              // Added
        CentreHalfForward,
        HalfForward,        // Added (was HalfForwardFlank)
        HalfForwardFlank,   // Keep original
        ForwardPocket,      // Added
        FullForwardFlank,   // Keep original
        FullForward,
        Utility             // Added
    }

    /// <summary>
    /// Represents an individual player's skills.
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public int Kicking;
        public int Handballing;
        public int Tackling;
        public int Speed;
        public int Stamina;
        public int Knowledge;
        public int Playmaking;

        /// <summary>
        /// Returns the average of all stat values.
        /// </summary>
        public float GetAverage()
        {
            return (Kicking + Handballing + Tackling + Speed + Stamina + Knowledge + Playmaking) / 7f;
        }
    }

    /// <summary>
    /// Core data model for a football player.
    /// </summary>
    [Serializable]
    public class Player
    {
            public string Id { get; private set; }
            public string Name;
            public int Age;
            public string State;
            public string History;
            public PlayerRole Role;
            public float PotentialCeiling;
            public float Morale;
            public float Stamina;
        public PlayerStats Stats;
        public ContractDetails Contract;
        
        // Development system (commented out until systems are available)
        // public AFLManager.Systems.Development.PlayerDevelopment Development;
        
        // Form and condition tracking (commented out until systems are available)
        // public AFLManager.Systems.Development.PlayerFormCondition FormCondition;

        [NonSerialized] public Team CurrentTeam;
        [NonSerialized] public Sprite Portrait;



            /// <summary>
            /// Constructor: assigns a new unique ID.
            /// </summary>
        public Player()
        {
            Id = Guid.NewGuid().ToString();
            Stats = new PlayerStats();
            // Development = new AFLManager.Systems.Development.PlayerDevelopment();
            // FormCondition = new AFLManager.Systems.Development.PlayerFormCondition();
            
            // Set position-specific development weights (commented out until systems are available)
            // if (Development != null)
            // {
            //     Development.Weights = AFLManager.Systems.Development.PositionDevelopmentWeights.CreateForPosition(Role);
            // }
        }

        /// <summary>
        /// Details of a player's contract.
        /// </summary>
        [Serializable]
        public class ContractDetails
        {
            public float Salary;
            public int YearsRemaining;
        }

        /// <summary>
        /// Levels of competition.
        /// </summary>
        public enum LeagueLevel
        {
            Local,
            Regional,
            State,
            Interstate,
            AFL
        }

        /// <summary>
        /// Represents coaching skill modifiers.
        /// </summary>
        [Serializable]
        public class CoachSkills
        {
            public float WellBalanced;
            public float Attacking;
            public float DefensiveWall;
            public float StaminaSaving;
            public float Playmaker;
            public float TeamCulture;
        }

        /// <summary>
        /// Records team performance.
        /// </summary>
        [Serializable]
        public struct WinLossRecord
        {
            public int Wins;
            public int Losses;
            public int Draws;
        }

        /// <summary>
        /// Core data model for a team.
        /// </summary>
        [Serializable]
        public class Team
        {
            [SerializeField] private string _id;
            public string Id 
            { 
                get { return _id; } 
                private set { _id = value; } 
            }
            public string Name;
            public LeagueLevel Level;
            public List<Player> Roster;
            public float Budget;
            public float SalaryCap;
            public CoachSkills CoachModifiers;
            public WinLossRecord Record;
            public int Premierships;

            /// <summary>
            /// Constructor: assigns a new unique ID.
            /// </summary>
            public Team()
            {
                _id = Guid.NewGuid().ToString();
                Roster = new List<Player>();
                CoachModifiers = new CoachSkills();
            }
        }
    }
}
