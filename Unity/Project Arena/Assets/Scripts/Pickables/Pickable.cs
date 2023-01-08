using UnityEngine;

namespace Pickables
{
    /// <summary>
    ///     Pickable is an abstract class used to implement any kind of object that can be picked up by
    ///     the player.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public abstract class Pickable : MonoBehaviour
    {
        public enum PickupType
        {
            MEDKIT,

            AMMO
        }

        private const float CHECK_WAIT = 0.1f;

        [SerializeField] private PickupType type;
        [SerializeField] protected float cooldown = 30f;
        [SerializeField] protected GameObject pickable;
        private float lastCheck;

        private float pickedUpTime;

        public bool IsActive { get; private set; } = true;

        public float Cooldown => cooldown;

        // Use this for initialization
        protected void Start()
        {
            ActivatePickable();
        }

        // Update is called once per frame
        protected void Update()
        {
            if (!IsActive && Time.time > pickedUpTime + cooldown) ActivatePickable();
        }

        protected void OnTriggerStay(Collider other)
        {
            // Menage the interaction with the player.
            if (other.gameObject.CompareTag("Player") && Time.time > lastCheck + CHECK_WAIT)
            {
                if (CanBePicked(other.gameObject) && IsActive)
                {
                    PickUp(other.gameObject);
                    DeactivatePickable();
                }

                lastCheck = Time.time;
            }
        }

        public PickupType GetPickupType()
        {
            return type;
        }

        // Tells if the player really needs the pickable.
        protected abstract bool CanBePicked(GameObject player);

        // Gives to the player the content of the pickable.
        protected abstract void PickUp(GameObject player);

        // Activates the pickable.
        private void ActivatePickable()
        {
            pickable.SetActive(true);
            IsActive = true;
        }

        // Deactivate the pickable.
        private void DeactivatePickable()
        {
            pickable.SetActive(false);
            pickedUpTime = Time.time;
            IsActive = false;
        }
    }
}