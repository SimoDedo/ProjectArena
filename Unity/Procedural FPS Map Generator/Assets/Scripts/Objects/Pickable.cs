using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public abstract class Pickable : MonoBehaviour {

    [SerializeField] protected float cooldown = 30f;
    [SerializeField] protected GameObject pickable;

    protected float pickedUpTime = 0f;
    protected bool isActive = true;

    protected bool defaultShaderSet = false;

    // Use this for initialization
    protected void Start() {
        ActivatePickable();
    }

    // Update is called once per frame
    protected void Update() {
        if (!isActive && Time.time > pickedUpTime + cooldown) {
            ActivatePickable();
        }
    }

    protected void OnTriggerEnter(Collider other) {
        // Menage the interaction with the player.
        if (other.gameObject.tag == "Player") {
            if (CanBePicked(other.gameObject) && isActive) {
                PickUp(other.gameObject);
                DeactivatePickable();
            }
        }
    }

    // Tells if the player really needs the pickable.
    abstract protected bool CanBePicked(GameObject player);

    // Gives to the player the content of the pickable.
    abstract protected void PickUp(GameObject player);

    // Activates the pickable.
    protected void ActivatePickable() {
        pickable.SetActive(true);
        isActive = true;
    }

    // Deactivate the pickable.
    protected void DeactivatePickable() {
        pickable.SetActive(false);
        pickedUpTime = Time.time;
        isActive = false;
    }

}