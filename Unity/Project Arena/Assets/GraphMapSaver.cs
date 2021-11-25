using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssemblyGraph;
using AssemblyMaps.Map_Generator;
using UnityEngine;

public class GraphMapSaver : MonoBehaviour
{
    [SerializeField] private GraphMapGenerator generator;
    [SerializeField] private string mapPrefix;
    [SerializeField] private string statsPrefix;

    [SerializeField] private int mapsToGenerate = 10;
    private int mapsGenerated = 0;
    private string experimentDirectory;

    private void Start()
    {
        experimentDirectory = Application.persistentDataPath + "/Export/Maps/";
        CreateDirectory(experimentDirectory);
    }

    // Update is called once per frame
    void Update()
    {
        if (generator.IsReady() && mapsGenerated < mapsToGenerate)
        {
            mapsGenerated++;
            var map = generator.GenerateMap(mapsGenerated.ToString(), false);
            var areas = generator.ConvertMapToAreas();

            var properties = MapAnalyzer.CalculateGraphProperties(areas);

            var mapToString = MapUtils.GetStringFromCharMap(map);

            var propertiesLog = JsonUtility.ToJson(properties);

            File.WriteAllText(experimentDirectory + "/" + mapPrefix + "_" + mapsGenerated + ".txt", mapToString);
            File.WriteAllText(experimentDirectory + "/" + statsPrefix + "_" + mapsGenerated + ".json", propertiesLog);

            Debug.Log("DONE map " + mapsGenerated + "!");
        }
    }

    // Creates a directory if needed.
    private static void CreateDirectory(string directory)
    {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    }
}