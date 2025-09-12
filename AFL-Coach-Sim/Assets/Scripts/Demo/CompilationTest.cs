// File: Assets/Scripts/Demo/CompilationTest.cs
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems;

namespace AFLManager.Demo
{
    /// <summary>
    /// Simple test to verify compilation works for the enhanced skills system
    /// </summary>
    public class CompilationTest : MonoBehaviour
    {
        [ContextMenu("Test Compilation")]
        private void TestCompilation()
        {
            Debug.Log("=== Compilation Test ===");

            // Test all the methods that were causing compilation errors
            var testPlayer = new Player
            {
                Name = "Test Player",
                Role = PlayerRole.Centre,
                Stats = new PlayerStats { Kicking = 80 }
            };

            // Test method calls that were failing before
            try
            {
                // These should now compile successfully
                var coreRole = PlayerSkillsAdapter.MapUnityRoleToCoreRole(PlayerRole.Centre);
                Debug.Log($"Mapped role: {coreRole}");

                var stats = PlayerSkillsAdapter.ConvertAttributesToPlayerStats(new AFLCoachSim.Core.Domain.Entities.Attributes { Kicking = 85 });
                Debug.Log($"Converted stats - Kicking: {stats.Kicking}");

                var rating = EnhancedPlayerOperations.CalculateOverallRating(testPlayer);
                Debug.Log($"Overall rating: {rating}");

                var skillValue = EnhancedPlayerOperations.GetPlayerSkill(testPlayer, "kicking");
                Debug.Log($"Kicking skill: {skillValue}");

                Debug.Log("✅ All compilation tests PASSED!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Compilation test FAILED: {e.Message}");
            }
        }
    }
}
