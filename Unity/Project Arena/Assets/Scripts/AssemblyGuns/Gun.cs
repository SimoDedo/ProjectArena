using System.Collections;
using AssemblyLogging;
using UnityEngine;
using UnityEngine.UI;

// TODO add type (blast vs non-blast)

/// <summary>
/// Gun is an abstract class used to implement any kind of ranged weapon.
/// </summary>
public abstract class Gun : MonoBehaviour, ILoggable
{
    [Header("Objects")] [SerializeField] protected Camera headCamera;
    [SerializeField] protected GameObject muzzleFlash;

    [Header("Gun parameters")] [SerializeField]
    protected int damage = 10;

    [SerializeField] protected float dispersion = 0f;
    [SerializeField] protected int projectilesPerShot = 1;
    [SerializeField] protected bool infinteAmmo = false;
    [SerializeField] protected int chargerSize;
    [SerializeField] protected int maximumAmmo;
    [SerializeField] protected float reloadTime = 1f;

    [SerializeField] protected float cooldownTime = 0.1f;

    [SerializeField] private bool isBlastWeapon;
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

    [Header("Appearence")] [SerializeField]
    protected float muzzleFlashDuration = 0.05f;

    [SerializeField] protected float recoil = 0.05f;

    [Header("Aim")] [SerializeField] protected bool aimEnabled = false;
    [SerializeField] protected bool overlayEnabled = false;
    [SerializeField] protected float zoom = 1f;
    [SerializeField] protected Camera weaponCamera;
    [SerializeField] protected Vector3 aimPosition;
    [SerializeField] protected Image aimOverlay;

    [Header("UI")] 
    [SerializeField] protected bool displayUIIfAvailable = true;

    protected bool canDisplayUI;

    // Default ammo.
    private int defaultAmmoInCharger;
    private int defaultTotalAmmo;

    // Variables to manage ammo.
    protected int ammoInCharger;
    protected int totalAmmo;

    // Variables to manage cooldown and reload.
    private float cooldownStart;
    private float reloadStart;
    private bool coolingDown;
    private bool reloading;

    // Variables to manage the aim.
    private bool aiming;
    private bool animatingAim;
    private float aimStart;
    private float originalFOV;

    protected GunUIManager gunUIManagerScript;
    private PlayerUIManager playerUIManagerScript;
    protected Entity ownerEntityScript;

    // Is the gun being used?
    private bool used;

    // Is the input enabled?
    // Gun identifier.
    protected int gunId;

    // Do I have to log?
    protected bool loggingGame = true;

    public bool IsReloading => reloading;

    protected void Awake()
    {
        if (aimEnabled)
        {
            originalFOV = headCamera.fieldOfView;
        }
    }

    protected void Update()
    {
        if (used)
        {
            if (reloading || coolingDown)
            {
                UpdateTimers();
            }

            if (animatingAim)
            {
                AnimateAim();
            }
        }
    }

    protected void OnDisable()
    {
        if (aimEnabled)
        {
            ResetAim();
        }

        if (muzzleFlash.activeSelf)
        {
            muzzleFlash.SetActive(false);
        }
    }

    // Allows accepting input and enables all the childrens.
    public void Wield()
    {
        SetChildrenEnabled(true);
        muzzleFlash.SetActive(false);
        used = true;

        if (ammoInCharger == 0 && CanReload())
        {
            Reload();
        }
    }

    // Stops reloading, stops aiming, disallows accepting input and disables all the childrens.
    public void Stow()
    {
        // When I switch guns I stop the reloading, but not the cooldown.
        reloading = false;

        if (canDisplayUI)
        {
            playerUIManagerScript.StopReloading();
        }

        if (aimEnabled)
        {
            ResetAim();
        }

        SetChildrenEnabled(false);
        used = false;
    }

    // Ends the reload or the cooldown phases if possible. 
    private void UpdateTimers()
    {
        if (reloading)
        {
            if (Time.time > reloadStart + reloadTime)
            {
                // Log if needed.
                if (loggingGame)
                {
                    ReloadInfoGameEvent.Instance.Raise(new ReloadInfo
                    {
                        ownerId = ownerEntityScript.GetID(),
                        gunId = gunId,
                        ammoInCharger = ammoInCharger,
                        totalAmmo = totalAmmo
                    });
                }

                // Stop the reloading.
                reloading = false;
                // Update charger and total ammo count.
                if (infinteAmmo)
                {
                    ammoInCharger = chargerSize;
                }
                else if (totalAmmo >= chargerSize - ammoInCharger)
                {
                    totalAmmo -= chargerSize - ammoInCharger;
                    ammoInCharger = chargerSize;
                }
                else
                {
                    ammoInCharger += totalAmmo;
                    totalAmmo = 0;
                }

                // Set the ammo in the UI.
                if (canDisplayUI)
                {
                    gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
                }
            }
        }
        else if (coolingDown)
        {
            if (Time.time > cooldownStart + cooldownTime)
            {
                coolingDown = false;
            }
        }
    }

    // Called by player, sets references to the game manager, to the player script itself and to 
    // the player UI.
    public void SetupGun(GameManager gms, Entity e, PlayerUIManager puims, int id)
    {
        ownerEntityScript = e;
        playerUIManagerScript = puims;

        ammoInCharger = chargerSize;
        totalAmmo = maximumAmmo / 2 - chargerSize;
        defaultAmmoInCharger = ammoInCharger;
        defaultTotalAmmo = totalAmmo;

        gunId = id;

        canDisplayUI = displayUIIfAvailable && playerUIManagerScript != null; 
        if (canDisplayUI)
        {
            gunUIManagerScript = GetComponent<GunUIManager>();
            gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
        }
    }

    // Called by the opponent, sets references to the game manager and to the player script itself.
    public void SetupGun(GameManager gms, Entity e)
    {
        ownerEntityScript = e;

        playerUIManagerScript = null;
        canDisplayUI = false;

        ammoInCharger = chargerSize;
        totalAmmo = maximumAmmo / 2 - chargerSize;

        defaultAmmoInCharger = ammoInCharger;
        defaultTotalAmmo = totalAmmo;
    }

    // I can reload when I have ammo left, my charger isn't full and I'm not reloading.
    public bool CanReload()
    {
        return (totalAmmo > 0 || infinteAmmo) && ammoInCharger < chargerSize && !reloading;
    }

    // I can shoot when I'm not reloading and I'm not in cooldown.
    // FIXME is it ok if I report being able to shoot even if not equipped?
    public bool CanShoot()
    {
        return !reloading && !coolingDown && ammoInCharger > 0;
    }

    public bool CanAim()
    {
        return aimEnabled;
    }

    // Shots.
    public abstract void Shoot();

    // Aims.
    public void Aim(bool aim)
    {
        aiming = aim;
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
        if (aiming)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition,
                (Time.time - aimStart) * 10f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero,
                (Time.time - aimStart) * 10f);
        }

        if (transform.localPosition == aimPosition && aiming)
        {
            EnableAimOverlay(true);
            ownerEntityScript.SlowEntity(0.4f);
            headCamera.fieldOfView = originalFOV / zoom;
            animatingAim = false;
        }
        else if (transform.localPosition == Vector3.zero && !aiming)
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
            gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
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
        reloading = true;
    }

    // Tells if the gun has the maximum number of ammo.
    public bool IsFull()
    {
        return totalAmmo == maximumAmmo || infinteAmmo;
    }

    // Adds ammo.
    public void AddAmmo(int amount)
    {
        if (totalAmmo + amount < maximumAmmo)
        {
            totalAmmo += amount;
        }
        else
        {
            totalAmmo = maximumAmmo;
        }

        if (gameObject.activeSelf && canDisplayUI)
        {
            gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
            if (used && ammoInCharger == 0 && CanReload())
            {
                Reload();
            }
        }
    }

    public bool IsBlastWeapon => isBlastWeapon;

    public abstract bool IsProjectileWeapon
    {
        get;
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
        if (ammoInCharger == 0 && CanReload())
        {
            Reload();
        }
    }

    // Activates/deactivates the children objects, with the exception of muzzle flashed which must
    // always be deactivated.
    private void SetChildrenEnabled(bool active)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    // Resets the ammo.
    public void ResetAmmo()
    {
        ammoInCharger = defaultAmmoInCharger;
        totalAmmo = defaultTotalAmmo;

        if (canDisplayUI)
        {
            gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
        }
    }

    // Setups stuff for the loggingGame.
    public void SetupLogging()
    {
        loggingGame = true;
    }

    public int GetCurrentAmmo()
    {
        return infinteAmmo ? maximumAmmo : totalAmmo;
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

    public abstract float MaxRange { get; } 
}