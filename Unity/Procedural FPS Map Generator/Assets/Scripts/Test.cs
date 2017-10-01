using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    List<int> myList;
    int[] intArray;
    int myInt;

	// Use this for initialization
	void Start () {
        myList = new List<int>();
        intArray = new int[3];

        intArray[0] = 1;
        intArray[1] = 2;
        intArray[2] = 3;

        myList.Add(1);

        myInt = 1;

        DoStuff(myList, intArray, myInt);

        foreach (int i in myList) {
            Debug.Log("List entry: " + i);
        }

        for (int i = 0; i < intArray.GetLength(0);  i++) {
            Debug.Log("Array entry: " + intArray[i]);
        }

        Debug.Log("Integer: " + myInt);
    }

    private void DoStuff(List<int> l, int[] a, int i) {
        l.Add(2);

        a[0] = 4;
        a[1] = 5;
        a[2] = 6;

        i = 100;
    }

}