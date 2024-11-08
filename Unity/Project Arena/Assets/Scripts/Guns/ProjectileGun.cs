﻿using System.Collections.Generic;
using Logging;
using UnityEngine;

namespace Guns
{
    /// <summary>
    ///     ProjectileGun is an implementation of Gun. A projectile gun uses gameobjects with an attached
    ///     Projectile script as projectiles. Projectiles are stored in a queue and created only when
    ///     needed.
    /// </summary>
    public class ProjectileGun : Gun
    {
        [Header("Projectile parameters")] [SerializeField]
        private GameObject projectilePosition;

        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileLifeTime;
        [SerializeField] private float projectileSpeed;

        private readonly Queue<GameObject> projectileList = new Queue<GameObject>();
        private GameObject projectiles;
        private Transform t;

        public override float MaxRange => projectileLifeTime * projectileSpeed;

        public override bool IsProjectileWeapon => true;

        private void Start()
        {
            t = transform;
            projectiles = new GameObject("Projectiles - " + gameObject.name);
            projectiles.transform.localPosition = Vector3.zero;
        }

        private void OnDestroy()
        {
            Destroy(projectiles);
        }

        public override float GetProjectileSpeed()
        {
            return projectileSpeed;
        }

        public override void Shoot()
        {
            base.Shoot();
            #if !UNITY_SERVER || UNITY_EDITOR
            StartCoroutine(ShowMuzzleFlash());
            #endif

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
                    projectilesPerShot = projectilesPerShot,
                    direction = root.eulerAngles.y,
                    ownerId = ownerEntityScript.GetID(),
                    gunID = gunId,
                    ammoNotInCharger = ammoNotInCharger,
                });
            }

            if (canDisplayUI) gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);

            for (var i = 0; i < projectilesPerShot; i++)
            {
                Quaternion rotation;

                if (Dispersion != 0)
                    rotation = GetDeviatedRotation();
                else
                    rotation = t.rotation;

                InstantiateProjectile(rotation);
            }

            SetCooldown();
        }

        // Instantiate a projectile and shoot it.
        private void InstantiateProjectile(Quaternion rotation)
        {
            GameObject projectile;
            // Retrive a projectile from the list if possible, otherwise create a new one.
            if (projectileList.Count > 0)
            {
                projectile = projectileList.Dequeue();
                projectile.SetActive(true);
            }
            else
            {
                projectile = Instantiate(projectilePrefab);
                projectile.transform.parent = projectiles.transform;
                projectile.name = projectilePrefab.name;
                projectile.GetComponent<Projectile>().SetupProjectile(projectileLifeTime,
                    projectileSpeed, this, damage, ownerEntityScript.GetID());
            }

            // Place and fire the projectile.
            projectile.GetComponent<Projectile>().Fire(projectilePosition.transform.position, rotation);
        }

        // Adds a projectile back to the queue.
        public void RecoverProjectile(GameObject p)
        {
            projectileList.Enqueue(p);
        }

        // Deviates the rotation randomly inside a cone with the given aperture.
        private Quaternion GetDeviatedRotation()
        {
            var eulerRotation = t.rotation.eulerAngles;
            eulerRotation.x += Random.Range(-Dispersion / 2, Dispersion / 2);
            eulerRotation.y += Random.Range(-Dispersion / 2, Dispersion / 2);
            return Quaternion.Euler(eulerRotation);
        }

        public override Vector3 GetProjectileSpawnerForwardDirection()
        {
            return projectilePosition.transform.forward;
        }
    }
}