using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Simulation; // DeterministicRandom

namespace AFLCoachSim.Core.Engine.Match.Fatigue
{
    public sealed class FatigueModel
    {
        // Intensity multipliers by phase
        private const float CenterBounceLoad = 0.6f;
        private const float StoppageLoad     = 0.7f;
        private const float OpenPlayLoad     = 1.0f;
        private const float Inside50Load     = 1.1f;
        private const float ShotLoad         = 0.8f;
        private const float KickInLoad       = 0.7f;

        // How much condition drains per sim-second at baseline (Condition 0..100)
        private const float BaseDrainPerSec = 0.010f; // ~12 per 20min at max load

        public void ApplyFatigue(IList<PlayerRuntime> squad, Phase phase, int dtSeconds)
        {
            float phaseLoad = PhaseLoad(phase);

            for (int i = 0; i < squad.Count; i++)
            {
                var pr = squad[i];
                if (pr.InjuredOut) continue;

                // Only on-field players accumulate play time & drain condition
                if (pr.OnField)
                {
                    pr.SecondsPlayed += dtSeconds;
                    pr.SecondsSinceRotation += dtSeconds;

                    // endurance mitigates drain; condition floors at 0
                    float endurance = pr.Player.Endurance <= 0 ? 1f : (pr.Player.Endurance / 100f);
                    float drain = BaseDrainPerSec * phaseLoad * (1.15f - 0.5f * endurance) * dtSeconds;

                    // Condition is an int on Player (0..100): drain, clamp
                    int newCondition = pr.Player.Condition - (int)(drain * 100f); // amplify to “points”
                    if (newCondition < 0) newCondition = 0;
                    pr.Player.Condition = newCondition;
                }
                else
                {
                    // On bench recovers a little condition back (up to 100)
                    if (pr.Player.Condition < 100)
                    {
                        int rec = (int)(dtSeconds * 0.6f); // 0.6 points/second ≈ 36/Min
                        int c = pr.Player.Condition + rec;
                        pr.Player.Condition = c > 100 ? 100 : c;
                    }
                }

                // Convert condition to fatigue multiplier (so ratings scale 0.75..1.0 typically)
                pr.FatigueMult = 0.75f + 0.25f * (pr.Player.Condition / 100f);
                if (pr.FatigueMult < 0.6f) pr.FatigueMult = 0.6f; // hard floor
            }
        }

        private static float PhaseLoad(Phase p)
        {
            switch (p)
            {
                case Phase.CenterBounce: return CenterBounceLoad;
                case Phase.Stoppage:     return StoppageLoad;
                case Phase.OpenPlay:     return OpenPlayLoad;
                case Phase.Inside50:     return Inside50Load;
                case Phase.ShotOnGoal:   return ShotLoad;
                case Phase.KickIn:       return KickInLoad;
                default:                 return 1f;
            }
        }
    }
}