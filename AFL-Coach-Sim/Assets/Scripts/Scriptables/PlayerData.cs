// File: Assets/Scripts/Scriptables/PlayerData.cs
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Scriptables
{
    [CreateAssetMenu(
        fileName = "NewPlayerData",
        menuName = "AFL Manager/Player Data",
        order = 1)]
    public class PlayerData : ScriptableObject
    {
        public string Name;
        public int Age;
        public string State;
        [TextArea]
        public string History;
        public PlayerRole Role;
        public PlayerStats Stats;
        public int PotentialCeiling;
        public ContractDetails Contract;
        public float Morale;
        public float Stamina;

        public Player ToModel()
        {
            var player = new Player
            {
                Name = Name,
                Age = Age,
                State = State,
                History = History,
                Role = Role,
                PotentialCeiling = PotentialCeiling,
                Morale = Morale,
                Stamina = Stamina,
                Contract = Contract
            };
            player.Stats = Stats;
            return player;
        }
    }
}