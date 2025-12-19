using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Engine.Match.Runtime;

namespace AFLCoachSim.Core.Engine.Match
{
    public static class Rating
    {
        // ----------------------------
        // M2-style (no fatigue/injury)
        // ----------------------------
        public static float MidfieldUnit(List<Player> onField)
        {
            if (onField == null || onField.Count == 0) return 1f;
            // take top 5 of score = 0.45*Clearance + 0.25*Strength + 0.15*Positioning + 0.15*DecisionMaking
            float[] top = new float[5];
            int topCount = 0;

            for (int i = 0; i < onField.Count; i++)
            {
                var p = onField[i];
                float s = 0.45f * p.Attr.Clearance + 0.25f * p.Attr.Strength
                        + 0.15f * p.Attr.Positioning + 0.15f * p.Attr.DecisionMaking;

                InsertTopDescending(top, ref topCount, s, 5);
            }

            return Average(top, topCount);
        }

        public static float Inside50Quality(List<Player> onField)
        {
            if (onField == null || onField.Count == 0) return 1f;
            // take top 6 of score = 0.5*Marking + 0.3*Kicking + 0.2*DecisionMaking
            float[] top = new float[6];
            int topCount = 0;

            for (int i = 0; i < onField.Count; i++)
            {
                var p = onField[i];
                float s = 0.5f * p.Attr.Marking + 0.3f * p.Attr.Kicking + 0.2f * p.Attr.DecisionMaking;
                InsertTopDescending(top, ref topCount, s, 6);
            }

            return Average(top, topCount);
        }

        public static float DefensePressure(List<Player> onField)
        {
            if (onField == null || onField.Count == 0) return 1f;

            float sum = 0f;
            int cnt = 0;
            for (int i = 0; i < onField.Count; i++)
            {
                var p = onField[i];
                float s = 0.5f * p.Attr.Tackling + 0.3f * p.Attr.Positioning + 0.2f * p.Attr.WorkRate;
                sum += s; cnt++;
            }
            return cnt == 0 ? 1f : (sum / cnt);
        }

        // ---------------------------------
        // M3-style (fatigue/injury aware)
        // ---------------------------------

        /// <summary>
        /// Position-aware midfield rating - prioritizes actual midfielders and ruckmen
        /// OPTIMIZED: Uses zero-allocation method when RNG is provided
        /// </summary>
        public static float MidfieldUnit(IList<PlayerRuntime> onField, Simulation.DeterministicRandom rng = null)
        {
            if (onField == null || onField.Count == 0) return 1f;

            // Use optimized method when RNG is available (called from hot path)
            if (rng != null)
            {
                return Selection.PositionalSelector.GetCenterBounceRatingOptimized(onField, rng);
            }

            // Fallback for backward compatibility (when called without RNG)
            // This path creates allocations but is only used outside the hot path
            var centerBounceParticipants = Selection.PositionalSelector.GetCenterBounceParticipants(onField, new Simulation.DeterministicRandom(12345), 5);

            if (centerBounceParticipants.Count == 0)
            {
                // Fallback to original logic if no suitable players
                float[] top = new float[5];
                int topCount = 0;

                for (int i = 0; i < onField.Count; i++)
                {
                    var pr = onField[i];
                    var a = pr.Player.Attr;
                    float s = (0.45f * a.Clearance + 0.25f * a.Strength
                             + 0.15f * a.Positioning + 0.15f * a.DecisionMaking)
                             * pr.FatigueMult * pr.InjuryMult;

                    InsertTopDescending(top, ref topCount, s, 5);
                }

                return Average(top, topCount);
            }

            // Use position-appropriate players
            float sum = 0f;
            for (int i = 0; i < centerBounceParticipants.Count; i++)
            {
                var pr = centerBounceParticipants[i];
                var a = pr.Player.Attr;
                float s = (0.45f * a.Clearance + 0.25f * a.Strength
                         + 0.15f * a.Positioning + 0.15f * a.DecisionMaking)
                         * pr.FatigueMult * pr.InjuryMult;
                sum += s;
            }

            return sum / centerBounceParticipants.Count;
        }

        /// <summary>
        /// Position-aware Inside50 quality rating - prioritizes forwards and attacking midfielders
        /// OPTIMIZED: Uses zero-allocation method when RNG is provided
        /// </summary>
        public static float Inside50Quality(IList<PlayerRuntime> onField, Simulation.DeterministicRandom rng = null)
        {
            if (onField == null || onField.Count == 0) return 1f;

            // Use optimized method when RNG is available (called from hot path)
            if (rng != null)
            {
                return Selection.PositionalSelector.GetInside50RatingOptimized(onField, rng);
            }

            // Fallback for backward compatibility (when called without RNG)
            var inside50Participants = Selection.PositionalSelector.GetInside50Participants(onField, new Simulation.DeterministicRandom(12345), 6);

            if (inside50Participants.Count == 0)
            {
                // Fallback to original logic
                float[] top = new float[6];
                int topCount = 0;

                for (int i = 0; i < onField.Count; i++)
                {
                    var pr = onField[i];
                    var a = pr.Player.Attr;
                    float s = (0.5f * a.Marking + 0.3f * a.Kicking + 0.2f * a.DecisionMaking)
                             * pr.FatigueMult * pr.InjuryMult;

                    InsertTopDescending(top, ref topCount, s, 6);
                }

                return Average(top, topCount);
            }

            // Use position-appropriate players
            float sum = 0f;
            for (int i = 0; i < inside50Participants.Count; i++)
            {
                var pr = inside50Participants[i];
                var a = pr.Player.Attr;
                float s = (0.5f * a.Marking + 0.3f * a.Kicking + 0.2f * a.DecisionMaking)
                         * pr.FatigueMult * pr.InjuryMult;
                sum += s;
            }

            return sum / inside50Participants.Count;
        }

        /// <summary>
        /// Position-aware defense pressure rating - prioritizes defenders and defensive midfielders
        /// OPTIMIZED: Uses zero-allocation method when RNG is provided
        /// </summary>
        public static float DefensePressure(IList<PlayerRuntime> onField, Simulation.DeterministicRandom rng = null)
        {
            if (onField == null || onField.Count == 0) return 1f;

            // Use optimized method when RNG is available (called from hot path)
            if (rng != null)
            {
                return Selection.PositionalSelector.GetDefenseRatingOptimized(onField, rng);
            }

            // Fallback for backward compatibility (when called without RNG)
            var defensiveParticipants = Selection.PositionalSelector.GetDefensiveParticipants(onField, new Simulation.DeterministicRandom(12345), 6);

            if (defensiveParticipants.Count == 0)
            {
                // Fallback to original logic using all players
                float fallbackSum = 0f;
                int cnt = 0;
                for (int i = 0; i < onField.Count; i++)
                {
                    var pr = onField[i];
                    var a = pr.Player.Attr;
                    float s = (0.5f * a.Tackling + 0.3f * a.Positioning + 0.2f * a.WorkRate)
                              * pr.FatigueMult * pr.InjuryMult;
                    fallbackSum += s; cnt++;
                }
                return cnt == 0 ? 1f : (fallbackSum / cnt);
            }

            // Use position-appropriate players
            float sum = 0f;
            for (int i = 0; i < defensiveParticipants.Count; i++)
            {
                var pr = defensiveParticipants[i];
                var a = pr.Player.Attr;
                float s = (0.5f * a.Tackling + 0.3f * a.Positioning + 0.2f * a.WorkRate)
                          * pr.FatigueMult * pr.InjuryMult;
                sum += s;
            }

            return sum / defensiveParticipants.Count;
        }

        // ----------------
        // Common helpers
        // ----------------
        public static float Softmax(float a, float b)
        {
            // stable two-class softmax using doubles
            double max = a > b ? a : b;
            double ea = System.Math.Exp(a - max);
            double eb = System.Math.Exp(b - max);
            return (float)(ea / (ea + eb));
        }

        public static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        private static void InsertTopDescending(float[] top, ref int count, float value, int capacity)
        {
            if (count < capacity)
            {
                top[count++] = value;
                // simple insertion to keep partial order (small n)
                for (int i = count - 1; i > 0 && top[i] > top[i - 1]; i--)
                {
                    float tmp = top[i - 1]; top[i - 1] = top[i]; top[i] = tmp;
                }
            }
            else if (value > top[count - 1])
            {
                top[count - 1] = value;
                for (int i = count - 1; i > 0 && top[i] > top[i - 1]; i--)
                {
                    float tmp = top[i - 1]; top[i - 1] = top[i]; top[i] = tmp;
                }
            }
        }

        private static float Average(float[] arr, int count)
        {
            if (count <= 0) return 1f;
            float sum = 0f;
            for (int i = 0; i < count; i++) sum += arr[i];
            return sum / count;
        }
    }
}