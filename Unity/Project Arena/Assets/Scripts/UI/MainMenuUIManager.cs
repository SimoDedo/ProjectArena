using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour {

    [Header("Scenes")] [SerializeField] Mode[] singleplayerModes;
    [SerializeField] Mode[] multiplayerModes;

    [Header("UI Sections")] [SerializeField] private GameObject main;
    [SerializeField] private GameObject singleplayer;
    [SerializeField] private GameObject multiplayer;
    [SerializeField] private GameObject about;
    [SerializeField] private GameObject error;
    [SerializeField] private GameObject loading;

    [Header("Singleplayer Fields")] [SerializeField] private GameObject nextModeSP;
    [SerializeField] private GameObject previousModeSP;
    [SerializeField] private GameObject nextMapSP;
    [SerializeField] private GameObject previousMapSP;
    [SerializeField] private GameObject nextGenerationSP;
    [SerializeField] private GameObject previousGenerationSP;
    [SerializeField] private Text generationTextSP;
    [SerializeField] private Text modeTextSP;
    [SerializeField] private Text exportTextSP;
    [SerializeField] private Text mapTextSP;
    [SerializeField] private InputField inputSP;
    [SerializeField] private Toggle exportSP;

    [Header("Multiplayer Fields")] [SerializeField] private GameObject nextModeMP;
    [SerializeField] private GameObject previousModeMP;
    [SerializeField] private GameObject nextMapMP;
    [SerializeField] private GameObject previousMapMP;
    [SerializeField] private GameObject nextGenerationMP;
    [SerializeField] private GameObject previousGenerationMP;
    [SerializeField] private Text generationTextMP;
    [SerializeField] private Text modeTextMP;
    [SerializeField] private Text exportTextMP;
    [SerializeField] private Text mapTextMP;
    [SerializeField] private InputField inputMP;
    [SerializeField] private Toggle exportMP;

    [Header("About Fields")] [SerializeField] private GameObject import;
    [SerializeField] private GameObject export;
    [SerializeField] private GameObject importButton;
    [SerializeField] private GameObject exportButton;

    [Header("Error Fields")] [SerializeField] private Text errorText;

    [Header("Loading Fields")] [SerializeField] private Text loadingText;

    [Header("Other")] [SerializeField] private GameObject parameterManagerPrefab;
    [SerializeField] private MapValidator mapValidator;

    private GameObject openedSection;

    private int currentMode = 0;
    private int currentMap = 0;
    private int currentGeneration = 0;

    private bool exportData;
    private bool allowIO;
    private String mapDNA;
    private String currentScene;

    private String importPath;
    private String exportPath;

    private ParameterManager parameterManagerScript;

    public void Start() {
        if (!GameObject.Find("Parameter Manager"))
            InstantiateParameterManager();
        else
            parameterManagerScript = GameObject.Find("Parameter Manager").GetComponent<ParameterManager>();

        openedSection = main;

        if (parameterManagerScript.GetErrorCode() != 0) {
            OpenSection(error);
            SetErrorMessage(parameterManagerScript.GetErrorCode(), parameterManagerScript.GetErrorMessage());
            parameterManagerScript.SetErrorCode(0);
        }

        if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer) {
            allowIO = true;
            importPath = Application.persistentDataPath + "/Import";
            exportPath = Application.persistentDataPath + "/Export";
            // Create the import directory if needed.
            if (!Directory.Exists(importPath))
                Directory.CreateDirectory(importPath);
            // Create the export directory if needed.
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);
        } else {
            allowIO = false;
            exportSP.isOn = false;
            exportSP.interactable = false;
            exportTextSP.color = exportSP.GetComponent<Toggle>().colors.disabledColor;
            exportMP.isOn = false;
            exportMP.interactable = false;
            exportTextMP.color = exportMP.GetComponent<Toggle>().colors.disabledColor;
        }

        if (Application.platform == RuntimePlatform.WindowsPlayer) {
            importButton.SetActive(true);
            exportButton.SetActive(true);
        } else {
            import.SetActive(true);
            export.SetActive(true);
            import.GetComponent<Text>().text = "Import folder: " + importPath;
            export.GetComponent<Text>().text = "Export folder: " + exportPath;
        }
    }

    // Starts the loading of the rigth scene.
    public void LoadScene() {
        parameterManagerScript.SetGenerationMode(currentGeneration);
        parameterManagerScript.SetMapDNA(mapDNA);
        parameterManagerScript.SetExport(exportData && allowIO);
        parameterManagerScript.SetExportPath(exportPath);

        Loading();
    }

    // Shows the loading animation and loads.
    void Loading() {
        loadingText.text = "Loading";
        OpenSection(loading);
        if (currentGeneration == 2) {
            loadingText.text = "Validating the map";
            int errorCode = mapValidator.ValidateMap(parameterManagerScript.GetMapDNA());
            if (errorCode == 0) {
                SceneManager.LoadScene(currentScene);
            } else {
                SetErrorMessage(errorCode, null);
                OpenSection(error);
            }
        } else if (currentGeneration == 3) {
            loadingText.text = "Validating the map";
            int errorCode = mapValidator.ValidateGeneticMap(parameterManagerScript.GetMapDNA());
            if (errorCode == 0) {
                SetErrorMessage(1, "Everything went fine, time to implement the generation.");
                OpenSection(error);
            } else {
                SetErrorMessage(errorCode, null);
                OpenSection(error);
            }
        } else
            SceneManager.LoadScene(currentScene);
    }

    // Quits the game.
    public void Exit() {
        Application.Quit();
    }

    // Opens the singleplayer menu.
    public void OpenSingleplayer() {
        OpenSection(singleplayer);
        ResetValues();
        UpdateSingleplayerModes();
        ActivateCurrentModeSP();
    }

    // Opens the multiplayer menu.
    public void OpenMultiplayer() {
        OpenSection(multiplayer);
        ResetValues();
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
        openedSection.SetActive(false);
        section.SetActive(true);
        openedSection = section;
    }

    // Resets all the indices.
    private void ResetValues() {
        currentMode = 0;
        currentMap = 0;
        currentGeneration = 0;
        exportData = true;
    }

    // Sets the error message.
    private void SetErrorMessage(int errorCode, string errorMessage) {
        switch (errorCode) {
            case 1:
                errorText.text = errorMessage;
                break;
            case 2:
                errorText.text = "Error while loading the map.\nThe specified file was not found.\nPlease put the file in the rigth folder.";
                break;
            case 3:
                errorText.text = "Error while loading the map.\nThe map must be rectangular, with at least one spawn point and walls around its border.";
                break;
            case 4:
                errorText.text = "Error while loading the map.\nThe map exceeds the maximum dimension.";
                break;
            case 5:
                errorText.text = "Error while loading the map.\nThe genome doesn't follow the expected convention.";
                break;
            default:
                errorText.text = "Something really bad just happened.";
                break;
        }
    }

    /* SINGLEPLAYER */

    // Enables or disbales the change mode buttons
    private void UpdateSingleplayerModes() {
        if (singleplayerModes.Length > 1) {
            previousModeSP.GetComponent<Button>().interactable = true;
            nextModeSP.GetComponent<Button>().interactable = true;
        } else {
            previousModeSP.GetComponent<Button>().interactable = false;
            nextModeSP.GetComponent<Button>().interactable = false;
        }
    }

    // Enables or disbales the change map buttons
    private void UpdateSingleplayerMaps() {
        if (singleplayerModes[currentMode].maps.Length > 1) {
            previousMapSP.GetComponent<Button>().interactable = true;
            nextMapSP.GetComponent<Button>().interactable = true;
        } else {
            previousMapSP.GetComponent<Button>().interactable = false;
            nextMapSP.GetComponent<Button>().interactable = false;
        }
    }

    // Enables or disbales the change generation buttons
    private void UpdateSingleplayerGenerations() {
        if (singleplayerModes[currentMode].maps[currentMap].enabledGenerations.Length > 1) {
            previousGenerationSP.GetComponent<Button>().interactable = true;
            nextGenerationSP.GetComponent<Button>().interactable = true;
        } else {
            previousGenerationSP.GetComponent<Button>().interactable = false;
            nextGenerationSP.GetComponent<Button>().interactable = false;
        }
    }

    // Shows the next mode in the singleplayer menu.
    public void SingleplayerNextMode() {
        currentMode = GetCiruclarIndex(currentMode, singleplayerModes.Length - 1, true);
        ActivateCurrentModeSP();
    }

    // Shows the previous mode in the singleplayer menu.
    public void SingleplayerPreviousMode() {
        currentMode = GetCiruclarIndex(currentMode, singleplayerModes.Length - 1, false);
        ActivateCurrentModeSP();
    }

    // Shows the next maps in the singleplayer menu.
    public void SingleplayerNextMap() {
        currentMap = GetCiruclarIndex(currentMap, singleplayerModes[currentMode].maps.Length - 1, true);
        ActivateCurrentMapSP();
    }

    // Shows the previous maps in the singleplayer menu.
    public void SingleplayerPreviousMap() {
        currentMap = GetCiruclarIndex(currentMap, singleplayerModes[currentMode].maps.Length - 1, false);
        ActivateCurrentMapSP();
    }

    // Shows the next generation method in the singleplayer menu.
    public void SingleplayerNextGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration, singleplayerModes[currentMode].maps[currentMap].enabledGenerations, true);
        ActivateCurrentGenerationSP();
    }

    // Shows the previous generation method in the singleplayer menu.
    public void SingleplayerPreviousGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration, singleplayerModes[currentMode].maps[currentMap].enabledGenerations, false);
        ActivateCurrentGenerationSP();
    }

    private void ActivateCurrentModeSP() {
        modeTextSP.text = singleplayerModes[currentMode].modeName;

        currentMap = 0;
        currentGeneration = GetMinGenerationIndex(singleplayerModes[currentMode].maps[currentMap].enabledGenerations);

        UpdateSingleplayerMaps();
        ActivateCurrentMapSP();
    }

    private void ActivateCurrentMapSP() {
        mapTextSP.text = singleplayerModes[currentMode].maps[currentMap].mapName;

        currentGeneration = GetMinGenerationIndex(singleplayerModes[currentMode].maps[currentMap].enabledGenerations);

        UpdateSingleplayerGenerations();
        ActivateCurrentGenerationSP();
    }

    private void ActivateCurrentGenerationSP() {
        generationTextSP.text = singleplayerModes[currentMode].maps[currentMap].enabledGenerations[currentGeneration].generationName;
        currentScene = singleplayerModes[currentMode].maps[currentMap].enabledGenerations[currentGeneration].scene;

        if (currentGeneration == 0) {
            inputSP.interactable = false;
            inputSP.text = "";
            inputSP.placeholder.GetComponent<Text>().text = GetSeed();
        } else {
            inputSP.interactable = true;
            inputSP.text = "";
            inputSP.placeholder.GetComponent<Text>().text = singleplayerModes[currentMode].maps[currentMap].enabledGenerations[currentGeneration].placeholder;
        }
    }

    /* MULTIPLAYER */

    // Enables or disbales the change mode buttons
    private void UpdateMultiplayerModes() {
        if (multiplayerModes.Length > 1) {
            previousModeMP.GetComponent<Button>().interactable = true;
            nextModeMP.GetComponent<Button>().interactable = true;
        } else {
            previousModeMP.GetComponent<Button>().interactable = false;
            nextModeMP.GetComponent<Button>().interactable = false;
        }
    }

    // Enables or disbales the change map buttons
    private void UpdateMultiplayerMaps() {
        if (multiplayerModes[currentMode].maps.Length > 1) {
            previousMapMP.GetComponent<Button>().interactable = true;
            nextMapMP.GetComponent<Button>().interactable = true;
        } else {
            previousMapMP.GetComponent<Button>().interactable = false;
            nextMapMP.GetComponent<Button>().interactable = false;
        }
    }

    // Enables or disbales the change generation buttons
    private void UpdateMultiplayerGenerations() {
        if (multiplayerModes[currentMode].maps[currentMap].enabledGenerations.Length > 1) {
            previousGenerationMP.GetComponent<Button>().interactable = true;
            nextGenerationMP.GetComponent<Button>().interactable = true;
        } else {
            previousGenerationMP.GetComponent<Button>().interactable = false;
            nextGenerationMP.GetComponent<Button>().interactable = false;
        }
    }

    // Shows the next mode in the multiplayer menu.
    public void MultiplayerNextMode() {
        currentMode = GetCiruclarIndex(currentMode, multiplayerModes.Length - 1, true);
        ActivateCurrentModeMP();
    }

    // Shows the previous mode in the multiplayer menu.
    public void MultiplayerPreviousMode() {
        currentMode = GetCiruclarIndex(currentMode, multiplayerModes.Length - 1, false);
        ActivateCurrentModeMP();
    }

    // Shows the next maps in the multiplayer menu.
    public void MultiplayerNextMap() {
        currentMap = GetCiruclarIndex(currentMap, multiplayerModes[currentMode].maps.Length - 1, true);
        ActivateCurrentMapMP();
    }

    // Shows the previous maps in the multiplayer menu.
    public void MultiplayerPreviousMap() {
        currentMap = GetCiruclarIndex(currentMap, multiplayerModes[currentMode].maps.Length - 1, false);
        ActivateCurrentMapMP();
    }

    // Shows the next generation method in the multiplayer menu.
    public void MultiplayerNextGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration, multiplayerModes[currentMode].maps[currentMap].enabledGenerations, true);
        ActivateCurrentGenerationMP();
    }

    // Shows the previous generation method in the multiplayer menu.
    public void MultiplayerPreviousGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration, multiplayerModes[currentMode].maps[currentMap].enabledGenerations, false);
        ActivateCurrentGenerationMP();
    }

    private void ActivateCurrentModeMP() {
        modeTextMP.text = multiplayerModes[currentMode].modeName;

        currentMap = 0;
        currentGeneration = GetMinGenerationIndex(multiplayerModes[currentMode].maps[currentMap].enabledGenerations);

        UpdateMultiplayerMaps();
        ActivateCurrentMapMP();
    }

    private void ActivateCurrentMapMP() {
        mapTextMP.text = multiplayerModes[currentMode].maps[currentMap].mapName;

        currentGeneration = GetMinGenerationIndex(multiplayerModes[currentMode].maps[currentMap].enabledGenerations);

        UpdateMultiplayerGenerations();
        ActivateCurrentGenerationMP();
    }

    private void ActivateCurrentGenerationMP() {
        generationTextMP.text = multiplayerModes[currentMode].maps[currentMap].enabledGenerations[currentGeneration].generationName;
        currentScene = multiplayerModes[currentMode].maps[currentMap].enabledGenerations[currentGeneration].scene;

        if (currentGeneration == 0) {
            inputMP.interactable = false;
            inputMP.text = "";
            inputMP.placeholder.GetComponent<Text>().text = GetSeed();
        } else {
            inputMP.interactable = true;
            inputMP.text = "";
            inputMP.placeholder.GetComponent<Text>().text = multiplayerModes[currentMode].maps[currentMap].enabledGenerations[currentGeneration].placeholder;
        }
    }

    /* HELPERS */

    // Returns the previous or the next circular index.
    private int GetCiruclarIndex(int current, int max, bool next) {
        if (next) {
            if (current == max)
                return 0;
            else
                return current + 1;
        } else {
            if (current == 0)
                return max;
            else
                return current - 1;
        }

    }

    // Returns the previous or the next circular index.
    private int GetCiruclarIndex(int current, Generation[] mask, bool next) {
        if (next) {
            for (int i = current; i < mask.Length; i++)
                if (mask[i].enabled && i != current)
                    return i;
            for (int i = 0; i < current; i++)
                if (mask[i].enabled && i != current)
                    return i;
        } else {
            for (int i = current; i >= 0; i--)
                if (mask[i].enabled && i != current)
                    return i;
            for (int i = mask.Length - 1; i > current; i--)
                if (mask[i].enabled && i != current)
                    return i;
        }

        return current;
    }

    private int GetMinGenerationIndex(Generation[] mask) {
        int i = 0;

        foreach (Generation g in mask) {
            if (g.enabled)
                break;
            else
                i++;
        }

        return i;
    }

    // Gets a seed and stores it.
    private string GetSeed() {
        mapDNA = System.DateTime.Now.GetHashCode().ToString();
        return mapDNA;
    }

    // Sets the map DNA.
    public void SetMapDNA(GameObject field) {
        mapDNA = importPath + "/" + field.GetComponent<InputField>().text;
    }

    // Sets the export data flag.
    public void SetExportData(GameObject toggle) {
        exportData = toggle.GetComponent<Toggle>().isOn;
    }

    // Opens the import data fodler.
    public void OpenImportFolder() {
        ShowExplorer(importPath);
    }

    // Opens the export data fodler.
    public void OpenExportFolder() {
        ShowExplorer(exportPath);
    }

    // Opens an explorer in Windows.
    public void ShowExplorer(string path) {
        path = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
    }

    // Instantiates the Parameter Manager.
    private void InstantiateParameterManager() {
        GameObject instance = Instantiate(parameterManagerPrefab);
        instance.name = parameterManagerPrefab.name;
        parameterManagerScript = instance.GetComponent<ParameterManager>();
    }

    [Serializable]
    private struct Mode {
        public string modeName;
        public Map[] maps;
    }

    [Serializable]
    private struct Map {
        public string mapName;
        public Generation[] enabledGenerations;
    }

    [Serializable]
    private struct Generation {
        public bool enabled;
        public string generationName;
        public string placeholder;
        public string scene;
    }

}