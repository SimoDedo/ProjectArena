using Entity.Component;
using Logging;
using Managers;
using Managers.Mode;
using Others;
using Pickables;
using UI;
using UnityEngine;

namespace Entity
{
    /// <summary>
    ///     Player is an implementation of Entity with a ILoggable interface, which allows its actions
    ///     to be logged. Player is the agent controlled by the user.
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

        // Codes of the numeric keys.
        private readonly KeyCode[] keyCodes =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9
        };


        // Player controller.
        private CharacterController controller;

        // Is the cursor locked?
        private bool cursorLocked;
        private GunManager gunManager;

        // Is the input enabled?
        private bool inputEnabled = true;

        // Penalty applied to mouse and keyboard movement.
        private float inputPenalty = 1f;

        // Time of the last position log.
        private float lastPositionLog;

        // Do I have to log?
        private bool loggingGame;

        // Tracks the movement the mouse has made.
        private Vector2 mouseLook;

        // Vector used to apply the movement.
        private Vector3 moveDirection = Vector3.zero;

        private PlayerUIManager playerUIManagerScript;


        private int previousGun = GunManager.NO_GUN;

        // Sensibility of the mouse.
        private float sensibility = 2f;

        // Smoothed value of the mouse
        private Vector2 smoothedDelta;
        private int CurrentGun => gunManager.CurrentGunIndex;

        private void Awake()
        {
            gunManager = new GunManager(this);
        }

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            playerUIManagerScript = GetComponent<PlayerUIManager>();

            // Get the mouse sensibility.
            if (PlayerPrefs.HasKey("MouseSensibility"))
                SetSensibility(PlayerPrefs.GetFloat("MouseSensibility"));
            else
                SetSensibility(ParameterManager.Instance.DefaultSensibility);
        }

        private void Update()
        {
            if (inputEnabled)
            {
                // If the cursor should be locked but it isn't, lock it when the user clicks.
                if (Input.GetMouseButtonDown(0))
                    if (cursorLocked && Cursor.lockState != CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }

                if (inGame)
                {
                    if (gunManager.CanCurrentGunAim())
                    {
                        if (Input.GetButtonDown("Fire2")) gunManager.SetCurrentGunAim(true);

                        if (Input.GetButtonUp("Fire2")) gunManager.SetCurrentGunAim(false);
                    }

                    if (Input.GetButton("Fire1") && gunManager.CanCurrentGunShoot()) gunManager.ShootCurrentGun();

                    if (Input.GetButtonDown("Reload") && gunManager.CanCurrentGunReload())
                        gunManager.ReloadCurrentGun();

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

        // Setups stuff for the loggingGame.
        public void SetupLogging()
        {
            loggingGame = true;
        }

        // Returns the next or the previous active gun.
        private int GetActiveGun(int currentGun, bool next)
        {
            var gunsCount = gunManager.NumberOfGuns;
            if (next)
            {
                // Try for the guns after it
                for (var i = currentGun + 1; i < gunsCount; i++)
                    if (gunManager.IsGunActive(i))
                        return i;

                // Try for the guns before it
                for (var i = 0; i < currentGun; i++)
                    if (gunManager.IsGunActive(i))
                        return i;

                // There's no other gun, return itself.
                return currentGun;
            }

            // Try for the guns before it
            for (var i = currentGun - 1; i >= 0; i--)
                if (gunManager.IsGunActive(i))
                    return i;

            // Try for the guns after it
            for (var i = gunsCount - 1; i > currentGun; i--)
                if (gunManager.IsGunActive(i))
                    return i;

            // There's no other gun, return itself.
            return currentGun;
        }


        // Sets up all the player parameter and does the same with all its guns.
        public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
        {
            gunManager.Prepare(gms, this, playerUIManagerScript, ag);
            gameManagerScript = gms;

            totalHealth = th;
            Health = th;
            entityID = id;

            playerUIManagerScript.SetActiveGuns(ag);
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
                Health = totalHealth;
            else
                Health += medkitPickable.RestoredHealth;

            playerUIManagerScript.SetHealth(Health, totalHealth);
        }

        public override bool CanBeSupplied(bool[] suppliedGuns)
        {
            return gunManager.CanBeSupplied(suppliedGuns);
        }

        public override void SupplyFromAmmoCrate(AmmoPickable ammoCrate)
        {
            gunManager.SupplyGuns(ammoCrate.SuppliedGuns, ammoCrate.AmmoAmounts);
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
            gunManager.ResetAmmo();
            ActivateLowestGun();
            SetInGame(true);

            playerUIManagerScript?.SetHealth(Health, totalHealth);
        }

        // Activates the lowest ranked gun.
        private void ActivateLowestGun()
        {
            // var gunCount = gunManager.NumberOfGuns();
            // for (int i = 0; i < gun; i++)
            // {
            // Activate it if is one among the active ones which has the lowest rank.
            // if (i == GetActiveGun(-1, true))
            // {
            //     ActivateGun(i);
            // }
            // }
            ActivateGun(GetActiveGun(-1, true));
        }

        // Switches weapon if possible.
        private void UpdateGun()
        {
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
            {
                gunManager.TrySwitchGuns(CurrentGun, GetActiveGun(CurrentGun, true));
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            {
                gunManager.TrySwitchGuns(CurrentGun, GetActiveGun(CurrentGun, false));
            }
            else
            {
                var gunsCount = gunManager.NumberOfGuns;
                for (var i = 0; i < gunsCount; i++)
                    if (i != CurrentGun && gunManager.IsGunActive(i) && Input.GetKeyDown(keyCodes[i]))
                        if (gunManager.TrySwitchGuns(CurrentGun, i))
                            break;
            }
        }

        // Activates a gun.
        private void ActivateGun(int toActivate)
        {
            gunManager.TryEquipGun(toActivate);
            // Guns[toActivate].GetComponent<Gun>().Wield();
            SetUIColor();
        }

        // Deactivates a gun.
        private void UnequipCurrentGun()
        {
            gunManager.TryEquipGun(GunManager.NO_GUN);
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
                if (Input.GetButton("Jump")) moveDirection.y = jumpSpeed;
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
            var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

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
        public override void SetInGame(bool b, bool isGameEnded = false)
        {
            if (b)
            {
                playerUIManagerScript.SetPlayerUIVisible(true);
                ActivateLowestGun();
            }
            else
            {
                playerUIManagerScript.SetPlayerUIVisible(false);
                UnequipCurrentGun();
            }

            inGame = b;
        }

        // Sets the UI colors.
        private void SetUIColor()
        {
            playerUIManagerScript.SetColorAll(playerUIManagerScript.GetGunColor(CurrentGun));
            gameManagerScript.SetUIColor(playerUIManagerScript.GetGunColor(CurrentGun));
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
                ActivateGun(previousGun);
            }
            else
            {
                previousGun = CurrentGun;
                UnequipCurrentGun();
            }
        }

        // Updates the sensibility.
        public void SetSensibility(float s)
        {
            sensibility = s;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                sensibility /= ParameterManager.Instance.WebSensibilityDownscale;
        }
    }
}