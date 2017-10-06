using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour {

    // Guns.
    [SerializeField] protected List<GameObject> guns;

    // Informations about the entity.
    protected bool[] activeGuns;
    protected int totalHealth;
    protected int health;
    protected int currentGun;
    protected bool inGame = false;

    protected GameManager gameManagerScript;

    // Sets all the entity parameters.
    public void SetupEntity(int th, bool[] agp, GameManager gms) {
        activeGuns = agp;
        gameManagerScript = gms;

        totalHealth = th;
        health = th;
        gameManagerScript.SetHealth(health, totalHealth);

        for (int i = 0; i < agp.GetLength(0); i++) {
            // Setup the gun.
            guns[i].GetComponent<Gun>().SetupGun(gms);
            // Activate it if is one among the active ones which has the lowest rank.
            if (i == GetActiveGun(0, true)) {
                currentGun = i;
                guns[i].SetActive(true);
                guns[i].GetComponent<Gun>().Wield();
                gameManagerScript.SetCurrentGun(i);
            }
        }
    }

    // Applies damage to the entity and eventually manages its death.
    public void TakeDamage(int damage) {
        health -= damage;

        // If the health goes under 0, kill the entity and start the respawn process.
        if (health <= 0f) {
            health = 0;
            // Kill the entity.
            Die();
            // Start the respawn process.
            StartCoroutine(gameManagerScript.WaitForRespawn(gameObject, this));
        }
    }

    // Kills the entity.
    protected abstract void Die();

    // Respawn the entity.
    public abstract void Respawn();

    // Returns the next or the previous active gun.
    protected int GetActiveGun(int currentGun, bool next) {
        if (next) {
            // Try for the guns after it
            for (int i = currentGun + 1; i < guns.Count; i++) {
                if (activeGuns[i])
                    return i;
            }
            // Try for the guns before it
            for (int i = 0; i < currentGun; i++) {
                if (activeGuns[i])
                    return i;
            }
            // There's no other gun, return itself.
            return currentGun;
        } else {
            // Try for the guns before it
            for (int i = currentGun - 1; i >= 0; i--) {
                if (activeGuns[i])
                    return i;
            }
            // Try for the guns after it
            for (int i = guns.Count - 1; i > currentGun; i--) {
                if (activeGuns[i])
                    return i;
            }
            // There's no other gun, return itself.
            return currentGun;
        }
    }

    // If the entity is enabled, tells if the it has full health.
    public bool CanBeHealed() {
        return health < totalHealth && inGame;
    }

    // Heals the entity.
    public void Heal(int restoredHealth) {
        if (health + restoredHealth > totalHealth)
            health = totalHealth;
        else
            health += restoredHealth;

        gameManagerScript.SetHealth(health, totalHealth);
    }

    // If the entity is enabled, tells if any of the weapons passed as parameters hasn't the maximum ammo.
    public bool CanBeSupllied(bool[] suppliedGuns) {
        if (inGame) {
            for (int i = 0; i < suppliedGuns.GetLength(0); i++) {
                if (suppliedGuns[i] && activeGuns[i] && !guns[i].GetComponent<Gun>().IsFull())
                    return true;
            }
        }
        return false;
    }

    // Increases the ammo of the available guns.
    public void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts) {
        for (int i = 0; i < suppliedGuns.GetLength(0); i++) {
            if (suppliedGuns[i] && activeGuns[i] && !guns[i].GetComponent<Gun>().IsFull())
                guns[i].GetComponent<Gun>().AddAmmo(ammoAmounts[i]);
        }
    }

    // Sets if the entity is in game, i.e. if it can move, shoot, interact with object and be hitten.
    abstract public void SetInGame(bool b);

}