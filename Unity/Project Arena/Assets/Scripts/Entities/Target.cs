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
    }

    public override void TakeDamage(int damage, int killerID) {
        health -= damage;

        Vector3 decreasedScale = target.transform.localScale;
        decreasedScale = originalScale * (health / totalHealthTarget / 4 + 0.75f);
        target.transform.localScale = decreasedScale;

        if (health < 1)
            Die(killerID);
    }

    protected override void Die(int id) {
        gameManagerScript.AddScore(bonusScore, bonusTime);
        Destroy(gameObject);
    }

    public override void Heal(int restoredHealth) { }

    public override void Respawn() { }

    public override void SetInGame(bool b) { }

}