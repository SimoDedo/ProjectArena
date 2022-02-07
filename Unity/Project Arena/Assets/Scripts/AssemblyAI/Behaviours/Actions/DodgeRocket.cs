using System;
using AssemblyAI.AI.Layer2;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class DodgeRocket : Action
    {
        [SerializeField] private SharedTransform rocketToDodge;
        private NavigationSystem navSystem;
        private Transform entityTransform;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            entityTransform = entity.transform;
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (navSystem.HasPath() && !navSystem.HasArrivedToDestination())
            {
                navSystem.MoveAlongPath();
                return TaskStatus.Running;
            }
            
            // This rocket is not mine... How do I dodge it?
            // Calculate it's trajectory and try to get away from it

            var projectileTransform = rocketToDodge.Value;
            var projectilePosition = projectileTransform.position;
            var projectileDirection = projectileTransform.forward;

            var myDirection = entityTransform.position - projectilePosition;
            var up = entityTransform.up;
            var angle = Vector3.SignedAngle(myDirection, projectileDirection, up);

            // Try to strife in direction that increases this angle
            var avoidDirection = Vector3.Cross(up, myDirection).normalized;
            if (angle > 0f) avoidDirection = -avoidDirection;
            var entityPosition = entityTransform.position;
            Debug.DrawLine(
                entityPosition,
                entityPosition + avoidDirection * navSystem.Speed,
                Color.cyan,
                0f,
                true
            );
            var path = navSystem.CalculatePath(entityPosition + avoidDirection);
            if (path.IsComplete())
            {
                navSystem.SetPath(path);
            }

            return TaskStatus.Running;
        }
    }
}