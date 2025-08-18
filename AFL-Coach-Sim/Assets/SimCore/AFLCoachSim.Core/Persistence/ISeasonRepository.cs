// Persistence/ISeasonRepository.cs
using System.Collections.Generic;
using AFLCoachSim.Core.DTO;

namespace AFLCoachSim.Core.Persistence
{
    public interface ISeasonRepository
    {
        void SaveResults(IEnumerable<MatchResultDTO> results);
        IEnumerable<MatchResultDTO> LoadResults();
    }
}