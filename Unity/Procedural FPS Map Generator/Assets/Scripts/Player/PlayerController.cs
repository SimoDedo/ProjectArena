using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    // Head object containing the camera.
    [SerializeField] private GameObject head;

    // Weapons.
    [SerializeField] private List<GameObject> weapons;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravity = 100f;

    // Sensitivity of the mouse.
    [SerializeField] private float sensitivity = 5.0f;
    // Smoothing factor.
    [SerializeField] private float smoothing = 2.0f;

    // Player controller.
    private CharacterController controller;

    // Tracks the movement the mouse has made.
    private Vector2 mouseLook;
    // Smoothed value of the mouse
    private Vector2 smoothedDelta;

    // Vector used to apply the movement.
    private Vector3 moveDirection = Vector3.zero;
    // Is the cursor locked?
    private bool cursorLocked = false;
    // Is the movement enabled?
    private bool movementEnabled = false;

    private GameManager gameManagerScript;

    // Informations about the player.
    private bool[] activeWeaponsPlayer;
    private int totalHealth;
    private int health;

    private void Start () {
        controller = GetComponent<CharacterController>();
    }

    private void Update () {
        // If the cursor should be locked but it isn't, lock it when the user clicks.
        if (Input.GetMouseButtonDown(0)) {
            if (cursorLocked && Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
        }

        if (movementEnabled) {
            UpdateCameraPosition();
            UpdatePlayerPosition();
        }
    }

    // Sets all the player parameters.
    public void SetupPlayer(int th, bool[] awp, GameManager gms) {
        totalHealth = th;
        activeWeaponsPlayer = awp;
        gameManagerScript = gms;

        for (int i = 0; i < awp.GetLength(0); i++) {
            weapons[i].GetComponent<Firearm>().SetupFirearm(gms);

            if (awp[i])
                weapons[i].SetActive(true);
            else
                weapons[i].SetActive(false);
        } 


    }

    // Updates the player position.
    private void UpdatePlayerPosition() {
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
            // TODO - ???
        }

        // Apply gravity to the direction and appy it using the controller.
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    // Updates the camera position.
    private void UpdateCameraPosition() {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Extract the delta of the mouse.
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothedDelta.x = Mathf.Lerp(smoothedDelta.x, mouseDelta.x, 1f / smoothing);
        smoothedDelta.y = Mathf.Lerp(smoothedDelta.y, mouseDelta.y, 1f / smoothing);
        mouseLook += smoothedDelta;

        // Impose a bound on the angle.
        mouseLook.y = Mathf.Clamp(mouseLook.y, -90f, 90f);

        // Apply the transformation.
        head.transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(mouseLook.x, transform.up);
    }

    // Locks the cursor in the center of the screen.
    public void LockCursor() {
        Cursor.lockState = CursorLockMode.Locked;
        cursorLocked = true;
    }

    // Unlocks the cursor.
    public void UnlockCursor() {
        Cursor.lockState = CursorLockMode.None;
        cursorLocked = false;
    }

    // Enables/disables the movement.
    public void SetMovementEnabled(bool b) {
        movementEnabled = b;

        // TODO - Disable/enable the weapons too.
    }

}