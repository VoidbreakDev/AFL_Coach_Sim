using System;
using System.Collections.Generic;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Factory for creating pre-defined weekly training schedule templates
    /// </summary>
    public static class TrainingScheduleTemplateFactory
    {
        /// <summary>
        /// Create a standard regular season training week
        /// </summary>
        public static WeeklyScheduleTemplate CreateStandardWeekTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Standard Training Week",
                TemplateType = ScheduleTemplateType.Standard,
                Description = "Balanced training schedule for regular season with skills, fitness, and tactical components",
                DayTemplates = new List<DailySessionTemplate>
                {
                    CreateStandardMonday(),
                    CreateStandardTuesday(),
                    CreateRestDay(DayOfWeek.Wednesday),
                    CreateStandardThursday(),
                    CreateStandardFriday(),
                    CreateRestDay(DayOfWeek.Saturday, "Pre-Match Rest"),
                    CreateRestDay(DayOfWeek.Sunday, "Match Day")
                }
            };
        }
        
        /// <summary>
        /// Create a match week template with reduced load before game
        /// </summary>
        public static WeeklyScheduleTemplate CreateMatchWeekTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Match Week Training",
                TemplateType = ScheduleTemplateType.MatchWeek,
                Description = "Training schedule for weeks containing matches, with taper before game",
                DayTemplates = new List<DailySessionTemplate>
                {
                    CreateMatchWeekMonday(),
                    CreateMatchWeekTuesday(), 
                    CreateRestDay(DayOfWeek.Wednesday, "Mid-Week Recovery"),
                    CreateMatchWeekThursday(),
                    CreateMatchPreparationFriday(),
                    CreateRestDay(DayOfWeek.Saturday, "Pre-Match Rest"),
                    CreateRestDay(DayOfWeek.Sunday, "Match Day")
                }
            };
        }
        
        /// <summary>
        /// Create a bye week template with increased training load
        /// </summary>
        public static WeeklyScheduleTemplate CreateByeWeekTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Bye Week Training",
                TemplateType = ScheduleTemplateType.ByeWeek,
                Description = "Intensive training schedule for bye weeks to maintain fitness and work on weaknesses",
                DayTemplates = new List<DailySessionTemplate>
                {
                    CreateByeWeekMonday(),
                    CreateByeWeekTuesday(),
                    CreateByeWeekWednesday(),
                    CreateByeWeekThursday(),
                    CreateByeWeekFriday(),
                    CreateActiveRecoverySaturday(),
                    CreateRestDay(DayOfWeek.Sunday, "Complete Rest")
                }
            };
        }
        
        /// <summary>
        /// Create a pre-season conditioning template
        /// </summary>
        public static WeeklyScheduleTemplate CreatePreSeasonTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Pre-Season Conditioning",
                TemplateType = ScheduleTemplateType.PreSeason,
                Description = "High-intensity fitness and conditioning focused training for pre-season",
                DayTemplates = new List<DailySessionTemplate>
                {
                    CreatePreSeasonMonday(),
                    CreatePreSeasonTuesday(),
                    CreatePreSeasonWednesday(),
                    CreatePreSeasonThursday(),
                    CreatePreSeasonFriday(),
                    CreateActiveRecoverySaturday(),
                    CreateRestDay(DayOfWeek.Sunday)
                }
            };
        }
        
        /// <summary>
        /// Create a finals preparation template
        /// </summary>
        public static WeeklyScheduleTemplate CreateFinalsTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Finals Preparation",
                TemplateType = ScheduleTemplateType.Finals,
                Description = "Precision training for finals with focus on game plan and pressure situations",
                DayTemplates = new List<DailySessionTemplate>
                {
                    CreateFinalsMonday(),
                    CreateFinalsTuesday(),
                    CreateRestDay(DayOfWeek.Wednesday, "Mental Preparation"),
                    CreateFinalsThursday(),
                    CreateFinalsFriday(),
                    CreateRestDay(DayOfWeek.Saturday, "Final Preparation"),
                    CreateRestDay(DayOfWeek.Sunday, "Finals Match")
                }
            };
        }
        
        /// <summary>
        /// Create a recovery week template
        /// </summary>
        public static WeeklyScheduleTemplate CreateRecoveryWeekTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Recovery Week",
                TemplateType = ScheduleTemplateType.Recovery,
                Description = "Low-intensity recovery focused training for player regeneration",
                DayTemplates = new List<DailySessionTemplate>
                {
                    CreateRecoveryMonday(),
                    CreateActiveRecoveryDay(DayOfWeek.Tuesday),
                    CreateRestDay(DayOfWeek.Wednesday),
                    CreateActiveRecoveryDay(DayOfWeek.Thursday),
                    CreateRestDay(DayOfWeek.Friday),
                    CreateRestDay(DayOfWeek.Saturday),
                    CreateRestDay(DayOfWeek.Sunday)
                }
            };
        }
        
        #region Standard Week Days
        
        private static DailySessionTemplate CreateStandardMonday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Monday,
                SessionName = "Skills Development",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(90), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 15f,
                        Notes = "Ball handling, kicking accuracy, marking under pressure"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 3f,
                        Notes = "Stretching and mobility work"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateStandardTuesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Tuesday,
                SessionName = "Fitness Training",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.Conditioning, 
                        Duration = TimeSpan.FromMinutes(75), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 20f,
                        Notes = "Running, strength training, explosiveness"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(15), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 2f,
                        Notes = "Cool-down and recovery"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateStandardThursday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Thursday,
                SessionName = "Tactical Training",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromMinutes(90), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 12f,
                        Notes = "Game plan implementation, set plays, positioning"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 6f,
                        Notes = "Light skills work to complement tactics"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateStandardFriday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Friday,
                SessionName = "Light Skills & Preparation",
                StartTime = new TimeSpan(10, 0, 0),
                Duration = TimeSpan.FromHours(1),
                SessionType = DailySessionType.Supplementary,
                SkipOnMatchDay = true,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(45), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 8f,
                        Notes = "Light skills work, confidence building"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Mental, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(15), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 2f,
                        Notes = "Mental preparation, visualization"
                    }
                }
            };
        }
        
        #endregion
        
        #region Match Week Days
        
        private static DailySessionTemplate CreateMatchWeekMonday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Monday,
                SessionName = "Post-Match Recovery",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1),
                SessionType = DailySessionType.Recovery,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(60), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 8f,
                        Notes = "Active recovery, stretching, light movement"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateMatchWeekTuesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Tuesday,
                SessionName = "Skills & Light Fitness",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(60), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 12f
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.Conditioning, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 10f
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateMatchWeekThursday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Thursday,
                SessionName = "Game Plan Review",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromMinutes(75), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 8f,
                        Notes = "Opposition analysis, set plays, light walk-through"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Mental, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(15), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 2f
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateMatchPreparationFriday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Friday,
                SessionName = "Match Preparation",
                StartTime = new TimeSpan(10, 0, 0),
                Duration = TimeSpan.FromMinutes(45),
                SessionType = DailySessionType.MatchPreparation,
                SkipOnMatchDay = false, // Keep this even on match day
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 5f,
                        Notes = "Light skills work, activation"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Mental, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(15), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 2f,
                        Notes = "Mental preparation, team meeting"
                    }
                }
            };
        }
        
        #endregion
        
        #region Bye Week Days
        
        private static DailySessionTemplate CreateByeWeekMonday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Monday,
                SessionName = "Intensive Skills",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(120), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 25f,
                        Notes = "Intensive skills work, individual coaching"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Specialized, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 8f,
                        Notes = "Position-specific training"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateByeWeekTuesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Tuesday,
                SessionName = "High-Intensity Fitness",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.Conditioning, 
                        Duration = TimeSpan.FromMinutes(90), 
                        Intensity = TrainingIntensity.VeryHigh, 
                        LoadMultiplier = 30f,
                        Notes = "High-intensity intervals, strength work"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 5f
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateByeWeekWednesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Wednesday,
                SessionName = "Tactical Deep Dive",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromMinutes(120), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 18f,
                        Notes = "Comprehensive tactical review, new plays"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateByeWeekThursday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Thursday,
                SessionName = "Combined Training",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(75), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 20f
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.Conditioning, 
                        Duration = TimeSpan.FromMinutes(75), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 22f
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateByeWeekFriday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Friday,
                SessionName = "Individual Development",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2),
                SessionType = DailySessionType.Supplementary,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Specialized, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(90), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 15f,
                        Notes = "Individual weakness work, specialized coaching"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Mental, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 5f,
                        Notes = "Goal setting, mental skills training"
                    }
                }
            };
        }
        
        #endregion
        
        #region Special Days
        
        private static DailySessionTemplate CreateRestDay(DayOfWeek day, string customName = null)
        {
            return new DailySessionTemplate
            {
                DayOfWeek = day,
                SessionName = customName ?? $"{day} Rest",
                IsRestDay = true,
                SessionType = DailySessionType.Recovery
            };
        }
        
        private static DailySessionTemplate CreateActiveRecoveryDay(DayOfWeek day)
        {
            return new DailySessionTemplate
            {
                DayOfWeek = day,
                SessionName = "Active Recovery",
                StartTime = new TimeSpan(10, 0, 0),
                Duration = TimeSpan.FromMinutes(45),
                SessionType = DailySessionType.Recovery,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(45), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 6f,
                        Notes = "Light movement, stretching, pool recovery"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateActiveRecoverySaturday()
        {
            return CreateActiveRecoveryDay(DayOfWeek.Saturday);
        }
        
        #endregion
        
        #region Pre-Season Days
        
        private static DailySessionTemplate CreatePreSeasonMonday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Monday,
                SessionName = "Conditioning Base",
                StartTime = new TimeSpan(8, 0, 0),
                Duration = TimeSpan.FromHours(3),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.Conditioning, 
                        Duration = TimeSpan.FromHours(2), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 35f,
                        Notes = "Aerobic base building, running loads"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromHours(1), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 8f
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreatePreSeasonTuesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Tuesday,
                SessionName = "Strength & Power",
                StartTime = new TimeSpan(8, 0, 0),
                Duration = TimeSpan.FromHours(2.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromHours(2), 
                        Intensity = TrainingIntensity.VeryHigh, 
                        LoadMultiplier = 40f,
                        Notes = "Weight training, power development"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 8f,
                        Notes = "Light skills work for active recovery"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreatePreSeasonWednesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Wednesday,
                SessionName = "Skills Integration",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromHours(1.5f), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 25f,
                        Notes = "Skill development under fatigue"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 8f
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreatePreSeasonThursday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Thursday,
                SessionName = "Game Simulation",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(2.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromHours(1.5f), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 20f,
                        Notes = "Match simulation, pressure training"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.Conditioning, 
                        Duration = TimeSpan.FromHours(1), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 15f,
                        Notes = "Match-specific conditioning"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreatePreSeasonFriday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Friday,
                SessionName = "Speed & Agility",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Fitness, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromHours(1), 
                        Intensity = TrainingIntensity.High, 
                        LoadMultiplier = 18f,
                        Notes = "Sprint work, agility, acceleration"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 5f
                    }
                }
            };
        }
        
        #endregion
        
        #region Finals Days
        
        private static DailySessionTemplate CreateFinalsMonday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Monday,
                SessionName = "Opposition Analysis",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1.5f),
                SessionType = DailySessionType.Tactical,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromHours(1), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 8f,
                        Notes = "Detailed opposition study, video analysis"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Mental, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 3f,
                        Notes = "Mental preparation, pressure handling"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateFinalsTuesday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Tuesday,
                SessionName = "Precision Training",
                StartTime = new TimeSpan(9, 0, 0),
                Duration = TimeSpan.FromHours(1.5f),
                SessionType = DailySessionType.Main,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromHours(1), 
                        Intensity = TrainingIntensity.Moderate, 
                        LoadMultiplier = 12f,
                        Notes = "Precision skills, pressure situations"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 6f,
                        Notes = "Set pieces, special situations"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateFinalsThursday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Thursday,
                SessionName = "Final Preparation",
                StartTime = new TimeSpan(10, 0, 0),
                Duration = TimeSpan.FromMinutes(75),
                SessionType = DailySessionType.MatchPreparation,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Tactical, 
                        Focus = TrainingFocus.TacticalAwareness, 
                        Duration = TimeSpan.FromMinutes(45), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 5f,
                        Notes = "Final tactical review, confidence building"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 4f,
                        Notes = "Light skills, rhythm work"
                    }
                }
            };
        }
        
        private static DailySessionTemplate CreateFinalsFriday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Friday,
                SessionName = "Final Tune-Up",
                StartTime = new TimeSpan(11, 0, 0),
                Duration = TimeSpan.FromMinutes(45),
                SessionType = DailySessionType.MatchPreparation,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Skills, 
                        Focus = TrainingFocus.SkillDevelopment, 
                        Duration = TimeSpan.FromMinutes(30), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 3f,
                        Notes = "Activation, feel-good skills"
                    },
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Mental, 
                        Focus = TrainingFocus.IndividualDevelopment, 
                        Duration = TimeSpan.FromMinutes(15), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 2f,
                        Notes = "Final mental preparation"
                    }
                }
            };
        }
        
        #endregion
        
        #region Recovery Week
        
        private static DailySessionTemplate CreateRecoveryMonday()
        {
            return new DailySessionTemplate
            {
                DayOfWeek = DayOfWeek.Monday,
                SessionName = "Gentle Movement",
                StartTime = new TimeSpan(10, 0, 0),
                Duration = TimeSpan.FromMinutes(60),
                SessionType = DailySessionType.Recovery,
                Components = new List<ComponentTemplate>
                {
                    new ComponentTemplate 
                    { 
                        Type = TrainingComponentType.Recovery, 
                        Focus = TrainingFocus.Recovery, 
                        Duration = TimeSpan.FromMinutes(60), 
                        Intensity = TrainingIntensity.Light, 
                        LoadMultiplier = 6f,
                        Notes = "Stretching, light movement, massage"
                    }
                }
            };
        }
        
        #endregion
        
        /// <summary>
        /// Get all available template types
        /// </summary>
        public static List<WeeklyScheduleTemplate> GetAllTemplates()
        {
            return new List<WeeklyScheduleTemplate>
            {
                CreateStandardWeekTemplate(),
                CreateMatchWeekTemplate(),
                CreateByeWeekTemplate(),
                CreatePreSeasonTemplate(),
                CreateFinalsTemplate(),
                CreateRecoveryWeekTemplate()
            };
        }
        
        /// <summary>
        /// Get template by type
        /// </summary>
        public static WeeklyScheduleTemplate GetTemplate(ScheduleTemplateType templateType)
        {
            return templateType switch
            {
                ScheduleTemplateType.Standard => CreateStandardWeekTemplate(),
                ScheduleTemplateType.MatchWeek => CreateMatchWeekTemplate(),
                ScheduleTemplateType.ByeWeek => CreateByeWeekTemplate(),
                ScheduleTemplateType.PreSeason => CreatePreSeasonTemplate(),
                ScheduleTemplateType.Finals => CreateFinalsTemplate(),
                ScheduleTemplateType.Recovery => CreateRecoveryWeekTemplate(),
                _ => CreateStandardWeekTemplate()
            };
        }
    }
}