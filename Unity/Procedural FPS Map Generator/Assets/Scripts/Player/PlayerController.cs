using UnityEngine;

public class PlayerController : MonoBehaviour {

    [SerializeField] private float speed = 10f; 

	private void Start () {
        // Turns off the cursor and locks it so that it stays inside the game windows.
        Cursor.lockState = CursorLockMode.Locked;
	}

    private void Update () {
        #if UNITY_EDITOR
            if (Input.GetKeyDown("escape"))
                Cursor.lockState = CursorLockMode.None;
        #endif

        float translation = Input.GetAxis("Vertical") * speed;
        float straffe = Input.GetAxis("Horizontal") * speed;
        // We calculate the deltaTime because update doesn't have a fixed duration.
        translation *= Time.deltaTime;
        straffe *= Time.deltaTime;

        // I strafe along the x axis and transalte along the z axis.
        transform.Translate(straffe, 0, translation);
	}

}