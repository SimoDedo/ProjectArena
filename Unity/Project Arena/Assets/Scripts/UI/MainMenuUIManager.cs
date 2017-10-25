using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour {

    [Header("Scenes")] [SerializeField] Scene[] singleplayerScenes;
    [SerializeField] Scene[] multiplayerScenes;
    [SerializeField] Scene[] otherScenes;

    [Header("UI Sections")] [SerializeField] private GameObject main;
    [SerializeField] private GameObject singleplayer;
    [SerializeField] private GameObject multiplayer;
    [SerializeField] private GameObject about;

    [Header("Singleplayer Fields")] [SerializeField] private GameObject mapSP;
    [SerializeField] private GameObject nextGenerationSP;
    [SerializeField] private GameObject previousGenerationSP;
    [SerializeField] private GameObject generationSP;
    [SerializeField] private GameObject inputSP;
    [SerializeField] private GameObject exportSP;
    [SerializeField] private GameObject exportTextSP;

    [Header("Multiplayer Fields")] [SerializeField] private GameObject mapMP;
    [SerializeField] private GameObject nextGenerationMP;
    [SerializeField] private GameObject previousGenerationMP;
    [SerializeField] private GameObject generationMP;
    [SerializeField] private GameObject inputMP;
    [SerializeField] private GameObject exportMP;
    [SerializeField] private GameObject exportTextMP;

    [Header("About Fields")] [SerializeField] private GameObject import;
    [SerializeField] private GameObject export;
    [SerializeField] private GameObject importButton;
    [SerializeField] private GameObject exportButton;

    [Header("Other")] [SerializeField] private ParameterContainer parameterContainer;

    private int currentOption;
    private GameObject openSection;

    private int mapIndex;
    private int sceneIndex;
    private int generationIndex;
    private bool exportData;
    private String mapDNA;

    private bool allowIO;

    public void Start() { 
        openSection = main;

        if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer) {
            allowIO = true;
        } else {
            allowIO = false;
            exportSP.GetComponent<Toggle>().isOn = false;
            exportSP.GetComponent<Toggle>().interactable = false;
            exportTextSP.GetComponent<Text>().color = exportSP.GetComponent<Toggle>().colors.disabledColor;
            exportMP.GetComponent<Toggle>().isOn = false;
            exportMP.GetComponent<Toggle>().interactable = false;
            exportTextMP.GetComponent<Text>().color = exportMP.GetComponent<Toggle>().colors.disabledColor;
        }

        // Create the import directory if needed.
        if (!Directory.Exists(Application.persistentDataPath + "/Import")) {
            Directory.CreateDirectory(Application.persistentDataPath + "/Import");
        }
        // Create the export directory if needed.
        if (!Directory.Exists(Application.persistentDataPath + "/Export")) {
            Directory.CreateDirectory(Application.persistentDataPath + "/Export");
        }

        if (Application.platform == RuntimePlatform.WindowsPlayer) {
            importButton.SetActive(true);
            exportButton.SetActive(true);
        } else {
            import.SetActive(true);
            export.SetActive(true);
            import.GetComponent<Text>().text = "Import folder: " + Application.persistentDataPath + "/Import";
            export.GetComponent<Text>().text = "Export folder: " + Application.persistentDataPath + "/Export";
        }
    }

    // Loads the rigth scene.
    public void LoadScene() {
        parameterContainer.SetGenerationMode(generationIndex);
        parameterContainer.SetMapDNA(mapDNA);
        parameterContainer.SetExport(exportData && allowIO);
        parameterContainer.SetExportPath(Application.persistentDataPath + "/Export");

        if (sceneIndex < singleplayerScenes.GetLength(0))
            SceneManager.LoadScene(singleplayerScenes[sceneIndex].sceneName);
        else if (sceneIndex < singleplayerScenes.GetLength(0) + multiplayerScenes.GetLength(0))
            SceneManager.LoadScene(multiplayerScenes[sceneIndex - singleplayerScenes.GetLength(0)].sceneName);
        else
            SceneManager.LoadScene(otherScenes[sceneIndex - singleplayerScenes.GetLength(0) - multiplayerScenes.GetLength(0)].sceneName);
    }

    // Quits the game.
    public void Exit() {
        Application.Quit();
    }

    // Opens the singleplayer menu.
    public void OpenSingleplayer() {
        OpenSection(singleplayer);
        ResetValues();
        UpdateGenerationField(singleplayerScenes[mapIndex].isGenetic, inputSP, generationSP, nextGenerationSP, previousGenerationSP);
    }

    // Opens the multiplayer menu.
    public void OpenMultiplayer() {
        OpenSection(multiplayer);
        ResetValues();
        UpdateGenerationField(multiplayerScenes[mapIndex].isGenetic, inputMP, generationMP, nextGenerationMP, previousGenerationMP);
    }

    // Opens the about menu.
    public void OpenAbout() {
        OpenSection(about);
    }

    // Opens the main menu.
    public void OpenMain() {
        OpenSection(main);
    }

    // Opens a menu.
    private void OpenSection(GameObject section) {
        openSection.SetActive(false);
        section.SetActive(true);
        openSection = section;
    }

    // Resets all the indices.
    private void ResetValues() {
        mapIndex = 0;
        generationIndex = 0;
        exportData = true;
    }

    // Shows the next maps in the singleplayer menu.
    public void SingleplayerNextMap() {
        IncreaseMapIndex();
        mapSP.GetComponent<Text>().text = "Map: " + singleplayerScenes[mapIndex].fieldName;
        UpdateGenerationField(singleplayerScenes[mapIndex].isGenetic, inputSP, generationSP, nextGenerationSP, previousGenerationSP);
        sceneIndex = mapIndex;
    }

    // Shows the previous maps in the singleplayer menu.
    public void SingleplayerPreviousMap() {
        DecreaseMapIndex();
        mapSP.GetComponent<Text>().text = "Map: " + singleplayerScenes[mapIndex].fieldName;
        UpdateGenerationField(singleplayerScenes[mapIndex].isGenetic, inputSP, generationSP, nextGenerationSP, previousGenerationSP);
        sceneIndex = mapIndex;
    }

    // Shows the next maps in the multiplayer menu.
    public void MultiplayerNextMap() {
        IncreaseMapIndex();
        mapMP.GetComponent<Text>().text = "Map: " + singleplayerScenes[mapIndex].fieldName;
        UpdateGenerationField(multiplayerScenes[mapIndex].isGenetic, inputMP, generationMP, nextGenerationMP, previousGenerationMP);
        sceneIndex = mapIndex + singleplayerScenes.GetLength(0);
    }

    // Shows the previous maps in the multiplayer menu.
    public void MultiplayerPreviousMap() {
        DecreaseMapIndex();
        mapMP.GetComponent<Text>().text = "Map: " + singleplayerScenes[mapIndex].fieldName;
        UpdateGenerationField(multiplayerScenes[mapIndex].isGenetic, inputMP, generationMP, nextGenerationMP, previousGenerationMP);
        sceneIndex = mapIndex + singleplayerScenes.GetLength(0);
    }

    // Increases by one the circular current map index.
    private void IncreaseMapIndex() {
        mapIndex++;
        if (mapIndex > singleplayerScenes.GetLength(0) - 1)
            mapIndex = 0;
    }

    // Decreases by one the circular current map index.
    private void DecreaseMapIndex() {
        mapIndex--;
        if (mapIndex < 0)
            mapIndex = singleplayerScenes.GetLength(0) - 1;
    }

    // Sets the generation field depending if the map is genetic or not.
    private void UpdateGenerationField(bool isGenetic, GameObject input, GameObject generation, GameObject next, GameObject previous) {
        if (isGenetic) {
            next.GetComponent<Button>().interactable = false;
            previous.GetComponent<Button>().interactable = false;
            generationIndex = -1;
            input.GetComponent<InputField>().interactable = true;
            generation.GetComponent<Text>().text = "Generation: genetic";
            input.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "Insert genome...";
            input.GetComponent<InputField>().text = "";
        } else {
            next.GetComponent<Button>().interactable = true;
            previous.GetComponent<Button>().interactable = true;
            generationIndex = 0;
            input.GetComponent<InputField>().interactable = false;
            generation.GetComponent<Text>().text = "Generation: random";
            input.GetComponent<InputField>().placeholder.GetComponent<Text>().text = GetSeed();
            input.GetComponent<InputField>().text = "";
        }
    }

    // Gets a seed and stores it.
    private string GetSeed() {
        mapDNA = System.DateTime.Now.GetHashCode().ToString();
        return mapDNA;
    }

    // Shows the next generation method in the singleplayer menu.
    public void SingleplayerNextGeneration() {
        IncreaseGenerationIndex();
        UpdateGeneration(inputSP, generationSP);
    }

    // Shows the previous generation method in the singleplayer menu.
    public void SingleplayerPreviousGeneration() {
        DecreaseGenerationIndex();
        UpdateGeneration(inputSP, generationSP);
    }

    // Shows the next generation method in the multiplayer menu.
    public void MultiplayerNextGeneration() {
        IncreaseGenerationIndex();
        UpdateGeneration(inputMP, generationMP);
    }

    // Shows the previous generation method in the multiplayer menu.
    public void MultiplayerPreviousGeneration() {
        DecreaseGenerationIndex();
        UpdateGeneration(inputMP, generationMP);
    }

    // Updates the generation mode depending on the current index.
    private void UpdateGeneration(GameObject input, GameObject generation) {
        switch (generationIndex) {
            case 0:
                input.GetComponent<InputField>().interactable = false;
                generation.GetComponent<Text>().text = "Generation: random";
                input.GetComponent<InputField>().placeholder.GetComponent<Text>().text = GetSeed();
                input.GetComponent<InputField>().text = "";
                break;
            case 1:
                input.GetComponent<InputField>().interactable = true;
                generation.GetComponent<Text>().text = "Generation: seed";
                input.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "Insert seed...";
                input.GetComponent<InputField>().text = "";
                break;
            case 2:
                input.GetComponent<InputField>().interactable = true;
                generation.GetComponent<Text>().text = "Generation: file";
                input.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "Insert file name...";
                input.GetComponent<InputField>().text = "";
                break;
        }
    }

    // Increases by one the circular current generation index.
    private void IncreaseGenerationIndex() {
        generationIndex++;
        if (!allowIO && generationIndex > 1)
            generationIndex = 0;
        else if (generationIndex > 2)
            generationIndex = 0;
    }

    // Decreases by one the circular current generation index.
    private void DecreaseGenerationIndex() {
        generationIndex--;
        if (generationIndex < 0) {
            if (!allowIO)
                generationIndex = 1;
            else
                generationIndex = 2;
        }
    }

    // Sets the map DNA.
    public void SetMapDNA(GameObject field) {
        mapDNA = field.GetComponent<InputField>().text;
    }

    // Sets the export data flag.
    public void SetExportData(GameObject toggle) {
        exportData = toggle.GetComponent<Toggle>().isOn;
    }

    // Opens the import data fodler.
    public void OpenImportFolder() {
        ShowExplorer(Application.persistentDataPath + "/Import");
    }

    // Opens the export data fodler.
    public void OpenExportFolder() {
        ShowExplorer(Application.persistentDataPath + "/Export");
    }

    // Opens an explorer in Windows.
    public void ShowExplorer(string path) {
        path = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
    }

    [Serializable]
    private struct Scene {
        public string sceneName;
        public string fieldName;
        public bool isGenetic;
    }

}