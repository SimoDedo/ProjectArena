using UnityEngine;

public class PlayerController : MonoBehaviour {

    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravity = 100f;
    [SerializeField] private float midAirPenalization = 10f;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;

    private void Start () {
        controller = GetComponent<CharacterController>();
    }

    private void Update () {
        // If grounded I can jump, if I'm not grounded my movement is penalized.
        if (controller.isGrounded) {
            // Read the inputs.
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
            // Jump if needed.
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;
        } else {

        }

        // Apply gravity to the direction and appy it using the controller.
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
	}

    // Locks the cursor in the center of the screen.
    public void LockCursor() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Unlocks the cursor.
    public void UnlockCursor() {
        Cursor.lockState = CursorLockMode.None;
    }

}