using UnityEngine;

public class Opponent : MonoBehaviour {

    private int maximumHealth;
    private int health;

    private GameManager gameManagerScript;

    // Sets up the opponent.
    public void SetupOpponent(int h, GameManager gms) {
        maximumHealth = h;
        health = h;
        gameManagerScript = gms;
    }

    // Subtracts life.
    public void TakeDamage(int damage) {
        health -= damage;
        Debug.Log("Hit");

        if (health <= 0f) {
            health = 0;
            Die();
        }
    }

    private void Die() {
        gameManagerScript.Respawn(this.gameObject);
        health = maximumHealth;
    }

}