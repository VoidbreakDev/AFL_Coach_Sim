using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Domain.Entities
{
    public sealed class Player
    {
        public PlayerId Id { get; set; }
        public string Name { get; set; } = "Player";
        public int Age { get; set; }
        public Role PrimaryRole { get; set; }
        public Attributes Attr { get; set; } = new Attributes();

        // meta
        public int Endurance { get; set; } = 50;   // stamina pool baseline
        public int Durability { get; set; } = 50;  // injury resistance
        public int Discipline { get; set; } = 50;  // frees against / reports

        // season/match state
        public int Condition { get; set; } = 100;  // 0..100 fatigue/health
        public int Form { get; set; } = 0;         // -20..+20 short-term drift
    }
}