// Domain/ValueObjects/TeamId.cs
namespace AFLCoachSim.Core.Domain.ValueObjects
{
    public readonly struct TeamId
    {
        public int Value { get; }
        public TeamId(int value) => Value = value;
        public override string ToString() => Value.ToString();

        // AFL Team Constants
        public static readonly TeamId None = new TeamId(0);
        public static readonly TeamId Adelaide = new TeamId(1);
        public static readonly TeamId Brisbane = new TeamId(2);
        public static readonly TeamId Carlton = new TeamId(3);
        public static readonly TeamId Collingwood = new TeamId(4);
        public static readonly TeamId Essendon = new TeamId(5);
        public static readonly TeamId Fremantle = new TeamId(6);
        public static readonly TeamId Geelong = new TeamId(7);
        public static readonly TeamId GoldCoast = new TeamId(8);
        public static readonly TeamId GWS = new TeamId(9);
        public static readonly TeamId Hawthorn = new TeamId(10);
        public static readonly TeamId Melbourne = new TeamId(11);
        public static readonly TeamId NorthMelbourne = new TeamId(12);
        public static readonly TeamId PortAdelaide = new TeamId(13);
        public static readonly TeamId Richmond = new TeamId(14);
        public static readonly TeamId StKilda = new TeamId(15);
        public static readonly TeamId Sydney = new TeamId(16);
        public static readonly TeamId WestCoast = new TeamId(17);
        public static readonly TeamId WesternBulldogs = new TeamId(18);

        public static bool operator ==(TeamId left, TeamId right) => left.Value == right.Value;
        public static bool operator !=(TeamId left, TeamId right) => left.Value != right.Value;
        
        // Implicit conversion operators for compatibility
        public static implicit operator int(TeamId teamId) => teamId.Value;
        public static implicit operator TeamId(int value) => new TeamId(value);
        
        public override bool Equals(object obj) => obj is TeamId other && this == other;
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Get all AFL team IDs (excluding None)
        /// </summary>
        public static TeamId[] GetAllTeams()
        {
            return new TeamId[]
            {
                Adelaide, Brisbane, Carlton, Collingwood, Essendon, Fremantle,
                Geelong, GoldCoast, GWS, Hawthorn, Melbourne, NorthMelbourne,
                PortAdelaide, Richmond, StKilda, Sydney, WestCoast, WesternBulldogs
            };
        }
    }
}
