using System;
using UnityEngine;

/// <summary>
/// Class representing an entity which is played by a bot.
/// </summary>
public class AIEntity : Entity
{
    [SerializeField] private GameObject head;
    [SerializeField] private float sensibility;
    [SerializeField] private float inputPenalty = 1f;

    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        activeGuns = ag;
        gameManagerScript = gms;

        totalHealth = th;
        health = th;
        entityID = id;

        for (int i = 0; i < ag.GetLength(0); i++)
        {
            // Setup the gun.
            var gun = guns[i].GetComponent<Gun>();
            gun.SetupGun(gms, this, null, i + 1);
            gun.Wield();
        }
    }

    public override void SetupEntity(GameManager gms, int id)
    {
        SetupEntity(totalHealth, activeGuns, gms, id);
    }

    public override void TakeDamage(int damage, int killerID)
    {
        throw new System.NotImplementedException();
    }

    protected override void Die(int id)
    {
        throw new System.NotImplementedException();
    }

    public override void Respawn()
    {
        throw new System.NotImplementedException();
    }

    public override void SlowEntity(float penalty)
    {
        inputPenalty = penalty;
    }

    public override void Heal(int restoredHealth)
    {
        throw new System.NotImplementedException();
    }

    public override void SetInGame(bool b)
    {
        throw new System.NotImplementedException();
    }

    private void Start()
    {
        SetupEntity(100, new[] {true}, null, 10);
    }

    [SerializeField] private Entity player;

    private void Update()
    {
        TryAttack(player);
    }


    /// <summary>
    /// This functions rotates head and body of the entity in order to look at the provided target.
    /// The rotation is subjected to the limitation given by the sensibility of the camera, so it might
    /// not be possible to immediately look at the target
    /// </summary>
    /// <param name="target">The point to look</param>
    public void AimTowards(Vector3 target)
    {
        var direction = (target - head.transform.position).normalized;

        var rotation = Quaternion.LookRotation(direction);

        var angles = rotation.eulerAngles;

        var desiredHeadRotation = Quaternion.Euler(angles.x, 0, 0);
        var desiredBodyRotation = Quaternion.Euler(0, angles.y, 0);

        var currentHeadRotation = head.transform.localRotation;
        var currentBodyRotation = transform.localRotation;

        var maxAngle = 2 * sensibility * inputPenalty;

        var newHeadRotation = Quaternion.RotateTowards(currentHeadRotation, desiredHeadRotation, maxAngle);
        var newBodyRotation = Quaternion.RotateTowards(currentBodyRotation, desiredBodyRotation, maxAngle);

        head.transform.localRotation = newHeadRotation;
        transform.localRotation = newBodyRotation;
    }

    public void TryAttack(Entity entity)
    {
        var entityGameObject = entity.gameObject;
        var reflexDelay = 4; // TODO Calculate with Gaussian 

        var positionTracker = entity.GetComponent<PositionTracker>();
        var position = positionTracker.GetPositionFromIndex(reflexDelay);
        AimTowards(position);
        if (Physics.Raycast(transform.position, transform.forward, out var point)
            && point.collider.gameObject == entityGameObject)
        {
            if (CanShoot())
            {
                Shoot();
            }
        }
    }


    public bool CanShoot()
    {
        var gun = guns[currentGun].GetComponent<Gun>();
        return gun.CanShoot();
    }

    public void CanReload()
    {
        var gun = guns[currentGun].GetComponent<Gun>();
        gun.CanReload();
    }

    public void Reload()
    {
        var gun = guns[currentGun].GetComponent<Gun>();
        gun.Reload();
    }

    public void Shoot()
    {
        var gun = guns[currentGun].GetComponent<Gun>();
        gun.Shoot();
    }
}