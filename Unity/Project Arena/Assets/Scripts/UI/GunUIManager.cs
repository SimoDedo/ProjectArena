using UnityEngine;
using UnityEngine.UI;

public class GunUIManager : MonoBehaviour {

    [SerializeField] private GameObject ammo;

    // Sets the ammo.
    public void SetAmmo(int charger, int tot) {
        if (tot == -1)
            ammo.GetComponent<Text>().text = charger.ToString() + "/∞";
        else
            ammo.GetComponent<Text>().text = charger.ToString() + "/" + tot.ToString();
    }

}