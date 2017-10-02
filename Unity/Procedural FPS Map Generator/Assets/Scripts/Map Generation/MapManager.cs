using System;
using System.IO;
using UnityEngine;

public class MapManager : MonoBehaviour {

    [SerializeField] private GameObject mapAssembler;
    [SerializeField] private GameObject mapGenerator;
    [SerializeField] private GameObject objectDisplacer;
    [SerializeField] private GameObject spawnPointManager;

    // Do I have to load the map from a .txt?
    [SerializeField] protected bool loadMapFromFile = false;
    // Path of the map to be laoded.
    [SerializeField] protected string textFilePath = null;

    // Do I have to assemble the map?
    [SerializeField] private bool assembleMap = false;
    // Do I have to update the map?
    [SerializeField] private bool mustUpdate = false;

    // Size of a square.
    [SerializeField] private float squareSize;
    // Heigth of the map.
    [SerializeField] private float heigth;
    // Category of the spawn point gameobjects in the object displacer. 
    [SerializeField] private string spawnPointCategory;
    
    private MapGenerator mapGeneratorScript;
    private MapAssebler mapAssemblerScript;
    private ObjectDisplacer objectDisplacerScript;
    private SpawnPointManager spawnPointManagerScript;

    private char charWall;

    char[,] map;

    void Start() {
        #if UNITY_EDITOR
                UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif

        mapAssemblerScript = mapAssembler.GetComponent<MapAssebler>();
        mapGeneratorScript = mapGenerator.GetComponent<MapGenerator>();
        objectDisplacerScript = objectDisplacer.GetComponent<ObjectDisplacer>();
        spawnPointManagerScript = spawnPointManager.GetComponent<SpawnPointManager>();

        mustUpdate = true;
    }

    void Update() {
        if (mustUpdate && mapAssemblerScript.IsReady() && mapGeneratorScript.IsReady()
            && objectDisplacerScript.IsReady() && spawnPointManagerScript.IsReady()) {
            ManageMap();
            mustUpdate = false;
        }
    }

    private void ManageMap() {
        if (loadMapFromFile) {
            // Load the map.
            LoadMapFromText();
        } else {
            // Generate the map.
            map = mapGeneratorScript.GenerateMap();
        }

        if (assembleMap) {
            // Assemble the map.
            mapAssemblerScript.AssembleMap(map, squareSize, heigth);
            // Displace the objects.
            objectDisplacerScript.DisplaceObjects(map, squareSize, heigth);
            // Set the spawn points.
            spawnPointManagerScript.SetSpawnPoints(objectDisplacerScript.GetObjectsByCategory(spawnPointCategory));
        }
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

}