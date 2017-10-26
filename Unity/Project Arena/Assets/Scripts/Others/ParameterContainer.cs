using UnityEngine;

public class ParameterContainer : MonoBehaviour {

    private int generationMode;
    private string mapDNA;
    private bool export;
    private string exportPath;

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

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

}