using System.Collections;
using UnityEngine;

public class Target : Entity, ILoggable {

    [Header("Target")] [SerializeField] private GameObject target;
    [SerializeField] private int totalHealthTarget;
    [SerializeField] private int bonusTime;
    [SerializeField] private int bonusScore;

    Vector3 originalScale;

    private Shader oldShader;
    private MeshRenderer[] meshList;
    private float currentAlpha = 0;

    private Laser[] laserList;

    // Do I have to log?
    private bool logging = false;
    // Experiment manager.
    private ExperimentManager experimentManagerScript;

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

        // Log if needed.
        if (gms.IsLogging()) {
            SetupLogging(gms.GetExperimentManager());
            experimentManagerScript.LogSpawn(transform.position.x, transform.position.z, gameObject.name);
        }

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

        if (currentAlpha != 1) {
            yield return new WaitForSeconds(0.01f);
            currentAlpha = 1;
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

            // Log if needed.
            if (logging)
                experimentManagerScript.LogHit(transform.position.x, transform.position.z, gameObject.name, "Player " + killerID, damage);

            if (health < 1)
                Die(killerID);
        }
    }

    protected override void Die(int id) {
        inGame = false;
        gameManagerScript.AddScore(bonusScore, bonusTime);

        // Log if needed.
        if (logging)
            experimentManagerScript.LogKill(transform.position.x, transform.position.z, gameObject.name, "Player " + id);

        Destroy(gameObject);
    }

    private Color SetAlpha(Color c, float alpha) {
        c.a = alpha;
        return c;
    }

    public override void Heal(int restoredHealth) { }

    public override void Respawn() { }

    public override void SetInGame(bool b) { }

    public override void SlowEntity(float penalty) { }

    // Setups stuff for the logging.
    public void SetupLogging(ExperimentManager em) {
        experimentManagerScript = em;
        logging = true;
    }

}