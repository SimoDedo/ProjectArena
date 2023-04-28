using System.Collections;
using Logging;
using Managers.Mode;
using Others;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Guns
{
    /// <summary>
    ///     Gun is an abstract class used to implement any kind of ranged weapon.
    /// </summary>
    public abstract class Gun : MonoBehaviour, ILoggable
    {
        [Header("Objects")] [SerializeField] protected Camera headCamera;
        [SerializeField] protected GameObject muzzleFlash;

        [Header("Gun parameters")] [SerializeField]
        protected int damage = 10;

        /// <summary>
        /// Dispersion of the weapon while not isAiming down the sight.
        /// </summary>
        [FormerlySerializedAs("dispersion")] [SerializeField] protected float normalDispersion;
        /// <summary>
        /// Dispersion of the weapon while isAiming down the sight.
        /// </summary>
        [FormerlySerializedAs("dispersion")] [SerializeField] protected float aimingDispersion;

        protected float Dispersion => isAiming && !animatingAim ? aimingDispersion : normalDispersion;

        [SerializeField] protected int projectilesPerShot = 1;
        [SerializeField] protected bool infinteAmmo;
        [SerializeField] protected int chargerSize;
        [SerializeField] protected int maximumAmmo;
        [SerializeField] protected float reloadTime = 1f;

        [SerializeField] protected float cooldownTime = 0.1f;

        [SerializeField] private bool isBlastWeapon;

        [Header("Appearence")] [SerializeField]
        protected float muzzleFlashDuration = 0.05f;

        [SerializeField] protected float recoil = 0.05f;

        // Noise decay: 1/distanceSquared
        // TODO use decibels?
        [Header("Noise")] [SerializeField] private float shotNoise = 3000;
        
        [Header("Aim")] [SerializeField] protected bool aimEnabled;
        [SerializeField] protected bool overlayEnabled;
        [SerializeField] protected float zoom = 1f;
        [SerializeField] protected Camera weaponCamera;
        [SerializeField] protected Vector3 aimPosition;
        [SerializeField] protected Image aimOverlay;

        [Header("UI")] [SerializeField] protected bool displayUIIfAvailable = true;

        // Variables to manage the aim.
        protected bool isAiming;
        private float aimStart;

        // Variables to manage ammo.
        protected int ammoInCharger;
        private bool animatingAim;

        protected bool canDisplayUI;

        // Variables to manage cooldown and reload.
        private float cooldownStart;
        private bool coolingDown;

        // Default ammo.
        private int defaultAmmoInCharger;
        private int defaultTotalAmmo;

        // Is the input enabled?
        // Gun identifier.
        protected int gunId;

        protected GunUIManager gunUIManagerScript;

        // Do I have to log?
        protected bool loggingGame = true;
        private float originalFOV;
        protected Entity.Entity ownerEntityScript;
        private PlayerUIManager playerUIManagerScript;
        private float reloadStart;
        protected int ammoNotInCharger;

        // Is the gun being used?
        private bool used;

        public bool IsReloading { get; private set; }

        public bool IsBlastWeapon => isBlastWeapon;

        public abstract bool IsProjectileWeapon { get; }

        public abstract float MaxRange { get; }

        protected void Awake()
        {
            if (aimEnabled) originalFOV = headCamera.fieldOfView;
        }

        protected void Update()
        {
            if (used)
            {
                if (IsReloading || coolingDown) UpdateTimers();

                if (animatingAim) AnimateAim();
            }
        }

        protected void OnDisable()
        {
            if (aimEnabled) ResetAim();

            if (muzzleFlash.activeSelf) muzzleFlash.SetActive(false);
        }

        // Setups stuff for the loggingGame.
        public void SetupLogging()
        {
            loggingGame = true;
        }
        // [SerializeField] protected GunType gunType;

        // public GunType GetGunType()
        // {
        //     return gunType;
        // }

        // public enum GunType
        // {
        //     Sniper_Rifle,
        //     Assault_Rifle,
        //     Shotgun,
        //     Rocket_Launcher
        // }

        public abstract float GetProjectileSpeed();
        public abstract Vector3 GetProjectileSpawnerForwardDirection();

        // Allows accepting input and enables all the childrens.
        public void Wield()
        {
            SetChildrenEnabled(true);
            muzzleFlash.SetActive(false);
            used = true;

            if (ammoInCharger == 0 && CanReload()) Reload();
        }

        // Stops reloading, stops isAiming, disallows accepting input and disables all the childrens.
        public void Stow()
        {
            // When I switch guns I stop the reloading, but not the cooldown.
            IsReloading = false;

            if (canDisplayUI) playerUIManagerScript.StopReloading();

            if (aimEnabled) ResetAim();

            SetChildrenEnabled(false);
            used = false;
        }

        // Ends the reload or the cooldown phases if possible. 
        private void UpdateTimers()
        {
            if (IsReloading)
            {
                if (Time.time > reloadStart + reloadTime)
                {
                    // Log if needed.
                    if (loggingGame)
                        ReloadInfoGameEvent.Instance.Raise(new ReloadInfo
                        {
                            ownerId = ownerEntityScript.GetID(),
                            gunId = gunId,
                            ammoInCharger = ammoInCharger,
                            totalAmmo = ammoNotInCharger
                        });

                    // Stop the reloading.
                    IsReloading = false;
                    // Update charger and total ammo count.
                    if (infinteAmmo)
                    {
                        ammoInCharger = chargerSize;
                    }
                    else if (ammoNotInCharger >= chargerSize - ammoInCharger)
                    {
                        ammoNotInCharger -= chargerSize - ammoInCharger;
                        ammoInCharger = chargerSize;
                    }
                    else
                    {
                        ammoInCharger += ammoNotInCharger;
                        ammoNotInCharger = 0;
                    }

                    // Set the ammo in the UI.
                    if (canDisplayUI) gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);
                }
            }
            else if (coolingDown)
            {
                if (Time.time > cooldownStart + cooldownTime) coolingDown = false;
            }
        }

        // Called by player, sets references to the game manager, to the player script itself and to 
        // the player UI.
        public void SetupGun(GameManager gms, Entity.Entity e, PlayerUIManager puims, int id)
        {
            ownerEntityScript = e;
            playerUIManagerScript = puims;

            ammoInCharger = chargerSize;
            ammoNotInCharger = maximumAmmo / 2 - chargerSize;
            defaultAmmoInCharger = ammoInCharger;
            defaultTotalAmmo = ammoNotInCharger;

            gunId = id;

            canDisplayUI = displayUIIfAvailable && playerUIManagerScript != null;
            if (canDisplayUI)
            {
                gunUIManagerScript = GetComponent<GunUIManager>();
                gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);
            }
        }

        // Called by the opponent, sets references to the game manager and to the player script itself.
        public void SetupGun(GameManager gms, Entity.Entity e)
        {
            ownerEntityScript = e;

            playerUIManagerScript = null;
            canDisplayUI = false;

            ammoInCharger = chargerSize;
            ammoNotInCharger = maximumAmmo / 2 - chargerSize;

            defaultAmmoInCharger = ammoInCharger;
            defaultTotalAmmo = ammoNotInCharger;
        }

        // I can reload when I have ammo left, my charger isn't full and I'm not reloading.
        public bool CanReload()
        {
            return (ammoNotInCharger > 0 || infinteAmmo) && ammoInCharger < chargerSize && !IsReloading;
        }

        // I can shoot when I'm not reloading and I'm not in cooldown.
        // FIXME is it ok if I report being able to shoot even if not equipped?
        public bool CanShoot()
        {
            return !IsReloading && !coolingDown && ammoInCharger > 0;
        }

        public bool CanAim()
        {
            return aimEnabled;
        }

        // Shots.
        public virtual void Shoot()
        {
            ShootingSoundGameEvent.Instance.Raise(new GunShootingSoundInfo
            {
                gunOwnerId = ownerEntityScript.GetID(),
                gunLoudness = shotNoise,
                gunPosition = transform.position,
            });
        }

        // Aims.
        public void Aim(bool aim)
        {
            isAiming = aim;
            animatingAim = true;
            aimStart = Time.time;

            if (!aim)
            {
                EnableAimOverlay(false);
                ownerEntityScript.SlowEntity(1);
                headCamera.fieldOfView = originalFOV;
            }
        }

        // Animates the aim.
        protected void AnimateAim()
        {
            if (isAiming)
            {
                ownerEntityScript.SlowEntity(0.4f);
                transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition,
                    (Time.time - aimStart) * 3f);
            }
            else
                transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero,
                    (Time.time - aimStart) * 6f);

            if (transform.localPosition == aimPosition && isAiming)
            {
                EnableAimOverlay(true);
                headCamera.fieldOfView = originalFOV / zoom;
                animatingAim = false;
            }
            else if (transform.localPosition == Vector3.zero && !isAiming)
            {
                animatingAim = false;
            }
        }

        // Enables or disables the aim overlay.
        protected void EnableAimOverlay(bool enabled)
        {
            if (overlayEnabled)
            {
                weaponCamera.enabled = !enabled;
                aimOverlay.enabled = enabled;
            }
        }

        // Resets the aim.
        protected void ResetAim()
        {
            EnableAimOverlay(false);
            headCamera.fieldOfView = originalFOV;
            transform.localPosition = Vector3.zero;
            ownerEntityScript.SlowEntity(1);
        }

        // Reloads.
        public void Reload()
        {
            SetReload();

            if (canDisplayUI)
            {
                gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);
                playerUIManagerScript.SetCooldown(reloadTime);
            }
        }

        // Starts the cooldown phase.
        protected void SetCooldown()
        {
            cooldownStart = Time.time;
            coolingDown = true;
        }

        // Starts the reload phase.
        protected void SetReload()
        {
            reloadStart = Time.time;
            IsReloading = true;
        }

        // Tells if the gun has the maximum number of ammo.
        public bool IsFull()
        {
            return ammoNotInCharger == maximumAmmo || infinteAmmo;
        }

        // Adds ammo.
        public void AddAmmo(int amount)
        {
            if (ammoNotInCharger + amount < maximumAmmo)
                ammoNotInCharger += amount;
            else
                ammoNotInCharger = maximumAmmo;

            if (gameObject.activeSelf && canDisplayUI)
            {
                gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);
                if (used && ammoInCharger == 0 && CanReload()) Reload();
            }
        }

        // Show muzzle flash.
        protected IEnumerator ShowMuzzleFlash()
        {
            // Move the gun downwards.
            transform.position = new Vector3(transform.position.x, transform.position.y + recoil,
                transform.position.z);
            // Rotate the muzzle flesh and show it.
            muzzleFlash.transform.RotateAround(muzzleFlash.transform.position, transform.forward,
                Random.Range(0f, 360f));
            muzzleFlash.SetActive(true);
            // Wait.
            yield return new WaitForSeconds(muzzleFlashDuration);
            // Move the gun upwards and hide the muzzle flash.
            transform.position = new Vector3(transform.position.x, transform.position.y - recoil,
                transform.position.z);
            muzzleFlash.SetActive(false);
            // Reload if needed.
            if (ammoInCharger == 0 && CanReload()) Reload();
        }

        // Activates/deactivates the children objects, with the exception of muzzle flashed which must
        // always be deactivated.
        private void SetChildrenEnabled(bool active)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(active);
        }

        // Resets the ammo.
        public void ResetAmmo()
        {
            ammoInCharger = defaultAmmoInCharger;
            ammoNotInCharger = defaultTotalAmmo;

            if (canDisplayUI) gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);
        }

        public int GetCurrentAmmo()
        {
            return infinteAmmo ? maximumAmmo : ammoNotInCharger;
        }

        public int GetMaxAmmo()
        {
            return maximumAmmo;
        }

        public int GetAmmoClipSize()
        {
            return chargerSize;
        }

        public int GetLoadedAmmo()
        {
            return ammoInCharger;
        }

        public bool IsAiming()
        {
            return isAiming;
        }
    }
}