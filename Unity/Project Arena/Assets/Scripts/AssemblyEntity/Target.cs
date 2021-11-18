using System;
using System.Collections;
using AssemblyLogging;
using UnityEngine;

/// <summary>
/// Target is an implementation of Entity with a ILoggable interface, which allows its actions
/// to be logged. A target can be equipped with lasers. 
/// </summary>
public class Target : Entity, ILoggable {

    [Header("Target")]
    [SerializeField]
    private GameObject target;
    [SerializeField]
    private int bonusTime;
    [SerializeField]
    private int bonusScore;

    Vector3 originalScale;

    private Shader oldShader;
    private MeshRenderer[] meshList;
    private float currentAlpha = 0;

    private Laser[] laserList;

    // Do I have to log?
    private bool loggingGame = false;
    
    public override void SetupEntity(int th, bool[] ag, GameManager gms,
        int id) {
        originalScale = target.transform.localScale;
        gameManagerScript = gms;
        Health = totalHealth;

        originalLayer = gameObject.layer;
        var disabledLayer = LayerMask.NameToLayer("Ignore Raycast");
        ChangeLayersRecursively(transform, disabledLayer);

        meshList = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in meshList) {
            mr.material.shader = Shader.Find("Transparent/Diffuse");
            mr.material.color = SetAlpha(mr.material.color, 0f);
        }

        laserList = gameObject.GetComponentsInChildren<Laser>();

        // Log if needed.
        if (gms.IsLogging()) {
            SetupLogging();
            var position = transform.position;
            SpawnInfoGameEvent.Instance.Raise(new SpawnInfo
            {
                x = position.x,
                z = position.z,
                entityId = entityID,
                spawnEntity = gameObject.name
            });
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
            Health -= damage;

            target.transform.localScale = originalScale * ((float)Health / (float)totalHealth
                / 4f + 0.75f);

            // Log if needed.
            if (loggingGame) {
                var position = transform.position;
                HitInfoGameEvent.Instance.Raise(new HitInfo
                {
                    damage = damage,
                    hitEntityID = entityID,
                    hitEntity = gameObject.name,
                    hitterEntityID = killerID,
                    hitterEntity = "Player " + killerID,
                    x = position.x,
                    z = position.z
                });
            }

            if (Health < 1) {
                Die(killerID);
            }
        }
    }

    protected override void Die(int id) {
        inGame = false;
        gameManagerScript.AddScore(bonusScore, bonusTime);

        // Log if needed.
        if (loggingGame)
        {
            var position = transform.position;
            KillInfoGameEvent.Instance.Raise(new KillInfo
            {
                killedEntityID = entityID,
                killedEntity = gameObject.name,
                killerEntityID =  id,
                killerEntity =  "Player " + id,
                x = position.x,
                z = position.z
            });
        }

        Destroy(gameObject);
    }

    private Color SetAlpha(Color c, float alpha) {
        c.a = alpha;
        return c;
    }

    public override void HealFromMedkit(MedkitPickable medkit) { }

    // TODO new entity type which doesn't have ammo
    public override bool CanBeSupplied(bool[] suppliedGuns)
    {
        return false;
    }

    public override void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts)
    {
        throw new InvalidOperationException();
    }

    public override void Respawn() { }

    public override void SetInGame(bool b) { }

    public override void SlowEntity(float penalty) { }

    // Setups stuff for the loggingGame.
    public void SetupLogging() {
        loggingGame = true;
    }

}