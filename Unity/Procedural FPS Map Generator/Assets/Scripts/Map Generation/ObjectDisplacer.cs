using System;
using UnityEngine;

public class ObjectDisplacer : MonoBehaviour {

    // Custom objects that will be added to the map.
    [SerializeField] private CustomObject[] customObjects;
    // Object scale correction factor.
    [SerializeField] private float sizeCorrection = 1f;

    // Displace the custom objects inside the map.
    public void DisplaceObjects(char[,] map, float height, float squareSize) {

    }
    
    // Informations about an object. 
    [Serializable]
    private struct CustomObject {
        // Character which defines the object.
        public char objectChar;
        // Number of objects to be put in the map.
        public int numObjPerMap;
    }

}
