using System;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.Core;
using Others;
using UnityEngine;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Attempts to dodge the rocket specified.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class DodgeRocket : Task
    {
        public string rocketToDodgeKey;
        private Transform rocketToDodge;
        private Transform entityTransform;
        private NavigationSystem navSystem;

        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            entityTransform = entity.transform;
        }

        public override void OnEnter()
        {
            rocketToDodge = Blackboard.Get<Transform>(rocketToDodgeKey);
        }

        public override void OnExit()
        {
            navSystem.CancelPath();
        }

        public override Status Run()
        {
            if (navSystem.HasPath() && !navSystem.HasArrivedToDestination())
            {
                navSystem.MoveAlongPath();
                return Status.Running;
            }

            // This rocket is not mine... How do I dodge it?
            // Calculate it's trajectory and try to get away from it
            
            var projectileTransform = rocketToDodge;
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
            if (path.IsComplete()) navSystem.SetPath(path);

            return Status.Running;
        }
    }
}