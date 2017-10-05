using UnityEngine;

public class AmmoPickable : Pickable {

    [SerializeField] private bool[] suppliedGuns;
    [SerializeField] private int[] ammoAmounts;

    protected override bool CanBePicked(GameObject player) {
        return player.GetComponent<PlayerController>().IsAnyEmpty(suppliedGuns);
    }

    protected override void PickUp(GameObject player) {
        player.GetComponent<PlayerController>().SupplyGuns(suppliedGuns, ammoAmounts);
    }

}