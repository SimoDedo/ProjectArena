using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMouseLock : MonoBehaviour {

    // Sensitivity of the mouse.
    public float sensitivity = 5.0f;
    // Smoothing factor.
    public float smoothing = 2.0f;

    // Tracks the movement the mouse has made.
    private Vector2 mouseLook;
    // Smoothed value of the mouse
    private Vector2 smoothedDelta;

    // Player object.
    private GameObject player;

    void Start() {
        // The script is attached to the camera, but we also need the player.
        player = transform.parent.gameObject;
    }

    void Update() {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Extract the delta of the mouse.
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothedDelta.x = Mathf.Lerp(smoothedDelta.x, mouseDelta.x, 1f / smoothing);
        smoothedDelta.y = Mathf.Lerp(smoothedDelta.y, mouseDelta.y, 1f / smoothing);
        mouseLook += smoothedDelta;

        // Impose a bound on the angle.
        mouseLook.y = Mathf.Clamp(mouseLook.y, -90f, 90f);

        // Apply the transformation.
        transform.localRotation = Quaternion.AngleAxis(- mouseLook.y, Vector3.right);
        player.transform.localRotation = Quaternion.AngleAxis(mouseLook.x, player.transform.up);
    }

}