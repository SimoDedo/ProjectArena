using UnityEngine;

public class MedkitPickable : Pickable {

    [SerializeField] private int restoredHealth = 50;

    protected override bool CanBePicked(GameObject player) {
        return !player.GetComponent<PlayerController>().IsHealthFull();
    }

    protected override void PickUp(GameObject player) {
        player.GetComponent<PlayerController>().Heal(restoredHealth);
    }

}