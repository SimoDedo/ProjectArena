using System.Linq;
using Bonsai;
using Bonsai.CustomNodes;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BonsaiBehaviours.Conditions
{
    /// <summary>
    /// Returns Success if the cover position is still valid (that is, there is an obstacle between the position and the enemy),
    /// Failure otherwise.
    /// </summary>
    [BonsaiNode("Conditional/")]
    public class IsCoverStillValid : TimedEvaluationConditionalAbort
    {
        public string pathInfoString;
        private NavMeshPath PathInfo => Blackboard.Get<NavMeshPath>(pathInfoString);
        private AIEntity me;
        private Entity.Entity enemy;
        private Vector3 finalPos;
        private int lineCastLayerMask;

        public override void OnStart()
        {
            lineCastLayerMask = ~LayerMask.GetMask("Entity", "Projectile", "Ignore Raycast");
        }

        public override void OnEnter()
        {
            finalPos = PathInfo.corners.Last();
            me = Actor.GetComponent<AIEntity>();
            enemy = me.GetEnemy();
        }

        public override bool Condition()
        {
            var canSeeEnemyFromFinalPosition =
                !Physics.Linecast(finalPos, enemy.transform.position, lineCastLayerMask);
            return canSeeEnemyFromFinalPosition;

        }

        public override Status Run()
        {
            return Condition() ? Status.Failure : Status.Success;
        }
    }
}