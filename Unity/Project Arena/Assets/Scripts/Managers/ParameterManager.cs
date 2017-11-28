using UnityEngine;
using UnityEngine.SceneManagement;

public class ParameterManager : MonoBehaviour {

    // Map data.
    private int generationMode;
    private string mapDNA;

    // Export data.
    private bool export;
    private string exportPath;

    // Error data.
    private int errorCode = 0;
    private string errorMessage;

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    /* SUPPORT FUNCTIONS */

    // Menages errors going back to the main menu.
    public void ErrorBackToMenu(int errorCode) {
        SetErrorCode(errorCode);
        SceneManager.LoadScene("Menu");
        Cursor.lockState = CursorLockMode.None;
    }

    // Menages errors going back to the main menu.
    public void ErrorBackToMenu(string errorMessage) {
        SetErrorCode(1);
        SetErrorMessage(errorMessage);
        SceneManager.LoadScene("Menu");
        Cursor.lockState = CursorLockMode.None;
    }


    /* GETTERS AND SETTERS */

    public void SetGenerationMode(int gm) {
        generationMode = gm;
    }

    public int GetGenerationMode() {
        return generationMode;
    }

    public void SetMapDNA(string mDNA) {
        mapDNA = mDNA;
    }

    public string GetMapDNA() {
        return mapDNA;
    }

    public void SetExport(bool e) {
        export = e;
    }

    public bool GetExport() {
        return export;
    }

    public void SetExportPath(string ep) {
        exportPath = ep;
    }

    public string GetExportPath() {
        return exportPath;
    }

    public void SetErrorCode(int code) {
        errorCode = code;
    }

    public int GetErrorCode() {
        return errorCode;
    }

    public void SetErrorMessage(string em) {
        errorMessage = em;
    }

    public string GetErrorMessage() {
        return errorMessage;
    }

}