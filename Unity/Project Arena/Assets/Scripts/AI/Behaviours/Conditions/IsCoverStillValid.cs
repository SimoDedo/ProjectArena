using System;
using System.Linq;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if the cover position is still valid (that is, there is an obstacle between the position and the enemy),
    /// Failure otherwise.
    /// </summary>
    [Serializable]
    public class IsCoverStillValid : Conditional
    {
        [SerializeField] private SharedSelectedPathInfo pathInfo;
        private AIEntity me;
        private Entity.Entity enemy;
        private Vector3 finalPos;

        public override void OnStart()
        {
            finalPos = pathInfo.Value.corners.Last();
            me = gameObject.GetComponent<AIEntity>();
            enemy = me.GetEnemy();
        }

        public override TaskStatus OnUpdate()
        {
            me.SetIgnoreRaycast(true);
            var canSeeEnemyFromFinalPosition =
                !Physics.Linecast(finalPos, enemy.transform.position, out var hit, Physics.DefaultRaycastLayers) ||
                hit.collider.gameObject == enemy.gameObject;
            me.SetIgnoreRaycast(false);

            return canSeeEnemyFromFinalPosition ? TaskStatus.Failure : TaskStatus.Success;
        }
    }
}