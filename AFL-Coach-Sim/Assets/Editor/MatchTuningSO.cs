#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID
using UnityEngine;

namespace AFLCoachSim.Core.Engine.Match.Tuning
{
    /// <summary>
    /// Editor-facing ScriptableObject with sliders. Converts to MatchTuning at runtime via ToRuntime().
    /// </summary>
    [CreateAssetMenu(fileName = "MatchTuning", menuName = "AFL Coach Sim/Match Tuning", order = 0)]
    public sealed class MatchTuningSO : ScriptableObject
    {
        [Header("Injuries")]
        [Range(0f, 0.005f)] public float InjuryBasePerMinuteRisk = 0.0006f;
        [Range(0.5f, 1.5f)] public float InjuryOpenPlayMult = 1.00f;
        [Range(0.5f, 1.5f)] public float InjuryInside50Mult = 1.15f;
        [Range(0f, 1f)]     public float InjuryFatigueScale = 0.6f;
        [Range(0f, 1f)]     public float InjuryDurabilityScale = 0.5f;
        [Range(0, 6)]       public int   InjuryMaxPerTeam = 2;

        [Header("Weather → Progress (Inside 50 entries)")]
        [Range(0f, 60f)] public float WeatherProgressPenalty_Windy = 10f;
        [Range(0f, 60f)] public float WeatherProgressPenalty_LightRain = 20f;
        [Range(0f, 60f)] public float WeatherProgressPenalty_HeavyRain = 35f;

        [Header("Weather → Shot Accuracy (goal prob subtraction)")]
        [Range(0f, 0.8f)] public float WeatherAccuracyPenalty_Windy = 0.12f;
        [Range(0f, 0.8f)] public float WeatherAccuracyPenalty_LightRain = 0.20f;
        [Range(0f, 0.8f)] public float WeatherAccuracyPenalty_HeavyRain = 0.35f;

        [Header("Engine Baselines")]
        [Range(0f, 1f)]    public float ProgressBase = 0.45f;
        [Range(1f, 800f)]  public float ProgressScaleDivisor = 260f; // Inverted when building runtime tuning
        [Range(0f, 1f)]    public float ShotBaseGoal = 0.25f;
        [Range(0f, 1f)]    public float ShotScaleWithQual = 0.25f;

        public MatchTuning ToRuntime()
        {
            return new MatchTuning
            {
                InjuryBasePerMinuteRisk   = InjuryBasePerMinuteRisk,
                InjuryOpenPlayMult        = InjuryOpenPlayMult,
                InjuryInside50Mult        = InjuryInside50Mult,
                InjuryFatigueScale        = InjuryFatigueScale,
                InjuryDurabilityScale     = InjuryDurabilityScale,
                InjuryMaxPerTeam          = InjuryMaxPerTeam,

                WeatherProgressPenalty_Windy     = WeatherProgressPenalty_Windy,
                WeatherProgressPenalty_LightRain = WeatherProgressPenalty_LightRain,
                WeatherProgressPenalty_HeavyRain = WeatherProgressPenalty_HeavyRain,

                WeatherAccuracyPenalty_Windy     = WeatherAccuracyPenalty_Windy,
                WeatherAccuracyPenalty_LightRain = WeatherAccuracyPenalty_LightRain,
                WeatherAccuracyPenalty_HeavyRain = WeatherAccuracyPenalty_HeavyRain,

                ProgressBase      = ProgressBase,
                ProgressScale     = 1f / Mathf.Max(1f, ProgressScaleDivisor),
                ShotBaseGoal      = ShotBaseGoal,
                ShotScaleWithQual = ShotScaleWithQual
            };
        }
    }
}
#endif
