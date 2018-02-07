using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ParameterManager allows to exchange information between different scenes.
/// </summary>
public class ParameterManager : MonoBehaviour {

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

    // Other data.
    [HideInInspector] public Quaternion BackgroundRotation { get; set; }

    void Awake() {
        ErrorCode = 0;

        DontDestroyOnLoad(transform.gameObject);
    }

    /* SUPPORT FUNCTIONS */

    // Menages errors going back to the main menu.
    public void ErrorBackToMenu(int errorCode) {
        ErrorCode = errorCode;
        SceneManager.LoadScene("Menu");
    }

    // Menages errors going back to the main menu.
    public void ErrorBackToMenu(string errorMessage) {
        ErrorCode = 1;
        ErrorMessage = errorMessage;
        SceneManager.LoadScene("Menu");
    }

}