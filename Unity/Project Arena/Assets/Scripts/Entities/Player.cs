﻿using ScriptableObjectArchitecture;
using UnityEngine;

/// <summary>
/// Player is an implementation of Entity with a ILoggable interface, which allows its actions
/// to be logged. Player is the agent controlled by the user.
/// </summary>
public class Player : Entity, ILoggable
{
    // Head object containing the camera.
    [Header("Player")] [SerializeField] private GameObject head;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravity = 100f;
    [SerializeField] private Transform point;

    // Smoothing factor.
    [SerializeField] private float smoothing = 2.0f;

    // Player controller.
    private CharacterController controller;

    // Tracks the movement the mouse has made.
    private Vector2 mouseLook;

    // Smoothed value of the mouse
    private Vector2 smoothedDelta;

    // Sensibility of the mouse.
    private float sensibility = 2f;

    // Vector used to apply the movement.
    private Vector3 moveDirection = Vector3.zero;

    // Penalty applied to mouse and keyboard movement.
    private float inputPenalty = 1f;

    // Is the cursor locked?
    private bool cursorLocked = false;

    // Is the input enabled?
    private bool inputEnabled = true;

    // Do I have to log?
    private bool loggingGame = false;

    // Time of the last position log.
    private float lastPositionLog = 0;

    private PlayerUIManager playerUIManagerScript;

    // Codes of the numeric keys.
    private KeyCode[] keyCodes =
    {
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

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerUIManagerScript = GetComponent<PlayerUIManager>();

        // Get the mouse sensibility.
        if (PlayerPrefs.HasKey("MouseSensibility"))
        {
            SetSensibility(PlayerPrefs.GetFloat("MouseSensibility"));
        }
        else
        {
            SetSensibility(ParameterManager.Instance.DefaultSensibility);
        }
    }

    private void Update()
    {
        if (inputEnabled)
        {
            // If the cursor should be locked but it isn't, lock it when the user clicks.
            if (Input.GetMouseButtonDown(0))
            {
                if (cursorLocked && Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (inGame)
            {
                var actualGun = guns[currentGun].GetComponent<Gun>();
                if (actualGun.CanAim())
                {
                    if (Input.GetButtonDown("Fire2"))
                    {
                        actualGun.Aim(true);
                    }

                    if (Input.GetButtonUp("Fire2"))
                    {
                        actualGun.Aim(false);
                    }
                }

                if (Input.GetButton("Fire1") && actualGun.CanShoot())
                {
                    actualGun.Shoot();
                }

                if (Input.GetButtonDown("Reload") && actualGun.CanReload())
                {
                    actualGun.Reload();
                }

                UpdateCameraPosition();
                UpdatePosition();
                UpdateGun();
                // Log if needed.
                if (loggingGame && Time.time > lastPositionLog + 0.5)
                {
                    var t = transform;
                    var position = t.position;
                    PositionInfoGameEvent.Instance.Raise(
                        new PositionInfo {x = position.x, z = position.z,
                            dir = t.eulerAngles.y, entityID = entityID});
                    lastPositionLog = Time.time;
                }
            }
            else
            {
                UpdateVerticalPosition();
            }
        }
    }

    // Sets up all the player parameter and does the same with all its guns.
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        activeGuns = ag;
        gameManagerScript = gms;

        totalHealth = th;
        health = th;
        entityID = id;

        playerUIManagerScript.SetActiveGuns(ag);

        for (int i = 0; i < ag.GetLength(0); i++)
        {
            // Setup the gun.
            guns[i].GetComponent<Gun>().SetupGun(gms, this, playerUIManagerScript, i + 1);
        }
    }

    // Sets up all the player parameters and does the same with all its guns.
    public override void SetupEntity(GameManager gms, int id)
    {
        SetupEntity(totalHealth, activeGuns, gms, id);
    }

    // Applies damage to the player and eventually manages its death.
    public override void TakeDamage(int damage, int killerID)
    {
        if (inGame)
        {
            health -= damage;

            // If the health goes under 0, kill the entity and start the respawn process.
            if (health <= 0f)
            {
                health = 0;
                // Kill the entity.
                Die(killerID);
            }

            playerUIManagerScript.SetHealth(health, totalHealth);
            playerUIManagerScript.ShowDamage();
        }
    }

    // Heals the player.
    public override void HealFromMedkit(MedkitPickable medkitPickable)
    {
        if (health + medkitPickable.RestoredHealth > totalHealth)
        {
            health = totalHealth;
        }
        else
        {
            health += medkitPickable.RestoredHealth;
        }

        playerUIManagerScript.SetHealth(health, totalHealth);
    }

    // Kills the player.
    protected override void Die(int id)
    {
        gameManagerScript.AddScore(id, entityID);
        SetInGame(false);
        // Start the respawn process.
        gameManagerScript.ManageEntityDeath(gameObject, this);
    }

    // Respawns the player.
    public override void Respawn()
    {
        health = totalHealth;
        ResetAllAmmo();
        ActivateLowestGun();
        SetInGame(true);

        playerUIManagerScript.SetHealth(health, totalHealth);
    }

    // Activates the lowest ranked gun.
    private void ActivateLowestGun()
    {
        for (int i = 0; i < activeGuns.GetLength(0); i++)
        {
            // Activate it if is one among the active ones which has the lowest rank.
            if (i == GetActiveGun(-1, true))
            {
                ActivateGun(i);
            }
        }
    }

    // Switches weapon if possible.
    private void UpdateGun()
    {
        if (Time.time > lastSwitched + switchWait)
        {
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
            {
                SwitchGuns(currentGun, GetActiveGun(currentGun, true));
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            {
                SwitchGuns(currentGun, GetActiveGun(currentGun, false));
            }
            else
            {
                for (int i = 0; i < guns.Count; i++)
                {
                    if (i != currentGun && activeGuns[i] && Input.GetKeyDown(keyCodes[i]))
                    {
                        SwitchGuns(currentGun, i);
                    }
                }
            }
        }
    }

    // Deactivates a gun and actiates another.
    private void SwitchGuns(int toDeactivate, int toActivate)
    {
        lastSwitched = Time.time;

        if (toDeactivate != toActivate)
        {
            DeactivateGun(toDeactivate);
            ActivateGun(toActivate);
        }
    }

    // Activates a gun.
    private void ActivateGun(int toActivate)
    {
        guns[toActivate].GetComponent<Gun>().Wield();
        currentGun = toActivate;
        SetUIColor();
    }

    // Deactivates a gun.
    private void DeactivateGun(int toDeactivate)
    {
        guns[toDeactivate].GetComponent<Gun>().Stow();
    }

    // Updates the position.
    private void UpdatePosition()
    {
        // If grounded I can jump, if I'm not grounded my movement is penalized.
        if (controller.isGrounded)
        {
            // Read the inputs.
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed * inputPenalty;
            // Jump if needed.
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
            }
        }

        // Apply gravity to the direction and apply it using the controller.
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    // Updates the vertical position.
    private void UpdateVerticalPosition()
    {
        if (controller.isGrounded)
        {
            moveDirection = new Vector3(0, 0, 0);
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);
        }
    }

    // Enables or disables the input.
    internal void EnableInput(bool b)
    {
        inputEnabled = b;

        if (b)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Updates the camera position.
    private void UpdateCameraPosition()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Extract the delta of the mouse.
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensibility * smoothing * inputPenalty,
            sensibility * smoothing * inputPenalty));
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
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    // Unlocks the cursor.
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    // Sets if the player is in game.
    public override void SetInGame(bool b)
    {
        if (b)
        {
            playerUIManagerScript.SetPlayerUIVisible(true);
            ActivateGun(currentGun);
        }
        else
        {
            playerUIManagerScript.SetPlayerUIVisible(false);
            DeactivateGun(currentGun);
        }

        inGame = b;
    }

    // Sets the UI colors.
    private void SetUIColor()
    {
        playerUIManagerScript.SetColorAll(playerUIManagerScript.GetGunColor(currentGun));
        gameManagerScript.SetUIColor(playerUIManagerScript.GetGunColor(currentGun));
    }

    public override void SlowEntity(float penalty)
    {
        inputPenalty = penalty;
    }

    // Shows current guns
    public void ShowGun(bool b)
    {
        if (b)
        {
            ActivateGun(currentGun);
        }
        else
        {
            DeactivateGun(currentGun);
        }
    }

    // Updates the sensibility.
    public void SetSensibility(float s)
    {
        sensibility = s;
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            sensibility /= ParameterManager.Instance.WebSensibilityDownscale;
        }
    }

    // Setups stuff for the loggingGame.
    public void SetupLogging()
    {
        loggingGame = true;
    }
}