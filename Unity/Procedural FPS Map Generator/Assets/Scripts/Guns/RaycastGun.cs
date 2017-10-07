using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastGun : Gun {

    [Header("Raycast parameters")] [SerializeField] private bool limitRange = false;
    [SerializeField] private float range = 100f;
    [SerializeField] private GameObject sparkPrefab;

    private Queue<GameObject> sparkList = new Queue<GameObject>();

    protected override void Shoot() {
        StartCoroutine(ShowMuzzleFlash());
        ammoInCharger -= 1;

        if (hasUI)
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);

        for (int i = 0; i < projectilesPerShot; i++) {
            RaycastHit hit;
            Vector3 direction;

            if (dispersion != 0)
                direction = GetDeviatedDirection(headCamera.transform.forward, dispersion);
            else
                direction = headCamera.transform.forward;

            if ((limitRange && Physics.Raycast(headCamera.transform.position, direction, out hit, range)) || (!limitRange && Physics.Raycast(headCamera.transform.position, direction, out hit))) {
                StartCoroutine(ShowSpark(hit));
                Opponent opp = hit.transform.GetComponent<Opponent>();
                if (opp != null)
                    opp.TakeDamage(damage, ownerEntityScript.GetID());
            }
        }

        SetCooldown();
    }

    // Show a spark at the hit point flash.
    protected IEnumerator ShowSpark(RaycastHit hit) {
        GameObject spark;
        // Retrive a spark from the list if possible, otherwise create a new one.
        if (sparkList.Count > 0) {
            spark = sparkList.Dequeue();
            spark.SetActive(true);
        } else {
            spark = (GameObject)Instantiate(sparkPrefab);
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

}