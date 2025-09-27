namespace AFLCoachSim.Core.Engine.Match.Tuning
{
    /// <summary>
    /// Pure data container used by the runtime engine (no Unity types).
    /// Tune defaults here; the ScriptableObject mirrors these values for editor tweaking.
    /// </summary>
    public sealed class MatchTuning
    {
        // --- INJURY SYSTEM ---
        // Base Risk Parameters
        public float InjuryBasePerMinuteRisk = 0.0006f; // Lower baseline for AFL realism
        public float InjuryOpenPlayMult      = 1.00f;
        public float InjuryInside50Mult      = 1.15f;
        public float InjuryFatigueScale      = 0.6f;    // How strongly fatigue raises risk
        public float InjuryDurabilityScale   = 0.5f;    // How strongly low durability raises risk
        public int   InjuryMaxPerTeam        = 2;       // Hard cap per team per match
        
        // Enhanced Injury Type Probabilities (by Phase)
        // Center Bounce - High contact, contest situations
        public float InjuryCenterBounce_Muscle     = 0.25f;
        public float InjuryCenterBounce_Joint      = 0.20f;
        public float InjuryCenterBounce_Bone       = 0.20f;
        public float InjuryCenterBounce_Ligament   = 0.15f;
        public float InjuryCenterBounce_Concussion = 0.15f;
        public float InjuryCenterBounce_Other      = 0.05f;
        
        // Open Play - Running based injuries
        public float InjuryOpenPlay_Muscle     = 0.40f;
        public float InjuryOpenPlay_Joint      = 0.30f;
        public float InjuryOpenPlay_Bone       = 0.08f;
        public float InjuryOpenPlay_Ligament   = 0.12f;
        public float InjuryOpenPlay_Concussion = 0.05f;
        public float InjuryOpenPlay_Other      = 0.05f;
        
        // Inside 50 - High contact, marking contests
        public float InjuryInside50_Muscle     = 0.30f;
        public float InjuryInside50_Joint      = 0.25f;
        public float InjuryInside50_Bone       = 0.15f;
        public float InjuryInside50_Ligament   = 0.15f;
        public float InjuryInside50_Concussion = 0.10f;
        public float InjuryInside50_Other      = 0.05f;
        
        // Age-based Risk Modifiers
        public float InjuryAgeRisk_Under21    = 0.85f; // Younger players slightly less prone
        public float InjuryAgeRisk_21to25     = 1.00f; // Prime age baseline
        public float InjuryAgeRisk_26to30     = 1.10f; // Slight increase
        public float InjuryAgeRisk_31to35     = 1.25f; // Notable increase
        public float InjuryAgeRisk_Over35     = 1.45f; // Significant increase
        
        // Recovery Time Modifiers (affects base recovery from unified system)
        public float RecoveryTime_TrainingMod = 0.90f; // Training can aid recovery slightly
        public float RecoveryTime_MatchMod    = 1.10f; // Match play may slow recovery
        public float RecoveryTime_RestMod     = 0.85f; // Complete rest aids recovery
        
        // Performance Impact Scaling
        public float PerfImpact_Niggle   = 0.95f; // 5% reduction
        public float PerfImpact_Minor    = 0.85f; // 15% reduction
        public float PerfImpact_Moderate = 0.70f; // 30% reduction
        public float PerfImpact_Major    = 0.50f; // 50% reduction
        public float PerfImpact_Severe   = 0.30f; // 70% reduction

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