// Persistence/InMemorySeasonRepository.cs
using System.Collections.Generic;
using AFLCoachSim.Core.DTO;

namespace AFLCoachSim.Core.Persistence
{
    public sealed class InMemorySeasonRepository : ISeasonRepository
    {
        private readonly List<MatchResultDTO> _store = new();
        public void SaveResults(IEnumerable<MatchResultDTO> results)
        {
            _store.Clear(); _store.AddRange(results);
        }
        public IEnumerable<MatchResultDTO> LoadResults() => _store.ToArray();
    }
}