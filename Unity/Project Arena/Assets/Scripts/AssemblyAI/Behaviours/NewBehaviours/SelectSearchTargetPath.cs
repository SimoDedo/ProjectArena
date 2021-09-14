using System;
using AI.Behaviours.NewBehaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer1.Sensors;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.NewBehaviours
{
    [Serializable]
    public class SelectSearchTargetPath : Action
    {
        private NavMeshAgent agent;
        private AIEntity entity;
        private AISightSensor sensor;
        private GameObject enemy;
        [SerializeField] private SharedSelectedPathInfo selectedPath;
        private bool canUsePremonition;
        private float premonitionSkill;
        private Vector3 startingPoint;
        private float maxRange;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            sensor = GetComponent<AISightSensor>();
            agent = GetComponent<NavMeshAgent>();
            enemy = entity.GetEnemy();
            premonitionSkill = entity.GetPremonition();
            startingPoint = transform.position;
            maxRange = sensor.GetObstacleDistanceInDirection(startingPoint,
                enemy.transform.position - startingPoint, Physics.IgnoreRaycastLayer);
        }

        public override TaskStatus OnUpdate()
        {
            // Se premonizione, allora setto percorso verso bersaglio come obiettivo
            // Altrimenti, a seconda che sia primo guess o meno, mi comporto diversamente
            var path = new NavMeshPath();
            var premonitionSuccess = Random.value < GetPremonitionSuccessProbability();
            if (premonitionSuccess && canUsePremonition)
                agent.CalculatePath(enemy.transform.position, path);
            else
            {
                canUsePremonition = false;
                var position = transform.position;
                var canExit = false;
                do
                {
                    // TODO watch out for infinite loops
                    Vector3 randomPoint = Random.insideUnitCircle * maxRange;
                    randomPoint.z = randomPoint.y;
                    randomPoint.y = position.y;
                    if (sensor.GetAngleFromLookDirection(randomPoint - position) < 90f)
                    {
                        if (!sensor.CanSeePosition(randomPoint, Physics.IgnoreRaycastLayer))
                        {
                            path = new NavMeshPath();
                            agent.CalculatePath(randomPoint, path);
                            if (path.status == NavMeshPathStatus.PathComplete)
                                canExit = true;
                        }
                    }
                } while (canExit); 
                // Do raycast in target direction and report farthest point seen.
                // that is your max search range
                // Select random point in that range in the general direction of the enemy that cannot
                // be seen from the starting position.
            }

            selectedPath.Value = path;
            return TaskStatus.Success;
        }

        private float GetPremonitionSuccessProbability()
        {
            if (premonitionSkill < 0.3) return premonitionSkill * 0.2f / 0.3f; // From 0 to 20%
            if (premonitionSkill < 0.6) return 0.2f + (premonitionSkill - 0.3f); // From 20% to 50%
            return 0.5f + (premonitionSkill - 0.6f) * 0.25f / 0.4f; // From 50% to 75%
        }
    }
}