using System.Collections.Generic;
using AFLCoachSim.Core.Training;
using AFLCoachSim.Core.DTO;

namespace AFLCoachSim.Core.Persistence
{
    /// <summary>
    /// Repository interface for training data persistence operations
    /// </summary>
    public interface ITrainingRepository
    {
        // === Development Potential ===
        void SavePlayerPotential(int playerId, DevelopmentPotential potential);
        DevelopmentPotential LoadPlayerPotential(int playerId);
        void SaveAllPlayerPotentials(IDictionary<int, DevelopmentPotential> potentials);
        IDictionary<int, DevelopmentPotential> LoadAllPlayerPotentials();

        // === Training Enrollments ===
        void SavePlayerEnrollment(PlayerTrainingEnrollment enrollment);
        void SavePlayerEnrollments(int playerId, IEnumerable<PlayerTrainingEnrollment> enrollments);
        IEnumerable<PlayerTrainingEnrollment> LoadPlayerEnrollments(int playerId);
        IDictionary<int, IList<PlayerTrainingEnrollment>> LoadAllEnrollments();
        void RemovePlayerEnrollment(int playerId, string programId);

        // === Training Sessions ===
        void SaveTrainingSession(TrainingSession session);
        void SaveCompletedSessions(IEnumerable<TrainingSession> sessions);
        void SaveScheduledSessions(IEnumerable<TrainingSession> sessions);
        IEnumerable<TrainingSession> LoadCompletedSessions();
        IEnumerable<TrainingSession> LoadScheduledSessions();
        TrainingSession LoadTrainingSession(string sessionId);

        // === Training Efficiency ===
        void SavePlayerEfficiency(int playerId, IEnumerable<TrainingEfficiency> efficiencies);
        IEnumerable<TrainingEfficiency> LoadPlayerEfficiencies(int playerId);
        IDictionary<int, IList<TrainingEfficiency>> LoadAllEfficiencies();

        // === Bulk Operations ===
        void SaveAllTrainingData(TrainingDataDTO trainingData);
        TrainingDataDTO LoadAllTrainingData();
        void ClearAllTrainingData();
        void ClearPlayerTrainingData(int playerId);

        // === Maintenance Operations ===
        bool HasTrainingData();
        void BackupTrainingData(string backupSuffix);
        bool RestoreTrainingData(string backupSuffix);
    }
}