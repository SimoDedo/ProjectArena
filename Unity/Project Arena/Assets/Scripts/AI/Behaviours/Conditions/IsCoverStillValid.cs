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
        private Entity.Entity enemy;
        private Vector3 finalPos;

        public override void OnStart()
        {
            finalPos = pathInfo.Value.corners.Last();
            enemy = gameObject.GetComponent<AIEntity>().GetEnemy();
        }

        public override TaskStatus OnUpdate()
        {
            if (!Physics.Linecast(finalPos, enemy.transform.position, out var hit) ||
                hit.collider.gameObject == enemy.gameObject)
                // We can see the enemy from that position, no good! 
                return TaskStatus.Failure;
            return TaskStatus.Success;
        }
    }
}