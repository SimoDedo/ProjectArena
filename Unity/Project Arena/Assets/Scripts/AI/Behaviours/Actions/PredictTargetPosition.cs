using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Attempts to estimated the current position of an enemy after it was lost.
    /// The higher the prediction score of the enemy, the more likely the prediction is correct.
    /// </summary>
    [Serializable]
    public class PredictTargetPosition : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        private Entity.Entity enemy;
        private AIEntity entity;
        private bool failedPreviousPrediction;
        private NavigationSystem navSystem;
        private float predictionSkill;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            enemy = entity.GetEnemy();
            predictionSkill = entity.GetPredictionSkill();
        }

        public override TaskStatus OnUpdate()
        {
            if (failedPreviousPrediction) return TaskStatus.Failure;

            if (Random.value > predictionSkill)
            {
                failedPreviousPrediction = true;
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