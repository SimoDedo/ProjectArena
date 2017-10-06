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
            if (i == GetActiveGun(0, true)) {
                currentGun = i;
                guns[i].SetActive(true);
            }
        }
    }

    // Kills the opponent.
    protected override void Die(int id) {
        gameManagerScript.AddKill(id, entityID);
        SetInGame(false);
    }

    // Respawns the opponent.
    public override void Respawn() {
        health = totalHealth;
        SetInGame(true);
    }

    // Sets if the opponent is in game.
    public override void SetInGame(bool b) {
        SetMeshVisible(transform, b);
        inGame = b;
    }

}