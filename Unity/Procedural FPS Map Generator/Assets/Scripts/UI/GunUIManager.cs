using UnityEngine;
using UnityEngine.UI;

public class GunUIManager : MonoBehaviour {

    [SerializeField] private GameObject ammo;

    // Sets the ammo.
    public void SetAmmo(int charger, int tot) {
        ammo.GetComponent<Text>().text = charger.ToString() + "/" + tot.ToString();
    }

}