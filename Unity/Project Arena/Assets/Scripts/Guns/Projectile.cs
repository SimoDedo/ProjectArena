﻿using UnityEngine;

namespace Guns
{
    /// <summary>
    ///     Projectile is an abstract class used to implement any kind of projectile.
    /// </summary>
    public abstract class Projectile : MonoBehaviour
    {
        [SerializeField] protected GameObject projectile;
        protected float damage;

        protected ProjectileGun projectileGunScript;
        protected float projectileLifeTime;
        protected float projectileSpeed;
        protected int shooterID;
        protected bool shot;
        protected float shotTime;

        public int ShooterID => shooterID;

        private void Update()
        {
            if (shot)
            {
                Step();

                if (Time.time > shotTime + projectileLifeTime) Recover();
            }
        }

        private void Step()
        {
            var t = transform;
            t.position += t.forward * (Time.deltaTime * projectileSpeed);
        }

        // Sets the projectile.
        public void SetupProjectile(float plt, float ps, ProjectileGun pg, float d, int s)
        {
            projectileLifeTime = plt;
            projectileSpeed = ps;
            projectileGunScript = pg;
            damage = d;
            shooterID = s;
        }

        // Fires the projectile.
        public void Fire(Vector3 position, Quaternion direction)
        {
            transform.position = position;
            transform.rotation = direction;
            shot = true;
            shotTime = Time.time;
            gameObject.SetActive(true);
            projectile.SetActive(true);
            Step();
        }

        // Recovers the projectile.
        public void Recover()
        {
            shot = false;
            projectileGunScript.RecoverProjectile(gameObject);
            gameObject.SetActive(false);
        }
    }
}