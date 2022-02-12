using System.IO;
using Maps;
using UnityEngine;
using Random = System.Random;

namespace Graph
{
    public class GraphMapSaverV2 : MonoBehaviour
    {
        [SerializeField] private int startingSeed;
        [SerializeField] private int numRows;
        [SerializeField] private int numColumns;
        [SerializeField] private int maxSize;
        [SerializeField] private float minRatio;
        [SerializeField] private float maxRatio;
        [SerializeField] private int thickness;
        [SerializeField] private int rowSeparation;
        [SerializeField] private int colSeparation;

        [SerializeField] private string mapPrefix;
        [SerializeField] private string statsPrefix;

        [SerializeField] private int mapsToGenerate = 10;
        private string experimentDirectory;

        private int mapsGenerated;

        private void Start()
        {
            mapsGenerated = startingSeed; // TODO REMOVE
            experimentDirectory = Application.persistentDataPath + "/Export/Maps/";
            CreateDirectory(experimentDirectory);
        }

        // Update is called once per frame
        private void Update()
        {
            if (mapsGenerated >= mapsToGenerate) return;
            var random = new Random(startingSeed++);
            var genome = GenomeGenerator.Generate(numRows, numColumns, maxSize, minRatio, maxRatio, thickness,
                rowSeparation,
                colSeparation, random);
            var translator = new GenomeTranslatorV1(random);
            translator.TranslateGenome(genome, out var map, out var areas);

            var properties = MapAnalyzer.CalculateGraphProperties(areas);

            MapAnalyzer.AddEverything(areas, map);

            map = MapEdit.AddBorders(map, 5, 'w');

            var finalMap = MapUtils.GetStringFromCharMap(map);

            mapsGenerated++;

            if (properties.degreeAvg > 2.5f && properties.diameter > 60)
            {
                var propertiesLog = JsonUtility.ToJson(properties);

                File.WriteAllText(experimentDirectory + "/" + mapPrefix + "_" + mapsGenerated + ".txt", finalMap);
                File.WriteAllText(experimentDirectory + "/" + statsPrefix + "_" + mapsGenerated + ".json",
                    propertiesLog);
            }

            Debug.Log("DONE map " + mapsGenerated + "!");
        }

        // Creates a directory if needed.
        private static void CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }
    }
}