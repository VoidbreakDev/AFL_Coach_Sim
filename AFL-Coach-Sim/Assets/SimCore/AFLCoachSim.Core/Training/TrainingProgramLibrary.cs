using System.Collections.Generic;
using AFLCoachSim.Core.Models;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Library of pre-built training programs for different AFL specializations and development needs
    /// </summary>
    public static class TrainingProgramLibrary
    {
        /// <summary>
        /// Get all available training programs
        /// </summary>
        public static List<TrainingProgram> GetAllPrograms()
        {
            var programs = new List<TrainingProgram>();
            
            // Add all program types
            programs.AddRange(GetFitnessPrograms());
            programs.AddRange(GetSkillsPrograms());
            programs.AddRange(GetTacticalPrograms());
            programs.AddRange(GetRecoveryPrograms());
            programs.AddRange(GetLeadershipPrograms());
            programs.AddRange(GetSpecializedPrograms());
            
            return programs;
        }

        #region Fitness Programs

        public static List<TrainingProgram> GetFitnessPrograms()
        {
            return new List<TrainingProgram>
            {
                CreateEnduranceBaseProgram(),
                CreateStrengthProgram(),
                CreateSpeedAgilityProgram(),
                CreatePreSeasonConditioningProgram(),
                CreateMidSeasonMaintenanceProgram()
            };
        }

        private static TrainingProgram CreateEnduranceBaseProgram()
        {
            return new TrainingProgram
            {
                Name = "Endurance Base Building",
                Description = "Develops cardiovascular fitness and running endurance critical for AFL performance",
                Type = TrainingType.Fitness,
                PrimaryFocus = TrainingFocus.Endurance,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Speed },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 2.5f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 28,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.2f,
                InjuryRiskModifier = 0.8f,
                FatigueRateModifier = 1.1f,
                StageMultipliers = new Dictionary<DevelopmentStage, float>
                {
                    {DevelopmentStage.Rookie, 1.3f},
                    {DevelopmentStage.Developing, 1.2f},
                    {DevelopmentStage.Prime, 1.0f},
                    {DevelopmentStage.Veteran, 0.8f}
                },
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.Centre, 1.4f},
                    {Position.Wing, 1.3f},
                    {Position.HalfForward, 1.1f},
                    {Position.HalfBack, 1.1f}
                }
            };
        }

        private static TrainingProgram CreateStrengthProgram()
        {
            return new TrainingProgram
            {
                Name = "Strength & Power Development",
                Description = "Builds functional strength for contests, marking, and physical duels",
                Type = TrainingType.Fitness,
                PrimaryFocus = TrainingFocus.Strength,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Contested", 2.0f},
                    {"Marking", 1.5f}
                },
                MinimumAge = 18,
                MaximumAge = 32,
                DurationDays = 21,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.0f,
                InjuryRiskModifier = 1.3f,
                FatigueRateModifier = 1.4f,
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.Ruckman, 1.5f},
                    {Position.FullForward, 1.3f},
                    {Position.FullBack, 1.3f},
                    {Position.Centre, 1.2f}
                }
            };
        }

        private static TrainingProgram CreateSpeedAgilityProgram()
        {
            return new TrainingProgram
            {
                Name = "Speed & Agility Training",
                Description = "Develops acceleration, top speed, and change of direction abilities",
                Type = TrainingType.Fitness,
                PrimaryFocus = TrainingFocus.Speed,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Agility },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 1.0f}
                },
                MinimumAge = 16,
                MaximumAge = 30,
                DurationDays = 14,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.1f,
                InjuryRiskModifier = 1.1f,
                FatigueRateModifier = 1.2f,
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.Wing, 1.4f},
                    {Position.HalfForward, 1.3f},
                    {Position.ForwardPocket, 1.2f},
                    {Position.BackPocket, 1.2f}
                }
            };
        }

        private static TrainingProgram CreatePreSeasonConditioningProgram()
        {
            return new TrainingProgram
            {
                Name = "Pre-Season Conditioning",
                Description = "Intensive pre-season program to build match fitness and prevent injuries",
                Type = TrainingType.Fitness,
                PrimaryFocus = TrainingFocus.Endurance,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Strength, TrainingFocus.InjuryPrevention },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 3.0f},
                    {"Contested", 1.5f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                TargetStage = DevelopmentStage.Rookie, // Suitable for all stages
                DurationDays = 42,
                BaseIntensity = TrainingIntensity.Elite,
                BaseEffectiveness = 1.3f,
                InjuryRiskModifier = 0.7f, // Injury prevention focus
                FatigueRateModifier = 1.5f
            };
        }

        private static TrainingProgram CreateMidSeasonMaintenanceProgram()
        {
            return new TrainingProgram
            {
                Name = "Mid-Season Maintenance",
                Description = "Maintains fitness levels during the competitive season",
                Type = TrainingType.Fitness,
                PrimaryFocus = TrainingFocus.LoadManagement,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Recovery },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 0.5f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 7, // Weekly program
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 0.8f,
                InjuryRiskModifier = 0.5f,
                FatigueRateModifier = 0.7f
            };
        }

        #endregion

        #region Skills Programs

        public static List<TrainingProgram> GetSkillsPrograms()
        {
            return new List<TrainingProgram>
            {
                CreateKickingAccuracyProgram(),
                CreateMarkingProgram(),
                CreateHandballingProgram(),
                CreateContestedBallProgram(),
                CreateGoalKickingProgram()
            };
        }

        private static TrainingProgram CreateKickingAccuracyProgram()
        {
            return new TrainingProgram
            {
                Name = "Kicking Accuracy & Technique",
                Description = "Improves kicking accuracy, distance, and technique under pressure",
                Type = TrainingType.Skills,
                PrimaryFocus = TrainingFocus.Kicking,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 3.0f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 21,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.4f,
                InjuryRiskModifier = 0.6f,
                FatigueRateModifier = 0.8f,
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.HalfBack, 1.5f},
                    {Position.Centre, 1.3f},
                    {Position.Wing, 1.3f},
                    {Position.HalfForward, 1.2f}
                }
            };
        }

        private static TrainingProgram CreateMarkingProgram()
        {
            return new TrainingProgram
            {
                Name = "Marking & Aerial Skills",
                Description = "Develops marking technique, timing, and aerial contests",
                Type = TrainingType.Skills,
                PrimaryFocus = TrainingFocus.Marking,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Marking", 2.8f}
                },
                MinimumAge = 16,
                MaximumAge = 33,
                DurationDays = 18,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.3f,
                InjuryRiskModifier = 0.9f,
                FatigueRateModifier = 1.0f,
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.FullForward, 1.4f},
                    {Position.FullBack, 1.4f},
                    {Position.Ruckman, 1.3f},
                    {Position.HalfForward, 1.2f},
                    {Position.HalfBack, 1.2f}
                }
            };
        }

        private static TrainingProgram CreateHandballingProgram()
        {
            return new TrainingProgram
            {
                Name = "Handballing & Ball Movement",
                Description = "Improves handballing accuracy and decision-making in traffic",
                Type = TrainingType.Skills,
                PrimaryFocus = TrainingFocus.Handballing,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Handballing", 2.5f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 14,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.2f,
                InjuryRiskModifier = 0.5f,
                FatigueRateModifier = 0.7f,
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.Centre, 1.4f},
                    {Position.Rover, 1.4f},
                    {Position.RuckRover, 1.3f}
                }
            };
        }

        private static TrainingProgram CreateContestedBallProgram()
        {
            return new TrainingProgram
            {
                Name = "Contested Ball & Pressure",
                Description = "Develops skills for winning contested possession and applying defensive pressure",
                Type = TrainingType.Skills,
                PrimaryFocus = TrainingFocus.Contested,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Pressure },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Contested", 2.8f},
                    {"Handballing", 1.0f}
                },
                MinimumAge = 17,
                MaximumAge = 32,
                DurationDays = 21,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.1f,
                InjuryRiskModifier = 1.2f,
                FatigueRateModifier = 1.3f,
                PositionMultipliers = new Dictionary<Position, float>
                {
                    {Position.Centre, 1.3f},
                    {Position.Rover, 1.3f},
                    {Position.RuckRover, 1.2f},
                    {Position.Ruckman, 1.1f}
                }
            };
        }

        private static TrainingProgram CreateGoalKickingProgram()
        {
            return new TrainingProgram
            {
                Name = "Goal Kicking Specialist",
                Description = "Specialized program for improving goal kicking accuracy and technique",
                Type = TrainingType.Specialized,
                PrimaryFocus = TrainingFocus.Kicking,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 3.5f}
                },
                MinimumAge = 17,
                MaximumAge = 35,
                SuitablePositions = new List<Position> 
                { 
                    Position.FullForward, Position.HalfForward, Position.ForwardPocket 
                },
                DurationDays = 28,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.6f,
                InjuryRiskModifier = 0.4f,
                FatigueRateModifier = 0.6f
            };
        }

        #endregion

        #region Tactical Programs

        public static List<TrainingProgram> GetTacticalPrograms()
        {
            return new List<TrainingProgram>
            {
                CreateGamePlanProgram(),
                CreatePositionalPlayProgram(),
                CreateSetPieceProgram(),
                CreateDefensivePressureProgram()
            };
        }

        private static TrainingProgram CreateGamePlanProgram()
        {
            return new TrainingProgram
            {
                Name = "Game Plan Implementation",
                Description = "Teaches team structures, ball movement, and strategic positioning",
                Type = TrainingType.Tactical,
                PrimaryFocus = TrainingFocus.GamePlan,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Positioning, TrainingFocus.DecisionMaking },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Handballing", 1.0f},
                    {"Kicking", 1.0f}
                },
                MinimumAge = 17,
                MaximumAge = 35,
                DurationDays = 35,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.0f,
                InjuryRiskModifier = 0.7f,
                FatigueRateModifier = 0.9f
            };
        }

        private static TrainingProgram CreatePositionalPlayProgram()
        {
            return new TrainingProgram
            {
                Name = "Positional Play & Awareness",
                Description = "Develops understanding of positional responsibilities and field awareness",
                Type = TrainingType.Tactical,
                PrimaryFocus = TrainingFocus.Positioning,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 0.8f},
                    {"Handballing", 0.8f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 21,
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 1.1f,
                InjuryRiskModifier = 0.5f,
                FatigueRateModifier = 0.6f
            };
        }

        private static TrainingProgram CreateSetPieceProgram()
        {
            return new TrainingProgram
            {
                Name = "Set Piece Specialization",
                Description = "Focuses on kick-ins, throw-ins, and centre bounce strategies",
                Type = TrainingType.Tactical,
                PrimaryFocus = TrainingFocus.SetPieces,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 1.5f},
                    {"Marking", 1.0f}
                },
                MinimumAge = 18,
                MaximumAge = 33,
                DurationDays = 14,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.2f,
                InjuryRiskModifier = 0.6f,
                FatigueRateModifier = 0.7f
            };
        }

        private static TrainingProgram CreateDefensivePressureProgram()
        {
            return new TrainingProgram
            {
                Name = "Defensive Pressure System",
                Description = "Teaches coordinated defensive pressure and turnovers",
                Type = TrainingType.Tactical,
                PrimaryFocus = TrainingFocus.Pressure,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Contested },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Contested", 1.8f},
                    {"Endurance", 1.2f}
                },
                MinimumAge = 17,
                MaximumAge = 32,
                DurationDays = 28,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.1f,
                InjuryRiskModifier = 1.0f,
                FatigueRateModifier = 1.2f
            };
        }

        #endregion

        #region Recovery Programs

        public static List<TrainingProgram> GetRecoveryPrograms()
        {
            return new List<TrainingProgram>
            {
                CreateActiveRecoveryProgram(),
                CreateInjuryPreventionProgram(),
                CreateVeteranLoadManagementProgram()
            };
        }

        private static TrainingProgram CreateActiveRecoveryProgram()
        {
            return new TrainingProgram
            {
                Name = "Active Recovery Protocol",
                Description = "Light activities to promote recovery while maintaining movement patterns",
                Type = TrainingType.Recovery,
                PrimaryFocus = TrainingFocus.Recovery,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Flexibility },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 0.3f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 7,
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 0.7f,
                InjuryRiskModifier = 0.3f,
                FatigueRateModifier = 0.4f
            };
        }

        private static TrainingProgram CreateInjuryPreventionProgram()
        {
            return new TrainingProgram
            {
                Name = "Injury Prevention & Conditioning",
                Description = "Targeted exercises to strengthen vulnerable areas and prevent injuries",
                Type = TrainingType.Recovery,
                PrimaryFocus = TrainingFocus.InjuryPrevention,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Flexibility, TrainingFocus.Strength },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 0.8f},
                    {"Contested", 0.5f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 21,
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 0.9f,
                InjuryRiskModifier = 0.2f,
                FatigueRateModifier = 0.5f
            };
        }

        private static TrainingProgram CreateVeteranLoadManagementProgram()
        {
            return new TrainingProgram
            {
                Name = "Veteran Load Management",
                Description = "Specialized program for managing training load of veteran players",
                Type = TrainingType.Recovery,
                PrimaryFocus = TrainingFocus.LoadManagement,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 0.5f}
                },
                MinimumAge = 30,
                MaximumAge = 40,
                TargetStage = DevelopmentStage.Veteran,
                DurationDays = 14,
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 1.0f,
                InjuryRiskModifier = 0.4f,
                FatigueRateModifier = 0.5f,
                StageMultipliers = new Dictionary<DevelopmentStage, float>
                {
                    {DevelopmentStage.Veteran, 1.3f},
                    {DevelopmentStage.Declining, 1.2f}
                }
            };
        }

        #endregion

        #region Leadership Programs

        public static List<TrainingProgram> GetLeadershipPrograms()
        {
            return new List<TrainingProgram>
            {
                CreateLeadershipDevelopmentProgram(),
                CreateMentorshipProgram(),
                CreateCaptaincyProgram()
            };
        }

        private static TrainingProgram CreateLeadershipDevelopmentProgram()
        {
            return new TrainingProgram
            {
                Name = "Leadership Development",
                Description = "Develops leadership qualities and communication skills",
                Type = TrainingType.Leadership,
                PrimaryFocus = TrainingFocus.Leadership,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Communication, TrainingFocus.DecisionMaking },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Handballing", 0.5f}
                },
                MinimumAge = 20,
                MaximumAge = 35,
                DurationDays = 35,
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 1.0f,
                InjuryRiskModifier = 0.3f,
                FatigueRateModifier = 0.5f
            };
        }

        private static TrainingProgram CreateMentorshipProgram()
        {
            return new TrainingProgram
            {
                Name = "Mentorship & Team Culture",
                Description = "Pairs experienced players with rookies for skill and culture development",
                Type = TrainingType.Leadership,
                PrimaryFocus = TrainingFocus.Communication,
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 0.8f},
                    {"Handballing", 0.8f}
                },
                MinimumAge = 16,
                MaximumAge = 35,
                DurationDays = 42,
                BaseIntensity = TrainingIntensity.Light,
                BaseEffectiveness = 1.2f,
                InjuryRiskModifier = 0.4f,
                FatigueRateModifier = 0.6f,
                StageMultipliers = new Dictionary<DevelopmentStage, float>
                {
                    {DevelopmentStage.Rookie, 1.4f}, // Rookies benefit most
                    {DevelopmentStage.Veteran, 1.2f}  // Veterans benefit from teaching
                }
            };
        }

        private static TrainingProgram CreateCaptaincyProgram()
        {
            return new TrainingProgram
            {
                Name = "Captaincy Excellence",
                Description = "Elite leadership program for current and future captains",
                Type = TrainingType.Leadership,
                PrimaryFocus = TrainingFocus.Leadership,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.DecisionMaking, TrainingFocus.Communication },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 1.0f},
                    {"Handballing", 1.0f}
                },
                MinimumAge = 22,
                MaximumAge = 35,
                DurationDays = 49,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.3f,
                InjuryRiskModifier = 0.5f,
                FatigueRateModifier = 0.7f
            };
        }

        #endregion

        #region Specialized Programs

        public static List<TrainingProgram> GetSpecializedPrograms()
        {
            return new List<TrainingProgram>
            {
                CreateRuckmanSpecialistProgram(),
                CreateSmallForwardProgram(),
                CreateKeyDefenderProgram(),
                CreateMidfielderRotationProgram(),
                CreateUtilityPlayerProgram()
            };
        }

        private static TrainingProgram CreateRuckmanSpecialistProgram()
        {
            return new TrainingProgram
            {
                Name = "Ruckman Specialist Training",
                Description = "Comprehensive ruck training covering contests, mobility, and forward craft",
                Type = TrainingType.Specialized,
                PrimaryFocus = TrainingFocus.Contested,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Marking, TrainingFocus.Strength },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Contested", 3.2f},
                    {"Marking", 2.5f}
                },
                MinimumAge = 18,
                MaximumAge = 34,
                SuitablePositions = new List<Position> { Position.Ruckman },
                DurationDays = 35,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.5f,
                InjuryRiskModifier = 1.3f,
                FatigueRateModifier = 1.4f
            };
        }

        private static TrainingProgram CreateSmallForwardProgram()
        {
            return new TrainingProgram
            {
                Name = "Small Forward Craft",
                Description = "Develops crumbing, pressure, and goal-sneak abilities",
                Type = TrainingType.Specialized,
                PrimaryFocus = TrainingFocus.Speed,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Kicking, TrainingFocus.Pressure },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 2.2f},
                    {"Contested", 1.8f}
                },
                MinimumAge = 17,
                MaximumAge = 32,
                SuitablePositions = new List<Position> { Position.ForwardPocket, Position.HalfForward },
                DurationDays = 28,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.3f,
                InjuryRiskModifier = 1.0f,
                FatigueRateModifier = 1.1f
            };
        }

        private static TrainingProgram CreateKeyDefenderProgram()
        {
            return new TrainingProgram
            {
                Name = "Key Defender Excellence",
                Description = "Develops intercepting, spoiling, and rebounding skills",
                Type = TrainingType.Specialized,
                PrimaryFocus = TrainingFocus.Marking,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Kicking, TrainingFocus.Positioning },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Marking", 2.8f},
                    {"Kicking", 2.0f}
                },
                MinimumAge = 18,
                MaximumAge = 34,
                SuitablePositions = new List<Position> { Position.FullBack, Position.HalfBack },
                DurationDays = 32,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.3f,
                InjuryRiskModifier = 0.9f,
                FatigueRateModifier = 1.0f
            };
        }

        private static TrainingProgram CreateMidfielderRotationProgram()
        {
            return new TrainingProgram
            {
                Name = "Midfielder Rotation System",
                Description = "Trains versatility across multiple midfield roles",
                Type = TrainingType.Specialized,
                PrimaryFocus = TrainingFocus.Endurance,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Handballing, TrainingFocus.DecisionMaking },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Endurance", 2.5f},
                    {"Handballing", 2.0f},
                    {"Contested", 1.5f}
                },
                MinimumAge = 18,
                MaximumAge = 32,
                SuitablePositions = new List<Position> { Position.Centre, Position.Wing, Position.Rover, Position.RuckRover },
                DurationDays = 35,
                BaseIntensity = TrainingIntensity.High,
                BaseEffectiveness = 1.2f,
                InjuryRiskModifier = 1.1f,
                FatigueRateModifier = 1.3f
            };
        }

        private static TrainingProgram CreateUtilityPlayerProgram()
        {
            return new TrainingProgram
            {
                Name = "Utility Player Development",
                Description = "Develops versatility to play multiple positions effectively",
                Type = TrainingType.Specialized,
                PrimaryFocus = TrainingFocus.Positioning,
                SecondaryFoci = new List<TrainingFocus> { TrainingFocus.Kicking, TrainingFocus.Marking, TrainingFocus.Endurance },
                AttributeTargets = new Dictionary<string, float>
                {
                    {"Kicking", 1.8f},
                    {"Marking", 1.5f},
                    {"Handballing", 1.5f},
                    {"Endurance", 1.2f}
                },
                MinimumAge = 19,
                MaximumAge = 30,
                DurationDays = 42,
                BaseIntensity = TrainingIntensity.Moderate,
                BaseEffectiveness = 1.1f,
                InjuryRiskModifier = 0.8f,
                FatigueRateModifier = 1.0f
            };
        }

        #endregion
    }
}