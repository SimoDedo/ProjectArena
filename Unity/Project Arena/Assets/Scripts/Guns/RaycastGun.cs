﻿using System;
using System.Collections;
using System.Collections.Generic;
using Logging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Guns
{
    /// <summary>
    ///     RaycastGun is an implementation of Gun. Since the raycast gun uses a raycast to find the hit
    ///     position, there is no time of fligth for the bullet.
    /// </summary>
    public class RaycastGun : Gun
    {
        [Header("Raycast parameters")] [SerializeField]
        private bool limitRange;

        [SerializeField] private float range = 100f;
        [SerializeField] private GameObject sparkPrefab;
        [SerializeField] private float sparkDuration = 0.01f;
        private int layerMask;

        private readonly Queue<GameObject> sparkList = new Queue<GameObject>();
        private GameObject sparks;
        private Transform t;

        public override bool IsProjectileWeapon => false;
        public override float MaxRange => limitRange ? range : float.MaxValue;

        private void Start()
        {
            t = transform;
            if (!limitRange) range = Mathf.Infinity;

            layerMask = LayerMask.GetMask("Default", "Wall", "Floor", "Entity");

            var transform1 = transform;
            sparks = new GameObject("Sparks - " + transform1.gameObject.name);
            sparks.transform.parent = transform1;
            sparks.transform.localPosition = Vector3.zero;
        }

        public override float GetProjectileSpeed()
        {
            return float.PositiveInfinity;
        }

        public override Vector3 GetProjectileSpawnerForwardDirection()
        {
            throw new NotImplementedException();
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
                    projectilesPerShot = projectilesPerShot,
                    ammoInCharger = ammoInCharger,
                    direction = root.eulerAngles.y,
                    ownerId = ownerEntityScript.GetID(),
                    gunID = gunId,
                    ammoNotInCharger = ammoNotInCharger
                });
            }

            if (canDisplayUI) gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : ammoNotInCharger);

            for (var i = 0; i < projectilesPerShot; i++)
            {
                RaycastHit hit;
                Vector3 direction;

                if (Dispersion != 0)
                    direction = GetDeviatedDirection();
                else
                    direction = headCamera.transform.forward;

                // Debug.DrawRay(headCamera.transform.position, direction * 100, Color.blue, 5, false);
                
                if (Physics.Raycast(headCamera.transform.position, direction, out hit, range, layerMask))
                {
                    #if !UNITY_SERVER || UNITY_EDITOR
                    StartCoroutine(ShowSpark(hit));
                    #endif
                    var entityScript = hit.transform.root.GetComponent<Entity.Entity>();
                    if (entityScript != null)
                    {
                        entityScript.TakeDamage(damage, ownerEntityScript.GetID());
                    }
                }
            }

            SetCooldown();
        }

        // Show a spark at the hit point flash.
        private IEnumerator ShowSpark(RaycastHit hit)
        {
            GameObject spark;
            // Retrive a spark from the list if possible, otherwise create a new one.
            if (sparkList.Count > 0)
            {
                spark = sparkList.Dequeue();
                spark.SetActive(true);
            }
            else
            {
                spark = Instantiate(sparkPrefab, sparks.transform, true);
                spark.name = sparkPrefab.name;
            }

            // Place the spark.
            spark.transform.position = hit.point;
            spark.transform.rotation = Random.rotation;
            // Wait.
            yield return new WaitForSeconds(sparkDuration);
            // Hide the spark and put it back in the list.
            spark.SetActive(false);
            sparkList.Enqueue(spark);
        }

        // Deviates the direction randomly inside a cone with the given aperture.
        private Vector3 GetDeviatedDirection()
        {
            var direction = headCamera.transform.eulerAngles;
            direction.x += Random.Range(-Dispersion / 2, Dispersion / 2);
            direction.y += Random.Range(-Dispersion / 2, Dispersion / 2);
            return Quaternion.Euler(direction) * Vector3.forward;
        }
    }
}