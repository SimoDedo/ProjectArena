using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class MapManager : CoreComponent {

    [Header("Core components")] [SerializeField] protected GameObject mapAssembler;
    [SerializeField] protected GameObject mapGenerator;
    [SerializeField] protected GameObject objectDisplacer;

    // Do I have to load the map from a .txt?
    [Header("Import")] [SerializeField] protected bool loadMapFromFile = false;
    // Path of the map to be laoded.
    [SerializeField] protected string textFilePath = null;

    // Category of the spawn point gameobjects in the object displacer. 
    [Header("Other")] [SerializeField] protected string spawnPointCategory;

    protected MapGenerator mapGeneratorScript;
    protected MapAssebler mapAssemblerScript;
    protected ObjectDisplacer objectDisplacerScript;

    protected string seed;
    protected bool export;
    protected string exportPath;
    
    private void Start() {
        ExtractParametersFromManager();

        InitializeAll();
    }

    private void Update() {
        if (!IsReady() && mapAssemblerScript.IsReady() && mapGeneratorScript.IsReady()
            && objectDisplacerScript.IsReady()) {
            SetReady(true);
        }
    }

    protected abstract void InitializeAll();

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
            export = GetParameterManager().GetExport();
            exportPath = GetParameterManager().GetExportPath();

            switch (GetParameterManager().GetGenerationMode()) {
                case 0:
                    loadMapFromFile = false;
                    seed = GetParameterManager().GetMapDNA();
                    break;
                case 1:
                    loadMapFromFile = false;
                    seed = GetParameterManager().GetMapDNA();
                    break;
                case 2:
                    loadMapFromFile = true;
                    seed = null;
                    textFilePath = GetParameterManager().GetMapDNA();
                    break;
                case 3:
                    loadMapFromFile = false;
                    seed = GetParameterManager().GetMapDNA();
                    textFilePath = GetParameterManager().GetMapDNA();
                    break;
            }
        }
    }

    // Returns the parameter Manager.
    protected ParameterManager GetParameterManager() {
        if (GameObject.Find("Parameter Manager") != null)
            return GameObject.Find("Parameter Manager").GetComponent<ParameterManager>();
        else
            return null;
    }

}