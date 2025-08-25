namespace AFLCoachSim.Core.Engine.Match.Tuning
{
    /// <summary>
    /// Pure data container used by the runtime engine (no Unity types).
    /// Tune defaults here; the ScriptableObject mirrors these values for editor tweaking.
    /// </summary>
    public sealed class MatchTuning
    {
        // --- INJURY ---
        public float InjuryBasePerMinuteRisk = 0.0006f; // Lower baseline for AFL realism
        public float InjuryOpenPlayMult      = 1.00f;
        public float InjuryInside50Mult      = 1.15f;
        public float InjuryFatigueScale      = 0.6f;    // How strongly fatigue raises risk
        public float InjuryDurabilityScale   = 0.5f;    // How strongly low durability raises risk
        public int   InjuryMaxPerTeam        = 2;       // Hard cap per team per match

        // --- WEATHER impact on ball movement (progress to Inside50) ---
        public float WeatherProgressPenalty_Windy     = 10f;
        public float WeatherProgressPenalty_LightRain = 20f;
        public float WeatherProgressPenalty_HeavyRain = 35f;

        // --- WEATHER impact on shot accuracy (goal probability subtraction) ---
        public float WeatherAccuracyPenalty_Windy     = 0.12f;
        public float WeatherAccuracyPenalty_LightRain = 0.20f;
        public float WeatherAccuracyPenalty_HeavyRain = 0.35f;

        // --- BASELINES for engine probabilities ---
        // Progress chance to move ball into F50:
        public float ProgressBase  = 0.45f;     // Base chance (before quality / penalties)
        public float ProgressScale = 1f / 260f; // Normalization: higher divisor => lower p

        // Shot conversion:
        public float ShotBaseGoal      = 0.25f; // Base chance to score a goal (before quality / weather)
        public float ShotScaleWithQual = 0.25f; // Contribution of quality to goal chance

        public static MatchTuning Default { get; } = new MatchTuning();
    }
}