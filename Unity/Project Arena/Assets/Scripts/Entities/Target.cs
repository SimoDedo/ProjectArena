using System.Collections;
using UnityEngine;

public class Target : Entity {

    [Header("Target")] [SerializeField] private GameObject target;
    [SerializeField] private int totalHealthTarget;
    [SerializeField] private int bonusTime;
    [SerializeField] private int bonusScore;

    Vector3 originalScale;

    private Shader oldShader;
    private MeshRenderer[] meshList;
    private float currentAlpha = 0;

    private Laser[] laserList;

    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id) {
        originalScale = target.transform.localScale;
        gameManagerScript = gms;
        health = totalHealthTarget;

        originalLayer = transform.gameObject.layer;
        ChangeLayersRecursively(transform, disabledLayer);

        meshList = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in meshList) {
            mr.material.shader = Shader.Find("Transparent/Diffuse");
            mr.material.color = SetAlpha(mr.material.color, 0f);
        }

        laserList = gameObject.GetComponentsInChildren<Laser>();

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn() {
        while (currentAlpha < 1) {
            yield return new WaitForSeconds(0.01f);
            currentAlpha += 0.01f;
            foreach (MeshRenderer mr in meshList) {
                mr.material.color = SetAlpha(mr.material.color, currentAlpha);
            }
        }

        if (laserList != null) { 
            foreach (Laser l in laserList) {
                l.SetActive(true);
            }
        }

        ChangeLayersRecursively(transform, originalLayer);
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

    private Color SetAlpha(Color c, float alpha) {
        c.a = alpha;
        return c;
    }

    public override void Heal(int restoredHealth) { }

    public override void Respawn() { }

    public override void SetInGame(bool b) { }

}