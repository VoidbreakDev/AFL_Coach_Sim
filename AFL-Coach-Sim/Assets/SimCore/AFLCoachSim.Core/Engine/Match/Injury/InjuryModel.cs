using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Match;            // <-- for Phase enum
using AFLCoachSim.Core.Engine.Match.Runtime;    // PlayerRuntime
using AFLCoachSim.Core.Engine.Simulation;       // DeterministicRandom
namespace AFLCoachSim.Core.Engine.Match.Injury

{
    public enum InjurySeverity { Niggle, Minor, Moderate, Major }

    public sealed class InjuryModel
    {
        // Baseline per-minute risk under normal intensity, before durability/fatigue scaling
        private const float BasePerMinuteRisk = 0.0025f; // 0.25% per minute baseline
        private const float OpenPlayRiskMult  = 1.2f;
        private const float Inside50RiskMult  = 1.3f;

        public int Step(IList<PlayerRuntime> onField, IList<PlayerRuntime> bench, Phase phase, int dtSeconds, DeterministicRandom rng)
        {
            {
    int injuries = 0;

    float mult = PhaseRiskMult(phase);
    float perSecondBase = BasePerMinuteRisk / 60f;

    for (int i = 0; i < onField.Count; i++)
    {
        var pr = onField[i];
        if (pr.InjuredOut) continue;

        float fatigueScale = 1f + (1f - pr.FatigueMult);
        float durabilityScale = 1f + (0.6f * (100 - pr.Player.Durability) / 100f);
        float p = perSecondBase * mult * fatigueScale * durabilityScale * dtSeconds;

        if (rng.NextFloat() < p)
        {
            var sev = RollSeverity(rng, pr);
            ApplyInjury(pr, sev);
            injuries++;
        }
    }

    for (int j = 0; j < bench.Count; j++)
    {
        var br = bench[j];
        if (br.ReturnInSeconds > 0)
        {
            br.ReturnInSeconds -= dtSeconds;
            if (br.ReturnInSeconds <= 0) br.ReturnInSeconds = 0;
        }
    }

    return injuries;
}
        }

        private static void ApplyInjury(PlayerRuntime pr, InjurySeverity sev)
        {
            switch (sev)
            {
                case InjurySeverity.Niggle:
                    pr.InjuryMult = 0.9f; // slight performance impact
                    pr.ReturnInSeconds = 30; // can continue shortly if subbed
                    break;
                case InjurySeverity.Minor:
                    pr.InjuryMult = 0.8f;
                    pr.ReturnInSeconds = 3 * 60; // need few minutes off
                    break;
                case InjurySeverity.Moderate:
                    pr.InjuryMult = 0.7f;
                    pr.InjuredOut = true; // out for match
                    break;
                case InjurySeverity.Major:
                    pr.InjuryMult = 0.5f;
                    pr.InjuredOut = true; // out for match (season logic later)
                    break;
            }
        }

        private static InjurySeverity RollSeverity(DeterministicRandom rng, PlayerRuntime pr)
        {
            // More fatigue increases chance of worse severity slightly
            float f = 1f - pr.FatigueMult; // 0..0.4
            float u = rng.NextFloat();

            if (u < 0.55f - 0.2f * f) return InjurySeverity.Niggle;
            if (u < 0.85f - 0.1f * f) return InjurySeverity.Minor;
            if (u < 0.96f)            return InjurySeverity.Moderate;
            return InjurySeverity.Major;
        }

        private static float PhaseRiskMult(Phase p)
        {
            switch (p)
            {
                case Phase.OpenPlay:   return OpenPlayRiskMult;
                case Phase.Inside50:   return Inside50RiskMult;
                default:               return 1f;
            }
        }
    }
}