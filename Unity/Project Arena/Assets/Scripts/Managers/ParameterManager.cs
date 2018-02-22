using UnityEngine;

/// <summary>
/// ParameterManager allows to exchange information between different scenes.
/// </summary>
public class ParameterManager : SceneSingleton<ParameterManager> {

    public enum BuildVersion { COMPLETE, GAME_ONLY, EXPERIMENT_CONTROL, EXPERIMENT_ONLY };

    // Map data.
    [HideInInspector] public int GenerationMode { get; set; }
    [HideInInspector] public string MapDNA { get; set; }
    [HideInInspector] public bool Flip { get; set; }

    // Export data.
    [HideInInspector] public bool Export { get; set; }
    [HideInInspector] public string ExportPath { get; set; }

    // Error data.
    [HideInInspector] public int ErrorCode { get; set; }
    [HideInInspector] public string ErrorMessage { get; set; }

    // Experiment data.
    [HideInInspector] public bool LogOnline { get; set; }
    [HideInInspector] public bool LogOffline { get; set; }
    [HideInInspector] public bool LogSetted { get; set; }
    [HideInInspector] public string ExperimentControlScene { get; set; }

    // Other data.
    [HideInInspector] public Quaternion BackgroundRotation { get; set; }
    [HideInInspector] public BuildVersion Version { get; set; }
    [HideInInspector] public string InitialScene { get; set; }

    void Awake() {
        ErrorCode = 0;

        LogOnline = true;
        LogOffline = true;
        LogSetted = false;

        DontDestroyOnLoad(transform.gameObject);
    }

}