public class Opponent : Entity {

    // Kills the opponent.
    protected override void Die() {
        SetInGame(false);
    }

    // Respawns the opponent.
    public override void Respawn() {
        health = totalHealth;
        SetInGame(true);
    }

    // Sets if the opponent is in game.
    public override void SetInGame(bool b) {
        if (b) {
            // TODO - Show the mesh.
        } else {
            // Hide the mesh.
        }

        inGame = b;
    }

}