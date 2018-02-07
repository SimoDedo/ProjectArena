using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapManager is an abstract class used to implement any kind of map manager. A map manager manages
/// the creation process of a map, which consists in generation (performed by a map generator), 
/// assembling (performed by a map assembler) and population (performed by an object displacer).
/// </summary>
public abstract class MapManager : CoreComponent {

    [Header("Core components")] [SerializeField] protected MapGenerator mapGeneratorScript;
    [SerializeField] protected MapAssebler mapAssemblerScript;
    [SerializeField] protected ObjectDisplacer objectDisplacerScript;

    // Do I have to load the map from a .txt?
    [Header("Import")] [SerializeField] protected bool loadMapFromFile = false;
    // Path of the map to be laoded.
    [SerializeField] protected string textFilePath = null;

    // Category of the spawn point gameobjects in the object displacer. 
    [Header("Other")] [SerializeField] protected string spawnPointCategory;

    protected string seed;
    protected bool export;
    protected string exportPath;
    protected bool flip;

    private void Start() {
        ExtractParametersFromManager();
    }

    private void Update() {
        if (!IsReady() && mapAssemblerScript.IsReady() && mapGeneratorScript.IsReady() &&
             objectDisplacerScript.IsReady()) {
            SetReady(true);
        }
    }

    public abstract void ManageMap(bool assembleMap);

    // Returns the spawn points.
    public List<GameObject> GetSpawnPoints() {
        return objectDisplacerScript.GetObjectsByCategory(spawnPointCategory);
    }

    // Loads the map from a text file.
    protected abstract void LoadMapFromText();

    // Extracts the parameters from the parameter Manager, if any.
    protected void ExtractParametersFromManager() {
        if (GetParameterManager() != null) {
            export = GetParameterManager().Export;
            exportPath = GetParameterManager().ExportPath;
            flip = GetParameterManager().Flip;

            switch (GetParameterManager().GenerationMode) {
                case 0:
                    loadMapFromFile = false;
                    seed = GetParameterManager().MapDNA;
                    break;
                case 1:
                    loadMapFromFile = false;
                    seed = GetParameterManager().MapDNA;
                    break;
                case 2:
                    loadMapFromFile = true;
                    seed = null;
                    textFilePath = GetParameterManager().MapDNA;
                    break;
                case 3:
                    loadMapFromFile = false;
                    seed = GetParameterManager().MapDNA;
                    textFilePath = GetParameterManager().MapDNA;
                    break;
                case 4:
                    loadMapFromFile = true;
                    seed = GetParameterManager().MapDNA;
                    textFilePath = null;
                    break;
            }
        }
    }

    // Returns the Parameter Manager.
    protected ParameterManager GetParameterManager() {
        if (GameObject.Find("Parameter Manager") != null) {
            return GameObject.Find("Parameter Manager").GetComponent<ParameterManager>();
        } else {
            return null;
        }
    }

    // Returns the Map Generator.
    public MapGenerator GetMapGenerator() {
        return mapGeneratorScript;
    }

}