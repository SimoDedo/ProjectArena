using UnityEngine;

public class Player : Entity {

    // Head object containing the camera.
    [SerializeField] private GameObject head;

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

    private PlayerUIManager playerUIManagerScript;

    // Codes of the numeric keys.
    private KeyCode[] keyCodes = {
         KeyCode.Alpha1,
         KeyCode.Alpha2,
         KeyCode.Alpha3,
         KeyCode.Alpha4,
         KeyCode.Alpha5,
         KeyCode.Alpha6,
         KeyCode.Alpha7,
         KeyCode.Alpha8,
         KeyCode.Alpha9,
     };

    // Variables to slow down the gun switchig.
    private float lastSwitched = 0f;
    private float switchWait = 0.05f;

    private void Start() {
        controller = GetComponent<CharacterController>();
        playerUIManagerScript = GetComponent<PlayerUIManager>();
    }

    private void Update() {
        // If the cursor should be locked but it isn't, lock it when the user clicks.
        if (Input.GetMouseButtonDown(0)) {
            if (cursorLocked && Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
        }

        // If I can move update the player position depending on the inputs.
        if (inGame) {
            UpdateCameraPosition();
            UpdatePosition();
            UpdateGun();
        }
    }

    // Sets up all the player parameter and does the same with all its guns.
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        activeGuns = ag;
        gameManagerScript = gms;

        totalHealth = th;
        health = th;
        entityID = id;

        playerUIManagerScript.SetActiveGuns(ag);

        for (int i = 0; i < ag.GetLength(0); i++) {
            // Setup the gun.
            guns[i].GetComponent<Gun>().SetupGun(gms, this, playerUIManagerScript);
            // Activate it if is one among the active ones which has the lowest rank.
            if (i == GetActiveGun(-1, true)) {
                ActivateGun(i);
            }
        }
    }

    // Kills the player.
    protected override void Die(int id) {
        gameManagerScript.AddKill(id, entityID);
        SetInGame(false);
    }

    // Respawns the player.
    public override void Respawn() {
        SetInGame(true);
        health = totalHealth;
    }

    // Switches weapon if possible.
    private void UpdateGun() {
        if (Time.time > lastSwitched + switchWait) {
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0) {
                SwitchGuns(currentGun, GetActiveGun(currentGun, true));
            } else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0) {
                SwitchGuns(currentGun, GetActiveGun(currentGun, false));
            } else {
                for (int i = 0; i < guns.Count; i++) {
                    if (i != currentGun && activeGuns[i] && Input.GetKeyDown(keyCodes[i])) {
                        SwitchGuns(currentGun, i);
                    }
                }
            }
        }
    }

    // Deactivates a gun and actiates another.
    private void SwitchGuns(int toDeactivate, int toActivate) {
        lastSwitched = Time.time;

        if (toDeactivate != toActivate) {
            DeactivateGun(toDeactivate);
            ActivateGun(toActivate);
        }
    }

    // Activates a gun.
    private void ActivateGun(int toActivate) {
        guns[toActivate].GetComponent<Gun>().Wield();
        currentGun = toActivate;
    }

    // Deactivates a gun.
    private void DeactivateGun(int toDeactivate) {
        guns[toDeactivate].GetComponent<Gun>().Stow();
    }

    // Updates the position.
    private void UpdatePosition() {
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

    // TODO - Sets if the player is in game.
    public override void SetInGame(bool b) {
        if (b) {
            playerUIManagerScript.SetPlayerUIVisible(true);
            ActivateGun(currentGun);
        } else {
            playerUIManagerScript.SetPlayerUIVisible(false);
            DeactivateGun(currentGun);
        }

        inGame = b;
    }

}