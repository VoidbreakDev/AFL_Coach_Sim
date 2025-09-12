using UnityEngine;

namespace AFLCoachSim.Core.Engine.Match.Tuning
{
    /// <summary>
    /// Central source of truth for tuning. In the Editor, reads from a ScriptableObject asset,
    /// otherwise falls back to MatchTuning.Default. Call Invalidate() after saving the asset.
    /// </summary>
    public static class MatchTuningProvider
    {
        public const string DefaultAssetPath = "Assets/Config/MatchTuning.asset";

        private static MatchTuning _cached;
#if UNITY_EDITOR
        private static MatchTuningSO _asset;
#endif

        /// <summary>
        /// Current tuning values. In Editor, pulls from asset (if present).
        /// In Player builds, returns the last cached (or Default).
        /// </summary>
        public static MatchTuning Current
        {
            get
            {
                if (_cached != null) return _cached;

#if UNITY_EDITOR
                // In Editor: try load the asset for live tweaks
                if (_asset == null)
                {
                    _asset = UnityEditor.AssetDatabase.LoadAssetAtPath<MatchTuningSO>(DefaultAssetPath);
                }
                if (_asset != null)
                {
                    _cached = _asset.ToRuntime();
                    return _cached;
                }
#endif
                // Fallback to baked default
                _cached = MatchTuning.Default;
                return _cached;
            }
        }

        /// <summary>
        /// Clears the cached runtime copy. Call after saving the asset so the next read reflects changes.
        /// </summary>
        public static void Invalidate()
        {
            _cached = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Loads the asset at the default path, creating it if missing.
        /// </summary>
        public static MatchTuningSO GetOrCreateAsset()
        {
            if (_asset == null)
            {
                _asset = UnityEditor.AssetDatabase.LoadAssetAtPath<MatchTuningSO>(DefaultAssetPath);
                if (_asset == null)
                {
                    var dir = System.IO.Path.GetDirectoryName(DefaultAssetPath);
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);

                    _asset = ScriptableObject.CreateInstance<MatchTuningSO>();
                    UnityEditor.AssetDatabase.CreateAsset(_asset, DefaultAssetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
            return _asset;
        }
#endif
    }
}