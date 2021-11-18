using System;
using AI.AI.Layer3;
using AI.KnowledgeBase;
using AssemblyAI.State;
using AssemblyAI.StateMachine;
using AssemblyEntity.Component;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using Entities.AI.Controller;
using Entities.AI.Layer1.Sensors;
using Entities.AI.Layer2;
using JetBrains.Annotations;
using UnityEngine;
using Utils;

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

[Serializable]
public struct BotCharacteristics
{
    [Header("Movement parameters")]
    [SerializeField] public float speed;
    [SerializeField] public FightingMovementSkill movementSkill;

    [Header("Sight parameters")]
    [SerializeField] public float fov;
    [SerializeField] public float maxCameraSpeed;
    [SerializeField] public float maxCameraAcceleration;

    [Header("Target reaction parameter")]
    [SerializeField] public float maxRange;
    [SerializeField] public float memoryWindow;
    [SerializeField] public float timeBeforeCanReact;
    
    [Header("Others")]
    [SerializeField]
    public CuriosityLevel curiosity;
    [SerializeField] [Range(0f,1f)] public float predictionSkill;
    [SerializeField] [Range(0f,1f)] public float aimingSkill;

    public static BotCharacteristics Default =>
        new BotCharacteristics
        {
            aimingSkill = 0.5f,
            curiosity = CuriosityLevel.Medium,
            fov = 60,
            maxCameraAcceleration = 2000f,
            maxCameraSpeed = 2000f,
            maxRange = 100f,
            memoryWindow = 0.5f,
            timeBeforeCanReact = 0.2f,
            movementSkill = FightingMovementSkill.CircleStrife,
            predictionSkill = 0.5f,
            speed = 16
        };
}

public enum FightingMovementSkill
{
    StandStill,
    MoveStraight,
    CircleStrife,
    CircleStrifeChangeDirection,
}
public enum CuriosityLevel
{
    Low,
    Medium,
    High,
}


public class AIEntity : Entity, ILoggable
{
    [SerializeField]
    private GameObject head;

    [SerializeField] private BotCharacteristics botParams = BotCharacteristics.Default;
    
    // Do I have to log?
    private bool loggingGame;

    // Time of the last position log.
    private float lastPositionLog;

    [SerializeField] private int health;

    public override int Health
    {
        get => health;
        protected set => health = value;
    }

    private AIMovementController movementController;
    private AISightSensor sightSensor;
    private AISightController sightController;
    private TargetKnowledgeBase targetKnowledgeBase;
    private PickupKnowledgeBase pickupKnowledgeBase;
    private NavigationSystem navigationSystem;
    private PickupPlanner pickupPlanner;
    private GunManager gunManager;
    
    private void PrepareComponents()
    {
        movementController = gameObject.AddComponent<AIMovementController>();
        movementController.Prepare(botParams.speed);
        sightController = gameObject.AddComponent<AISightController>();
        sightController.Prepare(head, botParams.maxCameraSpeed, botParams.maxCameraAcceleration);
        sightSensor = gameObject.AddComponent<AISightSensor>();
        sightSensor.Prepare(head, botParams.maxRange, botParams.fov);
        targetKnowledgeBase = gameObject.AddComponent<TargetKnowledgeBase>();
        targetKnowledgeBase.Prepare(sightSensor, enemy, botParams.memoryWindow, botParams.timeBeforeCanReact);
        pickupKnowledgeBase = gameObject.AddComponent<PickupKnowledgeBase>();
        pickupKnowledgeBase.Prepare(sightSensor);
        navigationSystem = gameObject.AddComponent<NavigationSystem>();
        navigationSystem.Prepare(movementController, botParams.speed);
        gunManager = gameObject.AddComponent<GunManager>();
        gunManager.Prepare();
        pickupPlanner = gameObject.AddComponent<PickupPlanner>();
        pickupPlanner.Prepare(this, navigationSystem, pickupKnowledgeBase, gunManager);
        stateMachine = new EntityStateMachine(this);
    }

    private IStateMachine stateMachine;
    
    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        PrepareComponents();
        
        gameManagerScript = gms;

        totalHealth = th;
        Health = th;
        entityID = id;

        gunManager.SetupGuns(gms, this, null, ag);
        gunManager.TryEquipGun(gunManager.FindLowestActiveGun());
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

    private float lastDamageTime = float.MinValue;

    public override void TakeDamage(int damage, int killerID)
    {
        if (inGame)
        {
            Debug.Log("Entity " + gameObject.name + " has taken damage!");
            Health -= damage;
            lastDamageTime = Time.time;
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
        gunManager.ResetAmmo();
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

        if (inGame) // TODO Check if necessary
        {
            stateMachine.Update();
        }
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

    public override bool CanBeSupplied(bool[] suppliedGuns)
    {
        return gunManager.CanBeSupplied(suppliedGuns);
    }

    public override void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts)
    {
        gunManager.SupplyGuns(suppliedGuns, ammoAmounts);
    }

    public int GetCurrentAmmoForGun(int index)
    {
        return gunManager.GetCurrentAmmoForGun(index);
    }

    public int GetMaxAmmoForGun(int index)
    {
        return gunManager.GetMaxAmmoForGun(index);
    }

    public override void SetInGame(bool b)
    {
        stateMachine.SetIsIdle(!b);
        navigationSystem.SetEnabled(b);
        GetComponent<CapsuleCollider>().enabled = b;
        MeshVisibility.SetMeshVisible(transform, b);
        inGame = b;
    }
    
    public void SetupLogging()
    {
        loggingGame = true;
    }

    public bool HasTakenDamageRecently()
    {
        return Time.time - lastDamageTime < 0.2f;
    }
    public FightingMovementSkill GetMovementSkill()
    {
        return botParams.movementSkill;
    }

    public CuriosityLevel GetCuriosity()
    {
        return botParams.curiosity;
    }
    
    public float GetPredictionSkill()
    {
        return botParams.predictionSkill;
    }

    [SerializeField] [NotNull] private Entity enemy;

    public Entity GetEnemy()
    {
        return enemy;
    }
    
    public float GetAimingSkill()
    {
        return botParams.aimingSkill;
    }

    public int GetMaxHealth()
    {
        return totalHealth;
    }

    // Won't work if bot was already set up.
    public void SetCharacteristics(BotCharacteristics botParams)
    {
        this.botParams = botParams;
    }
}