using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Simulation;

namespace AFLCoachSim.Core.Engine.Match.Selection
{
    /// <summary>
    /// Position-aware player selection for different match phases and situations
    /// PERFORMANCE: Optimized versions use manual loops and pre-allocated buffers to avoid LINQ allocations
    /// </summary>
    public static class PositionalSelector
    {
        // Thread-local buffers to avoid allocations during rating calculations
        [System.ThreadStatic]
        private static PlayerRuntime[] _tempBuffer;

        [System.ThreadStatic]
        private static float[] _tempRatings;

        private const int MaxPlayersOnField = 22;

        private static void EnsureBuffersAllocated()
        {
            if (_tempBuffer == null)
            {
                _tempBuffer = new PlayerRuntime[MaxPlayersOnField];
                _tempRatings = new float[MaxPlayersOnField];
            }
        }
        /// <summary>
        /// Selects players likely to be involved in center bounces (midfielders + ruckmen)
        /// </summary>
        public static List<Player> GetCenterBounceParticipants(List<Player> onField, DeterministicRandom rng, int count = 5)
        {
            if (onField == null || onField.Count == 0)
                return new List<Player>();

            var centerBounceGroup = PositionUtils.GetCenterBounceGroup(onField, p => p.PrimaryRole);
            
            // If we don't have enough specialists, include some general players
            if (centerBounceGroup.Count < count)
            {
                var others = onField.Except(centerBounceGroup)
                    .OrderByDescending(p => p.Attr.Clearance + p.Attr.Strength)
                    .Take(count - centerBounceGroup.Count);
                centerBounceGroup.AddRange(others);
            }

            // Select best available for center bounce work
            return centerBounceGroup
                .OrderByDescending(p => GetCenterBounceRating(p))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Selects players likely to be involved in Inside50 entries (forwards + attacking mids)
        /// </summary>
        public static List<Player> GetInside50Participants(List<Player> onField, DeterministicRandom rng, int count = 6)
        {
            if (onField == null || onField.Count == 0)
                return new List<Player>();

            var inside50Group = PositionUtils.GetInside50Group(onField, p => p.PrimaryRole);
            
            // If we don't have enough specialists, include some general players
            if (inside50Group.Count < count)
            {
                var others = onField.Except(inside50Group)
                    .OrderByDescending(p => p.Attr.Marking + p.Attr.Kicking)
                    .Take(count - inside50Group.Count);
                inside50Group.AddRange(others);
            }

            // Select best available for forward work
            return inside50Group
                .OrderByDescending(p => GetInside50Rating(p))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Selects players likely to be involved in defensive rebounds (defenders + defensive mids)
        /// </summary>
        public static List<Player> GetDefensiveParticipants(List<Player> onField, DeterministicRandom rng, int count = 6)
        {
            if (onField == null || onField.Count == 0)
                return new List<Player>();

            var defensiveGroup = PositionUtils.GetDefensiveGroup(onField, p => p.PrimaryRole);
            
            // If we don't have enough specialists, include some general players
            if (defensiveGroup.Count < count)
            {
                var others = onField.Except(defensiveGroup)
                    .OrderByDescending(p => p.Attr.Tackling + p.Attr.Positioning)
                    .Take(count - defensiveGroup.Count);
                defensiveGroup.AddRange(others);
            }

            // Select best available for defensive work
            return defensiveGroup
                .OrderByDescending(p => GetDefensiveRating(p))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets a single player most likely to be involved in a specific event
        /// </summary>
        public static Player GetPrimaryEventPlayer(List<Player> onField, MatchEventType eventType, DeterministicRandom rng)
        {
            if (onField == null || onField.Count == 0)
                return null;

            List<Player> candidates;
            switch (eventType)
            {
                case MatchEventType.CenterBounceWin:
                case MatchEventType.Clearance:
                    candidates = GetCenterBounceParticipants(onField, rng, 3);
                    break;

                case MatchEventType.Goal:
                case MatchEventType.Behind:
                case MatchEventType.Inside50Entry:
                case MatchEventType.Mark:
                case MatchEventType.SpectacularMark:
                    candidates = GetInside50Participants(onField, rng, 4);
                    break;

                case MatchEventType.Rebound50:
                case MatchEventType.Tackle:
                    candidates = GetDefensiveParticipants(onField, rng, 4);
                    break;

                case MatchEventType.Kick:
                case MatchEventType.Handball:
                    // General play - any player but weight by phase appropriateness
                    candidates = onField.OrderByDescending(p => 
                        p.Attr.Kicking + p.Attr.DecisionMaking + p.Attr.WorkRate).Take(8).ToList();
                    break;

                default:
                    candidates = onField.Take(5).ToList();
                    break;
            }

            return candidates.Count > 0 ? candidates[rng.NextInt(0, candidates.Count)] : onField[0];
        }

        /// <summary>
        /// Runtime version - selects from PlayerRuntime objects
        /// </summary>
        public static List<PlayerRuntime> GetCenterBounceParticipants(IList<PlayerRuntime> onField, DeterministicRandom rng, int count = 5)
        {
            if (onField == null || onField.Count == 0)
                return new List<PlayerRuntime>();

            var centerBounceGroup = PositionUtils.GetCenterBounceGroup(onField, p => p.Player.PrimaryRole);
            
            if (centerBounceGroup.Count < count)
            {
                var others = onField.Except(centerBounceGroup)
                    .OrderByDescending(p => (p.Player.Attr.Clearance + p.Player.Attr.Strength) * p.FatigueMult * p.InjuryMult)
                    .Take(count - centerBounceGroup.Count);
                centerBounceGroup.AddRange(others);
            }

            return centerBounceGroup
                .OrderByDescending(p => GetCenterBounceRating(p.Player) * p.FatigueMult * p.InjuryMult)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Runtime version for Inside50 participants
        /// </summary>
        public static List<PlayerRuntime> GetInside50Participants(IList<PlayerRuntime> onField, DeterministicRandom rng, int count = 6)
        {
            if (onField == null || onField.Count == 0)
                return new List<PlayerRuntime>();

            var inside50Group = PositionUtils.GetInside50Group(onField, p => p.Player.PrimaryRole);
            
            if (inside50Group.Count < count)
            {
                var others = onField.Except(inside50Group)
                    .OrderByDescending(p => (p.Player.Attr.Marking + p.Player.Attr.Kicking) * p.FatigueMult * p.InjuryMult)
                    .Take(count - inside50Group.Count);
                inside50Group.AddRange(others);
            }

            return inside50Group
                .OrderByDescending(p => GetInside50Rating(p.Player) * p.FatigueMult * p.InjuryMult)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Runtime version for defensive participants
        /// </summary>
        public static List<PlayerRuntime> GetDefensiveParticipants(IList<PlayerRuntime> onField, DeterministicRandom rng, int count = 6)
        {
            if (onField == null || onField.Count == 0)
                return new List<PlayerRuntime>();

            var defensiveGroup = PositionUtils.GetDefensiveGroup(onField, p => p.Player.PrimaryRole);
            
            if (defensiveGroup.Count < count)
            {
                var others = onField.Except(defensiveGroup)
                    .OrderByDescending(p => (p.Player.Attr.Tackling + p.Player.Attr.Positioning) * p.FatigueMult * p.InjuryMult)
                    .Take(count - defensiveGroup.Count);
                defensiveGroup.AddRange(others);
            }

            return defensiveGroup
                .OrderByDescending(p => GetDefensiveRating(p.Player) * p.FatigueMult * p.InjuryMult)
                .Take(count)
                .ToList();
        }

        // Private rating helpers
        private static float GetCenterBounceRating(Player player)
        {
            return 0.45f * player.Attr.Clearance + 0.25f * player.Attr.Strength
                 + 0.15f * player.Attr.Positioning + 0.15f * player.Attr.DecisionMaking;
        }

        private static float GetInside50Rating(Player player)
        {
            return 0.5f * player.Attr.Marking + 0.3f * player.Attr.Kicking + 0.2f * player.Attr.DecisionMaking;
        }

        private static float GetDefensiveRating(Player player)
        {
            return 0.5f * player.Attr.Tackling + 0.3f * player.Attr.Positioning + 0.2f * player.Attr.WorkRate;
        }

        // ========================================================================
        // OPTIMIZED METHODS - Zero allocation versions for hot paths
        // ========================================================================

        /// <summary>
        /// OPTIMIZED: Calculates average center bounce rating without allocations
        /// Used in hot path (called every match tick)
        /// </summary>
        public static float GetCenterBounceRatingOptimized(IList<PlayerRuntime> onField, DeterministicRandom rng)
        {
            if (onField == null || onField.Count == 0) return 1f;

            EnsureBuffersAllocated();

            // Step 1: Filter players suitable for center bounce work and calculate ratings
            int suitableCount = 0;
            for (int i = 0; i < onField.Count; i++)
            {
                var pr = onField[i];
                var role = pr.Player.PrimaryRole;

                // Only midfielders and ruckmen
                if (PositionUtils.IsMidfielder(role) || PositionUtils.IsRuckman(role))
                {
                    _tempBuffer[suitableCount] = pr;
                    var a = pr.Player.Attr;
                    _tempRatings[suitableCount] = (0.45f * a.Clearance + 0.25f * a.Strength
                                                   + 0.15f * a.Positioning + 0.15f * a.DecisionMaking)
                                                   * pr.FatigueMult * pr.InjuryMult;
                    suitableCount++;
                }
            }

            // Step 2: If we don't have enough specialists, fill with best available
            if (suitableCount < 5)
            {
                for (int i = 0; i < onField.Count && suitableCount < 5; i++)
                {
                    var pr = onField[i];
                    var role = pr.Player.PrimaryRole;

                    // Skip if already included
                    if (PositionUtils.IsMidfielder(role) || PositionUtils.IsRuckman(role))
                        continue;

                    _tempBuffer[suitableCount] = pr;
                    var a = pr.Player.Attr;
                    _tempRatings[suitableCount] = (0.45f * a.Clearance + 0.25f * a.Strength
                                                   + 0.15f * a.Positioning + 0.15f * a.DecisionMaking)
                                                   * pr.FatigueMult * pr.InjuryMult;
                    suitableCount++;
                }
            }

            // Step 3: Select top 5 by rating using partial sort (no allocation)
            int topN = suitableCount < 5 ? suitableCount : 5;
            PartialSortDescending(_tempRatings, suitableCount, topN);

            // Step 4: Calculate average rating
            float sum = 0f;
            for (int i = 0; i < topN; i++)
            {
                sum += _tempRatings[i];
            }

            return sum / topN;
        }

        /// <summary>
        /// OPTIMIZED: Calculates average Inside50 rating without allocations
        /// Used in hot path (called every match tick)
        /// </summary>
        public static float GetInside50RatingOptimized(IList<PlayerRuntime> onField, DeterministicRandom rng)
        {
            if (onField == null || onField.Count == 0) return 1f;

            EnsureBuffersAllocated();

            // Step 1: Filter forwards and attacking mids, calculate ratings
            int suitableCount = 0;
            for (int i = 0; i < onField.Count; i++)
            {
                var pr = onField[i];
                var role = pr.Player.PrimaryRole;

                if (PositionUtils.IsForward(role) || PositionUtils.IsMidfielder(role))
                {
                    _tempBuffer[suitableCount] = pr;
                    var a = pr.Player.Attr;
                    _tempRatings[suitableCount] = (0.5f * a.Marking + 0.3f * a.Kicking + 0.2f * a.DecisionMaking)
                                                   * pr.FatigueMult * pr.InjuryMult;
                    suitableCount++;
                }
            }

            // Step 2: Fill with best available if needed
            if (suitableCount < 6)
            {
                for (int i = 0; i < onField.Count && suitableCount < 6; i++)
                {
                    var pr = onField[i];
                    var role = pr.Player.PrimaryRole;

                    if (PositionUtils.IsForward(role) || PositionUtils.IsMidfielder(role))
                        continue;

                    _tempBuffer[suitableCount] = pr;
                    var a = pr.Player.Attr;
                    _tempRatings[suitableCount] = (0.5f * a.Marking + 0.3f * a.Kicking + 0.2f * a.DecisionMaking)
                                                   * pr.FatigueMult * pr.InjuryMult;
                    suitableCount++;
                }
            }

            // Step 3: Select top 6 by rating
            int topN = suitableCount < 6 ? suitableCount : 6;
            PartialSortDescending(_tempRatings, suitableCount, topN);

            // Step 4: Calculate average
            float sum = 0f;
            for (int i = 0; i < topN; i++)
            {
                sum += _tempRatings[i];
            }

            return sum / topN;
        }

        /// <summary>
        /// OPTIMIZED: Calculates average defensive rating without allocations
        /// Used in hot path (called every match tick)
        /// </summary>
        public static float GetDefenseRatingOptimized(IList<PlayerRuntime> onField, DeterministicRandom rng)
        {
            if (onField == null || onField.Count == 0) return 1f;

            EnsureBuffersAllocated();

            // Step 1: Filter defenders and defensive mids, calculate ratings
            int suitableCount = 0;
            for (int i = 0; i < onField.Count; i++)
            {
                var pr = onField[i];
                var role = pr.Player.PrimaryRole;

                if (PositionUtils.IsDefender(role) || PositionUtils.IsMidfielder(role))
                {
                    _tempBuffer[suitableCount] = pr;
                    var a = pr.Player.Attr;
                    _tempRatings[suitableCount] = (0.5f * a.Tackling + 0.3f * a.Positioning + 0.2f * a.WorkRate)
                                                   * pr.FatigueMult * pr.InjuryMult;
                    suitableCount++;
                }
            }

            // Step 2: Fill with best available if needed
            if (suitableCount < 6)
            {
                for (int i = 0; i < onField.Count && suitableCount < 6; i++)
                {
                    var pr = onField[i];
                    var role = pr.Player.PrimaryRole;

                    if (PositionUtils.IsDefender(role) || PositionUtils.IsMidfielder(role))
                        continue;

                    _tempBuffer[suitableCount] = pr;
                    var a = pr.Player.Attr;
                    _tempRatings[suitableCount] = (0.5f * a.Tackling + 0.3f * a.Positioning + 0.2f * a.WorkRate)
                                                   * pr.FatigueMult * pr.InjuryMult;
                    suitableCount++;
                }
            }

            // Step 3: Select top 6 by rating
            int topN = suitableCount < 6 ? suitableCount : 6;
            PartialSortDescending(_tempRatings, suitableCount, topN);

            // Step 4: Calculate average
            float sum = 0f;
            for (int i = 0; i < topN; i++)
            {
                sum += _tempRatings[i];
            }

            return sum / topN;
        }

        /// <summary>
        /// Partial sort: ensures the top N elements are in descending order
        /// Uses selection sort for top N elements (efficient for small N)
        /// ZERO allocations
        /// </summary>
        private static void PartialSortDescending(float[] ratings, int count, int topN)
        {
            for (int i = 0; i < topN && i < count; i++)
            {
                // Find max in remaining elements
                int maxIdx = i;
                for (int j = i + 1; j < count; j++)
                {
                    if (ratings[j] > ratings[maxIdx])
                        maxIdx = j;
                }

                // Swap if needed
                if (maxIdx != i)
                {
                    float temp = ratings[i];
                    ratings[i] = ratings[maxIdx];
                    ratings[maxIdx] = temp;
                }
            }
        }
    }
}
