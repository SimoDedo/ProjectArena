using System;
using System.Collections.Generic;
using Others;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Maps
{
    /// <summary>
    ///     ObjectDisplacer is a class used to place objects in a map.
    /// </summary>
    public class ObjectDisplacer : CoreComponent
    {
        // Height correction factor.
        [SerializeField] private float heightCorrection;

        // Object scale correction factor.
        [SerializeField] private float sizeCorrection = 1f;

        // Custom objects that will be added to the map.
        [SerializeField] private CustomObject[] customObjects;

        // Dictionary associating a categoty to a list of objects.
        private Dictionary<string, List<GameObject>> categoryObjectsDictionary;

        // Dictionary associating a char to a list of objects.
        private Dictionary<char, CustomObjectList> charObjectsDictionary;

        private void Start()
        {
            InitializeAll();

            SetReady(true);
        }

        // Creates all the category objects and adds them to the dictionary. An object with no category 
        // is assigned to the default one. Creates and adds the prefabs lists to the 
        // charObjectsDictionary. Sets the height direction correction value.
        private void InitializeAll()
        {
            charObjectsDictionary = new Dictionary<char, CustomObjectList>();
            categoryObjectsDictionary = new Dictionary<string, List<GameObject>>();

            foreach (var c in customObjects)
            {
                if (transform.Find("Default") && string.IsNullOrEmpty(c.category))
                {
                    var childObject = new GameObject("Default");
                    childObject.transform.parent = transform;
                    childObject.transform.localPosition = Vector3.zero;
                }
                else if (transform.Find(c.category) == null)
                {
                    var childObject = new GameObject(c.category);
                    childObject.transform.parent = transform;
                    childObject.transform.localPosition = Vector3.zero;
                }

                if (!charObjectsDictionary.ContainsKey(c.objectChar))
                    charObjectsDictionary.Add(c.objectChar, new CustomObjectList(c));
                else
                    charObjectsDictionary[c.objectChar].AddObject(c);
            }
        }

        // Displace the custom objects inside the map.
        public void DisplaceObjects(char[,] map, float squareSize, float height)
        {
            var rows = map.GetLength(0);
            var columns = map.GetLength(1);
            for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
                if (charObjectsDictionary.ContainsKey(map[r, c]))
                {
                    var currentObject = charObjectsDictionary[map[r, c]].GetObject();

                    var childObject = Instantiate(currentObject.prefab);
                    childObject.name = currentObject.prefab.name;
                    childObject.transform.parent = transform.Find(currentObject.category);

                    var halfSquareSize = squareSize / 2f;

                    childObject.transform.localPosition = new Vector3(
                        squareSize * c + halfSquareSize,
                        heightCorrection + height + currentObject.heightCorrection,
                        (rows - r - 1) * squareSize + halfSquareSize
                    );
                    childObject.transform.localScale *= sizeCorrection;

                    if (categoryObjectsDictionary.ContainsKey(currentObject.category))
                    {
                        categoryObjectsDictionary[currentObject.category].Add(childObject);
                    }
                    else
                    {
                        categoryObjectsDictionary.Add(currentObject.category,
                            new List<GameObject>());
                        categoryObjectsDictionary[currentObject.category].Add(childObject);
                    }
                }
        }

        // Remove all the custom objects.
        public void DestroyAllCustomObjects()
        {
            categoryObjectsDictionary.Clear();

            foreach (Transform category in transform)
            foreach (Transform child in category)
                Destroy(child.gameObject);
        }

        // Returns all the object which have the category passed as parameter.
        public List<GameObject> GetObjectsByCategory(string category)
        {
            try
            {
                return categoryObjectsDictionary[category];
            }
            catch (KeyNotFoundException)
            {
                ManageError(Error.HARD_ERROR, "Error while populating the map, no object of category " +
                                              category + " found in the dictionary.");
                return null;
            }
        }

        // Custom object. 
        [Serializable]
        private struct CustomObject
        {
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
        private class CustomObjectList
        {
            public readonly List<CustomObject> objs;

            public CustomObjectList(CustomObject obj)
            {
                objs = new List<CustomObject>
                {
                    obj
                };
            }

            public void AddObject(CustomObject obj)
            {
                objs.Add(obj);
            }

            public CustomObject GetObject()
            {
                var i = Random.Range(0, objs.Count);
                return objs[i];
            }
        }
    }
}