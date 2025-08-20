using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Selection
{
    public static class AutoSelector
    {
        // Naive: pick best 22 by WorkRate + Decision + Kicking + Marking; next 4 = bench.
        public static void Select22(List<Player> roster, TeamId teamId, List<Player> onField, List<Player> bench)
        {
            onField.Clear(); bench.Clear();
            if (roster == null || roster.Count == 0) return;

            IEnumerable<Player> ordered = roster
                .OrderByDescending(p => p.Attr.WorkRate + p.Attr.DecisionMaking + p.Attr.Kicking + p.Attr.Marking);

            foreach (var p in ordered.Take(22)) onField.Add(p);
            foreach (var p in ordered.Skip(22).Take(4)) bench.Add(p);
        }
    }
}