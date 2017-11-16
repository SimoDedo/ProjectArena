using UnityEngine;
using UnityEngine.SceneManagement;

public class ParameterManager : MonoBehaviour {

    // Parameters to tranfert data between scenes.
    private int generationMode;
    private string mapDNA;
    private bool export;
    private string exportPath;
    private int errorCode = 0;

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    /* SUPPORT FUNCTIONS */

    // Menages errors going back to the main menu.
    public void ErrorBackToMenu(int errorCode) {
        SetErrorCode(errorCode);
        SceneManager.LoadScene("Menu");
    }

    /* GETTERS AND SETTERS */

    public int GetGenerationMode() {
        return generationMode;
    }

    public void SetGenerationMode(int gm) {
        generationMode = gm;
    }

    public void SetMapDNA(string mDNA) {
        mapDNA = mDNA;
    }

    public string GetMapDNA() {
        return mapDNA;
    }

    public bool GetExport() {
        return export;
    }

    public void SetExport(bool e) {
        export = e;
    }

    public void SetExportPath(string ep) {
        exportPath = ep;
    }

    public string GetExportPath() {
        return exportPath;
    }

    public int GetErrorCode() {
        return errorCode;
    }

    public void SetErrorCode(int code) {
        errorCode = code;
    }

}