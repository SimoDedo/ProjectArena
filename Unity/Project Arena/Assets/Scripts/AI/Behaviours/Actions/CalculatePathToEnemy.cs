using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Attempts to estimated the current position of an enemy after it was lost.
    /// The higher the prediction score of the enemy, the more likely the prediction is correct.
    /// </summary>
    [Serializable]
    public class CalculatePathToEnemy : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        private Entity.Entity enemy;
        private AIEntity entity;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            enemy = entity.GetEnemy();
        }

        public override TaskStatus OnUpdate()
        {
            if (!enemy.IsAlive)
            {
                return TaskStatus.Failure;
            }
            var path = navSystem.CalculatePath(enemy.transform.position);
            if (path.IsComplete())
            {
                pathChosen.Value = path;
                return TaskStatus.Success;
            }

            // This shouldn't happen... Why is the enemy in a position that cannot be reached?
            Debug.LogWarning("Enemy position prediction failed! Enemy is unreachable");
            return TaskStatus.Failure;
        }
    }
}