using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Dump : MonoBehaviour
{
    private CharacterController chara;
    private void Start()
    {
        chara = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
        chara.Move(new Vector3(1.0f, 0.0f, 0.0f));
    }
}
