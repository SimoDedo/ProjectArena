using System;
using System.Linq;
using AssemblyEntity.Component;
using UnityEngine;
using Utils;

/// <summary>
/// Opponent is a simple implementation of Entity with no extra logic.
/// </summary>
public class Opponent : Entity
{

    private GunManager gunManager;
    
    private void Awake()
    {
        gunManager = gameObject.AddComponent<GunManager>();
        gunManager.Prepare();
    }

    // Sets up all the opponent parameters and does the same with all its guns.
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        gunManager.SetupGuns(gms, this, null, ag);
        gameManagerScript = gms;

        totalHealth = th;
        Health = th;
        entityID = id;
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

    public override bool CanBeSupplied(bool[] suppliedGuns)
    {
        return gunManager.CanBeSupplied(suppliedGuns);
    }

    public override void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts)
    {
        gunManager.SupplyGuns(suppliedGuns, ammoAmounts);
    }

    public override int GetTotalAmmoForGun(int index)
    {
        return gunManager.GetTotalAmmoForGun(index);
    }

    public override int GetMaxAmmoForGun(int index)
    {
        return gunManager.GetMaxAmmoForGun(index);
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
        gunManager.ResetAmmo();
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