using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Entity is an abstract class used to implement any kind of agent. An entity can spawn, recive
/// damage and die. An entity can also be healed, supplied with ammunitions and have weapons.
/// </summary>
[RequireComponent(typeof(PositionTracker))]
public abstract class Entity : MonoBehaviour
{
    [SerializeField] protected int totalHealth;
    [SerializeField] protected int disabledLayer;

    protected virtual List<bool> ActiveGuns
    {
        get { return Guns.Select(it => it.gameObject.activeSelf).ToList(); }
        set
        {
            for (var i = 0; i < Math.Min(Guns.Count, value.Count); i++)
                Guns[i].gameObject.SetActive(value[i]);
        }
    }

    protected virtual List<Gun> Guns { get; set; }
    public virtual int Health { get; protected set; }

    protected int entityID;
    protected int currentGun = 0;
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
    public bool isAlive => Health > 0;

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
    public bool CanBeSupplied(bool[] suppliedGuns)
    {
        if (inGame)
        {
            for (int i = 0; i < Math.Min(suppliedGuns.Length, Guns.Count); i++)
            {
                if (suppliedGuns[i] && ActiveGuns[i] && !Guns[i].IsFull())
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Increases the ammo of the available guns.
    public void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts)
    {
        for (int i = 0; i < suppliedGuns.GetLength(0); i++)
        {
            if (suppliedGuns[i] && ActiveGuns[i])
            {
                Guns[i].AddAmmo(ammoAmounts[i]);
            }
        }
    }

    // Sets if the entity is in game, i.e. if it can move, shoot, interact
    // with object and be hitten.
    public abstract void SetInGame(bool b);

    // Returns the ID of the entity.
    public int GetID()
    {
        return entityID;
    }

    // Sets if the entity must be ignored by raycast.
    protected void SetIgnoreRaycast(bool mustIgnore)
    {
        if (mustIgnore)
        {
            originalLayer = gameObject.layer;
            gameObject.layer = disabledLayer;
            ChangeLayersRecursively(transform, disabledLayer);
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

    // Resets the ammo of all the weapons.
    protected void ResetAllAmmo()
    {
        for (int i = 0; i < Guns.Count; i++)
        {
            if (ActiveGuns[i])
            {
                Guns[i].GetComponent<Gun>().ResetAmmo();
            }
        }
    }

    // Variables to slow down the gun switching.
    private float lastSwitched;
    private const float switchWait = 0.05f;

    /// <returns>True if the gun currently active is the one requested</returns>
    protected bool TrySwitchGuns(int toDeactivate, int toActivate)
    {
        if (Time.time > lastSwitched + switchWait)
        {
            if (toDeactivate != toActivate)
            {
                lastSwitched = Time.time;
                currentGun = toActivate;
                DeactivateGun(toDeactivate);
                ActivateGun(toActivate);
            }

            return true;
        }

        return toDeactivate == toActivate;
    }

    protected virtual void ActivateGun(int index)
    {
    }

    protected virtual void DeactivateGun(int index)
    {
    }
}