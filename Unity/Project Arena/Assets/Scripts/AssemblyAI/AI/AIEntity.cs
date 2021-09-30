using System;
using System.Collections.Generic;
using System.Linq;
using AI.KnowledgeBase;
using AI.State;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using Entities.AI.Controller;
using Entities.AI.Layer1.Sensors;
using Entities.AI.Layer2;
using JetBrains.Annotations;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;
using Wander = AI.State.Wander;

/// <summary>
/// Class representing an entity which is played by a bot.
/// </summary>

// List of characteristics of a bot:
// Movement: How good the bot is to move around when fighting.
// Curiosity: How much the bot looks around himself when wandering
// Aim: How good is the player aim
// Hoarder: How likely is the bot to move towards a collectible even during other actions
// Aggressiveness: How aggressively the bot plays, ignoring its own safety
// Camp: How much the bot tends to stay in the same place
// Weapon preference: how much a bot likes a specific weapon. 
// Reflexes: How quick the player reacts to events
// Premonition: How likely the player is at correctly guessing a lost target position

// Aside from reflexes, all the other properties fall in [0-1] range
public class AIEntity : Entity, ILoggable
{
    [Header("Movement parameters")] [SerializeField]
    private float speed = 16;

    [SerializeField] private float acceleration = 10;
    [SerializeField] private float angularSpeed = 10;

    [Header("Sight parameters")] [SerializeField]
    private GameObject head;

    [SerializeField] private float fov = 90f;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float sensibility = 5f;
    [SerializeField] private float inputPenalty = 1f;

    [Header("Target reaction parameter")] [SerializeField]
    private float memoryWindow = 0.5f;

    [SerializeField] private float reactionTime = 0.1f;
    [SerializeField] private float consecutiveTimeBeforeCanReact = 0.04f;

    // Do I have to log?
    private bool loggingGame;

    // Time of the last position log.
    private float lastPositionLog;

    public int health;
    public override int Health { get => health; protected set => health = value; }

    private AIMovementController movementController;
    private AISightSensor sightSensor;
    private AISightController sightController;
    private TargetKnowledgeBase targetKnowledgeBase;
    private PickupKnowledgeBase pickupKnowledgeBase;
    private NavigationSystem navigationSystem;

    private void Awake()
    {
        Guns = gameObject.GetComponentsInChildren<Gun>().ToList();
        movementController = gameObject.AddComponent<AIMovementController>();
        movementController.SetParameters(speed);
        sightController = gameObject.AddComponent<AISightController>();
        sightController.SetParameters(head, sensibility, inputPenalty);
        sightSensor = gameObject.AddComponent<AISightSensor>();
        sightSensor.SetParameters(head, maxRange, fov);
        targetKnowledgeBase = gameObject.AddComponent<TargetKnowledgeBase>();
        targetKnowledgeBase.SetParameters(sightSensor, enemy, memoryWindow, consecutiveTimeBeforeCanReact,
            reactionTime);
        pickupKnowledgeBase = gameObject.AddComponent<PickupKnowledgeBase>();
        pickupKnowledgeBase.SetupParameters(sightSensor);
        navigationSystem = gameObject.AddComponent<NavigationSystem>();
        navigationSystem.SetParameters(movementController, speed, acceleration, angularSpeed);
    }

    [NotNull] private IState currentState = new Idle();

    public void SetState([NotNull] IState state)
    {
        currentState.Exit();
        currentState = state;
        currentState.Enter();
    }

    [NotNull]
    public IState GetState()
    {
        return currentState;
    }

    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        gameManagerScript = gms;

        totalHealth = th;
        Health = th;
        entityID = id;

        for (var i = 0; i < ag.Length; i++)
        {
            // Setup the gun.
            var gun = Guns[i].GetComponent<Gun>();
            gun.SetupGun(gms, this, null, i + 1);
        }
        ActiveGuns = ag.ToList();
        ActivateLowestGun();

        pickupKnowledgeBase.DetectPickups();
        var position = transform.position;
        SpawnInfoGameEvent.Instance.Raise(new SpawnInfo
        {
            x = position.x,
            z = position.z,
            entityId = entityID,
            spawnEntity = gameObject.name
        });
    }

    public override void TakeDamage(int damage, int killerID)
    {
        if (inGame)
        {
            Health -= damage;

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
            if (Health <= 0f)
            {
                Health = 0;
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
        Health = totalHealth;
        ResetAllAmmo();
        // ActivateLowestGun();
        SetInGame(true);
    }

    private void Update()
    {
        if (loggingGame && Time.time > lastPositionLog + 0.5)
        {
            var t = transform;
            var position = t.position;
            PositionInfoGameEvent.Instance.Raise(
                new PositionInfo
                {
                    x = position.x, z = position.z,
                    dir = t.eulerAngles.y, entityID = entityID
                });
            lastPositionLog = Time.time;
        }

        currentState.Update();
    }

    private void ActivateLowestGun()
    {
        var firstIndex = Guns.FindIndex(it=>it.isActiveAndEnabled);
        Guns[firstIndex].Wield();
        currentGun = firstIndex;
        // EquipGun(firstIndex);
    }

    public bool EquipGun(int index)
    {
        if (index < 0 || index > Guns.Count)
            return false;
        if (!ActiveGuns[index])
            return false;
        return TrySwitchGuns(currentGun, index);
    }

    public Gun GetCurrentGun()
    {
        return Guns[currentGun];
    }

    public override void SlowEntity(float penalty)
    {
        sightController.SetInputPenalty(penalty);
    }

    public override void HealFromMedkit(MedkitPickable medkit)
    {
        if (Health + medkit.RestoredHealth > totalHealth)
        {
            Health = totalHealth;
        }
        else
        {
            Health += medkit.RestoredHealth;
        }

        GetComponent<PickupKnowledgeBase>().MarkConsumed(medkit);
    }

    public override void SetInGame(bool b)
    {
        if (b)
            SetState(new Wander(this));
        else
            SetState(new Idle());

        GetComponent<NavMeshAgent>().enabled = b;
        GetComponent<CapsuleCollider>().enabled = b;
        MeshVisibility.SetMeshVisible(transform, b);
        inGame = b;
    }


    public void SetupLogging()
    {
        loggingGame = true;
    }

    public bool CanSeeEnemy()
    {
        return GetComponent<TargetKnowledgeBase>().CanReactToTarget();
    }

    public bool HasLostTarget()
    {
        // TODO instead of querying the kb, I should have a personality module determining if I lost the target or not
        return GetComponent<TargetKnowledgeBase>().HasLostTarget();
    }

    public bool ShouldLookForHealth()
    {
        return Health < 50;
    }

    public bool HasTakenDamage()
    {
        // TODO
        return false;
    }

    public bool ReachedSearchTimeout(float startSearchTime)
    {
        // TODO
        return Time.time > startSearchTime + 5;
    }

    [Range(0f, 1f)] [SerializeField] private float movementSkill = 0.5f;

    public float GetMovementSkill()
    {
        return movementSkill;
    }

    [SerializeField] private CuriosityLevel curiosity = CuriosityLevel.Medium;

    public enum CuriosityLevel
    {
        Low,
        Medium,
        High,
    } 

    public CuriosityLevel GetCuriosity()
    {
        return curiosity;
    }

    [FormerlySerializedAs("premonition")] [Range(0f, 1f)] [SerializeField] private float predictionSkill = 0.5f;

    public float GetPredictionSkill()
    {
        return predictionSkill;
    }

    [SerializeField] [NotNull] private Entity enemy;

    public Entity GetEnemy()
    {
        return enemy;
    }

    public List<Gun> GetGuns()
    {
        return Guns;
    }

    [SerializeField] [Range(0f, 1f)] private float aimingSkill = 0.5f;
    public float GetAimingSkill()
    {
        return aimingSkill;
    }

    protected override void ActivateGun(int index)
    {
        Guns[index].Wield();
    }

    protected override void DeactivateGun(int index)
    {
        Guns[index].Stow();
    }
}