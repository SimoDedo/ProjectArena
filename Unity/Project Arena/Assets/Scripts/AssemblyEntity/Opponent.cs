using System;
using System.Linq;
using UnityEngine;
using Utils;

/// <summary>
/// Opponent is a simple implementation of Entity with no extra logic.
/// </summary>
public class Opponent : Entity {
    private void Awake()
    {
        Guns = gameObject.GetComponentsInChildren<Gun>().ToList();
    }

    // Sets up all the opponent parameters and does the same with all its guns.
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        ActiveGuns = ag.ToList();
        gameManagerScript = gms;

        totalHealth = th;
        Health = th;
        entityID = id;

        for (int i = 0; i < ag.GetLength(0); i++) {
            // Setup the gun.
            Guns[i].SetupGun(gms, this);
            // Activate if it is one among the active ones which has the lowest rank.
        }
    }
    
    // Applies damage to the opponent and eventually manages its death.
    public override void TakeDamage(int damage, int killerID) {
        if (inGame) {
            Health -= damage;

            // If the health goes under 0, kill the entity and start the respawn process.
            if (Health <= 0f) {
                Health = 0;
                // Kill the entity.
                Die(killerID);
            }
        }
    }

    // Heals the opponent.
    public override void HealFromMedkit(MedkitPickable medkit) {
        if (Health + medkit.RestoredHealth > totalHealth)
            Health = totalHealth;
        else
            Health += medkit.RestoredHealth;
    }

    // Kills the opponent.
    protected override void Die(int id) {
        gameManagerScript.AddScore(id, entityID);
        SetInGame(false);
        // Start the respawn process.
        gameManagerScript.ManageEntityDeath(gameObject, this);
    }

    // Respawns the opponent.
    public override void Respawn() {
        Health = totalHealth;
        ResetAllAmmo();
        SetInGame(true);
    }

    // Sets if the opponent is in game.
    public override void SetInGame(bool b) {
        SetIgnoreRaycast(!b);
        MeshVisibility.SetMeshVisible(transform, b);
        inGame = b;
    }

    public override void SlowEntity(float penalty) { }

}