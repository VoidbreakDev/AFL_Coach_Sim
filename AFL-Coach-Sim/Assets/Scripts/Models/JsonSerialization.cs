// File: Assets/Scripts/Models/JsonSerialization.cs
using UnityEngine;

namespace AFLManager.Models
{
    public static class JsonSerialization
    {
        /// <summary>
        /// Serializes any object to a JSON string using Unity’s JsonUtility.
        /// </summary>
        public static string ToJson<T>(T obj, bool prettyPrint = false)
        {
            return JsonUtility.ToJson(obj, prettyPrint);
        }

        /// <summary>
        /// Deserializes a JSON string back into an object of type T using JsonUtility.
        /// </summary>
        public static T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
