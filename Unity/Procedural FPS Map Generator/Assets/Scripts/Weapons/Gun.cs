using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {

    [Header("Objects")] [SerializeField] private Camera headCamera;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject projectile;

    [Header("Gun parameters")] [SerializeField] private int damage = 10;
    [SerializeField] private float dispersion = 1;
    [SerializeField] private int projectilesPerShot = 1;
    [SerializeField] private int chargerSize;
    [SerializeField] private int maximumAmmo;
    [SerializeField] private bool limitRange = false;
    [SerializeField] private float range = 100f;
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private float cooldownTime = 0.1f;

    [Header("Gun UI")] [SerializeField] private bool hasUI = false;

    private GameObject projectileStorage;
    private List<GameObject> availableProjectiles;

    // Variables to manage ammo.
    private int ammoInCharger;
    private int totalAmmo;

    // Variables to manage cooldown and reload.
    private float cooldownStart;
    private float reloadStart;
    private bool coolingDown;
    private bool reloading;

    private GameManager gameManagerScript;
    private GunUIManager gunUIManagerScript;
    private PlayerUIManager playerUIManagerScript;
    private Entity ownerEntityScript;

    private void Update() {
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

    // Stops reloading.
    public void Stow() {
        // When I switch guns I stop the reloading, but not the cooldown.
        reloading = false;

        if (hasUI)
            playerUIManagerScript.StopReloading();
    }

    // Ends the reload or the cooldown phases if possible. 
    private void UpdateTimers() {
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
    private bool CanReload() {
        return totalAmmo > 0 && ammoInCharger < chargerSize && !reloading;
    }

    // I can shoot when I'm not reloading and I'm not in cooldown.
    private bool CanShoot() {
        return !reloading && !coolingDown;
    }

    // Shots.
    private void Shoot() {
        ammoInCharger -= 1;

        if (hasUI)
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);

        RaycastHit hit;
        if (limitRange) {
            // TODO - Implement dispersion and projectilesPerShot here, changing #raycast and their direction.
            if (Physics.Raycast(headCamera.transform.position, headCamera.transform.forward, out hit, range)) {
                // TODO - abstract here!
                Opponent opp = hit.transform.GetComponent<Opponent>();
                if (opp != null)
                    opp.TakeDamage(damage);
            }
        } else if (Physics.Raycast(headCamera.transform.position, headCamera.transform.forward, out hit)) {
            // TODO - abstract here!
            Opponent opp = hit.transform.GetComponent<Opponent>();
            if (opp != null)
                opp.TakeDamage(damage);
        }

        SetCooldown();
    }

    // Reloads.
    private void Reload() {
        SetReload();

        if (hasUI) {
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);
            playerUIManagerScript.SetCooldown(reloadTime);
        }
    }

    // Starts the cooldown phase.
    private void SetCooldown() {
        cooldownStart = Time.time;
        coolingDown = true;
    }

    // Starts the reload phase.
    private void SetReload() {
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

}