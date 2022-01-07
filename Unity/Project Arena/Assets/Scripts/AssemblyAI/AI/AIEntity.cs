using System;
using System.Collections.Generic;
using AI.Actions;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer1.Actuator;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyAI.AI.Layer2;
using AssemblyAI.AI.Layer3;
using AssemblyAI.StateMachine;
using AssemblyEntity.Component;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using BotSpace;
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
    [Header("Movement parameters")] [SerializeField]
    public float speed;

    [SerializeField] public FightingMovementSkill movementSkill;

    [Header("Sight parameters")] [SerializeField]
    public float fov;

    [SerializeField] public float maxCameraSpeed;
    [SerializeField] public float maxCameraAcceleration;

    [Header("Target reaction parameter")] [SerializeField]
    public float maxRange;

    [SerializeField] public float memoryWindow;
    [SerializeField] public float detectionWindow;
    [SerializeField] public float timeBeforeCanReact;

    [Header("Others")] [SerializeField] public CuriosityLevel curiosity;
    [SerializeField] [Range(0f, 1f)] public float predictionSkill;
    [SerializeField] [Range(0f, 1f)] public float aimingSkill;

    // For how long the entity remains wary after receiving damage
    [SerializeField] public float recentDamageTimeout;

    public static BotCharacteristics Default =>
        new BotCharacteristics
        {
            aimingSkill = 0.5f,
            curiosity = CuriosityLevel.Medium,
            fov = 60,
            maxCameraAcceleration = 2000f,
            maxCameraSpeed = 2000f,
            maxRange = 100f,
            memoryWindow = 4f,
            detectionWindow = 2f,
            timeBeforeCanReact = 0.2f,
            movementSkill = FightingMovementSkill.CircleStrife,
            predictionSkill = 0.5f,
            speed = 16,
            recentDamageTimeout = 0.5f,
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
    [SerializeField] private GameObject head;

    [SerializeField] private BotCharacteristics botParams = BotCharacteristics.Default;

    // Do I have to log?
    private bool loggingGame;

    // Time of the last position log.
    private float lastPositionLog;

    [SerializeField] private BotState botState;

    public BotState BotState
    {
        get => botState;
        private set => botState = value;
    }

    public override int Health
    {
        get => BotState.Health;
        protected set => BotState.Health = value;
    }
    
    public int MaxHealth => totalHealth;

    public TargetKnowledgeBase TargetKb { get; private set; }

    public DamageSensor DamageSensor { get; private set; }

    public AIMovementController MovementController { get; private set; }
    public AISightSensor SightSensor { get; private set; }

    public AISightController SightController { get; private set; }

    public PickupKnowledgeBase PickupKnowledgeBase { get; private set; }

    public PickupPlanner PickupPlanner { get; private set; }

    public NavigationSystem NavigationSystem { get; private set; }

    public GunManager GunManager { get; private set; }

    private IStateMachine stateMachine;

    private void PrepareComponents(GameManager gms, bool[] ag)
    {
        MovementController = new AIMovementController(this, botParams.speed);
        SightController = new AISightController(this, head, botParams.maxCameraSpeed, botParams.maxCameraAcceleration);
        SightSensor = new AISightSensor(head, botParams.maxRange, botParams.fov);
        TargetKb = new TargetKnowledgeBase(this, enemy, botParams.memoryWindow, botParams.detectionWindow, botParams.timeBeforeCanReact);
        DamageSensor = new DamageSensor(botParams.recentDamageTimeout);
        PickupKnowledgeBase = new PickupKnowledgeBase(this);
        NavigationSystem = new NavigationSystem(this, botParams.speed);
        GunManager = new GunManager(this);
        PickupPlanner = new PickupPlanner(this);
        stateMachine = new EntityStateMachine(this);
        BotState = new BotState();

        GunManager.Prepare(gms, this, null, ag);
        TargetKb.Prepare();
        PickupKnowledgeBase.Prepare();
        SightController.Prepare();
        SightSensor.Prepare();
        MovementController.Prepare();
        PickupPlanner.Prepare();
        NavigationSystem.Prepare();
    }

    public override void SetupEntity(int th, bool[] ag, GameManager gms, int id)
    {
        PrepareComponents(gms, ag);
        gameManagerScript = gms;

        totalHealth = th;
        Health = th;
        entityID = id;


        GunManager.TryEquipGun(GunManager.FindLowestActiveGun());

        var position = transform.position;
        SpawnInfoGameEvent.Instance.Raise(
            new SpawnInfo {x = position.x, z = position.z, entityId = entityID, spawnEntity = gameObject.name}
        );
    }
    public override void TakeDamage(int damage, int killerID)
    {
        if (inGame)
        {
            Debug.Log("Entity " + gameObject.name + " has taken damage!");
            Health -= damage;
            var position = transform.position;
            HitInfoGameEvent.Instance.Raise(
                new HitInfo
                {
                    damage = damage,
                    hitEntityID = entityID,
                    hitEntity = gameObject.name,
                    hitterEntityID = killerID,
                    hitterEntity = "Player " + killerID,
                    x = position.x,
                    z = position.z
                }
            );
            if (killerID != entityID)
            {
                // We just got damaged and it was not self-inflicted, we might need to search the enemy with more
                // care.
                TargetKb.ApplyFocus();
                DamageSensor.GotDamaged();
            }

            // If the health goes under 0, kill the entity and start the respawn process.
            if (Health <= 0f)
            {
                Health = 0;
                // Kill the entity.
                Die(killerID);
            }
        }
    }

    protected override void Die(int id)
    {
        // TODO Reset the various states
        var position = transform.position;
        KillInfoGameEvent.Instance.Raise(
            new KillInfo
            {
                killedEntity = gameObject.name,
                killedEntityID = entityID,
                killerEntity = "Player" + id,
                killerEntityID = id,
                x = position.x,
                z = position.z
            }
        );
        gameManagerScript.AddScore(id, entityID);
        SetInGame(false);
        // Start the respawn process.
        gameManagerScript.ManageEntityDeath(gameObject, this);
    }

    public override void Respawn()
    {
        var position = transform.position;
        SpawnInfoGameEvent.Instance.Raise(
            new SpawnInfo {x = position.x, z = position.z, entityId = entityID, spawnEntity = gameObject.name}
        );
        Health = totalHealth;
        GunManager.ResetAmmo();
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
                new PositionInfo {x = position.x, z = position.z, dir = t.eulerAngles.y, entityID = entityID}
            );
            lastPositionLog = Time.time;
        }

        if (inGame)
        {
            TargetKb.Update();
            PickupKnowledgeBase.Update();
            stateMachine.Update();

            // Weapon select (if no enemy, choose weapon to reload)
            // ActionSelectWeapon.Perform(this);
            // Weapon reload if needed
            // ActionWeaponReload.Perform(this);
            // Aim according to needs (aim enemy vs look around)
            // ActionAimView.Perform(this);
            // Weapon shoot
            // ??? Done in ActionAimView?
            // Bot dodge rockets?
            ActionDodgeRockets.Perform(this);

            // Bot move
            NavigationSystem.MoveAlongPath();
            // TODO 
        }
    }


    public override void SlowEntity(float penalty)
    {
        SightController.SetInputPenalty(penalty);
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

        PickupKnowledgeBase.MarkConsumed(medkit);
    }

    public override bool CanBeSupplied(bool[] suppliedGuns)
    {
        return GunManager.CanBeSupplied(suppliedGuns);
    }

    public override void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts)
    {
        GunManager.SupplyGuns(suppliedGuns, ammoAmounts);
    }

    public override void SetInGame(bool b)
    {
        stateMachine.SetIsIdle(!b);
        NavigationSystem.SetEnabled(b);
        GetComponent<CapsuleCollider>().enabled = b;
        MeshVisibility.SetMeshVisible(transform, b);
        inGame = b;
    }

    public void SetupLogging()
    {
        loggingGame = true;
    }
    
    public FightingMovementSkill MovementSkill => botParams.movementSkill;

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


    // Won't work if bot was already set up.
    public void SetCharacteristics(BotCharacteristics botParams)
    {
        this.botParams = botParams;
    }
}

namespace BotSpace
{
    // TODO Define BotState contents (health, ammo, current target, timeouts for stuff, ...)
    [Serializable]
    public partial class BotState
    {
        [SerializeField] private int health;
        public int Health
        {
            get => health;
            set => health = value;
        }
    }
}