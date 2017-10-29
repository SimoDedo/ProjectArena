using UnityEngine;

public class Target : Entity {

    [Header("Target")] [SerializeField] private GameObject target;
    [SerializeField] private int totalHealthTarget;
    [SerializeField] private int bonusTime;
    [SerializeField] private int bonusScore;

    Vector3 originalScale;

    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        originalScale = target.transform.localScale;
        gameManagerScript = gms;
        health = totalHealthTarget;
        inGame = true;
    }

    public override void TakeDamage(int damage, int killerID) {
        if (inGame) {
            health -= damage;

            target.transform.localScale = originalScale * ((float)health / (float)totalHealthTarget / 4f + 0.75f);

            if (health < 1)
                Die(killerID);
        }
    }

    protected override void Die(int id) {
        inGame = false;
        gameManagerScript.AddScore(bonusScore, bonusTime);
        Destroy(gameObject);
    }

    public override void Heal(int restoredHealth) { }

    public override void Respawn() { }

    public override void SetInGame(bool b) { }

}