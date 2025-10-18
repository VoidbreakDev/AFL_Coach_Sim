using System;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Persistence;

namespace AFLCoachSim.Core.Injuries
{
    /// <summary>
    /// Stub service for injury operations to resolve compilation errors
    /// </summary>
    public class InjuryService
    {
        private readonly IInjuryRepository _repository;
        
        public InjuryService(IInjuryRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        
        // Stub methods - implement as needed
        public void RecordInjury(Injury injury)
        {
            _repository.SaveInjury(injury);
        }
        
        public Injury GetInjury(InjuryId id)
        {
            return _repository.LoadInjury(id);
        }
    }
}