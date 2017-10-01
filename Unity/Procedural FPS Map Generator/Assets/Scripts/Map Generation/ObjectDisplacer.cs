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
    private Dictionary<char, CustomObjectList> objectDictionary = new Dictionary<char, CustomObjectList>();

    private void Start() {
        InitializeAll();
    }

    // Creates all the category objects and adds them to the dictionary. An object with no category is 
    // assigned to the default one. Creates and adds the prefabs lists to the objectDictionary.
    private void InitializeAll() {
        foreach (CustomObject c in customObjects) {
            if (transform.Find("Default") && (c.category == "" || c.category == null)) {
                GameObject childObject = new GameObject("Default");
                childObject.transform.parent = transform;
                childObject.transform.localPosition = Vector3.zero;
            } else if (transform.Find(c.category) == null) {
                GameObject childObject = new GameObject(c.category);
                childObject.transform.parent = transform;
                childObject.transform.localPosition = Vector3.zero;
            }

            if (!objectDictionary.ContainsKey(c.objectChar)) {
                objectDictionary.Add(c.objectChar, new CustomObjectList(c));
            } else {
                objectDictionary[c.objectChar].AddObject(c);
            }
        }
    }

    // Displace the custom objects inside the map.
    public void DisplaceObjects(char[,] map, float squareSize, float height) {
        DestroyAllCustomObjects();

        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                if (objectDictionary.ContainsKey(map[x, y])) {
                    CustomObject currentObject = objectDictionary[map[x, y]].GetObject();

                    GameObject childObject = (GameObject) Instantiate(currentObject.prefab);
                    childObject.transform.parent = transform.Find(currentObject.category);
                    childObject.transform.localPosition = new Vector3(x * squareSize - map.GetLength(0) / 2,
                        heightDirCorrection * (height + currentObject.heightCorrection), 
                        y * squareSize - map.GetLength(1) / 2);
                    childObject.transform.localScale *= sizeCorrection;
                }
            }
        }
    }

    // Remove all the custom objects.
    private void DestroyAllCustomObjects() {
        foreach (Transform category in transform) {
            foreach (Transform child in category) {
                GameObject.Destroy(child.gameObject);
            }
        }
    }

    // Custom object. 
    [Serializable]
    private struct CustomObject {
        // Character which defines the object.
        public char objectChar;
        // Category of the object (optional).
        public string category;
        // Prefab of the object.
        public GameObject prefab;
        // Heigth correction factor.
        public float heightCorrection;
    }

    // List of custom objects which share the same char. 
    private class CustomObjectList {
        private List<CustomObject> objs;

        public CustomObjectList(CustomObject obj) {
            objs = new List<CustomObject>();
            objs.Add(obj);
        }

        public void AddObject(CustomObject obj) {
            objs.Add(obj);
        }

        public CustomObject GetObject() {
            int i = UnityEngine.Random.Range(0, objs.Count - 1);
            return objs[i];
        }

    }

}