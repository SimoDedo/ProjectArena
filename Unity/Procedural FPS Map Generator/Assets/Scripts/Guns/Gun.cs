using System.Collections;
using UnityEngine;

public abstract class Gun : MonoBehaviour {

    [Header("Objects")] [SerializeField] protected Camera headCamera;
    [SerializeField] protected GameObject muzzleFlash;

    [Header("Gun parameters")] [SerializeField] protected int damage = 10;
    [SerializeField] protected float dispersion = 0f;
    [SerializeField] protected int projectilesPerShot = 1;
    [SerializeField] protected int chargerSize;
    [SerializeField] protected int maximumAmmo;
    [SerializeField] protected float reloadTime = 1f;
    [SerializeField] protected float cooldownTime = 0.1f;

    [Header("Appearence")] [SerializeField] protected float muzzleFlashDuration = 0.05f;
    [SerializeField] protected float recoil = 0.05f;

    [Header("UI")]
    [SerializeField]
    protected bool hasUI = false;

    // Variables to manage ammo.
    protected int ammoInCharger;
    protected int totalAmmo;

    // Variables to manage cooldown and reload.
    protected float cooldownStart;
    protected float reloadStart;
    protected bool coolingDown;
    protected bool reloading;

    protected GameManager gameManagerScript;
    protected GunUIManager gunUIManagerScript;
    protected PlayerUIManager playerUIManagerScript;
    protected Entity ownerEntityScript;

    // Is teh gun being used?
    protected bool used = false;

    protected void Update() {
        if (used) {
            if (reloading || coolingDown)
                UpdateTimers();

            if (Input.GetButton("Fire1") && CanShoot()) {
                if (ammoInCharger > 0)
                    Shoot();
                else if (CanReload())
                    Reload();
            } else if (Input.GetButtonDown("Reload") && CanReload()) {
                Reload();
            }
        }
    }

    protected void OnDisable() {
        if (muzzleFlash.activeSelf)
            muzzleFlash.SetActive(false);
    }

    // Allows accepting input and enables all the childrens.
    public void Wield() {
        SetChildrenEnabled(true);
        muzzleFlash.SetActive(false);
        used = true;
    }

    // Stops reloading, disallows accepting input and disables all the childrens.
    public void Stow() {
        // When I switch guns I stop the reloading, but not the cooldown.
        reloading = false;

        if (hasUI)
            playerUIManagerScript.StopReloading();

        SetChildrenEnabled(false);
        used = false;
    }

    // Ends the reload or the cooldown phases if possible. 
    protected void UpdateTimers() {
        if (reloading) {
            if (Time.time > reloadStart + reloadTime) {
                // Stop the reloading.
                reloading = false;
                // Update charger and total ammo count.
                if (totalAmmo >= chargerSize - ammoInCharger) {
                    totalAmmo -= chargerSize - ammoInCharger;
                    ammoInCharger = chargerSize;
                } else {
                    ammoInCharger = ammoInCharger + totalAmmo;
                    totalAmmo = 0;
                }
                // Set the ammo in the UI.
                if (hasUI)
                    gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);
            }
        } else if (coolingDown) {
            if (Time.time > cooldownStart + cooldownTime)
                coolingDown = false;
        }
    }

    // Called by player, sets references to the game manager, to the player script itself and to the player UI.
    public void SetupGun(GameManager gms, Entity e, PlayerUIManager puims) {
        gameManagerScript = gms;
        ownerEntityScript = e;
        playerUIManagerScript = puims;

        ammoInCharger = chargerSize;
        totalAmmo = maximumAmmo / 2 - chargerSize;

        if (hasUI) {
            gunUIManagerScript = GetComponent<GunUIManager>();
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);
        }
    }

    // Called by the opponent, sets references to the game manager and to the player script itself.
    public void SetupGun(GameManager gms, Entity e) {
        gameManagerScript = gms;
        ownerEntityScript = e;

        playerUIManagerScript = null;
        hasUI = false;

        ammoInCharger = chargerSize;
        totalAmmo = maximumAmmo / 2 - chargerSize;

        if (hasUI)
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);
    }

    // I can reload when I have ammo left, my charger isn't full and I'm not reloading.
    protected bool CanReload() {
        return totalAmmo > 0 && ammoInCharger < chargerSize && !reloading;
    }

    // I can shoot when I'm not reloading and I'm not in cooldown.
    protected bool CanShoot() {
        return !reloading && !coolingDown;
    }

    // Shots.
    protected abstract void Shoot();

    // Reloads.
    protected void Reload() {
        SetReload();

        if (hasUI) {
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);
            playerUIManagerScript.SetCooldown(reloadTime);
        }
    }

    // Starts the cooldown phase.
    protected void SetCooldown() {
        cooldownStart = Time.time;
        coolingDown = true;
    }

    // Starts the reload phase.
    protected void SetReload() {
        reloadStart = Time.time;
        reloading = true;
    }

    // Tells if the gun has the maximum number of ammo.
    public bool IsFull() {
        return totalAmmo == maximumAmmo;
    }

    // Adds ammo.
    public void AddAmmo(int amount) {
        if (totalAmmo + amount < maximumAmmo)
            totalAmmo += amount;
        else
            totalAmmo = maximumAmmo;

        if (gameObject.activeSelf && hasUI)
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);
    }

    // Show muzzle flash.
    protected IEnumerator ShowMuzzleFlash() {
        // Move the gun downwards.
        transform.position = new Vector3(transform.position.x, transform.position.y + recoil, transform.position.z);
        // Rotate the muzzle flesh and show it.
        muzzleFlash.transform.RotateAround(muzzleFlash.transform.position, transform.forward, Random.Range(0f, 360f));
        muzzleFlash.SetActive(true);
        // Wait.
        yield return new WaitForSeconds(muzzleFlashDuration);
        // Move the gun upwards and hide the muzzle flash.
        transform.position = new Vector3(transform.position.x, transform.position.y - recoil, transform.position.z);
        muzzleFlash.SetActive(false);
    }

    // Activates/deactivates the children objects, with the exception of muzzle flashed which must always be deactivated.
    private void SetChildrenEnabled(bool active) {
        foreach (Transform child in transform) {
            child.gameObject.SetActive(active);
        }
    }

}