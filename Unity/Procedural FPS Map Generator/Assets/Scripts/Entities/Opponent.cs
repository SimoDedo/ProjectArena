public class Opponent : Entity {

    // Sets up all the player parameter and does the same with all its guns.
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        activeGuns = ag;
        gameManagerScript = gms;

        totalHealth = th;
        health = th;
        entityID = id;

        for (int i = 0; i < ag.GetLength(0); i++) {
            // Setup the gun.
            guns[i].GetComponent<Gun>().SetupGun(gms, this);
            // Activate it if is one among the active ones which has the lowest rank.
            if (i == GetActiveGun(-1, true)) {
                currentGun = i;
                guns[i].SetActive(true);
            }
        }
    }

    // Applies damage to the opponent and eventually manages its death.
    public override void TakeDamage(int damage, int killerID) {
        if (inGame) {
            health -= damage;

            // If the health goes under 0, kill the entity and start the respawn process.
            if (health <= 0f) {
                health = 0;
                // Kill the entity.
                Die(killerID);
                // Start the respawn process.
                StartCoroutine(gameManagerScript.WaitForRespawn(gameObject, this));
            }
        }
    }

    // Heals the opponent.
    public override void Heal(int restoredHealth) {
        if (health + restoredHealth > totalHealth)
            health = totalHealth;
        else
            health += restoredHealth;
    }

    // Kills the opponent.
    protected override void Die(int id) {
        gameManagerScript.AddKill(id, entityID);
        SetInGame(false);
    }

    // Respawns the opponent.
    public override void Respawn() {
        health = totalHealth;
        ResetAllAmmo();
        SetInGame(true);
    }

    // Sets if the opponent is in game.
    public override void SetInGame(bool b) {
        SetIgnoreRaycast(!b);
        SetMeshVisible(transform, b);
        inGame = b;
    }

}