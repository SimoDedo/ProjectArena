using UnityEngine;

/// <summary>
/// Entity is an abstract class used to implement any kind of agent. An entity can spawn, recive
/// damage and die. An entity can also be healed, supplied with ammunitions and have weapons.
/// </summary>
[RequireComponent(typeof(PositionTracker))]
public abstract class Entity : MonoBehaviour
{
    [SerializeField] protected int totalHealth;

    // TODO Move to subclasses, no reason for this to be public
    public virtual int Health { get; protected set; }

    protected int entityID;
    protected bool inGame = false;
    protected int originalLayer;

    protected GameManager gameManagerScript;

    // Sets all the entity parameters.
    public abstract void SetupEntity(int th, bool[] ag, GameManager gms, int id);

    // Applies damage to the entity and eventually manages its death.
    public abstract void TakeDamage(int damage, int killerID);

    // Kills the entity.
    protected abstract void Die(int id);

    // Respawns the entity.
    public abstract void Respawn();

    // Returns whether the entity is in game or not
    public bool isAlive => isActiveAndEnabled && Health > 0;

    // Slows down the entity.
    public abstract void SlowEntity(float penalty);

    // If the entity is enabled, tells if the it has full health.
    public bool CanBeHealed()
    {
        return Health < totalHealth && inGame;
    }

    // Heals the entity.
    public abstract void HealFromMedkit(MedkitPickable medkit);

    // If the entity is enabled, tells if any of the weapons passed as 
    // parameters hasn't the maximum ammo.
    public abstract bool CanBeSupplied(bool[] suppliedGuns);

    // Increases the ammo of the available guns.
    public abstract void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts);
    
    // Sets if the entity is in game, i.e. if it can move, shoot, interact
    // with object and be hit.
    public abstract void SetInGame(bool b);

    // Returns the ID of the entity.
    public int GetID()
    {
        return entityID;
    }

    // Sets if the entity must be ignored by raycast.
    public void SetIgnoreRaycast(bool mustIgnore)
    {
        if (mustIgnore)
        {
            originalLayer = gameObject.layer;
            var ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            gameObject.layer = ignoreLayer;
            ChangeLayersRecursively(transform, ignoreLayer);
        }
        else
        {
            gameObject.layer = originalLayer;
            ChangeLayersRecursively(transform, originalLayer);
        }
    }

    // Changes the layer recursively.
    protected void ChangeLayersRecursively(Transform t, int l)
    {
        foreach (Transform child in t)
        {
            child.gameObject.layer = l;
            ChangeLayersRecursively(child, l);
        }
    }
}