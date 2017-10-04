using System;
using System.Collections.Generic;
using UnityEngine;

public class Firearm : MonoBehaviour {

    [SerializeField] private Camera headCamera;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject projectile;

    [SerializeField] private int damage = 10;
    [SerializeField] private float range = 100f;
    [SerializeField] private float dispersion = 100f;
    [SerializeField] private int projectilesPerShot = 1;
    [SerializeField] private int chargerSize = 12;
    [SerializeField] private int maximumAmmo = 60;
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private float cooldownTime = 0.1f;

    private GameObject projectileStorage;
    private List<GameObject> availableProjectiles;

    private int ammoInCharger;
    private int totalAmmo;

    private float cooldownStart;
    private float reloadStart;
    private bool coolingDown;
    private bool reloading;

    private bool enemyDetected = false;

    private void Start() {
        ammoInCharger = chargerSize;
        totalAmmo = maximumAmmo / 2 - chargerSize;
    }

    private void Update() {
        UpdateCrosshair();

        if (reloading || coolingDown)
            UpdateTimers();

        if (Input.GetButtonDown("Fire1") && CanShoot()) {
            if (ammoInCharger > 0)
                Shoot();
            else if (CanReload())
                Reload();
        } else if (Input.GetButtonDown("Reload") && CanReload()) {
            Reload();
        }
    }

    // Ends the reload or the cooldown phases if possible. 
    private void UpdateTimers() {
        if (reloading) {
            if (Time.time > reloadStart + reloadTime)
                reloading = false;
        } else if (coolingDown) {
            if (Time.time > cooldownStart + cooldownTime)
                reloading = false;
        }
    }

    // I can reload when I have ammo left, my charger isn't full and I'm not reloading.
    private bool CanReload() {
        return totalAmmo > 0 && ammoInCharger < chargerSize && !reloading;
    }

    // I can shoot when I'm not reloading and I'm not in cooldown. I do't check the 
    // ammo because I check it later in order to recharge if possible.
    private bool CanShoot() {
        return !reloading && !coolingDown;
    }

    // Updates the crosshair if an enemy is detected.
    private void UpdateCrosshair() {
        RaycastHit hit;

        if (Physics.Raycast(headCamera.transform.position, headCamera.transform.forward, out hit, range)) {
            if (hit.transform.root.tag == "Opponent" && !enemyDetected) {
                // TODO: set the crosshair to red.
                enemyDetected = true;
            } else if (enemyDetected) {
                // TODO: set the crosshair to white.
                enemyDetected = false;
            }
        }
    }

    // Shots.
    private void Shoot() {
        // TODO: shoot.

        SetCooldown();
    }

    // Reloads.
    private void Reload() {
        // TODO: reload.

        SetReload();
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