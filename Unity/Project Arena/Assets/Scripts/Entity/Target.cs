using System;
using System.Collections;
using Guns;
using Logging;
using Managers.Mode;
using Others;
using Pickables;
using UnityEngine;

namespace Entity
{
    /// <summary>
    ///     Target is an implementation of Entity with a ILoggable interface, which allows its actions
    ///     to be logged. A target can be equipped with lasers.
    /// </summary>
    public class Target : Entity, ILoggable
    {
        [Header("Target")] [SerializeField] private GameObject target;

        [SerializeField] private int bonusTime;

        [SerializeField] private int bonusScore;

        private float currentAlpha;

        private Laser[] laserList;

        // Do I have to log?
        private bool loggingGame;
        private MeshRenderer[] meshList;

        private Shader oldShader;

        private Vector3 originalScale;

        // Setups stuff for the loggingGame.
        public void SetupLogging()
        {
            loggingGame = true;
        }

        public override void SetupEntity(int th, bool[] ag, GameManager gms,
            int id)
        {
            originalScale = target.transform.localScale;
            gameManagerScript = gms;
            Health = totalHealth;

            originalLayer = gameObject.layer;
            var disabledLayer = LayerMask.NameToLayer("Ignore Raycast");
            ChangeLayersRecursively(transform, disabledLayer);

            meshList = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in meshList)
            {
                mr.material.shader = Shader.Find("Transparent/Diffuse");
                mr.material.color = SetAlpha(mr.material.color, 0f);
            }

            laserList = gameObject.GetComponentsInChildren<Laser>();

            // Log if needed.
            if (gms.IsLogging())
            {
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
                
            MoveTowardsGround();
        }

        private IEnumerator FadeIn()
        {
            while (currentAlpha < 1)
            {
                yield return new WaitForSeconds(0.01f);
                currentAlpha += 0.01f;
                foreach (var mr in meshList) mr.material.color = SetAlpha(mr.material.color, currentAlpha);
            }

            if (currentAlpha != 1)
            {
                yield return new WaitForSeconds(0.01f);
                currentAlpha = 1;
                foreach (var mr in meshList) mr.material.color = SetAlpha(mr.material.color, currentAlpha);
            }

            if (laserList != null)
                foreach (var l in laserList)
                    l.SetActive(true);

            ChangeLayersRecursively(transform, originalLayer);
            inGame = true;
        }

        public override void TakeDamage(int damage, int killerID)
        {
            if (inGame)
            {
                Health -= damage;

                target.transform.localScale = originalScale * (Health / (float) totalHealth
                                                                      / 4f + 0.75f);

                // Log if needed.
                if (loggingGame)
                {
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

                if (Health < 1) Die(killerID);
            }
        }

        protected override void Die(int id)
        {
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
                    killerEntityID = id,
                    killerEntity = "Player " + id,
                    killedX = position.x,
                    killedZ = position.z,
                    // TODO
                    killerX = 0, 
                    killerZ = 0
                });
            }

            Destroy(gameObject);
        }

        private Color SetAlpha(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        public override void HealFromMedkit(MedkitPickable medkit)
        {
        }

        // TODO new entity type which doesn't have ammo
        public override bool CanBeSupplied(bool[] suppliedGuns)
        {
            return false;
        }

        public override void SupplyFromAmmoCrate(AmmoPickable ammoCrate)
        {
            throw new InvalidOperationException();
        }

        public override void Respawn()
        {
            MoveTowardsGround();
        }

        private void MoveTowardsGround()
        {
            var position = transform.position;
            position.y -= 2f;
            transform.position = position;
        }

        public override void SetInGame(bool b, bool isGameEnded = false)
        {
        }

        public override void SlowEntity(float penalty)
        {
        }
    }
}