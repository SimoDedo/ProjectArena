using UnityEngine;

public class RaycastGun : Gun {

    [Header("Raycast parameters")] [SerializeField] private bool limitRange = false;
    [SerializeField] private float range = 100f;

    protected override void Shoot() {
        StartCoroutine(ShowMuzzleFlash());
        ammoInCharger -= 1;

        if (hasUI)
            gunUIManagerScript.SetAmmo(ammoInCharger, totalAmmo);

        RaycastHit hit;
        if (limitRange) {
            // TODO - Implement dispersion and projectilesPerShot here, changing #raycast and their direction.
            if (Physics.Raycast(headCamera.transform.position, headCamera.transform.forward, out hit, range)) {
                Opponent opp = hit.transform.GetComponent<Opponent>();
                if (opp != null)
                    opp.TakeDamage(damage, ownerEntityScript.GetID());
            }
        } else if (Physics.Raycast(headCamera.transform.position, headCamera.transform.forward, out hit)) {
            Opponent opp = hit.transform.GetComponent<Opponent>();
            if (opp != null)
                opp.TakeDamage(damage, ownerEntityScript.GetID());
        }

        SetCooldown();
    }

}
