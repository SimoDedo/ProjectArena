using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapManager : CoreComponent {

    [Header("Core components")] [SerializeField] private GameObject mapAssembler;
    [SerializeField] private GameObject mapGenerator;
    [SerializeField] private GameObject objectDisplacer;

    // Do I have to load the map from a .txt?
    [Header("Import")] [SerializeField] protected bool loadMapFromFile = false;
    // Path of the map to be laoded.
    [SerializeField] protected string textFilePath = null;

    // Size of a square.
    [Header("Building")] [SerializeField] private float squareSize;
    // Heigth of the map.
    [SerializeField] private float heigth;
    // Category of the spawn point gameobjects in the object displacer. 
    [SerializeField] private string spawnPointCategory;

    private MapGenerator mapGeneratorScript;
    private MapAssebler mapAssemblerScript;
    private ObjectDisplacer objectDisplacerScript;

    private string seed;
    private bool export;
    private string exportPath;

    private char[,] map;
    
    private void Start() {
        ExtractParametersFromContainer();

        mapAssemblerScript = mapAssembler.GetComponent<MapAssebler>();
        mapGeneratorScript = mapGenerator.GetComponent<MapGenerator>();
        objectDisplacerScript = objectDisplacer.GetComponent<ObjectDisplacer>();
    }

    private void Update() {
        if (!IsReady() && mapAssemblerScript.IsReady() && mapGeneratorScript.IsReady()
            && objectDisplacerScript.IsReady()) {
            SetReady(true);
        }
    }

    public void ManageMap(bool assembleMap) {
        if (loadMapFromFile) {
            // Load the map.
            LoadMapFromText();
        } else {
            // Generate the map.
            if (GetParameterContainer() != null)
                map = mapGeneratorScript.GenerateMap(seed, export, exportPath);
            else
                map = mapGeneratorScript.GenerateMap();
        }

        if (assembleMap) {
            // Assemble the map.
            mapAssemblerScript.AssembleMap(map, mapGeneratorScript.GetWallChar(), mapGeneratorScript.GetRoomChar(), squareSize, heigth);
            // Displace the objects.
            objectDisplacerScript.DisplaceObjects(map, squareSize, heigth);
        }
    }

    // Returns the spawn points.
    public List<GameObject> GetSpawnPoints() {
        return objectDisplacerScript.GetObjectsByCategory(spawnPointCategory);
    }

    // Loads the map from a text file.
    protected void LoadMapFromText() {
        if (textFilePath == null && !Directory.Exists(textFilePath)) {
            Debug.LogError("Error while retrieving the folder, please insert a valid path.");
        } else {
            try {
                string[] lines = System.IO.File.ReadAllLines(@textFilePath);

                map = new char[lines[0].Length, lines.GetLength(0)];

                for (int x = 0; x < map.GetLength(0); x++) {
                    for (int y = 0; y < map.GetLength(1); y++) {
                        map[x, y] = lines[y][x];
                    }
                }
            } catch (Exception) {
                Debug.LogError("Error while loading the map, the supplied file is not valid.");
            }
        }
    }

    // Extracts the parameters from the parameter container, if any.
    private void ExtractParametersFromContainer() {
        if (GetParameterContainer() != null) {
            export = GetParameterContainer().GetExport();
            exportPath = GetParameterContainer().GetExportPath();

            switch (GetParameterContainer().GetGenerationMode()) {
                case 0:
                    loadMapFromFile = false;
                    seed = GetParameterContainer().GetMapDNA();
                    break;
                case 1:
                    loadMapFromFile = false;
                    seed = GetParameterContainer().GetMapDNA();
                    break;
                case 2:
                    seed = null;
                    loadMapFromFile = true;
                    textFilePath = GetParameterContainer().GetMapDNA();
                    break;
            }
        }
    }

    // Returns the parameter container.
    protected ParameterContainer GetParameterContainer() {
        if (GameObject.Find("Parameter Container") != null)
            return GameObject.Find("Parameter Container").GetComponent<ParameterContainer>();
        else
            return null;
    }

}