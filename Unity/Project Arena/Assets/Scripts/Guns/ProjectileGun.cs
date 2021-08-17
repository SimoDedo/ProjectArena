using System.Collections.Generic;
using ScriptableObjectArchitecture;
using UnityEngine;

/// <summary>
/// ProjectileGun is an implementation of Gun. A projectile gun uses gameobjects with an attached 
/// Projectile script as projectiles. Projectiles are stored in a queue and created only when 
/// needed.
/// </summary>
public class ProjectileGun : Gun {

    [Header("Projectile parameters")] [SerializeField] private GameObject projectilePosition;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileLifeTime;
    [SerializeField] private float projectileSpeed;

    private Queue<GameObject> projectileList = new Queue<GameObject>();
    private GameObject projectiles;
    private Transform t;
    private void Start()
    {
        t = transform;
        projectiles = new GameObject("Projectiles - " + gameObject.name);
        projectiles.transform.localPosition = Vector3.zero;
    }

    public override void Shoot() {
        StartCoroutine(ShowMuzzleFlash());

        ammoInCharger -= 1;

        // Log if needed.
        if (loggingGame)
        {
            var root = t.root;
            var position = root.position;
            ShotInfoGameEvent.Instance.Raise(new ShotInfo
            {
                x = position.x,
                z = position.z,
                ammoInCharger = ammoInCharger,
                direction = root.eulerAngles.y,
                gunID = gunId,
                totalAmmo = totalAmmo
            });
        }

        if (hasUI) {
            gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
        }

        for (int i = 0; i < projectilesPerShot; i++) {
            Quaternion rotation;

            if (dispersion != 0) {
                rotation = GetDeviatedRotation(t.rotation, dispersion);
            } else {
                rotation = t.rotation;
            }

            InstantiateProjectile(rotation);
        }

        SetCooldown();
    }

    // Instantiate a projectile and shoot it.
    private void InstantiateProjectile(Quaternion rotation) {
        GameObject projectile;
        // Retrive a projectile from the list if possible, otherwise create a new one.
        if (projectileList.Count > 0) {
            projectile = projectileList.Dequeue();
            projectile.SetActive(true);
        } else {
            projectile = (GameObject)Instantiate(projectilePrefab);
            projectile.transform.parent = projectiles.transform;
            projectile.name = projectilePrefab.name;
            projectile.GetComponent<Projectile>().SetupProjectile(projectileLifeTime,
                projectileSpeed, this, damage, ownerEntityScript.GetID());
        }
        // Place and fire the projectile.
        projectile.GetComponent<Projectile>().Fire(projectilePosition.transform.position, rotation);
    }

    // Adds a projectile back to the queue.
    public void RecoverProjectile(GameObject p) {
        projectileList.Enqueue(p);
    }

    // Deviates the rotation randomly inside a cone with the given aperture.
    private Quaternion GetDeviatedRotation(Quaternion rotation, float deviation) {
        Vector3 eulerRotation = rotation.eulerAngles;
        eulerRotation.x += Random.Range(-dispersion / 2, dispersion / 2);
        eulerRotation.y += Random.Range(-dispersion / 2, dispersion / 2);
        return Quaternion.Euler(eulerRotation);
    }

}