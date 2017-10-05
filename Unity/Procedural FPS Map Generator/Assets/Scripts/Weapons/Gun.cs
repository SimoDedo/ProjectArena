using System;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {

    [SerializeField] private Camera headCamera;

    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject projectile;

    [SerializeField] private int damage = 10;
    [SerializeField] private float dispersion = 1;
    [SerializeField] private int projectilesPerShot = 1;
    [SerializeField] private int chargerSize;
    [SerializeField] private int maximumAmmo;
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private float cooldownTime = 0.1f;

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

    // Shows the gun info in the UI:
    public void Wield() {
        // Set the ammo in the UI.
        gameManagerScript.SetAmmo(ammoInCharger, totalAmmo);
    }

    // Hides the gun info from the UI.
    public void Stow() {
        // When I switch guns I stop the reloading, but not the cooldown.
        reloading = false;
        gameManagerScript.StopReloading();
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
                gameManagerScript.SetAmmo(ammoInCharger, totalAmmo);
            }
        } else if (coolingDown) {
            if (Time.time > cooldownStart + cooldownTime)
                coolingDown = false;
        }
    }

    // Sets the game manager reference, the ammo and updates them in the UI.
    public void SetupGun(GameManager gms) {
        gameManagerScript = gms;

        ammoInCharger = chargerSize;
        totalAmmo = maximumAmmo / 2 - chargerSize;
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
        gameManagerScript.SetAmmo(ammoInCharger, totalAmmo);

        // TODO - Spawn projectiles.

        SetCooldown();
    }

    // Reloads.
    private void Reload() {
        SetReload();
        gameManagerScript.StartReloading(reloadTime);
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

}