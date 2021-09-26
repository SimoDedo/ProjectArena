using System;
using System.Linq;
using AssemblyLogging;
using ScriptableObjectArchitecture;
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

    private void Awake()
    {
        Guns = gameObject.GetComponentsInChildren<Gun>().ToList();
    }

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
                var actualGun = Guns[currentGun].GetComponent<Gun>();
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
                        new PositionInfo
                        {
                            x = position.x, z = position.z,
                            dir = t.eulerAngles.y, entityID = entityID
                        });
                    lastPositionLog = Time.time;
                }
            }
            else
            {
                UpdateVerticalPosition();
            }
        }
    }

    // Returns the next or the previous active gun.
    private int GetActiveGun(int currentGun, bool next)
    {
        if (next)
        {
            // Try for the guns after it
            for (int i = currentGun + 1; i < Guns.Count; i++)
            {
                if (ActiveGuns[i])
                {
                    return i;
                }
            }

            // Try for the guns before it
            for (int i = 0; i < currentGun; i++)
            {
                if (ActiveGuns[i])
                {
                    return i;
                }
            }

            // There's no other gun, return itself.
            return currentGun;
        }
        else
        {
            // Try for the guns before it
            for (int i = currentGun - 1; i >= 0; i--)
            {
                if (ActiveGuns[i])
                {
                    return i;
                }
            }

            // Try for the guns after it
            for (int i = Guns.Count - 1; i > currentGun; i--)
            {
                if (ActiveGuns[i])
                {
                    return i;
                }
            }

            // There's no other gun, return itself.
            return currentGun;
        }
    }


    // Sets up all the player parameter and does the same with all its guns.
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        ActiveGuns = ag.ToList();
        gameManagerScript = gms;

        totalHealth = th;
        Health = th;
        entityID = id;

        playerUIManagerScript.SetActiveGuns(ag);

        for (int i = 0; i < ag.GetLength(0); i++)
        {
            // Setup the gun.
            Guns[i].GetComponent<Gun>().SetupGun(gms, this, playerUIManagerScript, i + 1);
        }
    }

    // Applies damage to the player and eventually manages its death.
    public override void TakeDamage(int damage, int killerID)
    {
        if (inGame)
        {
            Health -= damage;

            // If the health goes under 0, kill the entity and start the respawn process.
            if (Health <= 0f)
            {
                Health = 0;
                // Kill the entity.
                Die(killerID);
            }

            playerUIManagerScript.SetHealth(Health, totalHealth);
            playerUIManagerScript.ShowDamage();
        }
    }

    // Heals the player.
    public override void HealFromMedkit(MedkitPickable medkitPickable)
    {
        if (Health + medkitPickable.RestoredHealth > totalHealth)
        {
            Health = totalHealth;
        }
        else
        {
            Health += medkitPickable.RestoredHealth;
        }

        playerUIManagerScript.SetHealth(Health, totalHealth);
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
        Health = totalHealth;
        ResetAllAmmo();
        ActivateLowestGun();
        SetInGame(true);

        playerUIManagerScript.SetHealth(Health, totalHealth);
    }

    // Activates the lowest ranked gun.
    private void ActivateLowestGun()
    {
        for (int i = 0; i < ActiveGuns.Count; i++)
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
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            TrySwitchGuns(currentGun, GetActiveGun(currentGun, true));
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            TrySwitchGuns(currentGun, GetActiveGun(currentGun, false));
        }
        else
        {
            for (int i = 0; i < Guns.Count; i++)
            {
                if (i != currentGun && ActiveGuns[i] && Input.GetKeyDown(keyCodes[i]))
                {
                    if (TrySwitchGuns(currentGun, i))
                        break;
                }
            }
        }
    }

    // Activates a gun.
    protected override void ActivateGun(int toActivate)
    {
        Guns[toActivate].GetComponent<Gun>().Wield();
        currentGun = toActivate;
        SetUIColor();
    }

    // Deactivates a gun.
    protected override void DeactivateGun(int toDeactivate)
    {
        Guns[toDeactivate].GetComponent<Gun>().Stow();
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