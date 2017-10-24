using System;
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
    [SerializeField] private GameObject settings;

    [Header("Singleplayer Fields")] [SerializeField] private GameObject mapSP;
    [SerializeField] private GameObject nextGenrationSP;
    [SerializeField] private GameObject previousGenerationSP;
    [SerializeField] private GameObject generationSP;
    [SerializeField] private GameObject inputSP;

    [Header("Multiplayer Fields")] [SerializeField] private GameObject mapMP;
    [SerializeField] private GameObject nextGenrationMP;
    [SerializeField] private GameObject previousGenerationMP;
    [SerializeField] private GameObject generationMP;
    [SerializeField] private GameObject inputMP;

    private int currentOption;
    private GameObject openSection;

    private int mapIndex;
    private int generationIndex;
    private bool exportData;
    private String mapDNA;

    public void Start() {
        openSection = main;
    }

    // Loads the rigth scene.
    public void LoadScene() {
        if (mapIndex < singleplayerScenes.GetLength(0))
            SceneManager.LoadScene(singleplayerScenes[mapIndex].sceneName);
        else if (mapIndex < singleplayerScenes.GetLength(0))
            SceneManager.LoadScene(multiplayerScenes[mapIndex - multiplayerScenes.GetLength(0)].sceneName);
        else if (mapIndex < singleplayerScenes.GetLength(0) - singleplayerScenes.GetLength(0) - multiplayerScenes.GetLength(0))
            SceneManager.LoadScene(otherScenes[mapIndex].sceneName);
    }

    // Quits the game.
    public void Exit() {
        Application.Quit();
    }

    // Opens the singleplayer menu.
    public void OpenSingleplayer() {
        OpenSection(singleplayer);
        ResetValues();
    }

    // Opens the multiplayer menu.
    public void OpenMultiplayer() {
        OpenSection(multiplayer);
        ResetValues();
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
        openSection.SetActive(false);
        section.SetActive(true);
        openSection = section;
    }

    // Resets all the indices.
    private void ResetValues() {
        mapIndex = 0;
        generationIndex = 0;
    }

    // Shows the next maps in the singleplayer menu.
    public void SingleplayerNextMap() {
        IncreaseMapIndex();
        mapSP.GetComponent<Text>().text = "Map: " + singleplayerScenes[mapIndex].fieldName;
        UpdateGenerationField(singleplayerScenes[mapIndex].isGenetic, inputSP, generationSP, nextGenrationSP, previousGenerationSP);
    }

    // Shows the previous maps in the singleplayer menu.
    public void SingleplayerPreviousMap() {
        DecreaseMapIndex();
        mapSP.GetComponent<Text>().text = "Map: " + singleplayerScenes[mapIndex].fieldName;
        UpdateGenerationField(singleplayerScenes[mapIndex].isGenetic, inputSP, generationSP, nextGenrationSP, previousGenerationSP);
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
        if (generationIndex > 2)
            generationIndex = 0;
    }

    // Decreases by one the circular current generation index.
    private void DecreaseGenerationIndex() {
        generationIndex--;
        if (generationIndex < 0)
            generationIndex = 2;
    }

    // Sets the map DNA.
    public void SetMapDNA(GameObject field) {
        mapDNA = field.GetComponent<InputField>().text;
    }

    // Sets the export data flag.
    public void SetExportData(GameObject toggle) {
        exportData = toggle.GetComponent<Toggle>().isOn;
    }

    [Serializable]
    private struct Scene {
        public string sceneName;
        public string fieldName;
        public bool isGenetic;
    }

}