using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Data
{
    public sealed class PlayerConfig
    {
        public int Id;
        public string Name = "Player";
        public int Age;
        public Role PrimaryRole;

        // Attributes
        public int Speed, Acceleration, Strength, Agility, Jump;
        public int Kicking, Marking, Handball, Tackling, Clearance, RuckWork, Spoiling;
        public int DecisionMaking, Composure, WorkRate, Positioning, Leadership;

        // Meta
        public int Endurance = 50, Durability = 50, Discipline = 50;

        public Player ToDomain()
        {
            return new Player
            {
                Id = new PlayerId(Id),
                Name = Name,
                Age = Age,
                PrimaryRole = PrimaryRole,
                Attr = new Attributes
                {
                    Speed = Speed, Acceleration = Acceleration, Strength = Strength, Agility = Agility, Jump = Jump,
                    Kicking = Kicking, Marking = Marking, Handball = Handball, Tackling = Tackling, Clearance = Clearance,
                    RuckWork = RuckWork, Spoiling = Spoiling, DecisionMaking = DecisionMaking, Composure = Composure,
                    WorkRate = WorkRate, Positioning = Positioning, Leadership = Leadership
                },
                Endurance = Endurance, Durability = Durability, Discipline = Discipline,
                Condition = 100, Form = 0
            };
        }
    }
}