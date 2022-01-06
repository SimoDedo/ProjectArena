using System;
using System.Linq;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AssemblyAI.Behaviours.Conditions
{
    [Serializable]
    public class IsCoverStillValid: Conditional
    {
        [SerializeField] private SharedSelectedPathInfo pathInfo;
        private Entity enemy;
        private Vector3 finalPos;

        public override void OnStart()
        {
            finalPos = pathInfo.Value.corners.Last();
            enemy = gameObject.GetComponent<AIEntity>().GetEnemy();
        }

        public override TaskStatus OnUpdate()
        {
            Debug.DrawLine(transform.position, finalPos, Color.magenta);
            Debug.DrawLine(enemy.transform.position, finalPos, Color.magenta);
            if (!Physics.Linecast(finalPos, enemy.transform.position, out var hit) ||
                hit.collider.gameObject == enemy.gameObject)
            {
                // We can see the enemy from that position, no good! 
                return TaskStatus.Failure;
            }
            return TaskStatus.Success;
        }
    }
}