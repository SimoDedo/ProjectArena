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
    // Support object to format the log.
    private JsonLog jLog;
    // Support object to format the log.
    private JsonKill jKill;
    // Support object to format the log.
    private JsonSpawn jSpawn;
    // Support object to format the log.
    private JsonHit jHit;

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
            LogSpawn();
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
                LogHit();

            if (health < 1)
                Die(killerID);
        }
    }

    protected override void Die(int id) {
        inGame = false;
        gameManagerScript.AddScore(bonusScore, bonusTime);

        // Log if needed.
        if (logging)
            LogKill();

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

        jLog = new JsonLog {
            log = ""
        };

        jSpawn = new JsonSpawn();
        jKill = new JsonKill();
        jHit = new JsonHit();

        logging = true;
    }

    // Logs spawn.
    private void LogSpawn() {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "spawn";
        jSpawn.x = transform.position.x.ToString();
        jSpawn.y = transform.position.z.ToString();
        jSpawn.spawnedEntity = gameObject.name.ToString();
        string log = JsonUtility.ToJson(jLog);
        experimentManagerScript.WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jSpawn) + "}");
    }

    // Logs a kill.
    protected void LogKill() {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "kill";
        jKill.x = transform.position.x.ToString();
        jKill.y = transform.position.y.ToString();
        jKill.killedEntity = gameObject.name.ToString();
        jKill.killerEntity = "Player";
        string log = JsonUtility.ToJson(jLog);
        experimentManagerScript.WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jKill) + "}");
    }

    // Logs a hit.
    protected void LogHit() {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "hit";
        jHit.x = transform.position.x.ToString();
        jHit.y = transform.position.y.ToString();
        jHit.hittedEntity = gameObject.name.ToString();
        jHit.hitterEntity = "Player";
        string log = JsonUtility.ToJson(jLog);
        experimentManagerScript.WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jHit) + "}");
    }

    private class JsonLog {
        public string time;
        public string type;
        public string log;
    }

    private class JsonKill {
        public string x;
        public string y;
        public string killedEntity;
        public string killerEntity;
    }

    private class JsonHit {
        public string x;
        public string y;
        public string hittedEntity;
        public string hitterEntity;
    }


    private class JsonSpawn {
        public string x;
        public string y;
        public string spawnedEntity;
    }

}