using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDisplacer : MonoBehaviour {

    // Object scale correction factor.
    [SerializeField] private float sizeCorrection = 1f;
    // Height direction correction factor.
    [SerializeField] private float heightDirCorrection = 1f;
    // Custom objects that will be added to the map.
    [SerializeField] private CustomObject[] customObjects;

    // Dictionary associating a char to a GameObject.
    private Dictionary<char, CustomObject> objectDictionary = new Dictionary<char, CustomObject>();
    // Dictionary associating the category to an object folder.
    private Dictionary<String, GameObject> categoryDictionary = new Dictionary<String, GameObject>();

    private void Start() {
        InitializeDictionaries();
    }

    // Creates all the category objects and adds them to the dictionary. An object with no category is 
    // assigned to the default one. Adds the prefabs to the objectDictionary.
    private void InitializeDictionaries() {
        foreach (CustomObject c in customObjects) {
            if (c.category == "" && !categoryDictionary.ContainsKey("Default")) {
                GameObject childObject = new GameObject("Default");
                categoryDictionary.Add("Default", childObject);
                childObject.transform.parent = transform;
                childObject.transform.localPosition = Vector3.zero;
            } else if (!categoryDictionary.ContainsKey(c.category)) {
                GameObject childObject = new GameObject(c.category);
                categoryDictionary.Add(c.category, childObject);
                childObject.transform.parent = transform;
                childObject.transform.localPosition = Vector3.zero;
            }

            objectDictionary.Add(c.objectChar, c);
        }
    }

    // Displace the custom objects inside the map.
    public void DisplaceObjects(char[,] map, float squareSize, float height) {
        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                if (objectDictionary.ContainsKey(map[x, y])) {
                    GameObject childObject = (GameObject) Instantiate(objectDictionary[map[x, y]].prefab);
                    childObject.transform.parent = categoryDictionary[objectDictionary[map[x, y]].category].transform;
                    childObject.transform.localPosition = new Vector3(x * squareSize - map.GetLength(0) / 2,
                        heightDirCorrection * (height + objectDictionary[map[x, y]].heightCorrection), 
                        y * squareSize - map.GetLength(1) / 2);
                    childObject.transform.localScale *= sizeCorrection;
                }
            }
        }
    }
    
    // Custom object. 
    [Serializable]
    private struct CustomObject {
        // Character which defines the object.
        public char objectChar;
        // Category of the object (optional).
        public String category;
        // Prefab of the object.
        public GameObject prefab;
        // Heigth correction factor.
        public float heightCorrection;
    }

}