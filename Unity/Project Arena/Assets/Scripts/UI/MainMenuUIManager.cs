using MapManipulation;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class manages the UI of the main menu. The main menu is composed by 3 sections: 
/// singleplayer, multiplayer and setting. Some buttons and options (quitButton and export) are hidden
/// or disabled in the web build.
/// </summary>
public class MainMenuUIManager : MonoBehaviour {

    [Header("Scenes")] [SerializeField] Mode[] singleplayerModes;
    [SerializeField] Mode[] multiplayerModes;

    [Header("UI sections")] [SerializeField] private GameObject main;
    [SerializeField] private GameObject singleplayer;
    [SerializeField] private GameObject multiplayer;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject error;
    [SerializeField] private GameObject loading;

    [Header("Main menu fields")] [SerializeField] private Button quitButton;
    [SerializeField] private Button experimentButton;

    [Header("Singleplayer fields")] [SerializeField] private GameObject nextModeSP;
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

    [Header("Multiplayer fields")] [SerializeField] private GameObject nextModeMP;
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

    [Header("Settings fields")] [SerializeField] private GameObject import;
    [SerializeField] private GameObject export;
    [SerializeField] private GameObject importButton;
    [SerializeField] private GameObject exportButton;
    [SerializeField] private Slider sensibilitySlider;

    [Header("Error fields")] [SerializeField] private Text errorText;

    [Header("Loading fields")] [SerializeField] private Text loadingText;

    [Header("Other")] [SerializeField] private RotateTranslateByAxis backgroundScript;

    private GameObject openedSection;

    private int currentMode = 0;
    private int currentMap = 0;
    private int currentGeneration = 0;
    private bool currentIsMultilevel = false;

    private bool exportData;
    private bool allowIO;
    private String mapDNA;
    private String currentScene;

    private String importPath;
    private String exportPath;

    public void Start() {
        Cursor.lockState = CursorLockMode.None;

        if (ParameterManager.HasInstance()) {
            if (ParameterManager.Instance.Version == ParameterManager.BuildVersion.COMPLETE) {
                experimentButton.interactable = true;
            }
            backgroundScript.SetRotation(ParameterManager.Instance.BackgroundRotation);
        }

        openedSection = main;

        if (ParameterManager.Instance.ErrorCode != 0) {
            OpenSection(error);
            SetErrorMessage(ParameterManager.Instance.ErrorCode,
                ParameterManager.Instance.ErrorMessage);
            ParameterManager.Instance.ErrorCode = 0;
        }

        exportSP.isOn = false;
        exportMP.isOn = false;

        if (Application.platform == RuntimePlatform.WebGLPlayer) {
            // Hide the quitButton button.
            quitButton.interactable = false;
            // Disable all the import/export.
            allowIO = false;
            exportSP.interactable = false;
            exportTextSP.color = exportSP.GetComponent<Toggle>().colors.disabledColor;
            exportMP.interactable = false;
            exportTextMP.color = exportMP.GetComponent<Toggle>().colors.disabledColor;
        } else {
            allowIO = true;
            importPath = Application.persistentDataPath + "/Import";
            exportPath = Application.persistentDataPath + "/Export";
            // Create the import directory if needed.
            if (!Directory.Exists(importPath)) {
                Directory.CreateDirectory(importPath);
            }
            // Create the export directory if needed.
            if (!Directory.Exists(exportPath)) {
                Directory.CreateDirectory(exportPath);
            }
        }

        // Get the mouse sensibility.
        if (PlayerPrefs.HasKey("MouseSensibility")) {
            sensibilitySlider.value = PlayerPrefs.GetFloat("MouseSensibility");
        } else {
            PlayerPrefs.SetFloat("MouseSensibility", sensibilitySlider.value);
        }

        if (Application.platform == RuntimePlatform.WindowsPlayer) {
            importButton.SetActive(true);
            exportButton.SetActive(true);
        } else {
            import.SetActive(true);
            export.SetActive(true);
            if (allowIO) {
                import.GetComponent<Text>().text = "Import folder: " + importPath;
                export.GetComponent<Text>().text = "Export folder: " + exportPath;
            } else {
                import.GetComponent<Text>().text = "The import folder is not available.";
                export.GetComponent<Text>().text = "The export folder is not available.";
            }
        }
    }

    // Starts the loading of the rigth scene.
    public void LoadScene() {
        if (currentGeneration == 2) {
            mapDNA = importPath + "/" + mapDNA;
        }

        ParameterManager.Instance.GenerationMode = currentGeneration;
        ParameterManager.Instance.MapDNA = mapDNA;
        ParameterManager.Instance.Export = exportData && allowIO;
        ParameterManager.Instance.ExportPath = exportPath;

        Loading();
    }

    // Loads a scene.
    public void LoadScene(string scene) {
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(scene);
    }

    // Shows the loading animation and loads.
    void Loading() {
        loadingText.text = "Loading";
        OpenSection(loading);
        if (currentGeneration == 2) {
            loadingText.text = "Validating the map";
            int errorCode = currentIsMultilevel ?
                MapValidate.ValidateMLMap(ParameterManager.Instance.MapDNA) :
                MapValidate.ValidateMap(ParameterManager.Instance.MapDNA);
            if (errorCode == 0) {
                Load(currentScene);
            } else {
                SetErrorMessage(errorCode, null);
                OpenSection(error);
            }
        } else if (currentGeneration == 3) {
            loadingText.text = "Validating the map";
            int errorCode = currentIsMultilevel ?
                MapValidate.ValidateGeneticMLMap(ParameterManager.Instance.MapDNA) :
                MapValidate.ValidateGeneticMap(ParameterManager.Instance.MapDNA);
            if (errorCode == 0) {
                Load(currentScene);
            } else {
                SetErrorMessage(errorCode, null);
                OpenSection(error);
            }
        } else {
            Load(currentScene);
        }
    }

    // Loads a scene.
    private void Load(string scene) {
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(scene);
    }

    // Quits the game.
    public void Exit() {
        Application.Quit();
    }

    // Opens the singleplayer menu.
    public void OpenSingleplayer() {
        exportSP.isOn = false;
        OpenSection(singleplayer);
        ResetValues();
        UpdateSingleplayerModes();
        ActivateCurrentModeSP();
    }

    // Opens the multiplayer menu.
    public void OpenMultiplayer() {
        exportMP.isOn = false;
        OpenSection(multiplayer);
        ResetValues();
        UpdateMultiplayerModes();
        ActivateCurrentModeMP();
    }

    // Opens the settings menu.
    public void OpenSettings() {
        OpenSection(settings);
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
        exportData = false;
    }

    // Sets the error message.
    private void SetErrorMessage(int errorCode, string errorMessage) {
        switch (errorCode) {
            case 1:
                errorText.text = errorMessage;
                break;
            case 2:
                errorText.text = "Error while loading the map.\nThe specified file was not " +
                    "found.\nPlease put the file in the rigth folder.";
                break;
            case 3:
                errorText.text = "Error while loading the map.\nThe map must be rectangular, " +
                    "with at least one spawn point and walls around its border.";
                break;
            case 4:
                errorText.text = "Error while loading the map.\nThe map exceeds the maximum " +
                    "dimension.";
                break;
            case 5:
                errorText.text = "Error while loading the map.\nThe genome doesn't follow the " +
                    "expected convention.";
                break;
            case 6:
                errorText.text = "Error while loading the map.\nEach level must be rectangular " +
                    "and have the same size, with at least one spawn point and walls around its " +
                    "border.";
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
        if (GetGeneratorsCount(singleplayerModes[currentMode].maps[currentMap].enabledGenerations)
            > 1) {
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
        currentMap = GetCiruclarIndex(currentMap, singleplayerModes[currentMode].maps.Length - 1,
            true);
        ActivateCurrentMapSP();
    }

    // Shows the previous maps in the singleplayer menu.
    public void SingleplayerPreviousMap() {
        currentMap = GetCiruclarIndex(currentMap, singleplayerModes[currentMode].maps.Length - 1,
            false);
        ActivateCurrentMapSP();
    }

    // Shows the next generation method in the singleplayer menu.
    public void SingleplayerNextGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration,
            singleplayerModes[currentMode].maps[currentMap].enabledGenerations, true);
        ActivateCurrentGenerationSP();
    }

    // Shows the previous generation method in the singleplayer menu.
    public void SingleplayerPreviousGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration,
            singleplayerModes[currentMode].maps[currentMap].enabledGenerations, false);
        ActivateCurrentGenerationSP();
    }

    private void ActivateCurrentModeSP() {
        modeTextSP.text = "Mode: " + singleplayerModes[currentMode].modeName;

        currentMap = 0;
        currentGeneration = GetMinGenerationIndex(singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations);
        exportSP.isOn = false;

        UpdateSingleplayerMaps();
        ActivateCurrentMapSP();
    }

    private void ActivateCurrentMapSP() {
        mapTextSP.text = "Map: " + singleplayerModes[currentMode].maps[currentMap].mapName;

        currentGeneration = GetMinGenerationIndex(singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations);
        currentIsMultilevel = singleplayerModes[currentMode].maps[currentMap].isMultilevel;

        UpdateSingleplayerGenerations();
        ActivateCurrentGenerationSP();
    }

    private void ActivateCurrentGenerationSP() {
        generationTextSP.text = "Generation: " + singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations[currentGeneration].generationName;
        currentScene = singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations[currentGeneration].scene;

        inputSP.interactable = singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations[currentGeneration].inputEnabled;
        inputSP.text = "";

        if (currentGeneration == 0) {
            inputSP.placeholder.GetComponent<Text>().text = GetSeed();
        } else {
            inputSP.placeholder.GetComponent<Text>().text = singleplayerModes[currentMode].
                maps[currentMap].enabledGenerations[currentGeneration].placeholder;
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
        if (GetGeneratorsCount(multiplayerModes[currentMode].maps[currentMap].enabledGenerations)
            > 1) {
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
        currentMap = GetCiruclarIndex(currentMap, multiplayerModes[currentMode].maps.Length - 1,
            true);
        ActivateCurrentMapMP();
    }

    // Shows the previous maps in the multiplayer menu.
    public void MultiplayerPreviousMap() {
        currentMap = GetCiruclarIndex(currentMap, multiplayerModes[currentMode].maps.Length - 1,
            false);
        ActivateCurrentMapMP();
    }

    // Shows the next generation method in the multiplayer menu.
    public void MultiplayerNextGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration,
            multiplayerModes[currentMode].maps[currentMap].enabledGenerations, true);
        ActivateCurrentGenerationMP();
    }

    // Shows the previous generation method in the multiplayer menu.
    public void MultiplayerPreviousGeneration() {
        currentGeneration = GetCiruclarIndex(currentGeneration, multiplayerModes[currentMode].
            maps[currentMap].enabledGenerations, false);
        ActivateCurrentGenerationMP();
    }

    private void ActivateCurrentModeMP() {
        modeTextMP.text = "Mode: " + multiplayerModes[currentMode].modeName;

        currentMap = 0;
        currentGeneration = GetMinGenerationIndex(multiplayerModes[currentMode].maps[currentMap].
            enabledGenerations);
        exportMP.isOn = false;

        UpdateMultiplayerMaps();
        ActivateCurrentMapMP();
    }

    private void ActivateCurrentMapMP() {
        mapTextMP.text = "Map: " + multiplayerModes[currentMode].maps[currentMap].mapName;

        currentGeneration = GetMinGenerationIndex(multiplayerModes[currentMode].maps[currentMap].
            enabledGenerations);
        currentIsMultilevel = multiplayerModes[currentMode].maps[currentMap].isMultilevel;

        UpdateMultiplayerGenerations();
        ActivateCurrentGenerationMP();
    }

    private void ActivateCurrentGenerationMP() {
        generationTextMP.text = "Generation: " + singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations[currentGeneration].generationName;
        currentScene = singleplayerModes[currentMode].maps[currentMap].
            enabledGenerations[currentGeneration].scene;

        inputMP.interactable = singleplayerModes[currentMode].maps[currentMap]
            .enabledGenerations[currentGeneration].inputEnabled;
        inputMP.text = "";

        if (currentGeneration == 0) {
            inputMP.placeholder.GetComponent<Text>().text = GetSeed();
        } else {
            inputMP.placeholder.GetComponent<Text>().text = singleplayerModes[currentMode].
                maps[currentMap].enabledGenerations[currentGeneration].placeholder;
        }
    }

    /* HELPERS */

    // Returns the previous or the next circular index.
    private int GetCiruclarIndex(int current, int max, bool next) {
        if (next) {
            if (current == max) {
                return 0;
            } else {
                return current + 1;
            }
        } else {
            if (current == 0) {
                return max;
            } else {
                return current - 1;
            }
        }
    }

    // Returns the previous or the next circular index.
    private int GetCiruclarIndex(int current, Generation[] mask, bool next) {
        if (next) {
            for (int i = current; i < mask.Length; i++) {
                if (mask[i].enabled && i != current) {
                    return i;
                }
            }
            for (int i = 0; i < current; i++) {
                if (mask[i].enabled && i != current) {
                    return i;
                }
            }
        } else {
            for (int i = current; i >= 0; i--) {
                if (mask[i].enabled && i != current) {
                    return i;
                }
            }
            for (int i = mask.Length - 1; i > current; i--) {
                if (mask[i].enabled && i != current) {
                    return i;
                }
            }
        }

        return current;
    }

    private int GetMinGenerationIndex(Generation[] mask) {
        int i = 0;

        foreach (Generation g in mask) {
            if (g.enabled) {
                break;
            } else {
                i++;
            }
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
        mapDNA = field.GetComponent<InputField>().text;
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
    private void ShowExplorer(string path) {
        path = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
    }

    // Returns the number of enables generators.
    private int GetGeneratorsCount(Generation[] generators) {
        int activeCount = 0;

        foreach (Generation g in generators)
            if (g.enabled)
                activeCount++;

        return activeCount;
    }

    // Sets the mouse sensibility in the preferences.
    public void SetMouseSensibility() {
        PlayerPrefs.SetFloat("MouseSensibility", sensibilitySlider.value);
    }

    [Serializable]
    private struct Mode {
        public string modeName;
        public Map[] maps;
    }

    [Serializable]
    private struct Map {
        public string mapName;
        public bool isMultilevel;
        public Generation[] enabledGenerations;
    }

    [Serializable]
    private struct Generation {
        public bool enabled;
        public bool inputEnabled;
        public string generationName;
        public string placeholder;
        public string scene;
    }

}