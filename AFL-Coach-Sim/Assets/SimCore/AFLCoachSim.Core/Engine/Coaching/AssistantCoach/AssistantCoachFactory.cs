using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Coaching.AssistantCoach
{
    /// <summary>
    /// Factory for creating assistant coaches with realistic profiles and backgrounds
    /// </summary>
    public static class AssistantCoachFactory
    {
        private static readonly Random _random = new Random();

        #region Name and Background Data

        private static readonly List<string> FirstNames = new List<string>
        {
            "Michael", "David", "James", "Robert", "John", "Paul", "Mark", "Andrew", "Peter", "Stephen",
            "Chris", "Matthew", "Daniel", "Simon", "Anthony", "Kevin", "Gary", "Jason", "Scott", "Adam",
            "Cameron", "Brett", "Shane", "Nathan", "Craig", "Damien", "Ben", "Luke", "Joel", "Ryan"
        };

        private static readonly List<string> LastNames = new List<string>
        {
            "Smith", "Johnson", "Brown", "Wilson", "Davis", "Miller", "Taylor", "Anderson", "Thomas", "Jackson",
            "White", "Harris", "Martin", "Thompson", "Garcia", "Martinez", "Robinson", "Clark", "Rodriguez", "Lewis",
            "Walker", "Hall", "Allen", "Young", "King", "Wright", "Lopez", "Hill", "Green", "Adams"
        };

        private static readonly List<string> AFLClubs = new List<string>
        {
            "Adelaide Crows", "Brisbane Lions", "Carlton Blues", "Collingwood Magpies", "Essendon Bombers",
            "Fremantle Dockers", "Geelong Cats", "Gold Coast Suns", "Greater Western Sydney Giants", "Hawthorn Hawks",
            "Melbourne Demons", "North Melbourne Kangaroos", "Port Adelaide Power", "Richmond Tigers", 
            "St Kilda Saints", "Sydney Swans", "West Coast Eagles", "Western Bulldogs"
        };

        private static readonly List<string> VFLClubs = new List<string>
        {
            "Box Hill Hawks", "Casey Demons", "Coburg Lions", "Footscray Bulldogs", "Northern Bullants",
            "Port Melbourne Borough", "Preston Bullants", "Richmond Tigers", "Sandringham Zebras", "Werribee Tigers",
            "Williamstown Seagulls", "Frankston Dolphins", "Bendigo Bombers", "Ballarat Panthers"
        };

        private static readonly List<string> Qualifications = new List<string>
        {
            "Level 1 AFL Coaching Accreditation", "Level 2 AFL Coaching Accreditation", "Level 3 AFL Coaching Accreditation",
            "Sports Science Degree", "Exercise Physiology Degree", "Sports Psychology Certification",
            "Strength and Conditioning Certificate", "First Aid Certificate", "Match Analysis Certification",
            "Youth Development Certificate", "Injury Prevention Specialist", "Tactical Analysis Diploma"
        };

        #endregion

        #region Public Factory Methods

        /// <summary>
        /// Generate a random assistant coach with specified specialization
        /// </summary>
        public static AssistantCoachProfile CreateRandomAssistant(AssistantCoachSpecialization specialization, 
            int skillLevel = -1, int experience = -1)
        {
            var assistant = new AssistantCoachProfile
            {
                Name = GenerateRandomName(),
                Specialization = specialization,
                Age = _random.Next(28, 65), // Realistic age range for coaches
                SkillLevel = skillLevel >= 0 ? skillLevel : GenerateSkillLevel(specialization),
                YearsExperience = experience >= 0 ? experience : GenerateExperience(specialization)
            };

            // Generate personality traits based on specialization
            GeneratePersonalityTraits(assistant);
            
            // Generate background and reputation
            GenerateBackground(assistant);
            
            // Set contract details
            SetContractDetails(assistant);

            return assistant;
        }

        /// <summary>
        /// Create an elite-level assistant coach (high skill, high reputation)
        /// </summary>
        public static AssistantCoachProfile CreateEliteAssistant(AssistantCoachSpecialization specialization)
        {
            var assistant = CreateRandomAssistant(specialization, _random.Next(80, 95), _random.Next(8, 20));
            
            // Elite assistants have better traits
            assistant.Reputation = _random.Next(70, 95);
            assistant.Communication += _random.Next(10, 25);
            assistant.Patience += _random.Next(10, 20);
            assistant.Innovation += _random.Next(5, 20);
            
            // Ensure no trait exceeds 100
            assistant.Communication = Math.Min(100f, assistant.Communication);
            assistant.Patience = Math.Min(100f, assistant.Patience);
            assistant.Innovation = Math.Min(100f, assistant.Innovation);
            
            // Higher salary for elite coaches
            assistant.SalaryPerWeek *= _random.Next(150, 300) / 100f;
            
            // More AFL experience
            assistant.PreviousClubs.AddRange(AFLClubs.OrderBy(x => _random.Next()).Take(_random.Next(2, 5)));
            
            CoreLogger.Log($"[AssistantFactory] Created elite {specialization} assistant: {assistant.Name}");
            return assistant;
        }

        /// <summary>
        /// Create a budget/rookie assistant coach (lower skill, lower cost)
        /// </summary>
        public static AssistantCoachProfile CreateRookieAssistant(AssistantCoachSpecialization specialization)
        {
            var assistant = CreateRandomAssistant(specialization, _random.Next(35, 65), _random.Next(1, 4));
            
            // Rookie assistants have mixed traits
            assistant.Reputation = _random.Next(20, 50);
            assistant.Communication = _random.Next(30, 70);
            assistant.Patience = _random.Next(40, 80); // Often more patient as they're learning
            assistant.Innovation = _random.Next(20, 60);
            assistant.Intensity = _random.Next(40, 90); // Can be very intense to prove themselves
            
            // Lower salary
            assistant.SalaryPerWeek *= _random.Next(40, 80) / 100f;
            
            // Mostly VFL/local experience
            assistant.PreviousClubs.AddRange(VFLClubs.OrderBy(x => _random.Next()).Take(_random.Next(1, 3)));
            
            CoreLogger.Log($"[AssistantFactory] Created rookie {specialization} assistant: {assistant.Name}");
            return assistant;
        }

        /// <summary>
        /// Create a veteran assistant coach (high experience, moderate skill)
        /// </summary>
        public static AssistantCoachProfile CreateVeteranAssistant(AssistantCoachSpecialization specialization)
        {
            var assistant = CreateRandomAssistant(specialization, _random.Next(60, 85), _random.Next(12, 30));
            
            // Veterans have excellent people skills
            assistant.Reputation = _random.Next(60, 85);
            assistant.Communication = _random.Next(70, 95);
            assistant.Patience = _random.Next(75, 95);
            assistant.Innovation = _random.Next(30, 70); // May be set in their ways
            assistant.Intensity = _random.Next(40, 75); // More measured approach
            assistant.Age = _random.Next(45, 62); // Older
            
            // Moderate salary - good value
            assistant.SalaryPerWeek *= _random.Next(90, 140) / 100f;
            
            // Extensive experience across multiple clubs
            var allClubs = AFLClubs.Concat(VFLClubs).ToList();
            assistant.PreviousClubs.AddRange(allClubs.OrderBy(x => _random.Next()).Take(_random.Next(3, 7)));
            
            // May have been a former player
            if (_random.NextDouble() < 0.4) // 40% chance
            {
                assistant.FormerPlayerPosition = GetRandomPlayerPosition(specialization);
            }
            
            CoreLogger.Log($"[AssistantFactory] Created veteran {specialization} assistant: {assistant.Name}");
            return assistant;
        }

        /// <summary>
        /// Create an assistant coach based on specific criteria
        /// </summary>
        public static AssistantCoachProfile CreateCustomAssistant(
            AssistantCoachSpecialization specialization,
            string name = null,
            int? skillLevel = null,
            int? experience = null,
            int? age = null,
            float? reputation = null)
        {
            var assistant = CreateRandomAssistant(specialization);
            
            if (!string.IsNullOrEmpty(name))
                assistant.Name = name;
            if (skillLevel.HasValue)
                assistant.SkillLevel = Math.Max(0f, Math.Min(100f, skillLevel.Value));
            if (experience.HasValue)
                assistant.YearsExperience = Math.Max(0, experience.Value);
            if (age.HasValue)
                assistant.Age = Math.Max(22, Math.Min(70, age.Value));
            if (reputation.HasValue)
                assistant.Reputation = Math.Max(0f, Math.Min(100f, reputation.Value));
                
            return assistant;
        }

        /// <summary>
        /// Generate a pool of available assistant coaches for hiring
        /// </summary>
        public static List<AssistantCoachProfile> GenerateHiringPool(int poolSize = 20)
        {
            var pool = new List<AssistantCoachProfile>();
            var specializations = Enum.GetValues(typeof(AssistantCoachSpecialization)).Cast<AssistantCoachSpecialization>().ToList();
            
            for (int i = 0; i < poolSize; i++)
            {
                var specialization = specializations[_random.Next(specializations.Count)];
                
                // Mix of different quality levels
                var qualityRoll = _random.NextDouble();
                AssistantCoachProfile assistant;
                
                if (qualityRoll < 0.15) // 15% elite
                    assistant = CreateEliteAssistant(specialization);
                else if (qualityRoll < 0.35) // 20% veteran  
                    assistant = CreateVeteranAssistant(specialization);
                else if (qualityRoll < 0.65) // 30% standard
                    assistant = CreateRandomAssistant(specialization);
                else // 35% rookie
                    assistant = CreateRookieAssistant(specialization);
                    
                pool.Add(assistant);
            }
            
            CoreLogger.Log($"[AssistantFactory] Generated hiring pool of {poolSize} assistant coaches");
            return pool.OrderBy(a => a.Specialization).ThenByDescending(a => a.CalculateOverallValue()).ToList();
        }

        #endregion

        #region Private Helper Methods

        private static string GenerateRandomName()
        {
            var firstName = FirstNames[_random.Next(FirstNames.Count)];
            var lastName = LastNames[_random.Next(LastNames.Count)];
            return $"{firstName} {lastName}";
        }

        private static int GenerateSkillLevel(AssistantCoachSpecialization specialization)
        {
            // Different specializations have different skill distributions
            return specialization switch
            {
                AssistantCoachSpecialization.TacticalCoach => _random.Next(50, 90), // Generally higher skilled
                AssistantCoachSpecialization.DevelopmentCoach => _random.Next(45, 85), // Good range
                AssistantCoachSpecialization.FitnessCoach => _random.Next(55, 90), // Scientific background
                AssistantCoachSpecialization.RecoveryCoach => _random.Next(50, 85), // Specialized field
                _ => _random.Next(40, 80) // Standard range for positional coaches
            };
        }

        private static int GenerateExperience(AssistantCoachSpecialization specialization)
        {
            // Some specializations require more experience
            return specialization switch
            {
                AssistantCoachSpecialization.TacticalCoach => _random.Next(3, 15), // Need experience
                AssistantCoachSpecialization.DevelopmentCoach => _random.Next(2, 12), // Can start younger
                _ => _random.Next(1, 10) // Standard range
            };
        }

        private static void GeneratePersonalityTraits(AssistantCoachProfile assistant)
        {
            // Base traits on specialization with some randomness
            switch (assistant.Specialization)
            {
                case AssistantCoachSpecialization.FitnessCoach:
                    assistant.Intensity = _random.Next(60, 95); // High intensity for fitness
                    assistant.Communication = _random.Next(50, 80);
                    assistant.Patience = _random.Next(40, 70);
                    assistant.Innovation = _random.Next(50, 85); // Science-based innovation
                    break;
                    
                case AssistantCoachSpecialization.DevelopmentCoach:
                    assistant.Patience = _random.Next(70, 95); // Very patient with young players
                    assistant.Communication = _random.Next(65, 90); // Good with communication
                    assistant.Intensity = _random.Next(40, 70); // Moderate intensity
                    assistant.Innovation = _random.Next(45, 75);
                    break;
                    
                case AssistantCoachSpecialization.TacticalCoach:
                    assistant.Innovation = _random.Next(60, 90); // Innovative tactics
                    assistant.Communication = _random.Next(55, 85);
                    assistant.Patience = _random.Next(50, 80);
                    assistant.Intensity = _random.Next(50, 80);
                    break;
                    
                case AssistantCoachSpecialization.RecoveryCoach:
                    assistant.Patience = _random.Next(60, 90); // Patient with recovery
                    assistant.Communication = _random.Next(60, 85);
                    assistant.Intensity = _random.Next(30, 60); // Lower intensity
                    assistant.Innovation = _random.Next(50, 80);
                    break;
                    
                default: // Positional coaches (Forward, Defense, Midfield)
                    assistant.Intensity = _random.Next(50, 85);
                    assistant.Communication = _random.Next(50, 80);
                    assistant.Patience = _random.Next(45, 75);
                    assistant.Innovation = _random.Next(40, 70);
                    break;
            }
        }

        private static void GenerateBackground(AssistantCoachProfile assistant)
        {
            // Generate reputation based on skill and experience
            float baseReputation = (assistant.SkillLevel + assistant.YearsExperience * 3f) / 2f;
            assistant.Reputation = Math.Max(10f, Math.Min(90f, baseReputation + _random.Next(-15, 16)));
            
            // Add some previous clubs based on experience
            var clubPool = assistant.YearsExperience > 5 ? AFLClubs.Concat(VFLClubs).ToList() : VFLClubs.ToList();
            int numClubs = Math.Max(1, assistant.YearsExperience / 3);
            assistant.PreviousClubs.AddRange(clubPool.OrderBy(x => _random.Next()).Take(Math.Min(numClubs, 5)));
            
            // Add qualifications based on specialization
            var relevantQuals = GetRelevantQualifications(assistant.Specialization);
            int numQuals = Math.Max(1, assistant.YearsExperience / 4 + _random.Next(0, 3));
            assistant.Qualifications.AddRange(relevantQuals.Concat(Qualifications).OrderBy(x => _random.Next()).Take(numQuals));
            
            // 30% chance of being a former player
            if (_random.NextDouble() < 0.3)
            {
                assistant.FormerPlayerPosition = GetRandomPlayerPosition(assistant.Specialization);
            }
        }

        private static void SetContractDetails(AssistantCoachProfile assistant)
        {
            // Base salary calculation
            float baseSalary = 800f; // Base weekly salary
            
            // Skill multiplier
            float skillMultiplier = 0.5f + (assistant.SkillLevel / 100f);
            
            // Experience multiplier  
            float expMultiplier = 1f + (assistant.YearsExperience * 0.03f);
            
            // Reputation multiplier
            float repMultiplier = 0.8f + (assistant.Reputation / 100f * 0.4f);
            
            // Specialization multiplier (some specializations are in higher demand)
            float specMultiplier = assistant.Specialization switch
            {
                AssistantCoachSpecialization.TacticalCoach => 1.3f,
                AssistantCoachSpecialization.FitnessCoach => 1.2f,
                AssistantCoachSpecialization.DevelopmentCoach => 1.1f,
                AssistantCoachSpecialization.RecoveryCoach => 1.2f,
                _ => 1.0f
            };
            
            assistant.SalaryPerWeek = baseSalary * skillMultiplier * expMultiplier * repMultiplier * specMultiplier;
            
            // Contract length (1-3 years, better coaches get longer contracts)
            int contractYears = assistant.SkillLevel > 70 ? _random.Next(2, 4) : _random.Next(1, 3);
            assistant.ContractWeeksRemaining = contractYears * 52;
        }

        private static List<string> GetRelevantQualifications(AssistantCoachSpecialization specialization)
        {
            return specialization switch
            {
                AssistantCoachSpecialization.FitnessCoach => new List<string> 
                { 
                    "Sports Science Degree", "Exercise Physiology Degree", "Strength and Conditioning Certificate" 
                },
                AssistantCoachSpecialization.TacticalCoach => new List<string> 
                { 
                    "Level 3 AFL Coaching Accreditation", "Match Analysis Certification", "Tactical Analysis Diploma" 
                },
                AssistantCoachSpecialization.DevelopmentCoach => new List<string> 
                { 
                    "Youth Development Certificate", "Sports Psychology Certification", "Level 2 AFL Coaching Accreditation" 
                },
                AssistantCoachSpecialization.RecoveryCoach => new List<string> 
                { 
                    "Sports Science Degree", "Injury Prevention Specialist", "First Aid Certificate" 
                },
                _ => new List<string> { "Level 1 AFL Coaching Accreditation", "Level 2 AFL Coaching Accreditation" }
            };
        }

        private static string GetRandomPlayerPosition(AssistantCoachSpecialization specialization)
        {
            return specialization switch
            {
                AssistantCoachSpecialization.ForwardCoach => new[] { "Full Forward", "Forward Pocket", "Half Forward" }[_random.Next(3)],
                AssistantCoachSpecialization.DefensiveCoach => new[] { "Full Back", "Back Pocket", "Half Back" }[_random.Next(3)],
                AssistantCoachSpecialization.MidfielderCoach => new[] { "Centre", "Wing", "Ruck", "Rover" }[_random.Next(4)],
                _ => new[] { "Centre", "Wing", "Half Back", "Half Forward", "Full Forward", "Full Back" }[_random.Next(6)]
            };
        }

        #endregion
    }
}