using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExperimentObjects {

    [Serializable]
    public class Study {
        [SerializeField] public string studyName;
        [SerializeField] public bool flip;
        [SerializeField] public List<Case> cases;
        [NonSerialized] public int completion;
    }

    [Serializable]
    public class Case {
        [SerializeField] public string caseName;
        [SerializeField] public List<TextAsset> maps;
        [SerializeField] public string scene;
        [NonSerialized] public int completion;
        [NonSerialized] public int mapIndex;

        public Case() {
            RandomizeCurrentMap();
        }

        public TextAsset GetCurrentMap() {
            return maps[mapIndex];
        }

        public void RandomizeCurrentMap() {
            if (maps != null) {
                mapIndex = UnityEngine.Random.Range(0, maps.Count);
            } else {
                mapIndex = 0;
            }
        }
    }

    public struct Coord {
        public float x;
        public float z;
    }

}
