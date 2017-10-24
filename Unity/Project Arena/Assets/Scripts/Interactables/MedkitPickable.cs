using UnityEngine;

public class MedkitPickable : Pickable {

    [SerializeField] private int restoredHealth = 50;

    protected override bool CanBePicked(GameObject entity) {
        return entity.GetComponent<Entity>().CanBeHealed();
    }

    protected override void PickUp(GameObject entity) {
        entity.GetComponent<Entity>().Heal(restoredHealth);
    }

}