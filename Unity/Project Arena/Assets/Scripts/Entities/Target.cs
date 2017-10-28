using UnityEngine;

public class Target : Entity {

    [Header("Target")] [SerializeField] private GameObject target;
    [SerializeField] private new int totalHealth;
    [SerializeField] private int bonusTime;
    [SerializeField] private int bonusScore;

    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        gameManagerScript = gms;
        health = totalHealth;
    }

    public override void TakeDamage(int damage, int killerID) {
        health -= damage;

        Vector3 decreasedScale = target.transform.localScale;
        decreasedScale *= (health / totalHealth / 2 + 0.5f);
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
