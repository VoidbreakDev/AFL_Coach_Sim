using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Selection
{
    public static class AutoSelector
    {
        /// <summary>
        /// Selects 22 players with balanced positional structure, plus 4 for bench
        /// </summary>
        public static void Select22(List<Player> roster, TeamId teamId, List<Player> onField, List<Player> bench)
        {
            onField.Clear(); bench.Clear();
            if (roster == null || roster.Count == 0) return;

            // Try position-aware selection first
            if (TrySelectByPosition(roster, onField, bench))
                return;

            // Fallback to old method if roster doesn't have enough positional variety
            IEnumerable<Player> ordered = roster
                .OrderByDescending(p => p.Attr.WorkRate + p.Attr.DecisionMaking + p.Attr.Kicking + p.Attr.Marking);

            foreach (var p in ordered.Take(22)) onField.Add(p);
            foreach (var p in ordered.Skip(22).Take(4)) bench.Add(p);
        }

        /// <summary>
        /// Attempts to select a balanced team by position. Returns false if roster lacks positional variety.
        /// </summary>
        private static bool TrySelectByPosition(List<Player> roster, List<Player> onField, List<Player> bench)
        {
            var structure = PositionUtils.GetIdealStructure();
            var selected = new List<Player>();
            var remaining = new List<Player>(roster);

            // Select by position group with some flexibility
            selected.AddRange(SelectBestByPosition(remaining, PositionGroup.Defense, structure.Defenders - 1, structure.Defenders + 1));
            selected.AddRange(SelectBestByPosition(remaining, PositionGroup.Midfield, structure.Midfielders - 1, structure.Midfielders + 1));
            selected.AddRange(SelectBestByPosition(remaining, PositionGroup.Forward, structure.Forwards - 1, structure.Forwards + 1));
            selected.AddRange(SelectBestByPosition(remaining, PositionGroup.Ruck, 1, structure.Ruckmen));

            // Fill remaining spots with best available
            var stillNeeded = 22 - selected.Count;
            if (stillNeeded > 0)
            {
                var available = remaining.Except(selected)
                    .OrderByDescending(GetOverallRating)
                    .Take(stillNeeded);
                selected.AddRange(available);
            }

            // Must have at least 22 players total
            if (selected.Count < 22)
                return false;

            // Take top 22 for field, next 4 for bench
            var finalSelected = selected.OrderByDescending(GetOverallRating).ToList();
            onField.AddRange(finalSelected.Take(22));
            
            var benchCandidates = remaining.Except(finalSelected)
                .OrderByDescending(GetOverallRating)
                .Take(4);
            bench.AddRange(benchCandidates);
            
            return true;
        }

        /// <summary>
        /// Selects best players from a specific position group
        /// </summary>
        private static List<Player> SelectBestByPosition(List<Player> roster, PositionGroup group, int min, int max)
        {
            var candidates = PositionUtils.GetByPositionGroup(roster, group, p => p.PrimaryRole);
            var count = System.Math.Max(min, System.Math.Min(max, candidates.Count));
            
            return candidates
                .OrderByDescending(p => GetPositionalRating(p, group))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets overall player rating for selection priority
        /// </summary>
        private static float GetOverallRating(Player player)
        {
            return player.Attr.WorkRate + player.Attr.DecisionMaking + player.Attr.Kicking + player.Attr.Marking;
        }

        /// <summary>
        /// Gets position-specific rating for a player
        /// </summary>
        private static float GetPositionalRating(Player player, PositionGroup group)
        {
            switch (group)
            {
                case PositionGroup.Defense:
                    return player.Attr.Tackling + player.Attr.Positioning + player.Attr.Marking + player.Attr.Kicking;
                    
                case PositionGroup.Midfield:
                    return player.Attr.Clearance + player.Attr.WorkRate + player.Attr.DecisionMaking + player.Attr.Speed;
                    
                case PositionGroup.Forward:
                    return player.Attr.Marking + player.Attr.Kicking + player.Attr.Speed + player.Attr.Agility;
                    
                case PositionGroup.Ruck:
                    return player.Attr.Strength + player.Attr.Clearance + player.Attr.Marking + player.Attr.RuckWork;
                    
                default:
                    return GetOverallRating(player);
            }
        }
    }
}