using System;
using AI;
using AI.KnowledgeBase;
using AI.State;
using BehaviorDesigner.Runtime;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// Class representing an entity which is played by a bot.
/// </summary>

[RequireComponent(typeof(HealthKnowledgeBase))]
[RequireComponent(typeof(TargetKnowledgeBase))]
public class AIEntity : Entity, ILoggable, IEntityDecisions
{
    [SerializeField] private GameObject head;
    [SerializeField] private float sensibility;
    [SerializeField] private float inputPenalty = 1f;
    
    // Do I have to log?
    private bool loggingGame = false;

    // Time of the last position log.
    private float lastPositionLog = 0;
    
    private IState currentState;

    public void SetState(IState state)
    {
        currentState.Exit();
        currentState = state;
        state.Enter();
    }

    public IState GetState()
    {
        return currentState;
    }
    
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        activeGuns = ag;
        gameManagerScript = gms;

        totalHealth = th;
        health = th;
        entityID = id;

        for (var i = 0; i < ag.Length; i++)
        {
            // Setup the gun.
            var gun = guns[i].GetComponent<Gun>();
            gun.SetupGun(gms, this, null, i + 1);
        }
        ActivateLowestGun();
        
        GetComponent<HealthKnowledgeBase>().DetectPickups();
        var position = transform.position;
        SpawnInfoGameEvent.Instance.Raise(new SpawnInfo
        {
            x = position.x,
            z = position.z,
            entityId = entityID,
            spawnEntity = gameObject.name
        });
    }

    public override void SetupEntity(GameManager gms, int id)
    {
        SetupEntity(totalHealth, activeGuns, gms, id);
    }

    public override void TakeDamage(int damage, int killerID)
    {
        if (inGame)
        {
            health -= damage;

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
            
            // If the health goes under 0, kill the entity and start the respawn process.
            if (health <= 0f)
            {
                health = 0;
                // Kill the entity.
                Die(killerID);
            }
            else
            {
                GetComponent<BehaviorTree>().SendEvent("Damaged");
            }
        }
    }

    protected override void Die(int id)
    {
        var position = transform.position;
        KillInfoGameEvent.Instance.Raise(new KillInfo
        {
            killedEntity = gameObject.name,
            killedEntityID = entityID,
            killerEntity = "Player" + id,
            killerEntityID = id,
            x = position.x,
            z = position.z
        });
        gameManagerScript.AddScore(id, entityID);
        SetInGame(false);
        // Start the respawn process.
        gameManagerScript.ManageEntityDeath(gameObject, this);
    }

    public override void Respawn()
    {
        var position = transform.position;
        SpawnInfoGameEvent.Instance.Raise(new SpawnInfo
        {
            x = position.x,
            z = position.z,
            entityId = entityID,
            spawnEntity = gameObject.name
        });
        health = totalHealth;
        ResetAllAmmo();
        ActivateLowestGun();
        SetInGame(true);
    }

    private void Update()
    {
        if (loggingGame && Time.time > lastPositionLog + 0.5)
        {
            var t = transform;
            var position = t.position;
            PositionInfoGameEvent.Instance.Raise(
                new PositionInfo {x = position.x, z = position.z,
                dir = t.eulerAngles.y, entityID = entityID});
            lastPositionLog = Time.time;
        }
    }

    private void ActivateLowestGun()
    {
        guns[0].GetComponent<Gun>().Wield();
    }

    public override void SlowEntity(float penalty)
    {
        inputPenalty = penalty;
    }

    public override void HealFromMedkit(MedkitPickable medkit)
    {
        if (health + medkit.RestoredHealth > totalHealth)
        {
            health = totalHealth;
        }
        else
        {
            health += medkit.RestoredHealth;
        }

        GetComponent<HealthKnowledgeBase>().MarkConsumed(medkit);
    }

    public override void SetInGame(bool b)
    {
        GetComponent<NavMeshAgent>().enabled = b;
        GetComponent<BehaviorTree>().enabled = b;
        SetMeshVisible(transform, b);
        inGame = b;
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
        var reflexDelay = Math.Max(0, Random.Range(-3, 10)); // TODO Calculate with Gaussian 

        var positionTracker = entity.GetComponent<PositionTracker>();
        var position = positionTracker.GetPositionFromIndex(reflexDelay);
        var realPosition = positionTracker.GetPositionFromIndex(0);
        // Debug.Log("RealPosition " + realPosition + " delayed " + position);
        AimTowards(position);
        var angle = Vector3.Angle(head.transform.forward, realPosition - head.transform.position);
        if (angle < 40)
            if (CanShoot())
                Shoot();
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

    public void SetupLogging()
    {
        loggingGame = true;
    }

    public bool CanSeeEnemy()
    {
        throw new NotImplementedException();
    }

    public bool IsCloseTo(GameObject obj)
    {
        throw new NotImplementedException();
    }

    public bool IsCloseToDestination()
    {
        throw new NotImplementedException();
    }

    public bool HasLostTarget()
    {
        // TODO instead of querying the kb, I should have a personality module determining if I lost the target or not
        return GetComponent<TargetKnowledgeBase>().HasLostTarget();
    }

    public bool ShouldLookForHealth()
    {
        // TODO
        return health < 50;
    }

    public bool HasTakenDamage()
    {
        throw new NotImplementedException();
    }

    public bool ReachedSearchTimeout(float startSearchTime)
    {
        throw new NotImplementedException();
    }

}

public interface IEntityDecisions
{
    bool CanSeeEnemy();
    bool IsCloseTo(GameObject obj);
    bool IsCloseToDestination();
    bool HasLostTarget();
    bool ShouldLookForHealth();
    bool HasTakenDamage();
    bool ReachedSearchTimeout(float startSearchTime);
}