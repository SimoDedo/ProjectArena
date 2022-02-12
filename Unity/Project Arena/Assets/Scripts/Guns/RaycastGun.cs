using System.Collections;
using System.Collections.Generic;
using Logging;
using UnityEngine;

namespace Guns
{
    /// <summary>
    /// RaycastGun is an implementation of Gun. Since the raycast gun uses a raycast to find the hit 
    /// position, there is no time of fligth for the bullet.
    /// </summary>
    public class RaycastGun : Gun {

        [Header("Raycast parameters")] [SerializeField] private bool limitRange;
        [SerializeField] private float range = 100f;
        [SerializeField] private GameObject sparkPrefab;
        [SerializeField] private float sparkDuration = 0.01f;
        [SerializeField] private LayerMask ignoredLayers;

        private Queue<GameObject> sparkList = new Queue<GameObject>();
        private GameObject sparks;
        private Transform t;
        private void Start()
        {
            t = transform;
            if (!limitRange) {
                range = Mathf.Infinity;
            }

            ignoredLayers = ~ignoredLayers;

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
            throw new System.NotImplementedException();
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
                    ownerId = ownerEntityScript.GetID(),
                    gunID = gunId,
                    totalAmmo = totalAmmo
                });
            }

            if (canDisplayUI) {
                gunUIManagerScript.SetAmmo(ammoInCharger, infinteAmmo ? -1 : totalAmmo);
            }

            for (int i = 0; i < projectilesPerShot; i++) {
                RaycastHit hit;
                Vector3 direction;

                if (dispersion != 0) {
                    direction = GetDeviatedDirection(headCamera.transform.forward, dispersion);
                } else {
                    direction = headCamera.transform.forward;
                }

                if (Physics.Raycast(headCamera.transform.position, direction, out hit, range,
                    ignoredLayers)) {
                    var entityScript = hit.transform.root.GetComponent<Entity.Entity>();
                    if (entityScript != null) {
                        StartCoroutine(ShowSpark(hit));
                        entityScript.TakeDamage(damage, ownerEntityScript.GetID());
                    }
                }
            }

            SetCooldown();
        }

        public override bool IsProjectileWeapon => false;
        public override float MaxRange => range;

        // Show a spark at the hit point flash.
        private IEnumerator ShowSpark(RaycastHit hit) {
            GameObject spark;
            // Retrive a spark from the list if possible, otherwise create a new one.
            if (sparkList.Count > 0) {
                spark = sparkList.Dequeue();
                spark.SetActive(true);
            } else {
                spark = (GameObject)Instantiate(sparkPrefab, sparks.transform, true);
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
        private Vector3 GetDeviatedDirection(Vector3 direction, float deviation) {
            direction = headCamera.transform.eulerAngles;
            direction.x += Random.Range(-dispersion / 2, dispersion / 2);
            direction.y += Random.Range(-dispersion / 2, dispersion / 2);
            return Quaternion.Euler(direction) * Vector3.forward;
        }

    }
}