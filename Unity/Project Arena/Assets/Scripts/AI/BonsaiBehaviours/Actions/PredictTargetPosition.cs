using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.Core;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Attempts to estimated the current position of an enemy after it was lost.
    /// The higher the prediction score of the enemy, the more likely the prediction is correct.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class PredictTargetPosition : Task
    {
        public string pathChosenKey;
        private NavMeshPath PathChosen
        {
            set => Blackboard.Set(pathChosenKey, value);
        }
        private Entity.Entity enemy;
        private AIEntity entity;
        private bool failedPreviousPrediction;
        private NavigationSystem navSystem;
        private float predictionSkill;

        public override void OnStart()
        {
            entity = Actor.GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            enemy = entity.GetEnemy();
            predictionSkill = entity.Characteristics.Prediction;
        }

        public override Status Run()
        {
            if (failedPreviousPrediction) return Status.Failure;

            if (Random.value > predictionSkill)
            {
                failedPreviousPrediction = true;
                return Status.Failure;
            }

            var path = navSystem.CalculatePath(enemy.transform.position);
            if (path.IsComplete())
            {
                PathChosen = path;
                return Status.Success;
            }

            // This shouldn't happen... Why is the enemy in a position that cannot be reached?
            Debug.LogWarning("Enemy position prediction failed! Enemy is unreachable");
            return Status.Failure;
        }
    }
}