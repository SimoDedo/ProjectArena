using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitCameraToMap : MonoBehaviour {

	public GameObject mapGenerator;

	private MapGenerator mapGeneratorScript;

	void Start () {
		mapGeneratorScript = mapGenerator.GetComponent<MapGenerator>();

		Fit();
	}
	
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			Fit();
		}
	}

	private void Fit() {
		Vector3 v = transform.position;
		v.y = mapGeneratorScript.GetMapSize();
		v.z = - mapGeneratorScript.GetMapSize();
		transform.position = v;
	}

}