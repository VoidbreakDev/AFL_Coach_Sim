using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Data;

namespace AFLCoachSim.Core.Engine.Match.Rotations
{
    public sealed class RotationManager
    {
        // Simple heuristics: rotate when on-field stint exceeds this
        private const int DefaultStintSeconds = 7 * 60; // ~7 minutes
        private const int MinStintSeconds = 4 * 60;
        private const int MaxBenchSeconds = 3 * 60;     // rest target

        public void MaybeRotate(
            IList<PlayerRuntime> onField, 
            IList<PlayerRuntime> bench, 
            TeamTactics tactics, 
            int dtSeconds)
        {
            // Target interchanges per game influences aggressiveness
            float aggressiveness = Clamp01((tactics.TargetInterchangesPerGame - 40) / 60f); // 0..1 around 40..100
            int stintThreshold = (int)(DefaultStintSeconds * (1f - 0.35f * aggressiveness));

            // Find the most fatigued eligible on-field player
            int swapOutIndex = -1;
            float worstScore = -1f;

            for (int i = 0; i < onField.Count; i++)
            {
                var pr = onField[i];
                if (pr.InjuredOut) continue;
                if (pr.SecondsSinceRotation < MinStintSeconds) continue;

                // score by low condition + long stint
                float score = (100 - pr.Player.Condition) + 0.05f * pr.SecondsSinceRotation;
                if (score > worstScore)
                {
                    worstScore = score;
                    swapOutIndex = i;
                }
            }

            if (swapOutIndex < 0) return;

            // find the best eligible bench replacement (highest condition)
            int swapInIndex = -1;
            int bestCond = -1;
            for (int j = 0; j < bench.Count; j++)
            {
                var br = bench[j];
                if (br.InjuredOut) continue;
                if (br.ReturnInSeconds > 0) continue; // still recovering from temp injury
                // prefer players that have rested at least some time
                if (br.SecondsSinceRotation < MaxBenchSeconds * 0.4f) continue;

                if (br.Player.Condition > bestCond)
                {
                    bestCond = br.Player.Condition;
                    swapInIndex = j;
                }
            }

            if (swapInIndex < 0) return;

            // Perform the swap
            var outPR = onField[swapOutIndex];
            var inPR  = bench[swapInIndex];

            outPR.OnField = false;
            outPR.SecondsSinceRotation = 0;

            inPR.OnField = true;
            inPR.SecondsSinceRotation = 0;

            // logical move: swap lists
            onField.RemoveAt(swapOutIndex);
            bench.RemoveAt(swapInIndex);
            bench.Add(outPR);
            onField.Add(inPR);
        }
        public bool MaybeRotate(
            IList<PlayerRuntime> onField,
            IList<PlayerRuntime> bench,
            TeamTactics tactics,
            int dtSeconds,
            out bool swappedThisTick)
        {
            swappedThisTick = false;

            float aggressiveness = Clamp01((tactics.TargetInterchangesPerGame - 40) / 60f);
            int stintThreshold = (int)(DefaultStintSeconds * (1f - 0.35f * aggressiveness));

            int swapOutIndex = -1;
            float worstScore = -1f;

            for (int i = 0; i < onField.Count; i++)
            {
                var pr = onField[i];
                if (pr.InjuredOut) continue;
                if (pr.SecondsSinceRotation < MinStintSeconds) continue;

                float score = (100 - pr.Player.Condition) + 0.05f * pr.SecondsSinceRotation;
                if (score > worstScore)
                {
                    worstScore = score;
                    swapOutIndex = i;
                }
            }

            if (swapOutIndex < 0) return false;

            int swapInIndex = -1;
            int bestCond = -1;
            for (int j = 0; j < bench.Count; j++)
            {
                var br = bench[j];
                if (br.InjuredOut) continue;
                if (br.ReturnInSeconds > 0) continue;
                if (br.SecondsSinceRotation < MaxBenchSeconds * 0.4f) continue;

                if (br.Player.Condition > bestCond)
                {
                    bestCond = br.Player.Condition;
                    swapInIndex = j;
                }
            }

            if (swapInIndex < 0) return false;

            var outPR = onField[swapOutIndex];
            var inPR  = bench[swapInIndex];

            outPR.OnField = false; outPR.SecondsSinceRotation = 0;
            inPR.OnField  = true;  inPR.SecondsSinceRotation  = 0;

            onField.RemoveAt(swapOutIndex);
            bench.RemoveAt(swapInIndex);
            bench.Add(outPR);
            onField.Add(inPR);

            swappedThisTick = true;
            return true;
        }
        private static float Clamp01(float v) { if (v < 0f) return 0f; if (v > 1f) return 1f; return v; }
    }
}